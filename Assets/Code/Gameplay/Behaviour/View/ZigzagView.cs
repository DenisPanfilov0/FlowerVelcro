using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerStickingService;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class ZigzagView : ItemView
    {
        [SerializeField] private float horizontalSpeed = 5f;
        [SerializeField] private float edgeMarginPercent = 0.05f;
        private float _screenWidth;
        private float _amplitude;
        private float _startTime;

        public override void Setup(IPlayerStickingService playerStickingService, Sprite icon, ItemSpawnerTypeId typeId, IHeartService heartService, IGameStateService gameStateService)
        {
            base.Setup(playerStickingService, icon, typeId, heartService, gameStateService);
            CalculateScreenBounds();
            Vector3 centerPos = transform.position;
            centerPos.x = Camera.main.transform.position.x;
            transform.position = centerPos;
            _startTime = Time.time;
        }

        public override void Reset()
        {
            base.Reset();
            _startTime = Time.time;
            if (Camera.main != null)
            {
                Vector3 centerPos = transform.position;
                centerPos.x = Camera.main.transform.position.x;
                transform.position = centerPos;
            }
        }

        private void CalculateScreenBounds()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float height = 2f * mainCamera.orthographicSize;
                _screenWidth = height * mainCamera.aspect;
                _amplitude = (_screenWidth / 2) * (1f - edgeMarginPercent);
            }
        }

        protected override void FixedUpdate()
        {
            if (_isFalling)
            {
                float t = Time.time - _startTime;
                float h_vel = _amplitude * horizontalSpeed * Mathf.Cos(t * horizontalSpeed);
                base.FixedUpdate();
                _rb.velocity = new Vector2(h_vel, _rb.velocity.y);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.GetComponent<PlayerView>())
            {
                _heartService?.DecreaseHeart();
                // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => _spawnerService?.ReturnToPool(this, _typeId)));
                _spawnerService.ReturnToPool(this, _typeId);
            }
        }
    }
}