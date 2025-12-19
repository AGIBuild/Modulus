## MODIFIED Requirements
### Requirement: Module Project Structure
系统 SHALL 根据目标 Host 生成对应的模块项目结构。

#### Scenario: Avalonia 模块包含 View 级菜单声明
- **WHEN** 创建 Avalonia 模块
- **THEN** 生成的默认 ViewModel（如 `MainViewModel`）包含 View 级菜单声明（例如 `[AvaloniaViewMenu]`）
- **AND** 生成的默认 View（如 `MainView.axaml`）与 ViewModel 通过约定绑定（无需额外 `IViewRegistry.Register` 代码）
- **AND** 生成的 View 不包含无参构造函数，仅提供 DI 构造函数（例如 `MainView(MainViewModel vm)`）
- **AND** 生成的 View 使用 `Design.IsDesignMode` 分支支持设计态渲染（无需运行时 DI）
- **AND** 模板包含覆写导航生命周期/拦截的示例代码（如 `CanNavigateFromAsync/CanNavigateToAsync`）

#### Scenario: Blazor 模块包含 View 级菜单声明
- **WHEN** 创建 Blazor 模块
- **THEN** 生成的默认页面（如 `MainView.razor`）包含 View 级菜单声明（例如 `@attribute [BlazorViewMenu]`）
- **AND** 页面包含/继承支持 ViewModel 绑定的基类示例（使 ViewModel 可覆写导航生命周期/拦截）


