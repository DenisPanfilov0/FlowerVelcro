using Code.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Features.RoomUpgrade
{
    public class RoomUpgradeWindow : MonoBehaviour
    {
        [SerializeField] private Button _closeWindow;
        [SerializeField] private Button _nextRoom;
        [SerializeField] private TMP_Text _price;

        [SerializeField] private TMP_Text _starCurrency;
        
        [SerializeField] private Transform _container;
        private RoomUpgradeModel _roomUpgradeModel;

        private RoomUpgradeItem _roomItem;
        private CurrencyModel _currencyModel;

        [Inject]
        public void Construct(RoomUpgradeModel roomUpgradeModel, CurrencyModel currencyModel)
        {
            _currencyModel = currencyModel;
            _roomUpgradeModel = roomUpgradeModel;
        }
        
        private void Start()
        {
            var roomUpgradeProgress = _roomUpgradeModel.GetRoomProgress();
            var roomUpgradeItem = _roomUpgradeModel.GetRoomItem();

            _roomItem = Instantiate(roomUpgradeItem, _container);
            _roomItem.gameObject.transform.SetSiblingIndex(1);


            if (_roomUpgradeModel.GetAvailableRoomsToOpen() != 0)
            {
                _roomItem.ActivateMultipleGroup(_roomUpgradeModel.GetGroupOpen());
                _nextRoom.interactable = _roomUpgradeModel.AvailableBuyButton();
                _price.text = _roomUpgradeModel.GetPrice().ToString();
            }
            else
            {
                _roomItem.ActivateAllGroup();
                _nextRoom.gameObject.SetActive(false);
            }
            

            

            _starCurrency.text = _currencyModel.GetStarCurrency().ToString();
            
            _closeWindow.onClick.AddListener(Hide);
            _nextRoom.onClick.AddListener(UpgradeRoom);

            _roomUpgradeModel.RoomUpgraded += ActivateGroup;
            _roomUpgradeModel.OpenNextRoom += OpenNextRoom;
            _currencyModel.StarCurrencyChanged += ChangeStarCurrency;
        }

        private void OnDestroy()
        {
            _closeWindow.onClick.RemoveListener(Hide);
            _nextRoom.onClick.RemoveListener(UpgradeRoom);
            
            _roomUpgradeModel.RoomUpgraded -= ActivateGroup;
            _roomUpgradeModel.OpenNextRoom -= OpenNextRoom;
            _currencyModel.StarCurrencyChanged -= ChangeStarCurrency;
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ActivateGroup(int activeGroup)
        {
            _roomItem.ActivateGroupWithParticles(activeGroup);
            
            _nextRoom.interactable = _roomUpgradeModel.AvailableBuyButton();
            _price.text = _roomUpgradeModel.GetPrice().ToString();
        }

        private void OpenNextRoom(int activeGroup)
        {
            _roomItem.ActivateGroupWithParticles(activeGroup);
            
            _nextRoom.gameObject.SetActive(false);
        }
        
        private void UpgradeRoom()
        {
            _roomUpgradeModel.UpgradeRoom();
        }

        private void ChangeStarCurrency(int value)
        {
            _starCurrency.text = value.ToString();
            
            _nextRoom.interactable = _roomUpgradeModel.AvailableBuyButton();
        }
    }
}