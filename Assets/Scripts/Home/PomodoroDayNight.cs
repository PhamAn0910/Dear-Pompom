using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;

public class PomodoroDayNight : MonoBehaviour
{
    [Serializable]
    public struct TimeOfDayBackgrounds
    {
        public GameObject[] backgrounds; // 0: Resting, 1: Learning, 2: Start Resting
    }

    public TimeOfDayBackgrounds[] dayCycles; // 0: Morning, 1: Noon, 2: Night
    public Button startButton;
    public Button setTimeButton;
    public TextMeshProUGUI timerText;
    public TMP_InputField minutesInput;
    public GameObject timerUI;
    public GameObject setupUI;

    private int currentTimeOfDay;
    private int currentState;
    private float timeRemaining;
    private bool timerIsRunning = false;
    private Coroutine restingTransitionCoroutine;

    // --- REFINED: Use unscaled time for accurate real-world duration ---
    private float sessionStartTimeUnscaled; // To track when the pomodoro session began (unaffected by Time.timeScale)
    private float initialPomodoroMinutes; // To store the initial duration set by user

    void Start()
    {
        SetupInitialState();
        startButton.onClick.AddListener(StartPomodoro);
        setTimeButton.onClick.AddListener(ShowTimeSetup);
        timerUI.SetActive(false);
        setupUI.SetActive(true);
    }

    void Update()
    {
        // Check real-world time every frame (or use InvokeRepeating for better performance)
        UpdateTimeOfDay();

        // --- REFINED: Use unscaledDeltaTime for timer updates ---
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.unscaledDeltaTime; // Use unscaledDeltaTime here!
                UpdateTimerDisplay();
            }
            else
            {
                timerIsRunning = false;
                OnPomodoroComplete();
            }
        }
    }

    void UpdateTimeOfDay()
    {
        int hour = DateTime.Now.Hour;
        int newTimeOfDay;

        if (hour >= 6 && hour < 12) newTimeOfDay = 0; // Morning
        else if (hour >= 12 && hour < 18) newTimeOfDay = 1; // Noon
        else newTimeOfDay = 2; // Night

        if (newTimeOfDay != currentTimeOfDay)
        {
            currentTimeOfDay = newTimeOfDay;
            UpdateBackground();
        }
    }
    
    void SetupInitialState()
    {
        currentTimeOfDay = GetCurrentTimeOfDay();
        currentState = 0; // Resting
        UpdateBackground();
    }

    int GetCurrentTimeOfDay()
    {
        int hour = DateTime.Now.Hour;
        if (hour >= 6 && hour < 12) return 0; // Morning
        if (hour >= 12 && hour < 18) return 1; // Noon
        return 2; // Night
    }

    void UpdateBackground()
    {
        // Disable all backgrounds first
        foreach (var cycle in dayCycles)
        {
            foreach (var bg in cycle.backgrounds)
            {
                if (bg != null) bg.SetActive(false);
            }
        }
        // Enable the correct one for the current state
        if (dayCycles[currentTimeOfDay].backgrounds.Length > currentState && dayCycles[currentTimeOfDay].backgrounds[currentState] != null)
        {
            dayCycles[currentTimeOfDay].backgrounds[currentState].SetActive(true);
        }
    }

    void ShowTimeSetup()
    {
        // --- NEW LOGGING CODE: Log interrupted session if timer was running ---
        if (timerIsRunning)
        {
            OnPomodoroInterrupted();
        }
        // --- END NEW LOGGING CODE ---

        timerIsRunning = false;
        if (restingTransitionCoroutine != null)
            StopCoroutine(restingTransitionCoroutine);

        setupUI.SetActive(true);
        timerUI.SetActive(false);
        SwitchToState(0); // Resting
    }

    void StartPomodoro()
    {
        if (int.TryParse(minutesInput.text, out int minutes))
        {
            timeRemaining = minutes * 60;
            initialPomodoroMinutes = minutes; // Store the initial duration
            timerIsRunning = true;
            SwitchToState(1); // Learning
            setupUI.SetActive(false);
            timerUI.SetActive(true);
            sessionStartTimeUnscaled = Time.unscaledTime; // Record start time using unscaled time
            // We moved the timer update logic into Update() to use Time.unscaledDeltaTime directly
            // No need for StartCoroutine(UpdateTimer()) if timer is updated in Update()
        }
    }

    void SwitchToState(int newState)
    {
        currentState = newState;
        UpdateBackground();
    }

    // --- REMOVED: No longer need UpdateTimer coroutine as logic is in Update() ---
    /*
    IEnumerator UpdateTimer()
    {
        while (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime; // This was a problem!
                UpdateTimerDisplay();
            }
            else
            {
                timerIsRunning = false;
                OnPomodoroComplete();
            }
            yield return null;
        }
    }
    */

    void OnPomodoroComplete()
    {
        SwitchToState(2); // Start Resting
        restingTransitionCoroutine = StartCoroutine(TransitionToResting());

        // --- NEW LOGGING CODE ---
        if (UserProgressManager.Instance != null)
        {
            float actualDurationMinutes = (Time.unscaledTime - sessionStartTimeUnscaled) / 60f; // Use unscaled time
            if (actualDurationMinutes < 0) actualDurationMinutes = 0; // Just in case, no negative duration

            ActivityLogEntry pomodoroLog = new ActivityLogEntry(
                "Pomodoro",          // activityType
                actualDurationMinutes, // durationMinutes (actual time spent)
                0,                   // score (not applicable)
                $"Pomodoro session of {initialPomodoroMinutes} minutes completed." // notes
            );

            UserProgressManager.Instance.currentUserProgress.AddLog(pomodoroLog);
            UserProgressManager.Instance.SaveProgress();

            Debug.Log($"Logged Pomodoro COMPLETED: {actualDurationMinutes:F1} minutes (Target: {initialPomodoroMinutes}).");
        }
        else
        {
            Debug.LogError("UserProgressManager.Instance is null. Cannot log Pomodoro activity.");
        }
        // --- END NEW LOGGING CODE ---
    }

    // --- NEW METHOD: To log interrupted Pomodoro sessions ---
    void OnPomodoroInterrupted()
    {
        if (UserProgressManager.Instance != null && sessionStartTimeUnscaled > 0) // Only log if a session actually started
        {
            float actualDurationMinutes = (Time.unscaledTime - sessionStartTimeUnscaled) / 60f;
            if (actualDurationMinutes < 0) actualDurationMinutes = 0;

            ActivityLogEntry pomodoroLog = new ActivityLogEntry(
                "PomodoroInterrupted", // Different activity type for interrupted sessions
                actualDurationMinutes,   // Actual duration
                0,
                $"Pomodoro session interrupted after {actualDurationMinutes:F1} minutes (Target: {initialPomodoroMinutes})."
            );

            UserProgressManager.Instance.currentUserProgress.AddLog(pomodoroLog);
            UserProgressManager.Instance.SaveProgress();

            Debug.Log($"Logged Pomodoro INTERRUPTED: {actualDurationMinutes:F1} minutes.");
        }
        sessionStartTimeUnscaled = 0; // Reset start time
        initialPomodoroMinutes = 0; // Reset initial duration
    }

    IEnumerator TransitionToResting()
    {
        yield return new WaitForSeconds(2f);
        SwitchToState(0); // Resting
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // --- Ensure any manual stop/reset method also calls OnPomodoroInterrupted ---
    void OnDisable()
    {
        // If the timer is running and the script becomes disabled (e.g., scene change), log it as interrupted
        if (timerIsRunning)
        {
            OnPomodoroInterrupted();
        }
    }
}