// ActivityLogEntry.cs
using System; // Required for DateTime

[System.Serializable] // This makes the class savable by Unity's JSON utility
public class ActivityLogEntry
{
    public string activityType;    // e.g., "Pomodoro", "Meditation", "MemoryCard" 
    public string timestamp;       // When the activity occurred (e.g., DateTime.Now.ToString("o") for ISO 8601) 
    public float durationMinutes;  // Duration of the activity in minutes 
    public int score;             // Score for games, 0 if not applicable 
    public string notes;           // For journal entries, or other context 
    public string difficulty;      // For games, e.g., "Easy", "Medium" 

    // Constructor for easily creating new activity log entries 
    public ActivityLogEntry(string type, float duration, int score = 0, string notes = "", string difficulty = "")
    {
        this.activityType = type;
        this.timestamp = DateTime.Now.ToString("o"); // Saves the exact date and time in a standard format
        this.durationMinutes = duration;
        this.score = score;
        this.notes = notes;
        this.difficulty = difficulty;
    }
}