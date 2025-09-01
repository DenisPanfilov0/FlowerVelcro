using Code.Infrastructure.Loading;
using Code.Infrastructure.States.StateInfrastructure;
using Code.Infrastructure.States.StateMachine;
using Code.Infrastructure.StaticData;
using Code.Progress.Data;

namespace Code.Infrastructure.States.GameStates
{
    public class BootstrapState : IState
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private const string TutorialSceneName = "TutorialScene";
        
        private readonly IGameStateMachine _stateMachine;
        private readonly IStaticDataService _staticDataService;
        private readonly ProgressData _progressData;
        private readonly ISceneLoader _sceneLoader;

        public BootstrapState(IGameStateMachine stateMachine, IStaticDataService staticDataService, ProgressData progressData, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _staticDataService = staticDataService;
            _progressData = progressData;
            _sceneLoader = sceneLoader;
        }
    
        public void Enter()
        {
            _staticDataService.LoadAll();

            if (_progressData.IsTutorialChecked)
            {
                // _stateMachine.Enter<LoadProgressState>();
                _sceneLoader.LoadScene(MainMenuSceneName/*, () => { _stateMachine.Enter<MainMenuState>(); }*/);
            }
            else
            {
                _sceneLoader.LoadScene(TutorialSceneName/*, () => { _stateMachine.Enter<MainMenuState>(); }*/);
            }
            
        }

        public void Exit()
        {
      
        }
    }
}