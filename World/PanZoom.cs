using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PanZoom : MonoBehaviour {
    Vector3 touchStart;
    public float zoomOutMin = 1f;
    public float zoomOutMax = 8f;
    public SpriteRenderer background;
    public float panSpeed = 1f;
    
    // Use this for initialization
    void Start () {
        
    }
    
    // Update is called once per frame
    void Update () {
        // Check if Mouse is available
        if (Mouse.current == null || background == null) return;
        
        Camera cam = Camera.main;
        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        Bounds bgBounds = background.bounds;
        
        if(Mouse.current.leftButton.wasPressedThisFrame){
            Vector2 mousePos = Mouse.current.position.ReadValue();
            touchStart = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
        }
        if(Mouse.current.leftButton.isPressed){
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 currentMouseWorld = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
            Vector3 direction = touchStart - currentMouseWorld;
            Vector3 newPosition = cam.transform.position + direction * panSpeed;
            // Clamp camera position to background bounds
            float minX = bgBounds.min.x + camWidth / 2f;
            float maxX = bgBounds.max.x - camWidth / 2f;
            float minY = bgBounds.min.y + camHeight / 2f;
            float maxY = bgBounds.max.y - camHeight / 2f;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            newPosition.z = cam.transform.position.z;
            cam.transform.position = newPosition;
        }
    }
}