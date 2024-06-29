[← back to readme](README.md)

# Nickel setup

## Windows

Note: Nickel requires Windows 10+.

1. Download the [latest version of Nickel](https://github.com/Shockah/Nickel/releases/latest).
2. Extract the `.zip` file in the place where you would like to store Nickel (and by default also the mods, save files and logs).
	* While the actual place you extract to does not matter for playing mods, it is recommended to extract to the game's Steam folder (`C:\Program Files\Steam\steamapps\common\Cobalt Core` by default). This will help when debugging any potential issues, and will also help when creating mods of your own. Additionally, this is the path where the [Vortex mod manager for NexusMods](https://www.nexusmods.com/about/vortex/) will look for Nickel to be installed. Alternatively, if you are not interested in using Vortex, the game's application data folder (`%appdata%\CobaltCore`) is also a good spot for installing Nickel.
3. Open the extracted Nickel folder.
4. Double-click `NickelLauncher.exe` to start Nickel to let it do its initial setup.
	* While it is unlikely, if `NickelLauncher.exe` happens to not work, you can alternatively try running `Nickel.exe` instead, but doing so will reduce some of the Nickel's logging capabilities.

## Steam Deck / Linux (Proton)

TBD.

# Where to find mods

The two great places to find Cobalt Core mods are:
* The [Cobalt Core section on NexusMods](https://www.nexusmods.com/cobaltcore).
* The [#cc-mod-showcase forum](https://discord.com/channels/806989214133780521/1171363893474508870) on the [Rocket Rat Games' (developers') Discord server](https://discord.gg/cncV5znGwA).
	* This Discord server is also *the* place to talk about mods - be it making them, playing them, or getting support.
	* Hop into the [#cc-mod-discussion channel](https://discord.com/channels/806989214133780521/1210710707717275658) if you want to talk about mods, or need any kind of help with the mods or Nickel.
	* Hop into the [#cc-mod-dev channel](https://discord.com/channels/806989214133780521/1138540954761035827) if you want to make some mods, or just see what other modders are talking about.

# Installing mods

After setting up Nickel, you can start adding mods. By default, Nickel comes with a `ModLibrary` folder and several pre-installed mods.

To install a mod, put it into the `ModLibrary` folder. Some (mostly "legacy") mods may require you to extract them into that folder, but newer mods should not have that limitation. If you do extract, make sure you do not have both the `.zip` file and the extracted mod in the `ModLibrary` folder.

## Organizing the `ModLibrary`

Nickel loads mods recursively from the `ModLibrary` folder. This means you can put mods in as many different subfolders as you wish within the root `ModLibrary` folder.

If you want a folder or a `.zip` file to be ignored by Nickel, prepend its name with a `.` -- for example, a folder called `.disabled` will not be looked into by Nickel.

## Legacy mods

Before Nickel was created, there existed a [much simpler, wildly different mod loader](https://github.com/Ewanderer/CobaltCoreModLoader). In the Nickel world, mods for that mod loader are called "legacy" mods. Nickel comes with a pre-installed `Nickel.Legacy` mod which handles loading of legacy mods, as if they were proper Nickel mods. Nickel and legacy mods can co-exist and even communicate with each other.

Currently, legacy mods **require** to be extracted into your `ModLibrary` folder and will not load otherwise. On the other hand, Nickel mods can be loaded directly from ZIP files, but in some cases the mods may not function properly when not extracted. **The current recommendation is to always extract your mods to avoid any problems.**

If a legacy mod is old enough, it is likely that its author did not include a `nickel.json` file with the mod yet. `nickel.json` files are how Nickel can tell that a given folder or a `.zip` file contains a mod it should try loading, and how it can figure out the mod load order. If a mod does not come with that file, Nickel will not be able to load that mod. Fortunately, after all other mods are loaded, `Nickel.Legacy` will look at such mods missing their `nickel.json` files and will try to create these files. Unfortunately, mods which just had their `nickel.json` files created cannot be loaded in retroactively, so they will only be loaded on subsequent launches of Nickel.

# Updating mods

Nickel comes with pre-installed mods which do automatic update checks for your mods (including Nickel itself), but to make these work (correctly, or even at all, depending on the update source), they need to be configured.

[Update checks configuration](update-checks.md)