using UnityEngine;

// Attach this to the Close BUTTON itself. Drag in the modal it should
// close. Does not auto-add any components — safe to attach anywhere.
public class ModalCloser : MonoBehaviour
{
    [Tooltip("Drag the modal panel (the object with ModalWindowScript) here.")]
    public ModalWindowScript modalToClose;

    [Tooltip("Optional — leave empty if you don't need back-button tracking.")]
    public BackButtonModalManager backButtonManager;

    public void CloseModal()
    {
        if (modalToClose == null)
        {
            Debug.LogWarning("[ModalCloser] No modal assigned!");
            return;
        }

        modalToClose.Hide();
        backButtonManager?.NotifyModalClosed(modalToClose);
    }
}