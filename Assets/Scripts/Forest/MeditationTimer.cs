using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;

public class MeditationTimer : MonoBehaviour
{
    public GameObject[] backgrounds; // 0: Awake, 1: StartMeditating, 2: Meditation
    public Button startButton;
    public Button setTimeButton;
    public TextMeshProUGUI timerText;
    public TMP_InputField minutesInput;
    public GameObject timerUI;
    public GameObject setupUI;

    private int currentState;
    private float timeRemaining;
    private bool timerIsRunning = false;
    private Coroutine meditatingTransitionCoroutine;

    // --- REFINED: Use unscaled time ---
    private float sessionStartTimeUnscaled; // To track when the meditation session began
    private float initialMeditationMinutes; // To store the initial duration set by user
    

    void Start()
    {
        SetupInitialState();
        startButton.onClick.AddListener(StartMeditation);
        setTimeButton.onClick.AddListener(ShowTimeSetup);
        
        // Initial UI setup
        timerUI.SetActive(false);
        setupUI.SetActive(true);
        timerText.gameObject.SetActive(false);
        minutesInput.gameObject.SetActive(true);
    }

    void SetupInitialState()
    {
        currentState = 0; // Awake
        UpdateBackground();
    }

    void UpdateBackground()
    {
        // Disable all backgrounds first
        foreach (var bg in backgrounds)
        {
            bg.SetActive(false);
        }
        // Enable the correct one
        backgrounds[currentState].SetActive(true);
    }

    void ShowTimeSetup()
    {
        // --- NEW LOGGING CODE: Log interrupted session if timer was running ---
        if (timerIsRunning)
        {
            OnMeditationInterrupted();
        }
        // --- END NEW LOGGING CODE ---

        timerIsRunning = false;
        if (meditatingTransitionCoroutine != null)
            StopCoroutine(meditatingTransitionCoroutine);

        setupUI.SetActive(true);
        timerUI.SetActive(false);
        timerText.gameObject.SetActive(false);
        minutesInput.gameObject.SetActive(true);
        SwitchToState(0); // Awake
    }

    void StartMeditation()
    {
        if (int.TryParse(minutesInput.text, out int minutes))
        {
            timeRemaining = minutes * 60;
            initialMeditationMinutes = minutes; // Store initial duration
            timerIsRunning = true;
            
            // Switch to StartMeditating state first
            SwitchToState(1); // StartMeditating
            
            // Hide setup UI and show timer UI
            setupUI.SetActive(false);
            timerUI.SetActive(true);
            
            // Switch input field to timer text
            minutesInput.gameObject.SetActive(false);
            timerText.gameObject.SetActive(true);
            
            // Start the transition coroutine
            meditatingTransitionCoroutine = StartCoroutine(TransitionToMeditation());
            sessionStartTimeUnscaled = Time.unscaledTime; // Record start time using unscaled time
        }
    }

    IEnumerator TransitionToMeditation()
    {
        // Wait in StartMeditating state for 2 seconds
        yield return new WaitForSeconds(0.3f);
        
        // Then switch to Meditation state and start the timer
        SwitchToState(2); // Meditation
        StartCoroutine(UpdateTimer());
    }

    void SwitchToState(int newState)
    {
        currentState = newState;
        UpdateBackground();
    }

    IEnumerator UpdateTimer()
    {
        while (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.unscaledDeltaTime; // --- REFINED: Use unscaledDeltaTime ---
                UpdateTimerDisplay();
            }
            else
            {
                timerIsRunning = false;
                OnMeditationComplete();
            }
            yield return null;
        }
    }

    void OnMeditationComplete()
    {
        // --- NEW LOGGING CODE ---
        if (UserProgressManager.Instance != null)
        {
            float actualDurationMinutes = (Time.unscaledTime - sessionStartTimeUnscaled) / 60f; // --- REFINED: Use unscaled time ---
            if (actualDurationMinutes < 0) actualDurationMinutes = 0;

            ActivityLogEntry meditationLog = new ActivityLogEntry(
                "Meditation",          // activityType
                actualDurationMinutes, // durationMinutes (actual time spent)
                0,                   // score (not applicable)
                $"Meditation session of {initialMeditationMinutes} minutes completed." // notes
            );

            UserProgressManager.Instance.currentUserProgress.AddLog(meditationLog);
            UserProgressManager.Instance.SaveProgress();

            Debug.Log($"Logged Meditation COMPLETED: {actualDurationMinutes:F1} minutes.");
        }
        else
        {
            Debug.LogError("UserProgressManager.Instance is null. Cannot log Meditation activity.");
        }
        // --- END NEW LOGGING CODE ---

        // When timer completes, transition back to Awake state
        SwitchToState(0); // Awake
        ShowTimeSetup(); // This will reset the UI as well
    }

    // New method to handle meditation interrupted (e.g., user clicks set time button)
    void OnMeditationInterrupted()
    {
        if (UserProgressManager.Instance != null && sessionStartTimeUnscaled > 0) // Only log if a session actually started
        {
            float actualDurationMinutes = (Time.unscaledTime - sessionStartTimeUnscaled) / 60f; // --- REFINED: Use unscaled time ---
            if (actualDurationMinutes < 0) actualDurationMinutes = 0;

            ActivityLogEntry meditationLog = new ActivityLogEntry(
                "MeditationInterrupted", // Different activity type for interrupted sessions
                actualDurationMinutes,   // Actual duration
                0,
                $"Meditation session interrupted after {actualDurationMinutes:F1} minutes."
            );

            UserProgressManager.Instance.currentUserProgress.AddLog(meditationLog);
            UserProgressManager.Instance.SaveProgress();

            Debug.Log($"Logged Interrupted Meditation: {actualDurationMinutes:F1} minutes.");
        }
        sessionStartTimeUnscaled = 0; // Reset start time
        initialMeditationMinutes = 0; // Reset initial duration
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void OnDisable()
    {
        // If the timer is running and the script becomes disabled (e.g., scene change), log it as interrupted
        if (timerIsRunning)
        {
            OnMeditationInterrupted();
        }
    }
}