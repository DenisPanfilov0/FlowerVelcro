using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Inventory;
using Code.Leaderboards;
using Code.Progress.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.MainMenu.Behaviour
{
    public class MainMenuHUD : MonoBehaviour
    {
        [SerializeField] private Button _openLBWindow;
        [SerializeField] private LeaderboardWindow _lbWindow;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _startMerge2Button;
        [SerializeField] private Button _openInventoryWindow;
        [SerializeField] private InventoryChanger _inventoryChanger;
        [SerializeField] private Button _openSettingWindow;
        [SerializeField] private SettingsWindow _settingWindow;
        [SerializeField] private TextMeshProUGUI _maxScore;
        [SerializeField] private TMP_Text _currency;
        
        private IGameStateMachine _stateMachine;
        private ProgressData _progress;
        // private bool _isSettingsPanelOpen = false;
        private Vector2 _initialPosition;
        private CurrencyModel _currencyModel;
        private AudioManager _audioManager;

        [Inject]
        public void Construct(IGameStateMachine stateMachine, ProgressData progress, CurrencyModel currencyModel, AudioManager audioManager)
        {
            _audioManager = audioManager;
            _currencyModel = currencyModel;
            _progress = progress;
            _stateMachine = stateMachine;
        }

        private void Start()
        {
            _openLBWindow.onClick.AddListener(OpenLeaderboardWindow);
            _startButton.onClick.AddListener(EnterGameLoop);
            _startMerge2Button.onClick.AddListener(EnterGameLoopMerge2);
            _openInventoryWindow.onClick.AddListener(OpenInventoryWindow);
            _openSettingWindow.onClick.AddListener(OpenSettingWindow);

            _maxScore.text = _progress.MaxScore.ToString();
            _currency.text = _currencyModel.GetCurrencyAmount().ToString();

            _currencyModel.AmountChanged += ChangePollenAmount;
        }

        private void OnDestroy()
        {
            _openLBWindow.onClick.RemoveListener(OpenLeaderboardWindow);
            _startButton.onClick.RemoveListener(EnterGameLoop);
            _startMerge2Button.onClick.RemoveListener(EnterGameLoopMerge2);
            _openInventoryWindow.onClick.RemoveListener(OpenInventoryWindow);
            _openSettingWindow.onClick.RemoveListener(OpenSettingWindow);
            
            _currencyModel.AmountChanged -= ChangePollenAmount;
        }

        public void OpenInventoryWindow()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _inventoryChanger.Show();
        }
        
        public void OpenSettingWindow()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _settingWindow.Show();
        }

        private void ChangePollenAmount(int amount)
        {
            _currency.text = $"{amount}";
        }


        private void EnterGameLoop()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _stateMachine.Enter<LoadGameLoopState>();
        }
        
        private void EnterGameLoopMerge2()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _stateMachine.Enter<LoadGameLoopMerge2State>();
        }

        private void OpenLeaderboardWindow()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _lbWindow.Show();
        }
    }
}