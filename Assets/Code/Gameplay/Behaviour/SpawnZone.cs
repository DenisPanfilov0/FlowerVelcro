using Code.Gameplay.Services.SpawnersServices;
using UnityEngine;
using Zenject;

namespace Code.Gameplay.Behaviour
{
    public class SpawnZone : MonoBehaviour
    {
        private BoxCollider2D _collider;
        private Camera _camera;

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
            UpdateColliderSize();
        }

        private void UpdateColliderSize()
        {
            float height = 2f * _camera.orthographicSize;
            float width = height * _camera.aspect;
            _collider.size = new Vector2(width, height);
        }
    }
}