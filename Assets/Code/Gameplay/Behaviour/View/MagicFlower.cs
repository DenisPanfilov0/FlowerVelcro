using UnityEngine;

namespace Code.Gameplay.Behaviour.View
{
    public class MagicFlower : Flower
    {
        public override void CloseFlower()
        {
            _icon.color = Color.gray;
            _isCollected = true;
            // StartCoroutine(GetComponent<ItemAppearance>().DisappearAnimation(() => _spawnerService?.ReturnToPool(this, _typeId)));
            _spawnerService?.ReturnToPool(this, _typeId);
            ParticleSystem particle = Instantiate(_particlePrefab, transform.parent);
            particle.transform.position = transform.position;
        }
    }
}