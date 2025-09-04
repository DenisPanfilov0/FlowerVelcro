using System;
using System.Collections.Generic;
using Code.Gameplay.Services.GameScore;
using Code.Gameplay.Services.Heart;
using Code.Infrastructure.Loading;
using Code.Progress.Data;
using UnityEngine;
using Zenject;

namespace Code.Tutorial
{
    public class TutorialStagChanger : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenuScene";
        
        [SerializeField] private List<StageTutorial> _stages;
        [SerializeField] private List<GameObject> _dialogueStages;
        private int _currentStage = 0;
        private GameScoreService _gameScoreService;
        private HeartService _heartService;
        private ISceneLoader _sceneLoader;
        private ProgressData _progressData;

        [Inject]
        public void Construct(GameScoreService gameScoreService, HeartService heartService, 
            ISceneLoader sceneLoader, ProgressData progressData)
        {
            _progressData = progressData;
            _sceneLoader = sceneLoader;
            _heartService = heartService;
            _gameScoreService = gameScoreService;
        }

        private void Start()
        {
            // _stages[_currentStage].gameObject.SetActive(true);
            _stages[_currentStage].StageActive();
            _dialogueStages[_currentStage].SetActive(true);

            _gameScoreService.ScoreChange += FlowerCollected;
            _heartService.HeartDecrease += StageRestart;
        }

        private void OnDestroy()
        {
            _gameScoreService.ScoreChange -= FlowerCollected;
            _heartService.HeartDecrease -= StageRestart;
        }

        private void FlowerCollected(int score)
        {
            _stages[_currentStage].FlowerCollected();
        }

        private void StageRestart()
        {
            _gameScoreService.ResetScore();
            _stages[_currentStage].StagRestart();
        }

        public void StageNext()
        {
            if (_currentStage < _stages.Count - 1)
            {
                _dialogueStages[_currentStage].SetActive(false);
                _currentStage++;
                _dialogueStages[_currentStage].SetActive(true);
                _stages[_currentStage].StageActive();
            }
            else
            {
                // _progressData.SetTutorialChecked(true);
                _sceneLoader.LoadScene(MainMenuSceneName);
            }
        }
    }
}