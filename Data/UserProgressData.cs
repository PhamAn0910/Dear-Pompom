// UserProgressData.cs
using System.Collections.Generic; // Required for List

[System.Serializable] // This makes the class savable
public class UserProgressData
{
    // A list to hold all recorded activity logs 
    public List<ActivityLogEntry> allActivityLogs;

    // Constructor to initialize the list when a new UserProgressData object is created 
    public UserProgressData()
    {
        allActivityLogs = new List<ActivityLogEntry>();
    }

    // Method to easily add a new activity log to the list 
    public void AddLog(ActivityLogEntry newLog)
    {
        allActivityLogs.Add(newLog);
    }
}
