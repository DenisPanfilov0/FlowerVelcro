using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Code.Features.CanvasOfFantasy
{
    public class CanvasOfFantasyTools : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private Button _pencil;
        [SerializeField] private Button _eraser;
        [SerializeField] private CanvasOfFantasyWindow _canvasOfFantasyWindow;
        [SerializeField] private RawImage _drawSurface;

        [Header("Drawing Settings")]
        [SerializeField] private Color _brushColor = Color.white;
        [SerializeField, Range(1, 64)] private int _brushSize = 12;
        [SerializeField, Range(0.01f, 0.1f)] private float _applyInterval = 0.03f; // 30 FPS обновление текстуры

        private Texture2D _drawTexture;
        private bool _isDrawing;
        private bool _isPencilActive;
        private Color _drawColor;
        private Vector2Int _lastDrawPos;
        private bool _hasLastPos;
        private float _applyTimer;

        private Queue<Vector2Int> _drawQueue = new Queue<Vector2Int>();

        private void Start()
        {
            _pencil.onClick.AddListener(OnPencilClicked);
            _eraser.onClick.AddListener(OnEraserClicked);
            InitDrawingTexture();
        }

        private void Update()
        {
            if (_drawQueue.Count > 0)
            {
                _applyTimer += Time.deltaTime;
                if (_applyTimer >= _applyInterval)
                {
                    _drawTexture.Apply(false); // Apply без пересоздания мипмапов
                    _applyTimer = 0f;
                }
            }
        }

        private void OnDestroy()
        {
            _pencil.onClick.RemoveListener(OnPencilClicked);
            _eraser.onClick.RemoveListener(OnEraserClicked);
        }

        private void InitDrawingTexture()
        {
            RectTransform rt = _drawSurface.rectTransform;
            int width = Mathf.RoundToInt(rt.rect.width);
            int height = Mathf.RoundToInt(rt.rect.height);

            _drawTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _drawTexture.filterMode = FilterMode.Bilinear;
            ClearTexture();
            _drawSurface.texture = _drawTexture;
        }

        private void ClearTexture()
        {
            Color32[] clear = new Color32[_drawTexture.width * _drawTexture.height];
            for (int i = 0; i < clear.Length; i++)
                clear[i] = new Color32(0, 0, 0, 0);
            _drawTexture.SetPixels32(clear);
            _drawTexture.Apply();
        }

        private void OnPencilClicked()
        {
            _isPencilActive = !_isPencilActive;

            if (_isPencilActive)
            {
                _canvasOfFantasyWindow.DeselectCurrentSticker();
                _canvasOfFantasyWindow.DeselectCurrentInteractiveSticker();

                foreach (var sticker in _canvasOfFantasyWindow.GetComponentsInChildren<InteractiveStickerItem>())
                    sticker.DisableInteractivity();

                _pencil.image.color = Color.green;
                _drawColor = _brushColor;
            }
            else
            {
                foreach (var sticker in _canvasOfFantasyWindow.GetComponentsInChildren<InteractiveStickerItem>())
                    sticker.EnableInteractivity();

                _pencil.image.color = Color.white;
            }
        }

        private void OnEraserClicked()
        {
            _drawColor = _drawColor.a > 0 ? new Color(0, 0, 0, 0) : _brushColor;
            _eraser.image.color = _drawColor.a == 0 ? Color.green : Color.white;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isPencilActive) return;

            _isDrawing = true;
            _hasLastPos = false;
            DrawAt(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDrawing || !_isPencilActive) return;

            DrawAt(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDrawing = false;
            _hasLastPos = false;

            if (_drawQueue.Count > 0)
            {
                _drawTexture.Apply(false);
                _drawQueue.Clear();
            }
        }

        private void DrawAt(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _drawSurface.rectTransform, screenPosition, null, out var localPoint);

            Rect rect = _drawSurface.rectTransform.rect;
            int x = Mathf.RoundToInt((localPoint.x - rect.x) * _drawTexture.width / rect.width);
            int y = Mathf.RoundToInt((localPoint.y - rect.y) * _drawTexture.height / rect.height);
            Vector2Int currentPos = new Vector2Int(x, y);

            // Пропускаем слишком близкие точки (экономия)
            if (_hasLastPos && Vector2Int.Distance(_lastDrawPos, currentPos) < _brushSize * 0.25f)
                return;

            if (_hasLastPos)
            {
                Vector2 diff = currentPos - _lastDrawPos;
                int steps = Mathf.CeilToInt(diff.magnitude);
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    int ix = Mathf.RoundToInt(Mathf.Lerp(_lastDrawPos.x, currentPos.x, t));
                    int iy = Mathf.RoundToInt(Mathf.Lerp(_lastDrawPos.y, currentPos.y, t));
                    QueueCircle(ix, iy);
                }
            }
            else
            {
                QueueCircle(x, y);
                _hasLastPos = true;
            }

            _lastDrawPos = currentPos;
        }

        private void QueueCircle(int cx, int cy)
        {
            int r = _brushSize;
            int sqrR = r * r;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy <= sqrR)
                    {
                        int px = Mathf.Clamp(cx + dx, 0, _drawTexture.width - 1);
                        int py = Mathf.Clamp(cy + dy, 0, _drawTexture.height - 1);
                        _drawTexture.SetPixel(px, py, _drawColor);
                    }
                }
            }

            _drawQueue.Enqueue(new Vector2Int(cx, cy));
        }
    }
}
