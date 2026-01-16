using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nickel;

internal static class NickelMacTerminalLauncher
{
	internal static bool Run(ProgramRunInfo info)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			Console.WriteLine($"This launcher is only supported on macOS. Please run {NickelConstants.Name} instead.");
			return false;
		}

		var psi = PrepareTerminalStartInfo(info.LaunchArgs);
		var process = Process.Start(psi);
		if (process is null)
		{
			Console.WriteLine($"Could not start {NickelConstants.Name}: no process was started.");
			return false;
		}

		return true;
	}
	
	private static ProcessStartInfo PrepareTerminalStartInfo(ParseResult args)
	{
		var executablePath = GetExecutablePath(args);
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

		foreach (var optionResult in args.CommandResult.Children.OfType<OptionResult>())
		{
			if (optionResult.Option == LaunchOptions.RestartWithTerminal)
				continue;
			
			if (optionResult.GetValueOrDefault() is not { } rawOptionValues)
				continue;
			var optionValues = (rawOptionValues as IEnumerable<object>) ?? [rawOptionValues];

			var alias = optionResult.Option.Aliases.MaxBy(alias => alias.Length)!;
			foreach (var optionValue in optionValues)
			{
				psi.ArgumentList.Add(alias);
				psi.ArgumentList.Add(optionValue.ToString()!);
			}
		}
		
		psi.ArgumentList.Add(LaunchOptions.RestartWithTerminal.Aliases.MaxBy(alias => alias.Length)!);
		psi.ArgumentList.Add(false.ToString());

		foreach (var unmatchedToken in args.UnmatchedTokens)
			psi.ArgumentList.Add(unmatchedToken);

		return psi;
	}

	private static FileInfo GetExecutablePath(ParseResult args)
	{
		// if (arguments.ExecutablePath is not null)
		// 	return arguments.ExecutablePath;

		return new FileInfo(Path.Combine(AppContext.BaseDirectory, NickelConstants.Name));
	}
}
