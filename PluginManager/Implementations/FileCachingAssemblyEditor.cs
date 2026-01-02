using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Nanoray.PluginManager;

public sealed class FileCachingAssemblyEditor(
	IAssemblyEditor wrapped,
	IWritableDirectoryInfo cacheDirectory
) : IAssemblyEditor
{
	private record Entry(
		string OriginalHash,
		string ResultHash,
		byte[] AssemblyEditorDescriptor,
		AssemblyEditorResult AssemblyEditorResult
	)
	{
		public bool InUse;
	}
	
	private readonly Dictionary<string, Entry> Entries = [];
	
	/// <inheritdoc/>
	public byte[] AssemblyEditorDescriptor
	{
		get
		{
			using var stream = new MemoryStream();
			using var writer = new BinaryWriter(stream);

			var selfDescriptor = Encoding.UTF8.GetBytes(this.GetType().FullName!);
			writer.Write(selfDescriptor.Length);
			writer.Write(selfDescriptor);

			var wrappedDescriptor = wrapped.AssemblyEditorDescriptor;
			writer.Write(wrappedDescriptor.Length);
			writer.Write(wrappedDescriptor);
			
			writer.Flush();
			return stream.ToArray();
		}
	}

	public void CleanupEntries()
	{
		foreach (var entry in this.Entries.Values.ToList())
		{
			if (entry.InUse)
				continue;
			this.RemoveEntry(entry.OriginalHash);
		}
	}

	public void ReadEntries()
	{
		this.Entries.Clear();
		
		var entriesFile = cacheDirectory.GetRelativeFile("entries.dat");
		if (!entriesFile.Exists)
			return;
		
		try
		{
			using var stream = entriesFile.OpenRead();
			using var reader = new BinaryReader(stream);

			for (var i = reader.ReadInt32(); i > 0; i--)
			{
				var originalHash = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
				var resultHash = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
				var assemblyEditorDescriptor = reader.ReadBytes(reader.ReadInt32());

				var messages = new List<AssemblyEditorResult.Message>();
				for (var j = reader.ReadInt32(); j > 0; j--)
				{
					var messageLevel = Enum.Parse<AssemblyEditorResult.MessageLevel>(Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())));
					var messageContent = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
					messages.Add(new(messageLevel, messageContent));
				}

				this.Entries[originalHash] = new(
					originalHash,
					resultHash,
					assemblyEditorDescriptor,
					new() { Messages = messages }
				);
			}
		}
		catch
		{
			// TODO: log
			this.Entries.Clear();
		}
	}

	public void WriteEntries()
	{
		var entriesFile = cacheDirectory.GetRelativeFile("entries.dat");
		using var stream = entriesFile.OpenWrite();
		using var writer = new BinaryWriter(stream);
		
		writer.Write(this.Entries.Count);
		foreach (var entry in this.Entries.Values)
		{
			var originalHashBytes = Encoding.UTF8.GetBytes(entry.OriginalHash);
			writer.Write(originalHashBytes.Length);
			writer.Write(originalHashBytes);
			
			var resultHashBytes = Encoding.UTF8.GetBytes(entry.ResultHash);
			writer.Write(resultHashBytes.Length);
			writer.Write(resultHashBytes);

			writer.Write(entry.AssemblyEditorDescriptor.Length);
			writer.Write(entry.AssemblyEditorDescriptor);
			
			writer.Write(entry.AssemblyEditorResult.Messages.Count);
			foreach (var message in entry.AssemblyEditorResult.Messages)
			{
				var messageLevelBytes = Encoding.UTF8.GetBytes(Enum.GetName(message.Level)!);
				writer.Write(messageLevelBytes.Length);
				writer.Write(messageLevelBytes);
				
				var messageContentBytes = Encoding.UTF8.GetBytes(message.Content);
				writer.Write(messageContentBytes.Length);
				writer.Write(messageContentBytes);
			}
		}

		foreach (var file in cacheDirectory.Files)
		{
			if (file is not IWritableFileInfo writableFile)
				continue;
			if (file.Name == "entries.dat")
				continue;
			if (!file.Name.EndsWith(".dll") && !file.Name.EndsWith(".pdb"))
				continue;
			if (this.Entries.ContainsKey(file.Name[..^4]))
				continue;
			
			writableFile.Delete();
		}
	}

	/// <inheritdoc/>
	public AssemblyEditorResult EditAssemblyStream(string name, ref Stream assemblyStream, ref Stream? symbolsStream)
	{
		var assemblyEditorDescriptor = this.AssemblyEditorDescriptor;
		
		var originalAssemblyMemoryStream = assemblyStream.ToMemoryStream();
		assemblyStream = originalAssemblyMemoryStream;
		var originalAssemblyHash = Convert.ToHexString(MD5.HashData(originalAssemblyMemoryStream.ToArray()));

		if (this.Entries.TryGetValue(originalAssemblyHash, out var existingEntry))
		{
			if (HandleExistingEntry(ref assemblyStream, ref symbolsStream) is { } existingResult)
				return existingResult;
			
			this.RemoveEntry(originalAssemblyHash);

			AssemblyEditorResult? HandleExistingEntry(ref Stream assemblyStream, ref Stream? symbolsStream)
			{
				if (!existingEntry.AssemblyEditorDescriptor.SequenceEqual(assemblyEditorDescriptor))
					return null;
				
				var originalAssemblyStream = assemblyStream;
				var originalSymbolsStream = symbolsStream;
			
				try
				{
					var assemblyFile = cacheDirectory.GetRelativeFile($"{existingEntry.OriginalHash}.dll");
					if (!assemblyFile.Exists)
						return null;
					var newAssemblyStream = assemblyFile.OpenRead();
				
					var symbolsFile = cacheDirectory.GetRelativeFile($"{existingEntry.OriginalHash}.pdb");
					if (!symbolsFile.Exists)
						symbolsFile = null;
					var newSymbolsStream = symbolsFile?.OpenRead();
				
					assemblyStream = newAssemblyStream;
					symbolsStream = newSymbolsStream;

					existingEntry.InUse = true;
					return existingEntry.AssemblyEditorResult;
				}
				catch
				{
					assemblyStream = originalAssemblyStream;
					symbolsStream = originalSymbolsStream;
					return null;
				}
			}
		}

		if (symbolsStream is not null)
		{
			var originalSymbolsMemoryStream = symbolsStream.ToMemoryStream();
			symbolsStream = originalSymbolsMemoryStream;
		}

		var assemblyEditorResult = wrapped.EditAssemblyStream(name, ref assemblyStream, ref symbolsStream);
		
		var resultAssemblyMemoryStream = assemblyStream.ToMemoryStream();
		assemblyStream = resultAssemblyMemoryStream;
		var resultAssemblyHash = Convert.ToHexString(MD5.HashData(resultAssemblyMemoryStream.ToArray()));

		MemoryStream? resultSymbolsMemoryStream = null;
		if (symbolsStream is not null)
		{
			resultSymbolsMemoryStream = symbolsStream.ToMemoryStream();
			symbolsStream = resultSymbolsMemoryStream;
		}
		
		this.PutEntry(
			new(
				originalAssemblyHash,
				resultAssemblyHash,
				assemblyEditorDescriptor,
				assemblyEditorResult
			) { InUse = true },
			resultAssemblyMemoryStream.ToArray(),
			resultSymbolsMemoryStream?.ToArray()
		);
		return assemblyEditorResult;
	}

	private void PutEntry(Entry entry, byte[] assemblyData, byte[]? symbolsData)
	{
		try
		{
			var assemblyFile = cacheDirectory.GetRelativeFile($"{entry.OriginalHash}.dll");
			using (var assemblyStream = assemblyFile.OpenWrite())
				assemblyStream.Write(assemblyData, 0, assemblyData.Length);

			if (symbolsData is not null)
			{
				var symbolsFile = cacheDirectory.GetRelativeFile($"{entry.OriginalHash}.pdb");
				using (var symbolsStream = symbolsFile.OpenWrite())
					symbolsStream.Write(symbolsData, 0, symbolsData.Length);
			}
			
			this.Entries[entry.OriginalHash] = entry;
		}
		catch
		{
			this.RemoveEntry(entry.OriginalHash);
			throw;
		}
	}

	private void RemoveEntry(string originalHash)
	{
		this.Entries.Remove(originalHash);

		try
		{
			var file = cacheDirectory.GetRelativeFile($"{originalHash}.dll");
			if (file.Exists)
				file.Delete();
		}
		catch
		{
			// ignored
		}
			
		try
		{
			var file = cacheDirectory.GetRelativeFile($"{originalHash}.pdb");
			if (file.Exists)
				file.Delete();
		}
		catch
		{
			// ignored
		}
	}
}

file static class StreamExt
{
	public static MemoryStream ToMemoryStream(this Stream stream, bool takeOwnership = true)
	{
		try
		{
			var memoryStream = new MemoryStream(capacity: (int)(stream.Length - stream.Position));
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0;
			return memoryStream;
		}
		finally
		{
			if (takeOwnership)
				stream.Dispose();
		}
	}
}
