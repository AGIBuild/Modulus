# 参与贡献 Modulus

感谢您对 Modulus 项目的关注！本指南将帮助您开始参与项目贡献。

## 入门指南

1. Fork 本仓库
2. 克隆您的 fork: `git clone https://github.com/your-username/modulus.git`
3. 创建新分支: `git checkout -b feature/your-feature-name`
4. 进行修改
5. 运行测试: `nuke test`
6. 提交更改: `git commit -m "Add feature"`
7. 推送到您的 fork: `git push origin feature/your-feature-name`
8. 创建 Pull Request

## 使用 AI 上下文与 GitHub Copilot

Modulus 提供了内置系统，为 GitHub Copilot 等 AI 工具引导项目上下文，使您更容易理解项目并获得符合项目规范的 AI 辅助。

### 使用 StartAI 命令

在开始使用 AI 辅助进行开发前，请运行:

```powershell
nuke StartAI
```

此命令将输出全面的项目上下文，您可以将其粘贴到 GitHub Copilot Chat 中，以引导其理解 Modulus 项目。

对于特定角色的上下文，使用 `--role` 参数:

```powershell
# 后端开发人员
nuke StartAI --role Backend

# 前端开发人员
nuke StartAI --role Frontend  

# 插件开发人员
nuke StartAI --role Plugin

# 文档贡献者
nuke StartAI --role Docs
```

### Copilot Chat 的快速参考命令

向 Copilot 提供上下文后，您可以在 Copilot Chat 中使用以下命令:

- `/sync` - 刷新项目上下文
- `/roadmap` - 查看项目路线图
- `/why <file>` - 获取特定文件目的的解释

## 文档标准

- 所有面向用户的文档都应有英文和中文版本
- 所有 Story 文档必须有双语版本（位于 `docs/en-US/stories/` 和 `docs/zh-CN/stories/`）
- 遵循 Story 命名约定: `S-XXXX-标题.md`
- 在 Story 文档中包含优先级和状态标签

## 代码风格指南

- 类名和公共成员使用 PascalCase
- 局部变量和参数使用 camelCase
- 私有字段前缀使用下划线 (`_privateField`)
- 为公共 API 添加 XML 文档注释
- 为所有新功能编写单元测试

## 构建和运行

- 使用 Nuke 构建系统: `nuke --help` 查看可用目标
- 运行应用程序: `nuke run`
- 构建所有组件: `nuke build`
- 运行测试: `nuke test`
- 打包插件: `nuke plugin`

## Pull Request 流程

1. 确保您的代码遵循项目的风格指南
2. 根据需要更新文档
3. 为新功能包含测试
4. 提交前确保所有测试通过
5. 在 PR 描述中链接任何相关议题
6. 等待项目维护者的审核

## 需要帮助？

如果您有任何问题，请随时开 issue 或加入我们的社区渠道。
