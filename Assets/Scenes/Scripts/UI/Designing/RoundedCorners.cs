using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Image))]
public class RoundedCorners : MonoBehaviour
{
    [Range(0f, 0.5f)]
    public float radius = 0.15f;

    [SerializeField] private Color panelColor = Color.white;

    private Image image;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void Apply()
    {
        image = GetComponent<Image>();
        if (image == null) return;

        image.sprite = CreateRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = panelColor;
    }

    Sprite CreateRoundedSprite()
    {
        int size = 512;
        int r = Mathf.RoundToInt(size * radius);

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = GetSmoothedAlpha(x, y, size, size, r);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        float border = r;
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border)
        );

        return sprite;
    }

    float GetSmoothedAlpha(int x, int y, int width, int height, int r)
    {
        // Fully inside the safe center area
        bool inCenterX = x >= r && x <= width - r;
        bool inCenterY = y >= r && y <= height - r;

        if (inCenterX || inCenterY) return 1f;

        // We are in a corner region — calculate distance from corner circle center
        int cx = (x < r) ? r : width - r;
        int cy = (y < r) ? r : height - r;

        float dx = x - cx;
        float dy = y - cy;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);

        // Smooth the edge with a 1.5px feather for anti-aliasing
        float feather = 1.5f;
        return 1f - Mathf.Clamp01((dist - r + feather) / feather);
    }
}