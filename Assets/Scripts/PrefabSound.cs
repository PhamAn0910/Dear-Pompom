using UnityEngine;
using UnityEngine.UI;

public class EntryButtonSound : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            SoundEffects.Instance.PlayClick();
        });
    }
}
