# 故障排除指南

本指南可帮助您解决在使用 Modulus 时可能遇到的常见问题。

## 安装问题

### .NET SDK 缺失
**问题**: 提示 .NET SDK 缺失或版本不匹配。
**解决方案**: 
1. 从 [Microsoft .NET 下载页面](https://dotnet.microsoft.com/download) 安装最新的 .NET SDK（版本 8.0 或更高）。
2. 在终端中运行 `dotnet --version` 验证安装。

### 模板安装失败
**问题**: `dotnet new modulus-app` 或 `dotnet new modulus-plugin` 失败。
**解决方案**:
1. 确保您已安装模板：`dotnet new install <模板文件夹路径>`
2. 检查安装输出中的错误信息。
3. 尝试使用管理员/提升的权限。

## 构建问题

### Nuke 构建失败
**问题**: `nuke build` 命令失败。
**解决方案**:
1. 确保已安装 Nuke.Global 工具：`dotnet tool install Nuke.GlobalTool --global`
2. 查看 `artifacts/logs` 目录中的构建日志，了解具体错误。
3. 验证所有必需的 SDK 和依赖项是否已安装。

### 缺少依赖项
**问题**: 构建失败，提示缺少包引用。
**解决方案**:
1. 恢复 NuGet 包：`dotnet restore`
2. 检查您的网络连接是否可以访问 NuGet 存储库。
3. 如果使用私有包，请验证身份验证是否正确设置。

## 插件问题

### 插件未加载
**问题**: 插件未在应用程序中显示。
**解决方案**:
1. 验证插件是否在正确的目录中：`%USERPROFILE%/.modulus/plugins` 或 `~/.modulus/plugins`
2. 检查插件程序集是否正确实现了 `IPlugin` 接口。
3. 确保插件的契约版本与主机兼容。
4. 检查应用程序日志中的插件加载错误。

### 插件崩溃
**问题**: 插件在操作过程中崩溃。
**解决方案**:
1. 检查日志中的异常。
2. 验证所有插件依赖项是否正确解析。
3. 确保插件与当前主机版本兼容。
4. 禁用插件后重启应用程序，以隔离问题。

### 配置问题
**问题**: 插件设置未被识别。
**解决方案**:
1. 验证 `pluginsettings.json` 格式是否正确（有效的 JSON）。
2. 检查文件是否在正确的位置（与插件 dll 相同的目录）。
3. 确保设置键与插件尝试访问的内容匹配。

## 本地化问题

### 缺少翻译
**问题**: 文本以默认语言而非用户语言显示。
**解决方案**:
1. 验证所需语言的 `lang.xx.json` 文件是否存在。
2. 检查语言代码是否与系统或用户指定的语言匹配。
3. 确保代码中的翻译键与语言文件中的匹配。

### 编码问题
**问题**: 特殊字符显示为乱码。
**解决方案**:
1. 确保所有语言文件均以 UTF-8 编码保存。
2. 检查文件中是否存在 BOM（字节顺序标记）问题。

## UI 集成问题

### 插件 UI 不显示
**问题**: 插件 UI 组件未在主应用程序中显示。
**解决方案**:
1. 检查 `GetMainView()` 或 `GetMenu()` 是否返回有效的 UI 组件。
2. 验证 UI 组件是否符合主机的 UI 框架要求。
3. 查找任何布局或样式不匹配的问题。

### 视觉故障
**问题**: 插件 UI 与应用程序主题不匹配。
**解决方案**:
1. 确保插件 UI 使用主机的主题系统。
2. 避免在插件 UI 中使用硬编码的颜色或样式。
3. 使用不同的主题设置进行测试。

## 调试和高级故障排除

### 启用调试日志
获取更详细的日志：
1. 设置 `MODULUS_LOG_LEVEL=Debug` 环境变量。
2. 在 `%USERPROFILE%/.modulus/logs` 或 `~/.modulus/logs` 中查看日志。

### 调试插件
调试插件：
1. 设置 `MODULUS_DEBUG_PLUGIN=MyPlugin` 环境变量。
2. 将调试器附加到主机进程。
3. 在插件代码中设置断点。

### 报告问题
报告问题时：
1. 包含完整的错误消息和堆栈跟踪。
2. 提供重现步骤。
3. 分享插件和主机版本信息。
4. 如果可能，附上相关日志。

## 需要更多帮助？

如果您仍然遇到问题：
- 查看 [GitHub 存储库](https://github.com/Agibuild/modulus) 中的公开问题。
- 加入我们的社区讨论。
- 直接联系开发团队。
