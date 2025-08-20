using System;

namespace Code.Gameplay.Services.GameScoreService
{
    public interface IGameScoreService
    {
        // void ScoreUpdate();
        void Cleanup();
        void IncreaseScore();
        int GetScore();
        event Action<int> ScoreChange;
    }
}