using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Add this

public class BackButtonModalManager : MonoBehaviour
{
    [Header("Modal Queue (drag to reorder open priority)")]
    public List<ModalWindowScript> modalQueue = new List<ModalWindowScript>();
    private ModalWindowScript _currentOpenModal;

    void Update()
    {
        // New Input System equivalent of Input.GetKeyDown(KeyCode.Escape)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleBackButton();
    }

    void HandleBackButton()
    {
        if (_currentOpenModal != null)
        {
            _currentOpenModal.Hide();
            _currentOpenModal = null;
            return;
        }

        foreach (var modal in modalQueue)
        {
            if (modal != null)
            {
                modal.Show();
                _currentOpenModal = modal;
                return;
            }
        }
    }

    public void NotifyModalClosed(ModalWindowScript modal)
    {
        if (_currentOpenModal == modal)
            _currentOpenModal = null;
    }

    public void NotifyModalOpened(ModalWindowScript modal)
    {
        _currentOpenModal = modal;
    }
}