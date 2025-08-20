// using Code.Gameplay.Services.FallManagerService;
// using Code.Gameplay.Services.PlayerFallingService;
// using UnityEngine;
// using Zenject;
//
// namespace Code.Gameplay.Behaviour
// {
//     public class UniqueUpdateController : MonoBehaviour
//     {
//         private IFallManagerService _fallManagerService;
//         private IPlayerFallingService _playerFallingService;
//
//         [Inject]
//         public void Construct(IFallManagerService fallManagerService, IPlayerFallingService playerFallingService)
//         {
//             _playerFallingService = playerFallingService;
//             _fallManagerService = fallManagerService;
//         }
//         
//         private void Update()
//         {
//             _fallManagerService.UpdateFallingObjects();
//             _playerFallingService.UpdatePlayerPosition();
//         }
//     }
// }