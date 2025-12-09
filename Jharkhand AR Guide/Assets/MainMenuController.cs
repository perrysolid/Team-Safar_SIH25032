using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Required to change scenes

public class MainMenuController : MonoBehaviour
{
    public TMP_InputField inputField;

    public void OnStartNavigationClicked()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            // 1. Save the text to our static memory
            AppManager.TargetLocationName = inputField.text;

            // 2. Load the AR Scene
            // IMPORTANT: Make sure your AR scene is exactly named "ARScene" (or change this line)
            SceneManager.LoadScene("Using Maps & Directions"); 
        }
    }
    public void OnIndoorStart()
    {
        SceneManager.LoadScene("NavigationSampleNavMesh");
    }
    public void Quit()
    {
        Application.Quit();
    }
    
}