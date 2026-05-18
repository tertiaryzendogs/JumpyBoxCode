using System.Collections;
using System.Threading;
using UnityEngine;
public class FrameRateManager : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    int maxFrameRate = 9999;
    public float targetFrameRate = 60f;
    float currentFrameTime;
    private void Awake()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync
        Application.targetFrameRate = maxFrameRate; // Set to a very high value to allow uncapped frame rate
        currentFrameTime = Time.realtimeSinceStartup;
        StartCoroutine(WaitForNextFrame());
    }

    private IEnumerator WaitForNextFrame()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame(); // Wait until the end of the current frame
            currentFrameTime += 1.0f / targetFrameRate; // Calculate the time for the next frame
            var t = Time.realtimeSinceStartup;
            var sleepTime = currentFrameTime - t - 0.01f; // Calculate how long to sleep
            if (sleepTime > 0)
            {
                Thread.Sleep((int)(sleepTime * 1000)); // Sleep for the calculated time
                while (t < currentFrameTime)
                {
                    t = Time.realtimeSinceStartup; // Wait until the target frame time is reached
                }
            }
        }
    }
}