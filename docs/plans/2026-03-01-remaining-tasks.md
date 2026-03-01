# 剩余任务清单

**生成日期**: 2026-03-01
**来源**: Sprint 5 剩余任务 (2026-02-28) + Code-vs-PRD 审计报告 (2026-02-28)
**验证基线**: 代码库实际状态逐项核实

---

## 总览

| 优先级 | 总数 | 已完成 | 剩余 |
|--------|------|--------|------|
| CRITICAL | 4 | 4 | 0 |
| HIGH | 4 | 4 | 0 |
| MEDIUM | ~~8~~ 6 | 6 | 0 |
| LOW | ~~2~~ 1 | 1 | 0 |
| **合计** | **~~18~~ 15** | **15** | **0** |

全部 15 项 OPEN 任务已清零 (2026-03-01)。

---

## CRITICAL (4 项) -- 全部完成

| # | ID | 标题 | 模块 | 状态 | 完成依据 |
|---|-----|------|------|------|----------|
| 1 | CODE-01 | CompleteAsync 未验证 TcmDiagnosis 必填 | MedicalCase | **已完成** | MedicalCaseStateService.cs:138-143 |
| 2 | CODE-02 | 编辑已打印医案未重置 IsPrinted | MedicalCase | **已完成** | MedicalCaseCommandService.cs 3 个编辑入口均有重置 |
| 3 | CODE-03 | LoginAsync 未撤销 AutoLoginToken Family | Auth | **已完成** | AuthService.cs:227-247 按 UserId 撤销 |
| 4 | CODE-04 | sysadmin API 层保护不足 | Auth | **已完成** | UsersController.cs:192,285 + SuperAdminOnly 策略 |

---

## HIGH (4 项) -- 全部完成

| # | ID | 标题 | 模块 | 状态 | 完成依据 |
|---|-----|------|------|------|----------|
| 5 | CODE-05/06 | MedicalCase FK 无 Fluent API | Infrastructure | **已完成** | MedicalCaseConfiguration.cs HasOne<Patient/User> 显式配置 |
| 6 | CODE-11 | Herb BatchDelete 无引用检查 | Herbs | **已完成** | HerbService.cs CheckReferenceAsync + FormulaHerbItems 计数 |
| 7 | T5-P3-06 | Desktop 写后缓存失效未调用 | LocalData | **已完成** | 5 个 ViewModel 注入 IDesktopCacheManager |
| 8 | T5-P2-42 | 同步前无网络/Token 检查 | Sync | **已完成** | SyncViewModel.cs ValidatePreConditionsAsync |

---

## MEDIUM (6 项) -- 全部完成

| # | ID | 标题 | 模块 | 状态 | 完成依据 |
|---|-----|------|------|------|----------|
| ~~9~~ | ~~T5-P3-17~~ | ~~登出未清除导航历史~~ | ~~Shell~~ | **已完成** | PerformLogoutAsync 添加 ClearHistory() |
| 10 | T5-P3-19 | Email 编辑缺失 | Shell | **已完成** | AccountSettingsViewModel.cs Email 属性 (注释 T5-P3-19) |
| 11 | T5-P3-03 | 追踪码缺 Severity 关联 | ErrorHandling | **已完成** | ProblemDetailsFactory.cs Severity 字段 (注释 T5-P3-03) |
| 12 | T5-P3-01 | Important 配置缺失无警告 | Configuration | **已完成** | ProductionConfigurationValidator.cs Warning (注释 T5-P3-01) |
| 13 | T5-P3-21 | 状态栏无同步标识 | Shell | **已完成** | MainWindowViewModel.cs 同步状态绑定 |
| 14 | DOC5-04 | Desktop Repository 文档不足 | 文档 | **已完成** | dual-mode.md DataSource 接口架构章节 |
| 15 | DOC5-05 | Sync 跨模块文档缺失 | 文档 | **已完成** | dual-mode.md 端到端调用链章节 |
| ~~16~~ | ~~CODE-07~~ | ~~DefaultRole "Staff" 应为 "Doctor"~~ | ~~Configuration~~ | **已完成** | UserManagementOptions.DefaultRole = "Doctor" |

---

## LOW (1 项) -- 全部完成

| # | ID | 标题 | 模块 | 状态 | 完成依据 |
|---|-----|------|------|------|----------|
| ~~17~~ | ~~DEAD-12~~ | ~~PlaceholderViews Patient 残留~~ | ~~Shell~~ | **已完成** | PlaceholderViews.cs 已删除 |
| 18 | T5-P3-20b | Checksum 计算范围 | Sync | **已完成** | ChecksumHelper.cs 双端 MedicalCase 聚合级哈希 |

---

## 执行批次回顾

```
Batch A (CRITICAL - 业务逻辑): CODE-01, CODE-02, CODE-03, CODE-04         -- Session 4 确认
Batch B (HIGH - 数据完整性):   CODE-05/06, CODE-11, T5-P3-06, T5-P2-42   -- Session 3 完成
Batch C (MEDIUM - 快速修复):   CODE-07, T5-P3-17, T5-P3-03               -- Session 2/4 完成
Batch D (MEDIUM - 功能增强):   T5-P3-19, T5-P3-01, T5-P3-21             -- Session 4 完成
Batch E (文档+清理):           DOC5-04, DOC5-05, DEAD-12, T5-P3-20b      -- Session 4/5 完成
```

---

## 已完成项汇总 (初始验证时确认)

以下项目在初始核实中确认已完成，未列入 15 项剩余任务:

| ID | 标题 | 验证依据 |
|----|------|----------|
| T5-P2-20/22 | 验方导入/历史复制价格实时获取 | HerbListControlViewModel.AddItem() 从 AllHerbs 同步 UnitPrice |
| T5-P2-39 | SyncMetadataDto 字段 | 6 个字段齐全 |
| T5-P2-40 | IgnoreQueryFilters | 已实现 |
| T5-P2-41 | OverwriteConflicts 配置化 | SyncOptions.OverwriteConflicts 已实现 |
| T5-P2-43 | 同步结果汇总 | SyncExecutionResult 完整实现 (4 项计数+错误列表) |
| T5-P3-02 | Token 错误码中文映射 | ClientErrorMessageMapper 638 行，含完整中文映射 |
| T5-P3-04 | 审计日志 365 天 | SecurityOptions.AuditRetentionDays = 365 |
| T5-P3-05 | Server 缓存失效 | CacheInvalidationService 已实现 |
| T5-P3-13 | PatientStatus 复用 CommonStatus | PatientStatus.cs 已删除 |
| T5-P3-14 | A4/A5 打印布局 | 4 个模板 + 纸张感知路由 |
| T5-P3-18 | 模块加载角色粒度 | ApplicationBootstrapper 已实现 |
| T5-P3-20 | Checksum 字段对齐 | SyncMetadataDto.Checksum (SHA256) 已定义 |
| A5-01 | Mock 框架统一 | 0 Moq / 49 NSubstitute |
| A5-05 | [Obsolete] 清理 | 3 处已处理 |
| A5-06 (部分) | FK Fluent API (AutoLoginToken+PrescriptionItem) | 已显式配置 |
| DOC5-03 | BaseReadRepository 文档 | server.md v1.8 已修正 |

---

*文档版本: v2.0 | 最后更新: 2026-03-01*
