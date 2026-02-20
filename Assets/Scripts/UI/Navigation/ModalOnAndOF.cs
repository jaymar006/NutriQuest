using UnityEngine;

public class ModalOnAndOF : MonoBehaviour
{
    public ModalWindowScript modal;

    public void OpenModal()
    {
        modal.Show();
    }

    public void CloseModal()
    {
        modal.Hide();
    }
}