# v1.0 实现任务清单

> 基线：2026-06-13 代码评估。系统 14/15 模块 COMPLETE，1 模块 MOSTLY COMPLETE（Printing）。
> 本清单仅列出已知缺口和待修复项，非全量开发计划。

## 优先级定义

| 级别 | 含义 | SLA |
|------|------|-----|
| P0 | 架构违规/安全缺陷 | 必须在发布前修复 |
| P1 | 功能缺口（PRD 明确要求） | 当前迭代 |
| P2 | 代码质量/技术债 | 下一迭代 |
| P3 | 增强优化 | 可延后 |

---

## P0 — 架构违规

### TASK-01: RegistrationsController 直接注入 AppDbContext
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:28`
- **违规**: 架构规则 P10 — Service 层不得直接注入 AppDbContext
- **修复**: 提取 RegistrationService，Controller 仅调用 Service
- **影响范围**: RegistrationsController + 新建 IRegistrationService/RegistrationService
- **测试**: 现有 Registration 测试应通过（集成测试不依赖内部实现）

### TASK-02: ConfigurationController 使用字符串角色而非 Policy
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs:13`
- **违规**: `[Authorize(Roles = "Admin")]` 应改为 `[Authorize(Policy = PolicyConstants.SuperAdminOnly)]`
- **修复**: 替换为 Policy 常量，确保只有 SuperAdmin 可修改配置
- **影响范围**: ConfigurationController 仅
- **测试**: Configuration 现有测试需更新授权断言

---

## P1 — 功能缺口

### TASK-03: FeatureToggleOptions 未在 Desktop 业务代码中使用
- **定义**: `src/Shared/LYBT.Shared.Configuration/Options/Client/FeatureToggleOptions.cs`
- **现状**: 14 个功能开关中仅 CardReaderEnabled 被检查，其余 13 个从未被业务代码引用
- **PRD 参考**: 11-configuration.md §B2 功能开关
- **修复**: 在各模块 Service/ViewModel 中添加 FeatureToggle 检查
- **涉及开关**:
  - `EnablePatientManagement` → PatientMasterDetailViewModel
  - `EnableHerbManagement` → HerbMasterDetailViewModel
  - `EnableFormulaManagement` → FormulaMasterDetailViewModel
  - `EnableMedicalCaseManagement` → MedicalCaseMasterDetailViewModel
  - `EnableRegistrationManagement` → RegistrationListViewModel
  - `EnablePrinting` → PrescriptionPrintHandler
  - `EnableSync` → SyncViewModel
  - `EnableAuditLog` → AuditLogViewer
  - `EnableDataExport` → 各模块导出功能
- **测试**: 每个开关需至少一个 Unit Test 验证禁用行为

### TASK-04: MedicalCase 缺少 RestoreAsync
- **现状**: Patient/Herb/Formula/User 都有 RestoreAsync，MedicalCase 没有
- **PRD 参考**: 07-medical-cases.md §B4 权限模型 — 未明确要求 restore，但与其他软删除实体不一致
- **评估**: MedicalCase 软删除后通过 RollbackRegistration 回滚挂号，restore 需考虑反向联动
- **建议**: 如果不需要 restore 功能，在 PRD 中明确标注"MedicalCase 软删除不可恢复"
- **如果实现**: 需要恢复关联 Registration + 检查 PrintLog 完整性

### TASK-05: 桌面端 Mapper 不一致
- **发现**: Desktop PatientMapper 未被使用；MedicalCaseMapper 注册为 Singleton 但 Service 用 `new()`
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mapping/PatientMapper.cs` (unused)
  - Server MedicalCaseMapper DI 注册 vs 实际使用不一致
- **修复**: 清理未使用的 Mapper 或改为使用；统一 DI 注册模式

### TASK-06: LocalData Mapper 使用 Both 策略（与其他 Mapper 不一致）
- **发现**: 所有 Server/Desktop Mapper 使用 `RequiredMappingStrategy.Target`，LocalData Mapper 使用默认 `Both`
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mapping/` 下所有 Mapper
- **风险**: Both 策略可能在源属性变更时意外覆盖
- **修复**: 统一为 Target 策略 + 显式映射

---

## P2 — 代码质量

### TASK-07: SyncService CS8602 可空引用警告
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/SyncService.cs:613`
- **修复**: 添加 null 检查或使用 null-forgiving operator

### TASK-08: API 文档与实现不一致项
- **来源**: docs/04-api-reference/ 审计发现
- **不一致项**:
  1. `import-template` 端点文档标注 `[AllowAnonymous]` 但代码继承类级 `[Authorize]`
  2. `POST /import` (Excel) 文档中存在但代码只实现 `batch-import` (JSON)
  3. `toggle-status` 文档标注 AdminOnly 但代码继承更宽泛的类级 Policy
  4. 部分创建端点返回 201 但文档写 200
- **修复**: 以代码为准更新文档，或以文档为准补全实现

### TASK-09: Desktop 模块 README 引用过时内容
- **现状**: 部分 Desktop 模块 README 仍引用 DataSource/SQLite 架构
- **修复**: 更新为当前 LocalDB + Repository 架构

### TASK-10: 未使用的桌面 PatientMapper
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mapping/PatientMapper.cs`
- **现状**: 已创建但从未实例化
- **修复**: 删除或在 PatientService 中启用

---

## P3 — 增强优化

### TASK-11: 打印模板扩展
- **现状**: 仅处方打印模板（A5/A4），`IPrintService<T>` 泛型设计已支持扩展
- **可选扩展**: 诊断报告、患者摘要、医案完整打印
- **优先级**: 低（PRD 未明确要求其他模板类型）

### TASK-12: API 版本化准备
- **现状**: URL 段版本 /api/v1/，无版本协商机制
- **建议**: 为 v2 做准备时添加版本中间件
- **优先级**: 低（当前仅 v1）

### TASK-13: 日志归档策略
- **现状**: 365 天保留，无归档/压缩
- **建议**: 添加 LogCleanupService 归档能力（按月压缩历史日志）
- **优先级**: 低

---

## 工作量估算

| 任务 | 优先级 | 预估工时 | 模块 |
|------|--------|----------|------|
| TASK-01 | P0 | 2-3h | Registration |
| TASK-02 | P0 | 0.5h | Configuration |
| TASK-03 | P1 | 4-6h | 全局 |
| TASK-04 | P1 | 2-4h 或文档决策 | MedicalCase |
| TASK-05 | P1 | 1-2h | Desktop 全局 |
| TASK-06 | P1 | 1-2h | LocalData |
| TASK-07 | P2 | 0.5h | Sync |
| TASK-08 | P2 | 2-3h | API Docs |
| TASK-09 | P2 | 1h | Desktop |
| TASK-10 | P2 | 0.5h | Patients |
| TASK-11 | P3 | 4-8h | Printing |
| TASK-12 | P3 | 2-3h | Server |
| TASK-13 | P3 | 1-2h | Logging |

**P0 总计**: ~3h | **P1 总计**: ~8-14h | **P2 总计**: ~4-5h

---

## 变更记录

| 日期 | 变更 | 作者 |
|------|------|------|
| 2026-06-13 | 初始版本 — 基于 PRD 100% + 架构文档完整评估 | AI Assistant |
