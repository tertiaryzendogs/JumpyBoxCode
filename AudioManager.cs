using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized audio manager for the Jumpy Box game.
/// Handles all sound effects and music playback from a single point.
/// Use as a singleton: AudioManager.Instance.PlayJumpSound(), etc.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;        // For sound effects
    [SerializeField] private AudioSource uiSfxSource;      // For UI sounds
    [SerializeField] private AudioSource musicSource;      // For background music

    [Header("Player Sound Effects")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip outOfJumpsClip;
    [SerializeField] private AudioClip jumpCollectibleClip;
    [SerializeField] private AudioClip stickyOnClip;
    [SerializeField] private AudioClip stickyOffClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip bounceClip;
    [SerializeField] private AudioClip teleportClip;
    [SerializeField] private AudioClip surfaceHitLightClip;
    [SerializeField] private AudioClip surfaceHitHardClip;

    [Header("UI Sound Effects")]
    [SerializeField] private AudioClip gameStartClip;
    [SerializeField] private AudioClip buttonClickConfirmClip;
    [SerializeField] private AudioClip buttonClickDenyClip;
    [SerializeField] private AudioClip sliderMoveClip;

    [Header("Achievement Sound Effects")]
    [SerializeField] private AudioClip zeroStarClip;
    [SerializeField] private AudioClip oneStarClip;
    [SerializeField] private AudioClip twoStarClip;
    [SerializeField] private AudioClip threeStarClip;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip levelMusic;
    [SerializeField] private List<LevelMusicRange> levelMusicRanges;

    [Header("Volume Settings")]
    [SerializeField] private float sfxVolume = 0.5f;
    [SerializeField] private float musicVolume = 0.5f;

    // Singleton instance
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern - ensure only one AudioManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize audio sources if not assigned
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        if (uiSfxSource == null)
            uiSfxSource = gameObject.AddComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        // Load volumes from PlayerPrefs
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);

        // Set up audio source settings
        sfxSource.volume = sfxVolume;
        uiSfxSource.volume = sfxVolume; // UI sounds use same volume as SFX
        musicSource.volume = musicVolume;

        musicSource.loop = true;
        PlayGameStartSound();
        PlayMenuMusic();
    }

    #region Player Sound Effects
    public void PlayJumpSound()
    {
        if (jumpClip != null)
            sfxSource.PlayOneShot(jumpClip);
    }

    public void PlayOutOfJumpsSound()
    {
        if (outOfJumpsClip != null)
            sfxSource.PlayOneShot(outOfJumpsClip);
    }

    public void PlayJumpCollectibleSound()
    {
        if (jumpCollectibleClip != null)
            sfxSource.PlayOneShot(jumpCollectibleClip);
    }

    public void PlayStickyOnSound()
    {
        if (stickyOnClip != null)
            sfxSource.PlayOneShot(stickyOnClip);
    }

    public void PlayStickyOffSound()
    {
        if (stickyOffClip != null)
            sfxSource.PlayOneShot(stickyOffClip);
    }

    public void PlayDeathSound()
    {
        if (deathClip != null)
            sfxSource.PlayOneShot(deathClip);
    }
    #endregion

    #region World Sound Effects
    public void PlayBounceSound(Vector3 position)
    {
        if (bounceClip != null)
            sfxSource.PlayOneShot(bounceClip);
    }

    public void PlayTeleportSound(Vector3 position)
    {
        if (teleportClip != null)
            sfxSource.PlayOneShot(teleportClip);
    }

    public void PlaySurfaceHitSound(Vector3 position, float velocityMagnitude)
    {
        AudioClip clipToPlay = null;
        // Determine which clip to play based on velocity
        if (velocityMagnitude > 8f) // Hard hit threshold
        {
            clipToPlay = surfaceHitHardClip;
        }
        else if (velocityMagnitude > 3f) // Light hit threshold
        {
            clipToPlay = surfaceHitLightClip;
        }

        if (clipToPlay != null)
            sfxSource.PlayOneShot(clipToPlay);
    }
    #endregion

    #region UI Sound Effects
    public void PlayGameStartSound()
    {
        if (gameStartClip != null)
            uiSfxSource.PlayOneShot(gameStartClip);
    }

    public void PlayButtonClickConfirmSound()
    {
        if (buttonClickConfirmClip != null)
            uiSfxSource.PlayOneShot(buttonClickConfirmClip);
    }

    public void PlayButtonClickDenySound()
    {
        if (buttonClickDenyClip != null)
            uiSfxSource.PlayOneShot(buttonClickDenyClip);
    }

    public void PlaySliderMoveSound()
    {
        if (sliderMoveClip != null)
            uiSfxSource.PlayOneShot(sliderMoveClip);
    }
    #endregion

    #region Achievement Sound Effects
    public void PlayStarAchievementSound(int starCount)
    {
        AudioClip clipToPlay = null;

        switch (starCount)
        {
            case 0:
                clipToPlay = zeroStarClip;
                break;
            case 1:
                clipToPlay = oneStarClip;
                break;
            case 2:
                clipToPlay = twoStarClip;
                break;
            case 3:
                clipToPlay = threeStarClip;
                break;
        }
        if (clipToPlay != null)
            sfxSource.PlayOneShot(clipToPlay);
    }

    public float GetStarClipLength(int starCount)
    {
        AudioClip clip = null;
        switch (starCount)
        {
            case 0: clip = zeroStarClip; break;
            case 1: clip = oneStarClip; break;
            case 2: clip = twoStarClip; break;
            case 3: clip = threeStarClip; break;
        }
        return clip != null ? clip.length : 0f;
    }
    #endregion

    #region Music
    public void PlayMenuMusic()
    {
        if (menuMusic != null && musicSource.clip != menuMusic)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    public void PlayLevelMusic()
    {
        if (musicSource.clip != null)
            musicSource.Play();
    }

    // New method: Selects and sets the clip based on level, plays only if changed
    public void SetLevelMusicForLevel(int level)
    {
        AudioClip selectedClip = levelMusic;  // Default to existing levelMusic
        foreach (var range in levelMusicRanges)
        {
            if (level >= range.minLevel && level <= range.maxLevel && range.clip != null)
            {
                selectedClip = range.clip;
                break;  // Use the first matching range
            }
        }
        if (selectedClip != musicSource.clip)
        {
            musicSource.clip = selectedClip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PauseLevelMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    public void UnPauseLevelMusic()
    {
        musicSource.UnPause();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        uiSfxSource.volume = sfxVolume; // UI sounds use same volume as SFX
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void SetUIVolume(float volume)
    {
        // UI volume is now the same as SFX volume
        SetSFXVolume(volume);
    }
    #endregion
}

[System.Serializable]
public struct LevelMusicRange
{
    public int minLevel;
    public int maxLevel;
    public AudioClip clip;
}
