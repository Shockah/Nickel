using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Nickel;

internal static class NickelLauncher
{
	private enum StartWrapperAndLogProcessResult
	{
		EarlyFail,
		LateFail,
		Success,
	}
	
	internal static bool Run(ProgramRunInfo info)
	{
		var realOut = Console.Out;
		var loggerFactory = LoggerFactory.Create(builder => Program.SetupDefaultLogger(builder, info, realOut));
		var logger = loggerFactory.CreateLogger($"{NickelConstants.Name}Launcher");
		Console.SetOut(new LoggerTextWriter(logger, LogLevel.Information, realOut));
		Console.SetError(new LoggerTextWriter(logger, LogLevel.Error, Console.Error));
		Dictionary<string, ILogger> categoryLoggers = [];
		logger.LogInformation("{IntroMessage}", NickelConstants.IntroMessage);

		var launchPath = new FileInfo(Environment.ProcessPath!);
		var pipeName = info.LaunchArgs.GetValueForOption(LaunchOptions.LogPipeName);
		if (string.IsNullOrEmpty(pipeName))
			pipeName = Guid.NewGuid().ToString();
		
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
			WorkingDirectory = launchPath.Directory?.FullName ?? "",
		};
		
		foreach (var optionResult in info.LaunchArgs.CommandResult.Children.OfType<OptionResult>())
		{
			if (optionResult.Option == LaunchOptions.LogPipeName)
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
		
		psi.ArgumentList.Add(LaunchOptions.WrapLaunch.LongestAlias);
		psi.ArgumentList.Add(false.ToString());
		psi.ArgumentList.Add(LaunchOptions.LogPipeName.LongestAlias);
		psi.ArgumentList.Add(pipeName);

		foreach (var unmatchedToken in info.LaunchArgs.UnmatchedTokens)
			psi.ArgumentList.Add(unmatchedToken);

		var result = StartWrapperAndLogProcess(psi, logger, loggerFactory);
		if (result == StartWrapperAndLogProcessResult.Success)
			return true;
		if (result == StartWrapperAndLogProcessResult.LateFail)
			return false;
		
		logger.LogWarning("Attempting to start {ModLoaderName} directly...", NickelConstants.Name);
		return Nickel.Run(info, loggerFactory);
	}

	private static StartWrapperAndLogProcessResult StartWrapperAndLogProcess(ProcessStartInfo psi, ILogger logger, ILoggerFactory loggerFactory)
	{
		var failedEarly = true;
		
		try
		{
			var exitingLauncher = false;
			var process = Process.Start(psi);
			if (process is null)
			{
				logger.LogCritical("Could not start {ModLoaderName}: no process was started.", NickelConstants.Name);
				return StartWrapperAndLogProcessResult.EarlyFail;
			}

			logger.LogDebug("Launched Nickel with PID {PID}.", process.Id);

			// Detect if parent process is killed
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

			failedEarly = false;

			process.WaitForExit();
			logger.Log(process.ExitCode == 0 ? LogLevel.Debug : LogLevel.Error, "{ModLoaderName} exited with code {Code}.", NickelConstants.Name, process.ExitCode);
			if (process.ExitCode != 0 && !exitingLauncher)
				Console.ReadLine();

			// Unsubscribe
			launcherProcess.Exited -= OnExited;
			Console.CancelKeyPress -= OnExited;
			AppDomain.CurrentDomain.ProcessExit -= OnExited;
			return process.ExitCode == 0 ? StartWrapperAndLogProcessResult.Success : StartWrapperAndLogProcessResult.LateFail;

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
		}
		catch (Exception ex)
		{
			logger.LogCritical("{Name} threw an exception: {Exception}", NickelConstants.Name, ex);
			return failedEarly ? StartWrapperAndLogProcessResult.EarlyFail : StartWrapperAndLogProcessResult.LateFail;
		}
	}
}
