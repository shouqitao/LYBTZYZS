# Proposal: optimize-medicalcase-api

## Summary

优化医案(MedicalCase)模块的API设计，统一端点规范、整合冗余查询、规范返回类型，为后续其他模块提供参考模板。

## Motivation

当前医案API存在以下问题：

1. **查询端点冗余**: 8个查询端点，功能重叠，调用方需要记忆多个端点
2. **返回类型不一致**: `CloseCaseAsync` 返回 `ApiResponse`，其他状态方法返回 `ApiResponse<MedicalCaseDetailDto>`
3. **GetById端点分裂**: `GetById` 和 `GetByIdWithDetails` 两个端点，增加调用复杂度
4. **DTO字段映射冗余**: Client端 `MedicalCaseDetailModel` 手动复制DTO字段

## Goals

1. **端点整合**: 将8个查询端点整合为3个核心端点
2. **返回类型统一**: 所有状态操作返回 `ApiResponse<MedicalCaseDetailDto>`
3. **参数化查询**: 通过查询参数控制行为，而非多端点
4. **文档完善**: 为优化后的API提供完整文档

## Non-Goals

1. 不改变聚合根设计模式（Consultation/Prescription通过MedicalCase管理）
2. 不改变Server端CQRS服务分层
3. 不涉及其他模块的改造（仅作为参考模板）

## Design Overview

### 优化后的端点结构

```
MedicalCase API (优化后)
├── 查询端点 (3个，原8个)
│   ├── GET /api/v1/medicalcases              → 统一列表查询（支持多场景）
│   ├── GET /api/v1/medicalcases/{id}         → 单项查询（合并details）
│   └── GET /api/v1/medicalcases/search       → 高级搜索
├── 命令端点 (6个，不变)
│   ├── POST /api/v1/medicalcases             → 创建
│   ├── PUT /api/v1/medicalcases/{id}         → 聚合保存
│   ├── DELETE /api/v1/medicalcases/{id}      → 删除
│   ├── POST /api/v1/medicalcases/batch-delete → 批量删除
│   ├── PUT /api/v1/medicalcases/{id}/prescription-flag → 处方标记
│   └── PUT /api/v1/medicalcases/{id}/status  → 状态更新
├── 状态端点 (3个，返回类型统一)
│   ├── PUT /api/v1/medicalcases/{id}/draft   → 保存草稿
│   ├── POST /api/v1/medicalcases/{id}/close  → 关闭（返回类型修正）
│   └── POST /api/v1/medicalcases/{id}/cancel → 取消
└── 辅助端点 (2个，不变)
    ├── GET /api/v1/medicalcases/{id}/permissions  → 权限
    └── GET /api/v1/medicalcases/{id}/audit-logs   → 审计日志
```

### 关键变更

| 变更项 | 变更前 | 变更后 |
|--------|--------|--------|
| GetById端点 | 2个（GetById + GetByIdWithDetails） | 1个（参数控制） |
| CloseCaseAsync返回 | `ApiResponse` | `ApiResponse<MedicalCaseDetailDto>` |
| 查询端点数量 | 8个 | 3个（整合） |
| 旧端点处理 | - | 标记Obsolete，保持兼容 |

## Risks and Mitigations

| 风险 | 缓解措施 |
|------|----------|
| 调用方迁移成本 | 旧端点标记Obsolete但保留，渐进式迁移 |
| 测试覆盖不足 | 新增集成测试验证端点行为 |
| 文档不同步 | 同步更新API文档和Swagger注释 |

## Success Criteria

1. 编译通过，无错误
2. 现有单元测试全部通过
3. 新增端点有对应测试用例
4. API文档更新完成
5. 旧端点正确标记Obsolete

## Related

- **依赖规范**: `client-api-conventions` - Client端API接口规范
- **依赖规范**: `dto-architecture` - DTO设计规范
- **参考**: `standardize-desktop-api-layer` - 已完成的API层标准化
