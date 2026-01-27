using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
			Console.WriteLine($"ModStoragePath: {modStorageDirectory.FullName}");
			Console.Error.WriteLine($"Couldn't read the {NickelConstants.Name} settings file: {ex}");
			return false;
		}

		var earlyLogs = LaunchOptions.ApplyToSettings(args, settings);
		var runInfo = new ProgramRunInfo(args, settings, modStorageDirectory, earlyLogs);
		
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && settings.RestartWithTerminal)
			return NickelMacTerminalLauncher.Run(runInfo);
		if (settings.WrapLaunch)
			return NickelLauncher.Run(runInfo);
		return Nickel.Run(runInfo);
	}

	internal static void SetupDefaultLogger(ILoggingBuilder builder, ProgramRunInfo info, TextWriter realOut)
	{
		var sanitizeFileLogs = info.Settings.Logging.SanitizeFileLogs ?? true;
		var sanitizeConsoleLogs = info.Settings.Logging.SanitizeConsoleLogs ?? false;
		var sanitizeAnything = sanitizeFileLogs || sanitizeConsoleLogs;
		var sanitizeUserDirectoryPathRegex = sanitizeAnything && info.Settings.Logging.SanitizeUserDirectoryPath && GetHomePath() is { } homePath
			? new Regex($@"(?<=\b){Regex.Escape(homePath).Replace("\\", "/").Replace("//", @"[\\\/]+")}(?=[\W\b])") : null;
		var sanitizeUserNameRegex = sanitizeAnything && info.Settings.Logging.SanitizeUserName
			? new Regex($@"(?<=\W){Regex.Escape(Environment.UserName)}(?=\W)") : null;
		Func<string, string> logMessageMutator = message => SanitizeLogMessage(message, sanitizeUserDirectoryPathRegex, sanitizeUserNameRegex);
		
		builder.SetMinimumLevel((LogLevel)Math.Min((int)info.Settings.Logging.MinimumFileLogLevel, (int)info.Settings.Logging.MinimumConsoleLogLevel));
		var fileLogDirectory = info.Settings.Logging.LogPath ?? GetOrCreateDefaultLogDirectory();
		builder.AddProvider(FileLoggerProvider.CreateNewLog(info.Settings.Logging.MinimumFileLogLevel, fileLogDirectory, info.Settings.Logging.TimestampedLogFiles, sanitizeFileLogs ? logMessageMutator : null));
		builder.AddProvider(new ConsoleLoggerProvider(info.Settings.Logging.MinimumConsoleLogLevel, realOut, false, sanitizeConsoleLogs ? logMessageMutator : null));
	}

	private static string SanitizeLogMessage(string message, Regex? sanitizeUserDirectoryPathRegex, Regex? sanitizeUserNameRegex)
	{
		if (sanitizeUserDirectoryPathRegex is not null)
			message = sanitizeUserDirectoryPathRegex.Replace(message, "~");
		if (sanitizeUserNameRegex is not null)
			message = sanitizeUserNameRegex.Replace(message, "%username%");
		return message;
	}
	
	private static string? GetHomePath()
	{
		if (Environment.GetEnvironmentVariable("HOME") is { } homePath && !string.IsNullOrEmpty(homePath))
			return Path.GetFullPath(homePath);
		if (Environment.GetEnvironmentVariable("USERPROFILE") is { } userProfilePath && !string.IsNullOrEmpty(userProfilePath))
			return Path.GetFullPath(userProfilePath);
		return null;
	}

	private static DirectoryInfo GetOrCreateDefaultLogDirectory()
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
