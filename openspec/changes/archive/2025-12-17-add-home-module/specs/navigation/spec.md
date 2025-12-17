## ADDED Requirements

### Requirement: Default Home Navigation

应用启动后，导航服务 SHALL 自动导航到 Home 主页模块，作为用户看到的第一个视图。

#### Scenario: Avalonia host navigates to Home on startup

- **WHEN** Avalonia Host 应用启动完成
- **AND** 所有模块初始化完毕
- **THEN** 自动导航到 `HomeViewModel`
- **AND** Home 主页视图显示在内容区域

#### Scenario: Blazor host routes to Home on startup

- **WHEN** Blazor Host 应用启动
- **AND** 用户访问根路径 `/`
- **THEN** 路由重定向到 `/home`
- **AND** Home 主页组件渲染

#### Scenario: Home menu item appears first in navigation

- **GIVEN** Home 模块已安装并注册
- **WHEN** 导航菜单渲染
- **THEN** Home 菜单项显示在主菜单列表的第一位（Order=1）
- **AND** 显示 Home 图标和 "Home" 或 "主页" 文字

### Requirement: Home Module Statistics Display

Home 主页 SHALL 展示框架和模块的统计信息，帮助用户了解当前系统状态。

#### Scenario: Display installed modules count

- **WHEN** Home 主页加载
- **THEN** 显示已安装模块的总数
- **AND** 统计数据从 `IModuleRepository` 获取

#### Scenario: Display running modules count

- **WHEN** Home 主页加载
- **THEN** 显示当前运行中模块的数量
- **AND** 运行中模块以视觉高亮方式在轨道区域展示

#### Scenario: Module card shows running status indicator

- **WHEN** Home 主页显示模块卡片
- **AND** 模块处于运行中状态
- **THEN** 卡片右上角显示绿色状态指示灯
- **AND** 指示灯有轻微闪烁动画

#### Scenario: Module card shows stopped status indicator

- **WHEN** Home 主页显示模块卡片
- **AND** 模块处于已停止状态
- **THEN** 卡片右上角显示灰色状态指示灯

#### Scenario: Display framework version

- **WHEN** Home 主页加载
- **THEN** 显示当前 Modulus 框架版本号

### Requirement: Home Module Visual Style

Home 主页 SHALL 采用"轨道控制中心"视觉风格，体现模块化框架的核心概念。

#### Scenario: Starfield background renders

- **WHEN** Home 主页显示
- **THEN** 背景显示深空渐变色
- **AND** 有细腻的星点粒子效果（可通过动画实现）

#### Scenario: Central logo with pulse animation

- **WHEN** Home 主页显示
- **THEN** 中央显示 Modulus Logo
- **AND** Logo 有发光脉冲动画效果

#### Scenario: Module cards float around core

- **WHEN** Home 主页显示
- **AND** 有已安装的模块
- **THEN** 模块以卡片形式围绕中央 Logo 展示
- **AND** 卡片有轻微的漂浮动画效果

#### Scenario: Feature cards grid

- **WHEN** Home 主页显示
- **THEN** 下方显示框架核心特性的卡片网格
- **AND** 包含多宿主架构、热重载、VS兼容、AI就绪等特性

#### Scenario: CLI quick start section

- **WHEN** Home 主页显示
- **THEN** 底部显示终端风格的 CLI 快速入门命令
- **AND** 展示 `modulus new`, `modulus build`, `modulus install` 等命令示例

### Requirement: Quick Create Module Action

Home 主页 SHALL 提供"快速创建模块"入口，方便用户快速开始开发新模块。

#### Scenario: Quick create button displayed

- **WHEN** Home 主页显示
- **THEN** CLI 命令区域旁显示"创建模块"按钮
- **AND** 按钮样式与整体视觉风格一致

#### Scenario: Quick create action triggered

- **WHEN** 用户点击"创建模块"按钮
- **THEN** 打开模块创建向导对话框
- **OR** 跳转到模块开发文档页面（如果向导未实现）

