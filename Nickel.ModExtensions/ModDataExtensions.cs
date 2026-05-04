using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nickel.ModExtensions;

/// <summary>
/// A wrapper simplifying access to mod data on objects.
/// </summary>
/// <typeparam name="TWrapped">The type of object to get mod data for.</typeparam>
public readonly ref struct ModDataWrapper<TWrapped> where TWrapped : notnull
{
	/// <summary>The object to get mod data for.</summary>
	public ref TWrapped Wrapped
		=> ref this.IsRef ? ref this.RefValue : ref Unsafe.AsRef(in this.Value);

	// ReSharper disable once ReplaceWithFieldKeyword
	private readonly TWrapped Value;
	private readonly ref TWrapped RefValue;
	private readonly bool IsRef;
	
	internal ModDataWrapper(ref TWrapped value)
	{
		this.Value = default!;
		this.RefValue = ref value;
		this.IsRef = true;
	}
	
	internal ModDataWrapper(TWrapped value)
	{
		this.Value = value;
		this.RefValue = ref Unsafe.NullRef<TWrapped>(); // never used
		this.IsRef = false;
	}
	
	/// <inheritdoc cref="IModData.Get"/>
	public T Get<T>(string key)
		=> ModExtensions.Helper.ModData.Get<T>(this.Wrapped, key);
		
	/// <inheritdoc cref="IModData.TryGet"/>
	public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T data)
		=> ModExtensions.Helper.ModData.TryGet(this.Wrapped, key, out data);
		
	/// <inheritdoc cref="IModData.GetOrDefault{T}(object,string,T)"/>
	public T GetOrDefault<T>(string key, T defaultValue)
		=> ModExtensions.Helper.ModData.GetOrDefault(this.Wrapped, key, defaultValue);
		
	/// <inheritdoc cref="IModData.GetOrDefault{T}(object,string)"/>
	public T GetOrDefault<T>(string key) where T : new()
		=> ModExtensions.Helper.ModData.GetOrDefault<T>(this.Wrapped, key);
		
	/// <inheritdoc cref="IModData.Obtain{T}(object,string,Func{T})"/>
	public T Obtain<T>(string key, Func<T> factory)
		=> ModExtensions.Helper.ModData.Obtain(this.Wrapped, key, factory);
		
	/// <inheritdoc cref="IModData.Obtain{T}(object,string)"/>
	public T Obtain<T>(string key) where T : new()
		=> ModExtensions.Helper.ModData.Obtain<T>(this.Wrapped, key);
		
	/// <inheritdoc cref="IModData.Contains"/>
	public bool Contains(string key)
		=> ModExtensions.Helper.ModData.Contains(this.Wrapped, key);
		
	/// <inheritdoc cref="IModData.Set"/>
	public void Set<T>(string key, T data)
		=> ModExtensions.Helper.ModData.Set(this.Wrapped, key, data);
		
	/// <inheritdoc cref="IModData.Remove"/>
	public void Remove(string key)
		=> ModExtensions.Helper.ModData.Remove(this.Wrapped, key);
}

/// <summary>
/// Hosts extensions for arbitrary mod data storage, relating to reference type-based data.
/// </summary>
public static class ModDataClassExtensions
{
	extension<TWrapped>(ModDataWrapper<TWrapped>) where TWrapped : class
	{
		/// <inheritdoc cref="ModDataWrapper{TWrapped}(TWrapped)"/>
		public static ModDataWrapper<TWrapped> Make(TWrapped value)
			=> new(value);
	}
	
	extension<TWrapped>(ModDataWrapper<TWrapped> wrapper) where TWrapped : notnull
	{
		/// <inheritdoc cref="IModDataClassExt.GetOptional"/>
		public T? GetOptional<T>(string key) where T : class
			=> ModExtensions.Helper.ModData.GetOptional<T>(wrapper.Wrapped, key);
		
		/// <inheritdoc cref="IModDataClassExt.SetOptional"/>
		public void SetOptional<T>(string key, T? data) where T : class
			=> ModExtensions.Helper.ModData.SetOptional(wrapper.Wrapped, key, data);
	}
}

/// <summary>
/// Hosts extensions for arbitrary mod data storage, relating to value type-based data.
/// </summary>
public static class ModDataStructExtensions
{
	extension<TWrapped>(ModDataWrapper<TWrapped>) where TWrapped : struct
	{
		/// <inheritdoc cref="ModDataWrapper{TWrapped}(ref TWrapped)"/>
		public static ModDataWrapper<TWrapped> Make(ref TWrapped value)
			=> new(ref value);
	}
	
	extension<TWrapped>(ModDataWrapper<TWrapped> wrapper) where TWrapped : notnull
	{
		/// <inheritdoc cref="IModDataStructExt.GetOptional"/>
		public T? GetOptional<T>(string key) where T : struct
			=> ModExtensions.Helper.ModData.GetOptional<T>(wrapper.Wrapped, key);
		
		/// <inheritdoc cref="IModDataStructExt.SetOptional"/>
		public void SetOptional<T>(string key, T? data) where T : struct
			=> ModExtensions.Helper.ModData.SetOptional(wrapper.Wrapped, key, data);
	}
}

/// <summary>
/// Hosts extensions for arbitrary mod data storage.
/// </summary>
public static class ModDataExtensions
{
	public static ModDataWrapper<Card> ModData(this Card card)
		=> ModDataWrapper<Card>.Make(card);
	
	public static ModDataWrapper<CardAction> ModData(this CardAction action)
		=> ModDataWrapper<CardAction>.Make(action);
	
	public static ModDataWrapper<Route> ModData(this Route route)
		=> ModDataWrapper<Route>.Make(route);
	
	public static ModDataWrapper<StuffBase> ModData(this StuffBase @object)
		=> ModDataWrapper<StuffBase>.Make(@object);
	
	public static ModDataWrapper<Artifact> ModData(this Artifact artifact)
		=> ModDataWrapper<Artifact>.Make(artifact);
	
	public static ModDataWrapper<State> ModData(this State state)
		=> ModDataWrapper<State>.Make(state);
	
	public static ModDataWrapper<Ship> ModData(this Ship ship)
		=> ModDataWrapper<Ship>.Make(ship);
	
	public static ModDataWrapper<Part> ModData(this Part part)
		=> ModDataWrapper<Part>.Make(part);
	
	public static ModDataWrapper<RunConfig> ModData(this RunConfig runConfig)
		=> ModDataWrapper<RunConfig>.Make(runConfig);
	
	public static ModDataWrapper<StoryNode> ModData(this StoryNode node)
		=> ModDataWrapper<StoryNode>.Make(node);
	
	public static ModDataWrapper<MapBase> ModData(this MapBase map)
		=> ModDataWrapper<MapBase>.Make(map);
}
