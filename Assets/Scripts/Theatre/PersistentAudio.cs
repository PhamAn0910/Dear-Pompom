using UnityEngine;

public class PersistentAudio : MonoBehaviour
{
    private static PersistentAudio instance;

    void Awake()
    {
        // Ensure only one instance exists (Singleton pattern)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Makes the object persistent
        }
        else
        {
            // If another instance exists, destroy this one to avoid duplicates
            Destroy(gameObject);
        }
    }
}