# Proposal: cleanup-obsolete-code

## Summary

清理项目中的废弃代码，包括已标记 `[Obsolete]` 的API端点、未使用的DTO类、过期TODO占位符、以及空实现代码。

## Problem Statement

项目经过多次重构后积累了以下技术债务：

1. **废弃API端点**: 6个已标记 `[Obsolete]` 的Controller端点仍存在于代码库中
2. **完整废弃Controller**: CacheHealthController 整个控制器从未被Client调用
3. **未使用DTO类**: 14个DTO类仅有定义但从未被代码引用
4. **过期TODO占位符**: 部分TODO注释对应的功能已完成或不再需要
5. **代码冗余**: 废弃代码增加维护成本，混淆代码库理解

## Proposed Solution

### Phase 1: 删除废弃API端点

删除以下已标记 `[Obsolete]` 的代码：

| 文件 | 方法 | 行数 | 原因 |
|------|------|------|------|
| CacheHealthController.cs | 整个文件 | ~197 | 运维功能，无Client调用 |
| HerbsController.cs | BatchDeleteHerbs | ~30 | 批量删除已废弃 |
| FormulasController.cs | BatchDeleteFormulas | ~30 | 批量删除已废弃 |
| MedicalCaseController.cs | CompleteMedicalCase | ~25 | 已有PUT /{id}/status替代 |
| UsersController.cs | BatchDeleteUsers | ~30 | 批量删除已废弃 |
| UsersController.cs | ToggleStatus | ~25 | 未被Client使用 |

**预计清理代码量**: ~340行

### Phase 2: 删除未使用DTO类

删除以下仅有定义但从未被代码引用的DTO类：

#### FormulaAnalysisDtos.cs (整个文件删除，6个DTO)
| DTO类名 | 行数 | 用途描述 |
|---------|------|----------|
| FormulaFromTemplateDto | ~12 | 从模板创建验方 |
| FormulaHistoryDto | ~17 | 验方历史记录 |
| FormulaTypeDto | ~7 | 验方类型枚举 |
| FormulaCopyResultDto | ~9 | 验方复制结果 |
| FormulaUsageStatDto | ~10 | 验方使用统计 |
| FormulaEffectivenessDto | ~10 | 验方效果评估 |

#### MedicalCaseDtos.cs (部分删除，4个DTO)
| DTO类名 | 行数 | 删除原因 |
|---------|------|----------|
| CompleteMedicalCaseDto | ~10 | 已有UpdateMedicalCaseStatusDto替代 |
| SuspendMedicalCaseDto | ~10 | 已有UpdateMedicalCaseStatusDto替代 |
| ArchiveMedicalCaseDto | ~14 | 已有UpdateMedicalCaseStatusDto替代 |
| DoctorMedicalCaseStatisticsDto | ~26 | 从未实现该统计功能 |

#### PatientOperationDtos.cs (部分删除，2个DTO)
| DTO类名 | 行数 | 删除原因 |
|---------|------|----------|
| PatientVisitHistoryDto | ~13 | 从未实现该功能 |
| PatientProfileManagementDto | ~28 | 从未实现该功能 |

#### HerbOperationDtos.cs (部分删除，2个DTO)
| DTO类名 | 行数 | 删除原因 |
|---------|------|----------|
| CompatibilitySuggestionDto | ~13 | 未来功能预留，当前未使用 |
| HerbSpecialPriceDto | ~46 | 未来功能预留，当前未使用 |

**预计清理代码量**: ~230行

### Phase 3: 清理过期TODO注释

评估并清理以下TODO注释：

| 文件 | 行号 | TODO内容 | 处理方式 |
|------|------|----------|----------|
| InformationDialogViewModel.cs | 31 | Phase 4C关闭对话框逻辑 | 验证已实现后删除 |
| ClinicalHomeViewModel.cs | 311 | 从服务获取今日统计数据 | 保留(真实待办) |
| MedicalCaseEventCoordinator.cs | 多处 | 定义各种Event | 保留(真实待办) |
| FormulaImportHandler.cs | 39,85 | Issue #1807验方导入 | 保留(有Issue关联) |
| PrescriptionPrintService.cs | 多处 | PRINT-4/5打印集成 | 保留(有Epic关联) |

### Phase 4: 验证清理效果

1. 确保所有测试通过
2. 验证API文档无废弃端点
3. 编译无警告
4. 验证DTO删除不影响序列化

## Impact Analysis

### Benefits
- 减少代码量 ~570行 (340行API + 230行DTO)
- 消除编译器废弃警告
- 简化API接口文档
- 降低维护成本
- 减少DTO类数量14个

### Risks
- **低风险**: 所有删除的API代码都已标记为 `[Obsolete]`
- **无Client依赖**: 经验证无Client调用这些端点
- **DTO未使用**: 经代码分析确认14个DTO从未被引用

### Dependencies
- 无外部依赖

## Success Criteria

1. 所有 `[Obsolete]` 标记的API端点已删除
2. CacheHealthController 整个文件已删除
3. FormulaAnalysisDtos.cs 整个文件已删除
4. 其他3个DTO文件中的未使用类已删除
5. `dotnet build` 无废弃警告
6. 所有单元测试通过
7. 集成测试通过

## Related Resources

- OpenSpec: `webapi-cleanup` - WebAPI清理规范
- OpenSpec: `dto-cleanup` - DTO清理规范
- CHANGELOG: `refactor-webapi-layer` - 之前的WebAPI重构记录
