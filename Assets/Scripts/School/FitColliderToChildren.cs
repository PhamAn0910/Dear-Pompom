using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FitColliderToChildren : MonoBehaviour
{
    [Header("Child Objects to Include")]
    [Tooltip("Drag child objects here that should be included in the collider")]
    public Transform[] childrenToInclude;

    private BoxCollider2D boxCollider;
    
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Auto-populate children if none are assigned
        if (childrenToInclude == null || childrenToInclude.Length == 0)
        {
            AutoPopulateChildren();
        }
        
        FitCollider();
    }

    void OnValidate()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
            
        if (boxCollider != null && childrenToInclude != null && childrenToInclude.Length > 0)
            FitCollider();
    }

    public void FitCollider()
    {
        if (childrenToInclude == null || childrenToInclude.Length == 0)
        {
            Debug.LogWarning("No children assigned to include in collider calculation", this);
            return;
        }
        
        // Get first child bounds to start with
        Bounds bounds = GetChildBounds(childrenToInclude[0]);
        
        // Include all other specified children
        for (int i = 1; i < childrenToInclude.Length; i++)
        {
            if (childrenToInclude[i] != null)
            {
                bounds.Encapsulate(GetChildBounds(childrenToInclude[i]));
            }
        }
        
        // Adjust collider
        boxCollider.offset = bounds.center - transform.position;
        boxCollider.size = bounds.size;
    }

    private Bounds GetChildBounds(Transform child)
    {
        Renderer renderer = child.GetComponent<Renderer>();
        Collider2D childCollider = child.GetComponent<Collider2D>();
        
        if (renderer != null)
        {
            return renderer.bounds;
        }
        else if (childCollider != null)
        {
            return childCollider.bounds;
        }
        
        // If no renderer or collider, just use the position
        return new Bounds(child.position, Vector3.zero);
    }

    private void AutoPopulateChildren()
    {
        // Get all direct children that have renderers or colliders
        Transform[] children = new Transform[transform.childCount];
        int validChildCount = 0;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Only include children that have renderers or colliders
            if (child.GetComponent<Renderer>() != null || child.GetComponent<Collider2D>() != null)
            {
                children[validChildCount] = child;
                validChildCount++;
            }
        }
        
        // Resize array to actual count
        if (validChildCount > 0)
        {
            childrenToInclude = new Transform[validChildCount];
            System.Array.Copy(children, childrenToInclude, validChildCount);
            Debug.Log($"Auto-populated {validChildCount} children for collider calculation", this);
        }
    }
}