using UnityEngine;

public class PersistentCanvasManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("PersistentUIMainCanvas will persist across scenes.");
    }
}