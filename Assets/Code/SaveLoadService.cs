using Newtonsoft.Json;
// using UnityEngine;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Code
{
    public class SaveLoadService
    {
        private const string VersionKey = "SaveLoadServiceVersion";
        private const string Version = "5.10"; // Текущая версия сервиса

        public SaveLoadService()
        {
            // Проверка версии при инициализации сервиса
            if (!CheckVersion())
            {
                // Если версия не совпадает или отсутствует, удаляем все данные
                PlayerPrefs.DeleteAll();
                PlayerPrefs.SetString(VersionKey, Version); // Устанавливаем текущую версию
                PlayerPrefs.Save();
            }
        }

        public void SaveData<T>(string key, T data)
        {
            string json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.SetString(VersionKey, Version); // Сохраняем текущую версию
            PlayerPrefs.Save();
        }

        public T LoadData<T>(string key) where T : class
        {
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

        private bool CheckVersion()
        {
            if (!PlayerPrefs.HasKey(VersionKey))
            {
                return false; // Ключ версии отсутствует
            }

            string savedVersion = PlayerPrefs.GetString(VersionKey);
            return savedVersion == Version; // Проверяем совпадение версий
        }
    }

    public interface ISaveLoad
    {
        void SaveData();
        void LoadData();
    }
}