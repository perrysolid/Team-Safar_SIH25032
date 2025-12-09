using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleRouteFetcher : MonoBehaviour
{
    [Header("API Settings")]
    public string apiKey="AIzaSyA6-JmTd3lps1Xh-hdwV1LOwTciY0lfQvk"; // PASTE KEY IN INSPECTOR, NOT HERE!

    [Serializable]
    public class GoogleRouteResponse { public Route[] routes; }
    [Serializable]
    public class Route { public OverviewPolyline overview_polyline; }
    [Serializable]
    public class OverviewPolyline { public string points; }

    public IEnumerator FetchRoute(double currentLat, double currentLng, double destLat, double destLng, Action<List<Vector2>, string> onPathFound)
    {
        // 1. Check if Key is missing
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("GoogleRouteFetcher: API Key is empty! Check Inspector.");
            yield break;
        }
        if(currentLat==0 || currentLng==0|| destLat==0 || destLng==0)
        yield break;
        
        string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={currentLat},{currentLng}&destination={destLat},{destLng}&mode=walking&key={apiKey}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GoogleRouteResponse>(request.downloadHandler.text);
                
                // 2. Safety Check: Did Google return a route?
                if (response != null && response.routes != null && response.routes.Length > 0)
                {
                    string encodedPoints = response.routes[0].overview_polyline.points;
                    List<Vector2> latLngList = DecodePolyline(encodedPoints);
                    
                    // Success! Pass both the list (for AR) and the string (for MiniMap)
                    onPathFound?.Invoke(latLngList, encodedPoints);
                }
                else
                {
                    Debug.LogError("Google API: No routes found. (Are points in the ocean?)");
                }
            }
            else
            {
                Debug.LogError("Google Network Error: " + request.error + "\n" + request.downloadHandler.text);
            }
        }
    }

    private List<Vector2> DecodePolyline(string encodedPoints)
    {
        if (string.IsNullOrEmpty(encodedPoints)) return null;
        List<Vector2> poly = new List<Vector2>();
        char[] polylinechars = encodedPoints.ToCharArray();
        int index = 0;
        int currentLat = 0;
        int currentLng = 0;

        while (index < polylinechars.Length)
        {
            int sum = 0;
            int shifter = 0;
            int next5bits;
            do {
                next5bits = polylinechars[index++] - 63;
                sum |= (next5bits & 31) << shifter;
                shifter += 5;
            } while (next5bits >= 32);
            int dLat = (sum & 1) != 0 ? ~(sum >> 1) : (sum >> 1);
            currentLat += dLat;

            sum = 0;
            shifter = 0;
            do {
                next5bits = polylinechars[index++] - 63;
                sum |= (next5bits & 31) << shifter;
                shifter += 5;
            } while (next5bits >= 32);
            int dLng = (sum & 1) != 0 ? ~(sum >> 1) : (sum >> 1);
            currentLng += dLng;

            poly.Add(new Vector2(currentLat / 100000.0f, currentLng / 100000.0f));
        }
        return poly;
    }
}