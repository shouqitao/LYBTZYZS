# Issue #1608 API Gap分析报告

**生成时间**: 2025-10-25
**分析范围**: Issue #1606删除Repository后，MedicalCase聚合根API的缺失方法
**影响范围**: Issue #1608无法完成，6个Prescriptions模块ViewModel依赖缺失的API

---

## 📋 执行摘要

Issue #1606删除了`IPrescriptionRepository`及其实现，但**未在MedicalCase聚合根API中补全对应的Write方法**，导致Prescription的创建和删除功能无法通过聚合根实现。

### 缺失的API方法

| 方法 | 原位置 | 应迁移到 | 当前状态 |
|-----|-------|---------|---------|
| `CreatePrescriptionAsync` | IPrescriptionRepository | IMedicalCaseService | ❌ 缺失 |
| `DeletePrescriptionAsync` | IPrescriptionRepository | IMedicalCaseService | ❌ 缺失 |
| `UpdatePrescriptionAsync` | IPrescriptionRepository | IMedicalCaseService | ✅ 已存在 |

### 影响的ViewModel（Issue #1608）

| ViewModel | 使用CreateAsync | 使用DeleteAsync | 阻塞状态 |
|-----------|----------------|----------------|---------|
| PrescriptionCommandHandler | ✅ (Line 171) | ✅ (Line 222, 440) | ❌ 无法重构 |
| PrescriptionDataManager | ✅ (Line 226) | ❌ | ❌ 无法重构 |
| PrescriptionManagementViewModel | ❌ | ✅ (Line 381) | ❌ 无法重构 |
| PrescriptionEditorDialogViewModel | ❌ | ❌ | ✅ 仅Update，可重构 |
| PrescriptionsMainViewModel | ❌ | ❌ | ✅ Read-only，可重构 |
| PrescriptionViewModel | ❌ | ❌ | ✅ Read-only，可重构 |

---

## 🔍 详细分析

### 1. CreatePrescriptionAsync 缺失

**原方法签名**（已删除）：
```csharp
// IPrescriptionRepository
Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto);
```

**当前Workaround**：
```csharp
// IMedicalCaseService (Line 60-62)
Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
    MedicalCaseCreateDto caseDto,
    ConsultationCreateDto consultationDto,
    PrescriptionCreateDto? prescriptionDto = null);
```

**问题**：
- `CreateWithDetailsAsync`仅适用于**创建新MedicalCase时附带创建Prescription**
- 无法支持**为已存在的MedicalCase添加Prescription**的场景
- 调用位置：
  - `PrescriptionCommandHandler.cs:171`
  - `PrescriptionDataManager.cs:226`

**推荐新API**：
```csharp
/// <summary>
/// 为已存在的医案创建处方（Issue #1608补充）
/// </summary>
/// <param name="medicalCaseId">医案ID</param>
/// <param name="dto">处方创建信息</param>
/// <returns>创建的处方信息</returns>
Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
    Guid medicalCaseId,
    PrescriptionCreateDto dto);
```

---

### 2. DeletePrescriptionAsync 缺失

**原方法签名**（已删除）：
```csharp
// IPrescriptionRepository
Task<bool> DeleteAsync(Guid prescriptionId);
```

**当前Workaround**：
```csharp
// IMedicalCaseService (Line 37)
Task<ServiceResult> DeleteAsync(Guid id); // 删除整个MedicalCase
```

**问题**：
- 当前API只能删除**整个MedicalCase聚合根**
- 无法单独删除Prescription（保留MedicalCase和Consultation）
- 根据架构讨论文档（`medicalcase-consultation-prescription-enhancement-discussion.md` A2决策）：
  - 用户应该可以选择是否删除Prescription
  - 删除Prescription应该是**独立操作**，不应删除整个MedicalCase
- 调用位置：
  - `PrescriptionCommandHandler.cs:222, 440`
  - `PrescriptionManagementViewModel.cs:381`

**推荐新API**：
```csharp
/// <summary>
/// 删除医案的处方（Issue #1608补充）
/// 根据A2决策：支持单独删除Prescription，保留MedicalCase和Consultation
/// </summary>
/// <param name="medicalCaseId">医案ID</param>
/// <returns>删除结果</returns>
Task<ServiceResult> DeletePrescriptionAsync(Guid medicalCaseId);
```

---

### 3. UpdatePrescriptionAsync 已存在 ✅

**现有方法**（`IMedicalCaseService.cs:83`）：
```csharp
Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(
    Guid medicalCaseId,
    PrescriptionUpdateDto dto);
```

**状态**：✅ 已实现，可直接使用。

---

## 🎯 实施计划

### Phase 1: Server端API补全（必需）

**文件修改**：
1. `src/Server/Core/LYBT.Server.Interfaces/Services/IMedicalCaseService.cs`
   - 添加 `CreatePrescriptionAsync` 方法声明
   - 添加 `DeletePrescriptionAsync` 方法声明

2. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
   - 实现 `CreatePrescriptionAsync` 方法
   - 实现 `DeletePrescriptionAsync` 方法
   - 验证聚合根一致性规则

3. `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/MedicalCaseController.cs`
   - 添加 `POST /api/v1/medicalcases/{id}/prescription` 端点
   - 添加 `DELETE /api/v1/medicalcases/{id}/prescription` 端点

**业务规则验证**：
- ✅ CreatePrescription前检查Consultation是否已存在
- ✅ CreatePrescription前检查Prescription是否已存在（避免重复）
- ✅ DeletePrescription时保留MedicalCase和Consultation
- ✅ 验证MedicalCase状态（Draft/Active/Completed/Archived）

---

### Phase 2: Client端API同步（必需）

**文件修改**：
1. `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
   - 添加 `CreatePrescriptionAsync` Refit方法
   - 添加 `DeletePrescriptionAsync` Refit方法

2. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseRepository.cs`
   - 添加 `CreatePrescriptionAsync` 方法声明

3. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
   - 实现 `CreatePrescriptionAsync` 方法（调用API）
   - 实现 `DeletePrescriptionAsync` 方法（调用API）

---

### Phase 3: Issue #1608 ViewModel重构（依赖Phase 1+2）

**重构顺序**（按复杂度）：
1. ✅ PrescriptionsMainViewModel（Read-only，无依赖）
2. ✅ PrescriptionViewModel（Read-only，无依赖）
3. ✅ PrescriptionEditorDialogViewModel（仅Update，依赖已存在）
4. ⏳ PrescriptionDataManager（依赖CreatePrescriptionAsync）
5. ⏳ PrescriptionManagementViewModel（依赖DeletePrescriptionAsync）
6. ⏳ PrescriptionCommandHandler（依赖Create+Delete）

---

## ✅ 验收标准

### Server端验证
- [ ] `IMedicalCaseService` 新增2个方法声明
- [ ] `MedicalCaseService` 实现2个方法
- [ ] `MedicalCaseController` 新增2个API端点
- [ ] Swagger文档显示新端点
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试覆盖新方法

### Client端验证
- [ ] `IMedicalCaseApi` 新增2个Refit方法
- [ ] `IMedicalCaseRepository` 新增2个方法声明
- [ ] `MedicalCaseRepository` 实现2个方法
- [ ] 编译通过：0 errors, 0 warnings

### 集成验证
- [ ] 启动Server端（HTTP 5001）
- [ ] 启动Client端
- [ ] 测试创建Prescription功能
- [ ] 测试删除Prescription功能（保留MedicalCase）
- [ ] 验证Swagger API可正常调用

---

## 📚 参考资料

**架构文档**：
- `docs/explanation/architecture/shared/medicalcase-consultation-prescription-enhancement-discussion.md`
  - A2决策：Prescription删除策略（软删除推荐，物理删除可选）
  - A5决策：严格1:1 Consultation:Prescription关系
  - A6决策：三表共享主键设计（长期Epic）

**代码参考**：
- `UpdateConsultationAsync`: 已完成的聚合根Write方法示例
- `UpdatePrescriptionAsync`: 已完成的Prescription更新方法

**相关Issue**：
- Issue #1606: Server/Client API不同步修复（删除Repository）
- Issue #1607: Consultation模块重构（已完成）
- Issue #1608: Prescriptions模块重构（当前阻塞）

---

## 🔄 后续优化建议

**长期优化**（可延后到MVP完成后）：
1. **批量操作API**：`BatchDeletePrescriptionsAsync`
2. **Prescription状态管理**：Draft/Active/Cancelled/Archived
3. **历史版本追踪**：Prescription修改历史记录
4. **权限控制**：谁可以删除已完成的Prescription

---

**生成工具**: Claude Code
**报告版本**: v1.0
**下次更新**: Phase 1实施后
