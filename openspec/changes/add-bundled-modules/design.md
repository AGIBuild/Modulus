## Context

Modulus 框架当前启动流程：
1. 扫描模块目录（`{AppBaseDir}/Modules/`、用户目录）
2. 读取每个模块的 `extension.vsixmanifest`
3. 逐个与数据库比对，不存在则安装
4. 从数据库加载已启用模块

**问题**：每次启动都执行目录扫描和 manifest 解析，即使模块没有变化。

**目标**：模块加载的唯一来源是数据库，内置模块通过 EF 迁移预置。

## Goals / Non-Goals

**Goals**:
- 内置模块数据通过 EF 迁移管理
- 提供 CLI 工具自动生成模块数据迁移
- 简化运行时启动流程
- 合并现有迁移，提供干净起点

**Non-Goals**:
- 用户安装模块仍使用现有流程（`InstallFromPathAsync`）
- 不修改模块打包格式（`.modpkg`）
- 开发模式（DEBUG）可保留目录扫描便于调试

## Decisions

### 1. 合并现有迁移

**Decision**: 将 8 个现有迁移合并为 `InitialCreate`，作为干净起点。

**步骤**：
1. 删除所有现有迁移文件
2. 删除本地数据库
3. 重新生成 `InitialCreate` 迁移（包含当前完整 schema）
4. 迁移中可包含 Host 内置模块（`HostModules`）的种子数据

### 2. EF 迁移管理模块数据

**Decision**: 使用 EF Core 迁移框架，但通过**强类型接口和扩展方法**避免硬编码。

#### 2.1 数据模型接口

```csharp
// Modulus.Infrastructure.Data/Seeding/IModuleSeedData.cs
public interface IModuleSeedData
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string? Description { get; }
    string? Author { get; }
    string Path { get; }
    bool IsSystem { get; }
    bool IsEnabled { get; }
    MenuLocation MenuLocation { get; }
    ModuleState State { get; }
}

public interface IMenuSeedData
{
    string Id { get; }
    string ModuleId { get; }
    string DisplayName { get; }
    string Icon { get; }
    string Route { get; }
    MenuLocation Location { get; }
    int Order { get; }
}
```

#### 2.2 迁移辅助扩展方法

```csharp
// Modulus.Infrastructure.Data/Seeding/MigrationBuilderExtensions.cs
public static class MigrationBuilderExtensions
{
    public static void InsertModule(this MigrationBuilder builder, IModuleSeedData module)
    {
        builder.InsertData(
            table: nameof(ModulusDbContext.Modules),
            columns: new[] 
            { 
                nameof(ModuleEntity.Id), 
                nameof(ModuleEntity.Name), 
                nameof(ModuleEntity.Version),
                // ... 使用 nameof() 确保编译时检查
            },
            values: new object?[] { module.Id, module.Name, module.Version, ... });
    }
    
    public static void InsertMenu(this MigrationBuilder builder, IMenuSeedData menu) { ... }
    public static void DeleteModule(this MigrationBuilder builder, string moduleId) { ... }
    public static void UpdateModuleVersion(this MigrationBuilder builder, string moduleId, string newVersion) { ... }
}
```

#### 2.3 生成的迁移示例

```csharp
// CLI 生成的迁移 - 强类型、无硬编码
public partial class SeedModule_AddEchoPlugin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertModule(new ModuleSeed
        {
            Id = "EchoPlugin",
            Name = "Echo Plugin",
            Version = "1.0.0",
            Description = "Demo echo functionality",
            Author = "AGIBuild",
            Path = "Modules/EchoPlugin/extension.vsixmanifest",
            IsSystem = true,
            IsEnabled = true,
            MenuLocation = MenuLocation.Main,
            State = ModuleState.Ready
        });
        
        migrationBuilder.InsertMenu(new MenuSeed
        {
            Id = "EchoPlugin.Main",
            ModuleId = "EchoPlugin",
            DisplayName = "Echo",
            Icon = "MessageCircle",
            Route = "echo",
            Location = MenuLocation.Main,
            Order = 100
        });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteModule("EchoPlugin");
    }
}

// file-scoped 实现类
file record ModuleSeed : IModuleSeedData { ... }
file record MenuSeed : IMenuSeedData { ... }
```

**优势**：
- 复用 EF 迁移版本管理、回滚能力
- **强类型接口** + `nameof()` 确保编译时检查
- 避免硬编码表名/列名
- 统一管理 Schema + 数据迁移

### 3. CLI 命令

**Decision**: `modulus add-module-migration`

**自动生成逻辑**：
1. 扫描 `src/Modules/` 目录，解析所有 manifest
2. 分析现有迁移，确定已种子的模块
3. 检测差异（新增/版本更新/删除）
4. 生成 EF 迁移类

**命名约定**：
- 文件名自动生成：`{timestamp}_SeedModule_{Action}.cs`
- 例：`20251210143052_SeedModule_AddEchoPlugin.cs`

**示例输出**：
```bash
$ modulus add-module-migration

Scanning modules...
  ✓ EchoPlugin v1.0.0 (new)
  ✓ ComponentsDemo v1.2.0 (updated)

Generating migration: 20251210143052_SeedModule_Update.cs
  + INSERT EchoPlugin
  ~ UPDATE ComponentsDemo

Done!
```

### 4. 启动流程简化

**Before**:
```csharp
await db.Database.MigrateAsync();

var installer = scope.ServiceProvider.GetRequiredService<SystemModuleInstaller>();
foreach (var dir in moduleDirectories)
{
    await installer.InstallFromDirectoryAsync(dir.Path, dir.IsSystem, hostType);
}

var enabledModules = await db.Modules.Where(m => m.IsEnabled).ToListAsync();
```

**After**:
```csharp
await db.Database.MigrateAsync();  // EF 迁移自动应用模块数据

// 用户安装模块目录仍需扫描（非内置）
if (userModulesDirectory != null && Directory.Exists(userModulesDirectory))
{
    await installer.InstallFromDirectoryAsync(userModulesDirectory, isSystem: false, hostType);
}

var enabledModules = await db.Modules.Where(m => m.IsEnabled).ToListAsync();
```

**Release 模式**：
- 内置模块：通过 EF 迁移预置，无需扫描
- 用户模块：保留目录扫描（`%APPDATA%/Modulus/Modules/`）

**Debug 模式**：
- 可保留 `artifacts/Modules/` 扫描，便于开发调试

### 5. 迁移目录结构

```
src/Shared/Modulus.Infrastructure.Data/Migrations/
├── 20251210000000_InitialCreate.cs                    # Schema + Host 模块种子
├── 20251210000000_InitialCreate.Designer.cs
├── 20251210143052_SeedModule_AddEchoPlugin.cs         # 模块数据
├── 20251210143052_SeedModule_AddEchoPlugin.Designer.cs
├── 20251211092134_SeedModule_AddComponentsDemo.cs     # 模块数据
└── ModulusDbContextModelSnapshot.cs
```

## Risks / Trade-offs

| 风险 | 影响 | Mitigation |
|------|------|------------|
| 迁移合并需要清除现有数据库 | 开发环境需重建 | 仅影响开发，提供清晰文档 |
| CLI 生成迁移需要解析 manifest | 依赖 manifest 格式 | 复用现有 VsixManifestReader |
| 开发模式仍需目录扫描 | DEBUG 和 RELEASE 行为不同 | 可接受，便于开发 |

## Open Questions

1. 是否在 `InitialCreate` 中包含默认内置模块种子？
   - 建议：是，将 HostModules（Settings 等）作为初始种子

2. 用户安装模块是否也迁移到 EF 迁移方式？
   - 建议：否，保持运行时安装流程
