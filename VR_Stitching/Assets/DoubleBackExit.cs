using UnityEngine;

public class DoubleBackExit : MonoBehaviour
{
    // How many seconds the user has to press back again
    private const float ExitWindow = 2.0f;
    private float _lastBackPressTime;

    void Update()
    {
        // On Android, KeyCode.Escape is the physical/gesture Back Button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Time.time - _lastBackPressTime < ExitWindow)
            {
                // 1. If pressed twice quickly, Quit Unity
                // This will return the user to the Flutter app (or OS)
                Debug.Log("Exiting Unity...");
                Application.Quit();
            }
            else
            {
                // 2. If first press, update time and show message
                _lastBackPressTime = Time.time;
                ShowAndroidToast("Press back again to exit");
            }
        }
    }

    // Helper to show the native Android gray bubble message
    private void ShowAndroidToast(string message)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", context, message, 0); // 0 = Toast.LENGTH_SHORT
            
            toast.Call("show");
        }
        catch (System.Exception e)
        {
            // Fallback for errors
            Debug.Log("Toast Error: " + e.Message);
        }
        #else
            // Fallback for Unity Editor testing
            Debug.Log("Toast: " + message);
        #endif
    }
}