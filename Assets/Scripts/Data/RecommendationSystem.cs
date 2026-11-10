// RecommendationSystem.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ queries like .LastOrDefault()
using TMPro; // Required for TextMeshProUGUI (if you're using TMPro for UI)
using System; // Required for DateTime

public class RecommendationSystem : MonoBehaviour
{
    // Assign these in the Inspector
    public GameObject recommendationUIPanel; // The UI panel that will show the recommendation
    public TextMeshProUGUI recommendationText; // The TextMeshProUGUI component to display the message

    [Header("Recommendation Rules Settings")]
    public int pomodoroSessionsForRelaxation = 3; // After this many Pomodoro sessions, suggest relaxation
    public float activityInactivityThresholdDays = 2; // Days after which an activity is considered "not done in a while"

    // Reference to the UserProgressManager (Singleton)
    private UserProgressManager userProgressManager;
    
    // Store pending recommendation to show after scene change
    private string pendingRecommendation = "";
    private bool shouldShowRecommendationOnSceneLoad = false;
    
    // Track previous scene for welcome messages
    private string previousSceneName = "";

    void Start()
    {
        // Make this GameObject persist across scenes
        DontDestroyOnLoad(gameObject);
        
        // Subscribe to scene change events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Ensure the UI panel is initially hidden
        if (recommendationUIPanel != null)
        {
            recommendationUIPanel.SetActive(false);
        }

        // Get the singleton instance of UserProgressManager
        userProgressManager = UserProgressManager.Instance;
        if (userProgressManager == null)
        {
            Debug.LogError("UserProgressManager.Instance not found! Make sure it's initialized in the scene.");
        }

        // Initialize previous scene name
        previousSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // You might want to trigger a recommendation check at specific times,
        // e.g., after an activity completes, or when the user enters the main hub.
        // For demonstration, we'll call it after a short delay or based on a game event.
        // For now, let's call it after a delay to simulate game startup check.
        // Invoke("CheckAndDisplayRecommendation", 2f); // Call after 2 seconds - COMMENTED OUT
    }

    void OnDestroy()
    {
        // Unsubscribe from scene events to prevent memory leaks
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Check for specific scene transitions for welcome messages
        if (previousSceneName == "StartMenu" && scene.name == "World")
        {
            // Show welcome message when entering World from StartMenu
            Invoke("ShowWelcomeMessage", 1f);
        }
        else if (shouldShowRecommendationOnSceneLoad && !string.IsNullOrEmpty(pendingRecommendation))
        {
            // Show pending recommendation after scene loads (with a small delay to ensure UI is ready)
            Invoke("ShowPendingRecommendation", 1f); // Small delay to ensure scene UI is ready
        }
        
        // Update previous scene name for next transition
        previousSceneName = scene.name;
    }

    private void ShowPendingRecommendation()
    {
        if (!string.IsNullOrEmpty(pendingRecommendation))
        {
            Debug.Log($"Showing delayed recommendation: {pendingRecommendation}");
            DisplayRecommendationUI(pendingRecommendation);
            
            // Clear the pending recommendation
            pendingRecommendation = "";
            shouldShowRecommendationOnSceneLoad = false;
        }
    }

    private void ShowWelcomeMessage()
    {
        Debug.Log("Showing welcome message for entering World scene");
        DisplayRecommendationUI("Welcome to the village of Pompom!");
    }

    /// <summary>
    /// This method can be called whenever you want to check for and display a new recommendation.
    /// E.g., at scene load, after an activity is completed, etc.
    /// </summary>
    public void CheckAndDisplayRecommendation()
    {
        CheckAndDisplayRecommendation(false); // Default to immediate display
    }

    /// <summary>
    /// Check for recommendations with option to delay display until scene change
    /// </summary>
    /// <param name="delayUntilSceneChange">If true, recommendation will be shown after next scene load</param>
    public void CheckAndDisplayRecommendation(bool delayUntilSceneChange = false)
    {
        if (userProgressManager == null)
        {
            Debug.LogError("RecommendationSystem: UserProgressManager is not available.");
            return;
        }

        string recommendation = GetRecommendation();

        if (!string.IsNullOrEmpty(recommendation))
        {
            Debug.Log($"Recommendation: {recommendation}");
            
            if (delayUntilSceneChange)
            {
                // Store recommendation to show after scene change
                pendingRecommendation = recommendation;
                shouldShowRecommendationOnSceneLoad = true;
                Debug.Log("Recommendation will be shown after next scene load.");
            }
            else
            {
                // Show immediately
                DisplayRecommendationUI(recommendation);
            }
        }
        else
        {
            Debug.Log("No specific recommendation at this time.");
            HideRecommendationUI();
        }
    }

    private string GetRecommendation()
    {
        // Get all user logs
        List<ActivityLogEntry> allLogs = userProgressManager.currentUserProgress.allActivityLogs;

        // Rule 2: After a Brain Game (Memory/Math), suggest physical activity or reflection - CHECK THIS FIRST!
        ActivityLogEntry lastActivity = allLogs.LastOrDefault();
        if (lastActivity != null)
        {
            if (lastActivity.activityType == "MemoryCard" || lastActivity.activityType == "Math Game")
            {
                // Check if it was done recently (e.g., within the last hour)
                if ((DateTime.Now - DateTime.Parse(lastActivity.timestamp)).TotalHours < 1)
                {
                    return "Great brain workout! A virtual walk in the Forest might clear your mind, or perhaps your Journal is waiting for reflection."; // 
                }
            }
        }

        // Rule 1: After X Pomodoro sessions, suggest relaxation - CHECK THIS SECOND
        int pomodoroCountToday = allLogs.Count(log =>
            log.activityType == "Pomodoro" &&
            DateTime.Parse(log.timestamp).Date == DateTime.Now.Date);

        if (pomodoroCountToday >= pomodoroSessionsForRelaxation)
        {
            return $"What a productive day! You've finished {pomodoroCountToday} Pomodoro sessions. This is the time to relax with music in the Theatre or a calming meditation session in the Forest!"; // 
        }

        // Rule 3: If a certain activity hasn't been done in a while, suggest revisiting it 
        // This requires tracking last visit for various activities. You'd need a list of all possible activities.
        // For simplicity, let's check a few key ones.
        string[] keyActivities = { "Meditation", "VirtualWalk", "JournalEntry", "Orchestra" }; // Adjust as per your game's activities

        foreach (string activityType in keyActivities)
        {
            ActivityLogEntry lastEntryOfType = allLogs
                .Where(log => log.activityType == activityType)
                .OrderByDescending(log => DateTime.Parse(log.timestamp))
                .FirstOrDefault();

            if (lastEntryOfType == null)
            {
                // If never done, suggest it
                return $"You haven't tried {activityType} yet! It could be a great way to explore.";
            }
            else
            {
                TimeSpan timeSinceLast = DateTime.Now - DateTime.Parse(lastEntryOfType.timestamp);
                if (timeSinceLast.TotalDays > activityInactivityThresholdDays)
                {
                    switch (activityType)
                    {
                        case "Meditation":
                            return $"It's been a while since you mediated. How about a calming meditation session in the Forest?";
                        case "VirtualWalk":
                            return $"You haven't gone for a virtual walk recently. The Forest is beautiful today!";
                        case "JournalEntry":
                            return $"It's been a while since you reflected. Your Journal is waiting for new thoughts.";
                        case "Orchestra":
                            return $"It's been a while since you visited the Theatre. Perhaps some music would boost your creativity today?"; // 
                        default:
                            return $"It's been a while since you last engaged in {activityType}. Perhaps it's time to revisit it?";
                    }
                }
            }
        }

        // Rule 4: Based on Journal Mood: If mood is low, suggest uplifting activities 
        // This requires your JournalEntry to capture mood as an int (e.g., 1-5, where 1 is low).
        // Assuming mood is stored in the 'score' field for JournalEntry, as per paper's suggestion for 'Outcome/Score'.
        ActivityLogEntry lastJournalEntry = allLogs
            .Where(log => log.activityType == "JournalEntry" || log.activityType == "JournalUpdate")
            .OrderByDescending(log => DateTime.Parse(log.timestamp))
            .FirstOrDefault();

        if (lastJournalEntry != null)
        {
            // Assuming score 0-2 for low mood (adjust based on your mood scale)
            if ((DateTime.Now - DateTime.Parse(lastJournalEntry.timestamp)).TotalHours < 24 && lastJournalEntry.score <= 2)
            {
                return "Feeling a bit down? The Theatre's music might lift your spirits!"; // 
            }
        }

        // No specific rule-based recommendation found
        return "";
    }

    private void DisplayRecommendationUI(string message)
    {
        if (recommendationUIPanel != null && recommendationText != null)
        {
            recommendationText.text = message;
            recommendationUIPanel.SetActive(true);
            // You might want to add animation or a sound effect here.
            // Also, consider adding a button to close the panel after user reads it.
        }
    }

    public void HideRecommendationUI()
    {
        if (recommendationUIPanel != null)
        {
            recommendationUIPanel.SetActive(false);
        }
    }

    // Optional: Add a public method to hide the UI via a UI button
    public void OnCloseRecommendationClicked()
    {
        HideRecommendationUI();
    }
}