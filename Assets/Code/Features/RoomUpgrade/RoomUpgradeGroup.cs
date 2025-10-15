using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Code.Features.RoomUpgrade
{
    public class RoomUpgradeGroup : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _objects;
        [SerializeField] private int _price;


        public void ActivateGroup()
        {
            foreach (var obj in _objects)
            {
                obj.SetActive(true);
            }
        }
        
        public void ActivateGroupWithParticles()
        {
            float delay = 0f;
            const float delayIncrement = 0.1f;

            foreach (var obj in _objects)
            {
                // Store the original scale
                Vector3 originalScale = obj.transform.localScale;
                
                // Set object active and scale to zero
                obj.SetActive(true);
                obj.transform.localScale = Vector3.zero;

                // Create a sequence for the bounce effect
                Sequence sequence = DOTween.Sequence();
                sequence.SetDelay(delay);
                sequence.Append(obj.transform.DOScale(originalScale * 1.3f, 0.5f)); // Scale to 120% over 1 second
                sequence.Append(obj.transform.DOScale(originalScale, 0.2f)); // Scale back to original over 0.2 seconds
                
                // Increment delay for the next object
                delay += delayIncrement;
            }
        }
        
        public int GetPrice()
        {
            return _price;
        }
    }
}