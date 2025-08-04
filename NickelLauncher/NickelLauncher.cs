using Microsoft.Extensions.Logging;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Nickel.Launcher;

internal sealed class NickelLauncher
{
	internal static int Main(string[] args)
	{
		var launchPathOption = new Option<FileInfo?>("--launchPath", "The path of the application to launch.");
		var logPathOption = new Option<DirectoryInfo?>("--logPath", "The folder logs will be stored in.");
		var timestampedLogFiles = new Option<bool?>("--keepLogs", "Uses timestamps for log filenames.");

		var rootCommand = new RootCommand(NickelConstants.IntroMessage) { TreatUnmatchedTokensAsErrors = false };
		rootCommand.AddOption(launchPathOption);
		rootCommand.AddOption(logPathOption);
		rootCommand.AddOption(timestampedLogFiles);

		rootCommand.SetHandler(context =>
		{
			var launchArguments = new LaunchArguments
			{
				LaunchPath = context.ParseResult.GetValueForOption(launchPathOption),
				LogPath = context.ParseResult.GetValueForOption(logPathOption),
				TimestampedLogFiles = context.ParseResult.GetValueForOption(timestampedLogFiles),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens,
			};
			context.ExitCode = CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static int CreateAndStartInstance(LaunchArguments arguments)
	{
		var realOut = Console.Out;
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.SetMinimumLevel(LogLevel.Trace);
			var fileLogDirectory = arguments.LogPath ?? GetOrCreateDefaultLogDirectory();
			var timestampedLogFiles = arguments.TimestampedLogFiles ?? false;
			builder.AddProvider(FileLoggerProvider.CreateNewLog(LogLevel.Trace, fileLogDirectory, timestampedLogFiles));
			builder.AddProvider(new ConsoleLoggerProvider(LogLevel.Information, realOut, disposeWriter: false));
		});
		var logger = loggerFactory.CreateLogger($"{NickelConstants.Name}Launcher");
		Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, realOut));
		Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
		Dictionary<string, ILogger> categoryLoggers = [];
		logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);

		var launchPath = arguments.LaunchPath;
		if (launchPath is null)
		{
			var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{NickelConstants.Name}.exe" : NickelConstants.Name;
			launchPath = new FileInfo(Path.Combine(AppContext.BaseDirectory, executableName));
		}
		
		var pipeName = Guid.NewGuid().ToString();
		using var logNamedPipeServer = string.IsNullOrEmpty(pipeName) ? null : new LogNamedPipeServer(pipeName, logger, e =>
		{
			if (!categoryLoggers.TryGetValue(e.CategoryName, out var categoryLogger))
			{
				categoryLogger = loggerFactory.CreateLogger(e.CategoryName);
				categoryLoggers[e.CategoryName] = categoryLogger;
			}
			categoryLogger.Log(e.LogLevel, "{Message}", e.Message);
		});

		var psi = new ProcessStartInfo
		{
			FileName = launchPath.FullName,
			CreateNoWindow = true,
			ErrorDialog = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = launchPath.Directory?.FullName,
		};

		if (!string.IsNullOrEmpty(pipeName))
		{
			psi.ArgumentList.Add("--logPipeName");
			psi.ArgumentList.Add(pipeName);
		}
		foreach (var unmatchedArgument in arguments.UnmatchedArguments)
			psi.ArgumentList.Add(unmatchedArgument);

		try
		{
			return StartAndLogProcess(psi, logger, loggerFactory);
		}
		catch (Exception ex)
		{
			logger.LogCritical("{Name} threw an exception: {Exception}", NickelConstants.Name, ex);
			return -1;
		}
		finally
		{
			loggerFactory.Dispose();
		}
	}

	private static int StartAndLogProcess(ProcessStartInfo psi, ILogger logger, ILoggerFactory loggerFactory)
	{
		var exitingLauncher = false;
		var process = Process.Start(psi);
		if (process is null)
		{
			logger.LogCritical("Could not start {ModLoaderName}: no process was started.", NickelConstants.Name);
			return -1;
		}

		logger.LogDebug("Launched Nickel with PID {PID}.", process.Id);

		// Detect if parent process is killed
		void OnExited(object? _, EventArgs e)
		{
			exitingLauncher = true;
			if (process.HasExited)
				return;
			logger.LogInformation("Attempting to close {ModLoaderName} gracefully.", NickelConstants.Name);
			process.CloseMainWindow();
			process.WaitForExit(1000);

			if (process.HasExited)
				return;
			logger.LogInformation("Killing {ModLoaderName}.", NickelConstants.Name);
			process.Kill();
		}

		var launcherProcess = Process.GetCurrentProcess();
		launcherProcess.EnableRaisingEvents = true;
		launcherProcess.Exited += OnExited;
		Console.CancelKeyPress += OnExited;
		AppDomain.CurrentDomain.ProcessExit += OnExited;

		// Subscribe to logging
		var launchedLogger = loggerFactory.CreateLogger(NickelConstants.Name);
		process.OutputDataReceived += (_, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
				launchedLogger.LogInformation("{Message}", e.Data);
		};
		process.ErrorDataReceived += (_, e) =>
		{
			if (!string.IsNullOrEmpty(e.Data))
				launchedLogger.LogError("{Message}", e.Data);
		};
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();

		process.WaitForExit();
		logger.Log(process.ExitCode == 0 ? LogLevel.Debug : LogLevel.Error, "{ModLoaderName} exited with code {Code}.", NickelConstants.Name, process.ExitCode);
		if (process.ExitCode != 0 && !exitingLauncher)
			Console.ReadLine();

		// Unsubscribe
		launcherProcess.Exited -= OnExited;
		Console.CancelKeyPress -= OnExited;
		AppDomain.CurrentDomain.ProcessExit -= OnExited;
		return process.ExitCode;
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
