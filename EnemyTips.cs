using System;
using System.Collections.Generic;

namespace REPOItemESP
{
    public static class EnemyTips
    {
        private static readonly Dictionary<string, string> Tips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Huntsman", "Blind - hunts by sound. Stay still and quiet." },
            { "Peeper", "Don't look at it." },
            { "Apex Predator", "Harmless unless you hurt it. Leave it alone." },
        };

        public static string Get(string enemyName)
        {
            if (enemyName != null && Tips.TryGetValue(enemyName, out string tip))
            {
                return tip;
            }
            return null;
        }
    }
}
