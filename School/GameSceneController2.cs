using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GameSceneController2 : MonoBehaviour
{
    [Header("Game Setup")]
    [SerializeField] private Card originalCard; // Complete card prefab with cardback/cardfront children
    [SerializeField] private TextMeshProUGUI scoreLabel;
    
    [Header("Grid Settings")]
    [SerializeField] private float defaultOffsetX = 2.2f; // Default fallback values
    [SerializeField] private float defaultOffsetY = 2.7f;
    [SerializeField] private float cardScale = 1f;

    private Card _firstRevealed;
    private Card _secondRevealed;
    private int _score = 0;
    private List<Card> _allCards = new List<Card>();

    public bool CanReveal => _secondRevealed == null;

    // --- NEW VARIABLE for logging ---
    private float gameStartTime; // To track when the game began

    private void Start()
    {
        // Initialize difficulty offsets on start
        InitializeDifficultyOffsets();
        
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Reset score
        _score = 0;
        UpdateScoreText();

        // Clear existing cards
        ClearExistingCards();

        // Check if GameManager exists and has valid settings
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null! Make sure GameManager exists in the scene.");
            return;
        }

        // Force refresh of difficulty settings in case we returned from another scene
        if (!GameManager.Instance.HasValidSettings())
        {
            Debug.LogError("GameManager has invalid settings!");
            return;
        }

        // --- NEW CODE: Record game start time ---
        gameStartTime = Time.time;

        var currentDifficulty = GameManager.Instance.GetCurrentDifficulty();
        if (currentDifficulty == null)
        {
            Debug.LogError("Current difficulty is null!");
            return;
        }

        CreateCardGrid(currentDifficulty);
    }

    private void ClearExistingCards()
    {
        // Destroy all existing cards
        foreach (Card card in _allCards)
        {
            if (card != null && card != originalCard) // Don't destroy the original prefab
            {
                DestroyImmediate(card.gameObject);
            }
        }
        _allCards.Clear();

        // Also clear any remaining card objects that are children of this transform
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child != originalCard?.transform) // Don't destroy the original
            {
                Card childCard = child.GetComponent<Card>();
                if (childCard != null && childCard != originalCard)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        // Reset revealed cards
        _firstRevealed = null;
        _secondRevealed = null;
        
        // Reset score
        _score = 0;
        UpdateScoreText();
    }

    private void CreateCardGrid(DifficultySettings.DifficultyLevel difficulty)
    {
        EnsureOriginalCard();
        
        if (originalCard == null)
        {
            Debug.LogError("Original card is not assigned!");
            return;
        }

        // Calculate dynamic card scale based on grid size
        float dynamicScale = CalculateCardScale(difficulty.rows, difficulty.cols);
        
        // Get offset values from difficulty settings, with fallback to defaults
        float offsetX = difficulty.offsetX > 0 ? difficulty.offsetX : defaultOffsetX;
        float offsetY = difficulty.offsetY > 0 ? difficulty.offsetY : defaultOffsetY;
        
        Debug.Log($"Level '{difficulty.name}' started with offsetX: {offsetX}, offsetY: {offsetY}");
        
        // Calculate grid dimensions and center position
        float actualOffsetX = offsetX * dynamicScale;
        float actualOffsetY = offsetY * dynamicScale;
        
        // Comment out centering logic
        float totalWidth = (difficulty.cols - 1) * actualOffsetX;
        float totalHeight = (difficulty.rows - 1) * actualOffsetY;
        Vector3 centerPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        Vector3 startPos = new Vector3(
            centerPos.x - totalWidth / 2,
            centerPos.y + totalHeight / 2,
            centerPos.z
        );

        // Vector3 startPos = originalCard.transform.position;

        // Create card pairs
        int pairCount = (difficulty.rows * difficulty.cols) / 2;
        int[] numbers = CreateCardPairs(pairCount);

        // Instantiate cards
        for (int i = 0; i < difficulty.cols; i++)
        {
            for (int j = 0; j < difficulty.rows; j++)
            {
                Card card = Instantiate(originalCard, transform); // Instantiate as child of this transform
                
                // Ensure the card is enabled after instantiation
                card.gameObject.SetActive(true);
                
                int index = j * difficulty.cols + i;
                int id = numbers[index];
                
                // Make sure we don't exceed available sprites
                int spriteIndex = Mathf.Min(id, difficulty.cardImages.Length - 1);
                card.SetCard(id, difficulty.cardImages[spriteIndex], difficulty.cardBackImage);

                float posX = startPos.x + (actualOffsetX * i);
                float posY = startPos.y - (actualOffsetY * j);
                card.transform.position = new Vector3(posX, posY, startPos.z);
                
                // Apply dynamic scale to the entire card, not individual components
                card.transform.localScale = Vector3.one * dynamicScale;
                
                // Ensure card starts in correct state
                card.Unreveal();
                
                _allCards.Add(card);
            }
        }

        originalCard.gameObject.SetActive(false);
        Debug.Log($"Created {_allCards.Count} cards for {difficulty.name} difficulty");
    }

    private float CalculateCardScale(int rows, int cols)
    {
        // Calculate scale based on screen size and number of cards
        float baseScale = cardScale;
        float maxCards = Mathf.Max(rows, cols);
        
        if (maxCards <= 4)
            return baseScale;
        else if (maxCards <= 6)
            return baseScale * 0.8f;
        else
            return baseScale * 0.6f;
    }

    private int[] CreateCardPairs(int pairCount)
    {
        int[] numbers = new int[pairCount * 2];
        for (int i = 0; i < pairCount; i++)
        {
            numbers[i * 2] = i;
            numbers[i * 2 + 1] = i;
        }
        return ShuffleArray(numbers);
    }

    private int[] ShuffleArray(int[] numbers)
    {
        int[] newArray = numbers.Clone() as int[];
        for (int i = 0; i < newArray.Length; i++)
        {
            int tmp = newArray[i];
            int r = Random.Range(i, newArray.Length);
            newArray[i] = newArray[r];
            newArray[r] = tmp;
        }
        return newArray;
    }

    public void CardRevealed(Card card)
    {
        if (!CanReveal) return;

        if (_firstRevealed == null)
        {
            _firstRevealed = card;
        }
        else
        {
            _secondRevealed = card;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        if (_firstRevealed.Id == _secondRevealed.Id)
        {
            // Cards match - increment score and keep them revealed
            _score++;
            UpdateScoreText();
            Debug.Log($"Match found! Score: {_score}");
            
            // Store references before they are reset
            Card matchedCard1 = _firstRevealed;
            Card matchedCard2 = _secondRevealed;

            // Reset revealed cards immediately so player can continue
            _firstRevealed = null;
            _secondRevealed = null;

            // Optionally disable the matched cards or mark them differently
            matchedCard1.gameObject.SetActive(false);
            matchedCard2.gameObject.SetActive(false);

            // --- NEW CODE: Check for win condition after a match ---
            CheckForWinCondition();
        }
        else
        {
            // Cards don't match - wait then hide them
            yield return new WaitForSeconds(0.5f);
            _firstRevealed.Unreveal();
            _secondRevealed.Unreveal();

            // Reset revealed cards
            _firstRevealed = null;
            _secondRevealed = null;
        }
    }

    // --- NEW METHOD ---
    private void CheckForWinCondition()
    {
        // A game is won if all cards that were created are now inactive.
        // We use LINQ's .All() to check this condition.
        if (_allCards.All(card => !card.gameObject.activeSelf))
        {
            Debug.Log("Memory Card Game Completed!");
            LogGameCompletion();
        }
    }

    // --- NEW METHOD ---
    private void LogGameCompletion()
    {
        if (UserProgressManager.Instance != null)
        {
            float gameDurationMinutes = (Time.time - gameStartTime) / 60f;
            if (gameDurationMinutes < 0) gameDurationMinutes = 0;

            string difficultyName = "Unknown";
            if (GameManager.Instance != null && GameManager.Instance.HasValidSettings())
            {
                difficultyName = GameManager.Instance.GetCurrentDifficulty().name;
            }

            // Use RecordActivity instead of manually adding logs
            UserProgressManager.Instance.RecordActivity(
                "MemoryCard",        // activityType
                gameDurationMinutes, // durationMinutes
                _score,              // score
                "Game completed successfully", // notes
                difficultyName       // difficulty
            );

            Debug.Log($"Logged Memory Card Game: Difficulty {difficultyName}, Score {_score}, Duration {gameDurationMinutes:F1} mins.");
        }
        else
        {
            Debug.LogError("UserProgressManager.Instance is null. Cannot log Memory Card activity.");
        }
    }

    private void UpdateScoreText()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = "Score: " + _score;
        }
    }

    // Public method to restart game with current difficulty
    public void RestartGame()
    {
        InitializeGame();
    }

    // Method to initialize or update offset values for difficulty levels
    public void InitializeDifficultyOffsets()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasValidSettings())
        {
            Debug.LogError("Cannot initialize difficulty offsets - GameManager or settings invalid");
            return;
        }

        var difficultySettings = GameManager.Instance.GetComponent<DifficultySettings>();
        if (difficultySettings == null)
        {
            Debug.LogError("DifficultySettings component not found on GameManager");
            return;
        }

        // Initialize default offset values for each difficulty level if they're not set
        foreach (var level in difficultySettings.levels)
        {
            if (level.offsetX <= 0) level.offsetX = defaultOffsetX;
            if (level.offsetY <= 0) level.offsetY = defaultOffsetY;
            
            Debug.Log($"Difficulty '{level.name}' initialized with offsetX: {level.offsetX}, offsetY: {level.offsetY}");
        }
    }

    // Method to update offset values for a specific difficulty level
    public void UpdateDifficultyOffsets(string difficultyName, float newOffsetX, float newOffsetY)
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasValidSettings())
        {
            Debug.LogError("Cannot update difficulty offsets - GameManager or settings invalid");
            return;
        }

        var difficultySettings = GameManager.Instance.GetComponent<DifficultySettings>();
        if (difficultySettings == null)
        {
            Debug.LogError("DifficultySettings component not found on GameManager");
            return;
        }

        foreach (var level in difficultySettings.levels)
        {
            if (level.name.Equals(difficultyName, System.StringComparison.OrdinalIgnoreCase))
            {
                level.offsetX = newOffsetX;
                level.offsetY = newOffsetY;
                Debug.Log($"Updated '{difficultyName}' offsets to X: {newOffsetX}, Y: {newOffsetY}");
                return;
            }
        }

        Debug.LogWarning($"Difficulty level '{difficultyName}' not found");
    }

    private void EnsureOriginalCard()
    {
        if (originalCard == null)
        {
            // Find the original card in the scene - look for inactive cards first
            Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (Card card in allCards)
            {
                // Look for the original card prefab (usually inactive or named specifically)
                if (!card.gameObject.activeSelf || card.name.Contains("Original") || card.name.Contains("Prefab"))
                {
                    originalCard = card;
                    Debug.Log($"Found original card: {card.name}");
                    break;
                }
            }
            
            // If still not found, try to find any card that's not in our active list
            if (originalCard == null && allCards.Length > 0)
            {
                foreach (Card card in allCards)
                {
                    if (!_allCards.Contains(card))
                    {
                        originalCard = card;
                        Debug.Log($"Using card as original: {card.name}");
                        // Make sure it's inactive so it can serve as template
                        originalCard.gameObject.SetActive(false);
                        break;
                    }
                }
            }
            
            if (originalCard == null)
            {
                Debug.LogError("No suitable original card found! Make sure there's an inactive Card prefab in the scene.");
            }
        }
    }
}