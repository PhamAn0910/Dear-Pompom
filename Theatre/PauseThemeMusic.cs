using UnityEngine;

public class TheatreSceneAudioController : MonoBehaviour
{
    public enum AudioHandlingMethod { Mute, Pause, Stop, Destroy }

    [Tooltip("Choose how to handle the persistent audio sources")]
    public AudioHandlingMethod audioHandlingMethod = AudioHandlingMethod.Mute;

    [Tooltip("If true, will restore audio when leaving this scene")]
    public bool restoreAudioOnExit = true;

    private AudioSource[] persistentAudioSources;
    private bool[] wasPlaying;
    private float[] originalVolumes;

    void Awake()
    {
        // Find all game objects with AudioSource components
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        // Filter for persistent audio sources (those marked DontDestroyOnLoad)
        persistentAudioSources = System.Array.FindAll(allAudioSources, source => 
            source.gameObject.scene.name == "DontDestroyOnLoad");

        // Store their original states if we need to restore later
        if (restoreAudioOnExit)
        {
            wasPlaying = new bool[persistentAudioSources.Length];
            originalVolumes = new float[persistentAudioSources.Length];
            
            for (int i = 0; i < persistentAudioSources.Length; i++)
            {
                wasPlaying[i] = persistentAudioSources[i].isPlaying;
                originalVolumes[i] = persistentAudioSources[i].volume;
            }
        }

        // Handle the audio sources based on the selected method
        HandlePersistentAudio();
    }

    void OnDestroy()
    {
        if (restoreAudioOnExit)
        {
            RestorePersistentAudio();
        }
    }

    private void HandlePersistentAudio()
    {
        foreach (AudioSource source in persistentAudioSources)
        {
            if (source == null) continue;

            switch (audioHandlingMethod)
            {
                case AudioHandlingMethod.Mute:
                    source.mute = true;
                    break;
                case AudioHandlingMethod.Pause:
                    if (source.isPlaying) source.Pause();
                    break;
                case AudioHandlingMethod.Stop:
                    source.Stop();
                    break;
                case AudioHandlingMethod.Destroy:
                    Destroy(source.gameObject);
                    break;
            }
        }

        Debug.Log($"Handled {persistentAudioSources.Length} persistent audio source(s) using method: {audioHandlingMethod}");
    }

    private void RestorePersistentAudio()
    {
        for (int i = 0; i < persistentAudioSources.Length; i++)
        {
            if (persistentAudioSources[i] == null) continue;

            switch (audioHandlingMethod)
            {
                case AudioHandlingMethod.Mute:
                    persistentAudioSources[i].mute = false;
                    break;
                case AudioHandlingMethod.Pause:
                    if (wasPlaying[i]) persistentAudioSources[i].UnPause();
                    break;
                case AudioHandlingMethod.Stop:
                    if (wasPlaying[i]) persistentAudioSources[i].Play();
                    break;
                case AudioHandlingMethod.Destroy:
                    // No restoration possible for destroyed objects
                    break;
            }

            // Restore original volume
            if (audioHandlingMethod != AudioHandlingMethod.Destroy)
            {
                persistentAudioSources[i].volume = originalVolumes[i];
            }
        }
    }
}