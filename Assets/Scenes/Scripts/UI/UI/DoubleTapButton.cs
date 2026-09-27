using UnityEngine;
using UnityEngine.UI;

public class DoubleTapButton : MonoBehaviour
{
    [SerializeField] private float doubleTapTime = 0.3f;

    private Button button;
    private float lastTapTime = -1f;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        float currentTime = Time.time;

        if (currentTime - lastTapTime <= doubleTapTime)
        {
            // Double tap detected
            DoubleTap();

            lastTapTime = -1f;
        }
        else
        {
            // First tap
            lastTapTime = currentTime;
        }
    }

    private void DoubleTap()
    {
        Debug.Log("Double tap detected!");

        // Put whatever you want the button to do here.
    }
}