using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip surpriseSound;
    [SerializeField] private AudioClip levelUpSound;
    
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySurpriseSound()
    {
        if (surpriseSound != null)
            audioSource.PlayOneShot(surpriseSound);
        else
            Debug.LogWarning("AudioManager: surpriseSound clip not assigned");
    }

    public void PlayLevelUpSound()
    {
        if (levelUpSound != null)
            audioSource.PlayOneShot(levelUpSound);
        else
            Debug.LogWarning("AudioManager: levelUpSound clip not assigned");
    }
}
