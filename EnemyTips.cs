using System;
using System.Collections.Generic;

namespace REPOItemESP
{
    public static class EnemyTips
    {
        private static readonly Dictionary<string, string> Tips = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Huntsman", "Blind, stay quiet" },
            { "Peeper", "Dont look at it" },
            { "Apex Predator", "Leave it alone" },
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
