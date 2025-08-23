using Code.Infrastructure.Loading;
using Code.Infrastructure.States.StateInfrastructure;
using Code.Infrastructure.States.StateMachine;
using Code.Progress.Data;

namespace Code.Infrastructure.States.GameStates
{
    public class LoadMainMenuState : IState
    {
        private const string MainMenuSceneName = "MainMenuScene";
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;
        private readonly ProgressData _progress;

        public LoadMainMenuState(IGameStateMachine stateMachine, ISceneLoader sceneLoader, ProgressData progress)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
            _progress = progress;
        }
    
        public void Enter()
        {
            _sceneLoader.LoadScene(MainMenuSceneName, EnterMainMenuState);
        }

        private void EnterMainMenuState()
        {
            _stateMachine.Enter<MainMenuState>();
        }

        public void Exit()
        {
      
        }
    }
}