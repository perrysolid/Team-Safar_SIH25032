using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;
using TMPro;
using UnityEngine.SceneManagement;

public class ARNavigator : MonoBehaviour
{
    [Header("AR Components")]
    public AREarthManager earthManager;
    public ARAnchorManager anchorManager;
    public GoogleRouteFetcher routeFetcher;
    public MiniMapController miniMap;
    
    [Header("Visuals")]
    public LineRenderer pathLine;
    public GameObject robotBot;
    public GameObject waypointPrefab; 

    [Header("UI & Debug")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI debugText; 

    [Header("Destination")]
    public double destLat = 24.4921; // Default: Baidyanath
    public double destLng = 86.7003;

    [Header("Testing")]
    public bool useRoomTestMode = true; // CHECK THIS FOR INDOOR TESTING

    private bool isScanning = true;
    private bool pathCreated = false;
    private List<GameObject> activeAnchors = new List<GameObject>();

    // NEW: Variables to remember where we are actually going
    private double activeTargetLat;
    private double activeTargetLng;

    void Update()
    {
        // 1. Safety Checks
        if (ARSession.state != ARSessionState.SessionTracking) 
        {
            if(debugText) debugText.text = "AR Initializing...";
            return; 
        }
        
        if (!earthManager || earthManager.EarthState != EarthState.Enabled)
        {
            if (debugText) debugText.text = "Waiting for VPS...";
            return;
        }

        // Pass current position to minimap for blue dot updates
        var pose = earthManager.CameraGeospatialPose;
        if(miniMap != null)
        {
            miniMap.currentLat = pose.Latitude;
            miniMap.currentLng = pose.Longitude;
        }

        // 2. Arrival Logic
        if (pathCreated) 
        {
            float dist = Vector3.Distance(Camera.main.transform.position, robotBot.transform.position);
            if (dist < 5.0f) {
                if(infoPanel) infoPanel.SetActive(true);
            }
        }

        // 3. Status
        UpdateDebugStatus();

        // 4. Scanning Logic (Loose check for indoors)
        if (isScanning && pose.OrientationYawAccuracy < 180 && pose.HorizontalAccuracy < 50)
        {
            // CRITICAL: Ensure we aren't at 0,0 (Ocean)
            if(pose.Latitude == 0 && pose.Longitude == 0) return;

            isScanning = false;
            
            // --- LOGIC FIX START ---
            
            if (useRoomTestMode)
            {
                if (debugText) debugText.text = "TEST MODE: Path 10m away...";
                // Generate Fake Target
                activeTargetLat = pose.Latitude + 0.00015; 
                activeTargetLng = pose.Longitude + 0.00015;
            }
            else
            {
                if (debugText) debugText.text = "Fetching Real Route...";
                // Use Real Target
                activeTargetLat = destLat;
                activeTargetLng = destLng;
            }

            Debug.Log($"Routing from {pose.Latitude},{pose.Longitude} TO {activeTargetLat},{activeTargetLng}");

            // Fetch Route using ACTIVE coordinates
            StartCoroutine(routeFetcher.FetchRoute(
                pose.Latitude, pose.Longitude,
                activeTargetLat, activeTargetLng,
                OnRouteReceived
            ));
            
            // --- LOGIC FIX END ---
        }
    }

    void UpdateDebugStatus()
    {
        var pose = earthManager.CameraGeospatialPose;
        string status = "";

        if (pose.OrientationYawAccuracy < 25)
            status += "<color=green>STREET VIEW LOCKED</color>\n";
        else
            status += "<color=yellow>COMPASS MODE (LOW ACC)</color>\n";

        status += $"Target: {destLat:F4}, {destLng:F4}\n";
        
        if(debugText) debugText.text = status;
    }

    void OnRouteReceived(List<Vector2> latLngPath, string encodedPath)
    {
        if(debugText) debugText.text = $"Route Found: {latLngPath.Count} pts. Optimizing...";
        
        pathLine.positionCount = 0; // Reset line
        var pose = earthManager.CameraGeospatialPose;
        
        // --- OPTIMIZATION START ---
        
        // 1. Determine "Step Size" based on path length
        // If path is huge (500 points), skip more. If short, keep detail.
        int step = 1;
        if (latLngPath.Count > 100) step = 5; // Skip 4 out of 5 points
        else if (latLngPath.Count > 50) step = 2;

        int lineIndex = 0;

        for (int i = 0; i < latLngPath.Count; i += step) // Increment by Step, not 1
        {
            // Always include the very last point so the path doesn't stop short
            if (i >= latLngPath.Count) i = latLngPath.Count - 1;

            // Create Anchor
            double alt = pose.Altitude - 1.5;
            ARGeospatialAnchor anchor = anchorManager.AddAnchor(latLngPath[i].x, latLngPath[i].y, alt, Quaternion.identity);

            if (anchor != null)
            {
                activeAnchors.Add(anchor.gameObject);
                
                // Update Line Renderer
                pathLine.positionCount++;
                pathLine.SetPosition(lineIndex, anchor.transform.position);
                lineIndex++;

                // Robot Logic (Place at first valid anchor)
                if (lineIndex == 1) 
                {
                    robotBot.transform.position = anchor.transform.position;
                    robotBot.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - anchor.transform.position);
                    robotBot.SetActive(true);
                }
            }
            
            // Force break loop if we hit the end
            if (i == latLngPath.Count - 1) break;
        }
        
        // --- OPTIMIZATION END ---

        // Update Map
        if (miniMap != null)
        {
            // If encodedPath is empty here, the map line will fail.
            // Check if this Log prints a string length > 0
            Debug.LogError($"Path String Length: {encodedPath?.Length}"); 
            
            miniMap.StartLiveMap(activeTargetLat, activeTargetLng, encodedPath);
        }

        pathCreated = true;
        StartCoroutine(UpdateLinePositions());
    }

    IEnumerator UpdateLinePositions()
    {
        while(true)
        {
            for (int i = 0; i < activeAnchors.Count; i++)
            {
                if (activeAnchors[i] != null)
                {
                    pathLine.SetPosition(i, activeAnchors[i].transform.position);
                }
            }
            yield return new WaitForSeconds(0.2f); 
        }
    }

    public void SetDestination(double lat, double lng)
    {
        // 1. Cleanup Old Path
        foreach(var obj in activeAnchors) 
        {
            if(obj != null) Destroy(obj);
        }
        activeAnchors.Clear();
        
        pathLine.positionCount = 0;
        robotBot.SetActive(false);
        
        if(infoPanel) infoPanel.SetActive(false);

        // 2. Set the Real Destination Variables
        destLat = lat;
        destLng = lng;

        // 3. Logic Reset
        pathCreated = false;
        isScanning = true; 
        
        // 4. Disable "Fake Room Mode" because we have a Real Target from the search
        useRoomTestMode = false; 

        // --- CRITICAL FIX: UPDATE THE MAP IMMEDIATELY ---
        // This puts the Red Marker on the map instantly, even before you tap to start.
        if (miniMap != null)
        {
            // We pass 'null' for the path string because we haven't calculated the blue line yet.
            // But we pass the Lat/Lng so the Red Dot appears.
            miniMap.StartLiveMap(destLat, destLng, null);
            // mini
        }

        if(debugText) debugText.text = $"Target Found!\n{lat}, {lng}\nTap screen to start.";
        var pose = earthManager.CameraGeospatialPose;

        if (isScanning && pose.OrientationYawAccuracy < 180 && pose.HorizontalAccuracy < 50)
        {
            // CRITICAL: Ensure we aren't at 0,0 (Ocean)
            if(pose.Latitude == 0 && pose.Longitude == 0) return;

            isScanning = false;
            
            // --- LOGIC FIX START ---
            
            if (useRoomTestMode)
            {
                if (debugText) debugText.text = "TEST MODE: Path 10m away...";
                // Generate Fake Target
                activeTargetLat = pose.Latitude + 0.00015; 
                activeTargetLng = pose.Longitude + 0.00015;
            }
            else
            {
                if (debugText) debugText.text = "Fetching Real Route...";
                // Use Real Target
                activeTargetLat = destLat;
                activeTargetLng = destLng;
            }

            Debug.Log($"Routing from {pose.Latitude},{pose.Longitude} TO {activeTargetLat},{activeTargetLng}");

            // Fetch Route using ACTIVE coordinates
            StartCoroutine(routeFetcher.FetchRoute(
                pose.Latitude, pose.Longitude,
                activeTargetLat, activeTargetLng,
                OnRouteReceived
            ));
            
            // --- LOGIC FIX END ---
        }
    }
    void Quit()
    {
        SceneManager.LoadScene("Main Scene");
    }
}