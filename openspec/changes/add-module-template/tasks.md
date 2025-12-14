# Tasks: 添加模块项目模板

## 0. 前置条件

- [ ] 0.1 确认 Modulus.Sdk 已发布到 NuGet
- [ ] 0.2 确认 Modulus.UI.Abstractions 已发布到 NuGet
- [ ] 0.3 确认 Modulus.UI.Avalonia 已发布到 NuGet

## 1. Visual Studio 项目模板

- [x] 1.1 创建 `templates/VisualStudio/` 目录结构
- [x] 1.2 创建 Avalonia 多项目模板：
  - 主 `.vstemplate`（MultiProject）
  - Core 子项目模板
  - UI.Avalonia 子项目模板
  - extension.vsixmanifest
- [x] 1.3 创建 Blazor 多项目模板：
  - 主 `.vstemplate`（MultiProject）
  - Core 子项目模板
  - UI.Blazor 子项目模板
  - extension.vsixmanifest
- [x] 1.4 创建模板安装脚本（PowerShell）
- [ ] 1.5 验证 VS 模板在 Visual Studio 中可用（需手动测试）

## 2. CLI 模板引擎

- [x] 2.1 创建 `src/Modulus.Cli/Templates/` 目录结构
- [x] 2.2 实现 `TemplateEngine` 类：变量替换、文件生成
- [x] 2.3 定义 `ModuleTemplateContext` 数据模型
- [x] 2.4 创建模板文件（嵌入式资源）

## 3. CLI 命令实现

- [x] 3.1 创建 `NewCommand.cs`，定义参数和选项
- [x] 3.2 实现向导模式：交互式选择 target 和收集信息
- [x] 3.3 实现批处理模式：从命令行参数读取
- [x] 3.4 实现输出目录验证
- [x] 3.5 在 `Program.cs` 中注册 `new` 命令

## 4. 验证和测试

- [ ] 4.1 验证 VS 模板生成的 Avalonia 项目可编译（需 NuGet 包发布后）
- [ ] 4.2 验证 VS 模板生成的 Blazor 项目可编译（需 NuGet 包发布后）
- [x] 4.3 验证 CLI 生成的 Avalonia 项目结构正确
- [x] 4.4 验证 CLI 生成的 Blazor 项目结构正确
- [ ] 4.5 验证生成的模块可被 Host 加载（需 NuGet 包发布后）

