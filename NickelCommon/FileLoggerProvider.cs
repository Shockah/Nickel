using Microsoft.Extensions.Logging;
using Nickel.Common;
using System;
using System.IO;
using System.Text;

namespace Nickel;

/// <summary>
/// An <see cref="ILoggerProvider"/> implementation which logs messages to files.
/// </summary>
/// <param name="level">The minimum log level to log. Messages at lower levels will be discarded.</param>
/// <param name="filePath">The file to log messages to.</param>
public sealed class FileLoggerProvider(LogLevel level, string filePath) : ILoggerProvider
{
	/// <summary>
	/// Creates a <see cref="FileLoggerProvider"/> which keeps rolling logs.
	/// </summary>
	/// <param name="level">The minimum log level to log. Messages at lower levels will be discarded.</param>
	/// <param name="directoryInfo">The directory that will store the log files.</param>
	/// <param name="timestampedLogFiles">Whether the log file names should contain the current timestamp. If <c>false</c>, will only use two log files (current and previous).</param>
	/// <returns></returns>
	public static FileLoggerProvider CreateNewLog(LogLevel level, DirectoryInfo directoryInfo, bool timestampedLogFiles)
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

		return new FileLoggerProvider(level, currentFilePath);
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

	/// <inheritdoc/>
	public void Dispose()
	{
		this.StreamWriter.Flush();
		this.StreamWriter.Dispose();
	}

	/// <inheritdoc/>
	public ILogger CreateLogger(string categoryName) =>
		new Logger(
			level,
			categoryName,
			logEntry =>
			{
				lock (this)
				{
					var timeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
					this.StreamWriter.WriteLine($"[{timeString}][{logEntry.LogLevel}][{categoryName}] {logEntry.Message}");
				}
			}
		);

	private sealed class Logger(LogLevel level, string categoryName, Action<LogEntry> loggingFunction) : ILogger
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
