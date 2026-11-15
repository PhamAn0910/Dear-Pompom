using DearPompom;
using UnityEngine;
using UnityEngine.UI;

public class EntryButtonSound : MonoBehaviour
{
    void Start()
    {
        // Play by id: works even if this scene has no SoundEffects object.
        GetComponent<Button>().onClick.AddListener(() => Services.Audio.PlaySfx(SoundEffects.ClickId));
    }
}
