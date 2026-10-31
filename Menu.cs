using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace REPOItemESP
{
    public partial class Plugin
    {
        private const int MenuWindowId = 0x52455350;
        private static readonly string[] Tabs = { "Items", "Enemies", "World", "Settings" };

        private bool menuOpen;
        private int tab;
        private Rect menuRect = new Rect(40f, 40f, 320f, 10f);
        private ConfigEntry<Key> rebinding;
        private readonly List<InputKey> noKeys = new List<InputKey>();

        private void SetMenuOpen(bool open)
        {
            menuOpen = open;
            rebinding = null;

            if (!open)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void DrawMenu(int id)
        {
            tab = GUILayout.Toolbar(tab, Tabs);
            GUILayout.Space(6f);
            espEnabled = GUILayout.Toggle(espEnabled, "ESP enabled (" + toggleKey.Value + ")");
            GUILayout.Space(6f);

            if (tab == 0)
            {
                Toggle(showItems, "Show valuables");
                Toggle(colorByValue, "Color by value");
                Toggle(showFragile, "Mark fragile items");
                GUILayout.Label("Hide items under: $" + minItemValue.Value.ToString("N0"));
                minItemValue.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(minItemValue.Value, 0f, 20000f) / 500f) * 500;
            }
            else if (tab == 1)
            {
                Toggle(showEnemies, "Show enemies");
                Toggle(showEnemyHealth, "Show health");
                Toggle(showEnemyTips, "Show tips");
                Toggle(offscreenArrows, "Off-screen arrows");
                Toggle(dangerWarning, "Danger warning");
                GUILayout.Label("Warn when closer than: " + dangerDistance.Value.ToString("0") + "m");
                dangerDistance.Value = Mathf.Round(GUILayout.HorizontalSlider(dangerDistance.Value, 5f, 40f));
            }
            else if (tab == 2)
            {
                Toggle(showExtractions, "Show extraction points");
                Toggle(showSummary, "Show level summary");
            }
            else
            {
                GUILayout.Label("Max distance: " + maxDistance.Value.ToString("0") + "m");
                maxDistance.Value = Mathf.Round(GUILayout.HorizontalSlider(maxDistance.Value, 10f, 300f) / 5f) * 5f;
                GUILayout.Label("Font size: " + fontSize.Value);
                fontSize.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(fontSize.Value, 10f, 24f));
                GUILayout.Space(6f);
                KeyButton("Menu key", menuKey);
                KeyButton("ESP toggle key", toggleKey);
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("Close (" + menuKey.Value + ")"))
            {
                SetMenuOpen(false);
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private static void Toggle(ConfigEntry<bool> entry, string label)
        {
            entry.Value = GUILayout.Toggle(entry.Value, label);
        }

        private void KeyButton(string label, ConfigEntry<Key> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            string text = rebinding == entry ? "Press a key..." : entry.Value.ToString();
            if (GUILayout.Button(text, GUILayout.Width(130f)))
            {
                rebinding = entry;
            }
            GUILayout.EndHorizontal();
        }

        private void CheckRebind(Keyboard kb)
        {
            if (kb.escapeKey.wasPressedThisFrame)
            {
                rebinding = null;
                return;
            }

            foreach (KeyControl key in kb.allKeys)
            {
                if (key != null && key.wasPressedThisFrame)
                {
                    rebinding.Value = key.keyCode;
                    rebinding = null;
                    return;
                }
            }
        }
    }
}
