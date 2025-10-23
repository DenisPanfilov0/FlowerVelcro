using UnityEngine;

namespace Code.Tutorial
{
    public class HandAnimationSpriteRenderer : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Vector3 originalScale;
        private float minScale = 0.8f;
        private float maxScale = 1.2f;
        private float animationDuration = 0.5f;

        private void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalScale = transform.localScale;
            StartCoroutine(AnimateHand());
        }

        private System.Collections.IEnumerator AnimateHand()
        {
            while (true)
            {
                // Scale up
                float time = 0f;
                while (time < animationDuration)
                {
                    time += Time.deltaTime;
                    float t = time / animationDuration;
                    float scale = Mathf.Lerp(minScale, maxScale, t);
                    transform.localScale = originalScale * scale;
                    yield return null;
                }

                // Scale down
                time = 0f;
                while (time < animationDuration)
                {
                    time += Time.deltaTime;
                    float t = time / animationDuration;
                    float scale = Mathf.Lerp(maxScale, minScale, t);
                    transform.localScale = originalScale * scale;
                    yield return null;
                }
            }
        }
    }
}