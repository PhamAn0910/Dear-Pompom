using UnityEngine;

public class CardDebug : MonoBehaviour
{
    void Start()
    {
        // Check all components
        Debug.Log($"=== Card Debug for {gameObject.name} ===");
        
        // Check colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        Debug.Log($"Number of Collider2D components: {colliders.Length}");
        
        foreach (var col in colliders)
        {
            Debug.Log($"Collider: {col.GetType().Name}, Enabled: {col.enabled}, IsTrigger: {col.isTrigger}");
            if (col is BoxCollider2D box)
            {
                Debug.Log($"BoxCollider2D Size: {box.size}, Offset: {box.offset}");
            }
        }
        
        // Check if there's a Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"Rigidbody2D: Kinematic={rb.bodyType == RigidbodyType2D.Kinematic}, BodyType={rb.bodyType}");
        }
        else
        {
            Debug.Log("No Rigidbody2D found");
        }
        
        // Check layer
        Debug.Log($"GameObject Layer: {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer})");
        
        // Check position
        Debug.Log($"Position: {transform.position}");
        
        // Check if sprite renderer exists and has sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Debug.Log($"SpriteRenderer: Sprite={sr.sprite?.name}, Enabled={sr.enabled}");
        }
    }
    
    // Test mouse events
    void OnMouseEnter()
    {
        Debug.Log($"MOUSE ENTER: {gameObject.name}");
    }
    
    void OnMouseExit()
    {
        Debug.Log($"MOUSE EXIT: {gameObject.name}");
    }
    
    void OnMouseDown()
    {
        Debug.Log($"MOUSE DOWN: {gameObject.name}");
    }
    
    void OnMouseUp()
    {
        Debug.Log($"MOUSE UP: {gameObject.name}");
    }
}