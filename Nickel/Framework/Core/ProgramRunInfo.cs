using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;

namespace Nickel;

internal record ProgramRunInfo(
	ParseResult LaunchArgs,
	Settings Settings,
	DirectoryInfo ModStorageDirectory,
	List<LogEntry.Local> EarlyLogs
)
{
	public void PushEarlyLogsToConsole()
	{
		foreach (var log in this.EarlyLogs)
		{
			if (log.LogLevel >= LogLevel.Error)
				Console.Error.WriteLine(log);
			else
				Console.WriteLine(log);
		}
	}

	public void PushEarlyLogsToLogger(ILogger logger)
	{
		foreach (var log in this.EarlyLogs)
			logger.Log(log.LogLevel, "{EarlyLog}", log.Message);
	}
}
