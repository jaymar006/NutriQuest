using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class UISFXManager : MonoBehaviour
{
    [System.Serializable]
    public class ButtonGroup
    {
        public string groupName;
        public AudioClip soundClip;

        [Tooltip("Parent object containing buttons")]
        public Transform buttonParent;

        [HideInInspector] public List<Button> cachedButtons = new List<Button>();
    }

    [SerializeField] private ButtonGroup[] buttonGroups;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private AudioSource audioSource;
    private Dictionary<Button, UnityAction> registeredListeners = new Dictionary<Button, UnityAction>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnEnable()
    {
        RegisterAllGroups();
    }

    private void OnDisable()
    {
        ClearAllListeners();
    }

    private void RegisterAllGroups()
    {
        ClearAllListeners();

        if (buttonGroups == null)
            return;

        foreach (var group in buttonGroups)
        {
            if (group == null || group.soundClip == null || group.buttonParent == null)
                continue;

            group.cachedButtons.Clear();

            Button[] buttons = group.buttonParent.GetComponentsInChildren<Button>(true);

            foreach (var button in buttons)
            {
                if (button == null || registeredListeners.ContainsKey(button))
                    continue;

                group.cachedButtons.Add(button);

                UnityAction action = () => Play(group.soundClip);

                button.onClick.AddListener(action);
                registeredListeners.Add(button, action);
            }
        }
    }

    private void ClearAllListeners()
    {
        foreach (var pair in registeredListeners)
        {
            if (pair.Key != null)
                pair.Key.onClick.RemoveListener(pair.Value);
        }

        registeredListeners.Clear();
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    public void RefreshGroups()
    {
        RegisterAllGroups();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }

    public float GetVolume()
    {
        return volume;
    }
}