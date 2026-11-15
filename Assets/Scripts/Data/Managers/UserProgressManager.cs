using System.Collections.Generic;
using DearPompom;
using DearPompom.Core;
using UnityEngine;

/// <summary>
/// Owns the player's save while the game runs. The ONLY way to log an activity is
/// <see cref="RecordActivity"/> (PRD D4); it appends through the core ActivityLog, publishes
/// <see cref="ActivityRecorded"/> on Services.Events and writes the save.
///
/// Created by <see cref="Services"/> before the first scene. The copy that still sits in the
/// StartMenu scene from 1.0 removes itself in Awake.
/// </summary>
public class UserProgressManager : MonoBehaviour
{
    private static UserProgressManager _instance;

    public static UserProgressManager Instance
    {
        get
        {
            if (_instance == null) Services.EnsureBooted();
            return _instance;
        }
    }

    public SaveData Save { get; private set; }
    public ActivityLog Log { get; private set; }

    /// <summary>Fresh query object over the current log. Cheap to create; do not cache across frames.</summary>
    public ProgressStats Stats { get { return new ProgressStats(Save.records, Services.Clock); } }

    private UserProgressData _legacyView;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Scene copy from 1.0. Remove only this component: its GameObject may host other scripts.
            Destroy(this);
            return;
        }

        _instance = this;
        Services.EnsureBooted();

        string description;
        Save = Services.Store.LoadProgress(Services.Clock, out description);
        Log = new ActivityLog(Save, Services.Clock);
        Debug.Log("[DearPompom] save: " + description);

        // Write straight away so a migrated or brand-new save exists on disk in the 2.0 format.
        SaveProgress();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>Logs one finished (or interrupted) activity and saves.</summary>
    /// <param name="durationMinutes">Real minutes spent.</param>
    /// <param name="completed">False when a timer was stopped early.</param>
    /// <param name="targetMinutes">Minutes the player planned, for timers.</param>
    public ActivityRecord RecordActivity(
        ActivityType type,
        float durationMinutes,
        bool completed = true,
        float targetMinutes = 0f,
        int score = 0,
        string difficulty = "",
        Mood mood = Mood.None,
        ActivityKind kind = ActivityKind.None,
        string notes = "")
    {
        if (Log == null)
        {
            Debug.LogError("[DearPompom] RecordActivity called before the save was loaded.");
            return null;
        }

        ActivityRecord record = Log.Add(type, durationMinutes, completed, targetMinutes, score, difficulty, mood, kind, notes);
        _legacyView = null;
        SaveProgress();
        Services.Events.Publish(new ActivityRecorded(record));
        return record;
    }

    public void SaveProgress()
    {
        if (Save != null) Services.Store.SaveProgress(Save);
    }

    /// <summary>Wipes the log and settings and starts over (PRD D7). The caller confirms with the player first.</summary>
    public void ResetAllData()
    {
        Services.Store.DeleteAll();
        Save = new SaveData { createdAt = Timestamp.Format(Services.Clock.Now) };
        Save.EnsureValid();
        Log = new ActivityLog(Save, Services.Clock);
        _legacyView = null;
        SaveProgress();
    }

    // Saving on quit is not reliable in a browser tab, so every change already saves immediately.
    // These are a last safety net for desktop builds.
    private void OnApplicationQuit() { if (_instance == this) SaveProgress(); }

    private void OnApplicationPause(bool paused) { if (paused && _instance == this) SaveProgress(); }

    // ------------------------------------------------------------------ 1.0 compatibility

    /// <summary>
    /// Read-only view of the log in the 1.0 shape, for the 1.0 chart scripts (GraphRenderer,
    /// DataGrapher) until the Progress screen is rebuilt on ProgressStats. Changes to this
    /// object are NOT saved. New code must use <see cref="Stats"/> or <see cref="Save"/>.
    /// </summary>
    public UserProgressData currentUserProgress
    {
        get
        {
            if (_legacyView == null) _legacyView = BuildLegacyView();
            return _legacyView;
        }
    }

    private UserProgressData BuildLegacyView()
    {
        var view = new UserProgressData();
        if (Save == null) return view;
        List<ActivityRecord> records = Save.records;
        for (int i = 0; i < records.Count; i++)
        {
            ActivityRecord r = records[i];
            if (r == null || !r.HasValidTime) continue;
            var entry = new ActivityLogEntry(LegacyActivityNames.For(r), r.durationMinutes, r.score, r.notes, r.difficulty);
            entry.timestamp = r.timestamp;
            entry.mood = (int)r.mood;
            view.allActivityLogs.Add(entry);
        }
        return view;
    }
}
