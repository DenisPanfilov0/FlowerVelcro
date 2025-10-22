using Code.Gameplay.Services.GameStateService;
using Code.Gameplay.Windows;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class Merge2Installer : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<IGameStateService>().AsSingle();

            
            Container.Bind<IWindowFactory>().To<WindowFactory>().AsSingle();
            Container.Bind<IWindowService>().To<WindowService>().AsSingle();

            // Container.BindInterfacesAndSelfTo<PlayerStickingService>().AsSingle();
            // Container.BindInterfacesAndSelfTo<HeartService>().AsSingle();

            
            // Container.BindInterfacesAndSelfTo<Camera>().FromInstance(_mainCamera).AsCached();
            
            // Container.Bind<ItemSpawnerConfig>().FromInstance(_spawnConfig).AsSingle().NonLazy();
            
            // Container.BindInterfacesAndSelfTo<ItemSpawnerService>().AsSingle();
            // Container.BindInterfacesAndSelfTo<GameScoreService>().AsSingle();
        }
    }
}