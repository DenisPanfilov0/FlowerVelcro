using System.Collections.Generic;
using UnityEngine;

namespace Code.Features.RoomUpgrade
{
    [CreateAssetMenu(fileName = "RoomUpgradeConfig", menuName = "Configs/Room Upgrade Config")]
    public class RoomUpgradeConfig : ScriptableObject
    {
        public List<RoomUpgradeItem> _roomUpgradeItems;
    }
}