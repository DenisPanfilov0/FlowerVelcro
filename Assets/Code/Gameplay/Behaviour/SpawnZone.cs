using Code.Gameplay.Services.SpawnersServices;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour
{
    public class SpawnZone : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private Camera _camera;

        private const float ReferenceWidth = 1179f; // Фиксированная референсная ширина в пикселях (ваш телефон)

        [Inject]
        public void Construct(ItemSpawnerService itemSpawner, Camera mainCamera)
        {
            itemSpawner.StartSpawn(transform);
            _camera = mainCamera;
        }

        private void Start()
        {
            _collider = gameObject.GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            AdjustCameraSize();
            UpdateColliderSize();
        }

        private void AdjustCameraSize()
        {
            // Берем референсный orthographicSize с текущей камеры (из инспектора)
            float referenceOrthographicSize = _camera.orthographicSize;

            // Текущая ширина экрана в пикселях
            float currentWidth = Screen.width;

            // Корректирующий фактор для достижения 34 при 2028
            float correctionFactor = 1.35f;

            // Корректируем orthographicSize с учетом ширины и коэффициента
            float targetOrthographicSize = referenceOrthographicSize * (ReferenceWidth / currentWidth) * correctionFactor;

            // Применяем с ограничением диапазона
            _camera.orthographicSize = Mathf.Clamp(targetOrthographicSize, 20f, 50f);
        }

        private void UpdateColliderSize()
        {
            float height = 2f * _camera.orthographicSize;
            float width = height * _camera.aspect;
            _collider.size = new Vector2(width, height);
        }
    }
}