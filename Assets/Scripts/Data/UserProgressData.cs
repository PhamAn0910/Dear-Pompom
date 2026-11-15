using System.Collections.Generic;

/// <summary>
/// 1.0 container. Kept ONLY as a read-only view for the 1.0 chart scripts; see
/// UserProgressManager.currentUserProgress. The real save is DearPompom.Core.SaveData.
/// </summary>
[System.Serializable]
public class UserProgressData
{
    public List<ActivityLogEntry> allActivityLogs = new List<ActivityLogEntry>();
}
