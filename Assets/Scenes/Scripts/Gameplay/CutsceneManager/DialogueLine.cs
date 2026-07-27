using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("Dialogue text shown when language is set to English.")]
        [TextArea(2, 5)]
        public string dialogueText;

        [Tooltip("Dialogue text shown when language is set to Filipino (Tagalog). " +
                 "Leave empty to fall back to the English text above.")]
        [TextArea(2, 5)]
        public string filipinoText = "";

        [Header("Text Formatting")]
        public bool useItalic = false;
        public bool useBold = false;

        [Header("Portrait And Animation")]
        [Tooltip("Specific emotion portrait for this line")]
        public Sprite emotionPortrait;

        [Header("Entrance Animation")]
        public EntranceType entranceType = EntranceType.None;
        [Tooltip("How long the slide/fade entrance takes (seconds)")]
        public float entranceDuration = 0.3f;


        [Header("Reaction Animation")]
        [Tooltip("Uncheck to suppress the tap-reaction animation for this line")]
        public bool enableReaction = false;
        public float reactionDuration = 0.35f;
        public float reactionScaleTarget = 1.3f;
        public bool enableShake = false;
        [Tooltip("Shake movement intensity in pixels")]
        public float shakeIntensity = 10f;
        public float shakeDuration = 0.2f;

        [Header("Outro Animation")]
        public OutroType outroType = OutroType.None;
        public float outroDuration = 0.4f;

        [Header("Custom Animation (Optional)")]
        [Tooltip("If assigned, plays on the portrait Animator instead of the built-in entrance animation")]
        public AnimationClip customEntranceClip;

        [Header("Optional UI Image")]
        [Tooltip("Optional UI Image to show during this line (e.g., background, decoration, icon)")]
        public Image optionalUIImage;

        [Header("Audio")]
        public AudioClip soundClip;

        [Header("Typing Sound Effect")]
        [Tooltip("Sound effect played during typewriter effect for this line")]
        public AudioClip typingSFX;

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

    public enum OutroType
    {
        None,
        SlideLeft,
        SlideRight
    }
}