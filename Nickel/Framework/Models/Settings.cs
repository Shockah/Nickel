using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace Nickel;

internal sealed class Settings
{
	public sealed class LoggingSettings
	{
		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel MinimumFileLogLevel = LogLevel.Debug;
	
		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel MinimumConsoleLogLevel = LogLevel.Information;
	
		[JsonProperty]
		public DirectoryInfo? LogPath;
	
		[JsonProperty]
		public bool TimestampedLogFiles;
	
		[JsonProperty]
		public bool SanitizeUserDirectoryPath;
	
		[JsonProperty]
		public bool SanitizeUserName;
	
		[JsonProperty]
		public bool? SanitizeFileLogs;
	
		[JsonProperty]
		public bool? SanitizeConsoleLogs;
	}

	[JsonProperty]
	public LoggingSettings Logging = new();
	
	[JsonProperty]
	public bool WrapLaunch = true;
	
	[JsonProperty]
	public bool RestartWithTerminal = true;
	
	[JsonProperty]
	public DebugMode DebugMode = DebugMode.Disabled;
	
	[JsonProperty]
	public bool InitSteam = true;
	
	[JsonProperty]
	public FileInfo? GamePath;
	
	[JsonProperty]
	public DirectoryInfo? ModsPath;
	
	[JsonProperty]
	public DirectoryInfo? InternalModsPath;
	
	[JsonProperty]
	public DirectoryInfo? ModStoragePath;
	
	[JsonProperty]
	public DirectoryInfo? PrivateModStoragePath;
	
	[JsonProperty]
	public DirectoryInfo? SavePath;
	
	[JsonProperty]
	public DirectoryInfo? AssemblyCachePath;
	
	[JsonProperty]
	public string? AttachDebuggerBeforeMod;
	
	[JsonProperty]
	public string? AttachDebuggerAfterMod;
	
	[JsonProperty]
	public ModLoadPhase? AttachDebuggerBeforeModLoadPhase;
	
	[JsonProperty]
	public ModLoadPhase? AttachDebuggerAfterModLoadPhase;
}
