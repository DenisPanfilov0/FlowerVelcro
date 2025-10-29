using Newtonsoft.Json;
// using UnityEngine;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Code
{
    public class SaveLoadService
    {
        private const string VersionKey = "SaveLoadServiceVersion";
        private const string Version = "107.0"; // Текущая версия сервиса
        private const string TutorialEndedKey = "TutorialEndedFlag";

        private bool _isTutorialEnded = false;

        public SaveLoadService()
        {
            LoadTutorialFlag();

            // Если туториал не завершён — очищаем все данные, чтобы не сохранять прогресс
            if (!_isTutorialEnded)
            {
                ClearAllData();
            }
            else
            {
                // Проверяем актуальность версии
                if (!CheckVersion())
                {
                    ClearAllData();
                }
            }
        }

        /// <summary>
        /// Сохраняет объект данных в PlayerPrefs.
        /// </summary>
        public void SaveData<T>(string key, T data)
        {
            // Если туториал не завершён — очищаем данные и не сохраняем
            if (!_isTutorialEnded)
            {
                ClearAllData();
                return;
            }

            string json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.SetString(VersionKey, Version);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загружает объект данных из PlayerPrefs.
        /// </summary>
        public T LoadData<T>(string key) where T : class
        {
            if (!_isTutorialEnded)
            {
                // Если туториал не завершён — всегда возвращаем null, чтобы не подгружать данные
                return null;
            }

            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (JsonException)
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Устанавливает флаг завершения туториала.
        /// </summary>
        public void SetTutorialEnded(bool isEnded)
        {
            _isTutorialEnded = isEnded;
            PlayerPrefs.SetInt(TutorialEndedKey, _isTutorialEnded ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Возвращает текущее состояние флага завершения туториала.
        /// </summary>
        public bool IsTutorialEnded()
        {
            return _isTutorialEnded;
        }

        /// <summary>
        /// Загружает состояние флага туториала из сохранений.
        /// </summary>
        private void LoadTutorialFlag()
        {
            if (PlayerPrefs.HasKey(TutorialEndedKey))
            {
                _isTutorialEnded = PlayerPrefs.GetInt(TutorialEndedKey) == 1;
            }
            else
            {
                _isTutorialEnded = false;
                PlayerPrefs.SetInt(TutorialEndedKey, 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Проверяет совпадение версии сохранений.
        /// </summary>
        private bool CheckVersion()
        {
            if (!PlayerPrefs.HasKey(VersionKey))
                return false;

            string savedVersion = PlayerPrefs.GetString(VersionKey);
            return savedVersion == Version;
        }

        /// <summary>
        /// Полностью очищает все сохранённые данные (используется при несоответствии версии или неокончённом туториале).
        /// </summary>
        private void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString(VersionKey, Version);
            PlayerPrefs.SetInt(TutorialEndedKey, _isTutorialEnded ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public interface ISaveLoad
    {
        void SaveData();
        void LoadData();
    }
}
