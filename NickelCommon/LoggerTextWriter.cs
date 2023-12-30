using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace Nickel.Common;

public sealed class LoggerTextWriter : TextWriter
{
	public const char IgnoreChar = '\u200B';

	public override Encoding Encoding
		=> this.Out.Encoding;

	public bool IgnoreNextIfNewline { get; set; }

	private ILogger Logger { get; }
	private LogLevel LogLevel { get; }
	private TextWriter Out { get; }

	public LoggerTextWriter(ILogger logger, LogLevel logLevel, TextWriter @out)
	{
		this.Logger = logger;
		this.LogLevel = logLevel;
		this.Out = @out;
	}

	public override void Write(char[] buffer, int index, int count)
	{
		// track newline skip
		var ignoreIfNewline = this.IgnoreNextIfNewline;
		this.IgnoreNextIfNewline = false;

		// get first character if valid
		if (count == 0 || index < 0 || index >= buffer.Length)
		{
			this.Out.Write(buffer, index, count);
			return;
		}
		var firstChar = buffer[index];

		// handle output
		if (firstChar == IgnoreChar)
		{
			this.Out.Write(buffer, index + 1, count - 1);
		}
		else if (char.IsControl(firstChar) && firstChar is not ('\r' or '\n'))
		{
			this.Out.Write(buffer, index, count);
		}
		else if (IsEmptyOrNewline(buffer))
		{
			if (!ignoreIfNewline)
				this.Out.Write(buffer, index, count);
		}
		else
		{
			this.Logger.Log(this.LogLevel, "{InterceptedMessage}", new string(buffer, index, count));
		}
	}

	public override void Write(char ch)
		=> this.Out.Write(ch);

	private static bool IsEmptyOrNewline(char[] buffer)
	{
		foreach (var ch in buffer)
			if (ch is not '\n' and not '\r')
				return false;
		return true;
	}
}
