# Desktop 测试体系 + 架构改进完整修复计划

> 基于 `desktop-test-audit-2026-08-19.md` + `desktop-architecture-design-eval-2026-08-19.md` 综合整理
> 创建时间：2026-08-19

---

## 一、修复总览

| 批次 | 级别 | 内容 | 工作量 | 状态 |
|------|------|------|--------|------|
| **B1** | P1 | 测试缺口补全（T-01 T-02） | M | ⬜ |
| **B2** | P1 | ADR-0020 Service 错误契约迁移（I-1） | L | ⬜ |
| **B3** | P2 | 测试边界补全 + 弱断言修复（T-04 T-06） | S | ⬜ |
| **B4** | P2 | 报表扩展 + 向导完善（I-3 I-4） | M | ⬜ |
| **B5** | P3 | ReportService 测试 + 文档注记（T-05 I-6） | S | ⬜ |

---

## 二、B1：测试缺口补全（P1）

### T-01：补 3 类聚合根 12 方法单测

**目标类**：
1. `FormulaImportDialogViewModel`（~180 行，0 覆盖）
   - `SearchAsync` — 搜索验方
   - `SelectFormula` — 选择验方
   - `PreviewImport` — 预览导入结果
   - `ImportToPrescription` — 导入到处方

2. `HistoryCopyDialogViewModel`（~160 行，0 覆盖）
   - `LoadHistory` — 加载历史医案
   - `FilterByDateRange` — 按日期筛选
   - `PreviewCopy` — 预览复制内容
   - `CopyToNewCase` — 复制到新医案

3. `MedicalCaseWorkspaceViewModel`（549 行，33% 覆盖）
   - `SaveComplete` — 保存成功回调
   - `SaveFailed` — 保存失败处理
   - `LeaveConfirm` — 离开确认
   - `EditModeStateMachine` 6 状态 × 10 事件（补至 10 场景）

**测试模式**：NSubstitute Mock + FluentAssertions

### T-02：MedicalCaseRepository 双路径单测

**目标**：`RecordPrint/Suspend/UpdateStatus` 的 `null` vs `throw` 双路径

**测试用例**（5 个）：
- `RecordPrint_Success_ReturnsTrue`
- `RecordPrint_NotFound_ReturnsNull`
- `Suspend_Success_ReturnsTrue`
- `Suspend_InProgress_ReturnsNull`
- `UpdateStatus_Conflict_ReturnsNull`

---

## 三、B2：ADR-0020 Service 错误契约迁移（P1）

### 目标

按 ADR-0020 统一 Service 层错误处理：
- **读操作**：返回 `null` 或空集合
- **写操作**：返回 `CommandResult`
- **异常**：仅 `ArgumentException` / `InvalidOperationException` 可抛

### 涉及文件

| 文件 | 当前问题 | 修复 |
|------|---------|------|
| `MedicalCaseLifecycleService.cs` | `SaveAsync` throw + `LoadDetailsAsync` return null 混用 | 统一为 CommandResult |
| `MedicalCaseCommandService.cs` | `SaveAsync` 手写 try/catch | 走 CrudServiceBase.ExecuteAsync |
| `ReportService.cs` | `GetDailyStatsAsync` return null | 保持 null（读操作） |
| `AuditLogService.cs` | `GetAuditLogsAsync` return null | 保持 null（读操作） |

### 验收

- 所有写操作返回 CommandResult
- 所有读操作返回 null/空集合
- 无 throw（除 ArgumentException/InvalidOperationException）
- 新增 CommandResult 单测

---

## 四、B3：测试边界补全 + 弱断言修复（P2）

### T-04：BatchImport 边界测试

**用例**（5 个）：
- `BatchImport_EmptyList_ReturnsValidationFail`
- `BatchImport_Count0_ReturnsValidationFail`
- `BatchImport_DuplicateSkip_ReturnsSkippedCount`
- `BatchImport_DuplicateUpdate_ReturnsUpdatedCount`
- `BatchImport_ConcurrencyConflict_ReturnsRetried`

### T-06：弱断言充实

**目标**：`PatientMasterDetailViewModelTests:286` 2 处 `BeNull` 改为 `Message/Items.Count`

---

## 五、B4：报表扩展 + 向导完善（P2）

### I-3：报表扩展趋势/绩效端点

**问题**：`ReportsHomeView` 仅消费 3/8 端点，趋势/绩效未接线

**修复**：
1. Server 端确认趋势/绩效端点是否已实现
2. Desktop 补 ViewModel 消费
3. 补 UI 绑定

### I-4：首次启动向导 5 步 UI

**问题**：`FirstRunSetupView` 仅框架，5 步未完善

**修复**：
1. 改密 → 诊所设置 → 连接配置 → Admin 创建 → 完成
2. 步骤指示器
3. 验证逻辑

---

## 六、B5：ReportService 测试 + 文档注记（P3）

### T-05：ReportService 补 3 单测

**用例**：
- `GetDailyStatsAsync_ReturnsData`
- `GetDailyStatsAsync_EmptyData_ReturnsZero`
- `GetDailyStatsAsync_Exception_Throws`

### I-6：文档差异注记

- `05-dual-mode.md` 补 FirstRun/Reports 缺口注记
- `02-desktop.md` 补 UI 需求缺口说明

---

## 七、执行策略

按批次串行执行（每个批次完成后验证再进入下一个）：

```
B1（测试补全）→ build 0/0 + 新测试通过
B2（ADR-0020）→ build 0/0 + 测试回归
B3（边界+断言）→ build 0/0 + 新测试通过
B4（报表+向导）→ build 0/0 + 功能验证
B5（测试+文档）→ build 0/0 + 文档同步
```

**每个批次完成后**：
1. `dotnet build --no-incremental` → 0/0
2. `dotnet test` → 无新增失败
3. Git commit + push
