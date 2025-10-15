using System;
using System.Collections.Generic;
using Code.Inventory;
using Zenject;

namespace Code.Features.RoomUpgrade
{
    [Serializable]
    public class RoomUpgradeSaveData
    {
        public int CurrentRoom;
        public int GroupOpened;
        public RoomUpgradeProgress RoomProgress; // Progress for each room
    }

    public class RoomUpgradeModel : IInitializable, ITickable/*, IDisposable*/
    {
        public event Action<int> RoomUpgraded;
        public event Action<int> OpenNextRoom;

        private readonly RoomUpgradeConfig _roomUpgradeConfig;
        private readonly CurrencyModel _currencyModel;
        private readonly SaveLoadService _saveLoadService;
        private RoomUpgradeProgress _roomProgress; // Progress for all rooms
        private int _currentRoom;
        private int _groupOpened;
        private bool _availableBuyButton;
        private int _costRoomUpgrade;
        private const string SAVE_KEY = "RoomUpgradeData";

        public RoomUpgradeModel(RoomUpgradeConfig roomUpgradeConfig, CurrencyModel currencyModel, SaveLoadService saveLoadService)
        {
            _roomUpgradeConfig = roomUpgradeConfig;
            _currencyModel = currencyModel;
            _saveLoadService = saveLoadService;
        }
        
        public void Initialize()
        {
            LoadData();

            if (!_roomProgress.AllRoomOpened)
            {
                _availableBuyButton = _currencyModel.CanStarSpend(_roomProgress.GroupProgress[_groupOpened].Price);
                _costRoomUpgrade = _roomProgress.GroupProgress[_groupOpened].Price;
            }
            else
            {
                // _availableBuyButton = _currencyModel.CanStarSpend(_roomProgress.GroupProgress[_groupOpened].Price);
                // _costRoomUpgrade = _roomProgress.GroupProgress[_groupOpened].Price;
            }
            
        }

        // public void Dispose()
        // {
        //     SaveData();
        // }

        public RoomUpgradeProgress GetRoomProgress()
        {
            return _roomProgress;
        }
        
        public RoomUpgradeItem GetRoomItem()
        {
            return _roomUpgradeConfig._roomUpgradeItems[_currentRoom];
        }

        public int GetGroupOpen()
        {
            if (!_roomProgress.AllRoomOpened)
            {
                return _groupOpened;
            }

            return _roomProgress.GroupProgress.Count;
        }

        public int GetPrice()
        {
            return _costRoomUpgrade;
            // return _roomProgress[_currentRoom].GroupProgress[_groupOpened].Price;
        }

        public void UpgradeRoom()
        {
            if (!_roomProgress.AllRoomOpened)
            {
                _currencyModel.SpendStarCurrency(_roomProgress.GroupProgress[_groupOpened].Price);
                
                _roomProgress.GroupProgress[_groupOpened].IsOpened = true;
                
                

                // SaveData(); // Save progress after upgrade

                if (_groupOpened + 1 < _roomProgress.GroupProgress.Count)
                {
                    _availableBuyButton = _currencyModel.CanStarSpend(_roomProgress.GroupProgress[_groupOpened + 1].Price);
                    _costRoomUpgrade = _roomProgress.GroupProgress[_groupOpened + 1].Price;
                    RoomUpgraded?.Invoke(_groupOpened);
                    
                    _groupOpened++;
                }
                else
                {
                    // _groupOpened = 0; // Reset for the next room
                    // SaveData(); // Save progress after opening new room
                    _roomProgress.AllRoomOpened = true;
                    OpenNextRoom?.Invoke(_groupOpened);
                }
                
                // _groupOpened++;
                
                SaveData(); // Save progress after upgrade

            }
        }

        public bool AvailableBuyButton()
        {
            return _availableBuyButton;
            // return _currencyModel.CanStarSpend(_roomProgress[_currentRoom].GroupProgress[_groupOpened].Price);
        }

        public int GetAvailableRoomsToOpen()
        {
            int a = 0;
            
            foreach (var groupProgress in _roomProgress.GroupProgress)
            {
                if (!groupProgress.IsOpened)
                {
                    a++;
                }
            }

            return a;
        }

        public void Tick()
        {
        }

        private void SaveData()
        {
            var data = new RoomUpgradeSaveData
            {
                CurrentRoom = _currentRoom,
                GroupOpened = _groupOpened,
                RoomProgress = _roomProgress
            };
            _saveLoadService.SaveData(SAVE_KEY, data);
        }

        private void LoadData()
        {
            var data = _saveLoadService.LoadData<RoomUpgradeSaveData>(SAVE_KEY);
            if (data != null)
            {
                _currentRoom = data.CurrentRoom;
                _groupOpened = data.GroupOpened;
                _roomProgress = data.RoomProgress;
            }
            else
            {
                // Initialize defaults
                _currentRoom = 0;
                _groupOpened = 0;
                _roomProgress = new RoomUpgradeProgress();

                // Initialize progress for all rooms in the config
                // for (int i = 0; i < _roomUpgradeConfig._roomUpgradeItems.Count; i++)
                // {
                    var groups = _roomUpgradeConfig._roomUpgradeItems[_currentRoom].GetGroupCount();
                    var roomProgress = new RoomUpgradeProgress
                    {
                        GroupProgress = new List<RoomUpgradeGroupProgress>()
                    };
                    
                    foreach (var group in groups)
                    {
                        roomProgress.GroupProgress.Add(new RoomUpgradeGroupProgress
                        {
                            Price = group.GetPrice(),
                            IsOpened = false
                        });
                    }
                    _roomProgress = roomProgress;
                // }
                SaveData(); // Save initial state
            }

            // Update _groupOpened based on loaded or initialized progress
            // _groupOpened = 0;
            // foreach (var groupProgress in _roomProgress[_currentRoom].GroupProgress)
            // {
            //     if (groupProgress.IsOpened)
            //     {
            //         _groupOpened++;
            //     }
            // }
        }
    }

    [Serializable]
    public class RoomUpgradeProgress
    {
        public List<RoomUpgradeGroupProgress> GroupProgress;
        public bool AllRoomOpened;
    }

    [Serializable]
    public class RoomUpgradeGroupProgress
    {
        public int Price;
        public bool IsOpened;
    }
}