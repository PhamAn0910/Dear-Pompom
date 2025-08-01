// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.InputSystem;

// public class Card : MonoBehaviour, IPointerClickHandler
// {
//     [SerializeField] private GameObject cardBack;
//     [SerializeField] private GameSceneController2 controller; // Keep this for inspector override if needed
//     [SerializeField] private SpriteRenderer cardFace;

//     private int _id;
//     public int Id => _id;

//     private void Awake() // Use Awake instead of Start
//     {
//         // Always try to find controller if not assigned
//         if (controller == null)
//         {
//             controller = FindFirstObjectByType<GameSceneController2>();
//             if (controller == null)
//             {
//                 Debug.LogError("GameSceneController not found in scene!");
//             }
//         }
//     }

//     public void SetCard(int id, Sprite image)
//     {
//         _id = id;
//         cardFace.sprite = image;
//     }

//     public void Unreveal()
//     {
//         cardBack.SetActive(true);
//     }
    
//     public void OnPointerClick(PointerEventData eventData)
//     {
//         Debug.Log("Card clicked via EventSystem!");
//         if (cardBack != null && cardBack.activeSelf && controller != null && controller.CanReveal)
//         {
//             Debug.Log("Revealing card!");
//             cardBack.SetActive(false);
//             controller.CardRevealed(this);
//         }
//     }
// }

using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject cardBack;
    [SerializeField] private SpriteRenderer cardBackRenderer; // Add reference to card back sprite renderer
    [SerializeField] private GameSceneController2 controller;
    [SerializeField] private SpriteRenderer cardFace;
    
    private int _id;
    private bool _isRevealed = false;

    public int Id => _id;
    public bool IsRevealed => _isRevealed;

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<GameSceneController2>();
            if (controller == null)
            {
                Debug.LogError("GameSceneController2 not found in scene!");
            }
        }
        
        // Auto-find card back renderer if not assigned
        if (cardBackRenderer == null && cardBack != null)
        {
            cardBackRenderer = cardBack.GetComponent<SpriteRenderer>();
        }
    }

    public void SetCard(int id, Sprite cardFaceImage, Sprite cardBackImage = null)
    {
        _id = id;
        if (cardFace != null)
        {
            cardFace.sprite = cardFaceImage;
            // Preserve original scale of card face
            cardFace.transform.localScale = Vector3.one;
        }
        
        // Set card back image if provided
        if (cardBackImage != null && cardBackRenderer != null)
        {
            cardBackRenderer.sprite = cardBackImage;
            // Preserve original scale of card back
            cardBackRenderer.transform.localScale = Vector3.one;
        }
        
        // Make sure card starts face down
        _isRevealed = false;
        if (cardBack != null)
        {
            cardBack.SetActive(true);
            // Ensure card back has correct scale
            cardBack.transform.localScale = Vector3.one;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isRevealed || controller == null || !controller.CanReveal)
            return;

        Reveal();
        controller.CardRevealed(this);
    }

    public void Reveal()
    {
        _isRevealed = true;
        if (cardBack != null)
        {
            cardBack.SetActive(false);
        }
    }

    public void Unreveal()
    {
        _isRevealed = false;
        if (cardBack != null)
        {
            cardBack.SetActive(true);
        }
    }
}