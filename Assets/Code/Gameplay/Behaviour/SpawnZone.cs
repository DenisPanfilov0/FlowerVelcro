using Code.Gameplay.Services.SpawnersServices.BombSpawnerService;
using Code.Gameplay.Services.SpawnersServices.HeartSpawnerService;
using Code.Gameplay.Services.SpawnersServices.SlimeSpawnerService;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour
{
    public class SpawnZone : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private Camera _camera;

        [Inject]
        public void Construct(IBombSpawnerService bombSpawner, IHeartSpawnerService heartSpawner, ISlimeSpawnerService slimeSpawner, Camera mainCamera)
        {
            bombSpawner.StartSpawn(transform);
            heartSpawner.StartSpawn(transform);
            slimeSpawner.StartSpawn(transform);
            _camera = mainCamera;
        }

        private void Start()
        {
            // _mainCamera = Camera.main;
            _collider = gameObject.GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            UpdateColliderSize();
        }

        private void UpdateColliderSize()
        {
            float height = 2f * _camera.orthographicSize; // Высота камеры
            float width = height * _camera.aspect; // Ширина с учётом соотношения сторон
            _collider.size = new Vector2(width, height);
        }

        // private void OnValidate()
        // {
        //     UpdateColliderSize(); // Для редактора
        // }
    }
}