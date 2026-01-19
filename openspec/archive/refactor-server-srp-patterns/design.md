# refactor-server-srp-patterns 设计文档

## 概述

基于 [proposal.md](./proposal.md) 的详细技术设计。

## 架构决策

### ADR-1: MedicalCaseController Mapping提取

**状态**: 已采纳

**背景**: MedicalCaseController包含3个内联Mapping方法（155-354行），违反单一职责原则。

**决策**: 迁移到MedicalCaseMapper，使用Mapperly源生成器

**实现**:
```csharp
// MedicalCaseMapper.cs 新增方法

// 方法1: 处方映射（需手动计算价格）
[MapperIgnoreTarget("SingleDosePrice")]
[MapperIgnoreTarget("TotalPrice")]
[MapperIgnoreTarget("TotalWeight")]
[MapperIgnoreTarget("MedicalCaseId")]
public partial PrescriptionDetailDto ToPrescriptionDetailDtoCore(Prescription entity);

public PrescriptionDetailDto MapPrescriptionWithPrice(Prescription entity, Guid medicalCaseId)
{
    var dto = ToPrescriptionDetailDtoCore(entity);
    dto.MedicalCaseId = medicalCaseId;
    dto.SingleDosePrice = entity.Items?.Sum(i => i.UnitPrice * i.Dosage) ?? 0;
    dto.TotalPrice = dto.SingleDosePrice * entity.DosageCount;
    dto.TotalWeight = entity.Items?.Sum(i => i.Dosage) ?? 0;
    return dto;
}

// 方法2: 简化医案映射
public MedicalCaseDetailDto MapToMedicalCaseDto(MedicalCase entity);

// 方法3: 完整医案详情映射
public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity);
```

**后果**:
- 正面: Controller职责单一，Mapping集中管理
- 负面: 需确保计算逻辑一致性

---

### ADR-2: 批量操作优化方案调整

**状态**: 已采纳（方案调整）

**背景**: 原计划创建BatchOperationControllerBase基类，但分析发现：
- MedicalCase仅有BatchDelete，无Status操作
- Patients无ToggleStatus
- 各Controller的Service接口签名不同

**决策**: 不创建BaseController，改为提取共享Helper和扩展方法

**实现**:
```csharp
// BatchOperationHelper.cs
public static class BatchOperationHelper
{
    public static IActionResult ValidateIds(List<Guid>? ids, string entityName)
    {
        if (ids == null || ids.Count == 0)
            return new BadRequestObjectResult($"请至少选择一个{entityName}");
        return null; // 验证通过
    }

    public static IActionResult HandleBatchResult(
        Result<BatchOperationResultDto> result,
        ILogger logger,
        string operationName)
    {
        if (!result.IsSuccess || result.Data == null)
            return new BadRequestObjectResult(result.ErrorMessage ?? "操作失败");

        logger.LogInformation("{Operation}成功: {Message}", operationName, result.Data.Message);
        return new OkObjectResult(result.Data);
    }
}
```

**后果**:
- 正面: 保持Controller灵活性，避免过度抽象
- 负面: 代码复用程度略低于基类方案

---

### ADR-3: Consultation/Prescriptions模块删除

**状态**: 已采纳

**背景**: 验证确认两个模块为死代码：
- 接口仅内部引用
- 无Controller依赖
- MedicalCase通过聚合服务处理所有逻辑

**决策**: 完全删除两个模块

**执行步骤**:
1. 从ServiceCollectionExtensions.cs移除注册（第95, 98行）
2. 从LYBT.Server.sln移除项目引用
3. 删除模块目录

**后果**:
- 正面: 清理死代码，减少维护负担
- 负面: 无（已确认无外部依赖）

---

### ADR-4: FormulaImportExportService提取

**状态**: 已采纳

**背景**: FormulaService(792行)包含Import/Export/Template逻辑(306行)

**决策**: 创建独立的IFormulaImportExportService

**接口设计**:
```csharp
public interface IFormulaImportExportService
{
    /// <summary>
    /// 从结构化数据导入验方
    /// </summary>
    Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(
        List<FormulaImportItemDto> items);

    /// <summary>
    /// 导出验方到Excel
    /// </summary>
    Task<MemoryStream> ExportAsync(FormulaExportOptionsDto options);

    /// <summary>
    /// 生成导入模板
    /// </summary>
    MemoryStream GenerateImportTemplate();
}
```

**依赖关系**:
```
FormulaImportExportService
├── IFormulaRepository (加载/保存验方)
├── ICrossModuleQueryService (药材匹配)
├── FormulaMapper (DTO⟷Entity映射)
└── ILogger
```

**后果**:
- 正面: FormulaService职责单一，Import/Export可独立测试
- 负面: 需更新Controller注入

---

### ADR-5: MedicalCaseCommandService保持现状

**状态**: 已采纳

**背景**: 分析1079行代码后发现：
- 已按职责拆分为Query/Command/State/Audit/Permission
- 1079行主要是业务逻辑复杂度导致
- 包含创建(305行)+处方管理(233行)+诊断(62行)+保存(90行)

**决策**: 不进一步拆分，仅提取重试逻辑为工具方法

**后果**:
- 正面: 避免过度设计
- 负面: 无

## 实现策略

### Phase 1: HIGH优先级 (H1)

#### 1.1 MedicalCaseMapper扩展

**文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMapper.cs`

新增方法:
1. `MapPrescriptionWithPrice(Prescription, Guid)` - 处方映射（含价格计算）
2. `MapToMedicalCaseDto(MedicalCase)` - 简化医案映射
3. `MapToMedicalCaseDetailDto(MedicalCase)` - 完整医案详情映射

#### 1.2 MedicalCaseController重构

**文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`

变更:
1. 注入`MedicalCaseMapper`
2. 替换3个私有方法调用为Mapper调用
3. 删除第155-354行的3个内联方法

### Phase 2: MEDIUM优先级 (M1, M2)

#### 2.1 死代码清理

**步骤**:
1. 编辑`ServiceCollectionExtensions.cs`，移除第95, 98行
2. 编辑`LYBT.Server.sln`，移除项目引用
3. 删除`src/Server/Modules/LYBT.Module.Consultation/`
4. 删除`src/Server/Modules/LYBT.Module.Prescriptions/`

#### 2.2 FormulaImportExportService创建

**新建文件**:
- `Formula/Interfaces/IFormulaImportExportService.cs`
- `Formula/Services/FormulaImportExportService.cs`

**修改文件**:
- `Formula/Interfaces/IFormulaService.cs` - 移除Import/Export方法签名
- `Formula/Services/FormulaService.cs` - 移除Import/Export实现
- `Formula/FormulaModule.cs` - 注册新服务
- `WebAPI/Controllers/FormulasController.cs` - 注入新服务

### Phase 3: LOW优先级 (L1, L2)

#### 3.1 CLAUDE.md文档

为以下模块创建CLAUDE.md:
- LYBT.Module.MedicalCase
- LYBT.Module.Formula
- LYBT.Module.Patients
- LYBT.Module.Herbs
- LYBT.Module.Users
- LYBT.Module.Auth

#### 3.2 Serena记忆

记录Server层SRP架构标准。

## 变更清单

### 新增文件

| 文件路径 | 说明 |
|----------|------|
| `Formula/Interfaces/IFormulaImportExportService.cs` | Import/Export服务接口 |
| `Formula/Services/FormulaImportExportService.cs` | Import/Export服务实现 |
| `MedicalCase/CLAUDE.md` | 模块文档 |
| `Formula/CLAUDE.md` | 模块文档 |
| `Patients/CLAUDE.md` | 模块文档 |
| `Herbs/CLAUDE.md` | 模块文档 |
| `Users/CLAUDE.md` | 模块文档 |
| `Auth/CLAUDE.md` | 模块文档 |

### 修改文件

| 文件路径 | 修改内容 |
|----------|----------|
| `MedicalCase/Mapping/MedicalCaseMapper.cs` | 添加3个Mapping方法 |
| `WebAPI/Controllers/MedicalCaseController.cs` | 移除内联Mapping，注入Mapper |
| `Formula/Interfaces/IFormulaService.cs` | 移除Import/Export方法签名 |
| `Formula/Services/FormulaService.cs` | 移除Import/Export实现 |
| `Formula/FormulaModule.cs` | 注册IFormulaImportExportService |
| `WebAPI/Controllers/FormulasController.cs` | 注入IFormulaImportExportService |
| `WebAPI/Extensions/ServiceCollectionExtensions.cs` | 移除死模块注册 |

### 删除文件

| 文件路径 | 原因 |
|----------|------|
| `LYBT.Module.Consultation/*` | 死代码模块 |
| `LYBT.Module.Prescriptions/*` | 死代码模块 |

## 测试策略

### 编译验证

```bash
dotnet build src/Server/LYBT.Server.sln -c Release --no-restore
```

### API测试

1. MedicalCase CRUD:
   - GET /api/medical-case
   - GET /api/medical-case/{id}
   - POST /api/medical-case
   - PUT /api/medical-case/{id}

2. Formula Import/Export:
   - POST /api/formulas/import
   - GET /api/formulas/export
   - GET /api/formulas/import-template

3. 批量操作:
   - POST /api/formulas/batch-delete
   - POST /api/users/batch-enable

## 回滚计划

如果变更失败:
1. git revert到Phase开始前的commit
2. 恢复ServiceCollectionExtensions.cs的模块注册
3. 恢复删除的模块目录（从git历史）

---

**设计者**: Claude Code
**日期**: 2026-01-19
**状态**: 待审批
