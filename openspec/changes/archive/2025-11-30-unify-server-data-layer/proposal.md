# Proposal: unify-server-data-layer

## Summary

统一 Server 端数据层（Code First）架构，消除 Entity、EF Configuration、枚举定义的不一致性，建立标准化的数据层规范。

## Problem Statement

当前数据层存在以下问题：

### 1. DateTime 处理不一致
- `BaseEntity.CreatedAt` 默认 `DateTime.Now`（本地时间）
- 部分字段 `DateTime`，部分 `DateTime?`，无明确规范
- 无 UTC 标准化

### 2. Status 枚举体系混乱
- `CommonStatus`：Patient、User、Herb、Formula、Consultation
- `MedicalCaseStatus`：仅 MedicalCase
- `PrescriptionStatus`：仅 Prescription
- **MedicalCase 同时有 `CaseStatus`（业务状态）和 `Status`（系统状态）**

### 3. EF Configuration 不一致
- `RowVersion` 配置：Patient/User 已配置，MedicalCase 被注释，Herb/Formula 缺失
- Fluent API 与 Data Annotations 混用
- 审计字段配置不统一

### 4. StringLength 不一致
- `Name` 字段：Patient(100)、Herb(50/100冲突)、Formula(100)
- `PinYinCode`：Patient(20)、User(50)、Herb(50)

### 5. 导航属性规范缺失
- `virtual` 关键字使用不一致
- List 初始化方式不一致（`= new()` vs 无初始化）

### 6. 命名规范不一致
- 目录名单复数混用：`Patients`(复数) vs `MedicalCase`(单数) vs `Consultation`(单数)
- 表名与实体命名空间不对应

## Proposed Solution

### Phase 1: BaseEntity 与审计字段统一
- DateTime 改为 UTC（DateTimeKind.Utc）
- 统一 nullable 规范
- 所有 Configuration 配置 RowVersion

### Phase 2: Status 枚举整合
- 评估 MedicalCase 双状态字段必要性
- 建立枚举使用规范文档

### Phase 3: EF Configuration 标准化
- 统一使用 Fluent API（移除冗余 Data Annotations）
- 建立 BaseEntityConfiguration 基类
- 标准化索引命名规范

### Phase 4: StringLength 统一
- 建立字段长度标准表
- 更新 Entity 和 Configuration 保持一致

### Phase 5: 导航属性规范化
- 统一使用 `virtual` 支持 Lazy Loading
- 统一 List 初始化模式

### Phase 6: Seed Data 完善
- 添加开发/测试环境 Seed Data
- 建立 Seed Data 管理规范

### Phase 7: 命名规范统一 (可选)
- 统一目录命名为复数形式 *(可选，影响范围大)*
- 确保 Entity 类名为单数、表名为复数
- 建立命名规范文档 *(推荐)*

## Impact Analysis

| 影响范围 | 说明 |
|----------|------|
| 实体类 | BaseEntity + 8个业务实体 |
| Configuration | 15个配置类 |
| Migration | 需要新 Migration |
| 测试 | 需要更新相关测试 |

## Risks

1. **数据迁移风险**：DateTime UTC 转换可能影响现有数据
2. **并发风险**：恢复 RowVersion 可能导致更新冲突
3. **兼容性风险**：枚举变更可能影响 API 契约

## Success Criteria

- 所有 Entity 继承统一的 BaseEntity
- 所有 Configuration 使用统一模式
- RowVersion 在所有核心实体启用
- Migration 可正常执行
- 所有现有测试通过

## Related Specs

- `dto-architecture` - DTO 统一定义规范
- `global-audit` - 全局审计规范
