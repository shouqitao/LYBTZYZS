# OpenSpec Proposal: cleanup-infrastructure-dead-code

## 元信息
- **提案ID**: cleanup-infrastructure-dead-code
- **创建日期**: 2025-12-05
- **作者**: Claude Code
- **状态**: Completed
- **优先级**: Medium
- **影响范围**: Server/Core/LYBT.Infrastructure

## 1. 问题陈述

LYBT.Infrastructure项目作为服务端核心基础设施层，经过多次迭代后积累了以下技术债务：

### 1.1 死代码/废弃代码
1. **LogSanitizer.cs** - 标记`[Obsolete]`，已被`SensitiveDataMasker`替代，无任何外部引用
2. **IRepositoryLegacy.cs.deleted** - 残留的删除标记文件（非.cs文件但在代码库中）
3. **SeedDataService.Seed()** - 未被任何代码调用，种子数据功能已迁移到DatabaseInitializationService

### 1.2 冗余代码
1. **ServiceLifetime枚举** (RepositoryServiceCollectionExtensions.cs:137-142) - 与`Microsoft.Extensions.DependencyInjection.ServiceLifetime`完全重复
2. **AddServerRepositories/AddRepositorySupportServices方法** - 方法体为空，无实际功能

### 1.3 位置不当的代码
1. **ValidationHelper.IsValidMedicalCaseStatusTransition** - MedicalCase领域逻辑放在Infrastructure层，违反DDD原则

### 1.4 代码质量问题
1. **ApiErrorCodes** - 定义了70+错误码常量，但仅HerbsController使用了1个，过度设计
2. **ConfigurationExtensions.MapToLegacyMemoryCacheConfig** - 返回空对象，映射逻辑未实现

## 2. 提议的解决方案

### Phase 1: 删除死代码 (低风险)
- 删除 `LogSanitizer.cs`
- 删除 `IRepositoryLegacy.cs.deleted`
- 删除 `SeedDataService.cs` 中未使用的代码（保留SuperAdminId常量和被调用的方法）

### Phase 2: 消除冗余 (低风险)
- 删除自定义`ServiceLifetime`枚举，使用.NET原生枚举
- 删除空方法`AddServerRepositories`和`AddRepositorySupportServices`

### Phase 3: 代码归位 (中风险)
- 将`ValidationHelper.IsValidMedicalCaseStatusTransition`迁移到`LYBT.Module.MedicalCase`
- 更新MedicalCaseStateService的引用

### Phase 4: 代码简化 (低风险)
- 简化`ApiErrorCodes`，仅保留实际使用的错误码
- 修复`MapToLegacyMemoryCacheConfig`的空实现或删除

## 3. 影响分析

### 3.1 受影响的文件
| 文件 | 操作 | 风险 |
|------|------|------|
| Utilities/LogSanitizer.cs | 删除 | 低 |
| Interfaces/IRepositoryLegacy.cs.deleted | 删除 | 无 |
| Data/Seeding/SeedDataService.cs | 重构 | 低 |
| DependencyInjection/RepositoryServiceCollectionExtensions.cs | 修改 | 低 |
| Utilities/ValidationHelper.cs | 迁移 | 中 |
| Web/ApiErrorCodes.cs | 简化 | 低 |
| Configuration/Extensions/ConfigurationExtensions.cs | 修复 | 低 |

### 3.2 依赖关系
- LogSanitizer: 无外部引用 (grep确认)
- IRepositoryLegacy: 无外部引用 (grep确认)
- SeedDataService.Seed(): 无调用点 (grep确认)
- 自定义ServiceLifetime: 仅内部使用
- ValidationHelper: 仅MedicalCaseStateService使用

## 4. 验收标准

1. **CLEAN-001**: 所有标记`[Obsolete]`的代码已删除
2. **CLEAN-002**: 无`.deleted`后缀的残留文件
3. **CLEAN-003**: 无未使用的public方法
4. **CLEAN-004**: Infrastructure层不包含领域特定逻辑
5. **CLEAN-005**: 编译通过，所有测试通过

## 5. 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 删除被间接引用的代码 | 低 | 高 | 编译验证 + 测试覆盖 |
| 迁移ValidationHelper破坏依赖 | 中 | 中 | 分步迁移，先添加后删除 |
| 简化ApiErrorCodes影响前端 | 低 | 中 | 仅删除确认未使用的常量 |

## 6. 实施建议

建议按Phase顺序执行，每个Phase完成后进行编译验证和测试：
1. Phase 1-2 可合并执行（均为低风险删除操作）
2. Phase 3 需单独执行（涉及跨模块迁移）
3. Phase 4 可选执行（代码质量改进，非必需）

## 7. 相关Issue/PR

- 无直接关联的Issue
- 延续 refactor-baseservice-permission OpenSpec 的清理工作
