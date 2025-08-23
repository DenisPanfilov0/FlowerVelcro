using Code.Gameplay.Services.FallManagerService;
using Code.Gameplay.Services.GameScoreService;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Services.HeartService;
using Code.Gameplay.Services.PlayerFallingService;
using Code.Gameplay.Services.PlayerStickingService;
using Code.Gameplay.Services.SpawnersServices;
using Code.Gameplay.Services.SpawnersServices.BombSpawnerService;
using Code.Gameplay.Services.SpawnersServices.HeartSpawnerService;
using Code.Gameplay.Services.SpawnersServices.SlimeSpawnerService;
using Code.Gameplay.Services.TimerService;
using Code.Gameplay.Windows;
using Code.GlobalScreen.Behaviour;
using Code.Infrastructure.Loading;
using Code.Infrastructure.States.Factory;
using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Infrastructure.StaticData;
using Code.Inventory;
using Code.MainMenu.Services.AmbientSoundService;
using Code.Progress.Data;
using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class BootstrapInstaller : MonoInstaller, ICoroutineRunner, IInitializable
    {
        [SerializeField] private InventorySkinConfigs _inventorySkinConfigs;
        [SerializeField] private CurrencyConfig _currencyConfig;
        [SerializeField] private AudioManager _audioManager;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SaveLoadService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ProgressData>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<InventoryModel>().AsSingle().NonLazy();
            Container.Bind<InventorySkinConfigs>().FromInstance(_inventorySkinConfigs).AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<CurrencyModel>().AsSingle().NonLazy();
            Container.Bind<CurrencyConfig>().FromInstance(_currencyConfig).AsSingle().NonLazy();
            
            Container.Bind<AudioManager>().FromComponentInNewPrefab(_audioManager).AsSingle().NonLazy();
            
            BindInfrastructureServices();
            BindCommonServices();
            BindMainMenuServices();
            BindGameplayServices();
            BindStateMachine();
            BindStateFactory();
            BindGameStates();
            BindProgressServices();

            
        }

        private void BindStateMachine()
        {
            Container.BindInterfacesAndSelfTo<GameStateMachine>().AsSingle();
        }

        private void BindStateFactory()
        {
            Container.BindInterfacesAndSelfTo<StateFactory>().AsSingle();
        }

        private void BindGameStates()
        {
            Container.BindInterfacesAndSelfTo<BootstrapState>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoadProgressState>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoadMainMenuState>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuState>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoadGameLoopState>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameLoopState>().AsSingle();
            Container.BindInterfacesAndSelfTo<RestartLevelState>().AsSingle();
        }
        
        private void BindProgressServices()
        {
            
        }
        
        private void BindMainMenuServices()
        {
            Container.Bind<IAmbientSoundService>().To<AmbientSoundService>().AsSingle();
        }

        private void BindGameplayServices()
        {
            // Container.Bind<IBombSpawnerService>().To<BombSpawnerService>().AsSingle();
            // Container.Bind<ISlimeSpawnerService>().To<SlimeSpawnerService>().AsSingle();
            
            
            
            // Container.BindInterfacesAndSelfTo<ItemSpawnerService>().AsSingle();
            
            
            
            // Container.Bind<IHeartSpawnerService>().To<HeartSpawnerService>().AsSingle();
            Container.Bind<IPlayerStickingService>().To<PlayerStickingService>().AsSingle();
            Container.Bind<IPlayerFallingService>().To<PlayerFallingService>().AsSingle();
            Container.BindInterfacesAndSelfTo<IGameStateService>().AsSingle();
            Container.Bind<IGameScoreService>().To<GameScoreService>().AsSingle();
            Container.Bind<IHeartService>().To<HeartService>().AsSingle();
            Container.Bind<IFallManagerService>().To<FallManagerService>().AsSingle();
        }

        private void BindInfrastructureServices()
        {
            Container.BindInterfacesTo<BootstrapInstaller>().FromInstance(this).AsSingle();
            Container.Bind<IStaticDataService>().To<StaticDataService>().AsSingle();
            Container.Bind<ITimerService>().To<TimerService>().AsSingle();
            Container.Bind<IWindowFactory>().To<WindowFactory>().AsSingle();
            Container.Bind<IWindowService>().To<WindowService>().AsSingle();
        }

        private void BindCommonServices()
        {
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
        }

        public void Initialize()
        {
            Container.Resolve<IGameStateMachine>().Enter<BootstrapState>();
        }
    }
}