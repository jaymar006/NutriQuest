using UnityEngine;

public class ModalOpener : MonoBehaviour
{
    [Tooltip("Drag the modal panel (the object with ModalWindowScript) here.")]
    public ModalWindowScript modalToOpen;

    [Tooltip("Optional — leave empty if you don't need back-button tracking.")]
    public BackButtonModalManager backButtonManager;

    public void OpenModal()
    {
        if (modalToOpen == null)
        {
            Debug.LogWarning("[ModalOpener] No modal assigned!");
            return;
        }

        modalToOpen.Show();
        backButtonManager?.NotifyModalOpened(modalToOpen);
    }
}