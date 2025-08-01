using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // Ensures there's always an Image component
public class InstrumentButton : MonoBehaviour
{
    [Tooltip("Drag the AudioSource for this instrument here")]
    public AudioSource instrumentAudioSource;
    
    private bool isMuted = false;
    private Image buttonImage;
    //private Color defaultColor;
    private Color32 myGreen = new Color32(255, 255, 255, 255); // (R, G, B, A)
    private Color32 myYellow = new Color32(255, 255, 255, 180); // (R, G, B, A)
    private bool isPlaying = false;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        //defaultColor = buttonImage.color;
        
        // Initialize color
        UpdateButtonColor();
    }
    
    public void ToggleInstrumentMute()
    {
        if (instrumentAudioSource == null)
        {
            Debug.LogWarning("No AudioSource assigned to this instrument button!");
            return;
        }
        
        isMuted = !isMuted;
        instrumentAudioSource.mute = isMuted;
        
        UpdateButtonColor();
    }
    
    public void SetPlayingState(bool playing)
    {
        isPlaying = playing;
        UpdateButtonColor();
    }
    
    private void UpdateButtonColor()
    {
        if (buttonImage == null) return;
        
        buttonImage.color = (isPlaying && !isMuted) ? myGreen : myYellow;
    }
}