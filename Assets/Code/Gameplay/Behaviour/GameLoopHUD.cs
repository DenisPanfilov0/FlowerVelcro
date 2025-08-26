using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Gameplay.Behaviour
{
    public class GameLoopHUD : MonoBehaviour
    {
        // [SerializeField] private Button _mainMenuButton;
        
        private IGameStateMachine _stateMachine;

        [Inject]
        public void Construct(IGameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        private void Start()
        {
            // _mainMenuButton.onClick.AddListener(EnterMainMenu);
        }

        private void OnDestroy()
        {
            // _mainMenuButton.onClick.RemoveListener(EnterMainMenu);
        }

        private void EnterMainMenu()
        {
            _stateMachine.Enter<LoadMainMenuState>();
        }
    }
}