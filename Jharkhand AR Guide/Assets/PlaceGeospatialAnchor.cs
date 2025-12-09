using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceGeospatialAnchor : MonoBehaviour
{
    public GameObject AnchorPrefab;
    public ARAnchorManager AnchorManager;
    public AREarthManager EarthManager;

    void Update()
    {
        if (EarthManager.EarthTrackingState == TrackingState.Tracking)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var pose = EarthManager.CameraGeospatialPose;
                ARGeospatialAnchor anchor = AnchorManager.AddAnchor(
                    pose.Latitude, pose.Longitude, pose.Altitude, Quaternion.identity);
                Instantiate(AnchorPrefab, anchor.transform);
            }
        }
    }
}
