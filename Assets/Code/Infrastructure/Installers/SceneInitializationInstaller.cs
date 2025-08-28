using System.Collections.Generic;
using Code.Gameplay.Services.PlayerFallingService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.SpawnersServices;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class SceneInitializationInstaller : MonoInstaller
    {
        public List<MonoBehaviour> Initializers;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private ItemSpawnerConfig _spawnConfig;
        
        public override void InstallBindings()
        {
            foreach (MonoBehaviour initializer in Initializers)
            {
                Container.BindInterfacesTo(initializer.GetType()).FromInstance(initializer).AsSingle();
            }

            Container.Bind<IPlayerStickingService>().To<PlayerStickingService>().AsSingle();
            Container.Bind<IPlayerFallingService>().To<PlayerFallingService>().AsSingle();
            
            Container.BindInterfacesAndSelfTo<Camera>().FromInstance(_mainCamera).AsCached();
            
            Container.Bind<ItemSpawnerConfig>().FromInstance(_spawnConfig).AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<ItemSpawnerService>().AsSingle();
        }
    }
}