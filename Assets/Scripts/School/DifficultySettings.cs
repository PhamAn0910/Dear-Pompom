using UnityEngine;

public class DifficultySettings : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyLevel
    {
        public string name;
        public int rows;
        public int cols;
        public Sprite[] cardImages;
        [Header("Card Back")]
        public Sprite cardBackImage;
        
        [Header("Grid Layout")]
        public float offsetX = 2.2f;
        public float offsetY = 2.7f;
        
        // Validation method
        public bool IsValid()
        {
            return rows > 0 && cols > 0 && (rows * cols) % 2 == 0 && cardImages != null && cardImages.Length >= (rows * cols) / 2;
        }
    }

    [Header("Difficulty Levels Configuration")]
    public DifficultyLevel[] levels;
}
