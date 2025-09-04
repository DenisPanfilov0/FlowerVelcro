using Code.Progress.Data;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Tutorial
{
    public class EndTutorial : MonoBehaviour
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _shadowInteractive;
        private ProgressData _progressData;
        private AudioManager _audioManager;

        [Inject]
        public void Construct(ProgressData progressData, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _progressData = progressData;
        }
        
        private void Start()
        {
            _backButton.onClick.AddListener(TutorialEnded);
            _shadowInteractive.onClick.AddListener(TutorialEnded);

            if (_progressData.IsTutorialChecked)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(TutorialEnded);
            _shadowInteractive.onClick.RemoveListener(TutorialEnded);
        }

        private void TutorialEnded()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _progressData.SetTutorialChecked(true);
            Destroy(gameObject);
        }
    }
}