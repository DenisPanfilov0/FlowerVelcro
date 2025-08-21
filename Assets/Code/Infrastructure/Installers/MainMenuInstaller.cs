using UnityEngine;
using Zenject;

namespace Code.Infrastructure.Installers
{
    public class MainMenuInstaller : MonoInstaller
    {
        [SerializeField] private Camera _mainCamera;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<Camera>().FromInstance(_mainCamera).AsCached();
        }
    }
}