using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI; // Required for Button

public class IndoorNavigationManager : MonoBehaviour
{
    [Header("Components")]
    public ARRaycastManager raycastManager;
    public GameObject robotGuide;
    
    [Header("Prefabs")]
    public GameObject arrowPrefab; // Drag your Green Cube/Arrow here

    [Header("UI")]
    public Button clearButton;

    private List<GameObject> spawnedArrows = new List<GameObject>();
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        robotGuide.SetActive(false);
        // Setup Button Listener
        if(clearButton) clearButton.onClick.AddListener(ClearPath);
    }

    void Update()
    {
        // 1. Detect Touch (Standard Input)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // Check if touching a UI button (don't place arrow if clicking button)
            if (IsPointerOverUI(Input.GetTouch(0))) return;

            // 2. Raycast against the detected floor planes
            if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                AddPathPoint(hitPose.position);
            }
        }
    }

    void AddPathPoint(Vector3 position)
    {
        // A. Spawn the new arrow
        GameObject newArrow = Instantiate(arrowPrefab, position + Vector3.up * 0.05f, Quaternion.identity);
        
        // B. Handle Rotation (Make previous arrow look at this new one)
        if (spawnedArrows.Count > 0)
        {
            GameObject previousArrow = spawnedArrows[spawnedArrows.Count - 1];
            
            // Look at the new point, but keep flat on ground (ignore Y height diff)
            Vector3 lookTarget = newArrow.transform.position;
            lookTarget.y = previousArrow.transform.position.y; 
            
            previousArrow.transform.LookAt(lookTarget);
        }

        // C. Add to list
        spawnedArrows.Add(newArrow);

        // D. Move Robot to the NEW point (The "Goal")
        UpdateRobotPosition(position, newArrow.transform.rotation);
    }

    void UpdateRobotPosition(Vector3 pos, Quaternion rot)
    {
        robotGuide.SetActive(true);
        robotGuide.transform.position = pos;
        
        // Make robot look at user initially, or match arrow direction
        robotGuide.transform.LookAt(Camera.main.transform); 
        
        // Correct Robot Tilt (Keep him upright)
        Vector3 euler = robotGuide.transform.eulerAngles;
        robotGuide.transform.rotation = Quaternion.Euler(0, euler.y, 0);
    }

    // Helper to clear everything
    public void ClearPath()
    {
        foreach (var obj in spawnedArrows)
        {
            Destroy(obj);
        }
        spawnedArrows.Clear();
        robotGuide.SetActive(false);
    }

    // Helper to prevent clicking through UI buttons
    private bool IsPointerOverUI(Touch touch)
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }
}