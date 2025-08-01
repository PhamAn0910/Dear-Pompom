using UnityEngine;

public class InstrumentStatic : MonoBehaviour
{
    private bool isMuted = false;
    private bool hasStartedPlaying = false;
    
    public void ToggleMute()
    {
        // Find all AudioSources in the scene
        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        
        if (!hasStartedPlaying)
        {
            // First click: Start playing all audio sources
            foreach (AudioSource audioSource in allAudioSources)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            hasStartedPlaying = true;
            isMuted = false;
            AudioListener.volume = 1f;
            Debug.Log($"Audio started playing - Found {allAudioSources.Length} audio sources");
        }
        else
        {
            // Subsequent clicks: Toggle mute/unmute
            isMuted = !isMuted;
            
            // Control global audio
            AudioListener.volume = isMuted ? 0f : 1f;
            
            // Also control all AudioSources
            foreach (AudioSource audioSource in allAudioSources)
            {
                audioSource.mute = isMuted;
            }
            
            Debug.Log($"Audio {(isMuted ? "muted" : "unmuted")} - Found {allAudioSources.Length} audio sources");
        }
        
        // If no audio sources found, suggest adding one
        if (allAudioSources.Length == 0)
        {
            Debug.LogWarning("No AudioSources found in scene! Add an AudioSource component with an AudioClip to hear audio.");
        }
    }
}
