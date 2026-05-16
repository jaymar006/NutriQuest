using UnityEngine;

public class PlayerNameManager : MonoBehaviour
{
    public static PlayerNameManager Instance { get; private set; }

    private const string PLAYER_NAME_KEY = "PlayerName";
    private const string DEFAULT_NAME = "Player";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Returns saved name or default if none saved
    public string GetPlayerName()
    {
        return PlayerPrefs.GetString(PLAYER_NAME_KEY, DEFAULT_NAME);
    }

    // Saves a new player name — trims whitespace, falls back to default if empty
    public void SetPlayerName(string newName)
    {
        string trimmed = newName != null ? newName.Trim() : "";
        string nameToSave = trimmed.Length > 0 ? trimmed : DEFAULT_NAME;
        PlayerPrefs.SetString(PLAYER_NAME_KEY, nameToSave);
        PlayerPrefs.Save();
        Debug.Log("[PlayerNameManager] Player name saved: " + nameToSave);
    }

    // Replaces {player} in any string with the saved player name
    // Call this anywhere before displaying text
    public static string InjectPlayerName(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return rawText;

        string name = Instance != null
            ? Instance.GetPlayerName()
            : DEFAULT_NAME;

        return rawText.Replace("{player}", name);
    }
}