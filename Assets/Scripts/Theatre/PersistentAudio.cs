using DearPompom;
using UnityEngine;

/// <summary>
/// The 1.0 background-music prefab (Music.prefab). Still owns its AudioSource, but hands the
/// volume to AudioManager as background music, so the Music slider and theatre ducking reach it.
/// </summary>
public class PersistentAudio : MonoBehaviour
{
    private static PersistentAudio instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Services.EnsureBooted();
        foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
            Services.Audio.Register(source, AudioChannel.Music, true);
    }
}
