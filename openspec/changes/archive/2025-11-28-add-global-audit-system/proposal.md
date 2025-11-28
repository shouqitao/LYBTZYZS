# Change: 添加全局统一审计系统

## Why

目前系统存在两套独立的审计实现:
1. **MedicalCaseAuditService**: 针对医案的字段级变更追踪(记录ChangedFields/OldValues/NewValues)
2. **SecurityAuditService**: 针对认证事件的审计(记录EventType/Success/ErrorMessage)

这两套实现缺乏统一架构，且其他业务实体(患者、处方、药材、方剂、用户)尚无审计覆盖。

需要建立全局统一审计系统，实现:
- 统一的审计服务基础架构
- 覆盖所有关键业务实体的变更追踪
- 前后端一致的审计日志查看能力

## What Changes

### 后端
- 创建通用 `IAuditService<TEntity>` 泛型接口
- 创建 `AuditLog` 统一实体(兼容字段级变更和事件级记录)
- 为以下实体添加审计支持:
  - Patient (患者)
  - Prescription (处方)
  - Herb (药材)
  - Formula (方剂)
  - User (用户)
  - Consultation (诊断记录)
- 保留现有 MedicalCaseAuditLog 和 SecurityAuditLog 的兼容性

### 前端
- 创建通用 `AuditLogDialogBase` 基类
- 在各管理界面添加"变更记录"查看功能
- 统一审计日志展示格式

## Impact

- **Affected specs**:
  - 新建 `global-audit` 规范
  - 修改 `medicalcase-lifecycle` (LIFECYCLE-008已实现的审计作为参考实现)

- **Affected code**:
  - 后端: LYBT.Infrastructure (新增通用审计基础设施)
  - 后端: 各Module的Service层 (集成审计调用)
  - 前端: LYBT.Desktop.Infrastructure (通用审计对话框)
  - 前端: 各Desktop Module (添加审计查看入口)
