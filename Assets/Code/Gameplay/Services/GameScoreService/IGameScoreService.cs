using System;

namespace Code.Gameplay.Services.GameScoreService
{
    public interface IGameScoreService
    {
        // void ScoreUpdate();
        void Cleanup();
        void IncreaseScore();
        event Action<int> ScoreChange;
    }
}