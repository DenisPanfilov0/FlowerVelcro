using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

namespace Code.GlobalScreen.Behaviour
{
    [Serializable]
    public class AudioSettingsData
    {
        public bool IsMusicMuted;
        public bool IsSfxMuted;
        public AudioClipTypeId CurrentTrackId;
    }

    public class AudioManager : SerializedMonoBehaviour, ISaveLoad
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [SerializeField] private Dictionary<AudioClipTypeId, AudioClip> backgroundMusicTracks;
        [SerializeField] public Dictionary<AudioClipTypeId, AudioClip> soundEffects;

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