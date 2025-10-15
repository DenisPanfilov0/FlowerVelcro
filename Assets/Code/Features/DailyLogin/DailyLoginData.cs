using System;
using Code.Features.DailyTask;
using UnityEngine;

namespace Code.Features.DailyLogin
{
    [Serializable]
    public class DailyLoginData
    {
        public int Id;
        public TypeReward TypeReward;
        public Sprite RewardIcon;
        public int RewardAmount;
    }
}