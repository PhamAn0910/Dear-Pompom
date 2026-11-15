using System;

/// <summary>
/// 1.0 log shape. Kept ONLY as a read-only view for the 1.0 chart scripts; see
/// UserProgressManager.currentUserProgress. New code uses DearPompom.Core.ActivityRecord.
/// </summary>
[Serializable]
public class ActivityLogEntry
{
    public string activityType;
    public string timestamp;
    public float durationMinutes;
    public int score;
    public string notes;
    public string difficulty;
    /// <summary>(int)DearPompom.Core.Mood; -1 when not a journal record.</summary>
    public int mood = -1;

    public ActivityLogEntry(string type, float duration, int score = 0, string notes = "", string difficulty = "")
    {
        this.activityType = type;
        this.timestamp = DateTime.Now.ToString("o");
        this.durationMinutes = duration;
        this.score = score;
        this.notes = notes;
        this.difficulty = difficulty;
    }
}
