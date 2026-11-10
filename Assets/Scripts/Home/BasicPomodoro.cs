using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PomodoroManager : MonoBehaviour
{
    public GameObject[] backgroundSprites; // 0: Resting, 1: Learning, 2: Start Resting
    public Button startButton;
    public Button setTimeButton;
    // public Text timerText;
    // public InputField minutesInput;
    public TextMeshProUGUI timerText;
    public TMP_InputField minutesInput;

    public GameObject timerUI;
    public GameObject setupUI;

    private int currentIndex = 0;
    private float timeRemaining;
    private bool timerIsRunning = false;
    private Coroutine restingTransitionCoroutine;

    void Start()
    {
        // Initial setup
        SetupRestingState();
        
        // Button listeners
        startButton.onClick.AddListener(StartPomodoro);
        setTimeButton.onClick.AddListener(ShowTimeSetup);
        
        // Hide timer UI initially
        timerUI.SetActive(false);
        setupUI.SetActive(true);
    }

    void SetupRestingState()
    {
        // Set to resting state
        currentIndex = 0;
        for (int i = 0; i < backgroundSprites.Length; i++)
        {
            backgroundSprites[i].SetActive(i == currentIndex);
        }
    }

    void ShowTimeSetup()
    {
        // Stop any running timer
        timerIsRunning = false;
        if (restingTransitionCoroutine != null)
        {
            StopCoroutine(restingTransitionCoroutine);
            restingTransitionCoroutine = null;
        }
        
        // Show setup UI
        setupUI.SetActive(true);
        timerUI.SetActive(false);
        
        // Return to resting state
        SetupRestingState();
    }

    void StartPomodoro()
    {
        // Parse input time
        if (int.TryParse(minutesInput.text, out int minutes))
        {
            if (minutes <= 0) minutes = 25; // Default to 25 minutes
            
            timeRemaining = minutes * 60;
            timerIsRunning = true;
            
            // Switch to learning state
            currentIndex = 1;
            backgroundSprites[0].SetActive(false);
            backgroundSprites[1].SetActive(true);
            
            // Show timer UI
            setupUI.SetActive(false);
            timerUI.SetActive(true);
            
            StartCoroutine(UpdateTimer());
        }
    }

    IEnumerator UpdateTimer()
    {
        while (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
            }
            else
            {
                // Timer finished
                timerIsRunning = false;
                OnPomodoroComplete();
            }
            yield return null;
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void OnPomodoroComplete()
    {
        // Switch to "Start Resting" state
        currentIndex = 2;
        backgroundSprites[1].SetActive(false);
        backgroundSprites[2].SetActive(true);
        
        // Start transition to resting state after a delay
        restingTransitionCoroutine = StartCoroutine(TransitionToResting());
    }

    IEnumerator TransitionToResting()
    {
        // Wait for 2 seconds in "Start Resting" state
        yield return new WaitForSeconds(2f);
        
        // Switch to resting state
        currentIndex = 0;
        backgroundSprites[2].SetActive(false);
        backgroundSprites[0].SetActive(true);
    }

    void OnDestroy()
    {
        // Clean up coroutines
        if (restingTransitionCoroutine != null)
        {
            StopCoroutine(restingTransitionCoroutine);
        }
    }
}