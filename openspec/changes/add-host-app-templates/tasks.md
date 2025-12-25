## 1. Specification

- [ ] 1.1 添加 `host-app-template` delta spec（CLI/VS 模板、Host 设计契约、可编译性）
- [ ] 1.2 更新 `runtime` delta spec：shared policy 支持 prefix/pattern + diagnostics
- [ ] 1.3 更新 `module-packaging` delta spec：packaging 复用 canonical policy（基于 InstallationTarget 推导 Host 集合）

## 2. Implementation (proposal 批准后执行)

- [ ] 2.1 在 CLI `modulus new` 中新增模板：`avaloniaapp` / `blazorapp`，并更新 `--list` 输出
- [ ] 2.2 新增 CLI Host App 模板文件（嵌入资源），生成可运行解决方案（Avalonia / Blazor Hybrid(MAUI)）
- [ ] 2.3 新增 Visual Studio Host App 项目模板（向导），并打包进 VSIX
- [ ] 2.4 实现 shared policy prefix/pattern 支持（runtime + diagnostics）
- [ ] 2.5 `modulus pack` 与 `nuke pack-module` 复用 canonical policy（包含 prefix/pattern），并基于 InstallationTarget 推导目标 Host 集合
- [ ] 2.6 添加验收测试：生成 Host App 后 `dotnet build` 成功（可选：运行最小启动）


