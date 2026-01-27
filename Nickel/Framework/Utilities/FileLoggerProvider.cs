using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Nickel;

internal sealed class FileLoggerProvider(
	LogLevel level,
	string filePath,
	Func<string, string>? mutator = null
) : ILoggerProvider
{
	public static FileLoggerProvider CreateNewLog(
		LogLevel level,
		DirectoryInfo directoryInfo,
		bool timestampedLogFiles,
		Func<string, string>? mutator = null
	)
	{
		if (timestampedLogFiles)
		{
			var now = DateTime.Now;
			var formattedDatetime = now.ToString("yyyy-MM-dd_HH-mm-ss");
			var timestampedFilePath = Path.Combine(directoryInfo.FullName, $"{formattedDatetime}.log");
			return new FileLoggerProvider(level, timestampedFilePath);
		}

		var currentFilePath = Path.Combine(directoryInfo.FullName, "Nickel.log");
		var prevFilePath = Path.Combine(directoryInfo.FullName, "Nickel.prev.log");

		if (File.Exists(currentFilePath))
			File.Move(currentFilePath, prevFilePath, true);

		return new FileLoggerProvider(level, currentFilePath, mutator);
	}

	private StreamWriter StreamWriter { get; } = new(
		filePath,
		Encoding.UTF8,
		new FileStreamOptions
		{
			Access = FileAccess.Write,
			BufferSize = 1,
			Mode = FileMode.CreateNew,
			Options = FileOptions.Asynchronous,
		}
	)
	{ AutoFlush = true };

	public void Dispose()
	{
		this.StreamWriter.Flush();
		this.StreamWriter.Dispose();
	}

	public ILogger CreateLogger(string categoryName) =>
		new Logger(
			level,
			categoryName,
			logEntry =>
			{
				var timeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				lock (this)
				{
					var message = mutator is null ? logEntry.Message : mutator(logEntry.Message);
					this.StreamWriter.WriteLine($"[{timeString}][{logEntry.LogLevel}][{logEntry.CategoryName}] {message}");
				}
			}
		);

	private sealed class Logger(
		LogLevel level,
		string categoryName,
		Action<LogEntry> loggingFunction
	) : ILogger
	{
		private string CategoryName { get; } = categoryName;
		private Action<LogEntry> LoggingFunction { get; } = loggingFunction;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel)
			=> logLevel >= level;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
		{
			if (this.IsEnabled(logLevel))
				this.LoggingFunction(new(this.CategoryName, logLevel, formatter(state, exception)));
		}
	}
}
