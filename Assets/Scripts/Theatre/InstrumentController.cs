using UnityEngine;

public class InstrumentController : MonoBehaviour
{
    public Sprite[] animationFrames; // Assign in Inspector
    public AudioSource audioSource;  // Assign in Inspector
    public float frameRate = 0.1f;   // Adjust animation speed
    public float dimAlpha = 0.5f;    // Dim when muted (optional)

    private SpriteRenderer spriteRenderer;
    private int currentFrame = 0;
    private bool isMuted = false;
    private float frameTimer = 0f;

    void Start()
    {
        // Get components (with error checks)
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on " + gameObject.name);
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (audioSource == null)
        {
            Debug.LogError("Assign an AudioSource to " + gameObject.name);
            enabled = false; // Disable script if critical components are missing
        }
        else
        {
            audioSource.playOnAwake = true;
            audioSource.loop = true;
        }

        // Set first frame
        if (animationFrames.Length > 0)
            spriteRenderer.sprite = animationFrames[0];
    }

    void Update()
    {
        if (!isMuted && animationFrames.Length > 0)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameRate)
            {
                frameTimer = 0f;
                currentFrame = (currentFrame + 1) % animationFrames.Length;
                spriteRenderer.sprite = animationFrames[currentFrame];
            }
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click
        {
            ToggleInstrument();
        }
    }

    public void ToggleInstrument()
    {
        isMuted = !isMuted;
        audioSource.mute = isMuted;

        // Dim sprite (optional)
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = isMuted ? dimAlpha : 1f;
            spriteRenderer.color = color;
        }
    }
}