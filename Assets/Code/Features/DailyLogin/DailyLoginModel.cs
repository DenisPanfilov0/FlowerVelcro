using System;
using System.Collections.Generic;
using System.Linq;
using Code.Features.DailyTask;
using Code.Inventory;
using UnityEngine;
using YG;
using Zenject;

namespace Code.Features.DailyLogin
{
    [Serializable]
    public class DailyLoginSaveData
    {
        public List<DailyLoginProgress> Logins = new();
        public long LastUpdateTime;
    }

    public class DailyLoginModel : IInitializable, ITickable, IDisposable
    {
        private const string SAVE_KEY = "DailyLoginData";

        public event Action<int> DailyLoginChanged;
        public event Action<long> TimeChanged;

        private readonly DailyLoginConfig _dailyLoginConfig;
        private readonly CurrencyModel _currencyModel;
        private readonly SaveLoadService _saveLoadService;
        private List<DailyLoginProgress> _dailyLoginProgress = new();
        private long _serverTime;
        private float _elapsedTimeSinceLastTick;
        private TimeZoneInfo _moscowTimeZone;

        public DailyLoginModel(DailyLoginConfig dailyLoginConfig, CurrencyModel currencyModel, SaveLoadService saveLoadService)
        {
            _dailyLoginConfig = dailyLoginConfig;
            _currencyModel = currencyModel;
            _saveLoadService = saveLoadService;
            _moscowTimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow", TimeSpan.FromHours(3), "Moscow", "Moscow");
        }

        public void Initialize()
        {
            _serverTime = YG2.ServerTime();
            _elapsedTimeSinceLastTick = 0f;

            LoadData();
            UpdateDailyLoginProgress();
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

                TimeChanged?.Invoke(timeUntilMidnightMs); // Notify subscribers about time until next midnight

                if (timeUntilMidnightMs <= 1000)
                {
                    UpdateDailyLoginProgress();
                    SaveData();
                }

                _elapsedTimeSinceLastTick -= secondsPassed;
            }
        }

        public List<DailyLoginProgress> GetDailyLoginInProgress()
        {
            return _dailyLoginProgress;
        }

        public void ClaimReward(int idReward)
        {
            var dailyLogin = _dailyLoginProgress.FirstOrDefault(x => x.Id == idReward);

            if (dailyLogin != null && dailyLogin.IsCompleted && !dailyLogin.IsRewarded)
            {
                switch (dailyLogin.TypeReward)
                {
                    case TypeReward.Unknown:
                        break;
                    case TypeReward.MainCurrency:
                        _currencyModel.AddCurrency(dailyLogin.RewardAmount);
                        dailyLogin.IsRewarded = true;
                        break;
                    case TypeReward.Star:
                        _currencyModel.AddStarCurrency(dailyLogin.RewardAmount);
                        dailyLogin.IsRewarded = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                SaveData();
                DailyLoginChanged?.Invoke(idReward);
            }
        }

        private void UpdateDailyLoginProgress()
        {
            var data = _saveLoadService.LoadData<DailyLoginSaveData>(SAVE_KEY);
            DateTime currentMoscowTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTimeOffset.FromUnixTimeMilliseconds(_serverTime).UtcDateTime, _moscowTimeZone);

            if (data != null)
            {
                DateTime lastUpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(data.LastUpdateTime).UtcDateTime;
                lastUpdateTime = TimeZoneInfo.ConvertTimeFromUtc(lastUpdateTime, _moscowTimeZone);

                bool isSameDay = lastUpdateTime.Date == currentMoscowTime.Date;

                if (!isSameDay)
                {
                    var nextIncomplete = _dailyLoginProgress.FirstOrDefault(x => !x.IsCompleted);
                    if (nextIncomplete != null)
                    {
                        nextIncomplete.IsCompleted = true;
                        DailyLoginChanged?.Invoke(nextIncomplete.Id);
                    }
                    else
                    {
                        ResetDailyLogins();
                    }
                }
            }
            else
            {
                var firstLogin = _dailyLoginProgress.FirstOrDefault();
                if (firstLogin != null)
                {
                    firstLogin.IsCompleted = true;
                    DailyLoginChanged?.Invoke(firstLogin.Id);
                }
            }

            SaveData();
        }

        private void ResetDailyLogins()
        {
            _dailyLoginProgress.Clear();

            foreach (var dailyLogin in _dailyLoginConfig.DailyLoginRewards)
            {
                _dailyLoginProgress.Add(new DailyLoginProgress
                {
                    Id = dailyLogin.Id,
                    TypeReward = dailyLogin.TypeReward,
                    RewardAmount = dailyLogin.RewardAmount,
                    IsCompleted = false,
                    IsRewarded = false
                });
            }

            var firstLogin = _dailyLoginProgress.FirstOrDefault();
            if (firstLogin != null)
            {
                firstLogin.IsCompleted = true;
                DailyLoginChanged?.Invoke(firstLogin.Id);
            }
        }

        private void SaveData()
        {
            var data = new DailyLoginSaveData
            {
                Logins = _dailyLoginProgress,
                LastUpdateTime = _serverTime
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<DailyLoginSaveData>(SAVE_KEY);
            if (data != null)
            {
                _dailyLoginProgress = data.Logins;
            }
            else
            {
                ResetDailyLogins();
                SaveData();
            }
        }
    }

    [Serializable]
    public class DailyLoginProgress
    {
        public int Id;
        public TypeReward TypeReward;
        public int RewardAmount;
        public bool IsCompleted;
        public bool IsRewarded;
    }
}