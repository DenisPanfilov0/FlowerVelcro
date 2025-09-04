using Assets.SimpleLocalization.Scripts;
using TMPro;
using UnityEngine;

namespace Code
{
    /// <summary>
    /// Localize TMP_Text component.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMP : MonoBehaviour
    {
        [SerializeField] private string LocalizationKey;
        private FontStyles _initialFontStyle; // Сохраняем исходный стиль

        private void Awake()
        {
            // Сохраняем исходный fontStyle при создании компонента
            TMP_Text tmpText = GetComponent<TMP_Text>();
            _initialFontStyle = tmpText.fontStyle;
        }

        private void Start()
        {
            Localize();
            LocalizationManager.OnLocalizationChanged += Localize;
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLocalizationChanged -= Localize;
        }

        private void Localize()
        {
            TMP_Text tmpText = GetComponent<TMP_Text>();
            tmpText.text = LocalizationManager.Localize(LocalizationKey);
            tmpText.font = LanguageFontService.Instance.GetFontByLanguageType();

            // Применяем настройки TMP_Text
            var settings = LanguageFontService.Instance.GetTMPSettingsByLanguageType();
            if (settings.HasValue && settings.Value.FontStyle.HasValue)
            {
                // Если есть специфичные настройки для языка (например, для японского), применяем их
                tmpText.fontStyle = settings.Value.FontStyle.Value;
            }
            else
            {
                // Для всех других языков восстанавливаем исходный стиль
                tmpText.fontStyle = _initialFontStyle;
            }
        }

        public void SetKey(string key)
        {
            LocalizationKey = key;
        }
    }
}