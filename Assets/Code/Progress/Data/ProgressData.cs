namespace Code.Progress.Data
{
    using UnityEngine;
    using Zenject;
    using System;

    [Serializable]
    public class ProgressData : IInitializable, IDisposable
    {
        private readonly SaveLoadService _saveLoadService;
        private const string SAVE_KEY = "ProgressData";

        public int MaxScore { get; private set; }
        public bool IsTutorialChecked { get; private set; }

        // [Inject]
        public ProgressData(SaveLoadService saveLoadService)
        {
            _saveLoadService = saveLoadService;
            MaxScore = 0;
            IsTutorialChecked = false;
        }

        public void Initialize()
        {
            LoadData();
        }

        public void Dispose()
        {
            SaveData();
        }

        public void ChangeMaxScore(int value)
        {
            MaxScore = value;
            SaveData(); // Сохраняем сразу после изменения
        }

        public void SetTutorialChecked(bool isChecked)
        {
            IsTutorialChecked = isChecked;
            SaveData(); // Сохраняем сразу после изменения
        }

        private void SaveData()
        {
            var data = new ProgressSaveData
            {
                MaxScore = MaxScore,
                IsTutorialChecked = IsTutorialChecked
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<ProgressSaveData>(SAVE_KEY);
            if (data != null)
            {
                MaxScore = data.MaxScore;
                IsTutorialChecked = data.IsTutorialChecked;
            }
            else
            {
                // Инициализация значений по умолчанию, если данных нет
                MaxScore = 0;
                IsTutorialChecked = false;
                SaveData(); // Сохраняем начальные значения
            }
        }
    }

    [Serializable]
    public class ProgressSaveData
    {
        public int MaxScore;
        public bool IsTutorialChecked;
    }
}