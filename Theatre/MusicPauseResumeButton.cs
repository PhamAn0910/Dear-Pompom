using UnityEngine;
using UnityEngine.UI;

public class MusicPauseResumeButton : MonoBehaviour
{
    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private Sprite resumeIcon;
    
    private Image buttonImage;
    private bool isPaused = false;
    
    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        UpdateButtonIcon();
    }
    
    public void TogglePauseResume()
    {
        isPaused = !isPaused;
        
        InstrumentButton[] allInstrumentButtons = FindObjectsByType<InstrumentButton>(FindObjectsSortMode.None);
        
        foreach (InstrumentButton instrumentButton in allInstrumentButtons)
        {
            AudioSource audioSource = instrumentButton.instrumentAudioSource;
            
            if (audioSource != null)
            {
                if (isPaused && audioSource.isPlaying)
                {
                    audioSource.Pause();
                    instrumentButton.SetPlayingState(false);
                }
                else if (!isPaused && !audioSource.isPlaying)
                {
                    audioSource.UnPause();
                    instrumentButton.SetPlayingState(true);
                }
            }
        }
        
        UpdateButtonIcon();
        Debug.Log(isPaused ? $"Paused {allInstrumentButtons.Length} instrument(s)" : 
                            $"Resumed {allInstrumentButtons.Length} instrument(s)");
    }
    
    private void UpdateButtonIcon()
    {
        if (buttonImage != null && pauseIcon != null && resumeIcon != null)
        {
            buttonImage.sprite = isPaused ? resumeIcon : pauseIcon;
        }
    }
}