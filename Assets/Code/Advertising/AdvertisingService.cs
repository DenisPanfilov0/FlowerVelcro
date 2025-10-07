using System;
using System.Threading.Tasks;
using UnityEngine;
using YG;
using Zenject;

namespace Code.Advertising
{
    public class AdvertisingService : IInitializable, ITickable
    {
        private const float FIXED_REWARD_TIME = 180f;
        private const float SECOND_INTERSTITIAL_TIME = 120f;
        
        private float _time;
        private TaskCompletionSource<bool> _rewardTaskCompletionSource;
        private bool _rewardReceived;
        private bool _firstInterstitialViewed;

        public void Initialize()
        {
            _firstInterstitialViewed = false;
            
            _time = FIXED_REWARD_TIME;

            YG2.onRewardAdv += RewardedAdv;
            YG2.onErrorRewardedAdv += CloseRewardedAdv;
        }

        private void RewardedAdv(string rewardId)
        {
            // Отмечаем, что награда получена
            _rewardReceived = true;
            _rewardTaskCompletionSource.TrySetResult(_rewardReceived);
            _rewardReceived = false;
        }

        private async void CloseRewardedAdv()
        {
            // Ждём 0.1 секунды после закрытия рекламы
            await Task.Delay(TimeSpan.FromSeconds(0.3));

            // Устанавливаем результат в зависимости от _rewardReceived
            if (_rewardTaskCompletionSource != null)
            {
                _rewardTaskCompletionSource.TrySetResult(_rewardReceived);
            }
        }

        public async Task<bool> AddReward(RewardType rewardType)
        {
            // Сбрасываем флаг награды и создаём TaskCompletionSource
            _rewardReceived = false;
            _rewardTaskCompletionSource = new TaskCompletionSource<bool>();

            // Показываем рекламу
            YG2.RewardedAdvShow(rewardType.ToString());

            // Ждём результата (завершается в CloseRewardedAdv после задержки)
            bool result = await _rewardTaskCompletionSource.Task;

            // Сбрасываем TaskCompletionSource
            _rewardTaskCompletionSource = null;

            // Если награда получена, сбрасываем таймер
            if (result)
            {
                _time = FIXED_REWARD_TIME;
            }

            return result;
        }

        public void AddInterstitial()
        {
            if (!_firstInterstitialViewed)
            {
                _firstInterstitialViewed = true;
                YG2.InterstitialAdvShow();
                _time = SECOND_INTERSTITIAL_TIME;
                return;
            }
            
            if (_time <= 0)
            {
                YG2.InterstitialAdvShow();
                _time = FIXED_REWARD_TIME;
            }
        }

        public void Tick()
        {
            if (_time > 0)
            {
                _time -= Time.deltaTime;
            }
        }
    }
    
    public enum RewardType
    {
        Unknown = 0,
        MultiplyReward = 1,
    }
}