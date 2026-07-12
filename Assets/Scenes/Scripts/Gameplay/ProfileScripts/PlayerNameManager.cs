using UnityEngine;

namespace Gameplay.CutsceneManager
{
    public class PlayerNameManager : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Singleton
        // -----------------------------------------------------------------------
        public static PlayerNameManager Instance { get; private set; }

        // -----------------------------------------------------------------------
        // Private constants
        // -----------------------------------------------------------------------

        private const string PLAYER_NAME_KEY = "PlayerName";
        private const string DEFAULT_NAME = "Player";

        // Placeholder tag used inside dialogue lines
        // Type {player} anywhere in your dialogue text to inject the player name
        private const string NAME_TAG = "{player}";

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

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

        // -----------------------------------------------------------------------
        // Public instance methods
        // Called by NameInputScreen via PlayerNameManager.Instance
        // -----------------------------------------------------------------------

        // Returns the saved name, or DEFAULT_NAME if none has been set yet
        public string GetPlayerName()
        {
            return PlayerPrefs.GetString(PLAYER_NAME_KEY, DEFAULT_NAME);
        }

        // Saves the player name - trims whitespace, falls back to default if empty
        // Called by NameInputScreen when the player confirms their name
        public void SetPlayerName(string newName)
        {
            string trimmed = newName != null ? newName.Trim() : "";
            string nameToSave = trimmed.Length > 0 ? trimmed : DEFAULT_NAME;
            PlayerPrefs.SetString(PLAYER_NAME_KEY, nameToSave);
            PlayerPrefs.Save();
            Debug.Log("[PlayerNameManager] Player name saved: " + nameToSave);
        }

        // Returns true if the player has already set a name
        // Useful for skipping the name input screen on returning sessions
        public bool HasPlayerName()
        {
            return PlayerPrefs.HasKey(PLAYER_NAME_KEY);
        }

        // Clears the saved name
        // Call this when starting a new game or resetting save data
        public void ClearPlayerName()
        {
            PlayerPrefs.DeleteKey(PLAYER_NAME_KEY);
            PlayerPrefs.Save();
            Debug.Log("[PlayerNameManager] Player name cleared.");
        }

        // -----------------------------------------------------------------------
        // Public static method
        // Called by DialogueManager.BuildFormattedText() on every dialogue line
        // Replaces every {player} tag in the text with the saved name
        // Works even if Instance is null by falling back to DEFAULT_NAME
        // -----------------------------------------------------------------------
        public static string InjectPlayerName(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return rawText;

            string name = Instance != null
                ? Instance.GetPlayerName()
                : PlayerPrefs.GetString(PLAYER_NAME_KEY, DEFAULT_NAME);

            return rawText.Replace(NAME_TAG, name);
        }
    }
}