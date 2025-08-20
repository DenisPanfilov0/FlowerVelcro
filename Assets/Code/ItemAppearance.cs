using System;
using System.Collections;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class ItemAppearance : MonoBehaviour
    {
        private const float AnimDuration = 0.2f;
        private Coroutine _currentAnimation;

        public void StartAppearAnimation()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }

            transform.localScale = Vector3.zero;
            _currentAnimation = StartCoroutine(AppearAnimation());
        }

        public IEnumerator DisappearAnimation(Action onComplete)
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }
            //
            // Vector3 startScale = transform.localScale;
            // float t = 0f;
            // while (t < 1f)
            // {
            //     t += Time.deltaTime / AnimDuration;
            //     transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
            // }
            // transform.localScale = Vector3.zero;
            onComplete?.Invoke();
            _currentAnimation = null;
        }

        private IEnumerator AppearAnimation()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / AnimDuration;
                float eased = EaseOutBack(t);
                transform.localScale = Vector3.one * eased;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _currentAnimation = null;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
    }
}