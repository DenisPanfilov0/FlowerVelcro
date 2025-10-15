using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.Features.CanvasOfFantasy
{
    public class InteractiveStickerItem : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _interactiveFrame;
        [SerializeField] private Button _scaleImage;
        [SerializeField] private Button _rotationImage;
        [SerializeField] private Button _deleteImage;
        [SerializeField] private Button _mirrorImage;
        [SerializeField] private Button _selectButton;
        
        [SerializeField] private Canvas _overlayCanvas; // Canvas для рамки и кнопок (сериализуемый)
        [SerializeField] private int _selectedSortingOrder = 100; // SortingOrder при выборе
        private int _originalSortingOrder; // Исходный SortingOrder для возврата

        private CanvasOfFantasyWindow _canvasOfFantasyWindow;
        private Vector2 _dragStartPosition;
        private Vector2 _dragStartLocalPosition;
        private Vector2 _scaleStartScreenPosition;
        private Vector2 _scaleDirection; // Direction from initial click to object center
        private float _initialScale;
        private float _initialRotation;
        private bool _isDragging;
        private bool _isMirrored;
        private bool _isScaleReversed; // Tracks if scale direction has flipped at minimum
        private RectTransform _canvasRectTransform; // Reference to the canvas
        private float _currentRotation; // Tracks current rotation for smoothing

        private void Awake()
        {
            if (_selectButton == null)
            {
                _selectButton = gameObject.GetComponent<Button>();
                if (_selectButton == null)
                {
                    _selectButton = gameObject.AddComponent<Button>();
                    _selectButton.transition = Selectable.Transition.None;
                }
            }

            AddEventTrigger(_selectButton, HandleSelectDrag);
            AddEventTrigger(_scaleImage, HandleScaleDrag);
            AddEventTrigger(_rotationImage, HandleRotationDrag);

            _canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        }

        private void Start()
        {
            _interactiveFrame.gameObject.SetActive(false);
            _scaleImage.gameObject.SetActive(false);
            _rotationImage.gameObject.SetActive(false);
            _deleteImage.gameObject.SetActive(false);
            _mirrorImage.gameObject.SetActive(false);

            _selectButton.onClick.AddListener(OnStickerClicked);
            _deleteImage.onClick.AddListener(OnDeleteClicked);
            _mirrorImage.onClick.AddListener(OnMirrorClicked);

            _isMirrored = false;
            _isScaleReversed = false;
            _currentRotation = 0f;
        }

        private void OnDestroy()
        {
            _selectButton.onClick.RemoveListener(OnStickerClicked);
            _deleteImage.onClick.RemoveListener(OnDeleteClicked);
            _mirrorImage.onClick.RemoveListener(OnMirrorClicked);
        }

        public void Setup(Sprite icon, CanvasOfFantasyWindow canvasOfFantasyWindow)
        {
            _icon.sprite = icon;
            _canvasOfFantasyWindow = canvasOfFantasyWindow;
        }

        public void Select()
        {
            _interactiveFrame.gameObject.SetActive(true);
            _scaleImage.gameObject.SetActive(true);
            _rotationImage.gameObject.SetActive(true);
            _deleteImage.gameObject.SetActive(true);
            _mirrorImage.gameObject.SetActive(true);

            // Поднимаем Canvas выше всех
            if (_overlayCanvas != null)
            {
                _overlayCanvas.overrideSorting = true; // включаем независимый порядок
                _originalSortingOrder = _overlayCanvas.sortingOrder; // сохраняем исходный порядок
                _overlayCanvas.sortingOrder = _selectedSortingOrder;
            }
        }

        public void Deselect()
        {
            _interactiveFrame.gameObject.SetActive(false);
            _scaleImage.gameObject.SetActive(false);
            _rotationImage.gameObject.SetActive(false);
            _deleteImage.gameObject.SetActive(false);
            _mirrorImage.gameObject.SetActive(false);
            _isDragging = false;

            // Возвращаем исходный SortingOrder Canvas
            if (_overlayCanvas != null)
            {
                _overlayCanvas.sortingOrder = _originalSortingOrder;
            }
        }

        public void EnableInteractivity()
        {
            _interactiveFrame.enabled = true;
            _scaleImage.enabled = true;
            _rotationImage.enabled = true;
            _deleteImage.enabled = true;
            _mirrorImage.enabled = true;
            _selectButton.enabled = true;
            _icon.raycastTarget = true;
        }

        public void DisableInteractivity()
        {
            _interactiveFrame.enabled = false;
            _scaleImage.enabled = false;
            _rotationImage.enabled = false;
            _deleteImage.enabled = false;
            _mirrorImage.enabled = false;
            _selectButton.enabled = false;
            _icon.raycastTarget = false;
        }

        private void OnStickerClicked()
        {
            _canvasOfFantasyWindow.OnInteractiveStickerSelected(this);
        }

        private void OnDeleteClicked()
        {
            _canvasOfFantasyWindow.DeselectCurrentInteractiveSticker();
            Destroy(gameObject);
        }

        private void OnMirrorClicked()
        {
            _isMirrored = !_isMirrored;
            _icon.transform.localRotation = Quaternion.Euler(0, _isMirrored ? 180 : 0, _currentRotation);
        }

        private void HandleSelectDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 || eventData.pointerId == 0) // Mouse or touch
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (!_isDragging)
                    {
                        _isDragging = true;
                        _dragStartPosition = eventData.position;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            _canvasRectTransform,
                            eventData.position,
                            null,
                            out _dragStartLocalPosition);
                    }

                    Vector2 currentLocalPosition;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRectTransform,
                        eventData.position,
                        null,
                        out currentLocalPosition);

                    Vector2 delta = currentLocalPosition - _dragStartLocalPosition;
                    transform.localPosition = transform.localPosition + (Vector3)delta;
                    _dragStartLocalPosition = currentLocalPosition;
                }
            }
            else if (_isDragging)
            {
                _isDragging = false;
            }
        }

        private bool _isScaling;

        private void HandleScaleDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 || eventData.pointerId == 0)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (eventData.dragging && !_isScaling)
                    {
                        _isScaling = true;
                        _initialScale = transform.localScale.x; // Сохраняем текущий масштаб
                        _scaleStartScreenPosition = eventData.position; // Стартовая позиция курсора
                    }

                    if (_isScaling)
                    {
                        // Разница между текущей и стартовой позицией по диагонали (чувствительность)
                        Vector2 delta = eventData.position - _scaleStartScreenPosition;
                        float distance = (delta.x + delta.y) * 0.0015f; // чувствительность 0.0015
                        float newScale = Mathf.Clamp(_initialScale + distance, 0.4f, 3f);

                        transform.localScale = new Vector3(newScale, newScale, newScale);
                    }
                }
            }
            else if (_isScaling)
            {
                _isScaling = false;
            }

            if (eventData.button == PointerEventData.InputButton.Left && eventData.dragging == false)
            {
                _isScaling = false;
            }
        }


        private void HandleRotationDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == -1 || eventData.pointerId == 0)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                {
                    if (!_isDragging)
                    {
                        _isDragging = true;
                        _initialRotation = _icon.transform.localRotation.eulerAngles.z;
                        _dragStartPosition = eventData.position;
                        _currentRotation = _initialRotation;
                    }

                    // Определяем угол между центром объекта и позицией курсора
                    Vector2 iconScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, _icon.transform.position);
                    Vector2 fromVector = _dragStartPosition - iconScreenPos;
                    Vector2 toVector = eventData.position - iconScreenPos;

                    float angle = Vector2.SignedAngle(fromVector, toVector);

                    // Вычисляем целевой угол
                    float targetRotation = _initialRotation + angle;

                    // Плавно интерполируем к новому углу
                    float smoothing = 0.15f; // Чем меньше — тем плавнее, но с "запаздыванием"
                    _currentRotation = Mathf.LerpAngle(_currentRotation, targetRotation, smoothing);

                    _icon.transform.localRotation = Quaternion.Euler(0, _isMirrored ? 180 : 0, _currentRotation);
                }
            }
            else if (_isDragging)
            {
                _isDragging = false;
            }
        }


        private void AddEventTrigger(Button button, System.Action<PointerEventData> dragHandler)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => dragHandler((PointerEventData)data));
            trigger.triggers.Add(beginDragEntry);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => dragHandler((PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            EventTrigger.Entry endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener((data) => dragHandler((PointerEventData)data));
            trigger.triggers.Add(endDragEntry);
        }
    }
}