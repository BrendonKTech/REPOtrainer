using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPOItemESP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public partial class Plugin : BaseUnityPlugin
    {
        private const string EspGuid = "BrendonKTech.repo.itemesp";
#if TRAINER
        public const string PluginGuid = "BrendonKTech.repo.trainer";
        public const string PluginName = "REPO Trainer";
#else
        public const string PluginGuid = EspGuid;
        public const string PluginName = "REPO Item ESP";
#endif
        public const string PluginVersion = "1.2.0";

        private ConfigEntry<Key> menuKey;
        private ConfigEntry<Key> toggleKey;

        private ConfigEntry<bool> showItems;
        private ConfigEntry<bool> colorByValue;
        private ConfigEntry<bool> showFragile;
        private ConfigEntry<int> minItemValue;
        private ConfigEntry<float> fragileThreshold;

        private ConfigEntry<bool> showEnemies;
        private ConfigEntry<bool> showEnemyHealth;
        private ConfigEntry<bool> showEnemyTips;
        private ConfigEntry<bool> offscreenArrows;
        private ConfigEntry<bool> dangerWarning;
        private ConfigEntry<float> dangerDistance;

        private ConfigEntry<bool> highlightItems;
        private ConfigEntry<bool> highlightEnemies;

        private ConfigEntry<bool> showExtractions;
        private ConfigEntry<bool> showSummary;

        private ConfigEntry<float> maxDistance;
        private ConfigEntry<int> fontSize;

        private bool espEnabled = true;
        private ValuableObject[] valuables = new ValuableObject[0];
        private ExtractionPoint[] extractions = new ExtractionPoint[0];
        private float nextRefresh;

        // trainer only, see Trainer.cs
        partial void TrainerUpdate();
        partial void DrawTrainerTab();

        private void Awake()
        {
#if !TRAINER
            // trainer has the esp built in
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Info.Location), "REPOTrainer.dll")))
            {
                Logger.LogWarning("REPOTrainer.dll is installed too and includes the ESP. This plugin will stay off.");
                enabled = false;
                return;
            }
#endif

            menuKey = Config.Bind("Keys", "MenuKey", Key.Insert, "Opens the settings menu.");
            toggleKey = Config.Bind("Keys", "ToggleKey", Key.F1, "Turns the ESP on/off.");

            showItems = Config.Bind("Items", "ShowItems", true, "Draw valuables.");
            colorByValue = Config.Bind("Items", "ColorByValue", true, "Color valuables by how much they're worth.");
            showFragile = Config.Bind("Items", "ShowFragile", true, "Mark valuables that break easily.");
            minItemValue = Config.Bind("Items", "MinItemValue", 0, "Hide valuables worth less than this.");
            fragileThreshold = Config.Bind("Items", "FragileThreshold", 70f, "Fragility (0-100) at or above which an item counts as fragile.");

            showEnemies = Config.Bind("Enemies", "ShowEnemies", true, "Draw enemies.");
            showEnemyHealth = Config.Bind("Enemies", "ShowEnemyHealth", true, "Show enemy health.");
            showEnemyTips = Config.Bind("Enemies", "ShowEnemyTips", true, "Show a short tip under known enemies.");
            offscreenArrows = Config.Bind("Enemies", "OffscreenArrows", true, "Arrows at the screen edge pointing to enemies you can't see.");
            dangerWarning = Config.Bind("Enemies", "DangerWarning", true, "Warning at the top of the screen when an enemy is close.");
            dangerDistance = Config.Bind("Enemies", "DangerDistance", 15f, "How close (metres) an enemy has to be for the warning.");

            highlightItems = Config.Bind("Highlight", "HighlightItems", true, "Show the shape of valuables through walls, in their value color.");
            highlightEnemies = Config.Bind("Highlight", "HighlightEnemies", true, "Show the shape of enemies through walls, in red.");

            showExtractions = Config.Bind("World", "ShowExtractions", true, "Draw extraction points.");
            showSummary = Config.Bind("World", "ShowSummary", true, "Show valuables/enemies/extractions left in the level.");

            maxDistance = Config.Bind("Display", "MaxDistance", 150f, "Hide anything further away than this (metres).");
            fontSize = Config.Bind("Display", "FontSize", 13, "Label text size.");

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded - {menuKey.Value} for menu, {toggleKey.Value} to toggle.");
        }

#if TRAINER
        // turn off the esp plugin if its also installed, old versions dont check
        private void Start()
        {
            if (Chainloader.PluginInfos.TryGetValue(EspGuid, out PluginInfo esp) && esp.Instance != null && esp.Instance.enabled)
            {
                esp.Instance.enabled = false;
                Logger.LogWarning("Turned off " + esp.Metadata.Name + " " + esp.Metadata.Version + ": the trainer already includes the ESP.");
            }
        }
#endif

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (rebinding != null)
                {
                    CheckRebind(kb);
                }
                else
                {
                    if (WasPressed(kb, menuKey.Value))
                    {
                        SetMenuOpen(!menuOpen);
                    }
                    if (WasPressed(kb, toggleKey.Value))
                    {
                        espEnabled = !espEnabled;
                    }
                }
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                valuables = FindObjectsByType<ValuableObject>(FindObjectsSortMode.None);
                extractions = FindObjectsByType<ExtractionPoint>(FindObjectsSortMode.None);
                rendererCache.Clear();
            }

            TrainerUpdate();
        }

        private void LateUpdate()
        {
            if (!menuOpen)
            {
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (InputManager.instance != null)
            {
                InputManager.instance.DisableAiming(0.1f);
                InputManager.instance.DisableControlsExcept(0.1f, noKeys);
            }
        }

        private void OnGUI()
        {
            SetupStyles();

            if (Event.current.type == EventType.Repaint)
            {
                // gets drawn next frame
                BeginHighlights();
                if (espEnabled)
                {
                    DrawEsp();
                }
            }

            if (menuOpen)
            {
                menuRect = GUILayout.Window(MenuWindowId, menuRect, DrawMenu, PluginName + " " + PluginVersion, GUILayout.Width(320));
            }
        }

        private static bool WasPressed(Keyboard kb, Key key)
        {
            return key != Key.None && kb[key].wasPressedThisFrame;
        }
    }
}
