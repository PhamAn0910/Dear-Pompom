using UnityEngine;

public class EndlessScrollingObject : MonoBehaviour
{
    public float scrollSpeed = 2f;         // Scrolling speed
    public float backgroundWidth;          // Width of a single background sprite
    public float overlapAmount = 0.1f;     // How much the backgrounds should overlap (0-1)

    private Transform[] backgrounds;
    private float resetPositionX;
    private float[] initialYPositions;
    private float effectiveWidth;          // Width after accounting for overlap

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

        // Calculate effective width (accounting for overlap)
        effectiveWidth = backgroundWidth * (1f - overlapAmount);

        // Get all child backgrounds
        backgrounds = new Transform[transform.childCount];
        initialYPositions = new float[transform.childCount];
        
        for (int i = 0; i < transform.childCount; i++)
        {
            backgrounds[i] = transform.GetChild(i);
            initialYPositions[i] = backgrounds[i].position.y;
        }

        // Arrange them horizontally with overlap
        for (int i = 0; i < backgrounds.Length; i++)
        {
            backgrounds[i].position = new Vector3(
                effectiveWidth * i, 
                initialYPositions[i], 
                backgrounds[i].position.z);
        }

        // Calculate reset position (when a background goes fully off-screen left)
        resetPositionX = -effectiveWidth;
    }

    void Update()
    {
        // Move all backgrounds left
        for (int i = 0; i < backgrounds.Length; i++)
        {
            Vector3 newPosition = backgrounds[i].position;
            newPosition.x -= scrollSpeed * Time.deltaTime;
            backgrounds[i].position = newPosition;

            // If a background is fully off-screen left, move it to the right
            if (backgrounds[i].position.x < resetPositionX)
            {
                // Find which background is currently farthest right
                float farthestRight = backgrounds[0].position.x;
                for (int j = 1; j < backgrounds.Length; j++)
                {
                    if (backgrounds[j].position.x > farthestRight)
                    {
                        farthestRight = backgrounds[j].position.x;
                    }
                }
                
                // Move this background to the right of the farthest right one
                backgrounds[i].position = new Vector3(
                    farthestRight + effectiveWidth,
                    initialYPositions[i],
                    backgrounds[i].position.z);
            }
        }
    }
}