using UnityEngine;

public class InstrumentAnimator : MonoBehaviour
{
    public AudioSource audioSource;  // Assign in Inspector
    public float dimAlpha = 0.5f;   // Dim opacity when muted (optional)
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isMuted = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
    }

    void OnMouseDown()
    {
        ToggleInstrument();
    }

    public void ToggleInstrument()
    {
        isMuted = !isMuted;
        audioSource.mute = isMuted;

        // Freeze/unfreeze animation
        animator.enabled = !isMuted;

        // Dim sprite (optional)
        Color color = spriteRenderer.color;
        color.a = isMuted ? dimAlpha : 1f;
        spriteRenderer.color = color;
    }
}