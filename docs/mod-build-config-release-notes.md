[← back to readme](README.md)

# Release notes

## Upcoming release

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