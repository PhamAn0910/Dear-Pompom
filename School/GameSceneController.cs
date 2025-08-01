using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public class GameSceneController : MonoBehaviour
{
    [SerializeField] private Sprite[] images;
    [SerializeField] private Card originalCard;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    public float OFFSET_X = 3f;
    public float OFFSET_Y = 4f;

    private const int GRID_ROWS = 2;
    private const int GRID_COLS = 4;
    // private const float OFFSET_X = 2f;
    // private const float OFFSET_Y = 2.5f;

    private Card _firstRevealed;
    private Card _secondRevealed;
    private int _score = 0;

    public bool CanReveal => _secondRevealed == null;

    void Start()
    {
        Vector3 startPos = originalCard.transform.position;

        int[] numbers = { 0, 0, 1, 1, 2, 2, 3, 3 };
        numbers = ShuffleArray(numbers);

        for (int i = 0; i < GRID_COLS; i++)
        {
            for (int j = 0; j < GRID_ROWS; j++)
            {
                Card card;
                if (i == 0 && j == 0)
                {
                    card = originalCard;
                }
                else
                {
                    card = Instantiate(originalCard) as Card;
                }

                int index = j * GRID_COLS + i;
                int id = numbers[index];
                card.SetCard(id, images[id]);

                float posX = (OFFSET_X * i) + startPos.x;
                float posY = -(OFFSET_Y * j) + startPos.y;
                card.transform.position = new Vector3(posX, posY, startPos.z);
            }
        }
    }

    int[] ShuffleArray(int[] numbers)
    {
        int[] newArray = numbers.Clone() as int[];
        for (int i = 0; i < newArray.Length; i++)
        {
            int tmp = newArray[i];
            int r = Random.Range(i, newArray.Length);
            newArray[i] = newArray[r];
            newArray[r] = tmp;
        }
        return newArray;
    }

    public void CardRevealed(Card card)
    {
        if (_firstRevealed == null)
        {
            _firstRevealed = card;
        }
        else
        {
            _secondRevealed = card;
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        if (_firstRevealed.Id == _secondRevealed.Id)
        {
            _score++;
            scoreLabel.text = "Score: " + _score;
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            _firstRevealed.Unreveal();
            _secondRevealed.Unreveal();
        }

        _firstRevealed = null;
        _secondRevealed = null;
    }
    
    // public class EventSystemDebug : MonoBehaviour
    // {
    //     void Start()
    //     {
    //         Debug.Log("=== EventSystem Debug ===");
            
    //         // Check if EventSystem exists
    //         EventSystem eventSystem = EventSystem.current;
    //         if (eventSystem != null)
    //         {
    //             Debug.Log($"EventSystem found: {eventSystem.gameObject.name}");
    //             Debug.Log($"EventSystem enabled: {eventSystem.enabled}");
    //             Debug.Log($"Current selected object: {eventSystem.currentSelectedGameObject?.name ?? "None"}");
    //         }
    //         else
    //         {
    //             Debug.LogError("No EventSystem found in scene!");
    //         }
            
    //         // Check for Input System UI Input Module
    //         var inputModule = FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    //         if (inputModule != null)
    //         {
    //             Debug.Log($"InputSystemUIInputModule found: {inputModule.gameObject.name}");
    //             Debug.Log($"InputSystemUIInputModule enabled: {inputModule.enabled}");
    //         }
    //         else
    //         {
    //             Debug.LogWarning("No InputSystemUIInputModule found!");
    //         }
            
    //         // Check for Physics2D Raycaster
    //         var raycaster = FindFirstObjectByType<Physics2DRaycaster>();
    //         if (raycaster != null)
    //         {
    //             Debug.Log($"Physics2DRaycaster found on: {raycaster.gameObject.name}");
    //             Debug.Log($"Physics2DRaycaster enabled: {raycaster.enabled}");
    //         }
    //         else
    //         {
    //             Debug.LogWarning("No Physics2DRaycaster found!");
    //         }
            
    //         // Check main camera
    //         Camera mainCam = Camera.main;
    //         if (mainCam != null)
    //         {
    //             Debug.Log($"Main Camera: {mainCam.gameObject.name}");
    //             Debug.Log($"Camera position: {mainCam.transform.position}");
    //             Debug.Log($"Camera projection: {mainCam.orthographic}");
    //         }
    //     }
    // }
}