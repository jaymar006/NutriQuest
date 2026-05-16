using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.CutsceneManager
{
    public class CharacterVN : MonoBehaviour
    {
        [Header("Character Info")]
        public string characterName;

        [Header("Portrait")]
        public Image portraitImage;
        public Vector2 portraitSize = new Vector2(200f, 200f);

        [Header("Dim Settings")]
        public float activeBrightness = 1f;
        public float inactiveBrightness = 0.4f;

        [Header("Pop Out Settings")]
        public Vector2 popOutOffset = new Vector2(20f, 10f);
        public float popDirection = 1f;

        private Vector2 originalPosition;
        private RectTransform portraitRect;

        private void Awake()
        {
            if (portraitImage != null)
            {
                portraitRect = portraitImage.GetComponent<RectTransform>();
                if (portraitRect != null)
                    originalPosition = portraitRect.anchoredPosition;
            }

            ApplyPortraitSize();
        }

        private void ApplyPortraitSize()
        {
            if (portraitRect == null && portraitImage != null)
                portraitRect = portraitImage.GetComponent<RectTransform>();

            if (portraitRect == null)
                return;

            portraitRect.sizeDelta = portraitSize;
            portraitImage.preserveAspect = true;
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null)
                return;

            if (sprite != null)
            {
                portraitImage.sprite = sprite;
            }

            ApplyPortraitSize();
        }

        public void SetActive(bool isActive)
        {
            if (portraitImage == null)
                return;

            float brightness = isActive ? activeBrightness : inactiveBrightness;
            Color c = portraitImage.color;
            portraitImage.color = new Color(brightness, brightness, brightness, c.a);
        }

        public void PopOut()
        {
            if (portraitRect == null)
                return;

            portraitRect.anchoredPosition = originalPosition + new Vector2(popOutOffset.x * popDirection, popOutOffset.y);
            portraitRect.SetAsLastSibling();
        }

        public void ResetPopOut()
        {
            if (portraitRect == null)
                return;

            portraitRect.anchoredPosition = originalPosition;
        }

        public void ApplyCustomSize(Vector2 size)
        {
            portraitSize = size;
            ApplyPortraitSize();
        }

        public bool IsUsingFallbackName()
        {
            return string.IsNullOrEmpty(characterName);
        }
    }
}