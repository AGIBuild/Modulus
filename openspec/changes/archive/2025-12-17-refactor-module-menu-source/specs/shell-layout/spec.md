## MODIFIED Requirements

### Requirement: Blazor dynamic module runtime stylesheet injection
Blazor Host MUST 支持为动态加载的第三方模块注入运行时样式，以避免依赖 CSS isolation/static web assets。

#### Scenario: Module provides stylesheet as embedded resource
- **WHEN** 第三方模块声明一个可供 Host 读取的 CSS 资源（例如 embedded resource）
- **THEN** Host 能在不执行模块代码的前提下定位该资源
- **AND** 将 CSS 内容注入到 WebView 的 `<head>` 中

#### Scenario: Navigation triggers module stylesheet injection
- **WHEN** 用户通过菜单导航到某个第三方模块页面
- **THEN** Host 在渲染该页面前完成样式注入
- **AND** 页面呈现使用新样式（具备可观察的样式生效信号）


