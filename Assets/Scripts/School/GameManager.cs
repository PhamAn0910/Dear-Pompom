// // GameManager.cs
// using UnityEngine;

// public class GameManager : MonoBehaviour
// {
//     public static GameManager Instance { get; private set; }

//     [SerializeField] private DifficultySettings difficultySettings;
//     private int currentLevelIndex = 0;

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     public void SetDifficulty(int levelIndex)
//     {
//         currentLevelIndex = Mathf.Clamp(levelIndex, 0, difficultySettings.levels.Length - 1);
//     }

//     public DifficultySettings.DifficultyLevel GetCurrentDifficulty()
//     {
//         return difficultySettings.levels[currentLevelIndex];
//     }
// }


using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private DifficultySettings difficultySettings;
    private int currentLevelIndex = 0;

    private void Awake()
    {
        // Don't destroy on load and ensure singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Validate settings on start
        if (difficultySettings == null)
        {
            Debug.LogError("DifficultySettings not assigned to GameManager!");
            return;
        }

        if (difficultySettings.levels == null || difficultySettings.levels.Length == 0)
        {
            Debug.LogError("No difficulty levels configured!");
            return;
        }

        // Validate each level
        for (int i = 0; i < difficultySettings.levels.Length; i++)
        {
            if (!difficultySettings.levels[i].IsValid())
            {
                Debug.LogError($"Difficulty level {i} ({difficultySettings.levels[i].name}) is not valid!");
            }
        }
    }

    public void SetDifficulty(int levelIndex)
    {
        EnsureDifficultySettings();
        
        if (difficultySettings == null)
        {
            Debug.LogError("DifficultySettings is null!");
            return;
        }

        if (difficultySettings.levels == null)
        {
            Debug.LogError("DifficultySettings.levels is null!");
            return;
        }

        if (levelIndex < 0 || levelIndex >= difficultySettings.levels.Length)
        {
            Debug.LogError($"Invalid level index: {levelIndex}. Available levels: 0-{difficultySettings.levels.Length - 1}");
            return;
        }

        currentLevelIndex = levelIndex;
        Debug.Log($"Difficulty set to: {difficultySettings.levels[currentLevelIndex].name}");
    }

    public DifficultySettings.DifficultyLevel GetCurrentDifficulty()
    {
        EnsureDifficultySettings();
        
        if (difficultySettings == null || difficultySettings.levels == null || 
            currentLevelIndex >= difficultySettings.levels.Length)
        {
            Debug.LogError("Cannot get current difficulty - settings invalid");
            return null;
        }

        return difficultySettings.levels[currentLevelIndex];
    }

    public bool HasValidSettings()
    {
        EnsureDifficultySettings();
        return difficultySettings != null && difficultySettings.levels != null && 
               difficultySettings.levels.Length > 0;
    }

    private void EnsureDifficultySettings()
    {
        if (difficultySettings == null)
        {
            difficultySettings = FindFirstObjectByType<DifficultySettings>();
            if (difficultySettings == null)
            {
                Debug.LogError("No DifficultySettings found in scene! Make sure there's a GameObject with DifficultySettings component.");
            }
        }
    }
}