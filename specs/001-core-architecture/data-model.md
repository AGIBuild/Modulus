# Data Model: Modulus 核心架构与双宿主运行时

**Feature**: `001-core-architecture`  
**Source Spec**: `specs/001-core-architecture/spec.md`  
**Last Updated**: 2025-12-03

本文件从规格中提取核心概念实体，描述它们的职责、关键字段与关系，供后续实现与 SDK 设计参考。

---

## 1. Module（模块）

**职责**: 表示一个垂直切片功能单元，可包含 Domain / Application / Infrastructure 以及可选的
Presentation / UI 实现。

**关键属性（示意字段名，仅为设计参考）**:

- `ModuleId`：模块唯一标识（**推荐使用 GUID**，例如 `"a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"`）
- `Version`：模块版本（语义化版本）
- `DisplayName`：模块对用户展示的名称
- `Description`：模块描述
- `SupportedHosts`：支持的宿主类型列表（如 `BlazorApp`, `AvaloniaApp`）
- `Assemblies`：该模块包含的程序集列表（含核心与 UI 程序集）
- `Dependencies`：对其它模块或运行时能力的依赖声明

**关系**:

- 一个 `Module` 由一个或多个程序集实现；
- 一个 `Module` 需要映射到一个或多个 `PluginPackage`。

---

## 2. Host（宿主）

**职责**: 提供外壳环境（窗口、生命周期、导航、菜单等），负责加载与管理模块。

**关键属性**:

- `HostId`：宿主标识（如 `"BlazorDesktop"`, `"AvaloniaDesktop"`）
- `HostType`：宿主类型枚举（`Blazor`, `Avalonia`, ...）
- `Capabilities`：宿主提供的能力集合（窗口管理、通知、文档标签页等）
- `LoadedModules`：当前已加载模块列表

**关系**:

- 一个 `Host` 可以同时加载多个 `Module`；
- 不同 `Host` 可以加载相同的 `Module` 核心程序集，但使用不同的 UI 程序集。

---

## 3. PluginPackage（插件包）

**职责**: 作为部署与分发单位，将模块的程序集与资源打包在一起，供宿主发现与加载。

**关键属性**:

- `PackageId`：插件包标识（通常与模块标识相关）
- `Version`：插件包版本
- `Manifest`：`Manifest` 对象（见下一节）
- `ContentPath`：包内容所在路径（本地文件路径或解压目录）
- `SignatureInfo`：签名与完整性校验信息（如签名文件路径或证书指纹）

**关系**:

- 一个 `PluginPackage` 至少包含一个 `Module` 的实现；
- 一个 `Module` 在不同部署形式下可以对应多个 `PluginPackage`（例如社区版 / 企业版）。

---

## 4. Manifest（模块 / 插件清单）

**职责**: 描述插件包中包含的模块信息、宿主支持情况与依赖关系，是运行时发现与加载的入口。

**关键属性**:

- `Id`：模块 / 插件标识（与 `ModuleId` 对应）
- `Version`：模块版本
- `Title`：显示名称
- `Description`：描述
- `Authors`：作者 / 组织信息
- `SupportedHosts`：支持的宿主枚举列表
- `CoreAssemblies`：核心程序集路径列表
- `UiAssemblies`：按宿主类型分组的 UI 程序集路径列表
- `Dependencies`：对其它模块或运行时能力的依赖

**关系**:

- 一个 `Manifest` 与一个 `PluginPackage` 一一对应；
- 运行时通过解析 `Manifest` 构建 `Module` 对象与加载计划。

---

## 5. UIAbstractionContract（UI 抽象契约）

**职责**: 定义模块表达 UI 意图的方式，屏蔽具体 UI 技术细节。

**核心接口示例（概念级，而非最终 API）**:

- `IUIFactory`：根据 ViewModel / 标识创建视图或 UI 容器；
- `IViewHost`：承载视图的宿主接口，用于显示 / 关闭 / 激活视图；
- `IUiCommand`：描述可绑定到 UI 的命令；
- `NotificationContract`：描述通知 / 提示的显示方式。

**关系**:

- 模块通过 `UIAbstractionContract` 与 `Host` 进行 UI 交互；
- 不同宿主提供各自的 UI 实现，但共享同一套抽象契约。

---

## 6. SDKBaseType（SDK 基类）

**职责**: 为模块与插件作者（含 AI）提供强类型基类与扩展点，固化推荐模式。

**典型基类示意**:

- `ModuleBase`：模块生命周期与依赖注册入口；
- `ToolPluginBase`：工具型插件基类，封装命令、UI 注册等；
- `DocumentPluginBase`：文档型 / 编辑器型插件基类。

**关系**:

- SDK 基类通常依赖 `UIAbstractionContract` 与核心运行时服务；
- 插件 / 模块实现者通过继承 SDK 基类与实现特定虚方法完成注册。

---

## 7. RuntimeContext / ModuleRuntime（运行时上下文）

**职责**: 表示运行中的模块系统状态，管理加载的模块、宿主与 ALC。

**关键属性**:

- `LoadedModules`：已加载的 `Module` 集合
- `Hosts`：已激活的 `Host` 集合
- `AssemblyLoadContexts`：每个模块或模块组对应的 ALC 信息
- `Mediator`：MediatR 实例或接口，用于请求 / 通知分发

**关系**:

- 运行时在启动时创建 `RuntimeContext`，之后所有模块加载 / 卸载操作都经由此上下文协调；
- 该模型为后续实现插件监控、诊断与调试提供基础。


