using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Inventory
{
    [CreateAssetMenu(menuName = "Configs/Inventory Skin Config", fileName = "InventorySkinConfig")]
    public class InventorySkinConfig : ScriptableObject
    {
        public InventoryCategoryType Type;
        public List<InventorySkinData> InventorySkins;
    }

    [Serializable]
    public class InventorySkinData
    {
        public int SkinId;
        public Sprite Icon;
        public bool IsLocked; // Added field to track locked state
    }
}