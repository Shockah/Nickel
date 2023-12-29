using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using OneOf;
using OneOf.Types;

namespace Nickel;

internal sealed class SteamCobaltCoreResolver : ICobaltCoreResolver
{
	private Func<FileInfo, FileInfo?, ICobaltCoreResolver> ResolverFactory { get; init; }

	public SteamCobaltCoreResolver(Func<FileInfo, FileInfo?, ICobaltCoreResolver> resolverFactory)
	{
		this.ResolverFactory = resolverFactory;
	}

	public OneOf<CobaltCoreResolveResult, Error<string>> ResolveCobaltCore()
	{
		List<string> potentialPaths = new();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			potentialPaths.AddRange(new string[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam/steamapps/common/Cobalt Core"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/steamapps/common/Cobalt Core"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/Cobalt Core"),
			});
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			potentialPaths.AddRange(new string[]
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/Steam/steamapps/common/Cobalt Core/Contents/MacOS"),
			});
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			if (Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Valve\\Steam", "SteamPath", null) is string steamPath)
				potentialPaths.Add(Path.Combine(steamPath, "steamapps\\common\\Cobalt Core"));

			potentialPaths.AddRange(new string[]
			{
				"C:\\Program Files\\Steam\\steamapps\\common\\Cobalt Core",
				"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Cobalt Core",
				"C:\\Steam\\steamapps\\common\\Cobalt Core",
			});
		}

		foreach (string potentialPath in potentialPaths)
		{
			DirectoryInfo directory = new(potentialPath);
			if (!directory.Exists)
				continue;

			FileInfo singleFileApplicationPath = new(Path.Combine(directory.FullName, "CobaltCore.exe"));
			if (!singleFileApplicationPath.Exists)
				continue;

			FileInfo? pdbPath = new(Path.Combine(directory.FullName, "CobaltCore.pdb"));
			if (pdbPath.Exists != true)
				pdbPath = null;

			var resolver = this.ResolverFactory(singleFileApplicationPath, pdbPath);
			return resolver.ResolveCobaltCore();
		}

		return new Error<string>("Could not find where the game is installed.");
	}
}
