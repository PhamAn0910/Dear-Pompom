using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class RenderPlace : MonoBehaviour
{
    public string sceneName; // Name of the scene to load
    
    // This function is called when the button is clicked
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not specified!");
        }
    }
}
