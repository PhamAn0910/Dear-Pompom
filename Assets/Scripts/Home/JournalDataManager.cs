using System.Collections.Generic;
using DearPompom;
using DearPompom.Core;
using UnityEngine;

/// <summary>
/// Stores journal entries (save/journal.json through SaveStore) and reports journal activity
/// to the activity log. The journal TEXT stays in the journal file only; the activity log gets
/// the mood and the time, never the words.
/// </summary>
public class JournalDataManager : MonoBehaviour
{
    private List<JournalEntry> entries = new List<JournalEntry>();

    // Time.unscaledTime when the journal UI was opened; -1 while closed.
    private float journalSessionStartTimeUnscaled = -1f;

    void Awake()
    {
        Services.EnsureBooted();
        LoadEntries();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) EndJournalSession();
    }

    void OnApplicationQuit()
    {
        EndJournalSession();
    }

    /// <summary>Called when the journal UI is opened.</summary>
    public void StartJournalSession()
    {
        if (journalSessionStartTimeUnscaled < 0f) journalSessionStartTimeUnscaled = Time.unscaledTime;
    }

    /// <summary>Called when the journal UI is closed. Logs the time spent with it open.</summary>
    public void EndJournalSession()
    {
        if (journalSessionStartTimeUnscaled < 0f) return;

        float minutes = Mathf.Max(0f, (Time.unscaledTime - journalSessionStartTimeUnscaled) / 60f);
        journalSessionStartTimeUnscaled = -1f;
        Record(minutes, Mood.None, ActivityKind.JournalSession);
    }

    /// <param name="mood">Index of the mood button (see DearPompom.Core.Mood).</param>
    public void SaveEntry(string text, int mood)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim())) return;

        entries.Add(new JournalEntry
        {
            date = System.DateTime.Now.ToString("MMM dd, yyyy HH:mm"),
            text = text,
            mood = mood,
            id = System.Guid.NewGuid().ToString()
        });
        SaveToFile();
        Record(0f, Moods.FromIndex(mood), ActivityKind.JournalEntry);
    }

    public void DeleteEntry(string id)
    {
        entries.RemoveAll(e => e.id == id);
        SaveToFile();
    }

    public void UpdateEntry(string id, string newText, int newMood)
    {
        JournalEntry entry = entries.Find(e => e.id == id);
        if (entry == null) return;

        entry.text = newText;
        entry.mood = newMood;
        SaveToFile();
        Record(0f, Moods.FromIndex(newMood), ActivityKind.JournalUpdate);
    }

    public List<JournalEntry> GetAllEntries() => new List<JournalEntry>(entries);

    private static void Record(float minutes, Mood mood, ActivityKind kind)
    {
        UserProgressManager progress = UserProgressManager.Instance;
        if (progress != null) progress.RecordActivity(ActivityType.Journal, minutes, mood: mood, kind: kind);
    }

    private void SaveToFile()
    {
        Services.Store.WriteText(SaveStore.JournalFile, JsonUtility.ToJson(new Wrapper { Entries = entries }, true));
    }

    private void LoadEntries()
    {
        entries = Parse(Services.Store.ReadText(SaveStore.JournalFile));
        if (entries != null) return;

        // First run of 2.0: bring the 1.0 journal into the save folder. The old file is left in place.
        string legacyPath = Services.Store.FindLegacyFile(SaveStore.LegacyJournalFile);
        if (legacyPath != null)
        {
            try { entries = Parse(System.IO.File.ReadAllText(legacyPath)); }
            catch (System.Exception e) { Debug.LogWarning("[DearPompom] could not read 1.0 journal: " + e.Message); }
            if (entries != null)
            {
                SaveToFile();
                Debug.Log("[DearPompom] imported " + entries.Count + " journal entries from 1.0");
                return;
            }
        }
        entries = new List<JournalEntry>();
    }

    /// <summary>Null when the text is missing or not a journal file.</summary>
    private static List<JournalEntry> Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
            return wrapper != null ? wrapper.Entries : null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[DearPompom] journal file could not be parsed: " + e.Message);
            return null;
        }
    }

    [System.Serializable]
    private class Wrapper { public List<JournalEntry> Entries; }
}

[System.Serializable]
public class JournalEntry
{
    public string id;
    public string date;
    public string text;
    public int mood;
}
