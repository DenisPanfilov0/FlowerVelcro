using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Features.DailyTask
{
    public class DailyTaskItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _taskName;
        [SerializeField] private Slider _taskProgressbar;
        [SerializeField] private TMP_Text _taskProgressCount;
        [SerializeField] private TMP_Text _rewardAmount;
        [SerializeField] private Image _rewardType;
        [SerializeField] private Button _claimReward;
        [SerializeField] private GameObject _isRewardCollected;
        
        private Action<DailyTaskType> _rewardClaimed;
        private DailyTaskType _taskType;

        public void Setup(DailyTaskProgress taskProgress, Sprite rewardIcon, Action<DailyTaskType> rewardClaimed)
        {
            _rewardClaimed = rewardClaimed;
            
            _taskName.text = taskProgress.TaskName;
            _taskProgressbar.maxValue = taskProgress.MaxProgress;
            _taskProgressbar.value = taskProgress.CurrentProgress;
            _taskProgressCount.text = $"{taskProgress.CurrentProgress} / {taskProgress.MaxProgress}";

            if (!taskProgress.IsRewarded)
            {
                _rewardAmount.text = $"{taskProgress.RewardAmount}";
                _rewardType.sprite = rewardIcon;
                _claimReward.interactable = taskProgress.CurrentProgress >= taskProgress.MaxProgress;
            }
            else
            {
                DisableRewardButton();
            }

            _taskType = taskProgress.TaskType;
            
            _claimReward.onClick.AddListener(ClaimReward);
        }

        private void OnDestroy()
        {
            _claimReward.onClick.RemoveListener(ClaimReward);
        }

        public void UpdateValue(DailyTaskProgress taskProgress)
        {
            if (!taskProgress.IsRewarded)
            {
                _taskProgressCount.text = $"{taskProgress.CurrentProgress} / {taskProgress.MaxProgress}";
                _taskProgressbar.value = taskProgress.CurrentProgress;
                _claimReward.interactable = taskProgress.CurrentProgress >= taskProgress.MaxProgress;
            }
            else
            {
                DisableRewardButton();
            }
        }

        private void DisableRewardButton()
        {
            _rewardAmount.gameObject.SetActive(false);
            _rewardType.gameObject.SetActive(false);
            _claimReward.gameObject.SetActive(false);
            _isRewardCollected.SetActive(true);
        }

        private void ClaimReward()
        {
            // DisableRewardButton();
            _rewardClaimed?.Invoke(_taskType);
        }
    }
}