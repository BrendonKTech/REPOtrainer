using System.Collections.Generic;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOItemESP
{
    // trainer stuff, only works solo or when hosting
    public partial class Plugin
    {
        private static readonly AccessTools.FieldRef<PlayerHealth, bool> GodModeRef =
            AccessTools.FieldRefAccess<PlayerHealth, bool>("godMode");
        private static readonly AccessTools.FieldRef<PlayerHealth, int> PlayerHealthRef =
            AccessTools.FieldRefAccess<PlayerHealth, int>("health");
        private static readonly AccessTools.FieldRef<PlayerHealth, int> PlayerMaxHealthRef =
            AccessTools.FieldRefAccess<PlayerHealth, int>("maxHealth");
        private static readonly AccessTools.FieldRef<PlayerAvatar, bool> DeadSetRef =
            AccessTools.FieldRefAccess<PlayerAvatar, bool>("deadSet");
        private static readonly AccessTools.FieldRef<PunManager, PhotonView> PunViewRef =
            AccessTools.FieldRefAccess<PunManager, PhotonView>("photonView");
        private static readonly AccessTools.FieldRef<StatsManager, SortedDictionary<string, Dictionary<string, int>>> StatDictsRef =
            AccessTools.FieldRefAccess<StatsManager, SortedDictionary<string, Dictionary<string, int>>>("dictionaryOfDictionaries");

        // names have to match TesterUpgradeCommandRPC
        private static readonly (string Name, string Label)[] Upgrades =
        {
            ("Health", "Health (+20 max)"),
            ("Stamina", "Stamina (+10)"),
            ("Speed", "Sprint speed"),
            ("Strength", "Grab strength"),
            ("Range", "Grab range"),
            ("Throw", "Throw strength"),
            ("ExtraJump", "Extra jump"),
            ("Launch", "Tumble launch"),
        };

        // not saved on purpose
        private bool infiniteHealth;
        private bool infiniteStamina;

        // so we only undo our own changes
        private bool setGodMode;
        private bool setDebugEnergy;

        partial void TrainerUpdate()
        {
            bool allowed = CanCheat();
            PlayerAvatar player = PlayerAvatar.instance;

            // games debug godmode, Hurt() does nothing while its on
            PlayerHealth health = player != null ? player.playerHealth : null;
            if (health != null)
            {
                if (allowed && infiniteHealth)
                {
                    GodModeRef(health) = true;
                    setGodMode = true;

                    int missing = PlayerMaxHealthRef(health) - PlayerHealthRef(health);
                    if (missing > 0 && !DeadSetRef(player))
                    {
                        health.Heal(missing, effect: false);
                    }
                }
                else if (setGodMode)
                {
                    GodModeRef(health) = false;
                    setGodMode = false;
                }
            }

            // debug flag, stops sprint drain
            PlayerController controller = PlayerController.instance;
            if (controller != null)
            {
                if (allowed && infiniteStamina)
                {
                    controller.DebugEnergy = true;
                    controller.EnergyCurrent = controller.EnergyStart;
                    setDebugEnergy = true;
                }
                else if (setDebugEnergy)
                {
                    controller.DebugEnergy = false;
                    setDebugEnergy = false;
                }
            }
        }

        partial void DrawTrainerTab()
        {
            if (!CanCheat())
            {
                GUILayout.Label("Trainer options only work in solo games or lobbies you host.");
                return;
            }

            infiniteHealth = GUILayout.Toggle(infiniteHealth, "Infinite health");
            infiniteStamina = GUILayout.Toggle(infiniteStamina, "Infinite stamina");

            // money is in thousands
            GUILayout.Space(6f);
            GUILayout.Label("Money: $" + SemiFunc.StatGetRunCurrency() + "K");
            GUILayout.BeginHorizontal();
            MoneyButton("-10K", -10);
            MoneyButton("+10K", 10);
            MoneyButton("+100K", 100);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            PlayerAvatar player = PlayerAvatar.instance;
            if (player == null || PunManager.instance == null)
            {
                GUILayout.Label("Load into a level or the shop to change upgrades.");
                return;
            }

            GUILayout.Label("Upgrades (kept in this save, like bought ones):");
            string steamID = SemiFunc.PlayerGetSteamID(player);
            SortedDictionary<string, Dictionary<string, int>> stats = StatDictsRef(StatsManager.instance);
            foreach ((string name, string label) in Upgrades)
            {
                int level = stats.TryGetValue("playerUpgrade" + name, out Dictionary<string, int> stat) ? stat.GetValueOrDefault(steamID, 0) : 0;

                GUILayout.BeginHorizontal();
                GUILayout.Label(label + ": " + level);
                if (GUILayout.Button("-", GUILayout.Width(28f)))
                {
                    AddUpgrade(steamID, name, -1);
                }
                if (GUILayout.Button("+", GUILayout.Width(28f)))
                {
                    AddUpgrade(steamID, name, 1);
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void MoneyButton(string label, int amount)
        {
            if (GUILayout.Button(label))
            {
                // host only
                SemiFunc.StatSetRunCurrency(Mathf.Max(0, SemiFunc.StatGetRunCurrency() + amount));
            }
        }

        // same as the /upgrade debug command. rpc only works from the host
        private static void AddUpgrade(string steamID, string name, int amount)
        {
            if (SemiFunc.IsMultiplayer())
            {
                PunViewRef(PunManager.instance).RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, name, amount);
            }
            else
            {
                PunManager.instance.TesterUpgradeCommandRPC(steamID, name, amount);
            }
        }

        private static bool CanCheat()
        {
            return GameManager.instance != null && RunManager.instance != null && StatsManager.instance != null
                && SemiFunc.IsMasterClientOrSingleplayer() && !SemiFunc.IsMainMenu();
        }
    }
}
