using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScrollingUIBackground : MonoBehaviour
{
    public float speed; // Speed Of Scroll
    public Vector2 direction; // Normalized Direction Of Scroll

    Image img;

    private void Start()
    {
        img = GetComponent<Image>();
    }

    private void Update()
    {
        img.material.mainTextureOffset += direction.normalized * Time.deltaTime * speed;
    }
}
