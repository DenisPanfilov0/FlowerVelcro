using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Code.Features.DailyLogin
{
    public class DailyLoginButton : MonoBehaviour
    {
        [SerializeField] private DailyLoginWindow _dailyLoginWindow;
        [SerializeField] private Button _dailyTaskOpen;

        private Tween _scaleTween;

        private void Start()
        {
            _dailyTaskOpen.onClick.AddListener(OpenDailyTaskWindow);

            // Start the looping scale animation
            // StartScaleAnimation();
        }

        private void OnDestroy()
        {
            _dailyTaskOpen.onClick.RemoveListener(OpenDailyTaskWindow);

            // Clean up the DOTween animation
            _scaleTween?.Kill();
        }

        private void OpenDailyTaskWindow()
        {
            _dailyLoginWindow.Show();
        }

        private void StartScaleAnimation()
        {
            // Ensure the button starts at its original scale
            transform.localScale = Vector3.one;

            // Create a subtle scale animation (e.g., from 1 to 1.1 and back)
            _scaleTween = transform.DOScale(1.07f, 0.93f)
                .SetEase(Ease.InOutSine) // Smooth easing for natural effect
                .SetLoops(-1, LoopType.Yoyo); // Loop indefinitely, yoyo style (up and back)
        }
    }
}