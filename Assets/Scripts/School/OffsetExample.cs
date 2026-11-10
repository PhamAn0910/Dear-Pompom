using UnityEngine;

/// <summary>
/// Example script showing how to configure different offset values for different difficulty levels.
/// This script can be attached to any GameObject and will set up offset values when the game starts.
/// </summary>
public class OffsetExample : MonoBehaviour
{
    [Header("Offset Configuration Examples")]
    [Tooltip("Offset values for Easy mode")]
    public Vector2 easyOffsets = new Vector2(2.5f, 3.0f);
    
    [Tooltip("Offset values for Medium mode")]
    public Vector2 mediumOffsets = new Vector2(2.2f, 2.7f);
    
    [Tooltip("Offset values for Hard mode")]
    public Vector2 hardOffsets = new Vector2(1.8f, 2.2f);
    
    [Tooltip("Offset values for Expert mode")]
    public Vector2 expertOffsets = new Vector2(1.5f, 1.8f);

    private void Start()
    {
        // Wait a frame to ensure GameManager and other components are initialized
        StartCoroutine(SetupOffsetsAfterFrame());
    }

    private System.Collections.IEnumerator SetupOffsetsAfterFrame()
    {
        yield return null; // Wait one frame
        
        SetupDifficultyOffsets();
    }

    private void SetupDifficultyOffsets()
    {
        var gameController = FindFirstObjectByType<GameSceneController2>();
        if (gameController == null)
        {
            Debug.LogWarning("GameSceneController2 not found in scene");
            return;
        }

        // Set up offset values for each difficulty level
        // Note: The difficulty names should match those configured in your DifficultySettings
        gameController.UpdateDifficultyOffsets("Easy", easyOffsets.x, easyOffsets.y);
        gameController.UpdateDifficultyOffsets("Medium", mediumOffsets.x, mediumOffsets.y);
        gameController.UpdateDifficultyOffsets("Hard", hardOffsets.x, hardOffsets.y);
        gameController.UpdateDifficultyOffsets("Expert", expertOffsets.x, expertOffsets.y);

        Debug.Log("Difficulty offset values have been configured:");
        Debug.Log($"Easy: {easyOffsets}");
        Debug.Log($"Medium: {mediumOffsets}");
        Debug.Log($"Hard: {hardOffsets}");
        Debug.Log($"Expert: {expertOffsets}");
    }

    // You can also call this method to update offsets at runtime
    public void UpdateOffsetsForCurrentDifficulty(float newOffsetX, float newOffsetY)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager instance not found");
            return;
        }

        var currentDifficulty = GameManager.Instance.GetCurrentDifficulty();
        if (currentDifficulty == null)
        {
            Debug.LogError("Current difficulty not found");
            return;
        }

        var gameController = FindFirstObjectByType<GameSceneController2>();
        if (gameController != null)
        {
            gameController.UpdateDifficultyOffsets(currentDifficulty.name, newOffsetX, newOffsetY);
            
            // Restart the game to apply the new offsets
            gameController.RestartGame();
        }
    }
}
