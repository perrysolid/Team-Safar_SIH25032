using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class GeminiVisionClient : MonoBehaviour
{
    [Header("Google API Key")]
    public string apiKey = "YOUR_API_KEY";

    private string geminiEndpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

    [Header("AR")]
    public ARCameraManager cameraManager;

    [Header("UI")]
    public TMP_Text combinedOutput;
    public GameObject imageSumPanel;
    public ARNavigator aRNavigator;


    public double latitude = 0;
    public double longitude = 0;
    private bool locationReady = false;

    // void Start()
    // {
    //     StartCoroutine(StartGPS());
    // }

    // IEnumerator StartGPS()
    // {
    //     if (!Input.location.isEnabledByUser)
    //     {
    //         locationReady = false;
    //         yield break;
    //     }

    //     Input.location.Start();

    //     int wait = 12;
    //     while (Input.location.status == LocationServiceStatus.Initializing && wait > 0)
    //     {
    //         yield return new WaitForSeconds(1);
    //         wait--;
    //     }

    //     if (Input.location.status == LocationServiceStatus.Running)
    //     {
    //         latitude = Input.location.lastData.latitude;
    //         longitude = Input.location.lastData.longitude;
    //         locationReady = true;
    //     }
    //     else
    //     {
    //         locationReady = false;
    //     }
    // }

    // Called from UI button
    public void ScanButton()
    {
        StartCoroutine(CapturePhoto());
    }

    IEnumerator CapturePhoto()
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            combinedOutput.text = "Camera image unavailable.";
            yield break;
        }

        // MUST use Async conversion in Unity 6+
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
            outputFormat = TextureFormat.RGB24,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // Start async request
        var request = cpuImage.ConvertAsync(conversionParams);
        cpuImage.Dispose();

        // Wait for completion
        while (!request.status.IsDone())
        {
            yield return null;
        }

        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            combinedOutput.text = "Image conversion failed.";
            request.Dispose();
            yield break;
        }
        imageSumPanel.SetActive(true);
        // Extract data
        var rawData = request.GetData<byte>();
        request.Dispose();

        Texture2D tex = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            TextureFormat.RGB24,
            false
        );

        tex.LoadRawTextureData(rawData);
        tex.Apply();

        string base64 = Convert.ToBase64String(tex.EncodeToJPG(70));
        Destroy(tex);

        // Update GPS if available
        if (locationReady && Input.location.status == LocationServiceStatus.Running)
        {
            latitude = Input.location.lastData.latitude;
            longitude = Input.location.lastData.longitude;
        }
        if(latitude==0 || longitude==0)
        {
            latitude=aRNavigator.miniMap.currentLat;
            longitude=aRNavigator.miniMap.currentLng;
        }

        StartCoroutine(SendToGemini(base64));
    }


    IEnumerator SendToGemini(string base64Image)
    {
        string url = geminiEndpoint + apiKey;

        // ---- FINAL PREMIUM PROMPT ----
        string prompt = @"
You are an advanced multimodal AR Tourist Guide. You interpret:
1. The real-world image.
2. The user's approximate GPS coordinates.
Latitude = " + latitude.ToString("F6") + @", Longitude = " + longitude.ToString("F6") + @".
  
IMPORTANT PRIORITY RULES (follow strictly):

PRIORITY 1 — IMAGE (70% weight)
- Identify ONLY what the image visually shows.
- Describe all visible objects, people, furniture, statues, temples, items, surroundings.
- The visual content MUST be the primary basis.
- If the image is a generic indoor room (hostel/home/office), do NOT assume a specific institution unless visible.

PRIORITY 2 — LOCATION (30% weight)
- Use coordinates ONLY to refine interpretation.
- If indoors: use location softly (e.g., ""may be inside campus""), but do NOT assert institution-specific identity.
- If outdoors and landmark-specific: use location to identify temple, statue, monument, or building.

FACTS & SIGNIFICANCE:
- Provide cultural, historical, or contextual facts ONLY about the OBJECT visible.
- For deities: mythology & symbolism.
- For hostel rooms: typical student lifestyle context.
- For objects: their purpose and usage.

NEARBY PLACES:
- ONLY if image seems outdoors/public → list 2–5 relevant nearby places.
- If indoors → return [].

OUTPUT FORMAT — RAW JSON ONLY:
{
  ""identified_object"": ""..."",
  ""actual_location_inference"": ""..."",
  ""description"": ""..."",
  ""historical_cultural_facts"": ""..."",
  ""nearby_places"": [],
  ""confidence"": ""high | medium | low""
}

STRICT RULES:
- NO markdown.
- NO backticks.
- NO ```json fences.
- NEVER override what the image actually shows.
- ALWAYS output only one JSON object.
";

        string jsonBody = @"
{
  ""contents"": [
    {
      ""parts"": [
        { ""text"": """ + Escape(prompt) + @""" },
        {
          ""inline_data"": {
            ""mime_type"": ""image/jpeg"",
            ""data"": """ + base64Image + @"""
          }
        }
      ]
    }
  ]
}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            combinedOutput.text = "Error: " + req.error;
            yield break;
        }

        string raw = req.downloadHandler.text;
        Debug.Log("GEMINI RAW:\n" + raw);

        ParseGeminiJSON(raw);
    }


    // --- BULLETPROOF JSON EXTRACTION ---
//     string ExtractInnerJson(string raw)
// {
//     int textIndex = raw.IndexOf("\"text\":");
//     if (textIndex < 0) return null;

//     // Beginning of the quoted JSON string
//     int startQuote = raw.IndexOf("\"", textIndex + 7);
//     if (startQuote < 0) return null;

//     bool escape = false;
//     bool inside = true;
//     System.Text.StringBuilder sb = new System.Text.StringBuilder();

//     // Extract until the REAL closing quote (not an escaped \")
//     for (int i = startQuote + 1; i < raw.Length && inside; i++)
//     {
//         char c = raw[i];

//         if (escape)
//         {
//             sb.Append(c);
//             escape = false;
//         }
//         else if (c == '\\')
//         {
//             escape = true;
//         }
//         else if (c == '"')
//         {
//             // END OF STRING
//             inside = false;
//         }
//         else
//         {
//             sb.Append(c);
//         }
//     }

//     string encoded = sb.ToString();

//     // Now unescape to get REAL JSON
//     encoded = encoded.Replace("\\n", "\n");
//     encoded = encoded.Replace("\\\"", "\"");
//     encoded = encoded.Replace("\\\\", "\\");

//     return encoded;
// }


    void ParseGeminiJSON(string raw)
    {
        // 1. Parse the Outer Layer (The Google API wrapper) using SimpleJSON
        JSONNode node = JSON.Parse(raw);

        // Check for API errors or empty responses
        if (node == null || node["candidates"] == null)
        {
            combinedOutput.text = "Error: Invalid API response.";
            return;
        }

        // 2. Extract the specific text string. 
        // SimpleJSON handles the unescaping of \" automatically here.
        string contentText = node["candidates"][0]["content"]["parts"][0]["text"];

        if (string.IsNullOrEmpty(contentText))
        {
            combinedOutput.text = "Error: No text in response.";
            return;
        }

        // 3. SAFETY CLEANUP: Remove Markdown formatting
        // Even with strict prompts, Gemini sometimes wraps JSON in ```json ... ```
        contentText = contentText.Replace("```json", "").Replace("```", "").Trim();

        // 4. Parse the Inner Layer (Your actual data)
        JSONNode json = JSON.Parse(contentText);

        if (json == null)
        {
            combinedOutput.text = "Failed to parse content JSON.\nRaw: " + contentText;
            return;
        }

        // --- BUILD COMBINED OUTPUT (Same as your logic) ---
        string output =
            "<b>What You're Seeing:</b>\n" + json["identified_object"] + "\n\n" +
            "<b>Location Insight:</b>\n" + json["actual_location_inference"] + "\n\n" +
            "<b>Description:</b>\n" + json["description"] + "\n\n" +
            "<b>Cultural / Historical Facts:</b>\n" + json["historical_cultural_facts"] + "\n\n" +
            "<b>Nearby Places:</b>\n";

        var places = json["nearby_places"].AsArray;

        if (places == null || places.Count == 0)
        {
            output += "None (indoor environment)\n\n";
        }
        else
        {
            foreach (KeyValuePair<string, JSONNode> p in places)
            {
                JSONNode item = p.Value;
                output += "- " + item["name"] + " (" + item["type"] + "), ~" +
                          item["distance_km"] + " km\n";
            }
            output += "\n";
        }

        output += "<b>Confidence:</b> " + json["confidence"];

        combinedOutput.text = output;
        Debug.Log("Parsed Final Output:\n" + output);
    }
    // Escape unsafe characters
    string Escape(string s)
    {
        return s.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
