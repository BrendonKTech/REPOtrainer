# REPO Item ESP

A BepInEx mod for **R.E.P.O.** that shows valuables and enemies through walls, with a few extras to help newer players learn the game.

- **Valuables**: name, current dollar value, and distance, colored by value (grey < $1k, green < $5k, yellow < $10k, orange $10k+), with a **FRAGILE** tag on items that break easily
- **Enemies** (red): name, health, distance, and a short tip for some enemies
- **Off-screen arrows** at the edge of the screen pointing to enemies you can't see
- **Danger warning** when an enemy gets close
- **Extraction points**, showing which one is active and its goal
- **Level summary**: valuables left (and their total value), enemies, and extractions completed
- **In-game menu** (default **Insert**) to change everything and rebind keys
- Toggle the whole ESP with **F1**

Everything is display-only. The mod doesn't change items, enemies, or other players.

Client-side only: nothing is sent over the network, and other players don't need the mod installed. It works whether you host or join.

> Please use it in solo games or with friends who know you're using it. It's meant as a learning and practice tool, not for getting an edge in public lobbies.

---

## Installing (players)

1. Install **BepInEx 5** (x64) into your R.E.P.O. folder, either:
   - with a mod manager (r2modman / Thunderstore Mod Manager), or
   - by hand: download `BepInEx_win_x64_5.4.x` from the [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) and extract it so that `winhttp.dll` sits next to `REPO.exe`.
2. Launch the game once and quit. This creates the `BepInEx\plugins` and `BepInEx\config` folders.
3. Download `REPOItemESP.dll` from [Releases](https://github.com/BrendonKTech/REPOtrainer/releases).
4. Put it in `R.E.P.O\BepInEx\plugins\`.
5. Start the game and load into a level. Press **Insert** for the menu, **F1** to toggle the ESP.

To find your game folder: Steam → right-click R.E.P.O. → **Manage** → **Browse local files**.

---

## Configuration

The easiest way is the in-game menu (**Insert**). While it's open, the mouse is freed and the game ignores your input. Changes are saved automatically.

Settings are stored in:

```
R.E.P.O\BepInEx\config\BrendonKTech.repo.itemesp.cfg
```

| Section | Setting | Default | Description |
|---|---|---|---|
| Keys | `MenuKey` | `Insert` | Opens the menu. |
| Keys | `ToggleKey` | `F1` | Turns the ESP on/off. |
| Items | `ShowItems` | `true` | Draw valuables. |
| Items | `ColorByValue` | `true` | Color valuables by value. |
| Items | `ShowFragile` | `true` | Tag items that break easily. |
| Items | `MinItemValue` | `0` | Hide valuables worth less than this. |
| Items | `FragileThreshold` | `70` | Fragility (0–100) at or above which an item is tagged fragile. |
| Enemies | `ShowEnemies` | `true` | Draw enemies. |
| Enemies | `ShowEnemyHealth` | `true` | Show enemy health. |
| Enemies | `ShowEnemyTips` | `true` | Show a tip under known enemies. |
| Enemies | `OffscreenArrows` | `true` | Arrows pointing to enemies off-screen. |
| Enemies | `DangerWarning` | `true` | Warning when an enemy is close. |
| Enemies | `DangerDistance` | `15` | Distance (metres) for the warning. |
| World | `ShowExtractions` | `true` | Draw extraction points. |
| World | `ShowSummary` | `true` | Show the level summary. |
| Display | `MaxDistance` | `150` | Hide anything further away than this (metres). |
| Display | `FontSize` | `13` | Label text size. |

Key names follow Unity's [Input System `Key` enum](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Key.html) (e.g. `F2`, `Insert`, `Backquote`), but it's easier to rebind from the menu.

If you use **REPOConfig** or **BepInEx ConfigurationManager**, these settings also show up there. ConfigurationManager uses F1 by default too, so you may want to rebind `ToggleKey`.

### Adding enemy tips

Tips live in `EnemyTips.cs`. The mod logs every enemy type it sees to `BepInEx\LogOutput.log` (`Enemy type seen: ...`), so you can copy the exact name from there and add a line.

---

## Building from source

### Requirements
- [.NET SDK](https://dotnet.microsoft.com/download) 6.0 or newer
- R.E.P.O. installed, with BepInEx 5 set up and launched at least once

The project compiles against the game's own DLLs, which are **not** included in this repo. They're read straight from your game install.

### Point the project at your game

By default the project looks for the game at:

```
C:\Program Files (x86)\Steam\steamapps\common\REPO
```

If yours is somewhere else, create a file called `Directory.Build.props` next to the `.csproj`:

```xml
<Project>
  <PropertyGroup>
    <GameDir>D:\SteamLibrary\steamapps\common\REPO</GameDir>
  </PropertyGroup>
</Project>
```

This file is in `.gitignore`, so your local path won't get committed.

### Build

```
dotnet build -c Release
```

The output is `bin\Release\netstandard2.1\REPOItemESP.dll`. Copy it to `R.E.P.O\BepInEx\plugins\`.

---

## How it works

- Valuables are found with `FindObjectsByType<ValuableObject>()` once per second. The price comes from `ValuableObject.dollarValueCurrent`, which the host syncs to all players.
- Enemies come from `EnemyDirector.instance.enemiesSpawned`. Despawned enemies are skipped using `EnemyParent.Spawned`, and labels follow `Enemy.CenterTransform`.
- Some of those fields are `internal`, so they're read with Harmony's `AccessTools.FieldRefAccess`.
- Labels are drawn in `OnGUI`, which renders on top of the 3D scene. Positions use `WorldToViewportPoint` because the game renders at a low resolution and upscales it.
- Extraction points come from `ExtractionPoint.currentState` / `haulGoal`, the summary from `RoundDirector`, and enemy health from `EnemyHealth.healthCurrent`.
- While the menu is open, game input is blocked with the game's own `InputManager.DisableAiming` / `DisableControlsExcept`.

| File | What's in it |
|---|---|
| `Plugin.cs` | Config, hotkeys, refreshing the object lists |
| `Esp.cs` | All the drawing (labels, arrows, summary, danger warning) |
| `Menu.cs` | The in-game settings window and key rebinding |
| `EnemyTips.cs` | Enemy name → tip text |

To look through the game's code yourself, open `REPO_Data\Managed\Assembly-CSharp.dll` in [dnSpyEx](https://github.com/dnSpyEx/dnSpy) or [ILSpy](https://github.com/icsharpcode/ILSpy).

---

## Troubleshooting

**Nothing shows up / no "REPO Item ESP loaded" message**
- Check `R.E.P.O\BepInEx\LogOutput.log`. If the file doesn't exist, BepInEx isn't loading.
- Make sure `winhttp.dll` and `doorstop_config.ini` are directly next to `REPO.exe`, not inside a subfolder.
- An old `winhttp.dll` left behind by a previous mod manager install can stop BepInEx from loading. Replace it with the one from the BepInEx zip.

**Mod loads but labels don't appear**
- Press the toggle key. It may have been turned off.
- You need to be in a level, not the lobby or main menu.
- Check `MaxDistance` and `MinItemValue` in the config.

**Build fails with "could not be found" / CS0246**
- `GameDir` is pointing at the wrong folder, or BepInEx hasn't been run yet (so `BepInEx\core` doesn't exist).

**Stopped working after a game update**
- The game's classes or fields may have been renamed. Open an issue with your `LogOutput.log`.

---

## Contributing

Issues and pull requests are welcome. If you're adding a feature that changes gameplay (not just displays info), please keep it limited to solo play or lobbies you host.

---

## Disclaimer

This is a fan-made mod and is not affiliated with or endorsed by semiwork. R.E.P.O. and all related assets belong to their respective owners. No game files are included in this repository.
