using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Features.DailyLogin
{
    public class DailyLoginWindow : MonoBehaviour
    {
        [SerializeField] private List<DailyLoginItem> _dailyLoginItems;
        [SerializeField] private Button _closeWindow;
        [SerializeField] private TMP_Text _timer;
        
        private DailyLoginModel _dailyLoginModel;
        private DailyLoginConfig _dailyLoginConfig;
        private LanguageModel _languageModel;

        [Inject]
        public void Construct(
            DailyLoginModel dailyLoginModel, 
            DailyLoginConfig dailyLoginConfig,
            LanguageModel languageModel)
        {
            _dailyLoginConfig = dailyLoginConfig;
            _dailyLoginModel = dailyLoginModel;
            _languageModel = languageModel;
        }

        private void Start()
        {
            _timer.gameObject.SetActive(false);
            
            var dailyLogins = _dailyLoginModel.GetDailyLoginInProgress();

            for (int i = 0; i < _dailyLoginItems.Count; i++)
            {
                var loginData = _dailyLoginConfig.DailyLoginRewards
                    .FirstOrDefault(x => x.Id == dailyLogins[i].Id);

                _dailyLoginItems[i].Setup(dailyLogins[i], loginData.RewardIcon, ClaimReward);
            }

            _dailyLoginModel.DailyLoginChanged += DailyLoginItemChange;
            _dailyLoginModel.TimeChanged += UpdateTimer;
            
            _closeWindow.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _dailyLoginModel.DailyLoginChanged -= DailyLoginItemChange;
            _dailyLoginModel.TimeChanged -= UpdateTimer;
            
            _closeWindow.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void UpdateTimer(long time)
        {
            _timer.gameObject.SetActive(true);
            
            TimeSpan timeUntilMidnight = TimeSpan.FromMilliseconds(time);

            var (h, m, s, label) = TimeLocalization.Get(_languageModel.GetLanguageType());

            string formattedTime = 
                $"{timeUntilMidnight.Hours:D2}{h} " +
                $"{timeUntilMidnight.Minutes:D2}{m} " +
                $"{timeUntilMidnight.Seconds:D2}{s}";

            _timer.text = $"{label}: {formattedTime}";
        }
        
        private void DailyLoginItemChange(int idReward)
        {
            _dailyLoginItems
                .FirstOrDefault(x => x.GetIdReward() == idReward)?
                .UpdateValue(_dailyLoginModel.GetDailyLoginInProgress()
                    .FirstOrDefault(x => x.Id == idReward));
        }

        private void ClaimReward(int idReward)
        {
            _dailyLoginModel.ClaimReward(idReward);
        }
    }
}
