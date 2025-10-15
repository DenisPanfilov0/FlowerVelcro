using System;
using System.Collections.Generic;
using System.Linq;
using Code.Inventory;
using UnityEngine;
using YG;
using Zenject;

namespace Code.Features.DailyTask
{
    [Serializable]
    public class DailyTaskSaveData
    {
        public List<DailyTaskProgress> Tasks = new();
        public long LastResetTime;
    }

    public class DailyTaskModel : IInitializable, ITickable, IDisposable
    {
        private const string SAVE_KEY = "DailyTaskData";
        
        public event Action<DailyTaskType> DailyTaskChanged;
        public event Action<long> TimeChanged;
        public event Action DayChanged;

        private List<DailyTaskProgress> _tasksProgress = new();
        private readonly DailyTaskConfig _dailyTaskConfig;
        private readonly CurrencyModel _currencyModel;
        private readonly SaveLoadService _saveLoadService;

        private long _serverTime;
        private float _elapsedTimeSinceLastTick;
        private TimeZoneInfo _moscowTimeZone;

        public DailyTaskModel(DailyTaskConfig dailyTaskConfig, CurrencyModel currencyModel, SaveLoadService saveLoadService)
        {
            _dailyTaskConfig = dailyTaskConfig;
            _currencyModel = currencyModel;
            _saveLoadService = saveLoadService;
            _moscowTimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow", TimeSpan.FromHours(3), "Moscow", "Moscow");
        }
        
        public void Initialize()
        {
            _serverTime = YG2.ServerTime();
            _elapsedTimeSinceLastTick = 0f;
            _currencyModel.SetDailyTaskModel(this);
            
            LoadData();
        }

        public void Dispose()
        {
            SaveData();
        }

        public void Tick()
        {
            _elapsedTimeSinceLastTick += Time.deltaTime;

            if (_elapsedTimeSinceLastTick >= 1f)
            {
                int secondsPassed = Mathf.FloorToInt(_elapsedTimeSinceLastTick);
                _serverTime += secondsPassed * 1000;

                DateTime utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(_serverTime).UtcDateTime;
                DateTime moscowDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _moscowTimeZone);
                DateTime nextMidnight = moscowDateTime.Date.AddDays(1);
                TimeSpan timeUntilMidnight = nextMidnight - moscowDateTime;

                long timeUntilMidnightMs = (long)timeUntilMidnight.TotalMilliseconds;

                TimeChanged?.Invoke(timeUntilMidnightMs);
                
                if (timeUntilMidnightMs <= 1000)
                {
                    ResetDailyTasks();
                    SaveData(); // Сохраняем после сброса
                    DayChanged?.Invoke();
                }

                _elapsedTimeSinceLastTick -= secondsPassed;
            }
        }

        public List<DailyTaskProgress> GetTaskInProgress()
        {
            return _tasksProgress;
        }
        
        public void DailyTaskCheck(DailyTaskType taskType, int count)
        {
            var task = _tasksProgress.FirstOrDefault(x => x.TaskType == taskType);

            if (task != null)
            {
                task.CurrentProgress += count;

                if (task.CurrentProgress > task.MaxProgress)
                {
                    task.CurrentProgress = task.MaxProgress;
                }

                SaveData(); // Сохраняем после изменения прогресса
            }
            
            DailyTaskChanged?.Invoke(taskType);
        }

        public void ClaimReward(DailyTaskType taskType)
        {
            var taskProgress = _tasksProgress.FirstOrDefault(x => x.TaskType == taskType);
            var type = taskProgress.TypeReward;

            switch (type)
            {
                case TypeReward.Unknown:
                    break;
                case TypeReward.MainCurrency:
                    _currencyModel.AddCurrency(taskProgress.RewardAmount);
                    taskProgress.IsRewarded = true;
                    break;
                case TypeReward.Star:
                    _currencyModel.AddStarCurrency(taskProgress.RewardAmount);
                    taskProgress.IsRewarded = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            DailyTaskCheck(DailyTaskType.CompleteAllTasks, 1);
            SaveData(); // Сохраняем после получения награды
            
            DailyTaskChanged?.Invoke(taskType);
        }

        private void ResetDailyTasks()
        {
            _tasksProgress.Clear();
            
            foreach (var task in _dailyTaskConfig.DailyTasks)
            {
                _tasksProgress.Add(new DailyTaskProgress
                {
                    TaskName = task.TaskName,
                    TaskType = task.TaskType,
                    TypeReward = task.TypeReward,
                    RewardAmount = task.RewardAmount,
                    CurrentProgress = 0,
                    MaxProgress = task.MaxProgress,
                    IsRewarded = false
                });
            }
        }

        private void SaveData()
        {
            var data = new DailyTaskSaveData
            {
                Tasks = _tasksProgress,
                LastResetTime = _serverTime // Сохраняем текущее серверное время
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<DailyTaskSaveData>(SAVE_KEY);
            if (data != null)
            {
                // Преобразуем время последнего сброса в московское время
                DateTime lastResetTime = DateTimeOffset.FromUnixTimeMilliseconds(data.LastResetTime).UtcDateTime;
                lastResetTime = TimeZoneInfo.ConvertTimeFromUtc(lastResetTime, _moscowTimeZone);
                
                // Текущее время в MSK
                DateTime currentMoscowTime = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTimeOffset.FromUnixTimeMilliseconds(_serverTime).UtcDateTime, _moscowTimeZone);

                // Проверяем, принадлежат ли задачи текущему дню
                bool isSameDay = lastResetTime.Date == currentMoscowTime.Date;

                if (isSameDay)
                {
                    // Задачи актуальны, загружаем их
                    _tasksProgress = data.Tasks;
                }
                else
                {
                    // Задачи истекли, создаем новые
                    ResetDailyTasks();
                    SaveData(); // Сохраняем новые задачи
                }
            }
            else
            {
                // Нет сохраненных данных, создаем новые задачи
                ResetDailyTasks();
                SaveData(); // Сохраняем начальные задачи
            }
        }
    }
    
    [Serializable]
    public class DailyTaskProgress
    {
        public string TaskName;
        public DailyTaskType TaskType;
        public TypeReward TypeReward;
        public int RewardAmount;
        public int CurrentProgress;
        public int MaxProgress;
        public bool IsRewarded;
    }
}