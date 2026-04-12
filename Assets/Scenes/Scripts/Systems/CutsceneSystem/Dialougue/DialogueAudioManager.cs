using UnityEngine;
using System.Collections.Generic;

public class DialogueAudioManager : MonoBehaviour
{
    public static DialogueAudioManager Instance { get; private set; }

    [System.Serializable]
    public class CharacterAudio
    {
        public string characterName;
        public AudioClip voiceIntroClip;
        public AudioClip typingSoundClip;
        [Range(0f, 1f)] public float typingVolume = 0.5f;
        public int typingSoundInterval = 1;
    }

    [SerializeField] private List<CharacterAudio> characterAudios = new List<CharacterAudio>();
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource typingSource;

    private void Awake()
    {
        Instance = this;

        // Auto-create AudioSources if missing
        if (voiceSource == null)
        {
            voiceSource = CreateAudioSource("VoiceSource");
        }
        if (typingSource == null)
        {
            typingSource = CreateAudioSource("TypingSource");
        }
    }

    private AudioSource CreateAudioSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.AddComponent<AudioSource>();
    }

    public void PlayVoiceIntro(string characterName)
    {
        CharacterAudio audio = GetCharacterAudio(characterName);
        if (audio?.voiceIntroClip == null) return;

        voiceSource.Stop();
        voiceSource.clip = audio.voiceIntroClip;
        voiceSource.Play();
    }

    public void PlayTypingSound(string characterName, int charIndex)
    {
        CharacterAudio audio = GetCharacterAudio(characterName);
        if (audio?.typingSoundClip == null) return;
        if (charIndex % audio.typingSoundInterval != 0) return;

        typingSource.PlayOneShot(audio.typingSoundClip, audio.typingVolume);
    }

    public void StopTypingSound()
    {
        typingSource.Stop();
    }

    private CharacterAudio GetCharacterAudio(string characterName)
    {
        return characterAudios.Find(a => a.characterName == characterName);
    }
}