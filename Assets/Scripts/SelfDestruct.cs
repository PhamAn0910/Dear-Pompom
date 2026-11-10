using UnityEngine;
using System.Collections;

public class SelfDestruct : MonoBehaviour
{
    public float lifetime = 3f;       // Total time before destruction
    public float fadeDuration = 1f;   // How long the fade-out lasts
    private SpriteRenderer spriteRenderer;
    private Color initialColor;       // Stores the starting color (including alpha)

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            initialColor = spriteRenderer.color; // Save the original color (including transparency)
            StartCoroutine(FadeOut());
        }
        else
        {
            // If no SpriteRenderer, just destroy after lifetime
            Destroy(gameObject, lifetime);
        }
    }

    IEnumerator FadeOut()
    {
        // Wait until it's time to start fading
        yield return new WaitForSeconds(lifetime - fadeDuration);
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // Fade from initial alpha down to 0
            float newAlpha = Mathf.Lerp(initialColor.a, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, newAlpha);
            yield return null;
        }
        
        Destroy(gameObject);
    }
}