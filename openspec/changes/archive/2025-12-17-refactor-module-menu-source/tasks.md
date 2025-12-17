## 1. Implementation
- [x] 1.1 移除 `extension.vsixmanifest` 中的菜单声明（manifest 不再包含菜单；菜单仅通过入口类型菜单属性声明）
- [x] 1.2 移除 `bundled-modules.json` 相关逻辑（Host seeder、资源文件、读取与写库路径）
- [x] 1.3 增加菜单属性解析器（metadata-only）：从模块 host-specific 入口类型解析 `[BlazorMenu]` / `[AvaloniaMenu]`
- [x] 1.4 更新安装流程：install/update 时解析菜单属性并投影到 DB；保留 `IsEnabled`（避免升级重置禁用状态）
- [x] 1.5 更新启动流程：允许从系统模块目录与用户模块目录扫描模块 manifest 并进行 install/update（替代旧的“硬编码列表/seed”）
- [x] 1.6 Blazor 动态模块样式：实现运行时 CSS 注入机制（模块提供 CSS 资源，Host 注入到 WebView）
- [x] 1.7 移除旧兼容逻辑（不支持旧菜单来源、旧模块入口兼容分支、旧 DB 假设）；对旧 DB 失败快并提示删除

## 2. Testing
- [x] 2.1 单元测试：菜单属性解析（含多菜单、缺字段、错误格式、host filter）
- [x] 2.2 单元测试：install/update 投影菜单到 DB（Replace 行为、保留 IsEnabled）
- [x] 2.3 集成测试：Host 启动扫描目录 → 安装模块 → 菜单写库 → `IMenuRegistry` 可渲染（Avalonia/Blazor 各 1 条关键路径）
- [x] 2.4 集成测试：Blazor 动态模块 CSS 注入最小闭环（验证注入发生与样式生效信号）

## 3. Documentation
- [x] 3.1 更新 OpenSpec：同步修改 `openspec/specs` 相关能力文档（runtime/module-packaging/shell-layout）
- [x] 3.2 更新工程文档：移除/更新对 `bundled-modules.json` 与 vsixmanifest 菜单声明的描述（仅更新既有文档文件，不新增 README）


