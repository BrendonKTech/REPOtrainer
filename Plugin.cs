using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPOItemESP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public partial class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "BrendonKTech.repo.itemesp";
        public const string PluginName = "REPO Item ESP";
        public const string PluginVersion = "1.1.0";

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

        private ConfigEntry<bool> showExtractions;
        private ConfigEntry<bool> showSummary;

        private ConfigEntry<float> maxDistance;
        private ConfigEntry<int> fontSize;

        private bool espEnabled = true;
        private ValuableObject[] valuables = new ValuableObject[0];
        private ExtractionPoint[] extractions = new ExtractionPoint[0];
        private float nextRefresh;

        private void Awake()
        {
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

            showExtractions = Config.Bind("World", "ShowExtractions", true, "Draw extraction points.");
            showSummary = Config.Bind("World", "ShowSummary", true, "Show valuables/enemies/extractions left in the level.");

            maxDistance = Config.Bind("Display", "MaxDistance", 150f, "Hide anything further away than this (metres).");
            fontSize = Config.Bind("Display", "FontSize", 13, "Label text size.");

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded - {menuKey.Value} for menu, {toggleKey.Value} to toggle.");
        }

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
            }
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

            if (espEnabled && Event.current.type == EventType.Repaint)
            {
                DrawEsp();
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
