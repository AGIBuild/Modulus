# Runtime & SDK Contracts (Conceptual)

**Feature**: `001-core-architecture`

本文件以概念层面枚举运行时与 SDK 需要暴露的关键契约，便于后续在代码中落实为具体接口与基类。

---

## 1. 模块与运行时契约

### Module discovery & lifecycle

- `IModuleDescriptor`
  - 描述模块的标识、版本、依赖等；
  - 来源于 manifest 解析结果。

- `IModule`
  - `Initialize(IModuleContext context)`
  - `StartAsync(CancellationToken ct)`
  - `StopAsync(CancellationToken ct)`

- `IModuleContext`
  - 提供对 DI 容器、MediatR、日志、配置等核心服务的访问。

---

## 2. UI 抽象层契约

- `IUIFactory`
  - 根据标识 / ViewModel 创建视图或 UI 容器；
  - 对不同宿主由各自 UI 项目实现。

- `IViewHost`
  - 显示 / 关闭 / 激活视图；
  - 与宿主窗口管理集成。

- `INotificationService`
  - 显示信息 / 警告 / 错误通知；
  - 不暴露具体 UI 控件。

---

## 3. SDK 基类契约

- `ModuleBase : IModule`
  - 提供默认的模块生命周期实现与辅助注册方法。

- `ToolPluginBase`
  - 为工具型插件提供命令注册、视图注册的模板方法；
  - 暴露最少必要的抽象供 AI 与人类实现。

- `DocumentPluginBase`
  - 用于文档 / 编辑器型插件，封装文档打开、保存、视图管理的模式。

---

后续在实现阶段，应将本文件中的概念契约细化为具体接口 / 抽象类，并在 XML 文档与 SDK 指南中同步。


