using Assets.SimpleLocalization.Scripts;
using TMPro;
using UnityEngine;

namespace Code
{
    /// <summary>
    /// Localize TMP_Text component.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMP : MonoBehaviour
    {
        [SerializeField] private string LocalizationKey;

        private void Start()
        {
            Localize();
            LocalizationManager.OnLocalizationChanged += Localize;
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLocalizationChanged -= Localize;
        }

        private void Localize()
        {
            GetComponent<TMP_Text>().text = LocalizationManager.Localize(LocalizationKey);
        }

        public void SetKey(string key)
        {
            LocalizationKey = key;
        }
    }
}