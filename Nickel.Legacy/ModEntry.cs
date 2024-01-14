using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel.Common;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel.Legacy;

public sealed class ModEntry : Mod
{
	public static string LegacyModType { get; } = $"{typeof(NickelConstants).Namespace!}.Legacy";
	private const string ManifestFileName = "nickel.json";

	private LegacyDatabase Database { get; }
	private ILogger Logger { get; }
	private IModHelper Helper { get; }
	private IModManifest Manifest { get; }

	public ModEntry(
		IPluginPackage<IModManifest> package,
		IModHelper helper,
		ILogger logger,
		ExtendablePluginLoader<IModManifest, Mod> extendablePluginLoader,
		Func<IPluginPackage<IModManifest>, IModHelper> byPackageHelperProvider,
		Func<IModManifest, IModHelper> byManifestHelperProvider,
		Func<IModManifest, ILogger> loggerProvider,
		IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest> loadContextProvider,
		IAssemblyPluginLoaderParameterInjector<IModManifest> assemblyPluginLoaderParameterInjector,
		IAssemblyEditor assemblyEditor
	)
	{
		this.Database = new(byManifestHelperProvider);
		this.Logger = logger;
		this.Helper = helper;
		this.Manifest = package.Manifest;
		helper.Events.OnModLoadPhaseFinished += this.OnModLoadPhaseFinished;
		helper.Events.OnLoadStringsForLocale += this.OnLoadStringsForLocale;

		var legacyAssemblyPluginLoader = new CallbackPluginLoader<IModManifest, Mod>(
			loader: new ConditionalPluginLoader<IModManifest, Mod>(
				loader: new SpecializedConvertingManifestPluginLoader<IAssemblyModManifest, IModManifest, Mod>(
					loader: new AssemblyPluginLoader<IAssemblyModManifest, ILegacyManifest, Mod>(
						requiredPluginDataProvider: p =>
							new AssemblyPluginLoaderRequiredPluginData
							{
								UniqueName = p.Manifest.UniqueName,
								EntryPointAssembly = p.Manifest.EntryPointAssembly,
								EntryPointType = p.Manifest.EntryPointType
							},
						loadContextProvider: loadContextProvider,
						partAssembler: new LegacyAssemblyPluginLoaderPartAssembler(
							helperProvider: byPackageHelperProvider,
							loggerProvider: loggerProvider,
							this.Database
						),
						parameterInjector: assemblyPluginLoaderParameterInjector,
						assemblyEditor: assemblyEditor
					),
					converter: m => m.AsAssemblyModManifest()
				),
				condition: package =>
				{
					if (package.Manifest.ModType != LegacyModType)
						return new No();
					return package.PackageRoot is IFileSystemInfo<FileInfoImpl, DirectoryInfoImpl>
						? new Yes()
						: new Error<string>($"Found a legacy mod, but it is not stored directly on disk (is it in a ZIP file?).");
				}
			),
			callback: (Mod mod) =>
			{
				if (mod is not LegacyModWrapper legacy)
					return;
				this.Database.LegacyManifests.AddRange(legacy.LegacyManifests);
			}
		);

		extendablePluginLoader.RegisterPluginLoader(legacyAssemblyPluginLoader);
	}

	[EventPriority(double.MaxValue)]
	private void OnModLoadPhaseFinished(object? _, ModLoadPhase phase)
	{
		if (phase != ModLoadPhase.AfterDbInit)
			return;
		this.Database.AfterDbInit();
		this.GenerateManifestsForLegacyModsMissingOne(this.Helper.ModRegistry.ModsDirectory, isRoot: true);
	}

	[EventPriority(double.MaxValue)]
	private void OnLoadStringsForLocale(object? _, LoadStringsForLocaleEventArgs e)
		=> this.Database.InjectLocalizations(e.Locale, e.Localizations);

	private void GenerateManifestsForLegacyModsMissingOne(DirectoryInfo directory, bool isRoot = false)
	{
		if (!isRoot)
		{
			var manifestFile = new FileInfo(Path.Combine(directory.FullName, ManifestFileName));
			if (manifestFile.Exists)
				return;
		}

		var definiteModFile = new FileInfo(Path.Combine(directory.FullName, $"{directory.Name}.dll"));
		if (definiteModFile.Exists)
		{
			this.GenerateManifestForLegacyMod(directory, definiteModFile);
			return;
		}

		foreach (var dllFile in directory.EnumerateFiles("*.dll"))
			if (this.GenerateManifestForLegacyMod(directory, dllFile))
				return;

		foreach (var childDirectory in directory.GetDirectories())
			this.GenerateManifestsForLegacyModsMissingOne(childDirectory);
	}

	private bool GenerateManifestForLegacyMod(DirectoryInfo directory, FileInfo assemblyFileName)
	{
		Assembly assembly;
		try
		{
			var temporaryContext = new AssemblyLoadContext($"{typeof(ModEntry).Namespace!}.ManifestGenerator", isCollectible: true);
			assembly = temporaryContext.LoadFromAssemblyPath(assemblyFileName.FullName);
		}
		catch
		{
			// not a managed DLL
			return false;
		}

		List<Type> manifestTypes;
		try
		{
			manifestTypes = assembly.GetTypes()
				.Where(t => t.IsAssignableTo(typeof(ILegacyManifest)))
				.ToList();
		}
		catch
		{
			// probably missing dependencies to load, we can't do anything about it
			// we don't even know if it's a legacy mod at this point, so just dropping out
			return false;
		}

		if (manifestTypes.Count == 0)
			return false;

		try
		{
			this.Logger.LogInformation("Generating a `{ManifestFileName}` file for a legacy mod that is missing one at `{Directory}`...", ManifestFileName, directory.FullName);

			var manifests = manifestTypes
				.Select(t => (ILegacyManifest)Activator.CreateInstance(t)!)
				.ToList();
			var name = manifests.Select(m => m.Name).MinBy(m => m.Length) ?? assembly.GetName().Name ?? assembly.GetName().FullName;

			var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() ?? throw new InvalidOperationException();
			var version = SemanticVersionParser.TryParse(attribute.InformationalVersion.Split("+")[0], out var parsedVersion)
				? parsedVersion : new SemanticVersion(1, 0, 0);

			var dependencies = manifests.SelectMany(m => m.Dependencies).ToList();
			var requiredDependencies = dependencies
				.Where(d => !d.IgnoreIfMissing)
				.DistinctBy(d => d.DependencyName);
			var optionalDependencies = dependencies
				.Where(d => d.IgnoreIfMissing)
				.Where(d => !requiredDependencies.Any(required => required.DependencyName == d.DependencyName))
				.DistinctBy(d => d.DependencyName);

			var manifest = new GeneratedLegacyModManifest
			{
				EntryPointAssembly = assemblyFileName.Name,
				UniqueName = name,
				RequiredApiVersion = this.Manifest.RequiredApiVersion,
				Version = version,
				Dependencies = new List<ModDependency>
				{
					new(this.Manifest.UniqueName, this.Manifest.Version, isRequired: true)
				}
					.Concat(requiredDependencies.Select(d => new ModDependency(d.DependencyName, isRequired: true)))
					.Concat(optionalDependencies.Select(d => new ModDependency(d.DependencyName, isRequired: false)))
					.ToHashSet()
			};

			var serializer = JsonSerializer.Create(new()
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
			});
			var manifestFile = new FileInfo(Path.Combine(directory.FullName, ManifestFileName));
			using var stream = manifestFile.OpenWrite();
			using var streamWriter = new StreamWriter(stream);
			serializer.Serialize(streamWriter, manifest);

			this.Logger.LogWarning("Successfully generated a `{ManifestFileName}` file for a legacy mod at `{Directory}`. The mod will be loaded the next time you start {ModLoaderName}.", ManifestFileName, directory.FullName, NickelConstants.Name);
			return true;
		}
		catch (Exception ex)
		{
			this.Logger.LogError("Could not generate a `{ManifestFileName}` file for legacy mod file `{LegacyModFileName}`: {Exception}", ManifestFileName, assemblyFileName.Name, ex);
			// we failed, but this was definitely a legacy mod file, so we've "handled it" - don't recurse any deeper
			return true;
		}
	}
}
