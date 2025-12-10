## 1. 合并现有迁移

- [ ] 1.1 删除现有 8 个迁移文件
- [ ] 1.2 删除本地开发数据库（`modulus.db`）
- [ ] 1.3 生成新的 `InitialCreate` 迁移（包含完整 schema）
- [ ] 1.4 在 `InitialCreate` 中添加 Host 内置模块种子数据（Settings 等）
- [ ] 1.5 验证迁移可正常应用

## 2. 强类型迁移基础设施

- [ ] 2.1 创建 `Seeding/` 目录于 `Modulus.Infrastructure.Data`
- [ ] 2.2 定义 `IModuleSeedData` 接口
- [ ] 2.3 定义 `IMenuSeedData` 接口
- [ ] 2.4 实现 `MigrationBuilderExtensions`（`InsertModule`、`InsertMenu`、`DeleteModule`、`UpdateModuleVersion`）
- [ ] 2.5 使用 `nameof()` 确保列名/表名编译时检查

## 3. CLI 命令实现

- [ ] 3.1 在 `Modulus.Cli` 添加 `add-module-migration` 命令
- [ ] 3.2 实现模块目录扫描和 manifest 解析
- [ ] 3.3 实现现有迁移分析，提取已种子模块
- [ ] 3.4 实现差异检测（新增/更新/删除）
- [ ] 3.5 生成 EF 迁移类（使用强类型扩展方法）
- [ ] 3.6 自动生成迁移文件名（`{timestamp}_SeedModule_{Action}.cs`）

## 4. 简化启动流程

- [ ] 4.1 修改 `ModulusApplicationFactory.CreateAsync`，移除内置模块目录扫描
- [ ] 4.2 保留用户模块目录扫描（`%APPDATA%/Modulus/Modules/`）
- [ ] 4.3 保留 DEBUG 模式下的 `artifacts/Modules/` 扫描（开发便利）
- [ ] 4.4 更新 Avalonia Host 启动逻辑
- [ ] 4.5 更新 Blazor Host 启动逻辑

## 5. 生成示例模块迁移

- [ ] 5.1 使用 CLI 为 EchoPlugin 生成数据迁移
- [ ] 5.2 使用 CLI 为 ComponentsDemo 生成数据迁移
- [ ] 5.3 验证迁移可正常应用并加载模块
