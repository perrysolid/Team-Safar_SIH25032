using UnityEngine;
using UnityEngine.UI;

public class MapInteractionController : MonoBehaviour
{
    [Header("Connections")]
    public MiniMapController miniMapScript; 
    public RectTransform mapContainer;      // The Parent (with ScrollRect + Mask)
    public RawImage mapRawImage;            // The Child (The actual Map Picture)
    public ScrollRect mapScrollRect;        // The ScrollRect Component
    public GameObject ExpandButton;
    public GameObject MapPanel;

    [Header("Settings")]
    public float animationSpeed = 5f;

    // State
    private bool isExpanded = false;
    
    
    // Saved "Mini" layout
    private Vector2 originalSize;
    private Vector2 originalPos;
    private Vector2 originalAnchorMin;
    public Vector3 targetImageScale = Vector3.one;
    private Vector2 originalAnchorMax;

    void Start()
    {
        // 1. Save the initial "Mini Mode" state
        originalSize = mapContainer.sizeDelta;
        originalPos = mapContainer.anchoredPosition;
        originalAnchorMin = mapContainer.anchorMin;
        originalAnchorMax = mapContainer.anchorMax;
 
        // 2. Ensure Scrolling is locked initially
        if (mapScrollRect) mapScrollRect.enabled = false;
        MapPanel.SetActive(false);
        targetImageScale = Vector3.one;
    }

    void Update()
    {
        // Smooth Animation Logic
        if (isExpanded)
        {
            // Expand to Full Screen
            mapContainer.anchorMin = Vector2.Lerp(mapContainer.anchorMin, Vector2.zero, Time.deltaTime * animationSpeed);
            mapContainer.anchorMax = Vector2.Lerp(mapContainer.anchorMax, Vector2.one, Time.deltaTime * animationSpeed);
            mapContainer.offsetMin = Vector2.Lerp(mapContainer.offsetMin, Vector2.zero, Time.deltaTime * animationSpeed);
            mapContainer.offsetMax = Vector2.Lerp(mapContainer.offsetMax, Vector2.zero, Time.deltaTime * animationSpeed);

        }
        else
        {
            // Shrink back to Corner
            mapContainer.anchorMin = Vector2.Lerp(mapContainer.anchorMin, originalAnchorMin, Time.deltaTime * animationSpeed);
            mapContainer.anchorMax = Vector2.Lerp(mapContainer.anchorMax, originalAnchorMax, Time.deltaTime * animationSpeed);
            mapContainer.sizeDelta = Vector2.Lerp(mapContainer.sizeDelta, originalSize, Time.deltaTime * animationSpeed);
            mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, originalPos, Time.deltaTime * animationSpeed);
        }
        mapRawImage.rectTransform.localScale = Vector3.Lerp(mapRawImage.rectTransform.localScale, targetImageScale, Time.deltaTime * animationSpeed);
    }

    public void ToggleMapSize()
    {
        isExpanded = !isExpanded;
        
        if (isExpanded)
        {
            ExpandButton.SetActive(false);
            // --- EXPAND MODE ---
            // 1. Download higher resolution map (800x800)
            miniMapScript.mapSize = 800;
            miniMapScript.ForceRefresh();
            MapPanel.SetActive(true);
            targetImageScale = 2.1f*Vector3.one;

            // 2. Enable Scrolling (The Trick)
            // if(mapScrollRect) mapScrollRect.enabled = true;
            
            // 3. Scale up the RawImage slightly so there is "room" to scroll
            // If the image fits perfectly, you can't scroll. We zoom it in a bit.
            // mapRawImage.rectTransform.localScale = new Vector3(1.5f, 1.5f, 1f); 
        }
        else 
        {
            // --- MINI MODE ---
            // 1. Revert resolution
            miniMapScript.mapSize = 400; 
            targetImageScale = Vector3.one;
            // Don't necessarily need to refresh immediately to save data, 
            // but resizing looks better if we do.
            
            // 2. Disable Scrolling
            // if(mapScrollRect) 
            // {
            //     mapScrollRect.StopMovement(); // Stop any inertia sliding
            //     mapScrollRect.enabled = false;
            // }

            // 3. RESET POSITION (Crucial)
            // Snap the image back to the center (0,0) of the container
            // mapRawImage.rectTransform.anchoredPosition = Vector2.zero;
            // mapRawImage.rectTransform.localScale = Vector3.one; // Reset scale
        }
    }
    public void CloseMap()
    {
        ToggleMapSize();
        ExpandButton.SetActive(true);
        MapPanel.SetActive(false);
    }

    // Zoom Buttons (Still useful!)
    public void ZoomIn()
    {
        if (miniMapScript.zoomLevel < 20) {
            miniMapScript.zoomLevel++;
            miniMapScript.ForceRefresh();
        }
    }

    public void ZoomOut()
    {
        if (miniMapScript.zoomLevel > 10) {
            miniMapScript.zoomLevel--;
            miniMapScript.ForceRefresh();
        }
    }
}