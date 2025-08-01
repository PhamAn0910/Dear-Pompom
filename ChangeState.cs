using UnityEngine;
using UnityEngine.UI;

public class BackgroundCycler : MonoBehaviour
{
    public GameObject[] backgroundSprites; // Assign your 3 sprites in inspector
    public Button cycleButton; // Assign your UI button
    
    private int currentIndex = 0;

    void Start()
    {
        // Ensure only the first sprite is active at start
        for (int i = 0; i < backgroundSprites.Length; i++)
        {
            backgroundSprites[i].SetActive(i == 0);
        }
        
        // Add click listener to the button
        cycleButton.onClick.AddListener(CycleBackground);
    }

    void CycleBackground()
    {
        // Disable current sprite
        backgroundSprites[currentIndex].SetActive(false);
        
        // Move to next index (wrap around if needed)
        currentIndex = (currentIndex + 1) % backgroundSprites.Length;
        
        // Enable next sprite
        backgroundSprites[currentIndex].SetActive(true);
    }
}