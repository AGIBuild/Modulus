## Context

CLI 已有 `.modpkg` 安装能力（`InstallCommand.cs`），但 UI 界面缺乏对应功能。用户需要在扩展管理界面直接安装包文件，且安装后无需重启即可使用。

### 现有架构
- `IModuleInstallerService.InstallFromPathAsync` - 从目录安装（CLI 调用）
- `ModuleListViewModel` - 两个 Host 各有一个版本，包含 `ImportModuleCommand`（目前只支持 manifest.json）
- `IModuleLoader.LoadAsync/UnloadAsync` - 运行时加载/卸载
- CLI `InstallCommand` 实现了完整的 modpkg 解压 → 复制 → 注册流程

## Goals / Non-Goals

### Goals
- 从 UI 选择 `.modpkg` 文件并安装
- 安装后自动加载模块（无需重启）
- 复用 CLI 的核心安装逻辑
- 安装冲突时提示用户确认覆盖

### Non-Goals
- 批量安装多个包
- 从 URL 下载安装
- 拖放安装

## Decisions

### 1. 安装服务层扩展

在 `IModuleInstallerService` 新增方法：
```csharp
Task<ModuleInstallResult> InstallFromPackageAsync(
    string packagePath,
    bool overwrite = false,
    string? hostType = null,
    CancellationToken cancellationToken = default);
```

返回值包含：
- `bool Success`
- `string? ModuleId` - 安装成功时的模块 ID
- `string? Error` - 错误信息
- `bool RequiresConfirmation` - 需要用户确认覆盖

此方法封装 CLI 的解压 → 复制 → 注册逻辑，并返回结果供 UI 层处理。

### 2. Avalonia UI 实现

使用 Avalonia `StorageProvider` API 打开文件选择对话框：
```csharp
var files = await topLevel.StorageProvider.OpenFilePickerAsync(
    new FilePickerOpenOptions
    {
        Title = "Select Module Package",
        FileTypeFilter = new[] { new FilePickerFileType("Module Package") { Patterns = new[] { "*.modpkg" } } },
        AllowMultiple = false
    });
```

优点：
- 跨平台支持良好（Windows/macOS/Linux）
- 无需额外依赖
- Avalonia 官方推荐方式

### 3. Blazor UI 实现

使用 MudBlazor `MudFileUpload` 组件：
```razor
<MudFileUpload T="IBrowserFile" Accept=".modpkg" OnFilesChanged="OnFileSelected">
    <ActivatorContent>
        <MudButton Variant="Variant.Filled" Color="Color.Primary">
            Install Package...
        </MudButton>
    </ActivatorContent>
</MudFileUpload>
```

文件通过 `IBrowserFile.CopyToAsync` 保存到临时目录后调用安装服务。

### 4. 安装后自动加载

安装完成后：
1. 调用 `IModuleLoader.LoadAsync(modulePath)` 运行时加载
2. 通过 `WeakReferenceMessenger` 发送 `MenuItemsAddedMessage` 更新导航
3. 刷新模块列表 UI

### 5. 覆盖确认流程

当目标目录已存在时：
1. 第一次调用 `InstallFromPackageAsync` 返回 `RequiresConfirmation=true`
2. UI 显示确认对话框
3. 用户确认后以 `overwrite=true` 再次调用

## Risks / Trade-offs

### 风险
- **文件锁定**: 如果模块正在运行，覆盖文件可能失败
  - 缓解: 安装前先卸载现有模块

- **部分安装失败**: 复制过程中断可能导致不完整安装
  - 缓解: 先复制到临时目录，成功后再移动

### Trade-offs
- 选择在 Core 层实现安装逻辑（而非 Host 层），增加了一定复杂度，但确保了两个 Host 的一致性

## Open Questions

- 是否需要进度指示器？（对于大包）→ 初版暂不实现，包通常较小

