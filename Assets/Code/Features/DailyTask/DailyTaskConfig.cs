using System.Collections.Generic;
using UnityEngine;

namespace Code.Features.DailyTask
{
    [CreateAssetMenu(fileName = "DailyTaskConfig", menuName = "Configs/Daily Task Config")]
    public class DailyTaskConfig : ScriptableObject
    {
        public List<DailyTaskData> DailyTasks;
    }
}