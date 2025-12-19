using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Modulus.Hosts.Tests.Architecture;

public class ViewModelConventionsTests
{
    [Fact]
    public void All_ViewModels_Under_Src_Must_Inherit_ViewModelBase()
    {
        var repoRoot = FindRepoRoot();
        var srcRoot = Path.Combine(repoRoot, "src");
        Assert.True(Directory.Exists(srcRoot), $"Expected 'src' directory at '{srcRoot}'.");

        var viewModelFiles = Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !IsIgnoredBuildOutputPath(p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(viewModelFiles);

        var declarations = new Dictionary<string, List<ClassDecl>>(StringComparer.Ordinal);

        foreach (var file in viewModelFiles)
        {
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            var ns = GetNamespace(root);
            foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var name = cls.Identifier.Text;
                if (!name.EndsWith("ViewModel", StringComparison.Ordinal))
                    continue;

                var key = string.IsNullOrWhiteSpace(ns) ? name : $"{ns}.{name}";

                var baseTypes = cls.BaseList?.Types
                    .Select(t => t.Type.ToString())
                    .ToList() ?? new List<string>();

                if (!declarations.TryGetValue(key, out var list))
                {
                    list = new List<ClassDecl>();
                    declarations.Add(key, list);
                }

                list.Add(new ClassDecl(file, name, baseTypes));
            }
        }

        // If repo has no classes matching "*ViewModel" under ViewModels folders, this convention test is irrelevant.
        Assert.NotEmpty(declarations);

        var offenders = new List<string>();

        foreach (var (typeName, parts) in declarations.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var allBases = parts.SelectMany(p => p.BaseTypes).ToList();
            var hasAnyBaseList = allBases.Count > 0;
            var inheritsViewModelBase = allBases.Any(IsViewModelBaseName);

            if (!hasAnyBaseList || !inheritsViewModelBase)
            {
                var files = string.Join(", ", parts.Select(p => Path.GetRelativePath(repoRoot, p.FilePath)));
                offenders.Add($"{typeName} (files: {files})");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "All ViewModels under src/** MUST inherit ViewModelBase. Offenders:\n" + string.Join("\n", offenders));
    }

    private static bool IsViewModelBaseName(string typeName)
    {
        // Support both 'ViewModelBase' and fully qualified names.
        if (string.Equals(typeName, "ViewModelBase", StringComparison.Ordinal))
            return true;

        if (typeName.EndsWith(".ViewModelBase", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool IsIgnoredBuildOutputPath(string path)
    {
        // Normalize to a single separator for substring checks.
        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        // Ignore generated build outputs inside src (some module projects keep build folders under their tree).
        foreach (var dir in new[] { "obj", "bin", "artifacts" })
        {
            var token = $"{Path.DirectorySeparatorChar}{dir}{Path.DirectorySeparatorChar}";
            if (normalized.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetNamespace(SyntaxNode root)
    {
        // Prefer file-scoped namespace (namespace X;)
        var fileScoped = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
        if (fileScoped != null) return fileScoped.Name.ToString();

        // Fall back to block namespace (namespace X { ... })
        var block = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
        if (block != null) return block.Name.ToString();

        return string.Empty;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Modulus.sln");
            if (File.Exists(sln))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (Modulus.sln not found) from test base directory.");
    }

    private sealed record ClassDecl(string FilePath, string ClassName, IReadOnlyList<string> BaseTypes);
}


