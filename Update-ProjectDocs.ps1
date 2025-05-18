# 项目进度状态生成脚本
# 该脚本扫描 docs 目录下的 story 文档，生成项目进度报告

param (
    [string]$ChineseReportPath = ".\docs\reports\story-progress-report.zh-CN.md",
    [string]$EnglishReportPath = ".\docs\reports\story-progress-report.en-US.md"
)

function Get-StoryFiles {
    param([string]$Lang)
    if ($Lang -eq "zh-CN") {
        $dir = ".\docs\zh-CN\stories"
    } else {
        $dir = ".\docs\en-US\stories"
    }
    $storyFiles = @()
    if (Test-Path $dir) {
        $storyFiles += Get-ChildItem -Path $dir -Filter "*.md"
    }
    return $storyFiles
}

function Get-StoryStatus {
    param([string]$Content)
    $total = ([regex]::Matches($Content, '- \[[ xX]\]')).Count
    $done = ([regex]::Matches($Content, '- \[[xX]\]')).Count
    return @{ Total = $total; Done = $done }
}

function Get-StoryId {
    param([string]$FileName)
    $match = [regex]::Match($FileName, 'S-(\d+)-')
    if ($match.Success) { return [int]$match.Groups[1].Value }
    return 9999
}

function Get-ProjectReport {
    param(
        [string]$Lang # zh-CN or en-US
    )
    $storyFiles = Get-StoryFiles -Lang $Lang | Sort-Object { Get-StoryId $_.Name }
    $totalTasks = 0
    $completedTasks = 0
    $storyRows = @()

    foreach ($file in $storyFiles) {
        $content = Get-Content -Path $file.FullName -Raw
        $status = Get-StoryStatus -Content $content
        $totalTasks += $status.Total
        $completedTasks += $status.Done
        $display = if ($status.Total -eq 0) { "N/A" } else { "$($status.Done)/$($status.Total)" }
        $storyRows += "| $($file.BaseName) | $display |"
    }

    $completionRate = if ($totalTasks -gt 0) { [math]::Round($completedTasks * 100.0 / $totalTasks) } else { 0 }

    if ($Lang -eq "zh-CN") {
        $report = @"
# Modulus 项目进度报告

生成时间: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
## 总体进度

- 总任务数: $totalTasks
- 已完成任务: $completedTasks
- 完成率: $completionRate%

## 各 Story 进度

| Story | 完成/总计 |
|-------|-----------|
$($storyRows -join "`n")
"@
    } else {
        $report = @"
# Modulus Project Progress Report

Generated at: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
## Overall Progress

- Total tasks: $totalTasks
- Completed tasks: $completedTasks
- Completion rate: $completionRate%

## Story Progress

| Story | Completed/Total |
|-------|-----------------|
$($storyRows -join "`n")
"@
    }
    return $report
}

# 清理无用的报告文件（只保留本次生成的两个）
$reportDir = Split-Path $ChineseReportPath -Parent
$allReports = Get-ChildItem -Path $reportDir -Filter "story-progress-report*.md" -ErrorAction SilentlyContinue
foreach ($f in $allReports) {
    if ($f.FullName -ne (Resolve-Path $ChineseReportPath) -and $f.FullName -ne (Resolve-Path $EnglishReportPath)) {
        Remove-Item $f.FullName -Force
    }
}

# 主执行流程
Write-Host "开始生成中英文项目进度报告..." -ForegroundColor Cyan

# 生成中文报告
$zhReport = Get-ProjectReport -Lang "zh-CN"
$zhDir = Split-Path $ChineseReportPath -Parent
if (-not (Test-Path $zhDir)) { New-Item -Path $zhDir -ItemType Directory -Force | Out-Null }
$zhReport | Out-File -FilePath $ChineseReportPath -Encoding UTF8
Write-Host "中文报告已生成: $ChineseReportPath" -ForegroundColor Green

# 生成英文报告
$enReport = Get-ProjectReport -Lang "en-US"
$enDir = Split-Path $EnglishReportPath -Parent
if (-not (Test-Path $enDir)) { New-Item -Path $enDir -ItemType Directory -Force | Out-Null }
$enReport | Out-File -FilePath $EnglishReportPath -Encoding UTF8
Write-Host "English report generated: $EnglishReportPath" -ForegroundColor Green

Write-Host "\n✅ 项目进度报告已更新！" -ForegroundColor Green
