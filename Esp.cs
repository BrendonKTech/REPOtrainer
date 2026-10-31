using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace REPOItemESP
{
    public partial class Plugin
    {
        private static readonly AccessTools.FieldRef<ValuableObject, float> ValueRef =
            AccessTools.FieldRefAccess<ValuableObject, float>("dollarValueCurrent");
        private static readonly AccessTools.FieldRef<EnemyParent, bool> SpawnedRef =
            AccessTools.FieldRefAccess<EnemyParent, bool>("Spawned");
        private static readonly AccessTools.FieldRef<EnemyParent, Enemy> EnemyRef =
            AccessTools.FieldRefAccess<EnemyParent, Enemy>("Enemy");
        private static readonly AccessTools.FieldRef<Enemy, EnemyHealth> EnemyHealthRef =
            AccessTools.FieldRefAccess<Enemy, EnemyHealth>("Health");
        private static readonly AccessTools.FieldRef<EnemyHealth, int> HealthCurrentRef =
            AccessTools.FieldRefAccess<EnemyHealth, int>("healthCurrent");
        private static readonly AccessTools.FieldRef<EnemyHealth, bool> DeadRef =
            AccessTools.FieldRefAccess<EnemyHealth, bool>("dead");
        private static readonly AccessTools.FieldRef<ExtractionPoint, ExtractionPoint.State> ExtractionStateRef =
            AccessTools.FieldRefAccess<ExtractionPoint, ExtractionPoint.State>("currentState");
        private static readonly AccessTools.FieldRef<RoundDirector, int> ExtractionsTotalRef =
            AccessTools.FieldRefAccess<RoundDirector, int>("extractionPoints");
        private static readonly AccessTools.FieldRef<RoundDirector, int> ExtractionsDoneRef =
            AccessTools.FieldRefAccess<RoundDirector, int>("extractionPointsCompleted");

        private static readonly Color ExtractionColor = new Color(0.3f, 0.8f, 1f);
        private static readonly Color DoneColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color EnemyColor = new Color(1f, 0.25f, 0.25f);

        private GUIStyle labelStyle;
        private GUIStyle shadowStyle;
        private GUIStyle summaryStyle;
        private GUIStyle bigStyle;
        private Texture2D arrowTexture;

        private int itemsLeft;
        private float valueLeft;
        private int enemiesActive;
        private string dangerName;
        private float dangerDist;
        private readonly HashSet<string> seenEnemies = new HashSet<string>();

        private void DrawEsp()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            itemsLeft = 0;
            valueLeft = 0f;
            enemiesActive = 0;
            dangerName = null;
            dangerDist = float.MaxValue;

            if (showExtractions.Value)
            {
                DrawExtractions(cam);
            }
            DrawItems(cam);
            DrawEnemies(cam);

            if (showSummary.Value)
            {
                DrawSummary();
            }
            if (dangerWarning.Value && dangerName != null)
            {
                DrawDanger();
            }
        }

        private void DrawItems(Camera cam)
        {
            foreach (ValuableObject valuable in valuables)
            {
                if (valuable == null)
                {
                    continue;
                }

                float value = ValueRef(valuable);
                itemsLeft++;
                valueLeft += value;

                if (!showItems.Value || value < minItemValue.Value)
                {
                    continue;
                }

                string text = CleanName(valuable.name) + "\n$" + value.ToString("N0");
                if (showFragile.Value && valuable.durabilityPreset != null && valuable.durabilityPreset.fragility >= fragileThreshold.Value)
                {
                    text += "\nFRAGILE";
                }

                Color color = colorByValue.Value ? ValueColor(value) : Color.green;
                DrawLabel(cam, valuable.transform.position, text, color);
            }
        }

        private void DrawEnemies(Camera cam)
        {
            if (EnemyDirector.instance == null)
            {
                return;
            }

            foreach (EnemyParent parent in EnemyDirector.instance.enemiesSpawned)
            {
                if (parent == null || !SpawnedRef(parent))
                {
                    continue;
                }

                Vector3 pos = parent.transform.position;
                string text = parent.enemyName;

                Enemy enemy = EnemyRef(parent);
                if (enemy != null)
                {
                    if (enemy.CenterTransform != null)
                    {
                        pos = enemy.CenterTransform.position;
                    }

                    EnemyHealth health = EnemyHealthRef(enemy);
                    if (health != null)
                    {
                        if (DeadRef(health))
                        {
                            continue;
                        }
                        if (showEnemyHealth.Value)
                        {
                            text += "\n" + HealthCurrentRef(health) + "/" + health.health + " HP";
                        }
                    }
                }

                if (seenEnemies.Add(parent.enemyName))
                {
                    Logger.LogInfo("Enemy type seen: " + parent.enemyName);
                }

                enemiesActive++;
                float distance = Vector3.Distance(cam.transform.position, pos);
                if (distance <= dangerDistance.Value && distance < dangerDist)
                {
                    dangerDist = distance;
                    dangerName = parent.enemyName;
                }

                if (!showEnemies.Value)
                {
                    continue;
                }

                if (showEnemyTips.Value)
                {
                    string tip = EnemyTips.Get(parent.enemyName);
                    if (tip != null)
                    {
                        text += "\n" + tip;
                    }
                }

                bool onScreen = DrawLabel(cam, pos, text, EnemyColor);
                if (!onScreen && offscreenArrows.Value && distance <= maxDistance.Value)
                {
                    DrawArrow(cam, pos, parent.enemyName + " " + distance.ToString("0") + "m", EnemyColor);
                }
            }
        }

        private void DrawExtractions(Camera cam)
        {
            foreach (ExtractionPoint point in extractions)
            {
                if (point == null)
                {
                    continue;
                }

                ExtractionPoint.State state = ExtractionStateRef(point);
                string text = "Extraction";
                Color color = ExtractionColor;

                if (state == ExtractionPoint.State.Complete)
                {
                    text += " (done)";
                    color = DoneColor;
                }
                else if (state == ExtractionPoint.State.Active)
                {
                    text += " (ACTIVE)\nGoal $" + point.haulGoal.ToString("N0");
                }

                DrawLabel(cam, point.transform.position + Vector3.up * 1.5f, text, color);
            }
        }

        private void DrawSummary()
        {
            string text = "Valuables: " + itemsLeft + " ($" + valueLeft.ToString("N0") + ")";
            text += "\nEnemies: " + enemiesActive;

            if (RoundDirector.instance != null)
            {
                int total = ExtractionsTotalRef(RoundDirector.instance);
                if (total > 0)
                {
                    text += "\nExtractions: " + ExtractionsDoneRef(RoundDirector.instance) + "/" + total;
                }
            }

            Vector2 size = summaryStyle.CalcSize(new GUIContent(text));
            Rect rect = new Rect(12f, Screen.height * 0.3f, size.x, size.y);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, Shadow(summaryStyle));
            summaryStyle.normal.textColor = Color.white;
            GUI.Label(rect, text, summaryStyle);
        }

        private void DrawDanger()
        {
            string text = "DANGER: " + dangerName + " " + dangerDist.ToString("0") + "m";
            string tip = EnemyTips.Get(dangerName);
            if (tip != null)
            {
                text += "\n" + tip;
            }

            float alpha = 0.6f + 0.4f * Mathf.PingPong(Time.unscaledTime * 2f, 1f);
            Color color = new Color(1f, 0.2f, 0.2f, alpha);
            DrawText(new Vector2(Screen.width / 2f, Screen.height * 0.18f), text, color, bigStyle);
        }

        private bool DrawLabel(Camera cam, Vector3 worldPos, string text, Color color)
        {
            float distance = Vector3.Distance(cam.transform.position, worldPos);
            if (distance > maxDistance.Value)
            {
                return false;
            }

            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
            {
                return false;
            }

            Vector2 screenPos = new Vector2(vp.x * Screen.width, (1f - vp.y) * Screen.height);
            DrawText(screenPos, text + "\n" + distance.ToString("0") + "m", color, labelStyle);
            return true;
        }

        private void DrawArrow(Camera cam, Vector3 worldPos, string text, Color color)
        {
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0f)
            {
                vp.x = 1f - vp.x;
                vp.y = 1f - vp.y;
            }

            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector2.down;
            }
            dir *= 0.45f / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));

            Vector2 pos = new Vector2((0.5f + dir.x) * Screen.width, (0.5f - dir.y) * Screen.height);
            Vector2 guiDir = new Vector2(dir.x, -dir.y).normalized;
            float angle = Mathf.Atan2(guiDir.y, guiDir.x) * Mathf.Rad2Deg;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pos);
            GUI.color = color;
            GUI.DrawTexture(new Rect(pos.x - 12f, pos.y - 12f, 24f, 24f), arrowTexture);
            GUI.color = Color.white;
            GUI.matrix = oldMatrix;

            DrawText(pos - guiDir * 34f, text, color, labelStyle);
        }

        private void DrawText(Vector2 center, string text, Color color, GUIStyle style)
        {
            Vector2 size = style.CalcSize(new GUIContent(text));
            Rect rect = new Rect(center.x - size.x / 2f, center.y - size.y / 2f, size.x, size.y);

            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, Shadow(style));
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private GUIStyle Shadow(GUIStyle style)
        {
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            return shadowStyle;
        }

        private void SetupStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.wordWrap = false;

                shadowStyle = new GUIStyle(labelStyle);
                shadowStyle.normal.textColor = Color.black;

                summaryStyle = new GUIStyle(labelStyle);
                summaryStyle.alignment = TextAnchor.UpperLeft;

                bigStyle = new GUIStyle(labelStyle);

                arrowTexture = MakeArrowTexture();
            }

            labelStyle.fontSize = fontSize.Value;
            summaryStyle.fontSize = fontSize.Value + 1;
            bigStyle.fontSize = fontSize.Value + 9;
        }

        private static Texture2D MakeArrowTexture()
        {
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool inside = x >= 4 && x <= 28 && Mathf.Abs(y - 15.5f) <= 16f * (28 - x) / 24f;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Color ValueColor(float value)
        {
            if (value >= 10000f)
            {
                return new Color(1f, 0.6f, 0f);
            }
            if (value >= 5000f)
            {
                return Color.yellow;
            }
            if (value >= 1000f)
            {
                return Color.green;
            }
            return new Color(0.85f, 0.85f, 0.85f);
        }

        private static string CleanName(string name)
        {
            return name.Replace("(Clone)", "").Replace("Valuable ", "").Trim();
        }
    }
}
