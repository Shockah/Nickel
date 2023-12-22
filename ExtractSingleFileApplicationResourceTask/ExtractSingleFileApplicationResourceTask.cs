using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SingleFileExtractor.Core;

namespace Nickel.ExtractSingleFileApplicationResourceTask;

public class ExtractSingleFileApplicationResourceTask : Task
{
    [Required]
    public string ExeInputPath { get; set; } = null!;

    [Required]
    public string ResourceName { get; set; } = null!;

    [Required]
    public string ResourceOutputPath { get; set; } = null!;

    static ExtractSingleFileApplicationResourceTask()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => Assembly.LoadFile(Path.Combine(typeof(ExtractSingleFileApplicationResourceTask).Assembly.Location, "..", $"{e.Name}.dll"));
    }

    public override bool Execute()
    {
        if (!File.Exists(ExeInputPath))
        {
            Log.LogError($"The provided path `{ExeInputPath}` does not exist.");
            return false;
        }

        ExecutableReader reader = new(ExeInputPath);
        if (!reader.IsSingleFile)
        {
            Log.LogError($"The file at the provided path `{ExeInputPath}` is not a single file executable.");
            return false;
        }
        if (!reader.IsSupported)
        {
            Log.LogError($"The file at the provided path `{ExeInputPath}` is not supported.");
            return false;
        }

        FileEntry? FindFileEntry()
        {
            foreach (var file in reader.Bundle.Files)
                if (file.RelativePath == ResourceName)
                    return file;
            return null;
        }

        if (FindFileEntry() is not { } entry)
        {
            Log.LogError($"The single file executable `{ExeInputPath}` does not contain a resource `{ResourceName}`.");
            return false;
        }

        MemoryStream stream = new();
        entry.AsStream().CopyTo(stream);
        byte[] resourceBytes = stream.ToArray();

        if (File.Exists(ResourceOutputPath))
        {
            using var md5 = MD5.Create();
            byte[] resourceHash = md5.ComputeHash(resourceBytes);
            byte[] existingHash = md5.ComputeHash(File.ReadAllBytes(ResourceOutputPath));
            if (resourceHash.SequenceEqual(existingHash))
                return true;
        }
        else
        {
            Directory.CreateDirectory(Directory.GetParent(ResourceOutputPath).FullName);
        }

        File.WriteAllBytes(ResourceOutputPath, resourceBytes);
        return true;
    }
}
