using UnityEngine;

public class ModalOnAndOff : MonoBehaviour
{
    public ModalWindowScript modal;

    // Drag your BackButtonModalManager GameObject here in the Inspector
    public BackButtonModalManager backButtonManager;

    public void OpenModal()
    {
        if (modal == null) return;
        modal.Show();
        backButtonManager?.NotifyModalOpened(modal);
    }

    public void CloseModal()
    {
        if (modal == null) return;
        modal.Hide();
        backButtonManager?.NotifyModalClosed(modal);
    }
}