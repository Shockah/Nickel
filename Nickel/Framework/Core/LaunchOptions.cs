using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nickel;

internal static class LaunchOptions
{
	public static readonly Lazy<IReadOnlyList<Option>> All = new(
		() => typeof(LaunchOptions).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			.Where(f => f.FieldType.IsAssignableTo(typeof(Option)))
			.Select(f => (Option)f.GetValue(null)!)
			.ToList()
	);
	
	// public static readonly Option<bool?> Vanilla = new("--vanilla", () => false, "Whether to run the vanilla game instead.") { Arity = ArgumentArity.ZeroOrOne };
	
	private static readonly Option<LogLevel?> MinimumFileLogLevel = new("--minimum-file-log-level", "The minimum log level that will be logged to the file.");
	private static readonly Option<LogLevel?> MinimumConsoleLogLevel = new("--minimum-console-log-level", "The minimum log level that will be logged to the console.");
	private static readonly Option<DirectoryInfo?> LogPath = new("--log-path", "The folder logs will be stored in.");
	private static readonly Option<bool?> TimestampedLogFiles = new("--keep-logs", "Uses timestamps for log filenames.");
	
	public static readonly Option<bool?> WrapLaunch = new("--wrap-launch", "Whether the mod loader should wrap another instance of it, allowing catching fatal exceptions.");
	public static readonly Option<string?> LogPipeName = new("--log-pipe-name") { IsHidden = true };
	public static readonly Option<bool?> RestartWithTerminal = new("--restart-with-terminal", "Whether the mod loader should restart itself via a terminal, making all logs visible.") { IsHidden = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) };
	
	private static readonly Option<bool?> Debug = new("--debug", "Whether the game should be ran in debug mode.");
	private static readonly Option<bool?> SaveInDebug = new("--save-in-debug", "Whether the game should be auto-saved even in debug mode.");
	private static readonly Option<bool?> InitSteam = new("--init-steam", "Whether Steam integration should be enabled.");
	private static readonly Option<FileInfo?> GamePath = new("--game-path", "The path to CobaltCore.exe.");
	private static readonly Option<DirectoryInfo?> ModsPath = new("--mods-path", "The path containing the mods to load.");
	private static readonly Option<DirectoryInfo?> InternalModsPath = new("--internal-mods-path", "The path containing the internal mods to load.");
	public static readonly Option<DirectoryInfo?> ModStoragePath = new("--mod-storage-path", "The path containing mod data, like settings (ones that are fine to share).");
	private static readonly Option<DirectoryInfo?> PrivateModStoragePath = new("--private-mod-storage-path", "The path containing private mod data, like settings (ones that should never be shared).");
	private static readonly Option<DirectoryInfo?> SavePath = new("--save-path", "The path that will store the save data.");
	private static readonly Option<DirectoryInfo?> AssemblyCachePath = new("--assembly-cache-path", "The path that will store cached assemblies.");
	private static readonly Option<string?> AttachDebuggerBeforeMod = new("--attach-debugger-before-mod", "The mod loader will attempt to attach a debugger before the given mod loads.");
	private static readonly Option<string?> AttachDebuggerAfterMod = new("--attach-debugger-after-mod", "The mod loader will attempt to attach a debugger after the given mod loads.");
	private static readonly Option<string?> AttachDebuggerBeforeModLoadPhase = new("--attach-debugger-before-mod-load-phase", "The mod loader will attempt to attach a debugger before mods from the given mod load phase start loading.");
	private static readonly Option<string?> AttachDebuggerAfterModLoadPhase = new("--attach-debugger-after-mod-load-phase", "The mod loader will attempt to attach a debugger after mods from the given mod load phase finish loading.");

	public static List<LogEntry.Local> ApplyToSettings(ParseResult launchArgs, Settings settings)
	{
		var logs = new List<LogEntry.Local>();
		
		launchArgs.Apply(MinimumFileLogLevel, ref settings.Logging.MinimumFileLogLevel);
		launchArgs.Apply(MinimumConsoleLogLevel, ref settings.Logging.MinimumConsoleLogLevel);
		launchArgs.Apply(LogPath, ref settings.Logging.LogPath);
		launchArgs.Apply(TimestampedLogFiles, ref settings.Logging.TimestampedLogFiles);
		
		launchArgs.Apply(WrapLaunch, ref settings.WrapLaunch);
		launchArgs.Apply(RestartWithTerminal, ref settings.RestartWithTerminal);

		if (launchArgs.GetValueForOption(Debug) is { } debug)
		{
			if (debug)
			{
				var saveInDebug = launchArgs.GetValueForOption(SaveInDebug) ?? true;
				settings.DebugMode = saveInDebug ? DebugMode.EnabledWithSaving : DebugMode.Enabled;
			}
			else
			{
				settings.DebugMode = DebugMode.Disabled;
			}
		}
		
		launchArgs.Apply(InitSteam, ref settings.InitSteam);
		launchArgs.Apply(GamePath, ref settings.GamePath);
		launchArgs.Apply(ModsPath, ref settings.ModsPath);
		launchArgs.Apply(InternalModsPath, ref settings.InternalModsPath);
		launchArgs.Apply(PrivateModStoragePath, ref settings.PrivateModStoragePath);
		launchArgs.Apply(SavePath, ref settings.SavePath);
		launchArgs.Apply(AssemblyCachePath, ref settings.AssemblyCachePath);
		launchArgs.Apply(AttachDebuggerBeforeMod, ref settings.AttachDebuggerBeforeMod);
		launchArgs.Apply(AttachDebuggerAfterMod, ref settings.AttachDebuggerAfterMod);
		
		if (launchArgs.GetValueForOption(AttachDebuggerBeforeModLoadPhase) is { } attachDebuggerBeforeModLoadPhaseRaw)
		{
			if (Enum.TryParse<ModLoadPhase>(attachDebuggerBeforeModLoadPhaseRaw, out var result))
				settings.AttachDebuggerBeforeModLoadPhase = result;
			else
				logs.Add(new(LogLevel.Error, $"The `{AttachDebuggerBeforeModLoadPhase.LongestAlias}` has an invalid value. Ignoring."));
		}
		
		if (launchArgs.GetValueForOption(AttachDebuggerAfterModLoadPhase) is { } attachDebuggerAfterModLoadPhaseRaw)
		{
			if (Enum.TryParse<ModLoadPhase>(attachDebuggerAfterModLoadPhaseRaw, out var result))
				settings.AttachDebuggerAfterModLoadPhase = result;
			else
				logs.Add(new(LogLevel.Error, $"The `{AttachDebuggerAfterModLoadPhase.LongestAlias}` has an invalid value. Ignoring."));
		}

		return logs;
	}
}

file static class LaunchOptionsClassExtensions
{
	extension(ParseResult launchArgs)
	{
		public void Apply<T>(Option<T?> option, ref T? setting) where T : class
		{
			if (launchArgs.GetValueForOption(option) is { } value)
				setting = value;
		}
	}
}

file static class LaunchOptionsStructExtensions
{
	extension(ParseResult launchArgs)
	{
		public void Apply<T>(Option<T?> option, ref T setting) where T : struct
		{
			if (launchArgs.GetValueForOption(option) is { } value)
				setting = value;
		}
		
		// public void Apply<T>(Option<T?> option, ref T? setting) where T : struct
		// {
		// 	if (launchArgs.GetValueForOption(option) is { } value)
		// 		setting = value;
		// }
	}
}
