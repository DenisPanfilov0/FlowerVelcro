using Code.GlobalScreen.Behaviour;
using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Inventory;
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
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _openInventoryWindow;
        [SerializeField] private InventoryChanger _inventoryChanger;
        [SerializeField] private Button _openSettingWindow;
        [SerializeField] private SettingsWindow _settingWindow;
        [SerializeField] private GameObject _setingsPanel;
        [SerializeField] private TextMeshProUGUI _maxScore;
        [SerializeField] private TMP_Text _currency;
        
        private IGameStateMachine _stateMachine;
        private ProgressData _progress;
        private bool _isSettingsPanelOpen = false;
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
            _startButton.onClick.AddListener(EnterGameLoop);
            _openInventoryWindow.onClick.AddListener(OpenInventoryWindow);
            _openSettingWindow.onClick.AddListener(OpenSettingWindow);

            _maxScore.text = _progress.MaxScore.ToString();
            _currency.text = _currencyModel.GetCurrencyAmount().ToString();

            RectTransform rectTransform = _setingsPanel.GetComponent<RectTransform>();
            _initialPosition = rectTransform.anchoredPosition;
            _setingsPanel.SetActive(false);

            _currencyModel.AmountChanged += ChangePollenAmount;
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(EnterGameLoop);
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

        // private void OpenOrCloseSettingsPanel()
        // {
        //     RectTransform rectTransform = _setingsPanel.GetComponent<RectTransform>();
        //     float panelHeight = rectTransform.rect.height;
        //
        //     Vector2 abovePosition = _initialPosition + new Vector2(0, panelHeight);
        //     Vector2 originalPosition = _initialPosition;
        //
        //     if (!_isSettingsPanelOpen)
        //     {
        //         _setingsPanel.SetActive(true);
        //         rectTransform.anchoredPosition = abovePosition;
        //
        //         rectTransform.DOAnchorPos(originalPosition, 0.5f)
        //             .SetEase(Ease.OutQuad)
        //             .OnComplete(() =>
        //             {
        //                 _isSettingsPanelOpen = true;
        //             });
        //     }
        //     else
        //     {
        //         rectTransform.DOAnchorPos(abovePosition, 0.5f)
        //             .SetEase(Ease.InQuad)
        //             .OnComplete(() =>
        //             {
        //                 _setingsPanel.SetActive(false);
        //                 _isSettingsPanelOpen = false;
        //             });
        //     }
        // }

        private void ChangePollenAmount(int amount)
        {
            _currency.text = $"{amount}";
        }


        private void EnterGameLoop()
        {
            _audioManager.PlaySoundEffect(AudioClipTypeId.ButtonClick);
            _stateMachine.Enter<LoadGameLoopState>();
        }
    }
}