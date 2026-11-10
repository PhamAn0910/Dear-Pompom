using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class JournalUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject notebookPanel;
    public TMP_InputField journalInput;
    public Button[] moodButtons;
    public Transform entriesContainer;
    public GameObject entryButtonPrefab;
    public Button saveButton;
    public Button closeButton;
    public TextMeshProUGUI titleText;

    [Header("Mood Icons")]
    // No need for separate moodIcons array - we'll get them from moodButtons

    private JournalDataManager dataManager;
    private int currentMood = 0;
    private string editingEntryId = null;

    [Header("Additional References")]
    public ScrollRect entriesScrollView; // Drag your EntriesScrollView here

    // private Color[] moodColors = new Color[]
    // {
    //     new Color(1f, 0.8f, 0f, 1f),    // Excited - Orange
    //     new Color(0f, 0.8f, 0.2f, 1f),  // Productive - Green
    //     new Color(1f, 0.9f, 0.2f, 1f),  // Happy - Yellow
    //     new Color(0.8f, 0.4f, 0.8f, 1f), // Emotional - Purple
    //     new Color(0.6f, 0.6f, 0.8f, 1f)  // Sad - Blue
    // };

    void Start()
    {
        dataManager = FindFirstObjectByType<JournalDataManager>();

        if (dataManager == null)
        {
            Debug.LogError("JournalDataManager not found! Make sure it exists in the scene.");
            return;
        }

        SetupMoodButtons();
        SetupUIButtons();
        notebookPanel.SetActive(false); // Start closed
    }

    private void SetupMoodButtons()
    {
        for (int i = 0; i < moodButtons.Length; i++)
        {
            int index = i;
            moodButtons[i].onClick.AddListener(() => SetMood(index));
        }

        SetMood(0); // Default to first mood
    }

    private void SetupUIButtons()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentEntry);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => ToggleNotebook(false));
    }

    public void ToggleNotebook(bool show)
    {
        notebookPanel.SetActive(show);
        if (show)
        {
            RefreshEntriesList();
            journalInput.text = "";
            editingEntryId = null;
            SetMood(0);
        }
    }

    public void SetMood(int moodIndex)
    {
        currentMood = moodIndex;
    }

    public void SaveCurrentEntry()
    {
        string text = journalInput.text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Cannot save empty entry");
            return;
        }

        if (editingEntryId != null)
        {
            dataManager.UpdateEntry(editingEntryId, text, currentMood);
            editingEntryId = null;
        }
        else
        {
            dataManager.SaveEntry(text, currentMood);
        }

        journalInput.text = "";
        SetMood(0);
        RefreshEntriesList();
    }

    // private void RefreshEntriesList()
    // {
    //     // Clear existing entries
    //     foreach (Transform child in entriesContainer)
    //     {
    //         Destroy(child.gameObject);
    //     }

    //     var allEntries = dataManager.GetAllEntries();
    //     allEntries.Reverse(); // Show newest first

    //     // Create entry buttons
    //     foreach (JournalEntry entry in allEntries)
    //     {
    //         CreateEntryButton(entry);
    //     }

    //     // Update title
    //     if (titleText != null)
    //         titleText.text = $"Past Entries ({allEntries.Count})";
    // }

    private void CreateEntryButton(JournalEntry entry)
    {
        GameObject entryButton = Instantiate(entryButtonPrefab, entriesContainer);

        // Set date text
        var dateText = entryButton.transform.Find("DateText").GetComponent<TextMeshProUGUI>();
        dateText.text = entry.date;

        // Set mood icon by copying from the corresponding mood button
        var moodIcon = entryButton.transform.Find("MoodIcon").GetComponent<Image>();
        if (entry.mood >= 0 && entry.mood < moodButtons.Length)
        {
            // Get the sprite from the mood button's Image component
            var buttonImage = moodButtons[entry.mood].GetComponent<Image>();
            if (buttonImage != null && buttonImage.sprite != null)
            {
                moodIcon.sprite = buttonImage.sprite;
            }
        }

        // Set up click listener
        entryButton.GetComponent<Button>().onClick.AddListener(() => LoadEntry(entry));
    }

    private void LoadEntry(JournalEntry entry)
    {
        journalInput.text = entry.text;
        currentMood = entry.mood;
        editingEntryId = entry.id;
        SetMood(entry.mood);

        Debug.Log($"Loaded entry for editing: {entry.date}");
    }

    public void DeleteCurrentEntry()
    {
        if (editingEntryId != null)
        {
            dataManager.DeleteEntry(editingEntryId);
            journalInput.text = "";
            editingEntryId = null;
            SetMood(0);
            RefreshEntriesList();
        }
    }
    
    // Add this method
    private void RefreshEntriesList()
    {
        // Clear existing entries
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }

        var allEntries = dataManager.GetAllEntries();
        allEntries.Reverse(); // Show newest first

        // Create entry buttons
        foreach (JournalEntry entry in allEntries)
        {
            CreateEntryButton(entry);
        }

        // Update title
        if (titleText != null)
            titleText.text = $"Past Entries ({allEntries.Count})";

        // Force layout rebuild and scroll to top
        StartCoroutine(ScrollToTopAfterFrame());
    }

    private System.Collections.IEnumerator ScrollToTopAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        
        if (entriesScrollView != null)
        {
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(entriesContainer.GetComponent<RectTransform>());
            
            // Scroll to top
            entriesScrollView.verticalNormalizedPosition = 1f;
        }
    }
}