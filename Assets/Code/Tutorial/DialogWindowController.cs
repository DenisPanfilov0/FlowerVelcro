using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Tutorial
{
    public class DialogWindowController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text dialogText;

        public void SetAlpha(float alpha)
        {
            // if (canvasGroup != null)
            //     canvasGroup.alpha = alpha;

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = alpha;
                backgroundImage.color = c;
            }

            if (dialogText != null)
            {
                Color c = dialogText.color;
                c.a = alpha;
                dialogText.color = c;
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
    }
}