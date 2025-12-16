## Context

Modulus 是一个插件式应用框架，当前缺少一个能展示框架价值的"门面"页面。用户第一次打开应用时看到的是模块管理列表，无法快速理解框架的核心卖点。

需要一个视觉上有吸引力、信息展示清晰的主页模块，作为应用的"第一印象"。

## Goals / Non-Goals

**Goals:**
- 创建视觉新颖的主页，展示 Modulus 框架的核心价值
- 应用启动后默认显示主页
- 展示已安装模块状态、框架特性、快速入门命令
- 支持 Avalonia 和 Blazor 双平台

**Non-Goals:**
- 不需要复杂的交互逻辑（主页主要是展示性质）
- 不需要实时数据刷新（模块状态在打开时获取即可）
- 不需要支持用户自定义布局

## Decisions

### 1. 设计风格：轨道控制中心

**选择**: 采用"太空/轨道"隐喻的设计语言

**原因**:
- "模块围绕核心运行"与"插件式框架"概念完美契合
- 深色科技感与现有主题风格一致
- 动态效果（漂浮、脉冲）增加视觉吸引力
- 区别于传统管理后台的"AI slop"风格

**视觉元素**:
```
┌─────────────────────────────────────────────────────────────┐
│ 🌟 星空粒子背景 (CSS/Canvas 实现)                           │
│                                                             │
│     ╭──────╮   ╭──────╮   ╭──────╮                         │
│     │Module│   │Module│   │Module│  ← 漂浮的模块卡片        │
│     ╰──────╯   ╰──────╯   ╰──────╯                         │
│                 ╭─────────────╮                             │
│                 │  MODULUS    │ ← 中央发光 Logo + 脉冲动画  │
│                 │    ◉ ✨     │                             │
│                 ╰─────────────╯                             │
│                                                             │
│  ┌─ 特性卡片 ──────────────────────────────────────────┐   │
│  │ 多宿主 │ 热重载 │ VS兼容 │ AI就绪 │                 │   │
│  └────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─ 快速开始 ─────────────────────────────────────────┐   │
│  │  $ modulus new MyModule -t avalonia                │   │
│  └────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 2. 模块结构

```
src/Modules/Home/
├── extension.vsixmanifest          # 模块清单
├── Home.Core/
│   ├── Home.Core.csproj
│   ├── HomeModule.cs               # ModulusPackage 入口
│   ├── ViewModels/
│   │   └── HomeViewModel.cs        # 主页 ViewModel
│   └── Services/
│       ├── IHomeStatisticsService.cs
│       └── HomeStatisticsService.cs
├── Home.UI.Avalonia/
│   ├── Home.UI.Avalonia.csproj
│   ├── HomeAvaloniaModule.cs       # UI 入口
│   ├── HomeView.axaml              # 主视图
│   ├── HomeView.axaml.cs
│   └── Controls/                   # 自定义控件
│       ├── StarfieldPanel.cs       # 星空背景
│       └── OrbitPanel.cs           # 轨道布局
└── Home.UI.Blazor/
    ├── Home.UI.Blazor.csproj
    ├── HomeBlazorModule.cs         # UI 入口
    └── HomePage.razor              # 主页面
```

### 3. 统计数据来源

**HomeStatisticsService** 通过注入获取数据：
- 已安装模块数：`IModuleRepository.GetAllAsync().Count`
- 运行中模块数：`IModuleManager.LoadedModules.Count`
- 框架版本：Assembly metadata

### 4. 默认导航实现

**Avalonia Host** (`App.axaml.cs`):
```csharp
// 原: shellVm.NavigateTo<ModuleListViewModel>();
// 改: shellVm.NavigateTo<HomeViewModel>();
```

**Blazor Host** (`Routes.razor`):
- 默认路由从 `/modules` 改为 `/home`

### 5. 菜单注册

菜单不在 manifest 中声明，必须通过 host-specific 模块入口类型的菜单属性声明（安装/更新时 metadata-only 解析并投影到 DB）。

示例（Avalonia）：
```csharp
[AvaloniaMenu("home", "Home", typeof(HomeViewModel), Icon = IconKind.Home, Order = 1)]
public sealed class HomeAvaloniaModule : AvaloniaModuleBase { }
```

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 星空动画可能影响低端设备性能 | 提供动画开关或根据设备性能自动降级 |
| 设计风格可能与用户期望不符 | 保持布局可扩展，便于后续调整 |
| 默认导航变更可能影响现有用户习惯 | ModuleList 仍然可以通过菜单访问 |

## Migration Plan

1. 创建 Home 模块，先不修改默认导航
2. 测试 Home 模块独立运行
3. 修改默认导航，全面测试
4. 合并代码

## Resolved Questions

- [x] 是否需要在 Home 页面添加"快速创建模块"的交互入口？**是** - 在 CLI 命令区域添加"创建模块"按钮，点击后打开模块创建向导或跳转到文档
- [x] 模块卡片是否需要显示运行状态（运行中/已停止）？**是** - 模块卡片右上角显示状态指示灯（绿色=运行中，灰色=已停止）

