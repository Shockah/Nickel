using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILegacyManifest = CobaltCoreModding.Definitions.ModManifests.IManifest;

namespace Nickel;

internal sealed class LegacyRegistry
	: IModLoaderContact, IPrelaunchContactPoint,
	ISpriteRegistry, IGlossaryRegisty,
	IDeckRegistry, IStatusRegistry, ICardRegistry, IArtifactRegistry,
	IAnimationRegistry, ICharacterRegistry,
	IPartTypeRegistry, IShipPartRegistry, IShipRegistry, IRawShipRegistry, IStartershipRegistry,
	IStoryRegistry
{
	public Assembly CobaltCoreAssembly
		=> typeof(DB).Assembly;

	public IEnumerable<ILegacyManifest> LoadedManifests
		=> this.Database.LegacyManifests;

	internal LegacyEventHub GlobalEventHub { get; } = new();

	private IModManifest ModManifest { get; }
	private IModHelper Helper { get; }
	private ILogger Logger { get; }

	public Func<object> GetCobaltCoreGraphicsDeviceFunc
		=> () => MG.inst.GraphicsDevice;

	private LegacyDatabase Database { get; }

	public LegacyRegistry(
		IModManifest modManifest,
		IModHelper helper,
		ILogger logger,
		LegacyDatabase database
	)
	{
		this.ModManifest = modManifest;
		this.Helper = helper;
		this.Logger = logger;
		this.Database = database;
	}

	public bool RegisterNewAssembly(Assembly assembly, DirectoryInfo working_directory)
		=> throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");

	public TApi? GetApi<TApi>(string modName) where TApi : class
		=> this.Helper.ModRegistry.GetApi<TApi>(modName);

	public ILegacyManifest LookupManifest(string globalName)
		=> this.LoadedManifests.FirstOrDefault(m => m.Name == globalName) ?? throw new KeyNotFoundException();

	public ExternalSprite LookupSprite(string globalName)
		=> this.Database.GetSprite(globalName);

	public bool RegisterArt(ExternalSprite sprite_data, int? overwrite_value = null)
	{
		if (overwrite_value is not null)
			throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
		this.Database.RegisterSprite(this.ModManifest, sprite_data);
		return true;
	}

	public ExternalGlossary LookupGlossary(string globalName)
		=> this.Database.GetGlossary(globalName);

	public bool RegisterGlossary(ExternalGlossary glossary)
	{
		this.Database.RegisterGlossary(this.ModManifest, glossary);
		return true;
	}

	public ExternalDeck LookupDeck(string globalName)
		=> this.Database.GetDeck(globalName);

	public bool RegisterDeck(ExternalDeck deck, int? overwrite = null)
	{
		if (overwrite is not null)
			throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
		this.Database.RegisterDeck(this.ModManifest, deck);
		return true;
	}

	public ExternalCard LookupCard(string globalName)
		=> this.Database.GetCard(globalName);

	public bool RegisterCard(ExternalCard card, string? overwrite = null)
	{
		if (!string.IsNullOrEmpty(overwrite))
			throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
		this.Database.RegisterCard(this.ModManifest, card);
		return true;
	}

	public ExternalArtifact LookupArtifact(string globalName)
		=> this.Database.GetArtifact(globalName);

	public bool RegisterArtifact(ExternalArtifact artifact, string? overwrite = null)
	{
		if (!string.IsNullOrEmpty(overwrite))
			throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
		this.Database.RegisterArtifact(this.ModManifest, artifact);
		return true;
	}

	public ExternalStatus LookupStatus(string globalName)
		=> this.Database.GetStatus(globalName);

	public bool RegisterStatus(ExternalStatus status)
	{
		this.Database.RegisterStatus(this.ModManifest, status);
		return true;
	}

	public ExternalAnimation LookupAnimation(string globalName)
		=> this.Database.GetAnimation(globalName);

	public bool RegisterAnimation(ExternalAnimation animation)
	{
		this.Database.RegisterAnimation(this.ModManifest, animation);
		return true;
	}

	public ExternalCharacter LookupCharacter(string globalName)
		=> this.Database.GetCharacter(globalName);

	public bool RegisterCharacter(ExternalCharacter character)
	{
		this.Database.RegisterCharacter(this.ModManifest, character);
		return true;
	}

	public ExternalPartType LookupPartType(string globalName)
		=> this.Database.GetPartType(globalName);

	public bool RegisterPartType(ExternalPartType externalPartType)
	{
		this.Database.RegisterPartType(this.ModManifest, externalPartType);
		return true;
	}

	public ExternalPart LookupPart(string globalName)
		=> this.Database.GetPart(globalName);

	public bool RegisterPart(ExternalPart externalPart)
	{
		this.Database.RegisterPart(this.ModManifest, externalPart);
		return true;
	}

	public bool RegisterRawPart(string global_name, int spr_value, int? off_spr_value = null)
	{
		this.Database.RegisterRawPart(this.ModManifest, global_name, spr_value, off_spr_value);
		return true;
	}

	public object LookupShip(string globalName)
		=> this.Database.ActualizeShip(globalName);

	public bool RegisterShip(ExternalShip ship)
	{
		this.Database.RegisterShip(ship);
		return true;
	}

	public bool RegisterShip(object shipObject, string global_name)
	{
		if (shipObject is not Ship ship)
		{
			this.Logger.LogError("Tried to register a new Ship, but the given object {Object} is not a `{ShipTypeName}`.", shipObject, typeof(Ship).FullName);
			return false;
		}

		this.Database.RegisterShip(ship, global_name);
		return true;
	}

	public object LookupStarterShip(string globalName)
		=> this.Database.GetStarterShip(globalName);

	public bool RegisterStartership(ExternalStarterShip starterShip)
	{
		this.Database.RegisterStarterShip(this.ModManifest, starterShip);
		return true;
	}

	public bool RegisterStory(ExternalStory story)
	{
		this.Database.RegisterStory(story);
		return true;
	}

	public bool RegisterChoice(string key, MethodInfo choice, bool intendedOverride = false)
	{
		this.Database.RegisterChoiceOrCommand(key, choice, intendedOverride, true);
		return true;
	}

	public bool RegisterCommand(string key, MethodInfo command, bool intendedOverride = false)
	{
		this.Database.RegisterChoiceOrCommand(key, command, intendedOverride, false);
		return true;
	}

	public bool RegisterInjector(ExternalStoryInjector injector)
		=> throw new NotImplementedException($"This method is not supported in {NickelConstants.Name}");
}
