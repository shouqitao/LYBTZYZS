# Proposal: refactor-service-layer

## Summary

重构Server端Service层，统一返回值类型、引入基类模式、规范化验证和错误处理，提升代码一致性和可维护性。

## Problem Statement

当前Service层存在以下问题：

### 1. 两种Result类型不一致
- `ServiceResult<T>`（`LYBT.Shared.Models.Contracts.Common`）- AuthService等使用
- `Result<T>`（`LYBT.Shared.Models.Common`）- HerbService等使用
- 两种类型API不同，增加开发者心智负担

### 2. 没有Service基类
- 14个Service各自独立实现
- 104个catch块重复相同的错误处理逻辑
- 没有统一的日志记录模式

### 3. God Class问题
- **MedicalCaseService**: 1465行、36个方法
- 违反单一职责原则（SRP）
- 难以测试和维护

### 4. 验证逻辑不一致
- 仅3个Service使用FluentValidation（Patients、Users、Herbs）
- 其他Service手工验证，易遗漏

### 5. 构造函数参数不统一
- 不同Service的依赖注入顺序不同
- 没有标准化的构造函数模式

## Proposed Solution

采用渐进式重构策略，分为以下阶段：

### Phase 1: 统一返回值类型
- 废弃`ServiceResult<T>`，统一使用`Result<T>`
- 迁移AuthService等到新类型
- 移除旧的ServiceResult类

### Phase 2: 引入Service基类
- 创建`BaseService<T>`提供公共功能
- 统一错误处理模式（包装catch逻辑）
- 标准化构造函数模式

### Phase 3: MedicalCaseService直接拆分
- 删除IMedicalCaseService和MedicalCaseService
- 创建职责单一的小Service（Command/Query/State）
- 同步更新Controller注入（不保留兼容性）

### Phase 4: 验证统一化
- 为所有Service添加FluentValidation
- 移除手工验证代码
- 统一验证错误返回格式

## Impact Analysis

### 影响范围
| 模块 | 影响类型 | 文件数 |
|------|---------|--------|
| Auth | Result类型迁移 | 4 |
| MedicalCase | 拆分重构 | 6+ |
| Patients/Users/Herbs | 基类迁移 | 3 |
| Formula/Consultation/Prescription | 验证添加 | 3 |
| Shared | 移除ServiceResult | 1 |

### 风险评估
- **低风险**: Phase 1-2（类型替换、基类提取）
- **中风险**: Phase 3（MedicalCaseService直接拆分，需同步更新Controller和测试）
- **低风险**: Phase 4（添加验证）

### 回归测试策略
- 每个Phase完成后运行完整测试套件
- Phase 3需要重写Service测试和更新集成测试

## 设计原则

本次重构遵循以下原则：

1. **不考虑向后兼容** - 直接按最佳实践重构，不保留旧接口
2. **消除两义性** - 统一Result类型，统一验证模式
3. **单一职责** - 拆分God Class为职责明确的小Service
4. **测试同步更新** - 重构Service同时重构测试

## Success Criteria

- [ ] 统一使用`Result<T>`返回值类型
- [ ] 所有Service继承`BaseService`基类
- [ ] MedicalCaseService拆分为多个职责明确的子服务
- [ ] 所有Service使用FluentValidation
- [ ] 编译通过，测试全绿
- [ ] 代码行数减少（消除重复）

## Related Items

- **Specs**: data-layer-conventions, repository-patterns
- **Prior Work**: refactor-repository-layer（已完成）
