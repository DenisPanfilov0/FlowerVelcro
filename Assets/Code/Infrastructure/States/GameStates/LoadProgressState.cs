using Code.Infrastructure.States.StateInfrastructure;
using Code.Infrastructure.States.StateMachine;
using Code.Progress.Data;

namespace Code.Infrastructure.States.GameStates
{
    public class LoadProgressState : IState
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly ProgressData _progress;
        private readonly SaveLoadService _saveLoadService;

        public LoadProgressState(IGameStateMachine stateMachine, ProgressData progress, SaveLoadService saveLoadService)
        {
            _stateMachine = stateMachine;
            _progress = progress;
            _saveLoadService = saveLoadService;
        }
    
        public void Enter()
        {
            // CreateNewProgress();
            
            EnterMainMenuState();
        }

        private void EnterMainMenuState()
        {
            _stateMachine.Enter<LoadMainMenuState>();
        }

        public void Exit()
        {
      
        }
        
        // private void CreateNewProgress()
        // {
        //     _progress.SetProgressData(new ProgressData(_saveLoadService));
        // }
    }
}