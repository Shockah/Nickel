[← back to readme](README.md)

# Release notes

## Upcoming release

* Fixed `Nickel.Legacy` reference for legacy mods.

## 1.2.1
Released 1 August 2024.

* The package now allows overriding `ModDeployModsPath`.

## 1.2.0
Released 24 July 2024.

* The package can now rewrite `ModName` and `ModVersion` tokens in your `nickel.json` files.
* The package now requires the `nickel.json` file for legacy mods.
* `Card` card trait field assignments analyzer now only produces informational diagnostics, instead of errors.

## 1.1.0
Released 22 July 2024.

* The package now analyzes your code for any direct enum usage (mostly `Spr` and `UK`) which could cause issues at runtime.
* The package now analyzes your code for any direct `Card` card trait field assignents which could break Nickel's card trait handling.

## 1.0.2
Released 14 July 2024.

* Potentially fixed the "Could not load file or assembly 'Newtonsoft.Json (...)" error when developing mods in Visual Studio.

## 1.0.1
Released 5 July 2024.

* The package no longer looks for Nickel in `%AppData%`.
* Potentially fixed random `MSB4018` "Could not load file or assembly" errors when using the package in Visual Studio.

## 1.0.0
Released 3 July 2024.

* Legacy mods can now use .NET 8 and interact with Nickel directly via a new `INickelManifest` interface.

## 0.12.0
Released 15 May 2024.

* The package now validates the `Version` in the `nickel.json` file against the provided `ModVersion` property (defaulting to `Version`).

## 0.11.0
Released 11 May 2024.

* Legacy mods' `CobaltCore.dll` is also publicized now.

## 0.8.0
Released 14 April 2024.

* General improvements, which should hopefully fix any issues with extracting the `CobaltCore.dll` file from the `CobaltCore.exe` file provided by the game.

## 0.4.0
Released 18 January 2024.

* Updated.

## 0.2.0
Released 10 January 2024.

* Essentials: Added mod source tooltips -- enable these by holding Alt.
* Added a way to lookup modded content by their unique name.
* Publicizing the `CobaltCore.dll` assembly.
* Added missing reference to the `OneOf` library.
* Fixed not reusing assemblies provided by the game.
* Fixed the `AfterDbInit` phase actually being too late.
* Fixed character serialization.
* Fixed hookable artifacts showing up on run summary screen.

## 0.1.0
Released 9 January 2024.

* Initial alpha release.