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
        public int TotalPollenCollected;
        public int TotalGamesPlayed;

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

        public void SaveData()
        {
            var data = new ProgressSaveData
            {
                MaxScore = MaxScore,
                IsTutorialChecked = IsTutorialChecked,
                TotalPollenCollected = TotalPollenCollected,
                TotalGamesPlayed = TotalGamesPlayed,
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
                TotalPollenCollected = data.TotalPollenCollected;
                TotalGamesPlayed = data.TotalGamesPlayed;
            }
            else
            {
                // Инициализация значений по умолчанию, если данных нет
                MaxScore = 0;
                IsTutorialChecked = false;
                TotalPollenCollected = 0;
                TotalGamesPlayed = 0;
                SaveData(); // Сохраняем начальные значения
            }
        }
    }

    [Serializable]
    public class ProgressSaveData
    {
        public int MaxScore;
        public bool IsTutorialChecked;
        public int TotalPollenCollected;
        public int TotalGamesPlayed;
    }
}