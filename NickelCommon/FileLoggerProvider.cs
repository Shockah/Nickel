using Microsoft.Extensions.Logging;
using Nickel.Common;
using System;
using System.IO;
using System.Text;

namespace Nickel;

public sealed class FileLoggerProvider(string filePath) : ILoggerProvider
{
	public static FileLoggerProvider CreateNewLog(DirectoryInfo directoryInfo, bool timestampedLogFiles)
	{
		if (timestampedLogFiles)
		{
			var now = DateTime.Now;
			var formattedDatetime = now.ToString("yyyy-MM-dd_HH-mm-ss");
			var timestampedFilePath = Path.Combine(directoryInfo.FullName, $"{formattedDatetime}.log");
			return new FileLoggerProvider(timestampedFilePath);
		}

		var currentFilePath = Path.Combine(directoryInfo.FullName, "Nickel.log");
		var prevFilePath = Path.Combine(directoryInfo.FullName, "Nickel.prev.log");

		if (File.Exists(currentFilePath))
			File.Move(currentFilePath, prevFilePath, true);

		return new FileLoggerProvider(currentFilePath);
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
			categoryName,
			logEntry =>
			{
				var timeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				this.StreamWriter.WriteLine($"[{timeString}][{logEntry.LogLevel}][{categoryName}] {logEntry.Message}");
			}
		);

	private sealed class Logger(string categoryName, Action<LogEntry> loggingFunction) : ILogger
	{
		private string CategoryName { get; } = categoryName;
		private Action<LogEntry> LoggingFunction { get; } = loggingFunction;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel)
			=> true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
			=> this.LoggingFunction(new(this.CategoryName, logLevel, formatter(state, exception)));
	}
}
