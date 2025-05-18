# Modulus 项目管理指南

本文档描述了 Modulus 项目的任务管理和进度跟踪方法。

## Story 命名规则

为了更好地组织和跟踪 Story，我们采用以下命名规则：

1. **文件命名格式**：`S-XXXX-标题.md`
   - 示例：`S-0001-初始项目设置.md`
   - 示例：`S-0042-插件签名验证.md`

2. **文档标题格式**：`# S-XXXX-标题`
   - 示例：`# S-0001-初始项目设置`
   - 示例：`# S-0042-插件签名验证`

3. **元数据标记**：
   ```markdown
   <!-- 优先级：P0 | P1 | P2 -->
   <!-- 状态：待开始 | 进行中 | 已完成 -->
   ```

## 使用 GitHub Projects 进行任务管理

我们使用 GitHub Projects 来可视化和管理项目进度。这提供了一个集中的位置来跟踪所有用户故事和任务。

### 设置步骤

1. 在 GitHub 仓库中，导航至 "Projects" 选项卡
2. 点击 "New project"
3. 选择 "Board" 模板
4. 创建以下列：
   - Backlog（待办）
   - In Progress（进行中）
   - Review（审核中）
   - Done（已完成）

### 如何将 Story 文档转换为任务项

1. 为每个 Story 创建一个 GitHub Issue
2. 在 Issue 描述中包含 Story 的详细信息和验收标准
3. 使用任务列表（`- [ ] 任务描述`）列出技术任务
4. 将 Issue 添加到项目看板
5. 根据进度更新 Issue 状态和任务完成情况

### 自动化脚本

我们提供了一个 PowerShell 脚本，可以扫描 docs/en-US/stories 和 docs/zh-CN/stories 目录中的 Story 文件，并自动生成进度报告：

```powershell
# 放置在项目根目录，命名为 Generate-StoryProgress.ps1
param (
    [string]$OutputPath = ".\story-progress-report.md"
)

function Get-StoryCompletion {
    param (
        [string]$StoryPath
    )
    
    $content = Get-Content -Path $StoryPath -Raw
    $storyId = [regex]::Match($content, 'Story (\d+)').Groups[1].Value
    $storyTitle = [regex]::Match($content, '# Story \d+\s+\*\*(.*?)\*\*').Groups[1].Value
    
    if ([string]::IsNullOrEmpty($storyTitle)) {
        $storyTitle = [regex]::Match($content, '# Story \d+\s+(.*?)$', [System.Text.RegularExpressions.RegexOptions]::Multiline).Groups[1].Value
    }
    
    $totalTasks = ([regex]::Matches($content, '\- \[ \]')).Count
    $completedTasks = ([regex]::Matches($content, '\- \[x\]')).Count
    
    $percentComplete = 0
    if ($totalTasks -gt 0) {
        $percentComplete = [math]::Round(($completedTasks / $totalTasks) * 100)
    }
    
    return @{
        Id = $storyId
        Title = $storyTitle
        TotalTasks = $totalTasks
        CompletedTasks = $completedTasks
        PercentComplete = $percentComplete
        Path = $StoryPath
    }
}

# 获取所有Story文件
$storyFiles = @()
$storyFiles += Get-ChildItem -Path ".\docs\en-US\stories\*.md" -Recurse
$storyFiles += Get-ChildItem -Path ".\docs\zh-CN\stories\*.md" -Recurse

# 分析每个Story文件
$stories = @()
foreach ($file in $storyFiles) {
    $story = Get-StoryCompletion -StoryPath $file.FullName
    $stories += $story
}

# 按Story ID排序
$stories = $stories | Sort-Object -Property Id

# 生成报告
$report = @"
# Modulus Story进度报告

*生成时间: $(Get-Date -Format "yyyy-MM-dd HH:mm")*

## 总体进度

总任务数: $($stories | Measure-Object -Property TotalTasks -Sum | Select-Object -ExpandProperty Sum)
已完成任务: $($stories | Measure-Object -Property CompletedTasks -Sum | Select-Object -ExpandProperty Sum)
总体完成率: $([math]::Round(($stories | Measure-Object -Property CompletedTasks -Sum | Select-Object -ExpandProperty Sum) / ($stories | Measure-Object -Property TotalTasks -Sum | Select-Object -ExpandProperty Sum) * 100))%

## 各Story进度

| ID | 标题 | 进度 | 完成/总计 |
|---|---|---|---|
"@

foreach ($story in $stories) {
    $progressBar = ""
    $progressBar = "[$("■" * ($story.PercentComplete / 10))$("□" * ((100 - $story.PercentComplete) / 10))] $($story.PercentComplete)%"
    
    $report += "`n| Story $($story.Id) | $($story.Title) | $progressBar | $($story.CompletedTasks)/$($story.TotalTasks) |"
}

# 写入报告文件
$report | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "报告已生成至: $OutputPath"
```

使用方法：
```
.\Generate-StoryProgress.ps1
```

这将生成一个包含所有 Story 进度的 Markdown 报告。
