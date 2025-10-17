using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Code.Features.CanvasOfFantasy
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasOfFantasyToolsRenderTexture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("UI & Tool References")]
        [SerializeField] private Button _pencil;
        [SerializeField] private Button _eraser;
        [SerializeField] private Button _clear;

        [Header("Duplicate Buttons")]
        [SerializeField] private Button _pencilDup;
        [SerializeField] private Button _eraserDup;
        [SerializeField] private Button _clearDup;

        [SerializeField] private CanvasOfFantasyWindow _canvasOfFantasyWindow;
        [SerializeField] private RawImage _drawSurface;
        [SerializeField] private Image stickersBackground;

        [Header("Brush Settings")]
        [SerializeField] private Color _brushColor = Color.white;
        [SerializeField, Range(1, 256)] private int _brushSizePixels = 15;
        [SerializeField, Range(0.0f, 1.0f)] private float _brushHardness = 0.9f;

        [Header("Brush Size Buttons")]
        [SerializeField] private Button _sizeSmall;
        [SerializeField] private Button _sizeMedium;
        [SerializeField] private Button _sizeLarge;
        [SerializeField] private Button _sizeXLarge;

        [Header("Color Buttons")]
        [SerializeField] private List<Button> _colorButtons = new List<Button>();
        [SerializeField] private List<Color> _availableColors = new List<Color>();

        [Header("UI Colors")]
        [SerializeField] private Color _selectedColorTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _activeToolColor = new Color(0f, 1f, 0f, 1f);
        [SerializeField] private Color _normalColor = Color.white;

        [Header("Selection Indicator Settings")]
        [SerializeField] private Sprite _selectionSprite;
        [SerializeField] private Color _selectionSpriteColor = Color.white;
        [SerializeField, Range(0.5f, 1f)] private float _selectionScaleFactor = 0.8f;

        private GameObject _activeColorIndicator;
        private GameObject _activeSizeIndicator;

        [Header("Performance")]
        [SerializeField, Tooltip("Масштаб разрешения рендертекстуры относительно UI: 1.0 = родной размер")]
        private float _resolutionScale = 1.0f;

        [Header("Shader / Material")]
        [SerializeField] private Material _brushMaterial;

        private RenderTexture _rtA;
        private RenderTexture _rtB;
        private RenderTexture _activeRT;

        private bool _isDrawing;
        private bool _pencilActive;
        private bool _eraserActive;
        private bool _isEraseMode;

        private Vector2 _lastLocalPos;
        private bool _hasLast;
        private Camera _uiCamera;
        private int _rtWidth;
        private int _rtHeight;

        private Button _activeColorButton;
        private Button _activeSizeButton;

        private void Start()
        {
            // Подписка на инструменты и дубляжи
            _pencil.onClick.AddListener(() => TogglePencil());
            _pencilDup.onClick.AddListener(() => TogglePencil());
            _eraser.onClick.AddListener(() => ToggleEraser());
            _eraserDup.onClick.AddListener(() => ToggleEraser());
            _clear.onClick.AddListener(() => ClearCanvas());
            _clearDup.onClick.AddListener(() => ClearCanvas());

            _sizeSmall.onClick.AddListener(() => OnSizeButtonClicked(_sizeSmall, 5));
            _sizeMedium.onClick.AddListener(() => OnSizeButtonClicked(_sizeMedium, 15));
            _sizeLarge.onClick.AddListener(() => OnSizeButtonClicked(_sizeLarge, 25));
            _sizeXLarge.onClick.AddListener(() => OnSizeButtonClicked(_sizeXLarge, 35));

            for (int i = 0; i < _colorButtons.Count; i++)
            {
                int index = i;
                if (index < _availableColors.Count)
                    _colorButtons[index].onClick.AddListener(() => OnColorButtonClicked(_colorButtons[index], _availableColors[index]));
            }

            _uiCamera = null;
            CreateOrResizeRTs();

            StartCoroutine(SetupInitialIndicators());

            // Подписка на выбор стикеров
            _canvasOfFantasyWindow.OnStickerSelectedEvent += OnStickerItemSelected;
        }

        private System.Collections.IEnumerator SetupInitialIndicators()
        {
            yield return new WaitForEndOfFrame();

            // Цвет
            if (_colorButtons.Count > 0 && _availableColors.Count > 0)
            {
                _activeColorButton = _colorButtons[0];
                _brushColor = _availableColors[0];
                _activeColorButton.interactable = false;
                _activeColorIndicator = CreateSelectionIndicator(_activeColorButton.transform);
            }

            // Размер
            if (_sizeMedium != null)
            {
                _activeSizeButton = _sizeMedium;
                _brushSizePixels = 15;
                _activeSizeButton.interactable = false;
                _activeSizeIndicator = CreateSelectionIndicator(_activeSizeButton.transform);
            }
        }

        private void OnDestroy()
        {
            _pencil.onClick.RemoveAllListeners();
            _pencilDup.onClick.RemoveAllListeners();
            _eraser.onClick.RemoveAllListeners();
            _eraserDup.onClick.RemoveAllListeners();
            _clear.onClick.RemoveAllListeners();
            _clearDup.onClick.RemoveAllListeners();

            _sizeSmall.onClick.RemoveAllListeners();
            _sizeMedium.onClick.RemoveAllListeners();
            _sizeLarge.onClick.RemoveAllListeners();
            _sizeXLarge.onClick.RemoveAllListeners();

            foreach (var b in _colorButtons)
                b.onClick.RemoveAllListeners();

            ReleaseRTs();

            if (_canvasOfFantasyWindow != null)
                _canvasOfFantasyWindow.OnStickerSelectedEvent -= OnStickerItemSelected;
        }

        #region --- Инструменты ---
        private void TogglePencil()
        {
            bool newState = !_pencilActive;
            _pencilActive = newState;
            _eraserActive = false;
            _isEraseMode = false;

            UpdateToolUI();
            UpdateDrawSurfaceRaycast();

            if (newState)
            {
                _canvasOfFantasyWindow.DeselectCurrentSticker();
            }
        }

        private void ToggleEraser()
        {
            bool newState = !_eraserActive;
            _eraserActive = newState;
            _pencilActive = false;
            _isEraseMode = newState;

            UpdateToolUI();
            UpdateDrawSurfaceRaycast();

            if (newState)
            {
                _canvasOfFantasyWindow.DeselectCurrentSticker();
            }
        }

        private void UpdateToolUI()
        {
            Color pencilColor = _pencilActive ? _activeToolColor : _normalColor;
            _pencil.image.color = pencilColor;
            _pencilDup.image.color = pencilColor;

            Color eraserColor = _eraserActive ? _activeToolColor : _normalColor;
            _eraser.image.color = eraserColor;
            _eraserDup.image.color = eraserColor;
        }

        private void UpdateDrawSurfaceRaycast()
        {
            _drawSurface.raycastTarget = _pencilActive || _eraserActive;
            stickersBackground.raycastTarget = !_drawSurface.raycastTarget;
        }
        #endregion

        #region --- Стикеры ---
        private void OnStickerItemSelected(StickerItem item)
        {
            _pencilActive = false;
            _eraserActive = false;
            _isEraseMode = false;
            UpdateToolUI();
            UpdateDrawSurfaceRaycast();
        }
        #endregion

        #region --- Цвета и размеры ---
        private void OnColorButtonClicked(Button button, Color color)
        {
            if (_activeColorButton == button) return;
            if (_activeColorIndicator != null) Destroy(_activeColorIndicator);
            if (_activeColorButton != null) _activeColorButton.interactable = true;

            _activeColorButton = button;
            _brushColor = color;
            _activeColorButton.interactable = false;

            _activeColorIndicator = CreateSelectionIndicator(button.transform);
        }

        private void OnSizeButtonClicked(Button button, int size)
        {
            if (_activeSizeButton == button) return;
            if (_activeSizeIndicator != null) Destroy(_activeSizeIndicator);
            if (_activeSizeButton != null) _activeSizeButton.interactable = true;

            _activeSizeButton = button;
            _brushSizePixels = size;
            _activeSizeButton.interactable = false;

            _activeSizeIndicator = CreateSelectionIndicator(button.transform);
        }

        private GameObject CreateSelectionIndicator(Transform parent)
        {
            if (_selectionSprite == null) return null;
            GameObject indicator = new GameObject("SelectionIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            indicator.transform.SetParent(parent, false);

            Image img = indicator.GetComponent<Image>();
            img.sprite = _selectionSprite;
            img.color = _selectionSpriteColor;
            img.raycastTarget = false;
            img.preserveAspect = true;

            RectTransform rt = indicator.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            rt.sizeDelta = parentRect.sizeDelta * _selectionScaleFactor;
            rt.anchoredPosition = Vector2.zero;

            return indicator;
        }
        #endregion

        #region --- Очистка ---
        public void ClearCanvas()
        {
            CreateOrResizeRTs();
            var old = RenderTexture.active;
            RenderTexture.active = _activeRT;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = old;
            _drawSurface.texture = _activeRT;
        }
        #endregion

        #region --- Рисование ---
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_pencilActive && !_eraserActive) return;
            CreateOrResizeRTs();
            _isDrawing = true;
            _hasLast = false;
            ProcessPointer(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDrawing || (!_pencilActive && !_eraserActive)) return;
            ProcessPointer(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pencilActive && !_eraserActive) return;
            _isDrawing = false;
            _hasLast = false;
        }

        private void ProcessPointer(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_drawSurface.rectTransform, screenPos, _uiCamera, out Vector2 localPoint);
            Rect rect = _drawSurface.rectTransform.rect;

            float u = (localPoint.x - rect.x) / rect.width;
            float v = (localPoint.y - rect.y) / rect.height;
            Vector2 uv = new Vector2(u, v);

            if (_hasLast)
            {
                float dist = Vector2.Distance(_lastLocalPos, localPoint);
                float step = Mathf.Max(1f, _brushSizePixels * 0.5f);
                int steps = Mathf.CeilToInt(dist / step);
                for (int i = 0; i <= steps; i++)
                {
                    Vector2 lerp = Vector2.Lerp(_lastLocalPos, localPoint, i / (float)steps);
                    DrawBrushAtUV(new Vector2((lerp.x - rect.x) / rect.width, (lerp.y - rect.y) / rect.height));
                }
            }
            else
            {
                DrawBrushAtUV(uv);
                _hasLast = true;
            }

            _lastLocalPos = localPoint;
        }

        private void DrawBrushAtUV(Vector2 uv)
        {
            if (_brushMaterial == null) return;

            RenderTexture target = (_activeRT == _rtA) ? _rtB : _rtA;

            float aspect = (float)_rtHeight / _rtWidth;
            Vector2 brushSizeUV = new Vector2(_brushSizePixels / (float)_rtWidth, _brushSizePixels / (float)_rtHeight);

            _brushMaterial.SetTexture("_MainTex", _activeRT);
            _brushMaterial.SetColor("_BrushColor", _brushColor);
            _brushMaterial.SetVector("_BrushPos", new Vector4(uv.x, uv.y, 0, 0));
            _brushMaterial.SetFloat("_BrushSize", brushSizeUV.x);
            _brushMaterial.SetFloat("_BrushHardness", Mathf.Clamp01(_brushHardness));
            _brushMaterial.SetFloat("_IsErase", _isEraseMode ? 1f : 0f);

            Graphics.Blit(_activeRT, target, _brushMaterial);
            _activeRT = target;
            _drawSurface.texture = _activeRT;
        }
        #endregion

        #region --- RenderTexture ---
        private void ReleaseRTs()
        {
            if (_rtA != null) { _rtA.Release(); Destroy(_rtA); _rtA = null; }
            if (_rtB != null) { _rtB.Release(); Destroy(_rtB); _rtB = null; }
            _activeRT = null;
        }

        private void CreateOrResizeRTs()
        {
            RectTransform rt = _drawSurface.rectTransform;
            int w = Mathf.Max(1, Mathf.RoundToInt(rt.rect.width * _resolutionScale));
            int h = Mathf.Max(1, Mathf.RoundToInt(rt.rect.height * _resolutionScale));

            if (_rtA != null && w == _rtWidth && h == _rtHeight)
                return;

            ReleaseRTs();
            _rtWidth = w;
            _rtHeight = h;

            RenderTextureDescriptor desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 0)
            {
                sRGB = true,
                useMipMap = false,
                autoGenerateMips = false
            };

            _rtA = new RenderTexture(desc) { filterMode = FilterMode.Bilinear };
            _rtB = new RenderTexture(desc) { filterMode = FilterMode.Bilinear };

            var clear = RenderTexture.active;
            RenderTexture.active = _rtA; GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = _rtB; GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = clear;

            _activeRT = _rtA;
            _drawSurface.texture = _activeRT;
        }
        #endregion
    }
}
