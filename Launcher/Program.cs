using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace REPOItemESPLauncher
{
    // turns bepinex on for one launch then back off
    internal static class Program
    {
        private const string SteamAppId = "3241660";
        private static readonly TimeSpan LoadTimeout = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan ProcessGrace = TimeSpan.FromSeconds(15);
        private static readonly Regex EnabledLine = new Regex(@"^(\s*enabled\s*=\s*)\S*", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static int Main()
        {
            Console.Title = "R.E.P.O. mod launcher";

            string gameDir = AppDomain.CurrentDomain.BaseDirectory;
            string ini = Path.Combine(gameDir, "doorstop_config.ini");
            string log = Path.Combine(gameDir, "BepInEx", "LogOutput.log");

            if (!File.Exists(Path.Combine(gameDir, "REPO.exe")))
            {
                return Fail("REPO.exe wasn't found next to this launcher.\n"
                    + "Unzip everything into your R.E.P.O. folder (Steam > right-click R.E.P.O. > Manage > Browse local files).");
            }
            if (!File.Exists(ini) || !File.Exists(Path.Combine(gameDir, "winhttp.dll")))
            {
                return Fail("winhttp.dll or doorstop_config.ini is missing. Unzip the whole package into the R.E.P.O. folder again.");
            }
            if (Process.GetProcessesByName("REPO").Length > 0)
            {
                return Fail("R.E.P.O. is already running. Close it, then run this again.");
            }

            DateTime launchedUtc = DateTime.UtcNow;
            try
            {
                SetDoorstopEnabled(ini, true);
            }
            catch (Exception e)
            {
                return Fail("Couldn't edit doorstop_config.ini: " + e.Message);
            }

            try
            {
                Console.WriteLine("Starting R.E.P.O. with mods through Steam...");
                Process.Start("steam://rungameid/" + SteamAppId);

                if (WaitForLoad(log, launchedUtc))
                {
                    Console.WriteLine("Loaded. In game: Insert = menu, F1 = toggle ESP.");
                    Thread.Sleep(3000);
                    return 0;
                }
                return Fail("The game didn't start within " + LoadTimeout.TotalMinutes + " minutes. "
                    + "If Steam was updating, run this again once it's done.");
            }
            finally
            {
                // always turn it back off
                try
                {
                    SetDoorstopEnabled(ini, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Warning: couldn't reset doorstop_config.ini (" + e.Message + "). "
                        + "The mod may also load on normal launches until you run this again.");
                    Thread.Sleep(5000);
                }
            }
        }

        // wait for the bepinex log to update, or the game to be open for a bit
        private static bool WaitForLoad(string log, DateTime launchedUtc)
        {
            DateTime deadline = DateTime.UtcNow + LoadTimeout;
            DateTime? seenProcess = null;

            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(log) && File.GetLastWriteTimeUtc(log) > launchedUtc)
                {
                    return true;
                }

                if (Process.GetProcessesByName("REPO").Length > 0)
                {
                    seenProcess = seenProcess ?? DateTime.UtcNow;
                    if (DateTime.UtcNow - seenProcess.Value > ProcessGrace)
                    {
                        return true;
                    }
                }

                Thread.Sleep(500);
            }
            return false;
        }

        private static void SetDoorstopEnabled(string ini, bool enabled)
        {
            string text = File.ReadAllText(ini);
            string updated = EnabledLine.Replace(text, "${1}" + (enabled ? "true" : "false"), 1);
            if (updated != text)
            {
                File.WriteAllText(ini, updated, new UTF8Encoding(false));
            }
        }

        private static int Fail(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("Press any key to close.");
            Console.ReadKey(true);
            return 1;
        }
    }
}
