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
    private Texture2D _generatedTexture;
    private Sprite _generatedSprite;
    private float _lastRadius = -1f;
    private Color _lastColor = new Color(-1f, -1f, -1f, -1f);

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        Apply();
    }

    void OnDisable()
    {
        // Free GPU memory when disabled; Apply() regenerates on re-enable.
        // While still enabled, Apply() early-outs via cached params below.
        DestroyGenerated();
    }

    void OnDestroy()
    {
        DestroyGenerated();
    }

    void Apply()
    {
        image = GetComponent<Image>();
        if (image == null) return;

        // Early-out: reuse cached sprite when parameters are unchanged.
        if (_generatedSprite != null &&
            Mathf.Approximately(_lastRadius, radius) &&
            _lastColor == panelColor)
        {
            image.sprite = _generatedSprite;
            image.type = Image.Type.Sliced;
            image.color = panelColor;
            return;
        }

        DestroyGenerated();

        _generatedTexture = CreateRoundedTexture();
        float border = Mathf.RoundToInt(512 * radius);
        _generatedSprite = Sprite.Create(
            _generatedTexture,
            new Rect(0, 0, 512, 512),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border)
        );

        _lastRadius = radius;
        _lastColor = panelColor;

        image.sprite = _generatedSprite;
        image.type = Image.Type.Sliced;
        image.color = panelColor;
    }

    void DestroyGenerated()
    {
        if (_generatedSprite != null)
        {
            if (image != null && image.sprite == _generatedSprite)
                image.sprite = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_generatedSprite);
            else
                Destroy(_generatedSprite);
#else
            Destroy(_generatedSprite);
#endif
            _generatedSprite = null;
        }
        if (_generatedTexture != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_generatedTexture);
            else
                Destroy(_generatedTexture);
#else
            Destroy(_generatedTexture);
#endif
            _generatedTexture = null;
        }
    }

    Sprite CreateRoundedSprite()
    {
        Texture2D tex = CreateRoundedTexture();

        float border = Mathf.RoundToInt(512 * radius);
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 512, 512),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border)
        );

        return sprite;
    }

    Texture2D CreateRoundedTexture()
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

        return tex;
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
