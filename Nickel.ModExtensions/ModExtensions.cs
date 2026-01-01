using System;

namespace Nickel.ModExtensions;

public static class ModExtensions
{
	private static IModHelper? HelperStorage;
	
	internal static IModHelper Helper
		=> HelperStorage ?? throw new NullReferenceException("Mod extensions cannot be used before calling `helper.PrepareModExtensions()`");
	
	extension(IModHelper helper)
	{
		public void PrepareModExtensions()
			=> HelperStorage = helper;
	}
}
