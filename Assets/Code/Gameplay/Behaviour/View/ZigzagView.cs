using Code.Configs.ItemSpawnerConfig;
// using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
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

        public override void Setup(PlayerStickingService playerStickingService, Sprite icon, 
            HeartService heartService, IGameStateService gameStateService, bool isTutorial)
        {
            base.Setup(playerStickingService, icon, heartService, gameStateService, isTutorial);
            CalculateScreenBounds();
            Vector3 centerPos = transform.position;
            centerPos.x = Camera.main.transform.position.x;
            transform.position = centerPos;
            _startTime = Time.time;

            // Set initial rotation based on initial velocity
            float initialVelocity = _amplitude * horizontalSpeed * Mathf.Cos(0 * horizontalSpeed);
            transform.rotation = Quaternion.Euler(0, initialVelocity >= 0 ? 180 : 0, 0);
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
                // Set initial rotation based on initial velocity
                float initialVelocity = _amplitude * horizontalSpeed * Mathf.Cos(0 * horizontalSpeed);
                transform.rotation = Quaternion.Euler(0, initialVelocity >= 0 ? 180 : 0, 0);
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
                _rb.linearVelocity = new Vector2(h_vel, _rb.linearVelocity.y);

                // Update rotation every frame based on horizontal velocity
                transform.rotation = Quaternion.Euler(0, h_vel >= 0 ? 180 : 0, 0);
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