using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Nickel;

public sealed class ConsoleLoggerProvider(LogLevel level, TextWriter textWriter, bool disposeWriter) : ILoggerProvider
{
	private const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
	private const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

	public void Dispose()
	{
		textWriter.Flush();
		if (disposeWriter)
			textWriter.Dispose();
	}

	public ILogger CreateLogger(string categoryName) =>
		new Logger(level, textWriter, categoryName);

	private sealed class Logger(LogLevel level, TextWriter textWriter, string categoryName) : ILogger
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

			textWriter.Write($"[{GetColoredLogLevelString(logLevel)}]");
			textWriter.Write($"[{categoryName}]");
			textWriter.Write($" {formatter(state, exception)}");
			textWriter.WriteLine();
		}

		private static string GetColoredLogLevelString(LogLevel logLevel)
		{
			var colors = GetLogLevelConsoleColors(logLevel);
			return ColoredMessage(GetLogLevelString(logLevel), colors.Background, colors.Foreground);
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

		private static string GetForegroundColorEscapeCode(ConsoleColor color)
			=> color switch
			{
				ConsoleColor.Black => "\x1B[30m",
				ConsoleColor.DarkRed => "\x1B[31m",
				ConsoleColor.DarkGreen => "\x1B[32m",
				ConsoleColor.DarkYellow => "\x1B[33m",
				ConsoleColor.DarkBlue => "\x1B[34m",
				ConsoleColor.DarkMagenta => "\x1B[35m",
				ConsoleColor.DarkCyan => "\x1B[36m",
				ConsoleColor.Gray => "\x1B[37m",
				ConsoleColor.Red => "\x1B[1m\x1B[31m",
				ConsoleColor.Green => "\x1B[1m\x1B[32m",
				ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
				ConsoleColor.Blue => "\x1B[1m\x1B[34m",
				ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
				ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
				ConsoleColor.White => "\x1B[1m\x1B[37m",
				_ => DefaultForegroundColor // default foreground color
			};

		private static string GetBackgroundColorEscapeCode(ConsoleColor color)
			=> color switch
			{
				ConsoleColor.Black => "\x1B[40m",
				ConsoleColor.DarkRed => "\x1B[41m",
				ConsoleColor.DarkGreen => "\x1B[42m",
				ConsoleColor.DarkYellow => "\x1B[43m",
				ConsoleColor.DarkBlue => "\x1B[44m",
				ConsoleColor.DarkMagenta => "\x1B[45m",
				ConsoleColor.DarkCyan => "\x1B[46m",
				ConsoleColor.Gray => "\x1B[47m",
				_ => DefaultBackgroundColor // Use default background color
			};

		private static string ColoredMessage(string message, ConsoleColor? background, ConsoleColor? foreground)
		{
			StringBuilder sb = new();
			// Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
			if (background is not null)
				sb.Append(GetBackgroundColorEscapeCode(background.Value));
			if (foreground is not null)
				sb.Append(GetForegroundColorEscapeCode(foreground.Value));
			sb.Append(message);
			if (foreground is not null)
				sb.Append(DefaultForegroundColor); // reset to default foreground color
			if (background is not null)
				sb.Append(DefaultBackgroundColor); // reset to the background color
			return $"{sb}";
		}

		private readonly struct ConsoleColors
		{
			public ConsoleColor? Foreground { get; init; }
			public ConsoleColor? Background { get; init; }
		}
	}
}
