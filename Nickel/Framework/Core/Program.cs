using Nanoray.PluginManager;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.InteropServices;

namespace Nickel;

internal static class Program
{
	internal static int Main(string[] args)
	{
		var rootCommand = new RootCommand(NickelConstants.IntroMessage);
		foreach (var option in LaunchOptions.All.Value)
			rootCommand.AddOption(option);

		rootCommand.SetHandler(context => context.ExitCode = RunCorrectProgram(context.ParseResult) ? 0 : 1);
		return rootCommand.Invoke(args);
	}

	private static bool RunCorrectProgram(ParseResult args)
	{
		var modStorageDirectory = args.GetValueForOption(LaunchOptions.ModStoragePath) ?? new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CobaltCore", NickelConstants.Name, "ModStorage"));

		Settings settings;
		try
		{
			settings = SettingsUtilities.ReadSettings<Settings>(new DirectoryInfoImpl(modStorageDirectory), true) ?? throw new InvalidDataException();
		}
		catch (Exception ex)
		{
			Console.WriteLine(NickelConstants.IntroMessage);
			Console.WriteLine($"ModStoragePath: {PathUtilities.SanitizePath(modStorageDirectory.FullName)}");
			Console.Error.WriteLine($"Couldn't read the {NickelConstants.Name} settings file: {ex}");
			return false;
		}

		var runInfo = new ProgramRunInfo(args, settings, modStorageDirectory);
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && (args.GetValueForOption(LaunchOptions.RestartWithTerminal) ?? true))
			return NickelMacTerminalLauncher.Run(runInfo);
		if (args.GetValueForOption(LaunchOptions.WrapLaunch) ?? true)
			return NickelLauncher.Run(runInfo);
		return Nickel.Run(runInfo);
	}
	
	internal static DirectoryInfo GetOrCreateDefaultLogDirectory()
	{
		DirectoryInfo directoryInfo;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			directoryInfo = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), NickelConstants.Name, "Logs"));
		else
			directoryInfo = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
		
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}
}
