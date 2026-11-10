// // UIManager.cs
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.SceneManagement;

// public class GameUIManager : MonoBehaviour
// {
//     [SerializeField] private Button easyButton;
//     [SerializeField] private Button mediumButton;
//     [SerializeField] private Button hardButton;

//     private void Start()
//     {
//         easyButton.onClick.AddListener(() => SelectDifficulty(0));
//         mediumButton.onClick.AddListener(() => SelectDifficulty(1));
//         hardButton.onClick.AddListener(() => SelectDifficulty(2));
//     }

//     private void SelectDifficulty(int levelIndex)
//     {
//         GameManager.Instance.SetDifficulty(levelIndex);
//         SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current scene
//     }
// }


using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    private void Start()
    {
        if (easyButton != null)
            easyButton.onClick.AddListener(() => SelectDifficulty(0));
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => SelectDifficulty(1));
        if (hardButton != null)
            hardButton.onClick.AddListener(() => SelectDifficulty(2));
    }

    private void SelectDifficulty(int levelIndex)
    {
        Debug.Log($"Selecting difficulty level: {levelIndex}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }

        GameManager.Instance.SetDifficulty(levelIndex);

        // If we're in the same scene, restart the game
        GameSceneController2 gameController = FindFirstObjectByType<GameSceneController2>();
        if (gameController != null)
        {
            gameController.RestartGame();
        }
        // If you want to load a different scene instead:
        // SceneManager.LoadScene("GameScene");
    }
}