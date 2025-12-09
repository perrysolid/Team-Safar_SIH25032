using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Globalization; // Needed for dot format (24.5 vs 24,5)

public class MiniMapController : MonoBehaviour
{
    [Header("Components")]
    public RawImage mapImage;
    public double currentLat;
    public double currentLng;

    [Header("Settings")]
    public string apiKey = "YOUR_API_KEY_HERE";
    public int zoomLevel = 18; 
    public int mapSize = 400;
    
    [Header("Live Update Settings")]
    public float updateDistanceMeters = 10f; 
    public float minUpdateInterval = 5f;     

    private double destLat, destLng;
    private string currentEncodedPath;
    private bool isTracking = false;
    
    public double lastLat, lastLng;
    private float timeSinceLastUpdate = 0f;

    public void StartLiveMap(double dLat, double dLng, string encodedPath)
    {
        destLat = dLat;
        destLng = dLng;
        currentEncodedPath = encodedPath;
        isTracking = true;
        RefreshMap();
    }

    void Update()
    {
        if (!isTracking) return;

        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate > minUpdateInterval)
        {
            CheckMovement();
        }
    }

    void CheckMovement()
    {
        if(currentLat == 0 && currentLng == 0) return;

        float distanceMoved = CalculateDist(currentLat, currentLng, lastLat, lastLng);

        if (distanceMoved >= updateDistanceMeters)
        {
            RefreshMap();
        }
    }

    void RefreshMap()
    {
        if(currentLat == 0 && currentLng == 0) return;

        lastLat = currentLat;
        lastLng = currentLng;
        timeSinceLastUpdate = 0f;

        StartCoroutine(FetchMapRoutine(lastLat, lastLng));
    }

    public void ForceRefresh()
    {
        RefreshMap();
    }

    IEnumerator FetchMapRoutine(double userLat, double userLng)
    {
        string baseUrl = "https://maps.googleapis.com/maps/api/staticmap?";
        System.Text.StringBuilder urlBuilder = new System.Text.StringBuilder(baseUrl);

        // 1. Basic Settings
        urlBuilder.Append($"size={mapSize}x{mapSize}");
        urlBuilder.Append("&maptype=roadmap");

        // 2. Zoom Logic (Auto-fit if destination exists)
        bool hasDestination = (destLat != 0 && destLng != 0);
        if (!hasDestination)
        {
            urlBuilder.Append($"&zoom={zoomLevel}");
        }

        // 3. Markers (User = Blue, Destination = Red)
        // Using InvariantCulture to ensure dots (.) instead of commas (,)
        urlBuilder.Append($"&markers=color:blue|label:U|{userLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{userLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        
        if (hasDestination)
        {
            urlBuilder.Append($"&markers=color:red|label:D|{destLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{destLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        // 4. THE PATH FIX (Critical Part)
        if (!string.IsNullOrEmpty(currentEncodedPath))
        {
            // EscapeURL fixes special characters like '\' in the Google string
            string safePath = UnityWebRequest.EscapeURL(currentEncodedPath);
            
            // Set Color to Solid Blue (0x0000ffff) and Weight to 5
            urlBuilder.Append($"&path=weight:5|color:0x0000ffff|enc:{safePath}");
        }
        else
        {
            Debug.LogWarning("MiniMap: Path string is empty. Map will show points only.");
        }

        // 5. Add Key
        urlBuilder.Append("&key=" + apiKey);

        // --- DEBUG: Uncomment this line to see the URL in Console ---
        // Debug.Log("Map URL: " + urlBuilder.ToString()); 

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(urlBuilder.ToString()))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                mapImage.texture = texture;
                mapImage.color = Color.white;
            }
            else
            {
                Debug.LogError($"Map Failed: {request.error}");
            }
        }
    }

    float CalculateDist(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6378.137; 
        var dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        var dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        var a = Mathf.Sin((float)dLat / 2) * Mathf.Sin((float)dLat / 2) +
                Mathf.Cos((float)(lat1 * Mathf.Deg2Rad)) * Mathf.Cos((float)(lat2 * Mathf.Deg2Rad)) *
                Mathf.Sin((float)dLon / 2) * Mathf.Sin((float)dLon / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return (float)(R * c) * 1000f; 
    }
}