using System;
using Zenject;

namespace Code.Inventory
{
    [Serializable]
    public class CurrencySaveData
    {
        public int CurrencyAmount;
    }

    public class CurrencyModel : IInitializable, IDisposable
    {
        public event Action<int> AmountChanged;

        private readonly CurrencyConfig _currencyConfig;
        private readonly SaveLoadService _saveLoadService;
        private int _currencyAmount;
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

        public void Dispose()
        {
            SaveData();
        }

        public bool CanSpend(int amount)
        {
            return _currencyAmount - amount >= 0;
        }

        public void AddCurrency(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Cannot add negative currency amount");
            }
            _currencyAmount += amount;
            AmountChanged?.Invoke(_currencyAmount);
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

        public int GetCurrencyAmount()
        {
            return _currencyAmount;
        }

        private void SaveData()
        {
            var data = new CurrencySaveData
            {
                CurrencyAmount = _currencyAmount
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<CurrencySaveData>(SAVE_KEY);
            if (data != null)
            {
                _currencyAmount = data.CurrencyAmount;
            }
            else
            {
                // Инициализация значения по умолчанию из конфига
                _currencyAmount = _currencyConfig.Amount;
                SaveData(); // Сохраняем начальное значение
            }
        }
    }
}