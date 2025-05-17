# Example Plugin

这是一个简单的Modulus插件示例，展示了插件系统的基本功能。

## 功能

- 实现了IPlugin接口
- 提供了基本的服务注册和初始化
- 多语言支持（英文和中文）

## 配置

插件通过`pluginsettings.json`文件配置，支持以下选项：

```json
{
  "ContractVersion": "2.0.0",
  "SettingA": "value",
  "SettingB": 123
}
```

## 开发指南

要继续开发这个插件：

1. 修改`PluginEntry.cs`文件来更改插件行为
2. 在`Services`目录中添加更多服务
3. 在`Resources`目录中更新翻译
4. 在`Views`目录中自定义UI

## 构建和部署

使用以下命令构建插件：

```
dotnet build
```

构建完成后，将插件DLL和配置文件复制到Modulus应用程序的插件目录中。
