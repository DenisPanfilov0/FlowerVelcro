// using UnityEngine;
//
// namespace Code.Gameplay.Behaviour.View
// {
//     public class ClickHandler : MonoBehaviour
//     {
//         void Update()
//         {
//             if (Input.GetMouseButtonDown(0))
//             {
//                 Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//                 RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
//
//                 GameObject her = hit.collider.gameObject;
//
//                 
//                 if (hit.collider != null)
//                 {
//                     var slime = hit.collider.GetComponent<SlimeView>();
//                     if (slime != null)
//                     {
//                         slime.OnClicked();
//                     }
//                 }
//             }
//         }
//     }
// }