using System.Collections.Generic;
using Code.Gameplay.Services.GameStateService;
using UnityEngine;

namespace Code.Gameplay.Services.FallManagerService
{
    public class FallManagerService : IFallManagerService
    {
        private readonly IGameStateService _gameStateService;
        private readonly List<GameObject> _fallingObjects = new List<GameObject>();
        // private readonly float _fallSpeed = 2.5f;

        // private bool _isCanFalling = true;
        // private bool _isGameStop = false;

        public FallManagerService(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService;

            _gameStateService.OnGameLose += () =>
            {
                // _isGameStop = true;
            };
        }

        public void AddFallingObject(GameObject fallingObject)
        {
            _fallingObjects.Add(fallingObject);
        }

        public void RemoveFallingObject(GameObject fallingObject)
        {
            _fallingObjects.Remove(fallingObject);
        }

        public void UpdateFallingObjects()
        {
            // if (!_isCanFalling || _isGameStop)
            // {
            //     return;
            // }
            //
            // foreach (var fallingObject in _fallingObjects)
            // {
            //     // RectTransform rectTransform = fallingObject.GetComponent<RectTransform>();
            //     if (fallingObject.transform != null)
            //     {
            //         fallingObject.transform.localPosition += Vector3.down * _fallSpeed;
            //     }
            // }
        }

        public void Cleanup()
        {
            _fallingObjects.Clear();
            // _isCanFalling = true;
            // _isGameStop = false;
        }
    }
}