using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RenderMap : MonoBehaviour
{
    public Button myButton; // Drag your button here in Inspector
    
    void Start()
    {
        // Connect button click to scene loading
        myButton.onClick.AddListener(LoadSpecificScene);
    }
    
    public void LoadSpecificScene()
    {
        SceneManager.LoadScene("Map"); // Replace with your actual scene name
    }
}
