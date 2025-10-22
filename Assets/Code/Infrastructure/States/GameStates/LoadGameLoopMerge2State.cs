using Code.Infrastructure.Loading;
using Code.Infrastructure.States.StateInfrastructure;
using Code.Infrastructure.States.StateMachine;

namespace Code.Infrastructure.States.GameStates
{
    public class LoadGameLoopMerge2State : IState
    {
        private const string GameLoopSceneName = "Merge2Scene";
        private readonly IGameStateMachine _stateMachine;
        private readonly ISceneLoader _sceneLoader;

        public LoadGameLoopMerge2State(IGameStateMachine stateMachine, ISceneLoader sceneLoader)
        {
            _stateMachine = stateMachine;
            _sceneLoader = sceneLoader;
        }
    
        public void Enter()
        {
            _sceneLoader.LoadScene(GameLoopSceneName, EnterGameLoopState);
        }

        private void EnterGameLoopState()
        {
            _stateMachine.Enter<GameLoopState>();
        }

        public void Exit()
        {
        }
    }
}