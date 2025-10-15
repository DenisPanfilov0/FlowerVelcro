using DG.Tweening;
using UnityEngine;

namespace Code
{
    public class ButtonScaleAnimation : MonoBehaviour
    {
        private Tween _scaleTween;

        private void Start()
        {
            // StartScaleAnimation();
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
        }

        private void StartScaleAnimation()
        {
            // Ensure the button starts at its original scale
            transform.localScale = Vector3.one;

            // Create a subtle scale animation (e.g., from 1 to 1.1 and back)
            _scaleTween = transform.DOScale(1.03f, 0.97f)
                .SetEase(Ease.InOutSine) // Smooth easing for natural effect
                .SetLoops(-1, LoopType.Yoyo); // Loop indefinitely, yoyo style (up and back)
        }
    }
}