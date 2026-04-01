using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DialogueAudioManager : MonoBehaviour
{
    public static DialogueAudioManager Instance { get; private set; }

    [System.Serializable]
    public class CharacterAudio
    {
        public string characterName;
        [Tooltip("SFX played when this character starts speaking.")]
        public AudioClip voiceIntroClip;
        [Tooltip("Typing sound unique to this character.")]
        public AudioClip typingSoundClip;
        [Tooltip("Volume of the typing sound.")]
        [Range(0f, 1f)] public float typingVolume = 0.5f;
        [Tooltip("How often typing sound plays (every N characters).")]
        public int typingSoundInterval = 1;
    }

    [Header("Character Audio")]
    [SerializeField] private List<CharacterAudio> characterAudios = new List<CharacterAudio>();

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource typingSource;

    private void Awake()
    {
        Instance = this;

        // Auto create audio sources if missing //
        if (voiceSource == null)
        {
            GameObject voiceGO = new GameObject("VoiceSource");
            voiceGO.transform.SetParent(transform);
            voiceSource = voiceGO.AddComponent<AudioSource>();
        }

        if (typingSource == null)
        {
            GameObject typingGO = new GameObject("TypingSource");
            typingGO.transform.SetParent(transform);
            typingSource = typingGO.AddComponent<AudioSource>();
        }
    }

    // Play voice intro when character starts speaking //
    public void PlayVoiceIntro(string characterName)
    {
        CharacterAudio audio = GetCharacterAudio(characterName);
        if (audio == null || audio.voiceIntroClip == null) return;

        voiceSource.Stop();
        voiceSource.clip = audio.voiceIntroClip;
        voiceSource.Play();
    }

    // Play typing sound per character //
    public void PlayTypingSound(string characterName, int charIndex)
    {
        CharacterAudio audio = GetCharacterAudio(characterName);
        if (audio == null || audio.typingSoundClip == null) return;

        // Only play every N characters //
        if (charIndex % audio.typingSoundInterval != 0) return;

        typingSource.PlayOneShot(audio.typingSoundClip, audio.typingVolume);
    }

    public void StopTypingSound()
    {
        typingSource.Stop();
    }

    private CharacterAudio GetCharacterAudio(string characterName)
    {
        foreach (CharacterAudio audio in characterAudios)
        {
            if (audio.characterName == characterName)
                return audio;
        }
        return null;
    }
}