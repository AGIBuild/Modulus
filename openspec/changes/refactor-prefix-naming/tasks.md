## 1. Specification

- [ ] 1.1 更新 `runtime` delta：Host Id 全量切换（InstallationTarget/TargetHost/示例）
- [ ] 1.2 更新 `module-template` delta：模板默认命名（RootNamespace/AssemblyName/Project）与 Host Id/包引用切换
- [ ] 1.3 更新 `module-packaging` delta：共享程序集剔除规则同步到新程序集名

## 2. Implementation

- [ ] 2.1 重命名 solution：生成 `Agibuild.Modulus.sln` 并确保引用路径正确
- [ ] 2.2 全仓重命名项目目录/文件名：`Modulus.*` → `Agibuild.Modulus.*`
- [ ] 2.3 更新所有 `.csproj`：
  - [ ] 设置 `AssemblyName` 为 `Agibuild.Modulus.*`
  - [ ] 设置 `RootNamespace` 为 `Agibuild.Modulus.*`
  - [ ] 更新 `PackageId` 为 `Agibuild.Modulus.*`（若存在）
- [ ] 2.4 全仓更新源码命名空间与引用（`namespace`、`using`、XAML `x:Class`、Blazor 组件命名空间等）
- [ ] 2.5 全仓更新 `extension.vsixmanifest`：
  - [ ] `InstallationTarget/@Id` → `Agibuild.Modulus.Host.*`
  - [ ] `Asset/@TargetHost` → `Agibuild.Modulus.Host.*`
- [ ] 2.6 更新 Host 实现（Avalonia/Blazor）对 Host Id 的注册与使用
- [ ] 2.7 更新 shared policy / allowlist / diagnostics（确保 Host/SDK 程序集按新名称共享）
- [ ] 2.8 更新模块打包剔除共享程序集逻辑（CLI/Nuke）匹配新程序集名
- [ ] 2.9 更新所有模板（VSIX / dotnet new / CLI 模板）到新命名体系
- [ ] 2.10 验收：
  - [ ] `dotnet restore` 成功
  - [ ] `dotnet build` 成功（包含 Host）
  - [ ] `dotnet test` 成功
  - [ ] 创建并加载一个新模板模块成功（使用新 Host Id）


