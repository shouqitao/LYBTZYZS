# simplify-desktop-data-layer Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Medium（分阶段实施）
- **预估工作量**: 4-6小时
- **状态**: ✅ 已完成

---

## Phase 1: MedicalCaseRepository扩展 ✅

### 1.1 IMedicalCaseRepository新增方法签名 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseRepository.cs`
- **变更**: 新增4个方法签名
  - `Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)`
  - `Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)`
  - `Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)`
  - `Task<MedicalCaseDetailDto?> SaveDraftAsync(Guid id, ConsultationInputDto? request)`
- **验证**: ✅ 接口编译通过

### 1.2 MedicalCaseRepository实现方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**: 实现4个方法，调用对应的_api方法，添加日志和错误处理
- **验证**: ✅ 编译通过

### 1.3 Phase 1编译验证 ✅
- ✅ 运行 `dotnet build` 确保0错误0警告

---

## Phase 2: 过期属性清理 ✅

### 2.1 删除MedicalCaseItem过期属性 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Items/MedicalCaseItem.cs`
- **变更**:
  - ✅ 删除 `CanStartConsultation` 属性定义
  - ✅ 删除 `CanCreatePrescription` 属性定义
  - ✅ 删除相关 `RaisePropertyChanged` 调用
- **验证**: ✅ 编译通过

### 2.2 更新MedicalCaseItemMapper ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseItemMapper.cs`
- **变更**:
  - ✅ 删除 `[MapperIgnoreTarget(nameof(MedicalCaseItem.CanStartConsultation))]`
  - ✅ 删除 `[MapperIgnoreTarget(nameof(MedicalCaseItem.CanCreatePrescription))]`
  - ✅ 删除 `[MapperIgnoreSource(nameof(MedicalCaseItem.CanStartConsultation))]`
  - ✅ 删除 `[MapperIgnoreSource(nameof(MedicalCaseItem.CanCreatePrescription))]`
- **验证**: ✅ 编译通过

### 2.3 Phase 2编译验证 ✅
- ✅ 运行 `dotnet build` 确保0错误0警告

---

## Phase 3: MedicalCaseService数据访问统一 ✅

### 3.1 移除_api字段 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  - ✅ 删除字段 `private readonly IMedicalCaseApi _api;`
  - ✅ 删除构造函数参数 `IMedicalCaseApi api`
  - ✅ 删除构造函数赋值 `_api = api`
- **验证**: ✅ 编译通过

### 3.2 删除无外部引用的CRUD转发方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**: 删除以下方法
  - ✅ `GetByIdSimpleAsync`
  - ✅ `UpdateSimpleAsync`
  - ✅ `CreateAsync`（简单版）
  - ✅ `GetPagedAsync`（Service版）
  - ✅ `QueryAsync`（Service版）
  - ✅ `DeleteAsync(Guid id)`（简单版）
  - ✅ `SearchAsync`
- **验证**: ✅ 无外部引用，安全删除

### 3.3 Phase 3编译验证 ✅
- ✅ 运行 `dotnet build` 确保0错误0警告

---

## Phase 4: Mapperly克隆实现 ✅

### 4.1 新增MedicalCaseCloneMapper ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Mappers/MedicalCaseCloneMapper.cs`（新建）
- **变更**: 创建Mapperly克隆映射器
- **验证**: ✅ Mapperly源生成器生成代码

### 4.2 替换Clone方法调用 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  - ✅ 新增字段 `private readonly MedicalCaseCloneMapper _cloneMapper = new();`
  - ✅ 替换所有 `CloneMedicalCaseDetail(...)` 调用为 `_cloneMapper.Clone(...)`
- **验证**: ✅ 编译通过

### 4.3 删除手写克隆方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**: 删除以下方法
  - ✅ `CloneMedicalCaseDetail`
  - ✅ `CloneConsultation`
  - ✅ `ClonePrescription`
- **验证**: ✅ 编译通过，无调用残留

### 4.4 Phase 4编译验证 ✅
- ✅ 运行 `dotnet build` 确保0错误0警告
- ✅ 确认Mapperly生成代码

---

## Phase 5: HerbService合并到Repository ✅

### 5.1 IHerbRepository新增包装方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Interfaces/IHerbRepository.cs`
- **变更**: 新增方法签名（在前序提案中已完成）
- **验证**: ✅ 接口编译通过

### 5.2 HerbRepository实现包装方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- **变更**: 实现4个包装方法（在前序提案中已完成）
- **验证**: ✅ 编译通过

### 5.3 更新HerbMasterDetailViewModel ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- **变更**:
  - ✅ 将 `IHerbService _herbService` 改为 `IHerbRepository _herbRepository`
  - ✅ 更新构造函数参数
  - ✅ 更新所有方法调用
- **验证**: ✅ 编译通过

### 5.4 更新FormulaMasterDetailViewModel ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- **变更**:
  - ✅ 将 `IHerbService _herbService` 改为 `IHerbRepository _herbRepository`
  - ✅ 更新构造函数参数
  - ✅ 更新 `LoadAllHerbsAsync` 方法调用
- **验证**: ✅ 编译通过

### 5.5 更新HerbsModule DI注册 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/HerbsModule.cs`
- **变更**: ✅ HerbService已删除，无需DI注册
- **验证**: ✅ DI注册正确

### 5.6 删除HerbService相关文件 ✅
- **文件**:
  - ✅ 删除 `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Services/HerbService.cs`
  - ✅ 删除 `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Interfaces/IHerbService.cs`
- **验证**: ✅ 无编译错误

### 5.7 更新Shell Logger注册 ✅
- **文件**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **变更**:
  - ✅ 删除 `using LYBT.Desktop.Herbs.Services;`
  - ✅ 删除 `RegisterLogger<HerbService>(containerRegistry);`
- **验证**: ✅ 编译通过

### 5.8 Phase 5编译验证 ✅
- ✅ 运行 `dotnet build LYBT.All.sln` 确保0错误0警告

---

## Phase 6: FormulaService精简 ✅

### 6.1 更新ViewModel使用Repository ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- **变更**:
  - ✅ `DeleteItemAsync` 改用 `_formulaRepository.DeleteAsync`
- **验证**: ✅ 编译通过

### 6.2 删除FormulaService CRUD转发方法 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaService.cs`
- **变更**: 删除以下方法
  - ✅ `DeleteAsync(Guid)` - 简化bool版本
  - ✅ `CreateAsync(FormulaInputDto)`
  - ✅ `UpdateAsync(FormulaInputDto)`
  - ✅ `GetPagedAsync`
  - ✅ `GetByIdAsync`
- **保留**: SaveFormulaAsync, CopyFormulaAsync, DeleteFormulaAsync等业务方法
- **验证**: ✅ 编译通过

### 6.3 更新IFormulaService接口 ✅
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Interfaces/IFormulaService.cs`
- **变更**: 删除6.2中删除方法的签名
- **验证**: ✅ 接口与实现一致

### 6.4 Phase 6编译验证 ✅
- ✅ 运行 `dotnet build LYBT.All.sln` 确保0错误0警告

---

## Dependencies

```
Phase 1 ────────────────────────┐
                                │
Phase 2 ────────────────────────┼──> Phase 3 ──> Phase 4 ──> Phase 5 ──> Phase 6
                                │
```

**依赖说明**:
- ✅ Phase 1和Phase 2可并行执行
- ✅ Phase 3依赖Phase 1（需Repository方法可用）
- ✅ Phase 4依赖Phase 3（需移除_api后才能清理Clone）
- ✅ Phase 5依赖Phase 4（核心模块稳定后处理边缘模块）
- ✅ Phase 6依赖Phase 5（按模块顺序处理）

---

## Validation Checklist

### 编译验证
- [x] Desktop解决方案编译通过（0错误0警告）

### 功能验证
- [ ] 医案创建功能正常
- [ ] 医案编辑功能正常
- [ ] 医案暂存功能正常
- [ ] 医案完成功能正常
- [ ] 医案取消功能正常
- [ ] 药材CRUD功能正常
- [ ] 验方保存/复制功能正常

### 代码质量验证
- [x] 无_api直接调用残留（MedicalCaseService）
- [x] 无HerbService引用残留
- [x] 无过期属性引用残留

---

## Notes

- 每个Phase完成后应单独commit，便于问题定位和回滚
- Phase 3是核心变更，需要仔细验证所有方法调用路径
- Mapperly生成代码在obj目录，不需要手动编写

---

**生成时间**: 2026-01-08 11:41
**完成时间**: 2026-01-08
**状态**: ✅ 已完成（编译验证通过）
