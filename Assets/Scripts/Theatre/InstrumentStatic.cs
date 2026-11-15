using UnityEngine;

/// <summary>
/// Theatre "play / mute all" toggle. First click starts the instruments, later clicks mute or
/// unmute them. Only touches AudioSources that belong to the Theatre scene: 1.0 set
/// AudioListener.volume, which silenced click sounds and everything else too (PRD B5).
/// </summary>
public class InstrumentStatic : MonoBehaviour
{
    private bool isMuted = false;
    private bool hasStartedPlaying = false;

    public void ToggleMute()
    {
        AudioSource[] all = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        if (!hasStartedPlaying)
        {
            hasStartedPlaying = true;
            isMuted = false;
        }
        else
        {
            isMuted = !isMuted;
        }

        foreach (AudioSource source in all)
        {
            if (!BelongsToThisScene(source)) continue;
            source.mute = isMuted;
            if (!isMuted && !source.isPlaying) source.Play();
        }
    }

    // Persistent objects (services, village music) live in the DontDestroyOnLoad scene.
    private bool BelongsToThisScene(AudioSource source)
    {
        return source != null && source.gameObject.scene == gameObject.scene;
    }
}
