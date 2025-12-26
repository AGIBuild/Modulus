namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Locates buildable project entry points (.sln or .csproj) under a directory.
/// </summary>
internal static class ProjectLocator
{
    internal static (string? Path, string? Type) FindProjectFile(string directory)
    {
        // Prefer solution file
        var slnFiles = Directory.GetFiles(directory, "*.sln");
        if (slnFiles.Length > 0)
        {
            return (slnFiles[0], "solution");
        }

        // Look for .csproj files
        var csprojFiles = Directory.GetFiles(directory, "*.csproj");
        if (csprojFiles.Length > 0)
        {
            // Check if this looks like a module project (has Modulus.Sdk reference)
            foreach (var csproj in csprojFiles)
            {
                var content = File.ReadAllText(csproj);
                if (content.Contains("Modulus.Sdk") || content.Contains("Agibuild.Modulus.Sdk"))
                {
                    return (csproj, "project");
                }
            }

            // Fallback to first csproj
            return (csprojFiles[0], "project");
        }

        // Check subdirectories for projects (multi-project module root)
        foreach (var subDir in Directory.GetDirectories(directory))
        {
            var subCsproj = Directory.GetFiles(subDir, "*.csproj");
            if (subCsproj.Length > 0)
            {
                // Found a subproject. Use it as a build entry point.
                return (subCsproj[0], "project");
            }
        }

        return (null, null);
    }

    internal static string? FindOutputDirectory(string projectDir, string configuration)
    {
        var candidates = new[]
        {
            Path.Combine(projectDir, "bin", configuration, "net10.0"),
            Path.Combine(projectDir, "bin", configuration, "net9.0"),
            Path.Combine(projectDir, "bin", configuration, "net8.0"),
            Path.Combine(projectDir, "bin", configuration),
            Path.Combine(projectDir, "output"),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        // Search for Core.dll in bin folder
        var binDir = Path.Combine(projectDir, "bin");
        if (Directory.Exists(binDir))
        {
            var dllFiles = Directory.GetFiles(binDir, "*.Core.dll", SearchOption.AllDirectories);
            if (dllFiles.Length > 0)
            {
                return Path.GetDirectoryName(dllFiles[0]);
            }
        }

        return null;
    }
}


