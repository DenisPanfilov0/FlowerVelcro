using System.Collections.Generic;
using Code.GlobalScreen.Behaviour;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code
{
    public class LanguageChanger : MonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private List<LanguageItem> _languageItems;
        [SerializeField] private Image _languageIcon;

        [Header("Simulation")]
        [SerializeField] private string lang;

        private AudioManager _audioManager;
        private LanguageModel _languageModel;

        [Inject]
        public void Construct(AudioManager audioManager, LanguageModel languageModel)
        {
            _audioManager = audioManager;
            _languageModel = languageModel;
        }

        private void Start()
        {
            // Инициализация всех LanguageItem
            foreach (var item in _languageItems)
            {
                item.Setup(this, _audioManager);
            }

            // // Имитация выбора языка при старте без звука
            // LanguageType language = _languageModel.GetLanguageType();
            // SelectLanguageItem(language);

            UpdateSimulationLang();
            _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(Hide);
        }

        public void SetLanguage(LanguageType language)
        {
            _languageModel.SwitchLanguage(language, SelectLanguageItem);
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

        private void SelectLanguageItem(LanguageType language)
        {
            foreach (var item in _languageItems)
            {
                bool isSelected = item.Language == language;
                item.SetSelected(isSelected);
                if (isSelected && _languageIcon != null)
                {
                    _languageIcon.sprite = item.GetSelectedImageSprite(); // Устанавливаем спрайт активного языка
                }
            }
        }

        [Button]
        private void UpdateSimulationLang()
        {
            lang = _languageModel.GetLanguage();
        }
    }
}