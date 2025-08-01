using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphRenderer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform graphContainer;
    public GameObject barPrefab;
    public GameObject labelPrefab;

    [Header("Graph Settings")]
    public float barWidth = 80f;
    public float barSpacing = 40f;
    public float graphHeightPadding = 50f;
    public float graphBottomPadding = 50f;
    public int numberOfBarsToShow = 7;
    public Color barColor = Color.cyan;
    public Color labelColor = Color.black;
    public float labelFontSize = 24f;
    public int yAxisTickCount = 3;
    public Color axisColor = Color.black;
    public float axisThickness = 3f;


    private UserProgressManager userProgressManager;

    void Start()
    {
        userProgressManager = UserProgressManager.Instance;
        if (userProgressManager == null)
        {
            Debug.LogError("UserProgressManager not found! Make sure it's in the scene and persistent.");
            return;
        }

        userProgressManager.LoadProgress(); 

        // Choose which graph to draw by uncommenting one:
        // DrawActivityDurationBarChart();
        // DrawPomodoroLineGraph(7); 
        DrawGameScoresBarChart(); // New: Call to draw game scores
    }

    public void DrawActivityDurationBarChart()
    {
        ClearGraph();

        Dictionary<string, float> activityDurations = new Dictionary<string, float>();
        if (userProgressManager.currentUserProgress != null && userProgressManager.currentUserProgress.allActivityLogs != null)
        {
            foreach (var log in userProgressManager.currentUserProgress.allActivityLogs)
            {
                if (activityDurations.ContainsKey(log.activityType))
                {
                    activityDurations[log.activityType] += log.durationMinutes;
                }
                else
                {
                    activityDurations.Add(log.activityType, log.durationMinutes);
                }
            }
        }

        var sortedActivities = activityDurations.OrderByDescending(pair => pair.Value).Take(numberOfBarsToShow).ToList();

        if (!sortedActivities.Any())
        {
            GameObject noDataLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI noDataLabelTMP = noDataLabelGO.GetComponent<TextMeshProUGUI>();
            noDataLabelTMP.text = "No activity data available.";
            noDataLabelTMP.color = labelColor;
            noDataLabelTMP.fontSize = labelFontSize * 1.2f;
            noDataLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform noDataLabelRect = noDataLabelGO.GetComponent<RectTransform>();
            noDataLabelRect.sizeDelta = graphContainer.rect.size;
            noDataLabelRect.anchoredPosition = Vector2.zero;
            return;
        }

        float trueMaxDuration = sortedActivities.Max(pair => pair.Value);
        float maxDurationForScale = trueMaxDuration;
        if (maxDurationForScale == 0) maxDurationForScale = 1;
        else maxDurationForScale *= 1.1f;

        float availableGraphHeight = graphContainer.rect.height - graphHeightPadding - graphBottomPadding; 
        float totalBarAndSpacingWidth = (barWidth * sortedActivities.Count) + (barSpacing * (sortedActivities.Count - 1));
        float startX = (graphContainer.rect.width - totalBarAndSpacingWidth) / 2f + (barWidth / 2f);

        // --- Draw X and Y Axes ---
        DrawAxisLines(availableGraphHeight, graphBottomPadding);

        // Draw Bars and Labels
        for (int i = 0; i < sortedActivities.Count; i++)
        {
            var entry = sortedActivities[i];
            float barHeight = (entry.Value / maxDurationForScale) * availableGraphHeight;
            if (barHeight < 1f && entry.Value > 0) barHeight = 1f;

            GameObject barGO = Instantiate(barPrefab, graphContainer);
            RectTransform barRect = barGO.GetComponent<RectTransform>();
            Image barImage = barGO.GetComponent<Image>();
            barImage.color = barColor;
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding);

            GameObject nameLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI nameLabelTMP = nameLabelGO.GetComponent<TextMeshProUGUI>();
            nameLabelTMP.text = entry.Key;
            nameLabelTMP.color = labelColor;
            nameLabelTMP.fontSize = labelFontSize;
            nameLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform nameLabelRect = nameLabelGO.GetComponent<RectTransform>();
            nameLabelRect.sizeDelta = new Vector2(barWidth * 1.5f, graphBottomPadding - 10f);
            nameLabelRect.pivot = new Vector2(0.5f, 1f);
            nameLabelRect.anchorMin = new Vector2(0f, 0f);
            nameLabelRect.anchorMax = new Vector2(0f, 0f);
            nameLabelRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding / 2f);

            GameObject valueLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI valueLabelTMP = valueLabelGO.GetComponent<TextMeshProUGUI>();
            valueLabelTMP.text = entry.Value.ToString("F1") + " min";
            valueLabelTMP.color = labelColor;
            valueLabelTMP.fontSize = labelFontSize;
            valueLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform valueLabelRect = valueLabelGO.GetComponent<RectTransform>();
            valueLabelRect.sizeDelta = new Vector2(barWidth * 1.5f, 30);
            valueLabelRect.pivot = new Vector2(0.5f, 0f);
            valueLabelRect.anchorMin = new Vector2(0f, 0f);
            valueLabelRect.anchorMax = new Vector2(0f, 0f);
            valueLabelRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding + barHeight + 10f);
        }

        // --- Draw Y-axis Labels (Ticks) ---
        // Add 0 min label
        GameObject zeroLabelGO = Instantiate(labelPrefab, graphContainer);
        TextMeshProUGUI zeroLabelTMP = zeroLabelGO.GetComponent<TextMeshProUGUI>();
        zeroLabelTMP.text = "0 min";
        zeroLabelTMP.color = labelColor;
        zeroLabelTMP.fontSize = labelFontSize * 0.8f;
        zeroLabelTMP.alignment = TextAlignmentOptions.Right;
        RectTransform zeroLabelRect = zeroLabelGO.GetComponent<RectTransform>();
        zeroLabelRect.sizeDelta = new Vector2(100, 30);
        zeroLabelRect.pivot = new Vector2(1f, 0.5f);
        zeroLabelRect.anchorMin = new Vector2(0f, 0f);
        zeroLabelRect.anchorMax = new Vector2(0f, 0f);
        zeroLabelRect.anchoredPosition = new Vector2(-10f, graphBottomPadding);

        for (int i = 1; i <= yAxisTickCount; i++)
        {
            float value = trueMaxDuration * ((float)i / yAxisTickCount);
            float yPos = (value / maxDurationForScale) * availableGraphHeight + graphBottomPadding;

            GameObject tickLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI tickLabelTMP = tickLabelGO.GetComponent<TextMeshProUGUI>();
            tickLabelTMP.text = value.ToString("F0") + " min";
            tickLabelTMP.color = labelColor;
            tickLabelTMP.fontSize = labelFontSize * 0.8f;
            tickLabelTMP.alignment = TextAlignmentOptions.Right;
            RectTransform tickLabelRect = tickLabelGO.GetComponent<RectTransform>();
            tickLabelRect.sizeDelta = new Vector2(100, 30);
            tickLabelRect.pivot = new Vector2(1f, 0.5f);
            tickLabelRect.anchorMin = new Vector2(0f, 0f);
            tickLabelRect.anchorMax = new Vector2(0f, 0f);
            tickLabelRect.anchoredPosition = new Vector2(-10f, yPos);
        }
    }

    public void DrawPomodoroLineGraph(int daysToLookBack)
    {
        ClearGraph();

        Dictionary<DateTime, float> dailyPomodoroDurations = new Dictionary<DateTime, float>();
        DateTime cutoffDate = DateTime.Now.Date.AddDays(-(daysToLookBack - 1));

        if (userProgressManager.currentUserProgress != null && userProgressManager.currentUserProgress.allActivityLogs != null)
        {
            foreach (var log in userProgressManager.currentUserProgress.allActivityLogs)
            {
                if (log.activityType == "Pomodoro")
                {
                    DateTime logDate = DateTime.Parse(log.timestamp).Date;
                    if (logDate >= cutoffDate)
                    {
                        if (dailyPomodoroDurations.ContainsKey(logDate))
                        {
                            dailyPomodoroDurations[logDate] += log.durationMinutes;
                        }
                        else
                        {
                            dailyPomodoroDurations.Add(logDate, log.durationMinutes);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < daysToLookBack; i++)
        {
            DateTime date = DateTime.Now.Date.AddDays(-i);
            if (!dailyPomodoroDurations.ContainsKey(date))
            {
                dailyPomodoroDurations.Add(date, 0f);
            }
        }

        var sortedDailyDurations = dailyPomodoroDurations.OrderBy(pair => pair.Key).ToList();

        if (!sortedDailyDurations.Any())
        {
            GameObject noDataLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI noDataLabelTMP = noDataLabelGO.GetComponent<TextMeshProUGUI>();
            noDataLabelTMP.text = "No Pomodoro data available for the selected period.";
            noDataLabelTMP.color = labelColor;
            noDataLabelTMP.fontSize = labelFontSize * 1.2f;
            noDataLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform noDataLabelRect = noDataLabelGO.GetComponent<RectTransform>();
            noDataLabelRect.sizeDelta = graphContainer.rect.size;
            noDataLabelRect.anchoredPosition = Vector2.zero;
            return;
        }

        float trueMaxDuration = sortedDailyDurations.Max(pair => pair.Value);
        float maxDurationForScale = trueMaxDuration;
        if (maxDurationForScale == 0) maxDurationForScale = 1;
        else maxDurationForScale *= 1.1f;

        float availableGraphWidth = graphContainer.rect.width - (barWidth + barSpacing); 
        float availableGraphHeight = graphContainer.rect.height - graphHeightPadding - graphBottomPadding;

        // --- Draw X and Y Axes ---
        DrawAxisLines(availableGraphHeight, graphBottomPadding);

        float xStep = availableGraphWidth / (daysToLookBack > 1 ? daysToLookBack - 1 : 1);
        float startX = barWidth / 2f; 

        Vector2 previousPoint = Vector2.zero;
        List<Vector2> graphPoints = new List<Vector2>();

        for (int i = 0; i < sortedDailyDurations.Count; i++)
        {
            var entry = sortedDailyDurations[i];
            float xPos = startX + i * xStep;
            float yPos = (entry.Value / maxDurationForScale) * availableGraphHeight + graphBottomPadding;
            graphPoints.Add(new Vector2(xPos, yPos));

            GameObject pointGO = Instantiate(barPrefab, graphContainer);
            RectTransform pointRect = pointGO.GetComponent<RectTransform>();
            Image pointImage = pointGO.GetComponent<Image>();
            pointImage.color = barColor;
            pointRect.sizeDelta = new Vector2(15, 15);
            pointRect.pivot = new Vector2(0.5f, 0.5f);
            pointRect.anchorMin = new Vector2(0f, 0f);
            pointRect.anchorMax = new Vector2(0f, 0f);
            pointRect.anchoredPosition = new Vector2(xPos, yPos);

            GameObject dateLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI dateLabelTMP = dateLabelGO.GetComponent<TextMeshProUGUI>();
            dateLabelTMP.text = entry.Key.ToString("MM/dd");
            dateLabelTMP.color = labelColor;
            dateLabelTMP.fontSize = labelFontSize * 0.8f;
            dateLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform dateLabelRect = dateLabelGO.GetComponent<RectTransform>();
            dateLabelRect.sizeDelta = new Vector2(xStep, graphBottomPadding - 10f);
            dateLabelRect.pivot = new Vector2(0.5f, 1f);
            dateLabelRect.anchorMin = new Vector2(0f, 0f);
            dateLabelRect.anchorMax = new Vector2(0f, 0f);
            dateLabelRect.anchoredPosition = new Vector2(xPos, graphBottomPadding / 2f);

            GameObject valueLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI valueLabelTMP = valueLabelGO.GetComponent<TextMeshProUGUI>();
            valueLabelTMP.text = entry.Value.ToString("F0");
            valueLabelTMP.color = labelColor;
            valueLabelTMP.fontSize = labelFontSize * 0.8f;
            valueLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform valueLabelRect = valueLabelGO.GetComponent<RectTransform>();
            valueLabelRect.sizeDelta = new Vector2(xStep, 30);
            valueLabelRect.pivot = new Vector2(0.5f, 0f);
            valueLabelRect.anchorMin = new Vector2(0f, 0f);
            valueLabelRect.anchorMax = new Vector2(0f, 0f);
            valueLabelRect.anchoredPosition = new Vector2(xPos, yPos + 10f);

            if (i > 0)
            {
                DrawLineSegment(previousPoint, graphPoints[i]);
            }
            previousPoint = graphPoints[i];
        }

        // --- Draw Y-axis Labels (Ticks) ---
        // Add 0 min label
        GameObject zeroLabelGO = Instantiate(labelPrefab, graphContainer);
        TextMeshProUGUI zeroLabelTMP = zeroLabelGO.GetComponent<TextMeshProUGUI>();
        zeroLabelTMP.text = "0 min";
        zeroLabelTMP.color = labelColor;
        zeroLabelTMP.fontSize = labelFontSize * 0.8f;
        zeroLabelTMP.alignment = TextAlignmentOptions.Right;
        RectTransform zeroLabelRect = zeroLabelGO.GetComponent<RectTransform>();
        zeroLabelRect.sizeDelta = new Vector2(100, 30);
        zeroLabelRect.pivot = new Vector2(1f, 0.5f);
        zeroLabelRect.anchorMin = new Vector2(0f, 0f);
        zeroLabelRect.anchorMax = new Vector2(0f, 0f);
        zeroLabelRect.anchoredPosition = new Vector2(-10f, graphBottomPadding);

        for (int i = 1; i <= yAxisTickCount; i++)
        {
            float value = trueMaxDuration * ((float)i / yAxisTickCount);
            float yPos = (value / maxDurationForScale) * availableGraphHeight + graphBottomPadding;

            GameObject tickLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI tickLabelTMP = tickLabelGO.GetComponent<TextMeshProUGUI>();
            tickLabelTMP.text = value.ToString("F0") + " min";
            tickLabelTMP.color = labelColor;
            tickLabelTMP.fontSize = labelFontSize * 0.8f;
            tickLabelTMP.alignment = TextAlignmentOptions.Right;
            RectTransform tickLabelRect = tickLabelGO.GetComponent<RectTransform>();
            tickLabelRect.sizeDelta = new Vector2(100, 30);
            tickLabelRect.pivot = new Vector2(1f, 0.5f);
            tickLabelRect.anchorMin = new Vector2(0f, 0f);
            tickLabelRect.anchorMax = new Vector2(0f, 0f);
            tickLabelRect.anchoredPosition = new Vector2(-10f, yPos);
        }
    }

    /// <summary>
    /// Draws a bar chart displaying latest scores for Memory Card (levels 1-3) and Math Game.
    /// </summary>
    public void DrawGameScoresBarChart()
    {
        ClearGraph();

        Dictionary<string, ActivityLogEntry> latestGameScores = new Dictionary<string, ActivityLogEntry>();

        if (userProgressManager.currentUserProgress != null && userProgressManager.currentUserProgress.allActivityLogs != null)
        {
            // Sort all logs by timestamp (most recent first)
            var sortedLogs = userProgressManager.currentUserProgress.allActivityLogs
                .Where(log => (log.activityType == "MemoryCard" || log.activityType == "Math Game") && log.score > 0)
                .OrderByDescending(log => DateTime.Parse(log.timestamp))
                .ToList();

            foreach (var log in sortedLogs)
            {
                string key = "";
                if (log.activityType == "MemoryCard")
                {
                    key = $"Memory Card ({log.difficulty})"; // e.g., "Memory Card (Easy)"
                }
                else if (log.activityType == "Math Game")
                {
                    key = "Math Game";
                }

                // Only keep the first (most recent) entry for each game type
                if (!string.IsNullOrEmpty(key) && !latestGameScores.ContainsKey(key))
                {
                    latestGameScores[key] = log;
                }
            }
        }

        // Prepare latest scores for display
        List<KeyValuePair<string, float>> latestScores = new List<KeyValuePair<string, float>>();
        foreach (var entry in latestGameScores)
        {
            latestScores.Add(new KeyValuePair<string, float>(entry.Key, entry.Value.score));
        }

        // Sort by latest score for consistent display
        var sortedScores = latestScores.OrderByDescending(pair => pair.Value).ToList();

        if (!sortedScores.Any())
        {
            GameObject noDataLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI noDataLabelTMP = noDataLabelGO.GetComponent<TextMeshProUGUI>();
            noDataLabelTMP.text = "No game score data available.";
            noDataLabelTMP.color = labelColor;
            noDataLabelTMP.fontSize = labelFontSize * 1.2f;
            noDataLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform noDataLabelRect = noDataLabelGO.GetComponent<RectTransform>();
            noDataLabelRect.sizeDelta = graphContainer.rect.size;
            noDataLabelRect.anchoredPosition = Vector2.zero;
            return;
        }

        float trueMaxScore = sortedScores.Max(pair => pair.Value);
        float maxScoreForScale = trueMaxScore;
        if (maxScoreForScale == 0) maxScoreForScale = 1;
        else maxScoreForScale *= 1.1f; // Add a small buffer for the top label

        float availableGraphHeight = graphContainer.rect.height - graphHeightPadding - graphBottomPadding;
        float totalBarAndSpacingWidth = (barWidth * sortedScores.Count) + (barSpacing * (sortedScores.Count - 1));
        float startX = (graphContainer.rect.width - totalBarAndSpacingWidth) / 2f + (barWidth / 2f);

        // --- Draw X and Y Axes ---
        DrawAxisLines(availableGraphHeight, graphBottomPadding);

        // Draw Bars and Labels
        for (int i = 0; i < sortedScores.Count; i++)
        {
            var entry = sortedScores[i];
            float barHeight = (entry.Value / maxScoreForScale) * availableGraphHeight;
            if (barHeight < 1f && entry.Value > 0) barHeight = 1f; // Ensure tiny bars are visible

            GameObject barGO = Instantiate(barPrefab, graphContainer);
            RectTransform barRect = barGO.GetComponent<RectTransform>();
            Image barImage = barGO.GetComponent<Image>();
            barImage.color = barColor; // Use a distinct color for game scores if desired, e.g., Color.green
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding);

            GameObject nameLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI nameLabelTMP = nameLabelGO.GetComponent<TextMeshProUGUI>();
            nameLabelTMP.text = entry.Key;
            nameLabelTMP.color = labelColor;
            nameLabelTMP.fontSize = labelFontSize;
            nameLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform nameLabelRect = nameLabelGO.GetComponent<RectTransform>();
            nameLabelRect.sizeDelta = new Vector2(barWidth * 1.5f, graphBottomPadding - 10f);
            nameLabelRect.pivot = new Vector2(0.5f, 1f);
            nameLabelRect.anchorMin = new Vector2(0f, 0f);
            nameLabelRect.anchorMax = new Vector2(0f, 0f);
            nameLabelRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding / 2f);

            GameObject valueLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI valueLabelTMP = valueLabelGO.GetComponent<TextMeshProUGUI>();
            valueLabelTMP.text = entry.Value.ToString("F0"); // Show latest scores as whole numbers
            valueLabelTMP.color = labelColor;
            valueLabelTMP.fontSize = labelFontSize;
            valueLabelTMP.alignment = TextAlignmentOptions.Center;
            RectTransform valueLabelRect = valueLabelGO.GetComponent<RectTransform>();
            valueLabelRect.sizeDelta = new Vector2(barWidth * 1.5f, 30);
            valueLabelRect.pivot = new Vector2(0.5f, 0f);
            valueLabelRect.anchorMin = new Vector2(0f, 0f);
            valueLabelRect.anchorMax = new Vector2(0f, 0f);
            valueLabelRect.anchoredPosition = new Vector2(startX + i * (barWidth + barSpacing), graphBottomPadding + barHeight + 10f);
        }

        // --- Draw Y-axis Labels (Ticks) ---
        // Add 0 score label
        GameObject zeroLabelGO = Instantiate(labelPrefab, graphContainer);
        TextMeshProUGUI zeroLabelTMP = zeroLabelGO.GetComponent<TextMeshProUGUI>();
        zeroLabelTMP.text = "0 Score";
        zeroLabelTMP.color = labelColor;
        zeroLabelTMP.fontSize = labelFontSize * 0.8f;
        zeroLabelTMP.alignment = TextAlignmentOptions.Right;
        RectTransform zeroLabelRect = zeroLabelGO.GetComponent<RectTransform>();
        zeroLabelRect.sizeDelta = new Vector2(100, 30);
        zeroLabelRect.pivot = new Vector2(1f, 0.5f);
        zeroLabelRect.anchorMin = new Vector2(0f, 0f);
        zeroLabelRect.anchorMax = new Vector2(0f, 0f);
        zeroLabelRect.anchoredPosition = new Vector2(-10f, graphBottomPadding);

        for (int i = 1; i <= yAxisTickCount; i++)
        {
            float value = trueMaxScore * ((float)i / yAxisTickCount);
            float yPos = (value / maxScoreForScale) * availableGraphHeight + graphBottomPadding;

            GameObject tickLabelGO = Instantiate(labelPrefab, graphContainer);
            TextMeshProUGUI tickLabelTMP = tickLabelGO.GetComponent<TextMeshProUGUI>();
            tickLabelTMP.text = value.ToString("F0") + " Score"; // Round to whole numbers for scores
            tickLabelTMP.color = labelColor;
            tickLabelTMP.fontSize = labelFontSize * 0.8f;
            tickLabelTMP.alignment = TextAlignmentOptions.Right;
            RectTransform tickLabelRect = tickLabelGO.GetComponent<RectTransform>();
            tickLabelRect.sizeDelta = new Vector2(100, 30);
            tickLabelRect.pivot = new Vector2(1f, 0.5f);
            tickLabelRect.anchorMin = new Vector2(0f, 0f);
            tickLabelRect.anchorMax = new Vector2(0f, 0f);
            tickLabelRect.anchoredPosition = new Vector2(-10f, yPos);
        }
    }


    // New helper method to draw the X and Y axis lines
    private void DrawAxisLines(float graphPlotHeight, float graphPlotBottomY)
    {
        // Draw Y-axis line (vertical)
        GameObject yAxisGO = Instantiate(barPrefab, graphContainer);
        RectTransform yAxisRect = yAxisGO.GetComponent<RectTransform>();
        Image yAxisImage = yAxisGO.GetComponent<Image>();

        yAxisImage.color = axisColor;
        yAxisRect.sizeDelta = new Vector2(axisThickness, graphPlotHeight + axisThickness); // Height of Y-axis
        yAxisRect.pivot = new Vector2(0.5f, 0f); // Bottom-center pivot
        yAxisRect.anchorMin = new Vector2(0f, 0f); // Anchor to bottom-left of parent
        yAxisRect.anchorMax = new Vector2(0f, 0f); // Anchor to bottom-left of parent
        // Position at the left edge of the plotting area
        yAxisRect.anchoredPosition = new Vector2(0f, graphPlotBottomY); 

        // Draw X-axis line (horizontal)
        GameObject xAxisGO = Instantiate(barPrefab, graphContainer);
        RectTransform xAxisRect = xAxisGO.GetComponent<RectTransform>();
        Image xAxisImage = xAxisGO.GetComponent<Image>();

        xAxisImage.color = axisColor;
        xAxisRect.sizeDelta = new Vector2(graphContainer.rect.width, axisThickness); // Width of X-axis
        xAxisRect.pivot = new Vector2(0f, 0.5f); // Left-center pivot
        xAxisRect.anchorMin = new Vector2(0f, 0f); // Anchor to bottom-left of parent
        xAxisRect.anchorMax = new Vector2(0f, 0f); // Anchor to bottom-left of parent
        // Position at the bottom edge of the plotting area
        xAxisRect.anchoredPosition = new Vector2(0f, graphPlotBottomY);
    }

    private void DrawLineSegment(Vector2 p1, Vector2 p2)
    {
        GameObject lineGO = Instantiate(barPrefab, graphContainer);
        RectTransform lineRect = lineGO.GetComponent<RectTransform>();
        Image lineImage = lineGO.GetComponent<Image>();

        lineImage.color = barColor;
        float thickness = 5f;

        Vector2 direction = p2 - p1;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(distance, thickness);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchorMin = new Vector2(0f, 0f);
        lineRect.anchorMax = new Vector2(0f, 0f);
        lineRect.anchoredPosition = p1;
        lineRect.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void ClearGraph()
    {
        foreach (Transform child in graphContainer)
        {
            Destroy(child.gameObject);
        }
    }
}