using Code.Advertising;
using Code.Features.DailyLogin;
using Code.Features.DailyTask;
using Code.Features.RoomUpgrade;
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
        [SerializeField] private LanguageFontConfig _languageFontConfig;
        [SerializeField] private DailyTaskConfig _dailyTaskConfig;
        [SerializeField] private DailyLoginConfig _dailyLoginConfig;
        [SerializeField] private RoomUpgradeConfig _roomUpgradeConfig;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<SaveLoadService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ProgressData>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<AdvertisingService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LeaderBoardModel>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<LanguageModel>().AsSingle().NonLazy();
            Container.Bind<LanguageFontConfig>().FromInstance(_languageFontConfig).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LanguageFontService>().AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<InventoryModel>().AsSingle().NonLazy();
            Container.Bind<InventorySkinConfigs>().FromInstance(_inventorySkinConfigs).AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<CurrencyModel>().AsSingle().NonLazy();
            Container.Bind<CurrencyConfig>().FromInstance(_currencyConfig).AsSingle().NonLazy();
            
            Container.Bind<AudioManager>().FromComponentInNewPrefab(_audioManager).AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<DailyTaskModel>().AsSingle().NonLazy();
            Container.Bind<DailyTaskConfig>().FromInstance(_dailyTaskConfig).AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<DailyLoginModel>().AsSingle().NonLazy();
            Container.Bind<DailyLoginConfig>().FromInstance(_dailyLoginConfig).AsSingle().NonLazy();
            
            Container.BindInterfacesAndSelfTo<RoomUpgradeModel>().AsSingle().NonLazy();
            Container.Bind<RoomUpgradeConfig>().FromInstance(_roomUpgradeConfig).AsSingle().NonLazy();
            
            
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
            Container.BindInterfacesAndSelfTo<LoadGameLoopMerge2State>().AsSingle();
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