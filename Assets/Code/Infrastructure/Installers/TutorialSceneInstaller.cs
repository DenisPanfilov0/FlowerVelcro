using System.Collections.Generic;
using Code.Gameplay.Services.GameScore;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.Heart;
using Code.Gameplay.Services.PlayerSticking;
using Code.Gameplay.Services.SpawnersServices;
using Code.Gameplay.Windows;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class TutorialSceneInstaller : MonoInstaller
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private ItemSpawnerConfig _spawnConfig;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<IGameStateService>().AsSingle();

            
            Container.Bind<IWindowFactory>().To<WindowFactory>().AsSingle();
            Container.Bind<IWindowService>().To<WindowService>().AsSingle();

            Container.BindInterfacesAndSelfTo<PlayerStickingService>().AsSingle();
            Container.BindInterfacesAndSelfTo<HeartService>().AsSingle();

            
            Container.BindInterfacesAndSelfTo<Camera>().FromInstance(_mainCamera).AsCached();
            
            Container.Bind<ItemSpawnerConfig>().FromInstance(_spawnConfig).AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ItemSpawnerService>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameScoreService>().AsSingle();
        }
    }
}