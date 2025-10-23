using UnityEngine;

namespace Code.Gameplay.Behaviour
{
    public class CameraSizeAdjuster : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        private int _lastScreenWidth;
        private float _checkTimer;
        private const float CheckInterval = 0.1f; // Check every 0.1 seconds
        private const float ReferenceWidth = 1179f; // Reference device width
        private const float ReferenceHeight = 2556f; // Reference device height
        private const float ReferenceCameraSize = 7f; // Camera size for 1179x2556
        private const float NineBySixteenCameraSize = 7.4f; // Camera size for 9:16 aspect ratio
        private const float ReferenceAspectRatio = ReferenceWidth / ReferenceHeight; // ~0.461
        private const float NineBySixteenAspectRatio = 9f / 16f; // 0.5625
        private const float CorrectionFactor = 1.35f; // Correction factor for scaling

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            AdjustCameraSize();
            _lastScreenWidth = Screen.width;
        }

        private void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer < CheckInterval)
                return;

            _checkTimer = 0f;

            if (Screen.width != _lastScreenWidth)
            {
                AdjustCameraSize();
                _lastScreenWidth = Screen.width;
            }
        }

        private void AdjustCameraSize()
        {
            float currentWidth = Screen.width;
            float currentHeight = Screen.height;
            float currentAspectRatio = currentWidth / currentHeight;

            float targetCameraSize;

            if (Mathf.Approximately(currentWidth, ReferenceWidth) && Mathf.Approximately(currentHeight, ReferenceHeight))
            {
                // Exact match for reference resolution (1179x2556)
                targetCameraSize = ReferenceCameraSize;
            }
            else if (Mathf.Approximately(currentAspectRatio, NineBySixteenAspectRatio))
            {
                // Exact match for 9:16 aspect ratio
                targetCameraSize = NineBySixteenCameraSize;
            }
            else
            {
                // Interpolate/extrapolate based on aspect ratio
                float referenceOrthographicSize = ReferenceCameraSize;
                float widthScaleFactor = ReferenceWidth / currentWidth;
                float aspectRatioDifference = currentAspectRatio - ReferenceAspectRatio;
                float aspectAdjustment = Mathf.Lerp(ReferenceCameraSize, NineBySixteenCameraSize, 
                    Mathf.InverseLerp(ReferenceAspectRatio, NineBySixteenAspectRatio, currentAspectRatio));
                
                targetCameraSize = referenceOrthographicSize * widthScaleFactor * CorrectionFactor;
                targetCameraSize = Mathf.Lerp(targetCameraSize, aspectAdjustment, 0.5f); // Blend width-based and aspect-based adjustments
            }

            _camera.orthographicSize = Mathf.Clamp(targetCameraSize, 5f, 10f);
        }
    }
}