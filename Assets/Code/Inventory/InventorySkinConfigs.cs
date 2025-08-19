using System.Collections.Generic;
using UnityEngine;

namespace Code.Inventory
{
    [CreateAssetMenu(menuName = "Configs/Inventory Skin Configs", fileName = "InventorySkinConfigs")]
    public class InventorySkinConfigs : ScriptableObject
    {
        public List<InventorySkinConfig> InventorySkinsConfigs;
    }
}