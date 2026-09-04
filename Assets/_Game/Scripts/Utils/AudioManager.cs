using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : SingletonComponent<AudioManager>
{
    [Range(0f, 1f)] public float bgMusicVolume;
    public Sound[] backgroudMusic;

    [Range(0f, 1f)] public float sfxVolume;
    public Sound[] sfxs;

    private AudioSource bgMusicAudioSource;
    private AudioSource sfxAudioSource;

    private readonly Dictionary<string, int> _lastPlayedFrame = new();
    private string _currentBgm;
    private Sound _currentBgmSound;

    public void SuppressSFX(string name)
    {
        _lastPlayedFrame[name] = Time.frameCount;
    }

    public void PlayThemeSong(string name)
    {
        Sound s = Array.Find(backgroudMusic, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        bgMusicAudioSource.clip = s.clip;
        bgMusicAudioSource.pitch = s.pitch;
        bgMusicAudioSource.loop = s.loop;
        bgMusicAudioSource.volume = bgMusicVolume * s.volume;
        bgMusicAudioSource.Play();
        _currentBgmSound = s;
    }

    public void PauseThemeSong()
    {
        if (bgMusicAudioSource != null && bgMusicAudioSource.isPlaying)
        {
            bgMusicAudioSource.Pause();
        }
    }

    public void ResumeThemeSong()
    {
        if (bgMusicAudioSource != null && !bgMusicAudioSource.isPlaying)
        {
            bgMusicAudioSource.UnPause();
        }
    }

    public void PlaySFXOneShot(string name)
    {
        var frame = Time.frameCount;
        if (_lastPlayedFrame.TryGetValue(name, out var lastFrame) && lastFrame == frame)
            return;
        _lastPlayedFrame[name] = frame;

        Sound s = Array.Find(sfxs, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        sfxAudioSource.pitch = s.pitch;
        sfxAudioSource.PlayOneShot(s.clip, s.volume);
    }

    public void PlaySFX(string name, bool forceRestart = true)
    {
        Sound s = Array.Find(sfxs, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        if (forceRestart || !s.source.isPlaying)
            s.source.Play();
    }

    public void StopSFX(string name)
    {
        Sound s = Array.Find(sfxs, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }

        s.source.Stop();
    }

    private void Start()
    {
        bgMusicVolume = DataHelper.MusicVolume;
        sfxVolume = DataHelper.SfxVolume;

        bgMusicAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource = gameObject.AddComponent<AudioSource>();

        bgMusicAudioSource.volume = bgMusicVolume;
        sfxAudioSource.volume = sfxVolume;

        foreach (var sound in sfxs)
        {
            sound.SetupAudioSource(gameObject);
            sound.source.volume = sfxVolume * sound.volume;
        }

        DataHelper.RegisterDataChangedCallback(DataKey.MUSIC_VOLUME, (o) => SetMusicVolume((float)o));
        DataHelper.RegisterDataChangedCallback(DataKey.SFX_VOLUME, (o) => SetSfxVolume((float)o));

        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var gameplayName = System.IO.Path.GetFileName(Key.GAMEPLAY_SCENE);
        var tutorialName = System.IO.Path.GetFileName(Key.TUTORIAL_SCENE);
        var target = (scene.name == gameplayName || scene.name == tutorialName)
            ? "gameplay" : "home";

        if (_currentBgm == target) return;
        _currentBgm = target;
        PlayThemeSong(target);
    }

    private void SetMusicVolume(float volume)
    {
        bgMusicVolume = volume;
        bgMusicAudioSource.volume = volume * (_currentBgmSound?.volume ?? 1f);
    }

    private void SetSfxVolume(float volume)
    {
        sfxVolume = volume;
        sfxAudioSource.volume = sfxVolume;
        foreach (var sound in sfxs)
        {
            sound.source.volume = sfxVolume * sound.volume;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Update Sfx Volume")]
    private void UpdateSfxVolume()
    {
        foreach (var sound in sfxs)
        {
            sound.source.volume = sfxVolume * sound.volume;
        }
    }
#endif
}

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0.1f, 3f)]
    public float pitch = 1;
    [Range(0, 1)] public float volume = 1;
    public bool loop;

    [HideInInspector]
    public AudioSource source;

    public void SetupAudioSource(GameObject gameObject)
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.pitch = pitch;
        source.loop = loop;
    }
}