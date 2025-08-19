using System;
using Zenject;

namespace Code.Inventory
{
    public class CurrencyModel : IInitializable
    {
        public event Action<int> AmountChanged; 
        
        private readonly CurrencyConfig _currencyConfig;
        private int _currencyAmount;

        public CurrencyModel(CurrencyConfig currencyConfig)
        {
            _currencyConfig = currencyConfig;
        }

        public void Initialize()
        {
            _currencyAmount = _currencyConfig.Amount;
        }

        public bool CanSpend(int amount)
        {
            return (_currencyAmount - amount >= 0);
        }

        public void AddCurrency(int amount)
        {
            if (CanSpend(amount))
            {
                _currencyAmount += amount;
                AmountChanged?.Invoke(_currencyAmount);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        
        public void SpendCurrency(int amount)
        {
            if (CanSpend(amount))
            {
                _currencyAmount -= amount;
                AmountChanged?.Invoke(_currencyAmount);
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}