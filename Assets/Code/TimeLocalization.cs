using System.Collections.Generic;

namespace Code
{
    public static class TimeLocalization
    {
        private static readonly Dictionary<LanguageType, (string h, string m, string s, string label)> _timeLabels =
            new()
            {
                { LanguageType.ru, ("ч", "м", "с", "До сброса") },
                { LanguageType.en, ("h", "m", "s", "Time until reset") },
                { LanguageType.tr, ("sa", "dk", "sn", "Sıfırlamaya kalan süre") },
                { LanguageType.fr, ("h", "m", "s", "Temps avant réinitialisation") },
                { LanguageType.es, ("h", "m", "s", "Tiempo hasta reinicio") },
                { LanguageType.de, ("Std", "Min", "Sek", "Zeit bis zum Zurücksetzen") },
                { LanguageType.id, ("j", "m", "d", "Waktu hingga reset") },
            };

        public static (string h, string m, string s, string label) Get(LanguageType type)
        {
            return _timeLabels.TryGetValue(type, out var val)
                ? val
                : _timeLabels[LanguageType.en]; // fallback
        }
    }
}