using Code.GlobalScreen.Behaviour;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code
{
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private Image _musicIconOn;
        [SerializeField] private Image _musicIconOff;
        [SerializeField] private Image _musicToogleOn;
        [SerializeField] private Button _musicChangeStateButton;
        
        [SerializeField] private Image _soundIconOn;
        [SerializeField] private Image _soundIconOff;
        [SerializeField] private Image _soundToogleOn;
        [SerializeField] private Button _soundChangeStateButton;

        [SerializeField] private Button _openLanguageChanger;
        [SerializeField] private LanguageChanger _languageChanger;

        [SerializeField] private Button _shadowInteractive; // Hides the current window, similar to the back button
        [SerializeField] private Button _backButton; // Closes the current window by deactivating it

        private AudioManager _audioManager;
        private LanguageModel _languageModel;

        [Inject]
        public void Construct(AudioManager audioManager, LanguageModel languageModel)
        {
            _languageModel = languageModel;
            _audioManager = audioManager;
        }

        private void Start()
        {
            InitializeUI();
            SetupButtonListeners();
            
            LanguageType language = _languageModel.GetLanguageType();
            _languageChanger.SetLanguage(language);
        }

        private void InitializeUI()
        {
            // Initialize music UI based on AudioManager state
            bool isMusicMuted = _audioManager.IsMusicMuted();
            _musicIconOn.gameObject.SetActive(!isMusicMuted);
            _musicIconOff.gameObject.SetActive(isMusicMuted);
            _musicToogleOn.gameObject.SetActive(!isMusicMuted);

            // Initialize sound UI based on AudioManager state
            bool isSfxMuted = _audioManager.IsSfxMuted();
            _soundIconOn.gameObject.SetActive(!isSfxMuted);
            _soundIconOff.gameObject.SetActive(isSfxMuted);
            _soundToogleOn.gameObject.SetActive(!isSfxMuted);
        }

        private void SetupButtonListeners()
        {
            _musicChangeStateButton.onClick.AddListener(ToggleMusic);
            _soundChangeStateButton.onClick.AddListener(ToggleSound);
            _openLanguageChanger.onClick.AddListener(OpenLanguageWindow);
            _shadowInteractive.onClick.AddListener(Hide);
            _backButton.onClick.AddListener(Hide);
        }

        private void ToggleMusic()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);

            if (_audioManager.IsMusicMuted())
            {
                _audioManager.EnableMusic();
                _musicIconOn.gameObject.SetActive(true);
                _musicIconOff.gameObject.SetActive(false);
                _musicToogleOn.gameObject.SetActive(true);
            }
            else
            {
                _audioManager.DisableMusic();
                _musicIconOn.gameObject.SetActive(false);
                _musicIconOff.gameObject.SetActive(true);
                _musicToogleOn.gameObject.SetActive(false);
            }
        }

        private void ToggleSound()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);

            if (_audioManager.IsSfxMuted())
            {
                _audioManager.EnableSfx();
                _soundIconOn.gameObject.SetActive(true);
                _soundIconOff.gameObject.SetActive(false);
                _soundToogleOn.gameObject.SetActive(true);
            }
            else
            {
                _audioManager.DisableSfx();
                _soundIconOn.gameObject.SetActive(false);
                _soundIconOff.gameObject.SetActive(true);
                _soundToogleOn.gameObject.SetActive(false);
            }
        }

        private void OpenLanguageWindow()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _languageChanger.Show();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _musicChangeStateButton.onClick.RemoveListener(ToggleMusic);
            _soundChangeStateButton.onClick.RemoveListener(ToggleSound);
            _openLanguageChanger.onClick.RemoveListener(OpenLanguageWindow);
            _shadowInteractive.onClick.RemoveListener(Hide);
            _backButton.onClick.RemoveListener(Hide);
        }
    }
}