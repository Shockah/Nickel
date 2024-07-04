[← back to readme](README.md)

# Nickel setup

## Windows

Note: Nickel requires Windows 10+.

1. Download the latest version of Nickel from [GitHub](https://github.com/Shockah/Nickel/releases/latest) or [NexusMods](https://www.nexusmods.com/cobaltcore/mods/1).
2. Extract the `.zip` file in the place where you would like to store Nickel (and by default also the mods and logs).
	* While the actual place you extract to does not matter for playing mods, it is recommended to extract to the game's Steam folder (`C:\Program Files\Steam\steamapps\common\Cobalt Core` by default). This will help when debugging any potential issues, and will also help when creating mods of your own. Additionally, this is the path where the [Vortex mod manager for NexusMods](https://www.nexusmods.com/about/vortex/) will look for Nickel to be installed. Alternatively, if you are not interested in using Vortex, the game's application data folder (`%appdata%\CobaltCore`) is also a good spot for installing Nickel.
3. Open the extracted Nickel folder.
4. Double-click `NickelLauncher.exe` to start Nickel to let it do its initial setup.
	* While it is unlikely, if `NickelLauncher.exe` happens not to work, you can alternatively try running `Nickel.exe` instead, but doing so will reduce some of Nickel's logging capabilities.

## Steam Deck / Linux (Proton)

This guide assumes you are already familiar with the Desktop mode of a Steam Deck, or some other environment you may be using on your Linux machine.

1. [Steam Deck only] Switch from Gaming mode to Desktop mode, if you are not in it already.
2. Download the latest version of Nickel from [GitHub](https://github.com/Shockah/Nickel/releases/latest) or [NexusMods](https://www.nexusmods.com/cobaltcore/mods/1).
3. Extract the `.zip` file in the place where you would like to store Nickel (and by default also the mods and logs).
4. Click on the Steam icon in your taskbar and choose "Library".
5. In the bottom-left corner, click "Add a Game", then choose "Add a Non-Steam Game...".
6. Click the "Browse" button. Navigate to the folder where you extracted Nickel to. Choose `NickelLauncher.exe`, then click "Open".
	* While it is unlikely, if `NickelLauncher.exe` happens not to work, you can alternatively try running `Nickel.exe` instead, but doing so will reduce some of Nickel's logging capabilities.
7. Click "Add Selected Programs".
8. Right-click the new `NickelLauncher.exe` entry, then choose "Properties".
9. Paste the below line into the "Launch Options" field:  
	`STEAM_COMPAT_DATA_PATH=~/.steam/steam/steamapps/compatdata/2179850/ %command%`
	* While you are on this screen, you can also change the `NickelLauncher.exe` name in the top field to your liking.
10. Click on the "Compatibility" tab on the left.
11. Tick the "Force the use of a specific Steam Play compatibility tool" checkbox setting.
12. From the list that appeared below the previous setting, choose the highest non-experimental version of Proton. At the moment of writing this guide, that was `Proton 9.0-2`, which was confirmed to work.
13. Close the properties window.
14. [Steam Deck only] Go back into Gaming mode.

## `NickelLauncher.exe` vs `Nickel.exe`

Nickel comes with two EXE files. The core of the mod loader is `Nickel.exe` and it provides all of the functionality. It is perfectly fine to use `Nickel.exe`. However, due to some technical problems, `Nickel.exe` is not capable of logging any "fatal" issues that could occur when modding the game.

`NickelLauncher.exe` is a wrapper around `Nickel.exe`, which detects fatal errors coming from `Nickel.exe`, and logs them correctly.

# Getting mods

The two great places to find Cobalt Core mods are:
* The [Cobalt Core section on NexusMods](https://www.nexusmods.com/cobaltcore).
* The [#cc-mod-showcase forum](https://discord.com/channels/806989214133780521/1171363893474508870) on the [Rocket Rat Games' (developers') Discord server](https://discord.gg/cncV5znGwA).
	* This Discord server is also *the* place to talk about mods - be it making them, playing them, or getting support.
	* Hop into the [#cc-mod-discussion channel](https://discord.com/channels/806989214133780521/1210710707717275658) if you want to talk about mods, or need any kind of help with the mods or Nickel.
	* Hop into the [#cc-mod-dev channel](https://discord.com/channels/806989214133780521/1138540954761035827) if you want to make some mods, or just see what other modders are talking about.

# Installing mods

After setting up Nickel, you can start adding mods. By default, Nickel comes with a `ModLibrary` folder and several pre-installed mods.

To install a mod, put it into the `ModLibrary` folder. It is recommended to extract the mod and remove the `.zip` file. While newer mods can work straight out of `.zip` files, some (mostly "legacy") mods may require you to extract them into the `ModLibrary` folder.

# Updating mods

Nickel comes with pre-installed mods which do automatic update checks for your mods (including Nickel itself), but to make these work (correctly, or even at all, depending on the update source), they need to be configured.

[Update checks configuration](update-checks.md)

# Troubleshooting

Nickel keeps two log files, by default stored in the `Logs` folder, which can be found in its own folder:
* `Nickel.log` is the log file for your currently ongoing or the last session.
* `Nickel.prev.log` is the log file for your previous session. It is useful if the game crashed and you ran Nickel again by mistake without examining/sharing your log file first.

If you are having any issues with the modded game, **always** include your log file. The file contains detailed information about your mod setup and anything that is going on. **Copy-pasting the text from the console that appears when running the game is *not* the same as sharing the log file.** The log file contains much more information than the console does.

If you need help with your log file, you can jump into the [#cc-mod-discussion channel](https://discord.com/channels/806989214133780521/1210710707717275658) on the [Rocket Rat Games' (developers') Discord server](https://discord.gg/cncV5znGwA).

# Organizing the `ModLibrary`

Nickel loads mods recursively from the `ModLibrary` folder. This means you can put mods in as many different subfolders as you wish within the root `ModLibrary` folder.

If you want a folder or a `.zip` file to be ignored by Nickel, prepend its name with a `.` - for example, a folder called `.disabled` will not be looked into by Nickel.

# Legacy mods

Before Nickel was created, there existed a [much simpler, wildly different mod loader](https://github.com/Ewanderer/CobaltCoreModLoader). In the Nickel world, mods for that mod loader are called "legacy" mods. Nickel comes with a pre-installed `Nickel.Legacy` mod which handles loading of legacy mods, as if they were proper Nickel mods. Nickel and legacy mods can co-exist and even communicate with each other.

Currently, legacy mods **require** to be extracted into your `ModLibrary` folder and will not load otherwise. On the other hand, Nickel mods can be loaded directly from ZIP files, but in some cases the mods may not function properly when not extracted. **The current recommendation is to always extract your mods to avoid any problems.**

If a legacy mod is old enough, it is likely that its author did not include a `nickel.json` file with the mod yet. `nickel.json` files are how Nickel can tell that a given folder or a `.zip` file contains a mod it should try loading, and how it can figure out the mod load order. If a mod does not come with that file, Nickel will not be able to load that mod. Fortunately, after all other mods are loaded, `Nickel.Legacy` will look at such mods missing their `nickel.json` files and will try to create these files. Unfortunately, mods which just had their `nickel.json` files created cannot be loaded in retroactively, so they will only be loaded on subsequent launches of Nickel.