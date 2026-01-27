using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Nickel;

internal sealed class ConsoleLoggerProvider(
	LogLevel level,
	TextWriter textWriter,
	bool disposeWriter,
	Func<string, string>? mutator = null
) : ILoggerProvider
{
	public void Dispose()
	{
		textWriter.Flush();
		if (disposeWriter)
			textWriter.Dispose();
	}

	public ILogger CreateLogger(string categoryName)
		=> new Logger(this, level, textWriter, categoryName, mutator);

	private sealed class Logger(
		ConsoleLoggerProvider provider,
		LogLevel level,
		TextWriter textWriter,
		string categoryName,
		Func<string, string>? mutator = null
	) : ILogger
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
			if (!this.IsEnabled(logLevel))
				return;
			var message = formatter(state, exception);
			message = mutator is null ? message : mutator(message);
			
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

					textWriter.Write($"[{categoryName}] {message}");
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
