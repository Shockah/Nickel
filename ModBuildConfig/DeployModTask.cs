using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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

	public override bool Execute()
	{
		// skip if nothing to do
		// (this must be checked before the manifest validation, to allow cases like unit test projects)
		if (!this.EnableModDeploy && !this.EnableModZip)
			return true;

		var modFiles = this.GetModFiles(this.TargetDir, this.ProjectDir).ToList();
		foreach (var (info, _) in modFiles)
			this.Log.LogWarning(info.Name);

		if (!modFiles.Any(e => e.Info.Name == ManifestFileName))
		{
			this.Log.LogError($"The required `{ManifestFileName}` file is missing.");
			return false;
		}

		if (this.EnableModDeploy)
			this.DeployMod(modFiles, Path.Combine(this.ModDeployModsPath, this.ModName));
		if (this.EnableModZip)
			this.ZipMod(modFiles, this.ModZipPath, this.ModName);

		return true;
	}

	private IEnumerable<(FileInfo Info, string RelativeName)> GetModFiles(string targetDir, string projectDir)
	{
		Uri projectDirUri = new(projectDir);

		FileInfo manifestFile = new(Path.Combine(projectDir, ManifestFileName));
		if (manifestFile.Exists)
			yield return (Info: manifestFile, RelativeName: "nickel.json");

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
	}

	private void DeployMod(IEnumerable<(FileInfo Info, string RelativeName)> modFiles, string destinationDir)
	{
		foreach (var (fileInfo, fileRelativeName) in modFiles)
		{
			var fromPath = fileInfo.FullName;
			var toPath = Path.Combine(destinationDir, fileRelativeName);

			Directory.CreateDirectory(Path.GetDirectoryName(toPath));
			File.Copy(fromPath, toPath, overwrite: true);
		}
	}

	private void ZipMod(IEnumerable<(FileInfo Info, string RelativeName)> modFiles, string destinationFile, string? innerDirName)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
		using Stream zipStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write);
		using ZipArchive archive = new(zipStream, ZipArchiveMode.Create);

		foreach (var (fileInfo, fileRelativeName) in modFiles)
		{
			var fromPath = fileInfo.FullName;
			var zipEntryName = fileRelativeName.Replace(Path.DirectorySeparatorChar, '/');
			if (innerDirName is not null)
				zipEntryName = $"{innerDirName}/{zipEntryName}";

			using var fileStream = new FileStream(fromPath, FileMode.Open, FileAccess.Read);
			using var fileStreamInZip = archive.CreateEntry(zipEntryName).Open();
			fileStream.CopyTo(fileStreamInZip);
		}
	}
}
