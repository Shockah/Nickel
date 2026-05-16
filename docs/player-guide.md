# Player guide

## Nickel setup

### Windows

Note: Nickel requires Windows 10+.

1. Download the latest version of Nickel from GitHub or NexusMods. Make sure you get the Windows version.
2. Extract the ".zip" file to a folder where you want Nickel to be installed.
	* The recommended location is your Cobalt Core Steam folder (`C:\Program Files\Steam\steamapps\common\Cobalt Core` by default). This is the most convenient location for debugging, and also the best location if you plan to create mods of your own.
3. Double-click "Nickel.exe" in the extracted folder to let Nickel perform its initial setup.
4. (Optional) Add Nickel to Steam as a non-Steam game.
	* Open Steam
	* Click "Add a Game" -> "Add a Non-Steam Game..."
	* Click "Browse" and select "Nickel.exe"
	* Click "Add Selected Programs"
	* This is optional, but useful for quick launching and keeping everything organized in your Steam library

### Steam Deck / Linux (Proton)

1. (Steam Deck only) Switch to Desktop Mode.
	* Press the Steam button, go to "Power", then select "Switch to Desktop".
2. Download the latest version of Nickel from GitHub or NexusMods. Make sure you get the Windows version.
3. Extract the ".zip" file to a folder where you want Nickel to be installed.
4. Open Steam.
5. Click "Add a Game" -> "Add a Non-Steam Game..."
6. Click "Browse" and select "Nickel.exe" from your extracted folder.
7. Click "Add Selected Programs".
8. Right-click "Nickel.exe" in your Steam library and select "Properties".
9. In "Launch Options", paste:
	```bash
	STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/2179850/ %command%
	```
10. Launch "Nickel.exe" once to let Nickel perform its initial setup.
11. (Steam Deck only) Return to Gaming Mode.

### macOS

Note: Nickel requires macOS 10.15+ (Catalina or newer).

1. Download the latest version of Nickel from GitHub or NexusMods. Make sure you get the Mac version.
2. Extract the ".zip" file to a folder where you want Nickel to be installed.
	* It is recommended to move "Nickel.app" into your "Applications" folder for easier access and troubleshooting.
3. Double-click "Nickel.app" to start Nickel and let it perform its initial setup.
	* If macOS shows a security warning, you may need to allow the app manually:
		1. Open System Settings -> Privacy & Security
		2. Click "Open Anyway" next to Nickel
	* If macOS says the app is "damaged", it is usually caused by quarantine restrictions. Open Terminal and run:
		```bash
		xattr -dr com.apple.quarantine /Applications/Nickel.app
		```
		Then try opening the app again.

## Getting mods

The two main places to find Cobalt Core mods are:
* The [Cobalt Core section on NexusMods](https://www.nexusmods.com/cobaltcore).
* The [#cc-mod-showcase forum](https://discord.com/channels/806989214133780521/1171363893474508870) on the [Rocket Rat Games' (developers') Discord server](https://discord.gg/cncV5znGwA).
	* This Discord server is also *the* place to talk about mods - be it making them, playing them, or getting support.
	* Hop into the [#cc-mod-discussion channel](https://discord.com/channels/806989214133780521/1210710707717275658) if you want to talk about mods, or need any kind of help with the mods or Nickel.
	* Hop into the [#cc-mod-dev channel](https://discord.com/channels/806989214133780521/1138540954761035827) if you want to make some mods, or just see what other modders are talking about.

## Installing mods

Mods are installed by placing them into the `ModLibrary` folder.
* `.zip` files are the simplest option: just download and drop them into `ModLibrary`.
* Extracted folders are also supported and are mainly useful for mod development or troubleshooting.

Nickel scans the `ModLibrary` folder recursively, so mods can be organized into subfolders if desired.

### Default locations

#### Windows / Steam Deck (Proton) / Linux
The `ModLibrary` folder is located inside the Nickel installation directory.

#### macOS
The `ModLibrary` folder is located at: `~/Library/Application Support/Nickel/ModLibrary`

### Notes

* You can mix `.zip` files and extracted folders freely.
* If both a `.zip` and an extracted folder for the same mod exist, Nickel will prefer the extracted version.
* Folders or archives starting with `.` (for example `.disabled`) are ignored.

## Updating mods

Nickel comes with pre-installed mods which do automatic update checks for your mods (including Nickel itself), but to make these work (correctly, or even at all, depending on the update source), they need to be configured.

[Update checks configuration](update-checks.md)

## Troubleshooting

Nickel keeps two log files, by default stored in the `Logs` folder:
* `Nickel.log` is the log file for your currently ongoing or the last session.
* `Nickel.prev.log` is the log file for your previous session. It is useful if the game crashed and you ran Nickel again by mistake without examining/sharing your log file first.

If you are having any issues with the modded game, **always** include your **log file**. The file contains detailed information about your mod setup and anything that is going on. **Copy-pasting the text from the console that appears when running the game is *not* the same as sharing the log file.** The log file contains much more information than the console does.

If you need help with your log file, you can jump into the [#cc-mod-discussion channel](https://discord.com/channels/806989214133780521/1210710707717275658) on the [Rocket Rat Games' (developers') Discord server](https://discord.gg/cncV5znGwA).

### Windows; Steam Deck / Linux (Proton)

The default `Logs` folder is contained in Nickel's folder.

### Mac

The default `Logs` folder can be found at `~/Library/Application Support/Nickel/Logs`.

## Organizing the `ModLibrary`

Nickel searches for mods recursively inside the `ModLibrary` folder. This means you can put mods in as many different subfolders as you wish within the root `ModLibrary` folder.

If you want a folder or a `.zip` file to be ignored by Nickel, prepend its name with a `.` - for example, a folder called `.disabled` will not be looked into by Nickel.

## Legacy mods

Before Nickel was created, there existed a [much simpler, wildly different mod loader](https://github.com/Ewanderer/CobaltCoreModLoader). Mods made for that system are called "legacy" mods.

Nickel includes a built-in `Nickel.Legacy` mod which can load these legacy mods as if they were normal Nickel mods. Legacy mods can coexist with Nickel mods and may also interact with them.

Legacy mods are identified differently from modern mods. If a legacy mod does not contain a `nickel.json` file, Nickel cannot recognize it during the initial mod discovery phase and therefore cannot load it.

After all mods have been discovered and the game has started, `Nickel.Legacy` will detect such legacy mods and generate `nickel.json` files for them automatically. However, mod discovery only happens once per launch. This means newly generated `nickel.json` files are not taken into account until the next time Nickel is started.

In short: legacy mods without `nickel.json` will be detected automatically, but they require a restart before they can be loaded.