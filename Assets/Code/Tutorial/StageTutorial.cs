using System;
using System.Collections.Generic;
using Code.Gameplay.Behaviour.View;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using UnityEngine;
using YG;
using Zenject;

namespace Code.Tutorial
{
    public class StageTutorial : MonoBehaviour
    {
        [SerializeField] private PlayerView _player;
        [SerializeField] private List<ItemView> _items;
        [SerializeField] private TutorialStagChanger _stageChanger;

        [SerializeField] private int _goalFlower;
        private int _currentFlowerCollected = 0;

        private PlayerStickingService _playerStickingService;
        private HeartService _heartService;
        private IGameStateService _gameStateService;
        private DiContainer _container;

        private PlayerView _playerCopy;
        private List<ItemView> _itemCopies = new();

        [Inject]
        public void Construct(PlayerStickingService playerStickingService, 
            HeartService heartService, IGameStateService gameStateService, DiContainer container)
        {
            _gameStateService = gameStateService;
            _heartService = heartService;
            _playerStickingService = playerStickingService;
            _container = container;
        }

        private void Start()
        {
            foreach (var item in _items)
            {
                item.Setup(_playerStickingService, null, _heartService, _gameStateService, true);
            }
            
            YG2.MetricaSend("tutorial", "Start", $"{gameObject.name}");
        }

        public void StageActive()
        {
            gameObject.SetActive(true);
            CreateCopies();
        }

        private void CreateCopies()
        {
            // Очищаем старые копии, если они существуют
            ClearCopies();

            // Создаем копию игрока через Zenject
            if (_player != null)
            {
                _playerCopy = _container.InstantiatePrefabForComponent<PlayerView>(
                    _player.gameObject, 
                    _player.transform.position, 
                    _player.transform.rotation, 
                    transform);
                _playerCopy.gameObject.SetActive(true);
                _player.gameObject.SetActive(false); // Деактивируем оригинал
                // _playerCopy.Setup(_playerStickingService, null, _heartService, _gameStateService, true);
            }
            else
            {
                Debug.LogWarning("PlayerView is missing! Please assign it in the inspector.");
            }

            // Создаем копии объектов через Zenject
            _itemCopies = new List<ItemView>();
            foreach (var item in _items)
            {
                if (item != null)
                {
                    ItemView itemCopy = _container.InstantiatePrefabForComponent<ItemView>(
                        item.gameObject, 
                        item.transform.position, 
                        item.transform.rotation, 
                        transform);
                    itemCopy.Setup(_playerStickingService, null, _heartService, _gameStateService, true);
                    itemCopy.gameObject.SetActive(true);
                    item.gameObject.SetActive(false); // Деактивируем оригинал
                    _itemCopies.Add(itemCopy);
                }
                else
                {
                    Debug.LogWarning("ItemView is missing in _items list!");
                }
            }
        }

        private void ClearCopies()
        {
            // Удаляем копию игрока
            if (_playerCopy != null)
            {
                Destroy(_playerCopy.gameObject);
                _playerCopy = null;
            }

            // Удаляем копии объектов
            foreach (var itemCopy in _itemCopies)
            {
                if (itemCopy != null)
                {
                    Destroy(itemCopy.gameObject);
                }
            }
            _itemCopies.Clear();
        }

        public void StageCompleted()
        {
            ClearCopies();
            gameObject.SetActive(false);
            _stageChanger.StageNext();
        }

        public void StagRestart()
        {
            _currentFlowerCollected = 0;
            ClearCopies();
            CreateCopies();
        }

        public void FlowerCollected()
        {
            _currentFlowerCollected++;

            if (_currentFlowerCollected >= _goalFlower)
            {
                StageCompleted();
            }
        }
    }
}