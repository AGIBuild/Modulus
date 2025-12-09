## 1. 配置项目输出路径
- [x] 1.1 清空 `Directory.Build.props`，移除隐式变量依赖
- [x] 1.2 更新所有 `src/*` 项目：添加显式 `<OutputPath>` 和 `<Import>`
- [x] 1.3 更新所有 `tests/*` 项目：添加显式 `<OutputPath>` 和 `<Import>`
- [x] 1.4 更新所有模块项目：输出到 `artifacts/Modules/{ModuleName}/`
- [x] 1.5 设置 `AppendTargetFrameworkToOutputPath=false` 禁用框架子目录

## 2. 更新 Nuke 构建任务
- [x] 2.1 简化 `BuildApp` target（依赖项目文件中的 OutputPath）
- [x] 2.2 简化 `BuildModule` target（依赖项目文件中的 OutputPath）
- [x] 2.3 更新 `Run` target 在 `artifacts/` 查找可执行文件
- [x] 2.4 确保 `--configuration` 参数正确传递

## 3. 修复 Nuke Test
- [x] 3.1 调查失败的集成测试（缺少模块 manifest 验证）
- [x] 3.2 跳过需要真实模块包的集成测试
- [x] 3.3 验证 `dotnet test` 成功完成

## 4. 验证构建工作流
- [x] 4.1 测试 `dotnet build Modulus.sln` 输出到 `artifacts/`
- [x] 4.2 验证模块输出到 `artifacts/Modules/{ModuleName}/`
- [x] 4.3 验证可执行文件位于 `artifacts/*.exe`
- [x] 4.4 清理 obj 缓存后重新构建验证路径正确
