# module-template Specification (Delta)

## ADDED Requirements

### Requirement: Template Version Policy (Release Train)
Generated module projects SHALL reference Modulus NuGet packages using a version policy that stays within the same release train as the target Host, to avoid accidental upgrades to incompatible minor/major versions.

#### Scenario: Template references packages within the same minor train
- **WHEN** Host 的 release train 为 `Major.Minor = 1.4`
- **AND** 用户使用模板创建模块项目
- **THEN** 生成的项目应引用 `Agibuild.Modulus.*` 的版本范围以锁定在 `1.4.x`（例如 `1.4.*` 或 `[1.4.0,1.5.0)`）
- **AND** 不应默认引用跨 minor 的浮动版本

#### Scenario: Template choice is explicit and consistent
- **WHEN** 系统选择了某一种模板版本策略（`1.4.*` 或 `[1.4.0,1.5.0)`）
- **THEN** CLI 模板与 VS / dotnet new 模板必须保持一致
- **AND** 文档与生成文件中的版本写法必须一致（避免用户困惑）


