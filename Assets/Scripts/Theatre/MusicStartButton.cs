using UnityEngine;

public class MusicStartButton : MonoBehaviour
{
    public void StartAllMusic()
    {
        // Find all InstrumentButtons instead of AudioSources
        InstrumentButton[] allInstrumentButtons = FindObjectsByType<InstrumentButton>(FindObjectsSortMode.None);
        
        foreach (InstrumentButton instrumentButton in allInstrumentButtons)
        {
            AudioSource audioSource = instrumentButton.instrumentAudioSource;
            
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                audioSource.mute = false;
                instrumentButton.SetPlayingState(true);
            }
        }
        
        Debug.Log($"Started {allInstrumentButtons.Length} instrument(s)");
    }
}