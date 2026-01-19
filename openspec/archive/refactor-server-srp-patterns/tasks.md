# refactor-server-srp-patterns Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Medium
- **预估工作量**: 1-2天

## Phase 1: HIGH优先级修复

### H1: MedicalCaseController Mapping提取

#### 1.1 MedicalCaseMapper扩展
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMapper.cs`
- **变更**:
  - 添加 `ToPrescriptionDetailDtoCore(Prescription)` 部分方法（Mapperly生成）
  - 添加 `MapPrescriptionWithPrice(Prescription, Guid)` 手动计算价格
  - 添加 `MapToMedicalCaseDto(MedicalCase)` 简化医案映射
  - 添加 `MapToMedicalCaseDetailDto(MedicalCase)` 完整医案详情映射
- **验证**: 编译通过，Mapper方法可调用

#### 1.2 MedicalCaseController重构
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  - 注入 `MedicalCaseMapper`
  - 替换内联Mapping调用为Mapper方法调用
  - 删除第155-354行的3个私有Mapping方法
- **验证**: Controller调用Mapper正常，API行为不变

#### 1.3 编译验证
- 运行 `dotnet build src/Server/LYBT.Server.sln -c Release --no-restore`
- 确保零编译错误

### H2: MedicalCaseCommandService评估（已完成）

**结论**: 经分析，该服务已按职责拆分为5个独立服务（Query/Command/State/Audit/Permission），1079行代码主要是业务复杂度导致，无需进一步拆分。

### H3: 批量操作优化（方案调整）

**调整说明**: 原计划创建BatchOperationControllerBase，但分析发现各Controller的Service接口签名差异大，强制继承会引入不必要复杂度。

#### 3.1 创建BatchOperationHelper（可选）
- **文件**: `src/Server/Services/LYBT.WebAPI/Helpers/BatchOperationHelper.cs`
- **变更**: 创建静态辅助类提供验证和结果处理
- **状态**: 延后，当前重复程度可接受

## Phase 2: MEDIUM优先级修复

### M1: Consultation/Prescriptions模块清理

#### 4.1 移除服务注册
- **文件**: `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`
- **变更**: 删除第95行 `AddConsultationModule` 和第98行 `AddPrescriptionsModule`
- **验证**: 编译通过

#### 4.2 更新解决方案文件
- **文件**: `src/Server/LYBT.Server.sln`
- **变更**: 移除Consultation和Prescriptions项目引用
- **验证**: 解决方案加载正常

#### 4.3 删除模块目录
- **目录**: `src/Server/Modules/LYBT.Module.Consultation/`
- **目录**: `src/Server/Modules/LYBT.Module.Prescriptions/`
- **变更**: 完全删除两个目录
- **验证**: 编译通过，无残留引用

#### 4.4 编译验证
- 运行 `dotnet build src/Server/LYBT.Server.sln -c Release --no-restore`
- 确保零编译错误

### M2: FormulaImportExportService提取

#### 5.1 创建接口
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaImportExportService.cs`
- **变更**: 定义ImportFromDataAsync/ExportAsync/GenerateImportTemplate方法
- **验证**: 接口定义完整

#### 5.2 创建服务实现
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaImportExportService.cs`
- **变更**: 从FormulaService迁移Import/Export实现（约306行）
- **依赖**: IFormulaRepository, ICrossModuleQueryService, FormulaMapper, ILogger
- **验证**: 服务实现完整

#### 5.3 更新FormulaService
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- **变更**: 移除Import/Export方法（保留接口签名的委托调用或直接移除）
- **验证**: FormulaService职责单一

#### 5.4 更新接口定义
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`
- **变更**: 移除Import/Export方法签名
- **验证**: 接口与实现一致

#### 5.5 注册服务
- **文件**: `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs`
- **变更**: 注册IFormulaImportExportService
- **验证**: DI容器配置正确

#### 5.6 更新Controller
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- **变更**: 注入IFormulaImportExportService，更新Import/Export端点调用
- **验证**: API行为不变

#### 5.7 编译验证
- 运行 `dotnet build src/Server/LYBT.Server.sln -c Release --no-restore`
- 确保零编译错误

## Phase 3: LOW优先级改进

### L1: Server模块CLAUDE.md

#### 7.1-7.6 创建模块文档
- **文件**: 为以下模块创建CLAUDE.md
  - `src/Server/Modules/LYBT.Module.MedicalCase/CLAUDE.md`
  - `src/Server/Modules/LYBT.Module.Formula/CLAUDE.md`
  - `src/Server/Modules/LYBT.Module.Patients/CLAUDE.md`
  - `src/Server/Modules/LYBT.Module.Herbs/CLAUDE.md`
  - `src/Server/Modules/LYBT.Module.Users/CLAUDE.md`
  - `src/Server/Modules/LYBT.Module.Auth/CLAUDE.md`
- **验证**: 文档内容符合模块实际结构

### L2: Serena记忆更新

#### 8.1 创建Server层SRP架构记忆
- **记忆名**: `server-srp-architecture`
- **内容**: 拆分模式、阈值标准、架构决策
- **验证**: 记忆可查询

## Dependencies

```
Phase 1 (H1) ────────────────────────┐
                                     │
Phase 2 (M1, M2) ────────────────────┼──> Phase 3 (L1, L2)
                                     │
（各Phase内任务独立，可并行）          │
```

**说明**:
- Phase 1和Phase 2可并行执行
- Phase 3依赖Phase 1/2完成后执行
- M1和M2相互独立，可并行

## Validation Checklist

- [ ] Server解决方案编译通过
- [ ] MedicalCase CRUD功能正常
- [ ] Formula导入导出功能正常
- [ ] 无运行时错误
- [ ] 代码审查通过

## Rollback Plan

如果变更失败:
1. `git revert` 到Phase开始前的commit
2. 恢复ServiceCollectionExtensions.cs的模块注册
3. 从git历史恢复删除的模块目录

---

**生成时间**: 2026-01-19
**状态**: 完整版（设计阶段细化完成）
