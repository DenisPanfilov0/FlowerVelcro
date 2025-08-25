using YG;
using Zenject;

namespace Code.Leaderboards
{
    public class LeaderBoardModel : IInitializable
    {
        public void Initialize()
        {
            
        }

        public void SetLeaderboard(LeaderBoardType type, int value)
        {
            YG2.SetLeaderboard(type.ToString(), value);
        }
    }

    public enum LeaderBoardType
    {
        Unknown = 0,
        FVBestRecordAllTime = 1,
    }
}