using UnityEngine;

public class EndlessScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 2f; // Speed at which the background moves left
    public float backgroundWidth; // Width of a single background sprite (set in Inspector or auto-detect)

    private Transform[] backgrounds; // Array of background transforms (at least 2 for seamless looping)
    private float resetPositionX; // X position where a background resets to the right

    void Start()
    {
        // Auto-detect background width if not set
        if (backgroundWidth <= 0)
        {
            SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                backgroundWidth = spriteRenderer.bounds.size.x;
            }
            else
            {
                Debug.LogError("No SpriteRenderer found! Set backgroundWidth manually.");
            }
        }

        // Get all child backgrounds (minimum 2 for seamless scrolling)
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            backgrounds[i] = transform.GetChild(i);
        }

        // Set initial positions (side by side)
        for (int i = 0; i < backgrounds.Length; i++)
        {
            backgrounds[i].position = new Vector3(backgroundWidth * i, 0, 0);
        }

        // Calculate reset position (when a background goes fully off-screen)
        resetPositionX = -backgroundWidth;
    }

    void Update()
    {
        // Move all backgrounds left
        foreach (Transform bg in backgrounds)
        {
            bg.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

            // If a background is fully off-screen, move it to the right
            if (bg.position.x < resetPositionX)
            {
                Vector3 newPos = bg.position;
                newPos.x += backgroundWidth * backgrounds.Length; // Move to the right of the last bg
                bg.position = newPos;
            }
        }
    }
}