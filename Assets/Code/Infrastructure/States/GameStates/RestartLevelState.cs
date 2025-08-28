using Code.Infrastructure.Loading;
using Code.Infrastructure.States.StateInfrastructure;
using Code.Infrastructure.States.StateMachine;

namespace Code.Infrastructure.States.GameStates
{
    public class RestartLevelState : IState
    {
        private const string RestartLevelSceneName = "RestartLevel";
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;

        public RestartLevelState(IGameStateMachine stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
        }
    
        public void Enter()
        {
            _sceneLoader.RestartScene(EnterRestartLevelState);
        }

        private void EnterRestartLevelState()
        {
            _stateMachine.Enter<GameLoopState>();
        }

        public void Exit()
        {
        }
    }
}