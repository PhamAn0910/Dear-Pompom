using DearPompom.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Logs time spent in scenes that count as an activity just by being there
/// (Theatre, forest walk). Lives on the services object; the 1.0 copy in StartMenu removes itself.
/// </summary>
public class SceneVisitLogger : MonoBehaviour
{
    /// <summary>Visits shorter than this are treated as passing through.</summary>
    public const float MinimumSeconds = 3f;

    private static SceneVisitLogger _instance;

    private float _sceneEnterTime;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        _sceneEnterTime = Time.unscaledTime;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        if (_instance != this) return;
        _instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _sceneEnterTime = Time.unscaledTime;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ActivityType type;
        if (scene.name == "Theatre") type = ActivityType.Theatre;
        else if (scene.name == "ForestWalking") type = ActivityType.ForestWalk;
        else return;

        float seconds = Time.unscaledTime - _sceneEnterTime;
        if (seconds <= MinimumSeconds) return;

        UserProgressManager progress = UserProgressManager.Instance;
        if (progress != null) progress.RecordActivity(type, seconds / 60f);
    }
}
