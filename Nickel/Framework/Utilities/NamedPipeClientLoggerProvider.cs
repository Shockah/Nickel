using System;
using System.IO;
using System.IO.Pipes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nickel.Common;

namespace Nickel.Framework.Utilities;

internal sealed class NamedPipeClientLoggerProvider : ILoggerProvider
{
	private string PipeName { get; }

	private JsonSerializer Serializer { get; } = JsonSerializer.Create();

	private NamedPipeClientStream? Client { get; set; }
	private JsonTextWriter? JsonWriter { get; set; }
	private bool IsClientRunning { get; set; } = false;

	public NamedPipeClientLoggerProvider(string pipeName)
	{
		this.PipeName = pipeName;
		Start(5000);
	}

	private void Start(int timeout)
	{
		if (this.IsClientRunning)
			return;
		this.IsClientRunning = true;
		this.Client = new NamedPipeClientStream(this.PipeName);
		try
		{
			this.Client.Connect(timeout);
			this.JsonWriter = new(new StreamWriter(this.Client));
		}
		catch (Exception)
		{
			this.Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		if (!this.IsClientRunning)
			return;
		IsClientRunning = false;
		JsonWriter?.Close();
		JsonWriter = null;
		Client?.Close();
		Client = null;
	}

	public ILogger CreateLogger(string categoryName)
	{
		this.Start(500);
		return new Logger(categoryName, e =>
		{
			this.Start(500);
			if (this.JsonWriter is not { } jsonWriter)
				throw new InvalidOperationException();
			this.Serializer.Serialize(jsonWriter, e);
			jsonWriter.Flush();
		});
	}

	private sealed class Logger : ILogger
	{
		private string CategoryName { get; }
		private Action<LogEntry> LoggingFunction { get; }

		public Logger(string categoryName, Action<LogEntry> loggingFunction)
		{
			this.CategoryName = categoryName;
			this.LoggingFunction = loggingFunction;
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel)
			=> true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> this.LoggingFunction(new(this.CategoryName, logLevel, formatter(state, exception)));
	}
}
