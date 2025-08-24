using System;
using Code.GlobalScreen.Behaviour;
using UnityEngine;
using UnityEngine.UI;

namespace Code
{
    public class LanguageItem : MonoBehaviour
    {
        [SerializeField] private Button _selectLanguage;
        [SerializeField] private Image _selectedImage;
        [SerializeField] private Image _languageIcon;
        [SerializeField] private LanguageType _language;
        private LanguageChanger _languageChanger;
        private AudioManager _audioManager;

        public LanguageType Language => _language;

        public void Setup(LanguageChanger languageChanger, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _languageChanger = languageChanger;
        }

        private void Start()
        {
            _selectLanguage.onClick.AddListener(SelectLanguage);
            _selectedImage.gameObject.SetActive(false); // По умолчанию изображение не активно
        }

        private void OnDestroy()
        {
            _selectLanguage.onClick.RemoveListener(SelectLanguage);
        }

        public void SelectLanguage()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _languageChanger.SetLanguage(_language);
        }

        public void SetSelected(bool isSelected)
        {
            _selectedImage.gameObject.SetActive(isSelected);
            _selectLanguage.interactable = !isSelected; // Отключаем интерактивность для выбранного языка
        }

        public Sprite GetSelectedImageSprite()
        {
            return _languageIcon != null ? _languageIcon.sprite : null;
        }
    }
}