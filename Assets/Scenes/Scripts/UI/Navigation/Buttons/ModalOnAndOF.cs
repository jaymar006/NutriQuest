using UnityEngine;

public class ModalOnAndOF : MonoBehaviour
{
    public ModalWindowScript modal;

    public void OpenModal()
    {
        if (modal == null) return;
        modal.Show();
    }

    public void CloseModal()
    {
        if (modal == null) return;
        modal.Hide();
    }
}