# Sprint 5 剩余任务清单

**生成日期**: 2026-02-28
**状态**: 待执行
**总计**: 28 项 (原计划 ~46 项，经验证 18 项已完成)

---

## 总览

Sprint 5 已完成 5 个 Batch + 文档归档。原计划剩余约 46 项任务，经逐项验证后发现大量已完成：

| 类别 | 原计划 | 已完成 | 剩余 |
|------|--------|--------|------|
| P2 功能完善 | 7 | 0 | 7 |
| P3 细节修复 | 21 | 6 (Batch 2/4) | 15 |
| A5 架构优化 | 6 | 3 | 3 |
| DOC5 文档补全 | 5 | 2 | 3 |
| D2 设计深化 | 6 | 6 | 0 |
| D5 Desktop 解耦 | 6 | 6 | 0 |
| PRD 修订 | 7 | 7 | 0 |
| **合计** | **~58** | **~30** | **28** |

> 注: 原统计 ~46 项存在分类重叠 (D2/D5 部分指向 A5/DOC5)，实际去重后约 58 项。

---

## 一、P2 功能完善 (7 项)

| # | 编号 | 标题 | 模块 | 关键文件 | 依赖 |
|---|------|------|------|----------|------|
| 1 | T5-P2-20 | 验方导入价格实时获取 | MedicalCases | `PrescriptionService.cs` | Batch 4 完成 |
| 2 | T5-P2-22 | 历史复制价格实时获取 | MedicalCases | `PrescriptionService.cs` | T5-P2-21 (已完成) |
| 3 | T5-P2-39 | SyncMetadataDto 补充缺失字段 | Sync | `SyncMetadataDto.cs` | 无 |
| 4 | T5-P2-40 | GetMetadataAsync 使用 IgnoreQueryFilters | Sync | `SyncService.cs` | 无 |
| 5 | T5-P2-41 | OverwriteConflicts 改为配置项 | Sync | `SyncService.cs`, AppSettings | 无 |
| 6 | T5-P2-42 | 同步前添加网络/Token 检查 | Sync | `SyncService.cs` | 无 |
| 7 | T5-P2-43 | 完善同步结果汇总 | Sync | `SyncResultDto.cs`, `SyncService.cs` | 无 |

**分组建议**:
- **Batch 6a**: T5-P2-20 + T5-P2-22 (医案价格实时获取，同一文件)
- **Batch 6b**: T5-P2-39~43 (同步增强，5 项集中在 Sync 模块)

---

## 二、P3 细节修复 (15 项)

### P3-CFG/ERR/NFR (6 项)

| # | 编号 | 标题 | 模块 | 关键文件 |
|---|------|------|------|----------|
| 8 | T5-P3-01 | Important 配置缺失改为警告 | Configuration | `ConfigurationValidator.cs` |
| 9 | T5-P3-02 | Token 错误码中文消息映射 | ErrorHandling | `ClientErrorMessageMapper.cs` |
| 10 | T5-P3-03 | 追踪码与 Severity 自动关联 | ErrorHandling | `ProblemDetailsFactory.cs` |
| 11 | T5-P3-04 | 审计日志保留 365 天 NFR 确认 | Logging | `AuditLogCleanupService.cs` |
| 12 | T5-P3-05 | Server 端缓存失效映射 | Caching | `CacheInvalidationStrategy.cs` |
| 13 | T5-P3-06 | Desktop 端写后缓存失效 | LocalData | `LocalDataCache.cs` |

### P3-Patient (1 项)

| # | 编号 | 标题 | 模块 | 关键文件 |
|---|------|------|------|----------|
| 14 | T5-P3-13 | PatientStatus 复用 CommonStatus | Patients | `PatientStatus.cs` -> 删除，改用 `CommonStatus` |

### P3-Print (3 项)

| # | 编号 | 标题 | 模块 | 关键文件 |
|---|------|------|------|----------|
| 15 | T5-P3-14 | A4/A5 排版差异处理 | Printing | `PrescriptionPrintService.cs`, Template XAML |
| 16 | T5-P3-15 | 药材名称过长截断 | Printing | `PrescriptionPrintTemplate.xaml` |
| 17 | T5-P3-16 | 空处方打印校验 | Printing | `PrescriptionPrintService.cs` |

### P3-Shell (3 项)

| # | 编号 | 标题 | 模块 | 关键文件 |
|---|------|------|------|----------|
| 18 | T5-P3-17 | 登出时清除导航历史 | Shell | `ShellViewModel.cs` |
| 19 | T5-P3-18 | 模块加载增加角色粒度 | Shell | `ModuleLoadingService.cs` |
| 20 | T5-P3-19 | 账户设置添加 Email 编辑 | Shell | `AccountSettingsView.xaml` + ViewModel |

### P3-Sync (2 项)

| # | 编号 | 标题 | 模块 | 关键文件 |
|---|------|------|------|----------|
| 21 | T5-P3-20 | Checksum 字段类型/长度对齐 | Sync | `SyncMetadataDto.cs`, `SyncMetadata.cs` |
| 22 | T5-P3-21 | 状态栏同步标识实现 | Shell | `StatusBar.xaml` + ViewModel |

**分组建议**:
- **Batch 7a**: T5-P3-01~06 (CFG/ERR/NFR，基础设施层)
- **Batch 7b**: T5-P3-13~16 (Patient + Print)
- **Batch 7c**: T5-P3-17~21 (Shell + Sync)

---

## 三、A5 架构优化 (3 项)

| # | 编号 | 标题 | 范围 | 工作量估算 |
|---|------|------|------|-----------|
| 23 | A5-01 | Mock 框架统一 (Moq) | tests/ (19 个 NSubstitute 文件 -> Moq) | 中 (2-3h) |
| 24 | A5-05 | [Obsolete] 标记清理 | 3 处: AuthEnums / MedicalCaseController / ICrossModuleQueryService | 小 (30min) |
| 25 | A5-06 | 外键关系补充显式 Fluent API | Infrastructure/Data/Configurations/*.cs | 中 (1-2h) |

**已完成 (不再需要)**:
- ~~A5-02~~: MedicalCase 直接引用 -> 评估完毕，决策保留
- ~~A5-03~~: Auth/Users 映射 -> 评估完毕，决策保留
- ~~A5-04~~: 空壳清理 -> 已完成 (Module.Consultation/Prescriptions/Interfaces 已删除)

---

## 四、DOC5 文档补全 (3 项)

| # | 编号 | 标题 | 目标文档 |
|---|------|------|----------|
| 26 | DOC5-03 | BaseReadRepository 设计说明 | `docs/03-architecture/server.md` |
| 27 | DOC5-04 | Desktop Repository 无基类现状说明 | `docs/05-development/desktop.md` |
| 28 | DOC5-05 | Sync 模块跨模块引用文档化 | `docs/03-architecture/server.md` |

**已完成 (不再需要)**:
- ~~DOC5-01~~: Patient Code 字段 -> data-model.md v1.5 已完整记载
- ~~DOC5-02~~: RefreshToken 字段 -> data-model.md v1.5 已完整记载

---

## 已验证完成的类别 (无需再执行)

| 类别 | 原计划 | 验证依据 |
|------|--------|----------|
| PRD 修订 (7 项) | SYS-03~05, FORM-14, NFR-07~09 | 健康检查已实现; PRD v1.6 已修订; NFR PRD v1.4 已修订 |
| D2 设计深化 (6 项) | D2-01~06 | FormulaService 已继承 BaseService; Validator 已迁移; 其余已决策 |
| D5 Desktop 解耦 (6 项) | D5-01~06 | IHerbSearchProvider + IFormulaSearchProvider 接口/实现/注册全部到位 |
| P3 Batch 2/4 (6 项) | T5-P3-07~12 | 已在 Sprint5 Batch 2 和 Batch 4 中完成 |

---

## 推荐执行顺序

```
Batch 6a: T5-P2-20 + T5-P2-22        (医案价格实时获取, 2 项)
Batch 6b: T5-P2-39~43                (同步增强, 5 项)
Batch 7a: T5-P3-01~06                (CFG/ERR/NFR, 6 项)
Batch 7b: T5-P3-13~16                (Patient + Print, 4 项)
Batch 7c: T5-P3-17~21                (Shell + Sync, 5 项)
Batch 8:  A5-01 + A5-05 + A5-06      (架构优化, 3 项)
Batch 9:  DOC5-03~05                  (文档补全, 3 项)
```

**总计**: 7 个 Batch，28 项任务。

---

*生成依据: `docs/plans/2026-02-22-full-sprint-design.md` 原计划 + 代码库逐项验证*
