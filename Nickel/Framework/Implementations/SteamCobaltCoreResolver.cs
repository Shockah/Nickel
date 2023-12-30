using Microsoft.Win32;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using VdfParser;

namespace Nickel;

internal sealed class SteamCobaltCoreResolver : ICobaltCoreResolver
{
	private Func<FileInfo, FileInfo?, ICobaltCoreResolver> ResolverFactory { get; }

	public SteamCobaltCoreResolver(Func<FileInfo, FileInfo?, ICobaltCoreResolver> resolverFactory)
	{
		this.ResolverFactory = resolverFactory;
	}

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		List<string> potentialSteamLocations = [];

		var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		if (isLinux)
		{
			potentialSteamLocations.AddRange(
				new[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share"),
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						".var/app/com.valvesoftware.Steam/data/Steam"
					),
				}
			);
		}
		else if (isOsx)
		{
			potentialSteamLocations.AddRange(
				new[]
				{
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						"Library/Application Support/Steam"
					),
				}
			);
		}
		else if (isWindows)
		{
			const string steamInstallKeyName = "InstallPath";
			const string steamInstallSubKey32 = @"SOFTWARE\Valve\Steam";
			const string steamInstallSubKey64 = @"SOFTWARE\WOW6432Node\Valve\Steam";

			foreach (var subkey in new[] { steamInstallSubKey32, steamInstallSubKey64 })
			{
				using var key = Registry.LocalMachine.OpenSubKey(subkey);
				var value = key?.GetValue(steamInstallKeyName, null);
				if (value is string sValue)
				{
					potentialSteamLocations.Add(sValue);
				}
			}

			potentialSteamLocations.AddRange(
				new[]
				{
					"C:\\Program Files\\Steam",
					"C:\\Program Files (x86)\\Steam",
					"C:\\Steam\\steamapps\\common",
				}
			);
		}

		var potentialSteamAppsPaths = new HashSet<string>();
		foreach (var potentialSteamPath in potentialSteamLocations)
		{
			// This should be safe to delete
			potentialSteamAppsPaths.Add(Path.Combine(potentialSteamPath, "steamapps"));

			// Check for steamapps folder in vdf file
			var libraryVdfPath = Path.Combine(potentialSteamPath, "steamapps", "libraryfolders.vdf");
			if (!File.Exists(libraryVdfPath))
			{
				continue;
			}

			using var libraryVdfFile = File.OpenRead(libraryVdfPath);
			var deserializer = new VdfDeserializer();

			if (deserializer.Deserialize(libraryVdfFile) is not IDictionary<string, dynamic> result)
			{
				continue;
			}

			if (result["libraryfolders"] is not IDictionary<string, dynamic> libraryFoldersVdfEntry)
			{
				continue;
			}

			foreach (var folderDynamic in libraryFoldersVdfEntry.Values)
			{
				if (folderDynamic is not IDictionary<string, dynamic> folderDict)
				{
					continue;
				}

				if (folderDict["path"] is not string path)
				{
					continue;
				}

				potentialSteamAppsPaths.Add(Path.Combine(path, "steamapps"));
			}
		}

		foreach (var potentialSteamAppPath in potentialSteamAppsPaths)
		{
			var potentialPath = Path.Combine(potentialSteamAppPath, "common", "Cobalt Core");
			if (isOsx)
			{
				potentialPath = Path.Combine(potentialPath, "Contents", "MacOS");
			}

			DirectoryInfo directory = new(potentialPath);
			if (!directory.Exists) continue;

			FileInfo singleFileApplicationPath = new(Path.Combine(directory.FullName, "CobaltCore.exe"));
			if (!singleFileApplicationPath.Exists) continue;

			FileInfo? pdbPath = new(Path.Combine(directory.FullName, "CobaltCore.pdb"));
			if (pdbPath.Exists != true) pdbPath = null;

			var resolver = this.ResolverFactory(singleFileApplicationPath, pdbPath);
			return resolver.ResolveCobaltCore();
		}

		return new Error<string>("Could not find where the game is installed.");
	}
}
