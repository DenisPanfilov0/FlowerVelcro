using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Features.DailyTask
{
    public class DailyTaskWindow : MonoBehaviour
    {
        [SerializeField] private Button _closeWindow;
        [SerializeField] private DailyTaskItem _dailyTaskItemPrefab;
        [SerializeField] private Transform _container;
        [SerializeField] private TMP_Text _timer;
        
        private DailyTaskModel _dailyTaskModel;
        private DailyTaskConfig _dailyTaskConfig;
        private Dictionary<DailyTaskType, DailyTaskItem> _taskItems = new();
        private LanguageModel _languageModel;

        [Inject]
        public void Construct(DailyTaskModel dailyTaskModel, DailyTaskConfig dailyTaskConfig, LanguageModel languageModel)
        {
            _languageModel = languageModel;
            _dailyTaskConfig = dailyTaskConfig;
            _dailyTaskModel = dailyTaskModel;
        }
        
        private void Start()
        {
            _timer.gameObject.SetActive(false);
            
            foreach (var taskProgress in _dailyTaskModel.GetTaskInProgress())
            {
                var taskItem = Instantiate(_dailyTaskItemPrefab, _container);
                DailyTaskType taskType = taskProgress.TaskType;
                
                _taskItems.Add(taskType, taskItem);
                
                taskItem.Setup(
                    taskProgress, 
                    _dailyTaskConfig.DailyTasks.FirstOrDefault(x => x.TaskType == taskProgress.TaskType).RewardIcon,
                    ClaimReward
                    );
            }

            _dailyTaskModel.DailyTaskChanged += DailyTaskUpdate;
            _dailyTaskModel.TimeChanged += UpdateTimer;
            _dailyTaskModel.DayChanged += ResetDailyTasks;
            
            _closeWindow.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ResetDailyTasks()
        {
            foreach (var view in _taskItems.Values)
            {
                Destroy(view.gameObject);
            }
            
            _taskItems.Clear();
            
            foreach (var taskProgress in _dailyTaskModel.GetTaskInProgress())
            {
                var taskItem = Instantiate(_dailyTaskItemPrefab, _container);
                DailyTaskType taskType = taskProgress.TaskType;
                
                _taskItems.Add(taskType, taskItem);
                
                taskItem.Setup(
                    taskProgress, 
                    _dailyTaskConfig.DailyTasks.FirstOrDefault(x => x.TaskType == taskProgress.TaskType).RewardIcon,
                    ClaimReward
                );
            }
        }

        private void DailyTaskUpdate(DailyTaskType taskType)
        {
            var taskProgress = _dailyTaskModel.GetTaskInProgress().FirstOrDefault(x => x.TaskType == taskType);
            _taskItems.TryGetValue(taskType, out var task);
            task.UpdateValue(taskProgress);
        }

        private void UpdateTimer(long time)
        {
            _timer.gameObject.SetActive(true);
            
            TimeSpan timeUntilMidnight = TimeSpan.FromMilliseconds(time);

            var (h, m, s, label) = TimeLocalization.Get(_languageModel.GetLanguageType());

            string formattedTime = $"{timeUntilMidnight.Hours:D2}{h} {timeUntilMidnight.Minutes:D2}{m} {timeUntilMidnight.Seconds:D2}{s}";

            _timer.text = $"{label}: {formattedTime}";
        }

        private void OnDestroy()
        {
            _closeWindow.onClick.RemoveListener(Hide);
            
            _dailyTaskModel.DailyTaskChanged -= DailyTaskUpdate;
            _dailyTaskModel.TimeChanged -= UpdateTimer;
            _dailyTaskModel.DayChanged -= ResetDailyTasks;
        }

        private void ClaimReward(DailyTaskType taskType)
        {
            _dailyTaskModel.ClaimReward(taskType);
        }
    }
}