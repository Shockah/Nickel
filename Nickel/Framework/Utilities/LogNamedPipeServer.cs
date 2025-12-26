using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Nickel;

internal sealed class LogNamedPipeServer : IDisposable
{
	private string PipeName { get; }
	private ILogger Logger { get; }
	private Action<LogEntry> LoggingFunction { get; }

	private JsonSerializer Serializer { get; } = JsonSerializer.Create();

	private NamedPipeServerStream? LastServer { get; set; }
	private bool IsServerRunning { get; set; } = true;

	public LogNamedPipeServer(string pipeName, ILogger logger, Action<LogEntry> loggingFunction)
	{
		this.PipeName = pipeName;
		this.Logger = logger;
		this.LoggingFunction = loggingFunction;
		this.Start();
	}

	private void Start()
	{
		if (!this.IsServerRunning)
			return;

		Task.Factory.StartNew(() =>
		{
			try
			{
				using var server = new NamedPipeServerStream(this.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				this.LastServer = server;
				server.WaitForConnection();

				using var streamReader = new StreamReader(server);
				using var jsonReader = new JsonTextReader(streamReader) { SupportMultipleContent = true };
				while (server.IsConnected && this.IsServerRunning)
				{
					var logEntry = this.Serializer.Deserialize<LogEntry?>(jsonReader);
					if (logEntry is null)
					{
						this.IsServerRunning = false;
						this.Dispose();
						break;
					}
					this.LoggingFunction(logEntry.Value);
					jsonReader.Read();
				}
			}
			catch (Exception ex)
			{
				if (this.IsServerRunning)
					this.Logger.LogError("There was a problem with a log named pipe {PipeName} connection: {Exception}", this.PipeName, ex);
			}
		});
	}

	public void Dispose()
	{
		if (!this.IsServerRunning)
			return;
		this.Logger.LogDebug("Stopping log named pipe {PipeName}.", this.PipeName);
		this.IsServerRunning = false;
		this.LastServer?.Close();
		this.LastServer = null;
	}
}
