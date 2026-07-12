using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using Gameplay.CutsceneManager;

public class NameInputScreen : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public Button confirmButton;
    public GameObject panelRoot;

    [Header("Config")]
    public string defaultName = "Player";
    public int maxNameLength = 20;

    [Header("Soft-lock Prevention")]
    [Tooltip("If true, clicking outside the panel is ignored")]
    public bool blockOutsideClicks = true;

    public Action<string> OnNameConfirmed;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (nameInputField != null)
            nameInputField.characterLimit = maxNameLength;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    public void Show()
    {
        Debug.Log("[NameInputScreen] Show() method called");

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            Debug.Log("[NameInputScreen] Panel activated successfully");
        }
        else
        {
            Debug.LogError("[NameInputScreen] panelRoot is null! Assign it in the Inspector.");
        }

        if (nameInputField != null)
        {
            nameInputField.text = PlayerNameManager.Instance != null
                ? PlayerNameManager.Instance.GetPlayerName()
                : PlayerPrefs.GetString("PlayerName", defaultName);

            nameInputField.Select();
            nameInputField.ActivateInputField();
        }
    }

    public void Hide()
    {
        Debug.Log("[NameInputScreen] Hide() method called");

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        string enteredName = nameInputField != null
            ? nameInputField.text.Trim()
            : "";

        if (string.IsNullOrEmpty(enteredName))
            enteredName = defaultName;

        if (PlayerNameManager.Instance != null)
            PlayerNameManager.Instance.SetPlayerName(enteredName);

        Debug.Log("[NameInputScreen] Name saved: " + enteredName);

        Hide();

        OnNameConfirmed?.Invoke(enteredName);
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                OnConfirmClicked();
            }
        }
    }
}