using UnityEngine;

public class TreeMovement : MonoBehaviour
{
    public float speed = 2f;
    public float destroyXPosition = -12f;
    public float verticalWobbleAmount = 0f; // goes up and down while moving
    public float wobbleFrequency = 1f;

    private float originalY;
    private float randomOffset;

    void Start()
    {
        originalY = transform.position.y;
        randomOffset = Random.Range(0f, 100f); // For unique wobble patterns
    }

    void Update()
    {
        // Move tree left
        transform.Translate(Vector3.left * speed * Time.deltaTime);
        
        // Add subtle vertical wobble
        float wobble = Mathf.Sin((Time.time + randomOffset) * wobbleFrequency) * verticalWobbleAmount;
        transform.position = new Vector3(transform.position.x, originalY + wobble, transform.position.z);
        
        // Destroy when off screen
        if (transform.position.x < destroyXPosition)
        {
            Destroy(gameObject);
        }
    }
}