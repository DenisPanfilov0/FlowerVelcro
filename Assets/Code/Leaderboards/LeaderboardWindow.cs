using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Leaderboards
{
    public class LeaderboardWindow : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        private AudioManager _audioManager;

        [Inject]
        public void Construct(AudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        private void Start()
        {
            _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            gameObject.SetActive(false);
        }
    }
}