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

		rootCommand.SetHandler(context =>
		{
			context.ExitCode = RunCorrectProgram(context.ParseResult) ? 0 : 1;
		});
		return rootCommand.Invoke(args);
	}

	private static bool RunCorrectProgram(ParseResult args)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && (args.GetValueForOption(LaunchOptions.RestartWithTerminal) ?? true))
			return NickelMacTerminalLauncher.Run(args);
		if (args.GetValueForOption(LaunchOptions.WrapLaunch) ?? true)
			return NickelLauncher.Run(args);
		return Nickel.Run(args);
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
