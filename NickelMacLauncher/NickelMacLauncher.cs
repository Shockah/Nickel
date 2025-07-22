using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Nickel.MacLauncher;

internal sealed class NickelMacLauncher
{
	internal static int Main(string[] args)
	{
		var skipLauncherOption = new Option<bool>("--skipLauncher", () => false, $"Whether {NickelConstants.Name} should be ran directly, instead of {NickelConstants.Name}Launcher.") { Arity = ArgumentArity.ZeroOrOne };
		var executablePathOption = new Option<FileInfo?>("--executablePath", "The path of the executable to run. Overrides `--skipLauncher`.");

		var rootCommand = new RootCommand(NickelConstants.IntroMessage) { TreatUnmatchedTokensAsErrors = false };
		rootCommand.AddOption(skipLauncherOption);

		rootCommand.SetHandler(context =>
		{
			var launchArguments = new LaunchArguments
			{
				SkipLauncher = context.ParseResult.GetValueForOption(skipLauncherOption),
				ExecutablePath = context.ParseResult.GetValueForOption(executablePathOption),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens,
			};
			context.ExitCode = CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static int CreateAndStartInstance(LaunchArguments arguments)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			Console.WriteLine($"This launcher is only supported on macOS. Please run {NickelConstants.Name}Launcher or {NickelConstants.Name} instead.");
			return -1;
		}

		var psi = PrepareTerminalStartInfo(arguments);
		var process = Process.Start(psi);
		if (process is null)
		{
			Console.WriteLine($"Could not start {NickelConstants.Name}: no process was started.");
			return -1;
		}

		return 0;
	}

	private static ProcessStartInfo PrepareTerminalStartInfo(LaunchArguments arguments)
	{
		var executablePath = GetExecutablePath(arguments);
		var psi = new ProcessStartInfo
		{
			CreateNoWindow = false,
			ErrorDialog = true,
			WorkingDirectory = AppContext.BaseDirectory,
		};

		if (Directory.Exists("/Applications/iTerm.app"))
		{
			psi.FileName = "open";
			psi.ArgumentList.Add("-a");
			psi.ArgumentList.Add("/Applications/iTerm.app");
			psi.ArgumentList.Add(executablePath.FullName);
		}
		else
		{
			psi.FileName = "open";
			psi.ArgumentList.Add("-W");
			psi.ArgumentList.Add(executablePath.FullName);
		}
		
		foreach (var unmatchedArgument in arguments.UnmatchedArguments)
			psi.ArgumentList.Add(unmatchedArgument);

		return psi;
	}

	private static FileInfo GetExecutablePath(LaunchArguments arguments)
	{
		if (arguments.ExecutablePath is not null)
			return arguments.ExecutablePath;

		if (arguments.SkipLauncher)
			return new FileInfo(Path.Combine(AppContext.BaseDirectory, NickelConstants.Name));
		return new FileInfo(Path.Combine(AppContext.BaseDirectory, $"{NickelConstants.Name}Launcher"));
	}
}
