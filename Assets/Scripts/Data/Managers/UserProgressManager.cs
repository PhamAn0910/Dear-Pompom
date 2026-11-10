// UserProgressManager.cs
using UnityEngine;
using System.IO; // Required for File operations (File.Exists, File.ReadAllText, File.WriteAllText)
using UnityEngine.SceneManagement;

public class UserProgressManager : MonoBehaviour
{
    private static UserProgressManager _instance;

    public static UserProgressManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UserProgressManager>();
            }

            if (_instance == null)
            {
                GameObject managerObject = new GameObject("UserProgressManager");
                _instance = managerObject.AddComponent<UserProgressManager>();
                Debug.Log("UserProgressManager instance was not found in the scene. A new one was created.");
            }

            return _instance;
        }
    }

    // This is the actual data container that holds all our logs
    public UserProgressData currentUserProgress;

    // The name of the file where data will be saved
    [SerializeField] // Allows this private field to be set in the Unity Inspector
    private string dataFileName = "userProgress.json"; // Default file name

    private string saveFilePath; // Full path to the save file
    private string _savePath; // Full path to the save file (used in Awake)

    private RecommendationSystem _recommendationSystem;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            _savePath = Path.Combine(Application.persistentDataPath, "userProgress.json");
            LoadProgress();
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    void Start()
    {
        Debug.Log("UserProgressManager Start() called"); // NEW DEBUG LINE
        
        // Find the RecommendationSystem in the scene.
        // Ensure the RecommendationSystemController GameObject is active in the scene.
        _recommendationSystem = FindFirstObjectByType<RecommendationSystem>();
        if (_recommendationSystem == null)
        {
            Debug.LogWarning("RecommendationSystem not found in scene. Recommendations will not be displayed.");
        }
        else
        {
            Debug.Log("RecommendationSystem found successfully!"); // NEW DEBUG LINE
        }
    }

    public void RecordActivity(string activityType, float duration, int score = 0, string notes = "", string difficulty = "")
    {
        Debug.Log($"RecordActivity called with: {activityType}"); // NEW DEBUG LINE
        
        ActivityLogEntry newLog = new ActivityLogEntry(activityType, duration, score, notes, difficulty);
        currentUserProgress.AddLog(newLog);
        Debug.Log($"Recorded activity: {activityType}, Duration: {duration}, Score: {score}, Notes: '{notes}'");

        SaveProgress(); // Save after every record for robustness

        // *** IMPORTANT: Trigger recommendation check after recording activity ***
        if (_recommendationSystem != null)
        {
            _recommendationSystem.CheckAndDisplayRecommendation(true); // TRUE = delay until scene change
            Debug.Log("Recommendation check triggered after recording activity (will show on next scene load).");
        }
        else
        {
            Debug.LogError("_recommendationSystem is NULL! Cannot trigger recommendation check."); // NEW DEBUG LINE
        }
    }

    private void InitializeProgress()
    {
        // Combine the persistent data path with the file name
        saveFilePath = Path.Combine(Application.persistentDataPath, dataFileName);
        Debug.Log("Save file path: " + saveFilePath);

        LoadProgress(); // Try to load existing progress when the game starts
    }

    // Call this method whenever you want to save the current progress
    public void SaveProgress()
    {
        // Ensure saveFilePath is set
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, dataFileName);
        }
        
        // Convert the currentUserProgress object into a JSON string
        string json = JsonUtility.ToJson(currentUserProgress, true); // 'true' for pretty printing (readable format)

        // Write the JSON string to the file
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Progress saved to: " + saveFilePath);
    }

    // Call this method to load existing progress from the file
    public void LoadProgress()
    {
        // Ensure saveFilePath is set
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, dataFileName);
        }
        
        // Check if the save file exists
        if (File.Exists(saveFilePath))
        {
            // Read all text from the file
            string json = File.ReadAllText(saveFilePath);

            // Convert the JSON string back into a UserProgressData object
            currentUserProgress = JsonUtility.FromJson<UserProgressData>(json);
            Debug.Log("Progress loaded from: " + saveFilePath);
        }
        else
        {
            // If the file doesn't exist, create a new UserProgressData object
            currentUserProgress = new UserProgressData();
            Debug.Log("No save file found. Creating new progress data.");
            SaveProgress(); // Save the new empty data immediately
        }
    }

    // This method is called when the application is quitting
    private void OnApplicationQuit()
    {
        SaveProgress(); // Ensure data is saved when the user closes the game
    }
}