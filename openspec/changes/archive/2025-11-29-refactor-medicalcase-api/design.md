# Design: refactor-medicalcase-api

## Context

### 现状分析
当前医案模块有三个API控制器：
1. **MedicalCaseController** - 聚合根主控制器，包含所有写操作
2. **ConsultationController** - 诊断只读控制器（写操作已标记删除）
3. **PrescriptionsController** - 处方只读控制器（写操作已标记删除）

### 问题
1. ConsultationController和PrescriptionsController中仍保留已删除方法的注释代码
2. 缺少明确的"暂存"和"取消"API端点
3. 权限检查分散在Controller和Service层

### 利益相关者
- 医生用户：需要清晰的医案操作流程
- 前端开发：需要统一的API调用规范
- 运维：需要清晰的审计日志

## Goals / Non-Goals

### Goals
1. 统一所有医案写操作到MedicalCaseController
2. 提供完整的医案生命周期API（创建、暂存、完成、取消、删除）
3. 清理过期代码，保持代码整洁
4. 权限检查逻辑统一到Service层

### Non-Goals
1. 不改变现有的数据库Schema
2. 不改变聚合根设计模式
3. 不增加新的业务功能（仅整理现有功能）

## Decisions

### Decision 1: API端点设计
选择RESTful动词+状态名称的URL设计：

| 操作 | HTTP方法 | 端点 | 说明 |
|------|----------|------|------|
| 创建医案 | POST | /api/v1/medicalcases | 创建新医案 |
| 获取医案 | GET | /api/v1/medicalcases/{id} | 获取医案详情 |
| 暂存医案 | PUT | /api/v1/medicalcases/{id}/draft | 保存为草稿 |
| 完成看诊 | PUT | /api/v1/medicalcases/{id}/complete | 完成并锁定 |
| 取消医案 | PUT | /api/v1/medicalcases/{id}/cancel | 取消医案 |
| 删除医案 | DELETE | /api/v1/medicalcases/{id} | 软删除 |
| 关闭医案 | PUT | /api/v1/medicalcases/{id}/close | 直接关闭 |

**备选方案**：使用PATCH + 状态参数
```
PATCH /api/v1/medicalcases/{id}
{ "status": "Draft" | "Completed" | "Cancelled" }
```
**不采用原因**：不够语义化，难以区分不同操作的业务逻辑

### Decision 2: 权限检查位置
**选择**：权限检查统一在Service层实现

**理由**：
- Service层可被多个入口调用（Controller、Background Job等）
- 权限逻辑与业务逻辑紧密相关
- 便于单元测试

**实现方式**：
```csharp
// Service方法签名包含操作者信息
Task<MedicalCase?> SaveDraftAsync(Guid id, Guid operatorId, bool isAdmin);
Task<MedicalCase?> CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason);
```

### Decision 3: 审计策略
依据现有`medicalcase-edit-modes`规范（EDITMODE-010）：

| 条件 | 需要审计 |
|------|----------|
| Completed状态修改 | 是 |
| 非创建医生修改 | 是 |
| 跨日修改 | 是 |
| 当天本人修改Draft/Active | 否 |

## Risks / Trade-offs

### Risk 1: API兼容性
**风险**：移除注释代码可能影响IDE自动补全
**缓解**：仅移除已明确标记为deleted的代码，保留Swagger文档说明

### Risk 2: 权限检查性能
**风险**：每次请求都进行权限检查可能增加延迟
**缓解**：使用缓存策略，用户角色信息从JWT解析无需数据库查询

### Risk 3: 状态机复杂性
**风险**：多个状态转换端点可能导致状态不一致
**缓解**：在Entity层实现状态机验证，拒绝非法转换

## Migration Plan

### Step 1: 准备阶段
1. 确保所有现有测试通过
2. 备份当前API文档

### Step 2: 清理阶段
1. 移除ConsultationController过期代码
2. 移除PrescriptionsController过期代码
3. 验证编译通过

### Step 3: 扩展阶段
1. 添加SaveDraftAsync到IMedicalCaseService
2. 添加CancelAsync到IMedicalCaseService
3. 实现新端点

### Step 4: 验证阶段
1. 运行全部单元测试
2. 运行集成测试
3. 更新API文档

### Rollback
如需回滚：
1. 恢复git历史版本
2. 清理数据库迁移（如有）

## Open Questions

1. **Q**: 取消操作是否需要二次确认？
   **A**: 待确认UI需求，当前设计支持可选的reason参数

2. **Q**: 暂存是否应该自动保存Consultation和Prescription？
   **A**: 是，暂存操作保存聚合根的完整状态

3. **Q**: 是否需要添加批量操作API？
   **A**: Phase 1不包含批量操作，后续根据需求添加
