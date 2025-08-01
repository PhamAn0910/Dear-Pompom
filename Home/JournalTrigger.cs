using UnityEngine;
using UnityEngine.InputSystem; // Add this namespace

public class JournalTrigger : MonoBehaviour
{
    public JournalUIManager journalUI;
    private Keyboard keyboard; // Reference to keyboard input

    void Start()
    {
        keyboard = Keyboard.current; // Initialize keyboard
    }

    void Update()
    {
        // Use new Input System's key detection
        if (keyboard.jKey.wasPressedThisFrame)
        {
            journalUI.ToggleNotebook(!journalUI.notebookPanel.activeSelf);
        }
    }
}