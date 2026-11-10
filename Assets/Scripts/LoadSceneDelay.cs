using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class LoadSceneDelay : MonoBehaviour
{
    public string sceneName;
    public SoundEffects soundEffects;

    public void OnButtonClick()
    {
        soundEffects.PlayClick();
        StartCoroutine(LoadSceneAfterDelay(0.1f));
    }

    IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}