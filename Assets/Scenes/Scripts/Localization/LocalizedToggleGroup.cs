using UnityEngine;

// Attach to something that stays active (e.g. Canvas). Drag in every
// LocalizedElement whose GameObject should be shown/hidden based on the
// current language — e.g. EnglishArchive / TagalogArchive.
public class LocalizedGroupToggle : MonoBehaviour
{
    [SerializeField] private LocalizedElement[] elements;

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        if (LocalizationManager.Instance == null) return;

        bool isFilipino = LocalizationManager.Instance.IsFilipino;

        foreach (var element in elements)
        {
            if (element == null) continue;
            bool shouldBeActive = (element.Language == ContentLanguage.Filipino) == isFilipino;
            element.gameObject.SetActive(shouldBeActive);
        }
    }
}