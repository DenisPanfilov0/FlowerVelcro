using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Inventory;
using Code.Progress.Provider;
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
        [SerializeField] private Button _volumeSettingsButton;
        [SerializeField] private GameObject _setingsPanel;
        [SerializeField] private TextMeshProUGUI _maxScore;
        [SerializeField] private TMP_Text _currency;
        
        private IGameStateMachine _stateMachine;
        private IProgressProvider _progress;
        private bool _isSettingsPanelOpen = false;
        private Vector2 _initialPosition;
        private CurrencyModel _currencyModel;

        [Inject]
        public void Construct(IGameStateMachine stateMachine, IProgressProvider progress, CurrencyModel currencyModel)
        {
            _currencyModel = currencyModel;
            _progress = progress;
            _stateMachine = stateMachine;
        }

        private void Start()
        {
            _startButton.onClick.AddListener(EnterGameLoop);
            _volumeSettingsButton.onClick.AddListener(OpenOrCloseSettingsPanel);

            _maxScore.text = _progress.ProgressData.MaxScore.ToString();
            _currency.text = _currencyModel.GetCurrencyAmount().ToString();

            RectTransform rectTransform = _setingsPanel.GetComponent<RectTransform>();
            _initialPosition = rectTransform.anchoredPosition;
            _setingsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(EnterGameLoop);
            _volumeSettingsButton.onClick.RemoveListener(OpenOrCloseSettingsPanel);
        }

        private void OpenOrCloseSettingsPanel()
        {
            RectTransform rectTransform = _setingsPanel.GetComponent<RectTransform>();
            float panelHeight = rectTransform.rect.height;

            Vector2 abovePosition = _initialPosition + new Vector2(0, panelHeight);
            Vector2 originalPosition = _initialPosition;

            if (!_isSettingsPanelOpen)
            {
                _setingsPanel.SetActive(true);
                rectTransform.anchoredPosition = abovePosition;

                rectTransform.DOAnchorPos(originalPosition, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _isSettingsPanelOpen = true;
                    });
            }
            else
            {
                rectTransform.DOAnchorPos(abovePosition, 0.5f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        _setingsPanel.SetActive(false);
                        _isSettingsPanelOpen = false;
                    });
            }
        }


        private void EnterGameLoop()
        {
            _stateMachine.Enter<LoadGameLoopState>();
        }
    }
}