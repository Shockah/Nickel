using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModShips(
	IModManifest modManifest,
	Func<ShipManager> shipManagerProvider,
	Func<PartManager> partManagerProvider
) : IModShips
{
	public IReadOnlyDictionary<string, IShipEntry> RegisteredShips
		=> this.RegisteredShipStorage;
	
	public IReadOnlyDictionary<string, IPartTypeEntry> RegisteredPartTypes
		=> this.RegisteredPartTypeStorage;
	
	public IReadOnlyDictionary<string, IPartEntry> RegisteredParts
		=> this.RegisteredPartStorage;
	
	private readonly Dictionary<string, IShipEntry> RegisteredShipStorage = [];
	private readonly Dictionary<string, IPartTypeEntry> RegisteredPartTypeStorage = [];
	private readonly Dictionary<string, IPartEntry> RegisteredPartStorage = [];
	
	public IShipEntry? LookupByUniqueName(string uniqueName)
		=> shipManagerProvider().LookupByUniqueName(uniqueName);

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
	{
		var entry = shipManagerProvider().RegisterShip(modManifest, name, configuration);
		this.RegisteredShipStorage[name] = entry;
		return entry;
	}

	public IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration)
	{
		var entry = partManagerProvider().RegisterPartType(modManifest, name, configuration);
		this.RegisteredPartTypeStorage[name] = entry;
		return entry;
	}

	public IPartEntry RegisterPart(string name, PartConfiguration configuration)
	{
		var entry = partManagerProvider().RegisterPart(modManifest, name, configuration);
		this.RegisteredPartStorage[name] = entry;
		return entry;
	}
}

internal sealed class VanillaModShips(
	IModManifest modManifest,
	Func<ShipManager> shipManagerProvider
) : IModShips
{
	private readonly Lazy<Dictionary<string, IShipEntry>> LazyRegisteredShips = new(
		() => StarterShip.ships
			.Select(d => shipManagerProvider().LookupByUniqueName(d.Key))
			.Where(e => e?.ModOwner == modManifest)
			.ToDictionary(e => e!.UniqueName, e => e!)
	);
	
	public IReadOnlyDictionary<string, IShipEntry> RegisteredShips
		=> this.LazyRegisteredShips.Value;
	
	// TODO: maybe implement one day
	public IReadOnlyDictionary<string, IPartTypeEntry> RegisteredPartTypes
		=> throw new NotImplementedException();
	
	// TODO: maybe implement one day
	public IReadOnlyDictionary<string, IPartEntry> RegisteredParts
		=> throw new NotImplementedException();
	
	public IShipEntry? LookupByUniqueName(string uniqueName)
		=> shipManagerProvider().LookupByUniqueName(uniqueName);

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
		=> throw new NotSupportedException();

	public IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration)
		=> throw new NotSupportedException();

	public IPartEntry RegisterPart(string name, PartConfiguration configuration)
		=> throw new NotSupportedException();
}
