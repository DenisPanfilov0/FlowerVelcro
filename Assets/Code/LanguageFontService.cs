using System;
using System.Collections.Generic;
using Assets.SimpleLocalization.Scripts;
using TMPro;
using Zenject;

namespace Code
{
    public class LanguageFontService : IInitializable, IDisposable
    {
        private readonly LanguageFontConfig _languageFontConfig;
        private readonly LanguageModel _languageModel;
        private Dictionary<LanguageType, TMP_FontAsset> _fonts;
        private Dictionary<LanguageType, TMPSettings> _settings;
        private LanguageType _languageType;

        public struct TMPSettings
        {
            public FontStyles? FontStyle;
        }

        private static LanguageFontService _instance;
        public static LanguageFontService Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("[LanguageFontService] Instance not initialized. Ensure Zenject has created the service before accessing.");
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Inject]
        public LanguageFontService(LanguageFontConfig languageFontConfig, LanguageModel languageModel)
        {
            _languageFontConfig = languageFontConfig;
            _languageModel = languageModel;
            _instance = this;
        }

        public void Initialize()
        {
            _fonts = new Dictionary<LanguageType, TMP_FontAsset>
            {
                { LanguageType.ru, _languageFontConfig.DefaultFont },
                { LanguageType.en, _languageFontConfig.DefaultFont },
                { LanguageType.tr, _languageFontConfig.DefaultFont },
                { LanguageType.fr, _languageFontConfig.DefaultFont },
                { LanguageType.es, _languageFontConfig.DefaultFont },
                { LanguageType.ja, _languageFontConfig.AsianFont },
                { LanguageType.de, _languageFontConfig.DefaultFont },
                { LanguageType.zh, _languageFontConfig.AsianFont },
                { LanguageType.id, _languageFontConfig.DefaultFont },
            };

            _settings = new Dictionary<LanguageType, TMPSettings>
            {
                { LanguageType.ru, new TMPSettings { FontStyle = null } },
                { LanguageType.en, new TMPSettings { FontStyle = null } },
                { LanguageType.tr, new TMPSettings { FontStyle = null } },
                { LanguageType.fr, new TMPSettings { FontStyle = null } },
                { LanguageType.es, new TMPSettings { FontStyle = null } },
                { LanguageType.ja, new TMPSettings { FontStyle = FontStyles.Normal } },
                { LanguageType.de, new TMPSettings { FontStyle = null } },
                { LanguageType.zh, new TMPSettings { FontStyle = FontStyles.Normal } },
                { LanguageType.id, new TMPSettings { FontStyle = null } },
            };

            LocalizationManager.OnLocalizationChanged += ChangeLanguageType;
            ChangeLanguageType();
        }

        public TMP_FontAsset GetFontByLanguageType()
        {
            _fonts.TryGetValue(_languageType, out var fontAsset);
            return fontAsset != null ? fontAsset : _languageFontConfig.DefaultFont;
        }

        public TMPSettings? GetTMPSettingsByLanguageType()
        {
            _settings.TryGetValue(_languageType, out var settings);
            return settings;
        }

        public void Dispose()
        {
            LocalizationManager.OnLocalizationChanged -= ChangeLanguageType;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ChangeLanguageType()
        {
            _languageType = _languageModel.GetLanguageType();
        }
    }
}