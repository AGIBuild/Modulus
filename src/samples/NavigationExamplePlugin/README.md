# Navigation Example Plugin

这是一个示例插件，展示了如何为Modulus应用创建一个带导航功能的插件。

## 功能

- 为主应用提供导航菜单项
- 显示自定义视图
- 提供多语言支持

## 开发指南

### 依赖项

- Modulus.Plugin.Abstractions
- Avalonia UI

### 构建说明

1. 确保已安装.NET 8 SDK
2. 在插件根目录运行 `dotnet build`
3. 构建输出将放置在 `bin/Debug/net8.0` 目录中

### 部署

将构建生成的DLL文件和所有相关JSON文件复制到Modulus应用的插件目录：

```
%USERPROFILE%\.modulus\plugins\NavigationExamplePlugin\
```

## 许可

此示例插件遵循MIT许可协议。
