using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class RenderPlaceWithoutCard : MonoBehaviour
{
    public string sceneName; // Name of the scene to load
    
    [Header("Card Management")]
    [SerializeField] private bool destroyCardsOnSceneLoad = true;
    [SerializeField] private bool preserveMusic = true;
    
    // This function is called when the button is clicked
    public void LoadScene()
    {
        Debug.Log($"Attempting to load scene: {sceneName}");

        if (destroyCardsOnSceneLoad)
        {
            DestroyMemoryGameObjects(); // Call the specific cleanup for memory game
        }
        
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not specified!");
        }
    }
    
    // NEW: Specific method to destroy objects related to the memory game scene
    private void DestroyMemoryGameObjects()
    {
        // Find the GameSceneController2 and destroy its GameObject
        // This will also destroy all cards that are children of GameSceneController2
        GameSceneController2 gameController = FindFirstObjectByType<GameSceneController2>();
        if (gameController != null)
        {
            Debug.Log($"Destroying GameSceneController2 GameObject: {gameController.name} and its children (cards).");
            Destroy(gameController.gameObject); // This is the key line
        }
        else
        {
            Debug.Log("No GameSceneController2 found to destroy. Cards might have already been destroyed or are not children of it.");
            // Fallback: destroy any remaining Card objects not tied to a controller
            DestroyAllCardInstances();
        }
    }

    // NEW/MODIFIED: Fallback to destroy any remaining Card instances
    // This method was part of the old DestroyCardPrefabs, now extracted for fallback
    private void DestroyAllCardInstances()
    {
        Card[] allCards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        if (allCards.Length > 0)
        {
            Debug.Log($"Destroying {allCards.Length} leftover card instances...");
            foreach (Card card in allCards)
            {
                if (card != null && card.gameObject != null)
                {
                    // Preserve audio if it's on the card itself and needs to persist
                    if (preserveMusic && HasPersistentAudio(card.gameObject))
                    {
                        PreserveAudioComponents(card.gameObject);
                    }
                    Destroy(card.gameObject);
                }
            }
        }
    }

    // This method is now the primary way to leave the memory game scene
    // MODIFIED: It now calls DestroyMemoryGameObjects()
    public void CleanupAndLoadScene()
    {
        Debug.Log("Cleaning up memory game scene before loading new scene...");
        
        // Ensure memory game specific objects are gone by destroying the controller
        DestroyMemoryGameObjects(); 
        
        // Optional: Clean up other objects while preserving music (if any are not tied to GameSceneController2)
        CleanupNonEssentialObjects();
        
        // Load the new scene
        LoadScene();
    }
    
    // Check if the game object or its children have persistent audio components
    private bool HasPersistentAudio(GameObject obj)
    {
        // ... (Keep your existing HasPersistentAudio method as is) ...
        PersistentAudio persistentAudio = obj.GetComponentInChildren<PersistentAudio>();
        if (persistentAudio != null)
        {
            return true;
        }
        
        AudioSource[] audioSources = obj.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.gameObject.scene.name == "DontDestroyOnLoad")
            {
                return true;
            }
        }
        return false;
    }
    
    // Preserve audio components by moving them to a persistent object
    private void PreserveAudioComponents(GameObject cardObject)
    {
        // ... (Keep your existing PreserveAudioComponents method as is) ...
        GameObject audioContainer = GameObject.Find("PersistentAudioContainer");
        if (audioContainer == null)
        {
            audioContainer = new GameObject("PersistentAudioContainer");
            DontDestroyOnLoad(audioContainer);
        }
        
        PersistentAudio[] persistentAudios = cardObject.GetComponentsInChildren<PersistentAudio>();
        foreach (PersistentAudio persistentAudio in persistentAudios)
        {
            persistentAudio.transform.SetParent(audioContainer.transform);
        }
        
        AudioSource[] audioSources = cardObject.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            if (audioSource.isPlaying || audioSource.clip != null)
            {
                audioSource.transform.SetParent(audioContainer.transform);
            }
        }
    }
    
    // Clean up other non-essential objects while preserving music and important game objects
    private void CleanupNonEssentialObjects()
    {
        // ... (Keep your existing CleanupNonEssentialObjects method as is) ...
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (ShouldPreserveObject(obj))
            {
                continue;
            }
            
            if (obj.scene.name == "DontDestroyOnLoad")
            {
                continue;
            }
            
            // Destroy objects that are not marked to be preserved and are not in DontDestroyOnLoad scene
            // Be very careful with this, as it can destroy unexpected objects.
            // Consider adding specific tags or layers for objects to be cleaned up.
            // For now, only destroy if it's not a root object and not part of the UI or other managers
            if (obj.transform.parent == null && obj.GetComponent<Canvas>() == null && obj.GetComponent<EventSystem>() == null)
            {
                // Debug.Log($"Consider destroying: {obj.name}"); // Uncomment to see what would be destroyed
                // Destroy(obj); // Uncomment with caution!
            }
        }
    }
    
    // Determine if an object should be preserved during cleanup
    private bool ShouldPreserveObject(GameObject obj)
    {
        // ... (Keep your existing ShouldPreserveObject method as is) ...
        if (obj.GetComponent<PersistentAudio>() != null) return true;
        if (obj.GetComponent<GameManager>() != null) return true;
        if (obj.GetComponent<Camera>() != null) return true; // Preserve Main Camera
        if (obj.name.Contains("EventSystem")) return true; // Preserve EventSystem
        if (obj.GetComponent<Canvas>() != null) return true; // Preserve UI Canvas
        
        return false;
    }

    // REMOVE THESE OLD METHODS (if they exist in your current script)
    // public void DestroyCardPrefabs() { /* ... */ }
    // public void DestroyCardPrefabsByTag(string tag) { /* ... */ }
    // public void DestroyCardsInController(GameSceneController2 controller) { /* ... */ }
}