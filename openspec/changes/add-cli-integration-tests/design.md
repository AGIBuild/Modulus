## Context

Modulus CLI 提供 6 个命令用于模块开发和管理。当前没有自动化测试来验证这些命令的端到端工作流程。需要建立集成测试框架，确保发布前能验证 CLI 功能正常。

**约束**:
- 测试必须隔离，不影响真实用户数据
- 测试数据库 schema 必须与 Host 应用一致
- 测试必须可在 CI/CD 环境中运行
- 测试必须覆盖完整生命周期

## Goals / Non-Goals

**Goals**:
- 验证每个 CLI 命令的基本功能
- 验证完整的模块开发工作流 (new → build → pack → install → list → uninstall)
- 验证生成的模块能被 `ModuleLoader` 正确加载
- 提供清晰的测试失败诊断信息
- 支持在 CI 中自动运行

**Non-Goals**:
- GUI 测试
- 性能测试
- 压力测试
- 完整 Host 应用集成测试（使用 `ModuleLoader` 直接验证即可）

## Decisions

### 1. 测试隔离策略

**Decision**: 使用 `CliTestContext` 类管理测试环境隔离。

每个测试使用独立的：
- 临时工作目录
- 测试数据库（通过 `CliServiceProvider` 参数传入）
- 模块安装目录

```csharp
public class CliTestContext : IDisposable
{
    public string WorkingDirectory { get; }
    public string DatabasePath { get; }
    public string ModulesDirectory { get; }
    private ServiceProvider? _serviceProvider;
    
    public CliTestContext()
    {
        var testId = Guid.NewGuid().ToString("N")[..8];
        WorkingDirectory = Path.Combine(Path.GetTempPath(), $"modulus-cli-test-{testId}");
        DatabasePath = Path.Combine(WorkingDirectory, "test.db");
        ModulesDirectory = Path.Combine(WorkingDirectory, "Modules");
        Directory.CreateDirectory(WorkingDirectory);
        Directory.CreateDirectory(ModulesDirectory);
    }
    
    public async Task<ServiceProvider> GetServiceProviderAsync()
    {
        if (_serviceProvider == null)
        {
            // 使用参数化的 CliServiceProvider
            _serviceProvider = CliServiceProvider.Build(
                verbose: false, 
                databasePath: DatabasePath,
                modulesDirectory: ModulesDirectory);
            
            // 运行 migrations 确保 schema 与 Host 一致
            await CliServiceProvider.EnsureMigratedAsync(_serviceProvider);
        }
        return _serviceProvider;
    }
    
    public void Dispose()
    {
        _serviceProvider?.Dispose();
        if (Directory.Exists(WorkingDirectory))
            Directory.Delete(WorkingDirectory, recursive: true);
    }
}
```

**Alternatives considered**:
- 使用环境变量 - 不够优雅，可能影响其他测试
- 使用 xUnit 的 `IAsyncLifetime` - 名称范围太广，不够具体
- 使用共享测试夹具 - 可能导致测试间干扰

### 2. CLI 执行方式

**Decision**: 对于需要数据库的命令，直接调用 CLI 内部逻辑；对于不需要数据库的命令，通过 `Process` 执行。

```csharp
public class CliRunner
{
    private readonly CliTestContext _context;
    
    // 对于 new/build/pack 等不需要数据库的命令，使用进程执行
    public async Task<CliResult> RunProcessAsync(string arguments, string? workingDir = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = arguments,
            WorkingDirectory = workingDir ?? _context.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // 执行并返回结果
    }
    
    // 对于 install/uninstall/list 等需要数据库的命令，直接调用内部逻辑
    public async Task<CliResult> InstallAsync(string source, bool force = false)
    {
        var provider = await _context.GetServiceProviderAsync();
        var installerService = provider.GetRequiredService<IModuleInstallerService>();
        // 直接调用安装逻辑
    }
}
```

**Alternatives considered**:
- 全部使用进程执行 + 环境变量 - 不够优雅，环境变量可能泄漏
- 全部使用内部调用 - 无法测试真实的 CLI 解析和进程启动

### 3. 数据库 Schema 一致性

**Decision**: 使用同一个 `ModulusDbContext` 和 EF Core Migrations。

Host 应用和 CLI 共用 `Modulus.Infrastructure.Data` 项目中的：
- `ModulusDbContext` - DbContext 定义
- `Migrations/` - 数据库迁移脚本

测试初始化时调用 `EnsureMigratedAsync()` 运行 migrations：

```csharp
// 测试初始化
var provider = CliServiceProvider.Build(databasePath: testDbPath);
await CliServiceProvider.EnsureMigratedAsync(provider);

// 此时测试数据库 schema 与 Host 完全一致
```

**关键保证**:
- Schema 来源唯一（`ModulusDbContext`）
- 使用相同的 Migration 脚本
- 无需手动同步或维护测试 schema

### 4. 模块加载验证

**Decision**: 使用 `ModuleLoader` 直接验证生成的模块能被加载，不需要启动 Host 应用。

```csharp
// 验证模块可加载（不需要数据库）
var runtimeContext = new RuntimeContext();
runtimeContext.SetCurrentHost(ModulusHostIds.Avalonia);

var loader = new ModuleLoader(
    runtimeContext,
    new DefaultManifestValidator(NullLogger<DefaultManifestValidator>.Instance),
    SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies()),
    new ModuleExecutionGuard(NullLogger<ModuleExecutionGuard>.Instance, runtimeContext),
    NullLogger<ModuleLoader>.Instance,
    NullLoggerFactory.Instance);

var descriptor = await loader.LoadAsync(modulePath);
Assert.NotNull(descriptor);
```

**优点**:
- 不需要启动 Host 进程
- 不需要数据库
- 测试执行快速
- 已有成熟模式（`Modulus.Modules.Tests`）

### 5. 测试组织结构

**Decision**: 按命令分文件，加一个端到端测试文件。

```
tests/Modulus.Cli.IntegrationTests/
├── Modulus.Cli.IntegrationTests.csproj
├── Infrastructure/
│   ├── CliTestContext.cs       # 测试环境隔离
│   ├── CliRunner.cs            # CLI 执行器
│   └── CliResult.cs            # 执行结果模型
├── Commands/
│   ├── NewCommandTests.cs      # modulus new
│   ├── BuildCommandTests.cs    # modulus build
│   ├── PackCommandTests.cs     # modulus pack
│   ├── InstallCommandTests.cs  # modulus install
│   ├── UninstallCommandTests.cs # modulus uninstall
│   └── ListCommandTests.cs     # modulus list
└── EndToEnd/
    ├── FullLifecycleTests.cs   # 完整生命周期测试
    └── ModuleLoadTests.cs      # 模块加载验证测试
```

### 6. 发布前测试门禁

**Decision**: CLI 发布和打包命令必须依赖测试通过。

```
nuke publish-cli
    └── DependsOn: TestCli
                    └── DependsOn: Compile

nuke pack-cli
    └── DependsOn: TestCli
                    └── DependsOn: Compile
```

**实现**:

```csharp
Target TestCli => _ => _
    .DependsOn(Compile)
    .Description("Run CLI integration tests")
    .Executes(() =>
    {
        DotNetTest(s => s
            .SetProjectFile(TestsDirectory / "Modulus.Cli.IntegrationTests")
            .SetConfiguration(Configuration));
    });

Target PackCli => _ => _
    .DependsOn(TestCli)  // 测试必须通过
    .Description("Pack CLI as a dotnet tool NuGet package")
    .Executes(() => { ... });

Target PublishCli => _ => _
    .DependsOn(PackCli)  // PackCli 已依赖 TestCli
    .Description("Publish CLI to NuGet.org")
    .Executes(() => { ... });
```

**效果**:
- 发布前自动运行 CLI 集成测试
- 测试失败时发布中止
- 确保发布的 CLI 工具功能正常

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 测试临时文件未清理 | 使用 `IDisposable` 模式，确保 `finally` 清理 |
| CI 环境差异 | 使用相对路径，避免硬编码 |
| 测试执行时间过长 | 使用 `--no-build` 选项跳过重复编译 |
| 测试阻塞紧急发布 | 可临时使用 `--skip TestCli` 绕过（需谨慎） |

## Open Questions

- 是否需要支持并行测试执行？（当前设计支持，因为每个测试使用独立目录）

