using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace Nickel;

/// <summary>
/// Implements a <see cref="TextWriter"/> which outputs to an <see cref="ILogger"/>.
/// </summary>
public sealed class LoggerTextWriter : TextWriter
{
	/// <inheritdoc/>
	public override Encoding Encoding
		=> this.Out.Encoding;

	private ILogger Logger { get; }
	private LogLevel LogLevel { get; }
	private TextWriter Out { get; }

	/// <summary>
	/// Creates a new <see cref="LoggerTextWriter"/> instance.
	/// </summary>
	/// <param name="logger">The logger to output to.</param>
	/// <param name="logLevel">The log level to use for all output.</param>
	/// <param name="out">The fallback <see cref="TextWriter"/> which will be used for any special output and additional configuration.</param>
	public LoggerTextWriter(ILogger logger, LogLevel logLevel, TextWriter @out)
	{
		this.Logger = logger;
		this.LogLevel = logLevel;
		this.Out = @out;
	}

	/// <inheritdoc/>
	public override void Write(char[] buffer, int index, int count)
	{
		// get first character if valid
		if (count == 0 || index < 0 || index >= buffer.Length)
		{
			this.Out.Write(buffer, index, count);
			return;
		}
		var firstChar = buffer[index];

		// handle output
		if (char.IsControl(firstChar) && firstChar is not ('\r' or '\n'))
			this.Out.Write(buffer, index, count);
		else if (IsEmptyOrNewline(buffer))
			this.Out.Write(buffer, index, count);
		else
			this.Logger.Log(this.LogLevel, "{InterceptedMessage}", new string(buffer, index, count));
	}

	/// <inheritdoc/>
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
