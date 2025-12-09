using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;

public class FallbackNavigator : MonoBehaviour
{
    [Header("AR System")]
    public AREarthManager earthManager;
    public Transform userCamera; 

    [Header("The Guide")]
    public GameObject robotGuide; 
    public TextMeshProUGUI distanceText; 
    public TextMeshProUGUI instructionText; 

    [Header("Destination (Set these in Inspector!)")]
    public double destLat = 24.4921; // Example: Baidyanath
    public double destLng = 86.7003;

    private bool isCompassMode = true; // DEFAULT TO TRUE (Safer for indoors)
    private bool isGPSReady = false;

    IEnumerator Start()
    {
        // 1. Hide robot initially so it doesn't clip into face
        robotGuide.SetActive(false);
        instructionText.text = "Waiting for GPS...";
        distanceText.text = "--";

        // 2. Start Sensors
        Input.compass.enabled = true;
        Input.location.Start(5f, 5f);

        // 3. Wait for GPS (Fixes the 8000km bug)
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            instructionText.text = "GPS Failed. Go Outside.";
        }
        else
        {
            isGPSReady = true;
            instructionText.text = "GPS Ready. Aligning...";
        }
        float currentLat = Input.location.lastData.latitude;
        float currentLng = Input.location.lastData.longitude;
        Debug.Log("Lat "+currentLat + "Lng"+currentLng);
    }

    void Update()
    {
        // 1. Monitor AR Status (The Crash Fix)
        // We wrap this in Try-Catch because Google's SDK can be buggy indoors
        try 
        {
            CheckARStatus();
        }
        catch 
        {
            // If AR crashes, force Compass Mode silently
            if (!isCompassMode) SwitchToCompassMode();
        }

        // 2. Run the Logic
        if (isCompassMode && isGPSReady)
        {
            UpdateCompassMode();
        }
    }

    void CheckARStatus()
    {
        // SAFETY: If manager is missing or Session isn't running, STOP.
        if (earthManager == null || ARSession.state != ARSessionState.SessionTracking)
        {
            if (!isCompassMode) SwitchToCompassMode();
            return;
        }

        // Only check EarthState if we are 100% sure AR is running
        if (earthManager.EarthState == EarthState.Enabled && 
            earthManager.EarthTrackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
        {
            if (isCompassMode) SwitchToVPSMode();
        }
        else
        {
            // Indoors/Lost -> Go Compass
            if (!isCompassMode) SwitchToCompassMode();
        }
    }

    void SwitchToCompassMode()
    {
        isCompassMode = true;
        robotGuide.SetActive(true); 
    }

    void SwitchToVPSMode()
    {
        isCompassMode = false;
        // In a full app, you would enable the AR Line Renderer here
        // For now, let's keep the robot but maybe change text
    }

    void UpdateCompassMode()
    {
        // A. GET DATA
        float currentLat = Input.location.lastData.latitude;
        float currentLng = Input.location.lastData.longitude;
        Debug.Log("Lat "+currentLat + "Lng"+currentLng);
        // Prevent (0,0) Bug
        if (currentLat == 0 && currentLng == 0) return;

        // B. CALCULATE DISTANCE & BEARING
        float distance = CalculateHaversineDistance(currentLat, currentLng, destLat, destLng);
        float bearing = CalculateBearing(currentLat, currentLng, destLat, destLng);
        float userHeading = Input.compass.trueHeading;

        // C. UPDATE UI
        distanceText.text = $"{Mathf.FloorToInt(distance)} meters";

        // D. POSITION ROBOT (The White Glitch Fix)
        // We force the robot to be 2 meters AWAY from the camera, at chest height.
        
        float relativeAngle = bearing - userHeading;
        Quaternion rotationToTarget = Quaternion.Euler(0, relativeAngle, 0);
        
        // Offset: 2 meters forward, 1 meter down (relative to camera)
        Vector3 targetPos = userCamera.position + (rotationToTarget * Vector3.forward * 2.0f);
        targetPos.y = userCamera.position.y - 1.2f; 

        // Smooth movement
        robotGuide.transform.position = Vector3.Lerp(robotGuide.transform.position, targetPos, Time.deltaTime * 5f);
        
        // Face the user
        robotGuide.transform.LookAt(userCamera);
        robotGuide.transform.eulerAngles = new Vector3(0, robotGuide.transform.eulerAngles.y, 0); // Keep upright

        // E. TURN INSTRUCTIONS
        float angleDiff = Mathf.DeltaAngle(0, relativeAngle); 
        if(Mathf.Abs(angleDiff) > 20) {
            instructionText.text = (angleDiff > 0) ? "Turn Right ->" : "<- Turn Left";
        } else {
            instructionText.text = "Walk Straight";
        }
    }

    // --- MATH HELPERS ---
    float CalculateHaversineDistance(float lat1, float lon1, double lat2, double lon2)
    {
        float R = 6371000; 
        float dLat = (float)((lat2 - lat1) * Mathf.Deg2Rad);
        float dLon = (float)((lon2 - lon1) * Mathf.Deg2Rad);
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos((float)(lat2 * Mathf.Deg2Rad)) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }

    float CalculateBearing(float startLat, float startLng, double destLat, double destLng)
    {
        float startLatRad = startLat * Mathf.Deg2Rad;
        float startLngRad = startLng * Mathf.Deg2Rad;
        float destLatRad = (float)destLat * Mathf.Deg2Rad;
        float destLngRad = (float)destLng * Mathf.Deg2Rad;

        float y = Mathf.Sin(destLngRad - startLngRad) * Mathf.Cos(destLatRad);
        float x = Mathf.Cos(startLatRad) * Mathf.Sin(destLatRad) -
                  Mathf.Sin(startLatRad) * Mathf.Cos(destLatRad) * Mathf.Cos(destLngRad - startLngRad);

        return (Mathf.Atan2(y, x) * Mathf.Rad2Deg + 360) % 360;
    }
}