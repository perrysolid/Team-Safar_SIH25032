using UnityEngine;
using UnityEngine.UI;

public class CompassRotator : MonoBehaviour
{
    [Header("UI Component")]
    public RectTransform dialRect; // Drag your Compass Dial Image here

    [Header("Settings")]
    public float smoothTime = 0.2f; // Higher = Slower/Smoother, Lower = Snappier

    private float currentVelocity; 
    private float currentHeading;

    void Start()
    {
        // Start the Compass Sensor
        Input.compass.enabled = true;
        
        // Start Location (Required if you want "True North" vs "Magnetic North")
        Input.location.Start();
    }

    void FixedUpdate()
    {
        // 1. Get the target heading (Real-world direction)
        // Use trueHeading if GPS is on, otherwise magneticHeading
        float targetHeading = Input.compass.trueHeading;

        // 2. Smoothly interpolate the rotation (Fixes the 359 -> 1 jitter)
        currentHeading = Mathf.SmoothDampAngle(currentHeading, targetHeading, ref currentVelocity, smoothTime);

        // 3. Apply Rotation
        // In Unity UI, positive Z rotation is Counter-Clockwise.
        // If we face East (90 deg), we need to rotate the dial 90 deg CCW 
        // to bring the "E" (which is usually on the right) to the top.
        dialRect.localEulerAngles = new Vector3(0, 0, currentHeading);
    }
}