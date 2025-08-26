using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Code
{
    [Serializable]
    public class AudioSettingsData
    {
        public bool IsMusicMuted;
        public bool IsSfxMuted;
        public AudioClipTypeId CurrentTrackId;
    }

    public class AudioManager : MonoBehaviour, ISaveLoad
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [SerializeField] private List<AudioClipTypeId> backgroundMusicTrackIds;
        [SerializeField] private List<AudioClip> backgroundMusicTrackClips;
        [SerializeField] private List<AudioClipTypeId> soundEffectIds;
        [SerializeField] private List<AudioClip> soundEffectClips;

        private Dictionary<AudioClipTypeId, AudioClip> backgroundMusicTracks;
        private Dictionary<AudioClipTypeId, AudioClip> soundEffects;

        private bool isMusicMuted = false;
        private bool isSfxMuted = false;
        private AudioClipTypeId currentTrackId = AudioClipTypeId.Unknown;
        private SaveLoadService saveLoadService;
        private const string AUDIO_SETTINGS_KEY = "AudioSettings";

        [Inject]
        public void Construct(SaveLoadService saveLoadService)
        {
            this.saveLoadService = saveLoadService;
            LoadData();
        }

        private void Start()
        {
            DontDestroyOnLoad(this);
            InitializeDictionaries();
        }

        private void InitializeDictionaries()
        {
            backgroundMusicTracks = new Dictionary<AudioClipTypeId, AudioClip>();
            soundEffects = new Dictionary<AudioClipTypeId, AudioClip>();

            for (int i = 0; i < backgroundMusicTrackIds.Count && i < backgroundMusicTrackClips.Count; i++)
            {
                if (backgroundMusicTrackIds[i] != AudioClipTypeId.Unknown && backgroundMusicTrackClips[i] != null)
                {
                    backgroundMusicTracks[backgroundMusicTrackIds[i]] = backgroundMusicTrackClips[i];
                }
            }

            for (int i = 0; i < soundEffectIds.Count && i < soundEffectClips.Count; i++)
            {
                if (soundEffectIds[i] != AudioClipTypeId.Unknown && soundEffectClips[i] != null)
                {
                    soundEffects[soundEffectIds[i]] = soundEffectClips[i];
                }
            }
        }

        public void PlayMusic(AudioClipTypeId musicId)
        {
            if (backgroundMusicTracks.TryGetValue(musicId, out AudioClip clip))
            {
                currentTrackId = musicId;
                musicSource.clip = clip;
                musicSource.loop = true;
                musicSource.Play();
                SaveData();
            }
        }

        public void PlaySoundEffect(AudioClipTypeId sfxId, float volume = 0.8f)
        {
            sfxSource.volume = volume;
            
            if (soundEffects.TryGetValue(sfxId, out AudioClip clip))
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void ToggleMusic()
        {
            isMusicMuted = !isMusicMuted;
            if (musicSource != null)
            {
                musicSource.mute = isMusicMuted;
                SaveData();
            }
        }

        public void ToggleSfx()
        {
            isSfxMuted = !isSfxMuted;
            if (sfxSource != null)
            {
                sfxSource.mute = isSfxMuted;
                SaveData();
            }
        }

        public void EnableMusic()
        {
            isMusicMuted = false;
            if (musicSource != null)
            {
                musicSource.mute = false;
                SaveData();
            }
        }

        public void DisableMusic()
        {
            isMusicMuted = true;
            if (musicSource != null)
            {
                musicSource.mute = true;
                SaveData();
            }
        }

        public void EnableSfx()
        {
            isSfxMuted = false;
            if (sfxSource != null)
            {
                sfxSource.mute = false;
                SaveData();
            }
        }

        public void DisableSfx()
        {
            isSfxMuted = true;
            if (sfxSource != null)
            {
                sfxSource.mute = true;
                SaveData();
            }
        }

        public void SetMusicMuteState(bool mute)
        {
            isMusicMuted = mute;
            if (musicSource != null)
            {
                musicSource.mute = mute;
                SaveData();
            }
        }

        public void SetSfxMuteState(bool mute)
        {
            isSfxMuted = mute;
            if (sfxSource != null)
            {
                sfxSource.mute = mute;
                SaveData();
            }
        }

        public bool IsMusicMuted()
        {
            return isMusicMuted;
        }

        public bool IsSfxMuted()
        {
            return isSfxMuted;
        }

        public void SaveData()
        {
            var settings = new AudioSettingsData
            {
                IsMusicMuted = isMusicMuted,
                IsSfxMuted = isSfxMuted,
                CurrentTrackId = currentTrackId
            };
            saveLoadService.SaveData(AUDIO_SETTINGS_KEY, settings);
        }

        public void LoadData()
        {
            var settings = saveLoadService.LoadData<AudioSettingsData>(AUDIO_SETTINGS_KEY);
            if (settings != null)
            {
                isMusicMuted = settings.IsMusicMuted;
                isSfxMuted = settings.IsSfxMuted;
                currentTrackId = settings.CurrentTrackId;

                if (musicSource != null)
                {
                    musicSource.mute = isMusicMuted;
                }
                if (sfxSource != null)
                {
                    sfxSource.mute = isSfxMuted;
                }
                if (currentTrackId != AudioClipTypeId.Unknown)
                {
                    PlayMusic(currentTrackId);
                }
            }
        }
    }

    public enum AudioClipTypeId
    {
        Unknown,
        ButtonClick,
        OpenChest,
        EndFly,
        NewRecord,
        CollectedPollen,
        Theme1,
    }
}