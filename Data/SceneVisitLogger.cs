using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneVisitLogger : MonoBehaviour
{
    private float sceneEnterTime;
    private string currentSceneName;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Record the time and name of the scene that was just loaded
        sceneEnterTime = Time.unscaledTime;
        currentSceneName = scene.name;
        Debug.Log($"Entered scene '{currentSceneName}' at {Time.time}.");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // We only care about the scene we are leaving
        string unloadedSceneName = scene.name;

        // Define which scenes should be logged
        string activityType = "";
        string notes = "";

        if (unloadedSceneName == "Theatre") // Your Theatre scene name
        {
            activityType = "TheatreVisit";
            notes = "Listened to instruments in the Theatre.";
        }
        else if (unloadedSceneName == "ForestWalking") 
        {
            activityType = "ForestWalk";
            notes = "Walked through the Forest.";
        }

        // If the unloaded scene is one we want to track, log the duration
        if (!string.IsNullOrEmpty(activityType))
        {
            if (UserProgressManager.Instance != null)
            {
                float durationSeconds = Time.unscaledTime - sceneEnterTime;
                
                // Only log if the visit was longer than a few seconds to avoid accidental logs
                if (durationSeconds > 3f)
                {
                    ActivityLogEntry visitLog = new ActivityLogEntry(
                        activityType,
                        durationSeconds / 60f, // Convert to minutes
                        0, // No score for a scene visit
                        notes
                    );

                    UserProgressManager.Instance.currentUserProgress.AddLog(visitLog);
                    UserProgressManager.Instance.SaveProgress(); // Save immediately
                    Debug.Log($"Logged {activityType} for scene '{unloadedSceneName}'. Duration: {durationSeconds:F1} seconds.");
                }
            }
            else
            {
                Debug.LogError($"UserProgressManager.Instance is null. Cannot log visit to '{unloadedSceneName}'.");
            }
        }
    }
}
