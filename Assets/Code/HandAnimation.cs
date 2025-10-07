// Класс для управления анимацией руки

using System.Collections;
using UnityEngine;

namespace Code
{
    [System.Serializable]
    public class HandAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject handObject;
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

        public void SetActive(bool active)
        {
            if (handObject != null)
            {
                handObject.SetActive(active);
            }
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