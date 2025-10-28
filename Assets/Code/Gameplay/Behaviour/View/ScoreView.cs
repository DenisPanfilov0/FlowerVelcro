using Code.Gameplay.Services.GameScore;
using Code.Progress.Data;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _score;
        [SerializeField] private GameObject _settingButton;
        private GameScoreService _gameScoreService;
        private ProgressData _progressData;

        [Inject]
        public void Construct(GameScoreService gameScoreService, ProgressData progressData)
        {
            _progressData = progressData;
            _gameScoreService = gameScoreService;
        }

        private void Start()
        {
            if (!_progressData.IsFeatureOpened)
            {
                _settingButton.SetActive(false);
            }
            
            _gameScoreService.ScoreChange += ScoreUpdate;

            _score.text = "0";
        }

        private void OnDestroy()
        {
            _gameScoreService.ScoreChange -= ScoreUpdate;
        }

        private void ScoreUpdate(int value)
        {
            _score.text = value.ToString();
        }
    }
}