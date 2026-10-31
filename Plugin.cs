using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace REPOItemESP
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "BrendonKTech.repo.itemesp";
        public const string PluginName = "REPO Item ESP";
        public const string PluginVersion = "1.0.0";

        private static readonly AccessTools.FieldRef<ValuableObject, float> ValueRef =
            AccessTools.FieldRefAccess<ValuableObject, float>("dollarValueCurrent");
        private static readonly AccessTools.FieldRef<EnemyParent, bool> SpawnedRef =
            AccessTools.FieldRefAccess<EnemyParent, bool>("Spawned");
        private static readonly AccessTools.FieldRef<EnemyParent, Enemy> EnemyRef =
            AccessTools.FieldRefAccess<EnemyParent, Enemy>("Enemy");

        private ConfigEntry<Key> toggleKey;
        private ConfigEntry<bool> showItems;
        private ConfigEntry<bool> showEnemies;
        private ConfigEntry<float> maxDistance;
        private ConfigEntry<int> minItemValue;

        private bool espEnabled = true;
        private ValuableObject[] valuables = new ValuableObject[0];
        private float nextRefresh;
        private GUIStyle labelStyle;
        private GUIStyle shadowStyle;

        private void Awake()
        {
            toggleKey = Config.Bind("General", "ToggleKey", Key.F1, "Key that turns the ESP on/off.");
            showItems = Config.Bind("General", "ShowItems", true, "Draw valuables.");
            showEnemies = Config.Bind("General", "ShowEnemies", true, "Draw enemies.");
            maxDistance = Config.Bind("General", "MaxDistance", 150f, "Hide anything further away than this (metres).");
            minItemValue = Config.Bind("General", "MinItemValue", 0, "Hide valuables worth less than this.");

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded - press {toggleKey.Value} to toggle.");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey.Value].wasPressedThisFrame)
            {
                espEnabled = !espEnabled;
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 1f;
                valuables = FindObjectsByType<ValuableObject>(FindObjectsSortMode.None);
            }
        }

        private void OnGUI()
        {
            if (!espEnabled || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            SetupStyles();

            if (showItems.Value)
            {
                foreach (ValuableObject valuable in valuables)
                {
                    if (valuable == null)
                    {
                        continue;
                    }

                    float value = ValueRef(valuable);
                    if (value < minItemValue.Value)
                    {
                        continue;
                    }

                    string text = CleanName(valuable.name) + "\n$" + value.ToString("N0");
                    DrawLabel(cam, valuable.transform.position, text, Color.green);
                }
            }

            if (showEnemies.Value && EnemyDirector.instance != null)
            {
                foreach (EnemyParent parent in EnemyDirector.instance.enemiesSpawned)
                {
                    if (parent == null || !SpawnedRef(parent))
                    {
                        continue;
                    }

                    Vector3 pos = parent.transform.position;
                    Enemy enemy = EnemyRef(parent);
                    if (enemy != null && enemy.CenterTransform != null)
                    {
                        pos = enemy.CenterTransform.position;
                    }

                    DrawLabel(cam, pos, parent.enemyName, Color.red);
                }
            }
        }

        private void DrawLabel(Camera cam, Vector3 worldPos, string text, Color color)
        {
            float distance = Vector3.Distance(cam.transform.position, worldPos);
            if (distance > maxDistance.Value)
            {
                return;
            }

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z <= 0f)
            {
                return;
            }

            float x = vp.x * Screen.width;
            float y = (1f - vp.y) * Screen.height;

            string full = text + "\n" + distance.ToString("0") + "m";
            Vector2 size = labelStyle.CalcSize(new GUIContent(full));
            Rect rect = new Rect(x - size.x / 2f, y - size.y / 2f, size.x, size.y);

            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), full, shadowStyle);
            labelStyle.normal.textColor = color;
            GUI.Label(rect, full, labelStyle);
        }

        private void SetupStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontSize = 13;
            labelStyle.fontStyle = FontStyle.Bold;

            shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = Color.black;
        }

        private static string CleanName(string name)
        {
            return name.Replace("(Clone)", "").Replace("Valuable ", "").Trim();
        }
    }
}
