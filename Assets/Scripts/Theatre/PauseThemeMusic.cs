using DearPompom;
using UnityEngine;

/// <summary>
/// Placed in the Theatre scene: fades the village music out while the theatre plays its own
/// song and brings it back on the way out (PRD M4). 1.0 muted every persistent AudioSource it
/// could find; now it asks AudioManager, which only touches background music.
/// </summary>
public class TheatreSceneAudioController : MonoBehaviour
{
    // Kept so the values serialized in the Theatre scene still load. Ducking replaces all four modes.
    public enum AudioHandlingMethod { Mute, Pause, Stop, Destroy }
    public AudioHandlingMethod audioHandlingMethod = AudioHandlingMethod.Mute;
    public bool restoreAudioOnExit = true;

    void Awake()
    {
        Services.EnsureBooted();
        Services.Audio.DuckBackground(true);
    }

    void OnDestroy()
    {
        if (restoreAudioOnExit && Services.Audio != null) Services.Audio.DuckBackground(false);
    }
}
