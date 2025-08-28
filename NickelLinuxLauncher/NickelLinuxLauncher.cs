#pragma warning disable IDE0130
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Nickel.LinuxLauncher;

internal sealed class NickelLinuxLauncher
{
	private static string[] supportedTerminals = [
		// the most recommended way to launch terminal, although user had to manually install it
		"xdg-terminal-exec", 
		// we only support desktop-shipped terminal
		"cosmic-term", // Cosmic
		"konsole", // KDE
		"gnome-terminal", // GNOME
		"xfce4-terminal", // XFCE
		"xterm" // ol' reliable
	];

	internal static int Main(string[] args)
	{
		var skipLauncherOption = new Option<bool>(
			"--skipLauncher", () => false, $"Whether {NickelConstants.Name} should be ran directly, instead of {NickelConstants.Name}Launcher."
			) { Arity = ArgumentArity.ZeroOrOne };

		var terminalOption = new Option<string?>("--terminal", "A preferred terminal to run. Fallbacks to any supported terminal if it does not exists.");
		var executablePathOption = new Option<FileInfo?>("--executablePath", "The path of the executable to run. Overrides `--skipLauncher`.");

		var rootCommand = new RootCommand(NickelConstants.IntroMessage) { TreatUnmatchedTokensAsErrors = false };
		rootCommand.AddOption(skipLauncherOption);
		rootCommand.AddOption(executablePathOption);
		rootCommand.AddOption(terminalOption);

		rootCommand.SetHandler(context =>
		{
			var launchArguments = new LaunchArguments
			{
				SkipLauncher = context.ParseResult.GetValueForOption(skipLauncherOption),
				Terminal = context.ParseResult.GetValueForOption(terminalOption),
				ExecutablePath = context.ParseResult.GetValueForOption(executablePathOption),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens,
			};
			context.ExitCode = CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static int CreateAndStartInstance(LaunchArguments arguments)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			Console.WriteLine($"This launcher is only supported on Linux. Please run {NickelConstants.Name}Launcher or {NickelConstants.Name} instead.");
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

		if (arguments.Terminal is not null && File.Exists("/usr/bin/" + arguments.Terminal))
		{
			return GetTerminalProcess(arguments.Terminal);
		}

		foreach (var term in supportedTerminals)
		{
			if (File.Exists("/usr/bin/" + term))
			{
				return GetTerminalProcess(term);
			}
		}

		ProcessStartInfo GetTerminalProcess(string term)
		{
			var psi = new ProcessStartInfo
			{
				CreateNoWindow = false,
				ErrorDialog = true,
				WorkingDirectory = AppContext.BaseDirectory,
				FileName = "/usr/bin/" + term,
				UseShellExecute = true
			};

			// special case
			if (term != "xdg-terminal-exec")
			{
				psi.ArgumentList.Add("-e");
				psi.ArgumentList.Add("sh");
				psi.ArgumentList.Add("-c");
			}

			psi.ArgumentList.Add("\"" + executablePath.FullName + "\"");

			foreach (var unmatchedArgument in arguments.UnmatchedArguments)
			{
				psi.ArgumentList.Add(unmatchedArgument);
			}

			return psi;	
		}

		var psiNoTerm = new ProcessStartInfo
		{
			CreateNoWindow = false,
			ErrorDialog = true,
			WorkingDirectory = AppContext.BaseDirectory,
			FileName = executablePath.FullName
		};

		foreach (var unmatchedArgument in arguments.UnmatchedArguments)
		{
			psiNoTerm.ArgumentList.Add(unmatchedArgument);
		}

		Console.WriteLine("No supported terminal, starting without a terminal.");
		Console.WriteLine("It is recommended to install 'xdg-terminal-exec' to open unsupported terminal.");

		return psiNoTerm;
	}

	private static FileInfo GetExecutablePath(LaunchArguments arguments)
	{
		if (arguments.ExecutablePath is not null)
		{
			return arguments.ExecutablePath;
		}

		if (arguments.SkipLauncher)
		{
			return new FileInfo(Path.Combine(AppContext.BaseDirectory, NickelConstants.Name));
		}

		return new FileInfo(Path.Combine(AppContext.BaseDirectory, $"{NickelConstants.Name}Launcher"));
	}
}
