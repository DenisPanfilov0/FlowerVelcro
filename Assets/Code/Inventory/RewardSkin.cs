using UnityEngine;
using UnityEngine.UI;

namespace Code.Inventory
{
    public class RewardSkin : MonoBehaviour
    {
        [SerializeField] private Image _skin;

        public void Setup(Sprite skin)
        {
            _skin.sprite = skin;
        }
    }
}