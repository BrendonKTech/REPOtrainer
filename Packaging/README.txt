REPO Item ESP v{VERSION}
========================

Shows valuables (with prices), enemies and extraction points through walls in R.E.P.O.
Client-side only. Other players don't need it. Please only use it with people who know.


INSTALL
-------
1. In Steam, right-click R.E.P.O. > Manage > Browse local files.
   This opens the folder that contains REPO.exe.
2. Unzip EVERYTHING from this zip into that folder (say Yes if asked to replace files).
   Afterwards "Play REPO with ESP.exe" and winhttp.dll should sit right next to REPO.exe.

That's it. BepInEx (the mod loader) is already included.


PLAYING
-------
- With the ESP:    double-click "Play REPO with ESP.exe" (make a desktop shortcut to it if you like).
- Without the ESP: launch R.E.P.O. from Steam like normal. The mod does NOT load.

The launcher starts the game through Steam, so Steam has to be installed.
The small window closes by itself once the game has started.

In a level:
- Insert   open the settings menu (the mouse is freed while it's open)
- F1       turn the ESP on/off
Both keys can be changed in the menu. Settings save automatically.

Windows SmartScreen may warn about the launcher because it isn't signed.
Click "More info" > "Run anyway".


IF SOMETHING GOES WRONG
-----------------------
- Nothing shows up: you need to be in a level, not the lobby. Press F1 in case it's toggled off.
- Check BepInEx\LogOutput.log in the game folder. It should mention "REPO Item ESP".
- If you closed the launcher window while the game was starting, the mod may also load
  on normal Steam launches. Running the launcher once fixes this.


UNINSTALL
---------
Delete these from the game folder:
  winhttp.dll, doorstop_config.ini, .doorstop_version, "Play REPO with ESP.exe", the BepInEx folder
Or use Steam > R.E.P.O. > Properties > Installed Files > Verify integrity (this keeps the
mod files, but they do nothing without winhttp.dll, so delete that one).


CREDITS
-------
BepInEx (https://github.com/BepInEx/BepInEx) is included unmodified under the LGPL-2.1 licence.
Not affiliated with or endorsed by semiwork.
