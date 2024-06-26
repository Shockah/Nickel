using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class ModData : IModData
{
	private IModManifest ModManifest { get; }
	private ModDataManager ModDataManager { get; }

	public ModData(IModManifest modManifest, ModDataManager modDataManager)
	{
		this.ModManifest = modManifest;
		this.ModDataManager = modDataManager;
	}

	public T GetModData<T>(object o, string key)
		=> this.ModDataManager.GetModData<T>(this.ModManifest, o, key);

	public bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> this.ModDataManager.TryGetModData(this.ModManifest, o, key, out data);

	public T GetModDataOrDefault<T>(object o, string key, T defaultValue)
		=> this.ModDataManager.GetModDataOrDefault(this.ModManifest, o, key, defaultValue);

	public T GetModDataOrDefault<T>(object o, string key) where T : new()
		=> this.ModDataManager.GetModDataOrDefault<T>(this.ModManifest, o, key);

	public T ObtainModData<T>(object o, string key, Func<T> factory)
		=> this.ModDataManager.ObtainModData(this.ModManifest, o, key, factory);

	public T ObtainModData<T>(object o, string key) where T : new()
		=> this.ModDataManager.ObtainModData<T>(this.ModManifest, o, key);

	public bool ContainsModData(object o, string key)
		=> this.ModDataManager.ContainsModData(this.ModManifest, o, key);

	public void SetModData<T>(object o, string key, T data)
		=> this.ModDataManager.SetModData(this.ModManifest, o, key, data);

	public void RemoveModData(object o, string key)
		=> this.ModDataManager.RemoveModData(this.ModManifest, o, key);
}
