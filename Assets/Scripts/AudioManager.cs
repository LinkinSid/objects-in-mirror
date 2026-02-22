using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music Tracks")]
    public AudioClip menuMusic;              // "All There Could Be"
    public AudioClip standardMusic;          // "It's Only You"
    public AudioClip chaseMusic;             // "Sciophobia"
    public AudioClip bossMusic;              // "H E A V E N"
    public AudioClip postBossMusic;          // "it's still raining"

    [Header("SFX")]
    public AudioClip damageSfx;
    public AudioClip footstepsSfx;
    public AudioClip runningFootstepsSfx;
    public AudioClip swimSfx;
    public AudioClip monsterSpottedSfx;
    public AudioClip bossRoarSfx;
    public AudioClip bossGruntSfx;
    public AudioClip speechBlipSfx;
    public AudioClip menuOpenSfx;
    public AudioClip menuScrollSfx;
    public AudioClip roomChangeSfx;
    public AudioClip meowSfx;
    public AudioClip dashSfx;
    public AudioClip dangerZoneBlastSfx;
    public AudioClip attackSfx;
    public AudioClip bossAttackSfx;
    public AudioClip deathSfx;
    public AudioClip monsterDeathSfx;
    public AudioClip bossDeathSfx;
    public AudioClip[] monsterAmbientSfx;   // Monster1, Monster2, Monster3

    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    public float crossfadeDuration = 1.5f;

    [Header("HEAVEN Loop Points (seconds)")]
    public float heavenLoopEnd = 161.22f;    // 2:41.22
    public float heavenLoopStart = 14.65f;   // 0:14.65

    // Audio sources
    AudioSource musicSourceA;
    AudioSource musicSourceB;
    AudioSource sfxSource;
    AudioSource footstepSource;

    // Crossfade state
    AudioSource activeMusicSource;
    AudioSource inactiveMusicSource;
    float crossfadeTimer;
    bool isCrossfading;

    // Enemy alert music
    AudioClip baseClip;
    int alertCount;
    bool isChaseMusicPlaying;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceB = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();
        footstepSource = gameObject.AddComponent<AudioSource>();

        ConfigureMusicSource(musicSourceA);
        ConfigureMusicSource(musicSourceB);

        sfxSource.playOnAwake = false;
        footstepSource.playOnAwake = false;
        footstepSource.loop = true;

        // Load saved volume prefs
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        sfxSource.volume = sfxVolume;
        footstepSource.volume = sfxVolume * 0.5f;

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;

        // Preload all music clips so first-play doesn't stutter
        PreloadClip(chaseMusic);
        PreloadClip(bossMusic);
        PreloadClip(postBossMusic);
        PreloadClip(standardMusic);
        PreloadClip(menuMusic);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void PreloadClip(AudioClip clip)
    {
        if (clip != null)
            clip.LoadAudioData();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    static void ConfigureMusicSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset alert state on scene change
        alertCount = 0;
        isChaseMusicPlaying = false;
        footstepSource.Stop();

        if (scene.name == "MainMenu")
        {
            baseClip = menuMusic;
            PlayMusic(menuMusic);
        }
        else if (scene.name == "Room-1")
        {
            baseClip = standardMusic;
            PlayMusic(standardMusic);
        }
    }

    void Update()
    {
        HandleHeavenLoop(musicSourceA);
        HandleHeavenLoop(musicSourceB);

        // Crossfade
        if (isCrossfading)
        {
            crossfadeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(crossfadeTimer / crossfadeDuration);

            activeMusicSource.volume = t * musicVolume;
            inactiveMusicSource.volume = (1f - t) * musicVolume;

            if (t >= 1f)
            {
                isCrossfading = false;
                inactiveMusicSource.Stop();
                inactiveMusicSource.clip = null;
            }
        }
    }

    void HandleHeavenLoop(AudioSource source)
    {
        if (source.clip != bossMusic || source.clip == null) return;

        if (source.isPlaying && source.time >= heavenLoopEnd)
        {
            source.time = heavenLoopStart;
        }
        else if (!source.isPlaying && source == activeMusicSource)
        {
            // Clip ended naturally — restart at loop point
            source.time = heavenLoopStart;
            source.Play();
        }
    }

    // ─── Public API ─────────────────────────────────────────

    public void PlayMusic(AudioClip clip, bool instant = false)
    {
        if (clip == null) return;
        if (activeMusicSource.clip == clip && activeMusicSource.isPlaying) return;

        // HEAVEN uses custom loop logic, not Unity's native loop
        bool nativeLoop = (clip != bossMusic);

        if (instant || !activeMusicSource.isPlaying)
        {
            activeMusicSource.clip = clip;
            activeMusicSource.loop = nativeLoop;
            activeMusicSource.volume = musicVolume;
            activeMusicSource.time = 0f;
            activeMusicSource.Play();
        }
        else
        {
            // Swap sources for crossfade
            var temp = activeMusicSource;
            activeMusicSource = inactiveMusicSource;
            inactiveMusicSource = temp;

            activeMusicSource.clip = clip;
            activeMusicSource.loop = nativeLoop;
            activeMusicSource.volume = 0f;
            activeMusicSource.time = 0f;
            activeMusicSource.Play();

            crossfadeTimer = 0f;
            isCrossfading = true;
        }
    }

    public void PlayBossMusic()
    {
        // Boss overrides everything — reset alert state
        alertCount = 0;
        isChaseMusicPlaying = false;
        PlayMusic(bossMusic);
    }

    public void PlayPostBossMusic()
    {
        baseClip = postBossMusic;
        alertCount = 0;
        isChaseMusicPlaying = false;
        PlayMusic(postBossMusic);
    }

    // ─── Enemy Alert Music ──────────────────────────────────

    public void EnemyAlertStarted()
    {
        alertCount++;
        if (alertCount == 1 && !isChaseMusicPlaying
            && activeMusicSource.clip != bossMusic)
        {
            isChaseMusicPlaying = true;
            PlayMusic(chaseMusic);
        }
    }

    public void EnemyAlertEnded()
    {
        alertCount = Mathf.Max(0, alertCount - 1);
        if (alertCount == 0 && isChaseMusicPlaying)
        {
            isChaseMusicPlaying = false;
            PlayMusic(baseClip);
        }
    }

    // ─── Footsteps ──────────────────────────────────────────

    public void StartFootsteps(bool running = false)
    {
        AudioClip clip = running && runningFootstepsSfx != null ? runningFootstepsSfx : footstepsSfx;
        if (clip == null) return;

        // Swap clip if it changed (walk ↔ run)
        if (footstepSource.clip != clip)
        {
            footstepSource.Stop();
            footstepSource.clip = clip;
        }

        if (!footstepSource.isPlaying)
        {
            footstepSource.volume = sfxVolume * 0.5f;
            footstepSource.Play();
        }
    }

    public void StopFootsteps()
    {
        if (footstepSource.isPlaying)
            footstepSource.Stop();
    }

    public void StopAllAudio()
    {
        StopMusic(false);
        activeMusicSource.clip = null;
        inactiveMusicSource.clip = null;
        StopFootsteps();
        sfxSource.Stop();
    }

    // ─── Music / SFX Control ────────────────────────────────

    public void StopMusic(bool fade = true)
    {
        if (fade)
        {
            var temp = activeMusicSource;
            activeMusicSource = inactiveMusicSource;
            inactiveMusicSource = temp;

            activeMusicSource.Stop();
            activeMusicSource.clip = null;
            activeMusicSource.volume = 0f;

            crossfadeTimer = 0f;
            isCrossfading = true;
        }
        else
        {
            activeMusicSource.Stop();
            inactiveMusicSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = vol;
        PlayerPrefs.SetFloat("MusicVolume", vol);
        if (!isCrossfading && activeMusicSource.isPlaying)
            activeMusicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = vol;
        PlayerPrefs.SetFloat("SFXVolume", vol);
        sfxSource.volume = sfxVolume;
        footstepSource.volume = sfxVolume * 0.5f;
    }

    // ─── Convenience SFX Methods ────────────────────────────

    public void PlayDamageSFX() => PlaySFX(damageSfx);
    public void PlayMonsterSpottedSFX() => PlaySFX(monsterSpottedSfx);
    public void PlayBossRoarSFX() => PlaySFX(bossRoarSfx);
    public void PlayBossGruntSFX() => PlaySFX(bossGruntSfx);
    public void PlaySwimSFX() => PlaySFX(swimSfx);
    public void PlaySpeechBlipSFX() => PlaySFX(speechBlipSfx, 0.5f);
    public void PlayMenuOpenSFX() => PlaySFX(menuOpenSfx);
    public void PlayMenuScrollSFX() => PlaySFX(menuScrollSfx);
    public void PlayRoomChangeSFX() => PlaySFX(roomChangeSfx);
    public void PlayDashSFX() => PlaySFX(dashSfx);
    public void PlayDangerZoneBlastSFX() => PlaySFX(dangerZoneBlastSfx);
    public void PlayAttackSFX() => PlaySFX(attackSfx);
    public void PlayBossAttackSFX() => PlaySFX(bossAttackSfx);
    public void PlayDeathSFX() => PlaySFX(deathSfx);
    public void PlayMonsterDeathSFX() => PlaySFX(monsterDeathSfx);
    public void PlayBossDeathSFX() => PlaySFX(bossDeathSfx);
}
