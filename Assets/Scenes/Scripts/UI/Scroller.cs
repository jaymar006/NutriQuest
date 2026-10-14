using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour 
{
    [SerializeField] private RawImage _img;
    [SerializeField] private float _x, _y;

    private Vector2 _scroll;
    private Rect _rect;

    void Awake()
    {
        _scroll = new Vector2(_x, _y);
        if (_img != null)
            _rect = _img.uvRect;
    }

    void OnEnable()
    {
        // Re-cache in case inspector values changed while disabled //
        _scroll = new Vector2(_x, _y);
        if (_img != null)
            _rect = _img.uvRect;
    }

    void Update()
    {
        if (_img == null || !_img.isActiveAndEnabled) return;
        if (_x == 0f && _y == 0f) return;
        if (!gameObject.activeInHierarchy) return;

        _rect.position += _scroll * Time.deltaTime;
        _img.uvRect = _rect;
    }
}
