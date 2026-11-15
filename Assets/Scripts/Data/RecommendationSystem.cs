using DearPompom;
using DearPompom.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows Pompom's letters in the 1.0 popup panel. This class only PRESENTS: which letter to
/// send is decided by the core RecommendationEngine (via Services.Recommendations).
///
/// Flow: an activity is recorded -> RecommendationReady arrives -> the letter waits ->
/// one second after the next scene loads it is shown and marked as shown.
/// Phase 2 replaces this popup with the mailbox (PRD C1, R3); the service stays the same.
/// </summary>
public class RecommendationSystem : MonoBehaviour
{
    // Assigned in the StartMenu scene (objects on the persistent UI canvas).
    public GameObject recommendationUIPanel;
    public TextMeshProUGUI recommendationText;

    private const float ShowDelaySeconds = 1f;
    private const string WelcomeText = "Welcome to the village of Pompom!";

    private static RecommendationSystem _instance;

    private Recommendation _pending;   // decided, waiting for the next scene
    private Recommendation _showing;   // on screen right now
    private string _previousSceneName = "";
    private bool _subscribed;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // StartMenu was loaded again; the first copy is still alive.
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        if (_instance != this) return;

        DontDestroyOnLoad(gameObject);
        if (recommendationUIPanel != null) recommendationUIPanel.SetActive(false);

        _previousSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Services.EnsureBooted();
        Services.Events.Subscribe<RecommendationReady>(OnRecommendationReady);
        _subscribed = true;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (!_subscribed) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Services.Events != null) Services.Events.Unsubscribe<RecommendationReady>(OnRecommendationReady);
    }

    private void OnRecommendationReady(RecommendationReady message)
    {
        // A newer suggestion replaces one that was never shown.
        _pending = message.Recommendation;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CancelInvoke();
        if (_previousSceneName == "StartMenu" && scene.name == "World")
            Invoke(nameof(ShowWelcomeMessage), ShowDelaySeconds);
        else if (_pending != null)
            Invoke(nameof(ShowPendingRecommendation), ShowDelaySeconds);

        _previousSceneName = scene.name;
    }

    private void ShowWelcomeMessage()
    {
        _showing = null;
        Display(WelcomeText);
    }

    private void ShowPendingRecommendation()
    {
        if (_pending == null) return;
        _showing = _pending;
        _pending = null;
        if (Display(_showing.Text)) Services.Recommendations.MarkShown(_showing);
        else _showing = null;
    }

    private bool Display(string message)
    {
        if (recommendationUIPanel == null || recommendationText == null) return false;
        recommendationText.text = message;
        recommendationUIPanel.SetActive(true);
        return true;
    }

    public void HideRecommendationUI()
    {
        if (recommendationUIPanel != null) recommendationUIPanel.SetActive(false);
    }

    /// <summary>Wired to the panel's close button in the StartMenu scene.</summary>
    public void OnCloseRecommendationClicked()
    {
        if (_showing != null) Services.Recommendations.MarkDismissed(_showing);
        _showing = null;
        HideRecommendationUI();
    }

    /// <summary>For a future "Go now" button: follows the letter to its activity's scene.</summary>
    public void OnGoNowClicked()
    {
        Recommendation followed = _showing;
        _showing = null;
        HideRecommendationUI();
        if (followed == null) return;
        Services.Recommendations.MarkAccepted(followed);
        if (followed.Target != ActivityType.Unknown) SceneManager.LoadScene(ActivityTypes.SceneName(followed.Target));
    }
}
