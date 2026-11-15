using DearPompom;
using UnityEngine;

/// <summary>
/// 1.0 click-sound helper, present in every scene. Now routes through AudioManager so the
/// SFX slider and mute apply. New code calls Services.Audio.PlaySfx directly.
/// </summary>
public class SoundEffects : MonoBehaviour
{
    public const string ClickId = "ui.click";

    public static SoundEffects Instance;

    public AudioSource src;
    public AudioClip clickSound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Services.EnsureBooted();
        Services.Audio.RegisterSfx(ClickId, clickSound);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayClick()
    {
        Services.Audio.PlaySfx(clickSound);
    }
}
