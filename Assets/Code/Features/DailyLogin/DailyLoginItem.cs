using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Features.DailyLogin
{
    public class DailyLoginItem : MonoBehaviour
    {
        [SerializeField] private Image _lockedBack;
        [SerializeField] private Image _unlockedBack;
        [SerializeField] private Image _rewardedBack;
        [SerializeField] private TMP_Text _rewardedText;
        
        [SerializeField] private Image _rewardIcon;
        [SerializeField] private TMP_Text _rewardAmount;

        [SerializeField] private Button _claimReward;
        private Action<int> _claimRewardAction;
        private int _idReward;

        public void Setup(DailyLoginProgress dailyLogin, Sprite loginDataRewardIcon, Action<int> claimRewardAction)
        {
            _idReward = dailyLogin.Id;
            _claimRewardAction = claimRewardAction;
            
            _rewardIcon.sprite = loginDataRewardIcon;
            _rewardAmount.text = $"{dailyLogin.RewardAmount}";
            
            if (dailyLogin.IsRewarded)
            {
                _rewardedBack.gameObject.SetActive(true);
                // _rewardedText.gameObject.SetActive(true);
            }
            else if (dailyLogin.IsCompleted)
            {
                _unlockedBack.gameObject.SetActive(true);
            }
            else
            {
                _lockedBack.gameObject.SetActive(true);
            }
        }

        public int GetIdReward()
        {
            return _idReward;
        }

        private void Start()
        {
            _claimReward.onClick.AddListener(ClaimReward);
        }

        private void OnDestroy()
        {
            _claimReward.onClick.RemoveListener(ClaimReward);
        }

        private void ClaimReward()
        {
            _claimRewardAction?.Invoke(_idReward);
        }

        public void UpdateValue(DailyLoginProgress dailyLogin)
        {
            if (dailyLogin.IsRewarded)
            {
                _rewardedBack.gameObject.SetActive(true);
                // _rewardedText.gameObject.SetActive(true);
            }
            else if (dailyLogin.IsCompleted)
            {
                _unlockedBack.gameObject.SetActive(true);
            }
            else
            {
                _lockedBack.gameObject.SetActive(true);
            }
        }
    }
}