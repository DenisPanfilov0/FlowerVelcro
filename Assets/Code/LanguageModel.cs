using System;
using Assets.SimpleLocalization.Scripts;
using YG;
using Zenject;

namespace Code
{
    [Serializable]
    public class LanguageSettingsData
    {
        public string Language;
        public LanguageType LanguageType;
    }

    public class LanguageModel : IInitializable
    {
        private string _language;
        private LanguageType _languageType;
        private SaveLoadService _saveLoadService;
        private const string LANGUAGE_SETTINGS_KEY = "LanguageSettings";

        [Inject]
        public void Construct(SaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
        }

        public void Initialize()
        {
            LocalizationManager.Read();
            
            // Загружаем сохранённые настройки языка
            var settings = _saveLoadService.LoadData<LanguageSettingsData>(LANGUAGE_SETTINGS_KEY);
            if (settings != null && Enum.IsDefined(typeof(LanguageType), settings.LanguageType))
            {
                _language = settings.Language;
                _languageType = settings.LanguageType;
                YG2.SwitchLanguage(_language); // Устанавливаем сохранённый язык
                LocalizationManager.Language = _language;
            }
            else
            {
                // Если нет сохранённых данных, используем текущий язык из YG2
                // _language = YG2.lang;
                if (Enum.TryParse<LanguageType>(_language, true, out var languageType))
                {
                    _languageType = languageType;
                }
                else
                {
                    // Опционально: установка языка по умолчанию
                    _languageType = LanguageType.en;
                    _language = _languageType.ToString();
                    YG2.SwitchLanguage(_language);
                }
                
                LocalizationManager.Language = _language;
                
                SaveData(); // Сохраняем начальный язык
            }
        }

        public void SwitchLanguage(LanguageType language, Action<LanguageType> onLanguageSelected)
        {
            _language = language.ToString();
            _languageType = language;
            YG2.SwitchLanguage(_language);
            LocalizationManager.Language = _language;
            SaveData(); // Сохраняем новый язык
            onLanguageSelected?.Invoke(language);
        }

        public string GetLanguage()
        {
            return _language;
        }

        public LanguageType GetLanguageType()
        {
            return _languageType;
        }

        private void SaveData()
        {
            var settings = new LanguageSettingsData
            {
                Language = _language,
                LanguageType = _languageType
            };
            _saveLoadService.SaveData(LANGUAGE_SETTINGS_KEY, settings);
        }
    }

    public enum LanguageType
    {
        ru, //
        en, //
        tr, //
        fr, //
        es, //
        ja, //
        de, //
        zh, //
        id, //
    }
}