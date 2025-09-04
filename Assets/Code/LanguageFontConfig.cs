using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Code
{
    [CreateAssetMenu(fileName = "LanguageFonts", menuName = "Configs/Language Font Config")]
    public class LanguageFontConfig : ScriptableObject
    {
        [SerializeField] public TMPro.TMP_FontAsset DefaultFont;
        [SerializeField] public TMPro.TMP_FontAsset AsianFont;
    }
}