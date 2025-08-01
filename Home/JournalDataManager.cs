using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System; // Added for DateTime in ActivityLogEntry

public class JournalDataManager : MonoBehaviour
{
    private List<JournalEntry> entries = new List<JournalEntry>();
    private string savePath;

    // Keep track of when the journal UI was opened
    private float journalSessionStartTimeUnscaled = -1f; // Initialize to -1 to indicate not started

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "journalData.json");
        LoadEntries();
    }

    // --- NEW: Log session duration when the application quits or is paused ---
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Application is losing focus, which is a good time to end the session
            EndJournalSession();
        }
    }

    void OnApplicationQuit()
    {
        // Also end the session when the application is closed
        EndJournalSession();
    }

    // Called when the journal UI is opened
    public void StartJournalSession()
    {
        if (journalSessionStartTimeUnscaled == -1f) // Only start if not already started
        {
            journalSessionStartTimeUnscaled = Time.unscaledTime;
            Debug.Log("Journal session started.");
        }
        else
        {
            Debug.LogWarning("Journal session already started. Ignoring redundant StartJournalSession call.");
        }
    }

    // Call this when the journal UI is closed (e.g., "Close" button clicked)
    public void EndJournalSession()
    {
        if (journalSessionStartTimeUnscaled != -1f) // Only log if a session was actually started
        {
            float durationMinutes = (Time.unscaledTime - journalSessionStartTimeUnscaled) / 60f;
            if (durationMinutes < 0) durationMinutes = 0; // Should not happen with unscaledTime, but good safeguard

            if (UserProgressManager.Instance != null)
            {
                ActivityLogEntry journalSessionLog = new ActivityLogEntry(
                    "JournalSession",    // activityType
                    durationMinutes,     // durationMinutes
                    0,                   // score (not applicable)
                    "Time spent in journal UI" // notes
                );

                UserProgressManager.Instance.currentUserProgress.AddLog(journalSessionLog);
                UserProgressManager.Instance.SaveProgress();
                Debug.Log($"Logged Journal Session: {durationMinutes:F1} minutes.");
            }
            else
            {
                Debug.LogError("UserProgressManager.Instance is null. Cannot log journal session.");
            }
            journalSessionStartTimeUnscaled = -1f; // Reset for next session
        }
        else
        {
            Debug.LogWarning("EndJournalSession called, but no session was active or started. Ignoring.");
        }
    }

    public void SaveEntry(string text, int mood)
    {
        if (string.IsNullOrEmpty(text.Trim())) return; // Don't save empty entries

        JournalEntry newEntry = new JournalEntry
        {
            date = System.DateTime.Now.ToString("MMM dd, yyyy HH:mm"),
            text = text,
            mood = mood,
            id = System.Guid.NewGuid().ToString()
        };

        entries.Add(newEntry);
        SaveToFile();
        Debug.Log($"Saved entry: {newEntry.date}");

        // --- NEW LOGGING CODE for individual entry ---
        if (UserProgressManager.Instance != null)
        {
            ActivityLogEntry journalEntryLog = new ActivityLogEntry(
                "JournalEntry",      // activityType
                0,                   // durationMinutes (not applicable for a single entry save)
                0,                   // score (not applicable)
                text                 // notes (the actual journal entry text)
            );

            UserProgressManager.Instance.currentUserProgress.AddLog(journalEntryLog);
            UserProgressManager.Instance.SaveProgress(); // Save immediately after an entry is added

            Debug.Log($"Logged Journal Entry: '{text.Substring(0, Mathf.Min(text.Length, 30))}...'");
        }
        else
        {
            Debug.LogError("UserProgressManager.Instance is null. Cannot log individual journal entry.");
        }
        // --- END NEW LOGGING CODE ---
    }

    public void DeleteEntry(string id)
    {
        entries.RemoveAll(e => e.id == id);
        SaveToFile();
    }

    public void UpdateEntry(string id, string newText, int newMood)
    {
        var entry = entries.Find(e => e.id == id);
        if (entry != null)
        {
            entry.text = newText;
            entry.mood = newMood;
            SaveToFile();

            // --- NEW LOGGING CODE for updating an entry ---
            if (UserProgressManager.Instance != null)
            {
                ActivityLogEntry journalUpdateLog = new ActivityLogEntry(
                    "JournalUpdate",     // activityType
                    0,                   // durationMinutes (not applicable for a single update)
                    0,                   // score (not applicable)
                    newText              // notes (the updated journal entry text)
                );

                UserProgressManager.Instance.currentUserProgress.AddLog(journalUpdateLog);
                UserProgressManager.Instance.SaveProgress();

                Debug.Log($"Logged Journal Update: '{newText.Substring(0, Mathf.Min(newText.Length, 30))}...'");
            }
            else
            {
                Debug.LogError("UserProgressManager.Instance is null. Cannot log journal update.");
            }
            // --- END NEW LOGGING CODE ---
        }
    }

    private void SaveToFile()
    {
        try
        {
            Wrapper wrapper = new Wrapper { Entries = entries };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved {entries.Count} entries to: {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save journal: {e.Message}");
        }
    }

    private void LoadEntries()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                var wrapper = JsonUtility.FromJson<Wrapper>(json);
                entries = wrapper?.Entries ?? new List<JournalEntry>();
                Debug.Log($"Loaded {entries.Count} entries");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load journal: {e.Message}");
            entries = new List<JournalEntry>();
        }
    }

    public List<JournalEntry> GetAllEntries() => new List<JournalEntry>(entries);

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