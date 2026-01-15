using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel.ModExtensions;

public static class ModDataExtensions
{
	extension(object o)
	{
		/// <inheritdoc cref="IModData.GetModData"/>
		public T GetModData<T>(string key)
			=> ModExtensions.Helper.ModData.GetModData<T>(o, key);
		
		/// <inheritdoc cref="IModData.TryGetModData"/>
		public bool TryGetModData<T>(string key, [MaybeNullWhen(false)] out T data)
			=> ModExtensions.Helper.ModData.TryGetModData(o, key, out data);
		
		/// <inheritdoc cref="IModData.GetModDataOrDefault{T}(object,string,T)"/>
		public T GetModDataOrDefault<T>(string key, T defaultValue)
			=> ModExtensions.Helper.ModData.GetModDataOrDefault(o, key, defaultValue);
		
		/// <inheritdoc cref="IModData.GetModDataOrDefault{T}(object,string)"/>
		public T GetModDataOrDefault<T>(string key) where T : new()
			=> ModExtensions.Helper.ModData.GetModDataOrDefault<T>(o, key);
		
		/// <inheritdoc cref="IModData.ObtainModData{T}(object,string,Func{T})"/>
		public T ObtainModData<T>(string key, Func<T> factory)
			=> ModExtensions.Helper.ModData.ObtainModData(o, key, factory);
		
		/// <inheritdoc cref="IModData.ObtainModData{T}(object,string)"/>
		public T ObtainModData<T>(string key) where T : new()
			=> ModExtensions.Helper.ModData.ObtainModData<T>(o, key);
		
		/// <inheritdoc cref="IModData.ContainsModData"/>
		public bool ContainsModData(string key)
			=> ModExtensions.Helper.ModData.ContainsModData(o, key);
		
		/// <inheritdoc cref="IModData.SetModData"/>
		public void SetModData<T>(string key, T data)
			=> ModExtensions.Helper.ModData.SetModData(o, key, data);
		
		/// <inheritdoc cref="IModData.RemoveModData"/>
		public void RemoveModData(string key)
			=> ModExtensions.Helper.ModData.RemoveModData(o, key);
	}
}

public static class ModDataClassExtensions
{
	extension(object o)
	{
		/// <inheritdoc cref="IModDataClassExt.GetOptionalModData"/>
		public T? GetOptionalModData<T>(string key) where T : class
			=>  ModExtensions.Helper.ModData.GetOptionalModData<T>(o, key);
		
		/// <inheritdoc cref="IModDataClassExt.SetOptionalModData"/>
		public void SetOptionalModData<T>(string key, T? data) where T : class
			=> ModExtensions.Helper.ModData.SetOptionalModData(o, key, data);
	}
}

public static class ModDataStructExtensions
{
	extension(object o)
	{
		/// <inheritdoc cref="IModDataClassExt.GetOptionalModData"/>
		public T? GetOptionalModData<T>(string key) where T : struct
			=>  ModExtensions.Helper.ModData.GetOptionalModData<T>(o, key);
		
		/// <inheritdoc cref="IModDataClassExt.SetOptionalModData"/>
		public void SetOptionalModData<T>(string key, T? data) where T : struct
			=> ModExtensions.Helper.ModData.SetOptionalModData(o, key, data);
	}
}
