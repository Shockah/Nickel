using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Nickel.Common;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;

namespace Nickel.Launcher;

internal class NickelLauncher
{
	internal static int Main(string[] args)
	{
		Option<FileInfo?> launchPathOption = new(
			name: "--launchPath",
			description: $"The path of the application to launch."
		);
		Option<DirectoryInfo?> logPathOption = new(
			name: "--logPath",
			description: "The folder logs will be stored in."
		);
		Option<bool?> timestampedLogFiles = new(
			name: "--keepLogs",
			description: "Uses timestamps for log filenames."
		);

		RootCommand rootCommand = new(NickelConstants.IntroMessage)
		{
			TreatUnmatchedTokensAsErrors = false
		};
		rootCommand.AddOption(launchPathOption);
		rootCommand.AddOption(logPathOption);
		rootCommand.AddOption(timestampedLogFiles);

		rootCommand.SetHandler((InvocationContext context) =>
		{
			LaunchArguments launchArguments = new()
			{
				LaunchPath = context.ParseResult.GetValueForOption(launchPathOption),
				UnmatchedArguments = context.ParseResult.UnmatchedTokens,
				LogPath = context.ParseResult.GetValueForOption(logPathOption),
				TimestampedLogFiles = context.ParseResult.GetValueForOption(timestampedLogFiles),
			};
			CreateAndStartInstance(launchArguments);
		});
		return rootCommand.Invoke(args);
	}

	private static void CreateAndStartInstance(LaunchArguments launchArguments)
	{
		var loggerFactory = LoggerFactory.Create(builder =>
		{
			var fileLogDirectory = launchArguments.LogPath ?? GetOrCreateDefaultLogDirectory();
			var timestampedLogFiles = launchArguments.TimestampedLogFiles ?? false;
			builder.AddProvider(FileLoggerProvider.CreateNewLog(fileLogDirectory, timestampedLogFiles));

			builder.AddConfiguration();
			builder.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
			builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

			LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerOptions, ConsoleLoggerProvider>(builder.Services);
		});
		var logger = loggerFactory.CreateLogger($"{NickelConstants.Name}Launcher");
		Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, Console.Out));
		Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
		Dictionary<string, ILogger> categoryLoggers = [];
		logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);

		var launchPath = launchArguments.LaunchPath ?? new($"{NickelConstants.Name}.exe"); // TODO: this probably doesn't work on non-Windows machines; make sure it does
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
		foreach (var unmatchedArgument in launchArguments.UnmatchedArguments)
			psi.ArgumentList.Add(unmatchedArgument);

		try
		{
			StartAndLogProcess(psi, logger, loggerFactory);
		}
		catch (Exception ex)
		{
			logger.LogCritical("{Name} threw an exception: {Exception}", NickelConstants.Name, ex);
		}
	}

	private static void StartAndLogProcess(ProcessStartInfo psi, ILogger logger, ILoggerFactory loggerFactory)
	{
		var process = Process.Start(psi);
		if (process is null)
		{
			logger.LogCritical("Could not start {ModLoaderName}: no process was started.", NickelConstants.Name);
			return;
		}

		logger.LogDebug("Launched Nickel with PID {PID}.", process.Id);

		// Detect if parent process is killed
		void OnExited(object? sender, EventArgs e)
		{
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
		process.OutputDataReceived += (_, e) => launchedLogger.LogInformation("{Message}", e.Data);
		process.ErrorDataReceived += (_, e) => launchedLogger.LogError("{Message}", e.Data);
		process.BeginErrorReadLine();
		process.BeginOutputReadLine();

		process.WaitForExit();

		logger.LogDebug("{ModLoaderName} process closed gracefully.", NickelConstants.Name);
		// Unsubscribe
		launcherProcess.Exited -= OnExited;
		Console.CancelKeyPress -= OnExited;
		AppDomain.CurrentDomain.ProcessExit -= OnExited;
	}

	private static DirectoryInfo GetOrCreateDefaultLogDirectory()
	{
		var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}
}
