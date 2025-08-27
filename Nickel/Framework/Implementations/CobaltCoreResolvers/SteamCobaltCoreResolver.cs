using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Nanoray.PluginManager;
using OneOf;
using OneOf.Types;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ValveKeyValue;

namespace Nickel;

internal sealed class SteamCobaltCoreResolver(
	Func<IFileInfo, IFileInfo?, ICobaltCoreResolver> resolverFactory,
	ILogger logger
) : ICobaltCoreResolver
{
	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		logger.LogTrace("Attempting to resolve Cobalt Core from its Steam path...");

		var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		if (isLinux)
		{
			string[] potentialSteamPaths = [
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var/app/com.valvesoftware.Steam/data/Steam"),
			];
			
			foreach (var potentialSteamPath in potentialSteamPaths)
				if (this.HandleSteamPath(potentialSteamPath, isWindows, isOsx) is { } result)
					return result;
		}
		else if (isOsx)
		{
			string[] potentialSteamPaths = [
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Steam"),
			];
			
			foreach (var potentialSteamPath in potentialSteamPaths)
				if (this.HandleSteamPath(potentialSteamPath, isWindows, isOsx) is { } result)
					return result;
		}
		else if (isWindows)
		{
			const string steamInstallKeyName = "InstallPath";
			const string steamInstallSubKey32 = @"SOFTWARE\Valve\Steam";
			const string steamInstallSubKey64 = @"SOFTWARE\WOW6432Node\Valve\Steam";

			logger.LogTrace("Accessing Windows registry for the Steam path...");
			foreach (var subkey in new[] { steamInstallSubKey64, steamInstallSubKey32 })
			{
				using var key = Registry.LocalMachine.OpenSubKey(subkey);
				var value = key?.GetValue(steamInstallKeyName, null);
				if (value is string sValue && this.HandleSteamPath(sValue, isWindows, isOsx) is { } result)
					return result;
			}
			
			string[] potentialSteamPaths = [
				@"C:\Program Files\Steam",
				@"C:\Program Files (x86)\Steam",
				@"C:\Steam\steamapps\common",
			];
			
			foreach (var potentialSteamPath in potentialSteamPaths)
				if (this.HandleSteamPath(potentialSteamPath, isWindows, isOsx) is { } result)
					return result;
		}
		
		var steamCompatDataPath = Environment.GetEnvironmentVariable("STEAM_COMPAT_DATA_PATH");
		if (!string.IsNullOrEmpty(steamCompatDataPath) && this.HandleSteamPath(Path.GetFullPath($@"Z:{steamCompatDataPath}\..\..\.."), isWindows, isOsx) is { } result2)
			return result2; // Steam Deck / Proton path

		return new Error<string>("Could not find where the game is installed.");
	}

	private OneOf<CobaltCoreResolveResult, Error<string>>? HandleSteamPath(string steamPath, bool isWindows, bool isOsx)
	{
		logger.LogTrace("Potential Steam path: {SteamPath}", PathUtilities.SanitizePath(steamPath));
		
		// This should be safe to delete
		if (this.HandleSteamAppsPath(Path.Combine(steamPath, "steamapps"), isWindows, isOsx) is { } result)
			return result;

		// Check for steamapps folder in vdf file
		var libraryVdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
		if (!File.Exists(libraryVdfPath))
			return null;
			
		logger.LogTrace("Found Steam library VDF file: {Path}", PathUtilities.SanitizePath(libraryVdfPath));

		try
		{
			using var libraryVdfStream = File.OpenRead(libraryVdfPath);
			var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
			var kvData = kv.Deserialize(libraryVdfStream);

			if (!kvData.Name.Equals("LibraryFolders", StringComparison.InvariantCultureIgnoreCase))
				return null;

			foreach (var kvLibraryFolder in kvData)
			{
				if (kvLibraryFolder.FirstOrDefault(c => c.Name.Equals("Path", StringComparison.InvariantCultureIgnoreCase) && c.Value.ValueType == KVValueType.String) is not { } kvLibraryFolderPath)
					continue;
				
				var libraryFolderPath = kvLibraryFolderPath.Value.ToString(CultureInfo.InvariantCulture);
				if (this.HandleSteamAppsPath(Path.Combine(libraryFolderPath, "steamapps"), isWindows, isOsx) is { } result2)
					return result2;
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning("Could not deserialize Steam library VDF file: {Exception}", ex);
		}

		return null;
	}

	private OneOf<CobaltCoreResolveResult, Error<string>>? HandleSteamAppsPath(string steamAppsPath, bool isWindows, bool isOsx)
	{
		logger.LogTrace("Potential SteamApps path: {SteamAppPath}", PathUtilities.SanitizePath(steamAppsPath));
			
		var potentialPath = Path.Combine(steamAppsPath, "common", "Cobalt Core");
		if (isOsx)
			potentialPath = Path.Combine(potentialPath, "Cobalt Core.app", "Contents", "MacOS");
			
		logger.LogTrace("Potential Cobalt Core path: {SteamAppPath}", PathUtilities.SanitizePath(potentialPath));

		var directory = new DirectoryInfo(potentialPath);
		if (!directory.Exists)
			return null;

		var singleFileApplicationPath = new FileInfoImpl(new FileInfo(Path.Combine(directory.FullName, isWindows ? "CobaltCore.exe" : "CobaltCore")));
		if (!singleFileApplicationPath.Exists)
			return null;
			
		logger.LogTrace("Resolved Steam Cobalt Core path: {Path}", PathUtilities.SanitizePath(singleFileApplicationPath.FullName));

		var pdbPath = new FileInfoImpl(new FileInfo(Path.Combine(directory.FullName, "CobaltCore.pdb")));
		if (pdbPath.Exists != true)
			pdbPath = null;
			
		logger.LogTrace("Resolved Steam Cobalt Core PDB path: {Path}", pdbPath is null ? "<null>" : PathUtilities.SanitizePath(pdbPath.FullName));

		var resolver = resolverFactory(singleFileApplicationPath, pdbPath);
		return resolver.ResolveCobaltCore();
	}
}
