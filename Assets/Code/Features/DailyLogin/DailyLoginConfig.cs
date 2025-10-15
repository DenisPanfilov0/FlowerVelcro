using System.Collections.Generic;
using UnityEngine;

namespace Code.Features.DailyLogin
{
    [CreateAssetMenu(fileName = "DailyLoginConfig", menuName = "Configs/Daily Login Config")]
    public class DailyLoginConfig : ScriptableObject
    {
        public List<DailyLoginData> DailyLoginRewards;
    }
}