using UnityEngine;
using UnityEngine.UI;

public class OptionButton : MonoBehaviour
{
    public int optionIndex;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        MathGameManager.Instance.OnOptionSelected(optionIndex);
    }
}