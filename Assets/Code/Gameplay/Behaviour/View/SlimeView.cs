using Code.Configs.ItemSpawnerConfig;
using Code.Gameplay.Services.GameStateService;
using DG.Tweening;
using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class SlimeView : Flower
    {
        public override void CloseFlower()
        {
            if (!_isTutorial)
            {
                _icon.color = Color.gray;
                _isCollected = true;
                // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => _spawnerService?.ReturnToPool(this, _typeId)));
                _spawnerService?.ReturnToPool(this, _typeId);
                ParticleSystem particle = Instantiate(_particlePrefab, transform.parent);
                particle.transform.position = transform.position;
            }
            else
            {
                // Спавн партиклов
                ParticleSystem particle = Instantiate(_particlePrefab, transform.parent);
                particle.transform.position = transform.position;

                // Анимация уменьшения масштаба с эффектом баунс
                transform.DOScale(Vector3.zero, 0.5f)
                    .SetEase(Ease.OutBounce);
            }
        }
    }
}