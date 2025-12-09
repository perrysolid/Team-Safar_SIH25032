using UnityEngine;
using UnityEngine.SceneManagement;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }
    public string deepLinkURL;
    public VRVideoLoader vRVideoLoader;
    private void Awake()
    {
        // Singleton pattern to keep this alive across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 1. Subscribe to the event (fires if app is already running)
            Application.deepLinkActivated += OnDeepLinkActivated;
            
            // 2. Check if app was just started BY a link (Cold Start)
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDeepLinkActivated(string url)
    {
        // 3. Store and Process the URL
        deepLinkURL = url;
        Debug.Log("Link received: " + url);

        // Example: Parse the URL to find parameters
        // url might be: myunityapp://game?level=5
        if (url.Contains("level=1"))
        {
            vRVideoLoader.i=1;
            // Load the specific scene or trigger logic
            // Debug.Log("Loading Level 5...");
            // // SceneManager.LoadScene("Level5"); 
        }
        if (url.Contains("level=2"))
        {
            vRVideoLoader.i=2;
        }
    }
}