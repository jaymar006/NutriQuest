using UnityEngine;

public class ModalOnAndOff : MonoBehaviour
{
    [Header("Assign BOTH language versions of this modal")]
    public ModalWindowScript englishModal;
    public ModalWindowScript filipinoModal;

    public BackButtonModalManager backButtonManager;

    // Tracks whichever one is actually open, so CloseModal() closes the
    // right one even if language was switched while it was open.
    private ModalWindowScript activeModal;

    public void OpenModal()
    {
        ModalWindowScript target = GetModalForCurrentLanguage();
        if (target == null) return;

        target.Show();
        activeModal = target;
        backButtonManager?.NotifyModalOpened(target);
    }

    public void CloseModal()
    {
        if (activeModal == null) return;

        activeModal.Hide();
        backButtonManager?.NotifyModalClosed(activeModal);
        activeModal = null;
    }

    private ModalWindowScript GetModalForCurrentLanguage()
    {
        bool isFilipino = LocalizationManager.Instance != null &&
                           LocalizationManager.Instance.IsFilipino;

        return isFilipino ? filipinoModal : englishModal;
    }
}