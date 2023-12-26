using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Nickel;

internal sealed class CustomConsoleFormatter : ConsoleFormatter
{
    private const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
    private const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

    public CustomConsoleFormatter() : base("simple")
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        textWriter.Write($"[{GetColoredLogLevelString(logEntry.LogLevel)}]");
        textWriter.Write($"[{logEntry.Category}]");
        textWriter.Write($" {logEntry.Formatter(logEntry.State, logEntry.Exception)}");
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

    private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 39;
    }

    private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.DarkRed,
            42 => ConsoleColor.DarkGreen,
            43 => ConsoleColor.DarkYellow,
            44 => ConsoleColor.DarkBlue,
            45 => ConsoleColor.DarkMagenta,
            46 => ConsoleColor.DarkCyan,
            47 => ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 49;
    }

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
