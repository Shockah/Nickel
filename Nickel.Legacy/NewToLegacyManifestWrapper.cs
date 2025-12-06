using CobaltCoreModding.Definitions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel.Legacy;

using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

public sealed class NewToLegacyManifestStub : ILegacyManifest
{
	public string Name
		=> this.ModManifest.UniqueName;

	public IEnumerable<DependencyEntry> Dependencies
		=> [];

	public DirectoryInfo? GameRootFolder
	{
		get => null;
		set { }
	}

	public DirectoryInfo? ModRootFolder
	{
		get => null;
		set { }
	}

	public ILogger? Logger
	{
		get => this.LoggerProvider();
		set { }
	}

	private readonly IModManifest ModManifest;
	private readonly Func<ILogger> LoggerProvider;

	public NewToLegacyManifestStub(IModManifest modManifest, Func<ILogger> loggerProvider)
	{
		this.ModManifest = modManifest;
		this.LoggerProvider = loggerProvider;
	}
}
