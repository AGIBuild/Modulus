# Modulus 插件模板

此目录为 `dotnet new modulus-plugin` 命令生成插件项目的模板内容。

## 目录结构
- PluginEntry.cs：插件主入口示例
- template.json：dotnet new 元数据

## 用法
```sh
dotnet new install <本目录路径>
dotnet new modulus-plugin -n MyPlugin
```

生成后请根据实际业务实现插件逻辑。
