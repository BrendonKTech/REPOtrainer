REPO Trainer v{VERSION}
=======================

Everything from REPO Item ESP (valuables, enemies and extraction points through walls),
plus trainer options:
- Infinite health
- Infinite stamina
- Add or remove money
- Add or remove upgrades (health, stamina, sprint speed, grab strength, grab range,
  throw strength, extra jump, tumble launch), the same ones you buy in the shop

The trainer options only work in SOLO games or lobbies YOU host. In someone else's
lobby they're greyed out and only the ESP works.

Money and upgrades are saved into your save file like normal ones. Make a copy of your
save first if you want to keep a clean one:
  %USERPROFILE%\AppData\LocalLow\semiwork\Repo\saves


INSTALL
-------
1. In Steam, right-click R.E.P.O. > Manage > Browse local files.
   This opens the folder that contains REPO.exe.
2. Unzip EVERYTHING from this zip into that folder (say Yes if asked to replace files).
   Afterwards "Play REPO with Trainer.exe" and winhttp.dll should sit right next to REPO.exe.

BepInEx (the mod loader) is already included. If you also have REPO Item ESP installed,
that's fine: it switches itself off, because the trainer already includes the ESP.


PLAYING
-------
- With the trainer: double-click "Play REPO with Trainer.exe".
- Without mods:     launch R.E.P.O. from Steam like normal. The mod does NOT load.

The launcher starts the game through Steam, so Steam has to be installed.
The small window closes by itself once the game has started.

In game:
- Insert   open the menu. Trainer options are on the "Trainer" tab.
- F1       turn the ESP on/off
Infinite health and stamina start off every time you launch.

Windows SmartScreen may warn about the launcher because it isn't signed.
Click "More info" > "Run anyway".


IF SOMETHING GOES WRONG
-----------------------
- Trainer tab says it only works in solo/hosted games: you joined someone else's lobby.
- Check BepInEx\LogOutput.log in the game folder. It should mention "REPO Trainer".
- If you closed the launcher window while the game was starting, the mod may also load
  on normal Steam launches. Running the launcher once fixes this.


UNINSTALL
---------
Delete these from the game folder:
  winhttp.dll, doorstop_config.ini, .doorstop_version, "Play REPO with Trainer.exe", the BepInEx folder


CREDITS
-------
BepInEx (https://github.com/BepInEx/BepInEx) is included unmodified under the LGPL-2.1 licence.
Not affiliated with or endorsed by semiwork.
