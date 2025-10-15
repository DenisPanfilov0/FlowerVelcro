using UnityEngine;

namespace Code.Inventory
{
    [CreateAssetMenu(menuName = "Configs / Currency Config", fileName = "CurrencyConfig")]
    public class CurrencyConfig : ScriptableObject
    {
        public Sprite Icon;
        public int Amount;

        public Sprite StarCurrency;
        public int AmountStar;
    }
}