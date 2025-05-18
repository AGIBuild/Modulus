🧑‍💻 用户故事（User Story）
作为一名使用 Modulus 模板开发跨平台插件式工具应用的开发者，
我希望能够通过简单的 CLI 命令（如 dotnet new modulus-app 和 dotnet new modulus-plugin）
快速创建主程序或插件模板项目，并使用一致的构建工具（如 nuke) 来运行、调试和打包整个项目，
以便我能专注于业务逻辑开发，而不需要关心底层项目结构或构建细节。


✅ 验收标准（Acceptance Criteria）

编号 | 验收标准描述
-- | --
AC1 | 成功安装必要的 .NET SDK（>= .NET 8）和 Avalonia 环境
AC2 | 能通过 dotnet new modulus-app 创建主程序项目
AC3 | 能通过 dotnet new modulus-plugin 创建插件模板项目
AC4 | 项目结构清晰，遵循分层架构，支持模块化开发
AC5 | 使用 Nuke 提供统一构建脚本，支持以下命令：
  | - nuke run：运行主程序
  | - nuke build：编译项目
  | - nuke pack：打包主程序与插件
  | - nuke clean：清理构建产物
AC6 | 项目模板中包含基本的插件加载逻辑（可空实现）

🏗️ 技术任务（Tasks）


编号 | 技术任务描述
-- | --
T1 | 创建 Git 仓库与解决方案结构，分为 src/、build/、templates/、tools/ 等
T2 | 安装并初始化 Nuke 构建系统
T3 | 编写 modulus-app 项目模板，放置于 templates/modulus-app
T4 | 编写 modulus-plugin 插件模板，放置于 templates/modulus-plugin
T5 | 配置 dotnet new 模板结构和 template.json 元数据
T6 | 添加 PluginLoader 空实现类至模板中
T7 | 提供 README 示例说明使用方式与创建方法
T8 | 添加基础 .editorconfig 和命名规范配置

📁 项目目录初步结构
``` sh
Modulus/
│
├── build/                     # Nuke 构建脚本目录
│   └── Build.cs               # 主构建脚本
│
├── src/
│   ├── Modulus.App/           # 主程序模板生成后目录（示例）
│   └── Modulus.PluginHost/    # 插件加载/管理功能（可抽象出来）
│
├── templates/
│   ├── modulus-app/           # dotnet new 模板：主程序项目
│   └── modulus-plugin/        # dotnet new 模板：插件项目
│
├── tools/
│   └── nuke/                  # nuke 配置生成后自动添加
│
├── .config/dotnet-tools.json # dotnet tool 配置
├── global.json                # 固定 SDK 版本
└── README.md                  # 项目说明文档
```

🔧 依赖与工具版本建议
- .NET SDK ≥ 8.0
- Avalonia UI ≥ 11.0
- Nuke.Build ≥ 6.2
- dotnet CLI ≥ 8.0
- OS: Windows/macOS（需支持 CLI）

📎 附加说明
模板设计应支持未来插件签名、本地化、配置、热更新等功能的演进。

可将 modulus-plugin 模板设计为独立构建插件，方便第三方集成。

nuke 可作为核心构建、测试、发布入口，替代繁琐的多平台脚本。