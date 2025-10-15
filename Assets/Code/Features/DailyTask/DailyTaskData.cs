using System;
using UnityEngine;

namespace Code.Features.DailyTask
{
    [Serializable]
    // [CreateAssetMenu(fileName = "DailyTaskData", menuName = "Configs/Daily Task Data")]
    public class DailyTaskData/* : ScriptableObject*/
    {
        public string TaskName;
        public DailyTaskType TaskType;
        public Sprite RewardIcon;
        public TypeReward TypeReward;
        public int RewardAmount;
        public int MaxProgress;
        public bool IsFinalTask;
    }

    public enum DailyTaskType
    {
        Unknown = 0,
        PlayGames3Points20,
        OpenImageChests2,
        CollectSugar900,
        CompleteAllTasks,
    }
    
    public enum TypeReward
    {
        Unknown = 0,
        MainCurrency = 1,
        Star = 2,
    }
}