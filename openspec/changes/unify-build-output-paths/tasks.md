## 1. 配置 MSBuild 输出路径
- [ ] 1.1 更新 `Directory.Build.props` 设置统一输出路径到 `artifacts/`
- [ ] 1.2 配置 `<OutputPath>` 指向 `$(SolutionDir)artifacts\`
- [ ] 1.3 确保模块项目输出到 `artifacts/Modules/{ModuleName}/`

## 2. 更新 Nuke 构建任务
- [ ] 2.1 更新 `BuildApp` target 使用 `artifacts/` 作为输出
- [ ] 2.2 更新 `BuildModule` target 使用 `artifacts/Modules/{ModuleName}/`
- [ ] 2.3 更新 `Run` target 在 `artifacts/` 查找可执行文件
- [ ] 2.4 确保 `--configuration` 参数正确传递（参考现有 Configuration 属性）

## 3. 修复 Nuke Test
- [ ] 3.1 调查失败的集成测试
- [ ] 3.2 修复或跳过不稳定的集成测试
- [ ] 3.3 验证 `nuke test` 成功完成

## 4. 验证构建工作流
- [ ] 4.1 测试 IDE Debug 构建输出到 `artifacts/`
- [ ] 4.2 测试 `nuke compile` 正确构建
- [ ] 4.3 测试 `nuke build` 生成可用构件
- [ ] 4.4 测试 `nuke build --configuration Release` 生成 Release 构件
- [ ] 4.5 测试 `nuke run` 正确启动应用程序
