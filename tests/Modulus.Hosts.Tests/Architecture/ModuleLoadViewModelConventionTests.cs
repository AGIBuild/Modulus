using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Xunit;

namespace Modulus.Hosts.Tests.Architecture;

public class ModuleLoadViewModelConventionTests
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), "modulus-vm-convention-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ModuleLoader_LoadAsync_Throws_When_ModuleContains_ViewModelNotInheriting_ViewModelBase()
    {
        Directory.CreateDirectory(_testRoot);

        var (moduleDir, _) = CreateBadViewModelModulePackage("badvm", ModulusHostIds.Avalonia);

        using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

        var runtimeContext = new RuntimeContext();
        runtimeContext.SetCurrentHost(ModulusHostIds.Avalonia);

        var sharedCatalog = SharedAssemblyCatalog.FromAssemblies(
            AppDomain.CurrentDomain.GetAssemblies(),
            logger: loggerFactory.CreateLogger<SharedAssemblyCatalog>());

        var manifestValidator = new DefaultManifestValidator(
            loggerFactory.CreateLogger<DefaultManifestValidator>(),
            runtimeContext);

        var executionGuard = new ModuleExecutionGuard(
            loggerFactory.CreateLogger<ModuleExecutionGuard>(),
            runtimeContext);

        var loader = new ModuleLoader(
            runtimeContext,
            manifestValidator,
            sharedCatalog,
            executionGuard,
            loggerFactory.CreateLogger<ModuleLoader>(),
            loggerFactory,
            hostServices: null,
            resolutionReporter: null);

        await Assert.ThrowsAsync<ViewModelConventionViolationException>(async () =>
        {
            await loader.LoadAsync(moduleDir, isSystem: true, skipModuleInitialization: true);
        });
    }

    private (string ModuleDir, string ModuleId) CreateBadViewModelModulePackage(string dirName, string targetHost)
    {
        var moduleDir = Path.Combine(_testRoot, dirName);
        Directory.CreateDirectory(moduleDir);

        var moduleId = $"{dirName}-{Guid.NewGuid():N}";
        var version = "1.0.0";
        var uiDllName = $"{dirName}.UI.dll";
        var uiDllPath = Path.Combine(moduleDir, uiDllName);

        CompileBadUiAssembly(uiDllPath);
        WriteManifest(moduleDir, moduleId, version, uiDllName, targetHost);

        Assert.True(File.Exists(Path.Combine(moduleDir, "extension.vsixmanifest")));
        Assert.True(File.Exists(uiDllPath));

        return (moduleDir, moduleId);
    }

    private static void CompileBadUiAssembly(string outputPath)
    {
        // Contains a class ending with ViewModel that does NOT inherit ViewModelBase => must fail at module load.
        var code = """
                   using Modulus.Sdk;

                   namespace Modulus.IntegrationTests.BadVm;

                   public sealed class BadViewModel { }

                   public sealed class TestModule : ModulusPackage
                   {
                       public override void ConfigureServices(IModuleLifecycleContext context) { }
                   }
                   """;

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ModulusPackage).Assembly.Location),
        };

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(runtimeDir))
        {
            foreach (var name in new[] { "System.Runtime.dll", "netstandard.dll" })
            {
                var p = Path.Combine(runtimeDir, name);
                if (File.Exists(p))
                    references.Add(MetadataReference.CreateFromFile(p));
            }
        }

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(outputPath),
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        EmitResult emit = compilation.Emit(outputPath);
        if (!emit.Success)
        {
            var diag = string.Join(Environment.NewLine, emit.Diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Failed to compile test module UI assembly:{Environment.NewLine}{diag}");
        }
    }

    private static void WriteManifest(string moduleDir, string moduleId, string version, string uiDllName, string targetHost)
    {
        XNamespace ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        var doc = new XDocument(
            new XElement(ns + "PackageManifest",
                new XAttribute("Version", "2.0.0"),
                new XElement(ns + "Metadata",
                    new XElement(ns + "Identity",
                        new XAttribute("Id", moduleId),
                        new XAttribute("Version", version),
                        new XAttribute("Language", "en-US"),
                        new XAttribute("Publisher", "Modulus.Tests")),
                    new XElement(ns + "DisplayName", moduleId),
                    new XElement(ns + "Description", "Test module")),
                new XElement(ns + "Installation",
                    new XElement(ns + "InstallationTarget",
                        new XAttribute("Id", targetHost),
                        new XAttribute("Version", "[0.0,]"))),
                new XElement(ns + "Assets",
                    new XElement(ns + "Asset",
                        // ModuleLoader loads both Package and Assembly, but the manifest validator REQUIRES a Package asset.
                        new XAttribute("Type", ModulusAssetTypes.Package),
                        new XAttribute("Path", uiDllName),
                        new XAttribute("TargetHost", targetHost)))));

        doc.Save(Path.Combine(moduleDir, "extension.vsixmanifest"));
    }
}


