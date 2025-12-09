using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // For Text Mesh Pro Input Field

public class LocationSearch : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField searchInput; // Drag your Input Field here
    public TextMeshProUGUI statusText; // To show "Searching..." or Error

    [Header("Connections")]
    public ARNavigator arNavigator;    // Drag your ARNavigator script here
    public GoogleRouteFetcher routeFetcher; // Drag Route Fetcher to get API Key

    [Serializable]
    public class GeocodeResponse { public GeocodeResult[] results; }
    [Serializable]
    public class GeocodeResult { public Geometry geometry; }
    [Serializable]
    public class Geometry { public Location location; }
    [Serializable]
    public class Location { public double lat; public double lng; }
    // Add this inside LocationSearch class
    void Start()
    {
        // Check if we came from the Main Menu with a target
        if (!string.IsNullOrEmpty(AppManager.TargetLocationName))
        {
            Debug.Log("Auto-Searching for: " + AppManager.TargetLocationName);
            
            // Put the text in the local box (visual feedback)
            if(searchInput) searchInput.text = AppManager.TargetLocationName;

            // Trigger the search automatically
            StartCoroutine(GetCoordinatesFromAddress(AppManager.TargetLocationName));

            // Clear the memory so it doesn't trigger again if we reload scene
            AppManager.TargetLocationName = "";
        }
    }

    public void OnSearchButtonClicked()
    {
        string locationName = searchInput.text;
        if (!string.IsNullOrEmpty(locationName))
        {
            StartCoroutine(GetCoordinatesFromAddress(locationName));
        }
    }

    IEnumerator GetCoordinatesFromAddress(string address)
    {
        statusText.text = "Searching...";
        
        // 1. Get API Key from the other script (so you don't paste it twice)
        string apiKey = routeFetcher.apiKey; 
        
        // 2. Encode the address (spaces become %20, etc.)
        string encodedAddress = UnityWebRequest.EscapeURL(address);

        // 3. Build URL
        string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={apiKey}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 4. Parse JSON
                GeocodeResponse response = JsonUtility.FromJson<GeocodeResponse>(request.downloadHandler.text);

                if (response.results.Length > 0)
                {
                    double lat = response.results[0].geometry.location.lat;
                    double lng = response.results[0].geometry.location.lng;

                    statusText.text = $"Found! {address} = {lat}, {lng}";
                    Debug.Log($"Geocoding Success: {address} = {lat}, {lng}");

                    // 5. Send to AR Navigator
                    // We call a new function in ARNavigator to update the destination
                    arNavigator.SetDestination(lat, lng);
                }
                else
                {
                    statusText.text = "Location not found.";
                }
            }
            else
            {
                statusText.text = "Network Error.";
                Debug.LogError(request.error);
            }
        }
    }
}