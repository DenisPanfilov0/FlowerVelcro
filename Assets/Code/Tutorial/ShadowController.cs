using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.Tutorial
{
    [RequireComponent(typeof(Image))]
    public class ShadowController : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image holeImage;
        [SerializeField] private Button _updateFigure;

        private Image shadowImage;
        private RectTransform shadowRect;
        private Texture2D cachedShadowTexture;
        private Vector2 shadowScreenBottomLeft;
        private Vector2 shadowScreenSize;
        private Camera uiCamera;
        private Canvas parentCanvas;
        private bool isInitialized = false;
        private bool isShadowFullyVisible = false;

        private UnityAction updateButtonAction;

        public event Action OnTransparentAreaReleased;

        private void Awake()
        {
            shadowImage = GetComponent<Image>();
            shadowRect = shadowImage.GetComponent<RectTransform>();
            shadowImage.raycastTarget = true;

            parentCanvas = shadowRect.GetComponentInParent<Canvas>();
            uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? parentCanvas.worldCamera
                : null;

            updateButtonAction = () => RedrawShadowWithImageSafe(holeImage);
        }

        private void OnEnable()
        {
            if (_updateFigure != null)
                _updateFigure.onClick.AddListener(updateButtonAction);

            StartCoroutine(EnsureInitializedNextFrame());
        }

        private void OnDisable()
        {
            if (_updateFigure != null)
                _updateFigure.onClick.RemoveListener(updateButtonAction);
        }

        private void OnDestroy()
        {
            if (_updateFigure != null)
                _updateFigure.onClick.RemoveListener(updateButtonAction);

            if (cachedShadowTexture != null)
                Destroy(cachedShadowTexture);
        }

        private IEnumerator EnsureInitializedNextFrame()
        {
            if (isInitialized) yield break;
            yield return null;
            yield return EnsureInitialized();
        }

        private IEnumerator EnsureInitialized()
        {
            if (isInitialized) yield break;

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            shadowRect.GetWorldCorners(corners);
            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

            shadowScreenBottomLeft = screenBL;
            shadowScreenSize = new Vector2(Mathf.Abs(screenTR.x - screenBL.x), Mathf.Abs(screenTR.y - screenBL.y));

            int texW = Mathf.Max(1, Mathf.RoundToInt(shadowScreenSize.x));
            int texH = Mathf.Max(1, Mathf.RoundToInt(shadowScreenSize.y));

            if (cachedShadowTexture != null)
                Destroy(cachedShadowTexture);

            cachedShadowTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            FillShadowTexture();
            ApplyShadowTexture();

            isInitialized = true;

            yield return StartCoroutine(WaitForFullShadowVisibility());
        }

        private IEnumerator WaitForFullShadowVisibility()
        {
            isShadowFullyVisible = false;
            yield return null;

            float duration = 0.1f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            isShadowFullyVisible = true;
        }

        private void FillShadowTexture()
        {
            Color fillColor = shadowImage.color;
            Color[] fillPixels = new Color[cachedShadowTexture.width * cachedShadowTexture.height];
            for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;
            cachedShadowTexture.SetPixels(fillPixels);
        }

        public void FillShadowTextureFullyOpaque()
        {
            if (cachedShadowTexture == null) return;

            Color fillColor = shadowImage.color;
            fillColor.a = 1f;
            Color[] fillPixels = new Color[cachedShadowTexture.width * cachedShadowTexture.height];
            for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;
            cachedShadowTexture.SetPixels(fillPixels);
            cachedShadowTexture.Apply();
            shadowImage.sprite = Sprite.Create(cachedShadowTexture,
                new Rect(0, 0, cachedShadowTexture.width, cachedShadowTexture.height),
                shadowRect.pivot, 100f);
        }

        private void ApplyShadowTexture()
        {
            cachedShadowTexture.Apply();
            shadowImage.sprite = Sprite.Create(cachedShadowTexture,
                new Rect(0, 0, cachedShadowTexture.width, cachedShadowTexture.height),
                shadowRect.pivot, 100f);
        }

        public void RedrawShadowWithImageSafe(Image targetImage)
        {
            if (!isInitialized)
            {
                StartCoroutine(DelayedInitAndRedraw(targetImage));
                return;
            }
            RedrawShadowWithImage_Internal(targetImage);
        }

        private IEnumerator DelayedInitAndRedraw(Image target)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return EnsureInitialized();
            RedrawShadowWithImage_Internal(target);
        }

        private void RedrawShadowWithImage_Internal(Image targetImage)
        {
            if (targetImage == null || targetImage.sprite == null || cachedShadowTexture == null) return;

            FillShadowTexture();

            Texture2D holeTex = targetImage.sprite.texture;
            if (holeTex == null || !holeTex.isReadable)
            {
                Debug.LogWarning("Hole texture is not readable. Enable Read/Write in import settings.");
                return;
            }

            RectTransform holeRectTrans = targetImage.rectTransform;
            Vector3[] holeCornersWorld = new Vector3[4];
            holeRectTrans.GetWorldCorners(holeCornersWorld);
            Vector2 holeBL = RectTransformUtility.WorldToScreenPoint(uiCamera, holeCornersWorld[0]);
            Vector2 holeTR = RectTransformUtility.WorldToScreenPoint(uiCamera, holeCornersWorld[2]);
            Vector2 holeScreenSize = new Vector2(Mathf.Abs(holeTR.x - holeBL.x), Mathf.Abs(holeTR.y - holeBL.y));

            int startX = Mathf.Clamp(Mathf.RoundToInt(holeBL.x - shadowScreenBottomLeft.x), 0, cachedShadowTexture.width - 1);
            int startY = Mathf.Clamp(Mathf.RoundToInt(holeBL.y - shadowScreenBottomLeft.y), 0, cachedShadowTexture.height - 1);
            int holeW = Mathf.RoundToInt(holeScreenSize.x);
            int holeH = Mathf.RoundToInt(holeScreenSize.y);
            int endX = Mathf.Clamp(startX + holeW, 0, cachedShadowTexture.width);
            int endY = Mathf.Clamp(startY + holeH, 0, cachedShadowTexture.height);

            if (endX <= startX || endY <= startY) return;

            Rect spriteRect = targetImage.sprite.rect;
            Color[] holePixels = holeTex.GetPixels((int)spriteRect.x, (int)spriteRect.y,
                (int)spriteRect.width, (int)spriteRect.height);
            Color[] shadowPixels = cachedShadowTexture.GetPixels();

            int overlapW = endX - startX;
            int overlapH = endY - startY;

            for (int y = 0; y < overlapH; y++)
            {
                float v = y / (float)overlapH;
                int srcY = Mathf.Clamp(Mathf.RoundToInt(v * (spriteRect.height - 1)), 0, (int)spriteRect.height - 1);

                for (int x = 0; x < overlapW; x++)
                {
                    float u = x / (float)overlapW;
                    int srcX = Mathf.Clamp(Mathf.RoundToInt(u * (spriteRect.width - 1)), 0, (int)spriteRect.width - 1);
                    int holeIndex = srcY * (int)spriteRect.width + srcX;
                    float alpha = holePixels[holeIndex].a;

                    if (alpha > 0.01f)
                    {
                        int shadowIndex = (startY + y) * cachedShadowTexture.width + (startX + x);
                        Color c = shadowPixels[shadowIndex];
                        c.a *= 1f - alpha;
                        shadowPixels[shadowIndex] = c;
                    }
                }
            }

            cachedShadowTexture.SetPixels(shadowPixels);
            cachedShadowTexture.Apply();
            shadowImage.sprite = Sprite.Create(cachedShadowTexture,
                new Rect(0, 0, cachedShadowTexture.width, cachedShadowTexture.height),
                shadowRect.pivot, 100f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInitialized || !isShadowFullyVisible) return;

            Vector2 mousePos = eventData.position;
            if (!IsMouseInOpaqueArea(mousePos)) // проверяем вырез
            {
                Button foundButton = FindTopmostActiveButtonUnderPosition(mousePos);
                if (foundButton != null)
                {
                    foundButton.onClick?.Invoke();
                    OnTransparentAreaReleased?.Invoke();
                }
            }
        }

        private Button FindTopmostActiveButtonUnderPosition(Vector2 screenPos)
        {
            if (EventSystem.current == null) return null;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                if (result.gameObject == gameObject) continue;
                Button btn = result.gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
                    return btn;
            }

            return null;
        }

        public bool IsMouseInOpaqueArea(Vector2 mouse)
        {
            if (cachedShadowTexture == null) return false;

            Rect shadowScreenRect = new Rect(shadowScreenBottomLeft, shadowScreenSize);
            if (!shadowScreenRect.Contains(mouse)) return true;

            int pixelX = Mathf.Clamp(Mathf.FloorToInt(mouse.x - shadowScreenBottomLeft.x), 0, cachedShadowTexture.width - 1);
            int pixelY = Mathf.Clamp(Mathf.FloorToInt(mouse.y - shadowScreenBottomLeft.y), 0, cachedShadowTexture.height - 1);

            Color pixel = cachedShadowTexture.GetPixel(pixelX, pixelY);
            return pixel.a > 0.01f;
        }
    }
}
