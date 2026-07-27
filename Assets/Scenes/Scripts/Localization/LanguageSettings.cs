using UnityEngine;
using TMPro;

public class LanguageSettings : MonoBehaviour
{
    public TMP_Dropdown languageDropdown;

    private void Start()
    {
        bool isFilipino = LocalizationManager.Instance != null &&
                          LocalizationManager.Instance.IsFilipino;

        languageDropdown.value = isFilipino ? 1 : 0;
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    private void OnLanguageChanged(int index)
    {
        if (LocalizationManager.Instance == null) return;

        if (index == 1) LocalizationManager.Instance.SetFilipino();
        else LocalizationManager.Instance.SetEnglish();
    }

    private void OnDestroy()
    {
        languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
    }
}