using Code.Advertising;
using Code.Gameplay.Services.GameScore;
using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Windows;
using Code.Infrastructure.Loading;
using Code.Infrastructure.States.Factory;
using Code.Infrastructure.States.GameStates;
using Code.Infrastructure.States.StateMachine;
using Code.Infrastructure.StaticData;
using Code.Inventory;
using Code.Leaderboards;
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
        [SerializeField] private SceneLoaderUI _sceneLoaderUI;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SaveLoadService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ProgressData>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<AdvertisingService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LanguageModel>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LeaderBoardModel>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<InventoryModel>().AsSingle().NonLazy();
            Container.Bind<InventorySkinConfigs>().FromInstance(_inventorySkinConfigs).AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<CurrencyModel>().AsSingle().NonLazy();
            Container.Bind<CurrencyConfig>().FromInstance(_currencyConfig).AsSingle().NonLazy();
            
            Container.Bind<AudioManager>().FromComponentInNewPrefab(_audioManager).AsSingle().NonLazy();
            
            
            BindInfrastructureServices();
            BindCommonServices();
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

        private void BindGameplayServices()
        {
            
        }

        private void BindInfrastructureServices()
        {
            Container.BindInterfacesTo<BootstrapInstaller>().FromInstance(this).AsSingle();
            Container.Bind<IStaticDataService>().To<StaticDataService>().AsSingle();
            
        }

        private void BindCommonServices()
        {
            Container.Bind<ISceneLoader>().To<SceneLoader>().AsSingle();
            Container.Bind<SceneLoaderUI>().FromComponentInNewPrefab(_sceneLoaderUI).AsSingle().NonLazy();
        }

        public void Initialize()
        {
            Container.Resolve<IGameStateMachine>().Enter<BootstrapState>();
        }
    }
}