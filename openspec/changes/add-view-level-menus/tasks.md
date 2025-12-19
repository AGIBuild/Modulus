## 1. Implementation
- [x] 1.1 在 `Modulus.Sdk` 新增 View 级菜单 Attribute（Blazor/Avalonia），并补齐 XML 注释与使用示例
- [x] 1.2 扩展安装期 metadata-only 菜单投影：读取模块级菜单（现有）+ 读取 View 级菜单（新增），按“模块父 → View 子”写入 `Menus`（填充 `ParentId`）
- [x] 1.3 实现“单 View 折叠规则”：View 数量为 1 时忽略 View 级菜单（仍允许声明），仅使用模块级菜单作为主菜单项
- [x] 1.4 运行时从 DB 注册 `IMenuRegistry` 时构建层级 `MenuItem.Children`，确保 Avalonia/Blazor Shell 渲染使用树结构
- [x] 1.5 Avalonia 导航从“类型扫描”改为“Key→Target 索引”，并引入 View↔ViewModel 约定绑定（无需模块手写 `IViewRegistry.Register`）
- [x] 1.5.1 Avalonia UI 工厂创建模块 View 时 MUST 使用 `RuntimeModuleHandle.CompositeServiceProvider`（支持 `MyView(MyViewModel vm)` 形式的 DI 构造与 Host 服务注入）
- [x] 1.6 Blazor 导航补齐 Key→Route 解析（避免基于命名猜测），并保持与 DB 投影一致
- [x] 1.7 新增 ViewModel 导航生命周期/拦截（可覆写 NavigateFrom/NavigateTo），并在导航服务中自动调用
- [x] 1.8 更新 `modulus new` 与 VS 模板：默认生成的 View/VM 包含 View 级菜单声明与导航拦截示例
- [x] 1.9 补充单元测试覆盖（必须）：
  - [x] 1.9.1 扩展 `tests/Modulus.Core.Tests/Installation/ModuleInstallerServiceMenuProjectionTests.cs`：覆盖 View 级菜单投影、`ParentId` 层级写入、单 View 折叠（View 菜单存在但不生效）
  - [x] 1.9.2 新增/扩展运行时菜单树组装测试（建议新增 `tests/Modulus.Core.Tests/Runtime/MenuTreeTests.cs`）：覆盖从 DB 菜单组装 `MenuItem.Children`，以及多 View 父菜单 `NavigationKey` 为空（仅展开不导航）
  - [x] 1.9.3 新增导航生命周期与拦截顺序测试（建议 `tests/Modulus.Hosts.Tests/NavigationViewModelLifecycleTests.cs`）：覆盖 guards → 当前 VM `CanNavigateFrom` → 目标 VM `CanNavigateTo` → `OnNavigatedFrom/To` 的调用顺序与短路行为
  - [x] 1.9.4 新增 Avalonia 控件层级菜单行为测试（建议扩展 `tests/Modulus.UI.Avalonia.Tests/NavigationViewTests.cs`）：覆盖父节点无 `NavigationKey` 时点击仅切换展开态、不触发导航命令
- [ ] 1.10 增加“完整导航机制”回归测试（可选但推荐）：在 `tests/Modulus.Hosts.Tests/ModulusApplicationIntegrationTests.cs` 基础上构造最小模块样例（单 View/多 View），验证从安装投影 → 运行时注册 → 导航解析的端到端链路（未实现）


