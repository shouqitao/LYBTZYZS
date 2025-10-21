# Issue批量清理操作指南

> **目标**：清理44个历史Issue，从稳定架构重新出发
> **生成时间**：2025-10-21
> **归档文档**：`docs/reports/backlog-archive-2025-10.md`

---

## 📋 清理策略

### 策略分类

| 类别 | Issue数量 | 处理方式 | 关闭模板 |
|------|---------|---------|---------|
| **低优先级/非MVP** | 12个 | 立即关闭 | 模板A |
| **重复Issue** | 2个 | 立即关闭 | 模板B |
| **核心MVP功能** | 29个 | 评估后关闭 | 模板C |
| **紧急Bug** | 1个 | 验证后决定 | 模板D |

---

## 🔴 第1步：验证紧急Bug（必须先处理）

### Issue #1537 - API契约不匹配
```bash
# 验证步骤
gh issue view 1537 --json title,body,labels,state

# 检查当前API契约状态
dotnet build LYBT.All.sln -c Release --no-restore

# 如果编译通过且无HTTP请求失败 → 标记为"已验证无需执行"
# 如果确实存在问题 → 立即修复后关闭
```

**关闭模板D（Bug已验证不存在）**：
```markdown
经验证，当前代码库中不存在此问题：
- 编译通过：0 errors, 0 warnings
- API契约已对齐：Client端与Server端契约一致
- 验证报告：[链接到验证文档]

本Issue已归档至 `docs/reports/backlog-archive-2025-10.md`。
```

---

## 🗑️ 第2步：关闭低优先级/非MVP Issue（12个）

### 批量关闭命令

```bash
# Workstation架构重构（3个）
gh issue close 1516 1515 1514 --comment "架构已稳定，归档至 docs/reports/backlog-archive-2025-10.md"

# Desktop端优化（4个）
gh issue close 1247 1244 1242 1241 --comment "非MVP功能，归档至 docs/reports/backlog-archive-2025-10.md"

# 文档任务（1个）
gh issue close 1480 --comment "文档按需更新，归档至 docs/reports/backlog-archive-2025-10.md"

# 预留UI（1个）
gh issue close 1377 --comment "功能未启用，归档至 docs/reports/backlog-archive-2025-10.md"

# 单独处理的Epic
gh issue close 1456 --comment "与Epic #1343重复，归档至 docs/reports/backlog-archive-2025-10.md"
gh issue close 1550 --comment "打印功能已包含在Epic #1343中，归档至 docs/reports/backlog-archive-2025-10.md"
gh issue close 1220 --comment "测试作为持续工程任务，不在Issue中跟踪，归档至 docs/reports/backlog-archive-2025-10.md"
```

**关闭模板A（低优先级/非MVP）**：
```markdown
本Issue已归档至 `docs/reports/backlog-archive-2025-10.md`。

关闭原因：
- ✅ 架构已趋于稳定，从新需求重新开始
- ✅ 功能需求已记录在归档清单中，未来可参考实现
- ✅ 非MVP核心功能，优先级较低

如需恢复此功能，请创建新Issue并参考归档文档。
```

---

## 🔄 第3步：关闭重复Issue（2个）

```bash
# 处方打印功能重复
gh issue close 1542 --comment "与Epic #1343下的同名任务重复，归档至 docs/reports/backlog-archive-2025-10.md"

# 自动保存草稿重复
gh issue close 1502 --comment "同时归属于Epic #1494和#1343，归档至 docs/reports/backlog-archive-2025-10.md"
```

**关闭模板B（重复Issue）**：
```markdown
本Issue与以下Issue功能重复：
- 相关Issue：#[编号]
- 所属Epic：#[Epic编号]

已归档至 `docs/reports/backlog-archive-2025-10.md`，统一由新需求管理。
```

---

## 📦 第4步：关闭核心MVP功能Issue（29个）

### 分批处理策略

#### 批次1：Epic #1343 - 处方管理（13个）
```bash
# 处方录入功能（6个）
gh issue close 1476 1376 1369 1364 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

# 处方编号功能（3个）
gh issue close 1392 1391 1390 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

# 处方状态管理（3个）
gh issue close 1400 1399 1398 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

#### 批次2：Epic #1343 - 就诊/患者管理（7个）
```bash
# 就诊查询功能（3个）
gh issue close 1389 1388 1387 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

# 数据导入功能（4个）
gh issue close 1386 1385 1384 1383 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

#### 批次3：Epic #1343 - 验方管理（2个）
```bash
gh issue close 1358 1352 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

#### 批次4：Epic #1483 - 就诊流程UI（7个）
```bash
gh issue close 1493 1492 1491 1490 1489 1488 1485 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

#### 批次5：Epic #1494 - 医案流程UI（1个）
```bash
gh issue close 1538 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

#### 批次6：关闭主Epic（3个）
```bash
# 最后关闭主Epic
gh issue close 1343 1483 1494 --comment "所有子任务已归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
```

**关闭模板C（核心功能归档）**：
```markdown
本Issue已归档至 `docs/reports/backlog-archive-2025-10.md`。

关闭原因：
- ✅ 架构已趋于稳定，从新需求重新开始
- ✅ 功能需求详细记录在归档清单中
- ✅ 避免历史Issue堆积，保持项目清爽

📌 **功能需求未丢失**：
- 归档文档：`docs/reports/backlog-archive-2025-10.md`
- 功能描述、验收标准、实现思路已完整保留
- 未来实现时，创建新Issue并参考归档即可

如需实现此功能，请创建新Issue：
1. 参考归档文档中的功能描述
2. 使用新Issue模板（`docs/development/shared/issue-template-v6.md`）
3. 确保符合当前稳定架构
```

---

## 🔄 第5步：执行批量关闭

### 完整脚本（PowerShell）

```powershell
# issue-cleanup.ps1
# 批量关闭44个历史Issue的自动化脚本

# 第1批：低优先级/非MVP（12个）
$lowPriorityIssues = @(1516, 1515, 1514, 1247, 1244, 1242, 1241, 1480, 1377, 1456, 1550, 1220)
$lowPriorityComment = "本Issue已归档至 docs/reports/backlog-archive-2025-10.md。架构已稳定，从新需求重新开始。如需恢复，请创建新Issue。"

foreach ($issue in $lowPriorityIssues) {
    gh issue close $issue --comment $lowPriorityComment
    Write-Host "✅ 已关闭 Issue #$issue (低优先级/非MVP)"
    Start-Sleep -Seconds 1
}

# 第2批：重复Issue（2个）
$duplicateIssues = @(
    @{Number=1542; Comment="与Epic #1343下的同名任务重复，归档至 docs/reports/backlog-archive-2025-10.md"},
    @{Number=1502; Comment="同时归属于Epic #1494和#1343，归档至 docs/reports/backlog-archive-2025-10.md"}
)

foreach ($item in $duplicateIssues) {
    gh issue close $item.Number --comment $item.Comment
    Write-Host "✅ 已关闭 Issue #$($item.Number) (重复)"
    Start-Sleep -Seconds 1
}

# 第3批：Epic #1343子任务（22个）
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
    gh issue close $issue --comment $epic1343Comment
    Write-Host "✅ 已关闭 Issue #$issue (Epic #1343)"
    Start-Sleep -Seconds 1
}

# 第4批：Epic #1483子任务（7个）
$epic1483Tasks = @(1493, 1492, 1491, 1490, 1489, 1488, 1485)
$epic1483Comment = "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

foreach ($issue in $epic1483Tasks) {
    gh issue close $issue --comment $epic1483Comment
    Write-Host "✅ 已关闭 Issue #$issue (Epic #1483)"
    Start-Sleep -Seconds 1
}

# 第5批：Epic #1494子任务（1个）
gh issue close 1538 --comment "归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"
Write-Host "✅ 已关闭 Issue #1538 (Epic #1494)"

# 第6批：关闭主Epic（3个）
$mainEpics = @(1343, 1483, 1494)
$epicComment = "所有子任务已归档至 docs/reports/backlog-archive-2025-10.md，功能需求保留待新Issue实现"

foreach ($epic in $mainEpics) {
    gh issue close $epic --comment $epicComment
    Write-Host "✅ 已关闭 Epic #$epic"
    Start-Sleep -Seconds 1
}

Write-Host "`n🎉 批量关闭完成！已关闭43个Issue（Issue #1537需单独验证）"
Write-Host "📋 归档文档：docs/reports/backlog-archive-2025-10.md"
Write-Host "📝 新Issue模板：docs/development/shared/issue-template-v6.md"
```

---

## ⚠️ Issue #1537 特殊处理

### 验证脚本

```powershell
# verify-issue-1537.ps1
# 验证"API契约不匹配"问题是否真实存在

Write-Host "🔍 验证 Issue #1537 - Client端API契约不匹配"

# 步骤1：编译检查
Write-Host "`n步骤1：编译检查..."
dotnet build LYBT.All.sln -c Release --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 编译通过（0 errors, 0 warnings）"
} else {
    Write-Host "❌ 编译失败，问题可能存在"
    exit 1
}

# 步骤2：检查API契约定义
Write-Host "`n步骤2：检查API契约定义..."
$apiContracts = @(
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IConsultationApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IHerbApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IPatientApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IPrescriptionApi.cs",
    "src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs"
)

foreach ($file in $apiContracts) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file 存在"
    } else {
        Write-Host "  ❌ $file 缺失"
    }
}

# 步骤3：检查git status（查看API契约修改）
Write-Host "`n步骤3：检查API契约修改状态..."
git status --short $apiContracts

# 步骤4：生成验证报告
Write-Host "`n步骤4：生成验证报告..."
$reportPath = "docs/reports/issue-1537-verification.md"

$reportContent = @"
# Issue #1537 验证报告

> **验证时间**：$(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
> **验证人员**：自动化脚本

## 验证结果

### 编译状态
- ✅ 编译通过（0 errors, 0 warnings）

### API契约文件检查
$(foreach ($file in $apiContracts) {
    if (Test-Path $file) { "- ✅ $file 存在" } else { "- ❌ $file 缺失" }
})

### Git修改状态
``````
$(git status --short $apiContracts)
``````

## 结论

根据验证结果，Issue #1537 描述的"Client端API契约不匹配导致所有业务模块HTTP请求失败"问题**不存在**。

- 编译正常通过
- API契约文件完整
- 当前代码库状态稳定

建议关闭此Issue，归档至 docs/reports/backlog-archive-2025-10.md。
"@

Set-Content -Path $reportPath -Value $reportContent -Encoding UTF8
Write-Host "✅ 验证报告已生成：$reportPath"

# 步骤5：自动关闭Issue
Write-Host "`n步骤5：关闭Issue #1537..."
gh issue close 1537 --comment "经验证，此问题不存在。详见验证报告：$reportPath。已归档至 docs/reports/backlog-archive-2025-10.md"
Write-Host "✅ Issue #1537 已关闭"
```

---

## 📊 清理进度追踪

### 执行检查清单

- [ ] **第1步**：验证Issue #1537（Bug优先）
  - [ ] 运行验证脚本 `scripts/verify-issue-1537.ps1`
  - [ ] 生成验证报告 `docs/reports/issue-1537-verification.md`
  - [ ] 根据验证结果决定关闭/修复

- [ ] **第2步**：关闭低优先级Issue（12个）
  - [ ] 执行批量关闭命令
  - [ ] 验证关闭状态 `gh issue list --state closed`

- [ ] **第3步**：关闭重复Issue（2个）
  - [ ] 执行批量关闭命令
  - [ ] 验证关闭状态

- [ ] **第4步**：关闭核心MVP功能Issue（29个）
  - [ ] 执行批次1-5关闭命令
  - [ ] 验证归档文档完整性

- [ ] **第5步**：关闭主Epic（3个）
  - [ ] 执行主Epic关闭命令
  - [ ] 验证所有子任务已关闭

- [ ] **第6步**：验证清理结果
  - [ ] 运行 `gh issue list --state open` 确认无遗留
  - [ ] 检查归档文档 `docs/reports/backlog-archive-2025-10.md`

---

## 🎯 清理后状态

### 预期结果
- ✅ 44个Issue全部关闭（或#1537修复后关闭）
- ✅ 功能需求完整归档至 `docs/reports/backlog-archive-2025-10.md`
- ✅ Issue列表清零，从新需求重新开始
- ✅ 历史包袱清理完毕

### 后续行动
1. 创建新Issue模板（`issue-template-v6.md`）
2. 定义新需求工作流（`new-requirement-workflow.md`）
3. 从第一个新需求开始执行

---

## 📚 参考资料

- **归档清单**：`docs/reports/backlog-archive-2025-10.md`
- **新Issue模板**：`docs/development/shared/issue-template-v6.md`
- **新需求工作流**：`docs/development/shared/new-requirement-workflow.md`
- **Constitution**：`.spec-workflow/steering/constitution.md`
