using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class PathPlacement : MonoBehaviour
{
    public AREarthManager EarthManager;
    public ARAnchorManager AnchorManager;
    public GameObject ArrowPrefab;

    public TMP_InputField  LatitudeInput;
    public TMP_InputField  LongitudeInput;
    public Button GoButton;

    private void Start()
    {
        GoButton.onClick.AddListener(OnGoClicked);
    }

    void OnGoClicked()
    {
        if (EarthManager.EarthTrackingState != UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
        {
            Debug.Log("Earth Tracking not ready");
            return;
        }

        double latStart = EarthManager.CameraGeospatialPose.Latitude;
        double lonStart = EarthManager.CameraGeospatialPose.Longitude;
        double altStart = EarthManager.CameraGeospatialPose.Altitude;

        double latEnd = double.Parse(LatitudeInput.text);
        double lonEnd = double.Parse(LongitudeInput.text);
        double altEnd = altStart; // for simplicity

        int numWaypoints = 10; // number of arrows to place

        for (int i = 0; i <= numWaypoints; i++)
        {
            double t = (double)i / numWaypoints;
            double lat = Mathf.Lerp((float)latStart, (float)latEnd, (float)t);
            double lon = Mathf.Lerp((float)lonStart, (float)lonEnd, (float)t);
            double alt = altStart;

            var anchor = AnchorManager.AddAnchor(lat, lon, alt, Quaternion.identity);
            if (anchor != null)
            {
                Instantiate(ArrowPrefab, anchor.transform);
            }
        }
    }
}
