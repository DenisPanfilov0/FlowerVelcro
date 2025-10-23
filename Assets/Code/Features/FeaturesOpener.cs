using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Code.Progress.Data;
using Code.Tutorial;

namespace Code.Features
{
    public class FeaturesOpener : MonoBehaviour
    {
        [Header("Objects Sequence")]
        [SerializeField] private List<GameObject> _objectsToAppear;     // <-- список объектов для поочередного появления
        [SerializeField] private GameObject _objectToActivateAfterSequence; // <-- объект, который включается после всех

        [Header("Visuals & Effects")]
        [SerializeField] private Image _shadow;
        [SerializeField] private ParticleSystem _particlesPrefab;

        [Header("Animation Settings")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private float _appearDuration = 0.9f;
        [SerializeField] private float _moveDuration = 1.5f;
        [SerializeField] private float _arcHeight = 100f;

        private ProgressData _progress;

        [Inject]
        public void Construct(ProgressData progress)
        {
            _progress = progress;
        }

        private void Start()
        {
            if (_progress.IsFeatureOpened)
            {
                Destroy(gameObject);
                return;
            }
            
            // Если игрок не играл вообще
            if (_progress.GamePlayed == 0)
            {
                foreach (var obj in _objectsToAppear)
                    obj.SetActive(false);
                
                _shadow.gameObject.SetActive(false);

                _progress.SetTutorialChecked(true);
                _objectToActivateAfterSequence.SetActive(true);
                _objectToActivateAfterSequence.GetComponent<EndTutorial>().PlayStage(0);
                
                return;
            }

            // Если уже больше одной игры — не показываем
            if (_progress.GamePlayed > 1)
            {
                Destroy(gameObject);
                Destroy(_objectToActivateAfterSequence.gameObject);
                return;
            }

            // Иначе — запускаем красивую последовательность
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            // Прячем все объекты
            foreach (var obj in _objectsToAppear)
                if (obj != null)
                    obj.SetActive(false);

            yield return new WaitForSeconds(0.3f);

            // Анимируем поочередно
            foreach (var obj in _objectsToAppear)
            {
                if (obj == null)
                    continue;

                yield return AnimateObject(obj);
                yield return new WaitForSeconds(0.3f);
            }

            // После всех появлений — активируем финальный объект
            if (_objectToActivateAfterSequence != null)
            {
                _objectToActivateAfterSequence.SetActive(true);
                _objectToActivateAfterSequence.GetComponent<EndTutorial>().PlayStage(1);

                _progress.SetAllFeatureOpened();
            }

            // После завершения — отключаем текущий объект
            yield return new WaitForSeconds(0.5f);
            // gameObject.SetActive(false);
            
            Destroy(gameObject);
        }

        private IEnumerator AnimateObject(GameObject targetObject)
        {
            if (targetObject == null)
                yield break;

            RectTransform rect = targetObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = targetObject.AddComponent<RectTransform>();

            Vector3 originalPos = rect.position;
            Vector3 spawnWorldPos = _spawnPoint != null ? _spawnPoint.position : originalPos;

            // Подготовка
            targetObject.SetActive(true);
            rect.localScale = Vector3.zero;
            rect.position = spawnWorldPos;

            // Партиклы
            if (_particlesPrefab != null)
            {
                var particles = Instantiate(_particlesPrefab, spawnWorldPos, Quaternion.identity, transform);
                Destroy(particles.gameObject, 2f);
            }

            // Плавное появление
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
