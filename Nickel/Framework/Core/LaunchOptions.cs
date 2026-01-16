using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nickel;

internal static class LaunchOptions
{
	public static readonly Lazy<IReadOnlyList<Option>> All = new(
		() => typeof(LaunchOptions).GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(f => f.FieldType.IsAssignableTo(typeof(Option)))
			.Select(f => (Option)f.GetValue(null)!)
			.ToList()
	);
	
	// public static readonly Option<bool?> Vanilla = new("--vanilla", () => false, "Whether to run the vanilla game instead.") { Arity = ArgumentArity.ZeroOrOne };
	public static readonly Option<bool?> WrapLaunch = new("--wrap-launch", "Whether the mod loader should wrap another instance of it, allowing catching fatal exceptions.");
	public static readonly Option<bool?> Debug = new("--debug", "Whether the game should be ran in debug mode.");
	public static readonly Option<bool?> SaveInDebug = new("--save-in-debug", "Whether the game should be auto-saved even in debug mode.");
	public static readonly Option<bool?> InitSteam = new("--init-steam", "Whether Steam integration should be enabled.");
	public static readonly Option<FileInfo?> GamePath = new("--game-path", "The path to CobaltCore.exe.");
	public static readonly Option<DirectoryInfo?> ModsPath = new("--mods-path", "The path containing the mods to load.");
	public static readonly Option<DirectoryInfo?> InternalModsPath = new("--internal-mods-path", "The path containing the internal mods to load.");
	public static readonly Option<DirectoryInfo?> ModStoragePath = new("--mod-storage-path", "The path containing mod data, like settings (ones that are fine to share).");
	public static readonly Option<DirectoryInfo?> PrivateModStoragePath = new("--private-mod-storage-path", "The path containing private mod data, like settings (ones that should never be shared).");
	public static readonly Option<DirectoryInfo?> SavePath = new("--save-path", "The path that will store the save data.");
	public static readonly Option<DirectoryInfo?> LogPath = new("--log-path", "The folder logs will be stored in.");
	public static readonly Option<DirectoryInfo?> AssemblyCachePath = new("--assembly-cache-path", "The path that will store cached assemblies.");
	public static readonly Option<string?> AttachDebuggerBeforeMod = new("--attach-debugger-before-mod", "The mod loader will attempt to attach a debugger before the given mod loads.");
	public static readonly Option<string?> AttachDebuggerAfterMod = new("--attach-debugger-after-mod", "The mod loader will attempt to attach a debugger after the given mod loads.");
	public static readonly Option<string?> AttachDebuggerBeforeModLoadPhase = new("--attach-debugger-before-mod-load-phase", "The mod loader will attempt to attach a debugger before mods from the given mod load phase start loading.");
	public static readonly Option<string?> AttachDebuggerAfterModLoadPhase = new("--attach-debugger-after-mod-load-phase", "The mod loader will attempt to attach a debugger after mods from the given mod load phase finish loading.");
	public static readonly Option<bool?> TimestampedLogFiles = new("--keep-logs", "Uses timestamps for log filenames.");
	public static readonly Option<bool?> RestartWithTerminal = new("--restart-with-terminal", "Whether the mod loader should restart itself via a terminal, making all logs visible.") { IsHidden = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) };
	public static readonly Option<string?> LogPipeName = new("--log-pipe-name") { IsHidden = true };
}
