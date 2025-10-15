using System.Collections.Generic;
using UnityEngine;

namespace Code.Features.RoomUpgrade
{
    public class RoomUpgradeItem : MonoBehaviour
    {
        [SerializeField] private List<RoomUpgradeGroup> _groups;

        public List<RoomUpgradeGroup> GetGroupCount()
        {
            return _groups;
        }

        public void ActivateAllGroup()
        {
            foreach (var group in _groups)
            {
                group.ActivateGroup();
            }
        }

        public void ActivateMultipleGroup(int groupCount)
        {
            for (int i = 0; i < groupCount; i++)
            {
                _groups[i].ActivateGroup();
            }
        }
        
        public void ActivateGroupWithParticles(int groupCount)
        { 
            _groups[groupCount].ActivateGroupWithParticles();
        }
    }
}