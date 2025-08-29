using Newtonsoft.Json;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Code
{
    public class SaveLoadService
    {
        public SaveLoadService()
        {
            // PlayerPrefs.DeleteAll();
        }
        
        public void SaveData<T>(string key, T data)
        {
            string json = JsonConvert.SerializeObject(data);
            PlayerPrefs.SetString(key, json);
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
    }

    public interface ISaveLoad
    {
        void SaveData();
        void LoadData();
    }
}