using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal readonly struct LaunchArguments
{
	public bool Vanilla { get; init; }
	public bool? WrapLaunch { get; init; }
	public bool? Debug { get; init; }
	public bool? SaveInDebug { get; init; }
	public bool? InitSteam { get; init; }
	public FileInfo? GamePath { get; init; }
	public DirectoryInfo? ModsPath { get; init; }
	public DirectoryInfo? InternalModsPath { get; init; }
	public DirectoryInfo? ModStoragePath { get; init; }
	public DirectoryInfo? PrivateModStoragePath { get; init; }
	public DirectoryInfo? SavePath { get; init; }
	public DirectoryInfo? LogPath { get; init; }
	public string? AttachDebuggerBeforeMod { get; init; }
	public string? AttachDebuggerAfterMod { get; init; }
	public string? AttachDebuggerBeforeModLoadPhase { get; init; }
	public string? AttachDebuggerAfterModLoadPhase { get; init; }
	public bool? TimestampedLogFiles { get; init; }
	public string? LogPipeName { get; init; }
	public IReadOnlyList<string> UnmatchedArguments { get; init; }
}
