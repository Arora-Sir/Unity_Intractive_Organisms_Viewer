using UnityEngine;

/// <summary>
/// Manages all audio playback in the application, including background music and sound effects.
/// Implemented as a singleton to provide global access to audio functionality.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Singleton instance accessible from anywhere
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Audio source for background music")]
    public AudioSource bgmSource;   // Dedicated source for background music (loops continuously)

    [Tooltip("Audio source for sound effects")]
    public AudioSource sfxSource;   // Dedicated source for sound effects (plays one-shot sounds)

    [Header("Audio Clips")]
    [Tooltip("Background music clip")]
    public AudioClip bgmClip;       // Main background music track

    [Tooltip("Frog sound effect")]
    public AudioClip frogClip;      // Sound played when interacting with play button

    [Tooltip("UI click sound effect")]
    public AudioClip clickClip;     // Sound played when buttons are clicked

    /// <summary>
    /// Sets up the singleton pattern and ensures this object persists between scenes.
    /// </summary>
    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            // This is the first instance - make it the singleton
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Another instance already exists - destroy this duplicate
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Starts playing background music when the manager is initialized.
    /// </summary>
    void Start()
    {
        PlayBGM();
    }

    /// <summary>
    /// Plays the background music in a loop.
    /// </summary>
    public void PlayBGM()
    {
        if (bgmSource && bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;  // Ensure music loops continuously
            bgmSource.Play();
        }
        else
        {
            Debug.LogWarning("Cannot play BGM: Missing audio source or clip reference");
        }
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// Plays the frog sound effect once.
    /// </summary>
    public void PlayFrogSound()
    {
        if (sfxSource && frogClip)
        {
            sfxSource.PlayOneShot(frogClip);
        }
        else
        {
            Debug.LogWarning("Cannot play frog sound: Missing audio source or clip reference");
        }
    }

    /// <summary>
    /// Plays the UI click sound effect once.
    /// </summary>
    public void PlayClickSound()
    {
        if (sfxSource && clickClip)
        {
            sfxSource.PlayOneShot(clickClip);
        }
        else
        {
            Debug.LogWarning("Cannot play click sound: Missing audio source or clip reference");
        }
    }
}
