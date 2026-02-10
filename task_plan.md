# Task Plan: 文档-代码对齐补全 + EntityAudit 技术债务清理

## Goal
1. 补全代码已有但文档缺失的模块 (CardReader/Health/Diagnostics) -- COMPLETE
2. 全量清除通用实体审计 (EntityAudit) 代码、DTO、测试和 DI 注册 -- COMPLETE

## Current Phase
ALL PHASES COMPLETE

## Design Reference
- 设计文档: `docs/plans/2026-02-10-doc-code-alignment-design.md`
- 文档补全计划: `docs/plans/2026-02-10-doc-code-alignment-plan.md` (7 Tasks)
- 审计清理计划: `docs/plans/2026-02-10-remove-entity-audit-plan.md` (8 Tasks)

---

## Phases

### Phase 1: 文档创建 (Task 1-3)
- [x] Task 1: 创建 card-reader.md 需求 + 修正数据模型
- [x] Task 2: 创建 health.md API参考
- [x] Task 3: 创建 diagnostics.md API参考
- **Status:** complete

### Phase 2: 架构+运维文档 (Task 4-5)
- [x] Task 4: desktop.md 新增 Controls/Dialogs/CardReader 章节
- [x] Task 5: 拆分 06-operations/ 三文件
- **Status:** complete

### Phase 3: 索引更新与清理 (Task 6-7)
- [x] Task 6: 更新 README 索引
- [x] Task 7: 删除残留 + 全量验证
- **Status:** complete

### Phase 4: EntityAudit Server 端清理 (Task A1-A3)
- [x] Task A1: 删除 EntityAuditLog, IAuditService, EntityAuditService, Configuration; 修改 AppDbContext
- [x] Task A2: 4 个 Module 移除 DI 注册 + using
- [x] Task A3: 删除 EntityAuditController, EntityAuditLogDto; 修改 appsettings.json, UserManagementOptions
- **Status:** complete

### Phase 5: EntityAudit Desktop/测试清理 (Task A4-A5)
- [x] Task A4: 删除 5 文件 (Dialog+ViewModel+Handlers); 修改 App.xaml.cs + 4 个 MasterDetailViewModel + UsersModule
- [x] Task A5: 删除 4 测试文件; 修改 ArchTests + AggregateRootArchTests 排除列表 + DesktopE2ETestFixture
- **Status:** complete

### Phase 6: Migration + 文档 + 验证 (Task A6-A8)
- [x] Task A6: 生成 RemoveEntityAuditLogsTable migration (仅 DropTable)
- [x] Task A7: 更新 server.md, api-reference/README, medical-cases.md, Shell/README
- [x] Task A8: 全量编译 0 errors, 全量测试 1431 passed 0 failed, 残留搜索仅 migration 历史
- **Status:** complete

---

## Decisions Made

| Decision | Rationale | Date |
|----------|-----------|------|
| EntityAudit 为技术债务，不补文档 | 审计功能未正式开发，代码待清除 | 2026-02-10 |
| 保留 AuditOperationType 枚举 | 被 MedicalCaseAudit 使用 | 2026-02-10 |
| 保留 MedicalCaseAudit + SecurityAudit | 正式功能，不清除 | 2026-02-10 |
| 不修改历史 migration | 创建新 migration 删表 | 2026-02-10 |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| DesktopE2ETestFixture 残留 IUserAuditHandler 注册 | 1 | 计划未覆盖，编译发现后补充修复 |
| Desktop UsersModule 残留 DI 注册 | 1 | 计划未覆盖 Desktop Module，grep 发现后补充修复 |

---
**Started**: 2026-02-10
**Last Updated**: 2026-02-10 (ALL 15 TASKS COMPLETE - 7 文档 + 8 审计清理)
