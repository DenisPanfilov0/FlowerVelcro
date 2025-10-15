using System.Collections;
using Code.Features.DailyLogin;
using Code.Features.DailyTask;
using Code.Progress.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Features
{
    public class FeaturesOpener : MonoBehaviour
    {
        [SerializeField] private Button _dailyLoginButton;
        [SerializeField] private Button _dailyTaskButton;
        [SerializeField] private Image _shadow;
        [SerializeField] private ParticleSystem _particlesPrefab;

        [Header("Animation Settings")]
        [SerializeField] private Transform _spawnPoint;     // <-- новая переменная
        [SerializeField] private float _appearDuration = 0.9f;
        [SerializeField] private float _moveDuration = 1.5f;
        [SerializeField] private float _arcHeight = 100f;   // высота дуги полёта

        private ProgressData _progress;

        [Inject]
        public void Construct(ProgressData progress)
        {
            _progress = progress;
        }

        private void Start()
        {
            // if (_progress.GamePlayed == 0)
            // {
            //     _dailyLoginButton.gameObject.SetActive(false);
            //     _dailyTaskButton.gameObject.SetActive(false);
            //     return;
            // }

            if (_progress.GamePlayed > 1)
            {
                Destroy(gameObject);
                return;
            }

            // Если сыграна ровно одна игра
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            // Прячем кнопки изначально
            _dailyLoginButton.gameObject.SetActive(false);
            _dailyTaskButton.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.3f);

            // Анимируем поочередно
            yield return AnimateButton(_dailyLoginButton);
            yield return new WaitForSeconds(0.3f);
            yield return AnimateButton(_dailyTaskButton);

            yield return new WaitForSeconds(0.3f);

            Destroy(gameObject);
        }

        private IEnumerator AnimateButton(Button targetButton)
        {
            if (targetButton == null)
                yield break;

            RectTransform rect = targetButton.GetComponent<RectTransform>();
            Vector3 originalPos = rect.position;

            // Место появления — позиция spawnPoint (в мировых координатах)
            Vector3 spawnWorldPos = _spawnPoint != null ? _spawnPoint.position : originalPos;

            // Подготовка
            targetButton.gameObject.SetActive(true);
            rect.localScale = Vector3.zero;
            rect.position = spawnWorldPos;

            // Создаём партиклы в месте появления
            if (_particlesPrefab != null)
            {
                var particles = Instantiate(_particlesPrefab, spawnWorldPos, Quaternion.identity, transform);
                Destroy(particles.gameObject, 2f);
            }

            // Анимация появления (из 0 → 1)
            yield return rect.DOScale(Vector3.one, _appearDuration)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();

            yield return new WaitForSeconds(0.25f);

            // Полёт по дуге
            Vector3 midPoint = (spawnWorldPos + originalPos) / 2f + Vector3.up * _arcHeight;

            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOPath(
                new[] { spawnWorldPos, midPoint, originalPos },
                _moveDuration,
                PathType.CatmullRom
            ).SetEase(Ease.InOutSine));
            seq.Join(rect.DOScale(1.1f, _moveDuration / 2f).SetLoops(2, LoopType.Yoyo));

            yield return seq.WaitForCompletion();
        }
    }
}
