## 1. 配置文件

- [ ] 1.1 创建 `bundled-modules.schema.json` JSON Schema 文件
- [ ] 1.2 创建 `src/Hosts/Modulus.Host.Avalonia/bundled-modules.json`
- [ ] 1.3 创建 `src/Hosts/Modulus.Host.Blazor/bundled-modules.json`
- [ ] 1.4 配置默认内置模块列表（如 EchoPlugin）

## 2. Nuke 构建目标

- [ ] 2.1 在 `build/BuildTasks.cs` 添加 `BundleModules` 目标
- [ ] 2.2 实现读取 `bundled-modules.json` 配置
- [ ] 2.3 验证配置的模块在 `artifacts/Modules/` 存在
- [ ] 2.4 输出打包结果摘要（模块数量、名称）
- [ ] 2.5 更新 `Build` 目标依赖 `BundleModules`

## 3. 选择性打包（可选增强）

- [ ] 3.1 支持 `--bundle-only` 参数仅打包配置的模块
- [ ] 3.2 清理未配置的模块（从 artifacts/Modules/ 移除）

## 4. 文档

- [ ] 4.1 更新 CONTRIBUTING.md 说明内置模块配置方式
- [ ] 4.2 添加 bundled-modules.json 示例到文档

