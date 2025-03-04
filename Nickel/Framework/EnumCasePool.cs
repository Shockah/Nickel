using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class EnumCasePool
{
	private sealed class Wrapper<TRaw> where TRaw : struct, INumber<TRaw>
	{
		private readonly Dictionary<Type, HashSet<TRaw>> ObtainedCases = [];
		private readonly Dictionary<Type, HashSet<TRaw>> FreedCases = [];
		private readonly Dictionary<Type, TRaw> NextCaseValue = [];

		private static TRaw GetInitialNextCase(TRaw maxDefinedCase)
		{
			var result = maxDefinedCase;
			result += result; // x 2
			result += result; // x 2 (for a total of x 4)
			result += result; // x 2 (for a total of x 8)
			return result + TRaw.One;
		}

		public TEnum ObtainEnumCase<TEnum>() where TEnum : struct, Enum
		{
			if (this.FreedCases.TryGetValue(typeof(TEnum), out var freedCases) && freedCases.Count != 0)
			{
				var @case = freedCases.First();
				freedCases.Remove(@case);
				return (TEnum)(object)@case;
			}

			if (!this.NextCaseValue.TryGetValue(typeof(TEnum), out var nextCaseValue))
			{
				var maxDefinedCase = (TRaw)(object)Enum.GetValues<TEnum>().Max();
				nextCaseValue = GetInitialNextCase(maxDefinedCase);
			}

			ref var obtainedCases = ref CollectionsMarshal.GetValueRefOrAddDefault(this.ObtainedCases, typeof(TEnum), out var obtainedCasesExists);
			if (!obtainedCasesExists)
				obtainedCases = [];
			obtainedCases!.Add(nextCaseValue);
			
			this.NextCaseValue[typeof(TEnum)] = nextCaseValue + TRaw.One;
			return (TEnum)(object)nextCaseValue;
		}

		public void FreeEnumCase<TEnum>(TEnum @case) where TEnum : struct, Enum
		{
			if (!this.ObtainedCases.TryGetValue(typeof(TEnum), out var obtainedCases))
				return;

			var raw = (TRaw)(object)@case;
			if (!obtainedCases.Remove(raw))
				return;

			ref var freedCases = ref CollectionsMarshal.GetValueRefOrAddDefault(this.FreedCases, typeof(TEnum), out var freedCasesExists);
			if (!freedCasesExists)
				freedCases = [];
			freedCases!.Add(raw);
		}
	}

	private readonly Wrapper<int> Ints = new();
	private readonly Wrapper<uint> Uints = new();
	private readonly Wrapper<long> Longs = new();
	private readonly Wrapper<ulong> Ulongs = new();
	private readonly Wrapper<short> Shorts = new();
	private readonly Wrapper<ushort> Ushorts = new();
	private readonly Wrapper<byte> Bytes = new();
	private readonly Wrapper<sbyte> Sbytes = new();

	public T ObtainEnumCase<T>() where T : struct, Enum
	{
		var type = Enum.GetUnderlyingType(typeof(T));
		if (type == typeof(int))
			return this.Ints.ObtainEnumCase<T>();
		if (type == typeof(uint))
			return this.Uints.ObtainEnumCase<T>();
		if (type == typeof(long))
			return this.Longs.ObtainEnumCase<T>();
		if (type == typeof(ulong))
			return this.Ulongs.ObtainEnumCase<T>();
		if (type == typeof(short))
			return this.Shorts.ObtainEnumCase<T>();
		if (type == typeof(ushort))
			return this.Ushorts.ObtainEnumCase<T>();
		if (type == typeof(byte))
			return this.Bytes.ObtainEnumCase<T>();
		if (type == typeof(sbyte))
			return this.Sbytes.ObtainEnumCase<T>();
		throw new ArgumentException($"Unsupported underlying enum type {type}");
	}

	public void FreeEnumCase<T>(T @case) where T : struct, Enum
	{
		var type = Enum.GetUnderlyingType(typeof(T));
		if (type == typeof(int))
			this.Ints.FreeEnumCase(@case);
		else if (type == typeof(uint))
			this.Uints.FreeEnumCase(@case);
		else if (type == typeof(long))
			this.Longs.FreeEnumCase(@case);
		else if (type == typeof(ulong))
			this.Ulongs.FreeEnumCase(@case);
		else if (type == typeof(short))
			this.Shorts.FreeEnumCase(@case);
		else if (type == typeof(ushort))
			this.Ushorts.FreeEnumCase(@case);
		else if (type == typeof(byte))
			this.Bytes.FreeEnumCase(@case);
		else if (type == typeof(sbyte))
			this.Sbytes.FreeEnumCase(@case);
		else
			throw new ArgumentException($"Unsupported underlying enum type {type}");
	}
}
