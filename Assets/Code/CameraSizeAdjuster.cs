using UnityEngine;

namespace Code.Gameplay.Behaviour
{
    [RequireComponent(typeof(Camera))]
    public class CameraSizeAdjuster : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _adjustSpeed = 2f; // скорость плавного изменения
        [SerializeField] private float _referenceWidth = 1179f;
        [SerializeField] private float _referenceHeight = 2556f;
        [SerializeField] private float _referenceCameraSize = 7f;
        [SerializeField] private float _minCameraSize = 5.5f;
        [SerializeField] private float _maxCameraSize = 7.5f;

        private float _targetCameraSize;
        private float _lastAspect;
        private const float CheckInterval = 0.2f;
        private float _timer;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;

            _lastAspect = GetCurrentAspectRatio();
            UpdateTargetCameraSize();
            _camera.orthographicSize = _targetCameraSize;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= CheckInterval)
            {
                _timer = 0f;
                float aspect = GetCurrentAspectRatio();
                if (!Mathf.Approximately(aspect, _lastAspect))
                {
                    _lastAspect = aspect;
                    UpdateTargetCameraSize();
                }
            }

            // Плавно двигаем текущий размер к целевому
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetCameraSize, Time.deltaTime * _adjustSpeed);
        }

        private void UpdateTargetCameraSize()
        {
            float currentAspect = GetCurrentAspectRatio();
            float referenceAspect = _referenceWidth / _referenceHeight;
            float ratio = Mathf.InverseLerp(referenceAspect, 9f / 16f, currentAspect);

            // Линейно интерполируем между 7 (при reference) и 6.7 (при 9:16)
            _targetCameraSize = Mathf.Lerp(_referenceCameraSize, 6.7f, ratio);
            _targetCameraSize = Mathf.Clamp(_targetCameraSize, _minCameraSize, _maxCameraSize);
        }

        private float GetCurrentAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }
    }
}
