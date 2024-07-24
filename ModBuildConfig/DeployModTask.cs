using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Nickel.ModBuildConfig;

public sealed class DeployModTask : Task
{
	private const string ManifestFileName = "nickel.json";

	[Required]
	public string ModName { get; set; } = null!;

	[Required]
	public string ModVersion { get; set; } = null!;

	[Required]
	public bool IsLegacyMod { get; set; }

	[Required]
	public string ProjectDir { get; set; } = null!;

	[Required]
	public string TargetDir { get; set; } = null!;

	[Required]
	public bool EnableModDeploy { get; set; }

	[Required]
	public string ModDeployModsPath { get; set; } = null!;

	[Required]
	public bool EnableModZip { get; set; }

	[Required]
	public string ModZipPath { get; set; } = null!;

	public string IncludedModProjectPaths { get; set; } = "";

	public string ModVersionValidation { get; set; } = Enum.GetName(typeof(ModVersionValidation), ModBuildConfig.ModVersionValidation.Error)!;

	public override bool Execute()
	{
		if (!Enum.TryParse<ModVersionValidation>(this.ModVersionValidation, ignoreCase: true, out var modVersionValidation))
		{
			this.Log.LogError($"The `ModVersionValidation` property has an invalid value `{this.ModVersionValidation}`.");
			return false;
		}

		// skip if nothing to do
		// (this must be checked before the manifest validation, to allow cases like unit test projects)
		if (!this.EnableModDeploy && !this.EnableModZip)
			return true;

		var modFiles = this.GetModFiles(this.TargetDir, this.ProjectDir).ToList();
		
		if (modFiles.FirstOrNull(e => e.Info.Name == ManifestFileName) is not { } manifestFile || !manifestFile.Info.Exists)
		{
			this.Log.LogError($"The required `{ManifestFileName}` file is missing.");
			return false;
		}

		if (this.EnableModDeploy && !this.DeployMod(modFiles, Path.Combine(this.ModDeployModsPath, this.ModName), modVersionValidation))
			return false;
		if (this.EnableModZip && !this.ZipMod(modFiles, this.ModZipPath, this.ModName, modVersionValidation))
			return false;

		return true;
	}

	private IEnumerable<(FileInfo Info, string RelativeName)> GetModFiles(string targetDir, string projectDir)
	{
		var projectDirUri = new Uri(projectDir);

		FileInfo manifestFile = new(Path.Combine(projectDir, ManifestFileName));
		if (manifestFile.Exists)
			yield return (Info: manifestFile, RelativeName: "nickel.json");

		foreach (var file in GetAllFilesFromDirectory(new(targetDir)))
			yield return file;

		this.IncludedModProjectPaths = this.IncludedModProjectPaths.Trim();
		if (this.IncludedModProjectPaths != "")
		{
			foreach (var includedProjectPath in this.IncludedModProjectPaths.Split(';'))
			{
				var path = Path.Combine(projectDir, includedProjectPath);
				if (Directory.Exists(path))
				{
					DirectoryInfo dirInfo = new(path);
					foreach (var file in GetAllFilesFromDirectory(dirInfo))
						yield return file;
				}
				else if (File.Exists(path))
				{
					Uri fileUri = new(path);
					var relativeName = projectDirUri.MakeRelativeUri(fileUri).OriginalString;
					yield return (new FileInfo(path), relativeName);
				}
			}
		}

		static IEnumerable<(FileInfo Info, string RelativeName)> GetAllFilesFromDirectory(DirectoryInfo dirInfo)
		{
			Uri dirUri = new(dirInfo.FullName);
			foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
			{
				Uri fileUri = new(file.FullName);
				var relativeName = dirUri.MakeRelativeUri(fileUri).OriginalString;
				yield return (Info: file, RelativeName: relativeName);
			}
		}
	}

	private bool DeployMod(IEnumerable<(FileInfo Info, string RelativeName)> modFiles, string destinationDir, ModVersionValidation modVersionValidation)
	{
		foreach (var (fileInfo, fileRelativeName) in modFiles)
		{
			var fromPath = fileInfo.FullName;
			var toPath = Path.Combine(destinationDir, fileRelativeName);

			Directory.CreateDirectory(Path.GetDirectoryName(toPath)!);

			if (!this.ModifyFile(fileInfo, fileRelativeName, modVersionValidation, out var modifiedStream))
				return false;
			
			if (modifiedStream is null)
				File.Copy(fromPath, toPath, overwrite: true);
			else
				File.WriteAllBytes(toPath, modifiedStream.ToArray());
		}

		return true;
	}

	private bool ZipMod(IEnumerable<(FileInfo Info, string RelativeName)> modFiles, string destinationFile, string? innerDirName, ModVersionValidation modVersionValidation)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
		using Stream zipStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write);
		using ZipArchive archive = new(zipStream, ZipArchiveMode.Create);

		foreach (var (fileInfo, fileRelativeName) in modFiles)
		{
			var fromPath = fileInfo.FullName;
			var zipEntryName = fileRelativeName.Replace(Path.DirectorySeparatorChar, '/');
			if (innerDirName is not null)
				zipEntryName = $"{innerDirName}/{zipEntryName}";
			
			if (!this.ModifyFile(fileInfo, fileRelativeName, modVersionValidation, out var modifiedStream))
				return false;
			
			using var fileStreamInZip = archive.CreateEntry(zipEntryName).Open();
			using var fileStream = (Stream?)modifiedStream ?? new FileStream(fromPath, FileMode.Open, FileAccess.Read);
			fileStream.CopyTo(fileStreamInZip);
		}

		return true;
	}

	private bool ModifyFile(FileInfo info, string relativeName, ModVersionValidation modVersionValidation, out MemoryStream? resultStream)
	{
		resultStream = null;
		if (relativeName != ManifestFileName)
			return true;

		var properties = new Dictionary<string, string>
		{
			{ nameof(this.ModName), this.ModName },
			{ nameof(this.ModVersion), this.ModVersion },
		};
		
		using var fileStream = info.OpenRead();
		using var reader = new StreamReader(fileStream);
		using var jsonReader = new JsonTextReader(reader);

		var modified = false;
		var json = JToken.ReadFrom(jsonReader);
		
		if (json is not JObject)
		{
			this.Log.LogError($"The `{ManifestFileName}` file is not a valid JSON file.");
			return false;
		}

		if (ModifyToken(json, ref modified) is { } resultJson)
			json = resultJson;
		
		if (json.Value<string>("Version") is not { } manifestVersion)
		{
			this.Log.LogError($"The `{ManifestFileName}` file does is missing a required `Version` field.");
			return false;
		}
		if (manifestVersion.Trim() != this.ModVersion.Trim())
		{
			var message = $"The `{ManifestFileName}` file specifies a `Version` of `{manifestVersion.Trim()}` which does not match the `ModVersion` property of `{this.ModVersion.Trim()}`.";
			switch (modVersionValidation)
			{
				case ModBuildConfig.ModVersionValidation.Warning:
					this.Log.LogWarning(message);
					break;
				case ModBuildConfig.ModVersionValidation.Error:
					this.Log.LogError(message);
					return false;
				case ModBuildConfig.ModVersionValidation.Disabled:
				default:
					break;
			}
		}

		if (modified)
		{
			var memoryStream = new MemoryStream();
			var writer = new StreamWriter(memoryStream);
			writer.Write(json.ToString(Formatting.Indented));
			writer.Flush();
			
			memoryStream.Position = 0;
			resultStream = memoryStream;
		}

		return true;

		JToken? ModifyToken(JToken? token, ref bool modified)
			=> token switch
			{
				JObject @object => ModifyObject(@object, ref modified),
				JArray array => ModifyArray(array, ref modified),
				JValue value => ModifyValue(value, ref modified),
				_ => token
			};

		JObject ModifyObject(JObject @object, ref bool modified)
		{
			var result = new JObject();
			foreach (var kvp in @object)
				result[kvp.Key] = ModifyToken(kvp.Value, ref modified);
			return result;
		}

		JArray ModifyArray(JArray array, ref bool modified)
		{
			var result = new JArray();
			foreach (var token in array)
				if (ModifyToken(token, ref modified) is { } newToken)
					result.Add(newToken);
			return result;
		}

		JValue ModifyValue(JValue value, ref bool modified)
			=> value.Value is string stringValue ? JValue.CreateString(ModifyString(stringValue, ref modified)) : value;

		string ModifyString(string text, ref bool modified)
		{
			foreach (var kvp in properties)
			{
				var newText = text.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
				if (newText != text)
				{
					modified = true;
					text = newText;
				}
			}
			return text;
		}
	}
}

file static class LinqExt
{
	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();
}
