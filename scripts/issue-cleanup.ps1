# issue-cleanup.ps1
# 批量关闭43个历史Issue的自动化脚本
# 生成时间：2025-10-21

Write-Host "🚀 开始批量关闭Issue..." -ForegroundColor Cyan
Write-Host "📋 归档文档：docs/reports/backlog-archive-2025-10.md" -ForegroundColor Gray
Write-Host ""

# 通用关闭评论模板
$archiveComment = "本Issue已归档至 docs/reports/backlog-archive-2025-10.md。架构已稳定，从新需求重新开始。如需恢复，请创建新Issue。"

# 第1批：低优先级/非MVP（12个）
Write-Host "=== 第1批：关闭低优先级/非MVP Issue（12个）===" -ForegroundColor Yellow
$lowPriorityIssues = @(1516, 1515, 1514, 1247, 1244, 1242, 1241, 1480, 1377, 1456, 1550, 1220)

foreach ($issue in $lowPriorityIssues) {
    try {
        gh issue close $issue --comment $archiveComment
        Write-Host "  ✅ 已关闭 Issue #$issue (低优先级/非MVP)" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ❌ 关闭 Issue #$issue 失败: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 第2批：重复Issue（2个）
Write-Host "=== 第2批：关闭重复Issue（2个）===" -ForegroundColor Yellow
$duplicateIssues = @(
    @{Number=1542; Comment="与Epic #1343下的同名任务重复，归档至 docs/reports/backlog-archive-2025-10.md"},
    @{Number=1502; Comment="同时归属于Epic #1494和#1343，归档至 docs/reports/backlog-archive-2025-10.md"}
)

foreach ($item in $duplicateIssues) {
    try {
        gh issue close $item.Number --comment $item.Comment
        Write-Host "  ✅ 已关闭 Issue #$($item.Number) (重复)" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ❌ 关闭 Issue #$($item.Number) 失败: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 第3批：Epic #1343子任务（22个）
Write-Host "=== 第3批：关闭Epic #1343子任务（22个）===" -ForegroundColor Yellow
$epic1343Tasks = @(
    1476, 1376, 1369, 1364,  # 处方录入
    1392, 1391, 1390,        # 处方编号
    1400, 1399, 1398,        # 处方状态
    1389, 1388, 1387,        # 就诊查询
    1386, 1385, 1384, 1383,  # 数据导入
    1358, 1352               # 验方管理
)

$epic1343Comment = @"
本Issue已归档至 docs/reports/backlog-archive-2025-10.md。

功能需求已保留，未来实现时请创建新Issue并参考归档文档。
新Issue模板：docs/development/shared/issue-template-v6.md
"@

foreach ($issue in $epic1343Tasks) {
    try {
        gh issue close $issue --comment $epic1343Comment
        Write-Host "  ✅ 已关闭 Issue #$issue (Epic #1343)" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ❌ 关闭 Issue #$issue 失败: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 第4批：Epic #1483子任务（7个）
Write-Host "=== 第4批：关闭Epic #1483子任务（7个）===" -ForegroundColor Yellow
$epic1483Tasks = @(1493, 1492, 1491, 1490, 1489, 1488, 1485)
$epic1483Comment = "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

foreach ($issue in $epic1483Tasks) {
    try {
        gh issue close $issue --comment $epic1483Comment
        Write-Host "  ✅ 已关闭 Issue #$issue (Epic #1483)" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ❌ 关闭 Issue #$issue 失败: $_" -ForegroundColor Red
    }
}

Write-Host ""

# 第5批：Epic #1494子任务（1个）
Write-Host "=== 第5批：关闭Epic #1494子任务（1个）===" -ForegroundColor Yellow
try {
    gh issue close 1538 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
    Write-Host "  ✅ 已关闭 Issue #1538 (Epic #1494)" -ForegroundColor Green
}
catch {
    Write-Host "  ❌ 关闭 Issue #1538 失败: $_" -ForegroundColor Red
}

Write-Host ""

# 第6批：关闭主Epic（3个）
Write-Host "=== 第6批：关闭主Epic（3个）===" -ForegroundColor Yellow
$mainEpics = @(1343, 1483, 1494)
$epicComment = "所有子任务已归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

foreach ($epic in $mainEpics) {
    try {
        gh issue close $epic --comment $epicComment
        Write-Host "  ✅ 已关闭 Epic #$epic" -ForegroundColor Green
        Start-Sleep -Seconds 1
    }
    catch {
        Write-Host "  ❌ 关闭 Epic #$epic 失败: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎉 批量关闭完成！" -ForegroundColor Cyan
Write-Host "📋 归档文档：docs/reports/backlog-archive-2025-10.md" -ForegroundColor Gray
Write-Host "📝 新Issue模板：docs/development/shared/issue-template-v6.md" -ForegroundColor Gray
Write-Host "🚀 新需求工作流：docs/development/shared/new-requirement-workflow.md" -ForegroundColor Gray
Write-Host ""
Write-Host "正在验证清理结果..." -ForegroundColor Cyan
gh issue list --state open --limit 10
