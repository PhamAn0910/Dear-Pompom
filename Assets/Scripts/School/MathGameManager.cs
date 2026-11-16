using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using DearPompom.Core;

public class MathGameManager : MonoBehaviour
{
    public static MathGameManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] optionTexts;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI streakText;

    [Header("Game Settings")]
    public float timePerQuestion = 15f;
    public int basePoints = 10;
    public float streakMultiplier = 0.5f; // 50% bonus per streak

    private int currentAnswer;
    private int score = 0;
    private int streak = 0;
    private float timeLeft;
    private bool gameActive = false;
    private float gameStartTime;

    [Header("Game Over UI")]
    public Button restartButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        EnsureOptionImages();
        StartNewGame();
    }

    void StartNewGame()
    {
        score = 0;
        streak = 0;
        UpdateScoreUI();
        GenerateNewQuestion();
        gameActive = true;
        gameStartTime = Time.unscaledTime;
    }

    void Update()
    {
        if (gameActive)
        {
            timeLeft -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(timeLeft).ToString();

            if (timeLeft <= 0)
            {
                timeLeft = 0;
                GameOver();
            }
        }
    }

    void GenerateNewQuestion()
    {
        timeLeft = timePerQuestion;

        // Generate random numbers and operation
        int a = UnityEngine.Random.Range(1, 10);
        int b = UnityEngine.Random.Range(1, 10);
        string[] operators = { "+", "-", "×", "÷" };
        string op = operators[UnityEngine.Random.Range(0, operators.Length)];

        // Calculate correct answer
        switch (op)
        {
            case "+":
                currentAnswer = a + b;
                break;
            case "-":
                currentAnswer = a - b;
                break;
            case "×":
                currentAnswer = a * b;
                break;
            case "÷":
                // Ensure division results in integer
                currentAnswer = a;
                a = a * b; // So a ÷ b = original a
                break;
        }

        questionText.text = $"{a} {op} {b} = ?";

        // Generate options (one correct, others random)
        int correctOption = UnityEngine.Random.Range(0, optionTexts.Length);
        List<int> usedAnswers = new List<int>() { currentAnswer };

        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i == correctOption)
            {
                optionTexts[i].text = currentAnswer.ToString();
            }
            else
            {
                int wrongAnswer;
                do
                {
                    // Generate plausible wrong answers
                    int offset = UnityEngine.Random.Range(-3, 4);
                    wrongAnswer = currentAnswer + offset;
                    if (wrongAnswer == currentAnswer) wrongAnswer += 5; // Ensure it's different
                } while (usedAnswers.Contains(wrongAnswer));

                usedAnswers.Add(wrongAnswer);
                optionTexts[i].text = wrongAnswer.ToString();
            }
        }
    }

    public void OnOptionSelected(int optionIndex)
    {
        if (!gameActive) return;

        if (optionTexts[optionIndex].text == currentAnswer.ToString())
        {
            // Correct answer
            streak++;
            int pointsEarned = basePoints + (int)(basePoints * streak * streakMultiplier);
            score += pointsEarned;

            // Visual feedback for correct answer
            StartFlash(optionIndex, Color.green);
        }
        else
        {
            // Wrong answer
            streak = 0;
            StartFlash(optionIndex, Color.red);
            StartFlash(FindCorrectOption(), Color.green);
        }

        UpdateScoreUI();
        GenerateNewQuestion();
    }

    private int FindCorrectOption()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i].text == currentAnswer.ToString())
            {
                return i;
            }
        }
        return 0;
    }

    // Each button keeps its resting colour and at most one running flash. The 1.0 code
    // read the "original" colour at flash start, so a second flash that began during the
    // first one saved green as the original and the button stayed green (bug-log W1).
    private Image[] optionImages;
    private Color[] optionRestColors;
    private Coroutine[] optionFlashes;

    private void EnsureOptionImages()
    {
        if (optionImages != null) return;

        optionImages = new Image[optionTexts.Length];
        optionRestColors = new Color[optionTexts.Length];
        optionFlashes = new Coroutine[optionTexts.Length];
        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionImages[i] = optionTexts[i].GetComponentInParent<Image>();
            optionRestColors[i] = optionImages[i] != null ? optionImages[i].color : Color.white;
        }
    }

    private void StartFlash(int index, Color color)
    {
        EnsureOptionImages();
        if (optionImages[index] == null) return;

        if (optionFlashes[index] != null) StopCoroutine(optionFlashes[index]);
        optionFlashes[index] = StartCoroutine(FlashOption(index, color));
    }

    IEnumerator FlashOption(int index, Color color)
    {
        optionImages[index].color = color;
        yield return new WaitForSeconds(0.5f);
        optionImages[index].color = optionRestColors[index];
        optionFlashes[index] = null;
    }

    void UpdateScoreUI()
    {
        scoreText.text = "Score: " + score;
        streakText.text = "Streak: " + streak + "x";
    }

    void GameOver()
    {
        gameActive = false;
        questionText.text = "Game Over!\nFinal Score: " + score;

        // Log the activity
        if (UserProgressManager.Instance != null)
        {
            float duration = Time.unscaledTime - gameStartTime;
            string difficulty = "Standard"; // Math game has one difficulty level
            
            // Use RecordActivity instead of manually adding logs
            UserProgressManager.Instance.RecordActivity(
                ActivityType.MathGame,
                duration / 60, // seconds to minutes
                score: score,
                difficulty: difficulty
            );
        }

        // Disable all options
        foreach (TextMeshProUGUI option in optionTexts)
        {
            option.GetComponentInParent<Button>().interactable = false;
        }

        // Show restart button
        restartButton.gameObject.SetActive(true);
    }
    
    public void RestartGame()
    {
        // Hide restart button
        restartButton.gameObject.SetActive(false);
        
        // Re-enable all option buttons
        foreach (TextMeshProUGUI option in optionTexts)
        {
            option.GetComponentInParent<Button>().interactable = true;
        }
        
        StartNewGame();
    }
}