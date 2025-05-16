# Modulus 主程序模板

此目录为 `dotnet new modulus-app` 命令生成主程序项目的模板内容。

## 目录结构
- Modulus.App/         # 业务逻辑与UI
- Modulus.App.Desktop/ # 启动入口
- template.json        # dotnet new 元数据

## 用法
```sh
dotnet new install <本目录路径>
dotnet new modulus-app -n MyApp
```

生成后请根据实际业务扩展主程序。
