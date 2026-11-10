using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PanImage : MonoBehaviour
{
    public Image imageToPan;
    public float panSpeed = 1f;
    private Vector2 lastMouseScreenPos;
    private RectTransform imageRect;
    private RectTransform parentRect;
    private bool isDragging = false;
    private Canvas canvas;

    void Start()
    {
        if (imageToPan != null)
        {
            imageRect = imageToPan.rectTransform;
            parentRect = imageRect.parent as RectTransform;
            canvas = imageToPan.canvas;
        }
    }

    void Update()
    {
        if (Mouse.current == null || imageToPan == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            lastMouseScreenPos = Mouse.current.position.ReadValue();
            isDragging = true;
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            Vector2 currentMouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 screenDelta = currentMouseScreenPos - lastMouseScreenPos;
            lastMouseScreenPos = currentMouseScreenPos;

            // Convert screen delta to canvas delta
            Vector2 canvasDelta = screenDelta / canvas.scaleFactor * panSpeed;

            Vector2 newPos = imageRect.anchoredPosition + canvasDelta;
            imageRect.anchoredPosition = ClampToParent(newPos);
        }
    }

    Vector2 ClampToParent(Vector2 pos)
    {
        if (parentRect == null || imageRect == null) return pos;
        
        // Calculate the bounds for the image to stay within parent
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;
        float imageWidth = imageRect.rect.width;
        float imageHeight = imageRect.rect.height;
        
        // If image is smaller than parent, center it and don't allow movement
        if (imageWidth <= parentWidth && imageHeight <= parentHeight)
        {
            return Vector2.zero;
        }
        
        // Calculate movement limits
        float maxX = (imageWidth - parentWidth) * 0.5f;
        float maxY = (imageHeight - parentHeight) * 0.5f;
        
        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        
        return pos;
    }
}
