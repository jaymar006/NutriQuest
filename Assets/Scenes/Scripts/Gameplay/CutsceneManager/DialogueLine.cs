using UnityEngine;

namespace Gameplay.CutsceneManager
{
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Speaker")]
        [Tooltip("Character who speaks this line. Leave null for custom or anonymous speakers.")]
        public CharacterVN character;

        [Header("Custom Speaker Override")]
        [Tooltip("Enable to use a custom name instead of character name")]
        public bool useCustomSpeakerName = false;

        [Tooltip("Custom name shown in dialogue box")]
        public string customSpeakerName = "";

        [Header("Dialogue Content")]
        [TextArea(2, 5)]
        public string dialogueText;

        [Header("Text Formatting")]
        public bool useItalic = false;
        public bool useBold = false;

        [Header("Portrait And Animation")]
        [Tooltip("Specific emotion portrait for this line")]
        public Sprite emotionPortrait;

        public EntranceType entranceType;

        [Header("Audio")]
        public AudioClip soundClip;

        [Header("Black Screen")]
        public bool useBlackScreen = false;

        [Tooltip("If true, dialogue box stays visible during black screen")]
        public bool excludeDialogueBoxFromBlackout = false;

        [Header("Dialogue Panel")]
        [Tooltip("Hide the entire dialogue panel for this line")]
        public bool hideDialoguePanel = false;

        [Header("Name Input Trigger")]
        [Tooltip("If true, opens name input screen after this line finishes")]
        public bool openNameInputAfterThisLine = false;

        public bool ShouldUseCustomName()
        {
            return !string.IsNullOrEmpty(customSpeakerName);
        }

        public bool HasValidSpeaker()
        {
            return character != null || ShouldUseCustomName();
        }

        public string GetSpeakerName()
        {
            if (ShouldUseCustomName())
                return customSpeakerName;

            if (character != null)
                return character.characterName;

            return "";
        }
    }

    public enum EntranceType
    {
        None,
        SlideFromLeft,
        SlideFromRight,
        FadeIn
    }
}