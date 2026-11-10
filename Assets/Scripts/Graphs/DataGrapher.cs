using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For LINQ operations like GroupBy, OrderBy
using TMPro; // Required for TextMeshProUGUI

public class DataGrapher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject with GraphHandler and GraphSettings here.")]
    public GraphHandler activityGraphHandler;
    public GraphHandler timeGraphHandler; // For a second graph (Time Spent)

    [Tooltip("Optional: A TextMeshProUGUI to display a title for the activity frequency graph.")]
    public TextMeshProUGUI activityFrequencyTitle;
    [Tooltip("Optional: A TextMeshProUGUI to display a title for the time spent graph.")]
    public TextMeshProUGUI timeSpentTitle;

    private UserProgressManager userProgressManager;

    void Start()
    {
        userProgressManager = UserProgressManager.Instance;
        if (userProgressManager == null)
        {
            Debug.LogError("UserProgressManager.Instance is null. Ensure UserProgressManager is in the scene.");
            enabled = false;
            return;
        }

        if (activityGraphHandler == null)
        {
            Debug.LogError("Activity Graph Handler is not assigned in DataGrapher.");
            enabled = false;
            return;
        }

        // If you want to display two separate graphs, ensure you have two GraphHandler instances
        // and two separate GameObjects with GraphHandler/GraphSettings attached.
        // For simplicity, we'll start with one, but the code is ready for two.
        if (timeGraphHandler == null)
        {
            Debug.LogWarning("Time Graph Handler is not assigned. Only activity frequency will be displayed.");
            // You might want to disable timeSpentTitle if it exists
            if (timeSpentTitle != null) timeSpentTitle.gameObject.SetActive(false);
        }

        // Initial graph generation
        GenerateGraphs();
    }

    // Call this method whenever you want to refresh the graphs
    public void GenerateGraphs()
    {
        if (userProgressManager == null || activityGraphHandler == null)
        {
            Debug.LogWarning("Cannot generate graphs: UserProgressManager or GraphHandler not ready.");
            return;
        }

        UserProgressData progressData = userProgressManager.currentUserProgress;
        List<ActivityLogEntry> allLogs = progressData.allActivityLogs;

        // --- 1. Process data for Activity Frequency (Bar Chart) ---
        // Group by activityType and count
        var activityCounts = allLogs
            .GroupBy(log => log.activityType)
            .Select(group => new { ActivityType = group.Key, Count = group.Count() })
            .OrderBy(item => item.ActivityType) // Order alphabetically for consistent display
            .ToList();

        // Clear existing points and prepare for new ones
        // Clear existing data points
        // You might need to add a method to GraphHandler to clear all instantiated points/lines
        // For now, we'll just add new points.
        // If the graph doesn't reset visually, we'll need to add a ClearAllPoints() method to GraphHandler.

        float xPos = 1f; // Start X position for bars
        float barSpacing = 2f; // Space between bars

        // Set appropriate corner values for the graph based on expected data range
        // Max count could be activityCounts.Max(ac => ac.Count)
        // Max X would be activityCounts.Count * barSpacing
        float maxCount = activityCounts.Any() ? activityCounts.Max(ac => ac.Count) : 10; // Default max Y
        activityGraphHandler.SetCornerValues(new Vector2(0, 0), new Vector2(activityCounts.Count * barSpacing + 1, maxCount * 1.2f)); // Add some padding

        foreach (var entry in activityCounts)
        {
            // Create a point for each activity type (representing the top of a bar)
            // X-axis will represent activity type, Y-axis will represent count
            activityGraphHandler.CreatePoint(new Vector2(xPos, entry.Count));
            xPos += barSpacing;
        }
        activityGraphHandler.UpdateGraph(); // Refresh the graph visuals

        // Update title
        if (activityFrequencyTitle != null)
        {
            activityFrequencyTitle.text = "Activity Frequency";
        }

        // --- 2. Process data for Time Spent on Activities (Bar Chart) ---
        if (timeGraphHandler != null)
        {
            var timeSpent = allLogs
                .GroupBy(log => log.activityType)
                .Select(group => new { ActivityType = group.Key, TotalDuration = group.Sum(log => log.durationMinutes) })
                .OrderBy(item => item.ActivityType)
                .ToList();

            timeGraphHandler.Values.Clear(); // Clear existing data points

            xPos = 1f; // Reset X position for second graph
            float maxDuration = timeSpent.Any() ? timeSpent.Max(ts => ts.TotalDuration) : 60; // Default max Y
            timeGraphHandler.SetCornerValues(new Vector2(0, 0), new Vector2(timeSpent.Count * barSpacing + 1, maxDuration * 1.2f));

            foreach (var entry in timeSpent)
            {
                timeGraphHandler.CreatePoint(new Vector2(xPos, entry.TotalDuration));
                xPos += barSpacing;
            }
            timeGraphHandler.UpdateGraph();

            // Update title
            if (timeSpentTitle != null)
            {
                timeSpentTitle.text = "Total Time Spent (Minutes)";
            }
        }

        // Debug logging for activity data
        Debug.Log($"Total activity logs found: {allLogs.Count}");
        Debug.Log($"Processed activity types: {activityCounts.Count}");
        if (activityCounts.Any())
        {
            foreach (var entry in activityCounts)
            {
                Debug.Log($"Activity: {entry.ActivityType}, Count: {entry.Count}");
            }
        }
        else
        {
            Debug.LogWarning("No activity data found to display!");
        }
    }

    // You might want to call GenerateGraphs() from a UI button or when the progress panel is opened.
    // Example:
    public void ShowGraphsPanel()
    {
        // Assuming your graph GameObjects are children of a panel you want to show/hide
        if (activityGraphHandler != null && activityGraphHandler.gameObject.transform.parent != null)
        {
            activityGraphHandler.gameObject.transform.parent.gameObject.SetActive(true);
        }
        if (timeGraphHandler != null && timeGraphHandler.gameObject.transform.parent != null)
        {
            timeGraphHandler.gameObject.transform.parent.gameObject.SetActive(true);
        }
        GenerateGraphs(); // Refresh data when showing
    }

    public void HideGraphsPanel()
    {
        if (activityGraphHandler != null && activityGraphHandler.gameObject.transform.parent != null)
        {
            activityGraphHandler.gameObject.transform.parent.gameObject.SetActive(false);
        }
        if (timeGraphHandler != null && timeGraphHandler.gameObject.transform.parent != null)
        {
            timeGraphHandler.gameObject.transform.parent.gameObject.SetActive(false);
        }
    }
}