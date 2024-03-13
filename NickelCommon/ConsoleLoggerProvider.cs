using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Nickel;

/// <summary>
/// An <see cref="ILoggerProvider"/> implementation which logs messages to the console.
/// </summary>
/// <param name="level">The minimum log level to log. Messages at lower levels will be discarded.</param>
/// <param name="textWriter">The console <see cref="TextWriter"/> to log messages to.</param>
/// <param name="disposeWriter">Whether the <c>textWriter</c> should be disposed when this provider is disposed.</param>
public sealed class ConsoleLoggerProvider(LogLevel level, TextWriter textWriter, bool disposeWriter) : ILoggerProvider
{
	/// <inheritdoc/>
	public void Dispose()
	{
		textWriter.Flush();
		if (disposeWriter)
			textWriter.Dispose();
	}

	/// <inheritdoc/>
	public ILogger CreateLogger(string categoryName) =>
		new Logger(this, level, textWriter, categoryName);

	private sealed class Logger(ConsoleLoggerProvider provider, LogLevel level, TextWriter textWriter, string categoryName) : ILogger
	{
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
			lock (provider)
			{
				if (!this.IsEnabled(logLevel))
					return;
				var logColor = GetLogLevelConsoleColors(logLevel);

				var oldBackgroundColor = Console.BackgroundColor;
				var oldForegroundColor = Console.ForegroundColor;
				try
				{
					textWriter.Write('[');

					Console.BackgroundColor = logColor.Background ?? oldBackgroundColor;
					Console.ForegroundColor = logColor.Foreground ?? oldForegroundColor;
					textWriter.Write(GetLogLevelString(logLevel));

					Console.BackgroundColor = oldBackgroundColor;
					Console.ForegroundColor = oldForegroundColor;
					textWriter.Write(']');

					textWriter.Write($"[{categoryName}]");
					textWriter.Write($" {formatter(state, exception)}");
					textWriter.WriteLine();

				}
				finally
				{
					Console.BackgroundColor = oldBackgroundColor;
					Console.ForegroundColor = oldForegroundColor;
				}
			}
		}

		private static string GetLogLevelString(LogLevel logLevel)
			=> logLevel switch
			{
				LogLevel.Trace => "trce",
				LogLevel.Debug => "dbug",
				LogLevel.Information => "info",
				LogLevel.Warning => "warn",
				LogLevel.Error => "fail",
				LogLevel.Critical => "crit",
				_ => throw new ArgumentOutOfRangeException(nameof(logLevel))
			};

		private static ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
			=> logLevel switch
			{
				LogLevel.Trace => new ConsoleColors { Foreground = ConsoleColor.Gray, Background = ConsoleColor.Black },
				LogLevel.Debug => new ConsoleColors { Foreground = ConsoleColor.Gray, Background = ConsoleColor.Black },
				LogLevel.Information => new ConsoleColors { Foreground = ConsoleColor.DarkGreen, Background = ConsoleColor.Black },
				LogLevel.Warning => new ConsoleColors { Foreground = ConsoleColor.Yellow, Background = ConsoleColor.Black },
				LogLevel.Error => new ConsoleColors { Foreground = ConsoleColor.Black, Background = ConsoleColor.DarkRed },
				LogLevel.Critical => new ConsoleColors { Foreground = ConsoleColor.White, Background = ConsoleColor.DarkRed },
				_ => new ConsoleColors()
			};

		private readonly struct ConsoleColors
		{
			public ConsoleColor? Foreground { get; init; }
			public ConsoleColor? Background { get; init; }
		}
	}
}
