 using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class TourStop
{
    public string cityName;
    public double startTimeSeconds;
    public float durationSeconds;
}

public class VRVideoLoader : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoFileName = "tour_demo.mp4";
    public string videoFileName2="Untitled3.mp4";
    
    // Configure stops in the Inspector!
    public TourStop[] tourPlan;

    private int currentIndex = 0;
    public int i=0;

    void Start()
    {
        if(i==1)
        {
        string videoPath = Path.Combine(Application.streamingAssetsPath, videoFileName);
        videoPlayer.url = videoPath;
        }
        if(i==2)
        {
            string videoPath = Path.Combine(Application.streamingAssetsPath, videoFileName2);
            videoPlayer.url = videoPath;
        }
        // Prepare first, then start tour
        videoPlayer.prepareCompleted += (vp) => StartCoroutine(RunTourSequence());
        videoPlayer.Prepare();
        
    }

    IEnumerator RunTourSequence()
    {
        
        while (currentIndex < tourPlan.Length)
        {
            TourStop stop = tourPlan[currentIndex];

            // 1. Jump to specific time
            videoPlayer.time = stop.startTimeSeconds;
            videoPlayer.Play();

            Debug.Log($"Arrived at {stop.cityName}");

            // 2. Wait for the duration
            yield return new WaitForSeconds(stop.durationSeconds);

            // 3. Move next
            currentIndex++;
        }
        


        Debug.Log("Tour Completed");
        videoPlayer.Stop();
    }
    public void AppQuit()
    {
        Application.Quit();
    }
}