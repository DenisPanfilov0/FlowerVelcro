using System;
using Code.Features.DailyLogin;
using Code.Features.DailyTask;
using Zenject;

namespace Code.Inventory
{
    [Serializable]
    public class CurrencySaveData
    {
        public int CurrencyAmount;
        public int StarCurrency;
    }

    public class CurrencyModel : IInitializable, IDisposable
    {
        public event Action<int> AmountChanged;
        public event Action<int> StarCurrencyChanged;

        private readonly CurrencyConfig _currencyConfig;
        private readonly SaveLoadService _saveLoadService;
        private int _currencyAmount;
        private int _starCurrency;
        private DailyTaskModel _dailyTaskModel;
        private const string SAVE_KEY = "CurrencyData";

        [Inject]
        public CurrencyModel(CurrencyConfig currencyConfig, SaveLoadService saveLoadService)
        {
            _currencyConfig = currencyConfig;
            _saveLoadService = saveLoadService;
        }

        public void Initialize()
        {
            LoadData();
        }

        public void SetDailyTaskModel(DailyTaskModel dailyTaskModel)
        {
            _dailyTaskModel = dailyTaskModel;
        }

        public void Dispose()
        {
            SaveData();
        }

        public bool CanSpend(int amount)
        {
            return _currencyAmount - amount >= 0;
        }
        
        public bool CanStarSpend(int amount)
        {
            return _starCurrency - amount >= 0;
        }

        public void AddCurrency(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Cannot add negative currency amount");
            }
            _currencyAmount += amount;
            _dailyTaskModel.DailyTaskCheck(DailyTaskType.CollectSugar900, amount);
            AmountChanged?.Invoke(_currencyAmount);
            SaveData(); // Сохраняем сразу после изменения
        }
        
        public void AddStarCurrency(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Cannot add negative currency amount");
            }
            _starCurrency += amount;
            // _dailyTaskModel.DailyTaskCheck(DailyTaskType.CollectSugar900, amount);
            StarCurrencyChanged?.Invoke(_starCurrency);
            SaveData(); // Сохраняем сразу после изменения
        }

        public void SpendCurrency(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Cannot spend negative currency amount");
            }
            if (CanSpend(amount))
            {
                _currencyAmount -= amount;
                AmountChanged?.Invoke(_currencyAmount);
                SaveData(); // Сохраняем сразу после изменения
            }
            else
            {
                throw new ArgumentException("Not enough currency to spend");
            }
        }
        
        public void SpendStarCurrency(int amount)
        {
            if (CanStarSpend(amount))
            {
                _starCurrency -= amount;
                StarCurrencyChanged?.Invoke(_starCurrency);
                SaveData(); // Сохраняем сразу после изменения
            }
        }

        public int GetCurrencyAmount()
        {
            return _currencyAmount;
        }

        public int GetStarCurrency()
        {
            return _starCurrency;
        }

        private void SaveData()
        {
            var data = new CurrencySaveData
            {
                CurrencyAmount = _currencyAmount,
                StarCurrency = _starCurrency
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<CurrencySaveData>(SAVE_KEY);
            if (data != null)
            {
                _currencyAmount = data.CurrencyAmount;
                _starCurrency = data.StarCurrency;
            }
            else
            {
                // Инициализация значения по умолчанию из конфига
                _currencyAmount = _currencyConfig.Amount;
                _starCurrency = _currencyConfig.AmountStar;
                SaveData(); // Сохраняем начальное значение
            }
        }
    }
}