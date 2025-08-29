using Code.Gameplay.Services.GameScore;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour.View
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _score;
        private GameScoreService _gameScoreService;

        [Inject]
        public void Construct(GameScoreService gameScoreService)
        {
            _gameScoreService = gameScoreService;
        }

        private void Start()
        {
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