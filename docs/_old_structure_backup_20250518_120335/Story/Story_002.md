<!-- 优先级：P0（最高） -->
# Story 002

**用户故事（User Story）**
作为开发者，我希望主程序和插件项目都能通过Nuke脚本统一构建、调试和打包，简化多平台开发流程。

**优先级说明**
本Story为当前开发的最高优先级（P0），需优先实现。

**细化验收标准（Acceptance Criteria）**
- Nuke脚本支持 nuke run：运行主程序（可配置项目路径）
- Nuke脚本支持 nuke build：编译主程序和所有插件项目
- Nuke脚本支持 nuke pack：打包主程序和插件为独立产物（如zip、nupkg等）
- Nuke脚本支持 nuke clean：清理所有构建产物
- Nuke脚本支持多平台（Windows/macOS）
- Nuke脚本可扩展支持CI/CD集成
- 构建日志清晰，失败有提示

**细化技术任务（Tasks）**
- [ ] 统一主程序与插件项目的构建入口（如通过解决方案或配置文件自动发现）
- [ ] 在 build/Build.cs 中实现 run/build/pack/clean 任务，支持参数化
- [ ] 支持多平台路径与依赖处理
- [ ] 输出产物到统一的 artifacts/ 目录
- [ ] 提供构建失败时的详细日志与错误提示
- [ ] 在 README 中补充 Nuke 使用说明与常见问题
- [ ] 预留CI/CD集成扩展点（如GitHub Actions、Azure Pipelines）

<!-- 其他Story优先级自动顺延 -->
