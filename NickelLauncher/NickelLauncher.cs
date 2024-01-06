using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Nickel.Common;
using Nickel.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

		RootCommand rootCommand = new(NickelConstants.IntroMessage);
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

		var launchPath = launchArguments.LaunchPath ?? new("Nickel.exe"); // TODO: this probably doesn't work on non-Windows machines; make sure it does
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

		Process process = new();
		process.StartInfo.FileName = launchPath.FullName;
		process.StartInfo.CreateNoWindow = true;
		process.StartInfo.ErrorDialog = false;
		process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.WorkingDirectory = launchPath.Directory?.FullName;

		var launchedLogger = loggerFactory.CreateLogger(NickelConstants.Name);
		process.OutputDataReceived += (_, e) => launchedLogger.LogInformation("{Message}", e.Data);
		process.ErrorDataReceived += (_, e) => launchedLogger.LogError("{Message}", e.Data);

		if (!string.IsNullOrEmpty(pipeName))
		{
			process.StartInfo.ArgumentList.Add("--logPipeName");
			process.StartInfo.ArgumentList.Add(pipeName);
		}
		foreach (var unmatchedArgument in launchArguments.UnmatchedArguments)
			process.StartInfo.ArgumentList.Add(unmatchedArgument);

		// Handle the AppDomain.ProcessExit event to terminate the child process when the parent process exits
		Console.WriteLine("Hello World!");
		AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
		{
			Console.WriteLine("Goodnight World!");
			if (process.HasExited) return;
			// Terminate the child process
			process.Close();
			process.WaitForExit(1000); // Wait for the process to exit

			if (process.HasExited) return;
			process.Kill();
			process.WaitForExit(1000); // Wait for the process to exit
		};
		
		try
		{
			process.Start();
			process.WaitForExit();
		}
		catch (Exception ex)
		{
			logger.LogCritical("{Name} threw an exception: {Exception}", NickelConstants.Name, ex);
		}
	}

	private static DirectoryInfo GetOrCreateDefaultLogDirectory()
	{
		var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
		if (!directoryInfo.Exists)
			directoryInfo.Create();
		return directoryInfo;
	}
}
