using Nanoray.PluginManager;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Xml.Linq;

namespace Nickel;

internal static class DllMap
{
	private static IDirectoryInfo? GameDirectory;

	// Register a call-back for native library resolution.
	public static void Register(IDirectoryInfo gamePath)
	{
		GameDirectory = gamePath;
		AssemblyLoadContext.Default.ResolvingUnmanagedDll += (assembly, name) => MapAndLoad(name, assembly, null);
	}

	// The callback: which loads the mapped libray in place of the original
	private static IntPtr MapAndLoad(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
    {
		if (GameDirectory is null)
		{
			return IntPtr.Zero;
		}

		string? mappedName = MapLibraryName(AppDomain.CurrentDomain.BaseDirectory, libraryName, out mappedName) ? mappedName : libraryName;

		if (mappedName is null)
		{
			return IntPtr.Zero;
		}

		if (NativeLibrary.TryLoad(Path.Combine(GameDirectory.FullName, mappedName), out var handle))
		{
			return handle;
		}

		return nint.Zero;
    }

    // Parse the assembly.xml file, and map the old name to the new name of a library.
    private static bool MapLibraryName(string nickelPath, string originalLibName, out string? mappedLibName)
    {
		if (nickelPath is null)
		{
			mappedLibName = null;
			return false;
		}

		var xmlPath = Path.Combine(nickelPath,
            NickelConstants.Name + "Map" + ".xml");

        mappedLibName = null;

        if (!File.Exists(xmlPath))
		{
            return false;
		}

        var root = XElement.Load(xmlPath);
		var map = root.Elements("dllmap")
			.Where(el => (string)el.Attribute("dll")! == originalLibName)
			.Select(x => x)
			.SingleOrDefault();

        if (map != null)
		{
            mappedLibName = map.Attribute("target")!.Value;
		}

        return mappedLibName != null;
    }
}
