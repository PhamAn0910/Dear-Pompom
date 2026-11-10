using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;
    [SerializeField] Animator transitionAnim;
    [SerializeField] float transitionTime = 1f; // Duration of the animation

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // void Start()
    // {
    //     // Assuming you have a fade-in animation
    //     GetComponent<Animator>().SetTrigger("FadeIn");
    // }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLevel();
        }
    }

    public void NextLevel()
    {
        StartCoroutine(LoadLevel());
    }
    
    IEnumerator LoadLevel()
    {
        //transitionAnim.SetTrigger("Start");
        
        // Wait for fade out to complete
        yield return new WaitForSeconds(transitionTime);
        
        // Load the scene (this happens during black screen)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}