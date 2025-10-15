using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Tutorial
{
    [System.Serializable]
    public class HandAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject handObject;
        [SerializeField] private Image handImage;
        private Vector3 originalScale;
        private float minScale = 0.8f;
        private float maxScale = 1.2f;
        private float animationDuration = 0.5f;
        private Coroutine animationCoroutine;

        public void Initialize()
        {
            if (handObject != null)
            {
                originalScale = handObject.transform.localScale;
                if (handImage == null)
                {
                    handImage = handObject.GetComponent<Image>();
                }
            }
        }

        public void Animate(MonoBehaviour monoBehaviour)
        {
            if (handObject == null) return;
            if (animationCoroutine != null)
            {
                monoBehaviour.StopCoroutine(animationCoroutine);
            }
            animationCoroutine = monoBehaviour.StartCoroutine(AnimateHand());
        }

        public void StopAnimate(MonoBehaviour monoBehaviour)
        {
            if (animationCoroutine != null)
            {
                monoBehaviour.StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            if (handObject != null)
            {
                handObject.transform.localScale = originalScale;
            }
        }

        public void SetActive(bool active)
        {
            if (handObject != null)
            {
                handObject.SetActive(active);
                if (active)
                {
                    handObject.transform.localScale = originalScale;
                }
            }
        }

        public void SetAlpha(float alpha)
        {
            if (handImage != null)
            {
                Color color = handImage.color;
                color.a = alpha;
                handImage.color = color;
            }
        }

        public IEnumerator FadeAlpha(float from, float to, float duration)
        {
            float time = 0f;
            SetAlpha(from);
            while (time < duration)
            {
                time += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, time / duration);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(to);
        }

        private IEnumerator AnimateHand()
        {
            while (true)
            {
                // Анимация увеличения
                float time = 0f;
                while (time < animationDuration)
                {
                    if (handObject == null) yield break;
                    time += Time.deltaTime;
                    float t = time / animationDuration;
                    float scale = Mathf.Lerp(minScale, maxScale, t);
                    handObject.transform.localScale = originalScale * scale;
                    yield return null;
                }

                // Анимация уменьшения
                time = 0f;
                while (time < animationDuration)
                {
                    if (handObject == null) yield break;
                    time += Time.deltaTime;
                    float t = time / animationDuration;
                    float scale = Mathf.Lerp(maxScale, minScale, t);
                    handObject.transform.localScale = originalScale * scale;
                    yield return null;
                }
            }
        }
    }
}