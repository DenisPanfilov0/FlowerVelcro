using System;

namespace Code.Gameplay.Services.GameScoreService
{
    public interface IGameScoreService
    {
        // void ScoreUpdate();
        void Cleanup();
        void IncreaseScore();
        int GetScore();
        bool CheckTheRecord();
        event Action<int> ScoreChange;
    }
}