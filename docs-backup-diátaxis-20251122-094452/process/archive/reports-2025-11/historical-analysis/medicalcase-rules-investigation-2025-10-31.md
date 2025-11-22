# MedicalCaseRules与Service层重复分析报告

**生成日期**: 2025-10-31
**调查范围**: MedicalCaseRules.cs vs MedicalCaseService.cs
**Epic Issue**: #1731 Server端校验体系完善 - Phase 1.1

---

## 📋 执行摘要

**核心发现**：MedicalCaseRules并非完全孤立的代码，而是定义了一些Service中**缺失或未完整实现**的业务规则。

| 规则 | Service实现情况 | 结论 |
|-----|---------------|------|
| CanCreateNewCase | ✅ 完全实现 | 重复逻辑 |
| CanEdit | ⚠️ 部分实现（缺失时间和权限检查）| **关键逻辑缺失** |
| CanDelete | ❌ 未实现 | **完全缺失** |
| CanComplete | ⚠️ 隐式验证 | 部分实现 |

**建议方案**：**集成使用MedicalCaseRules**（选项A），补充Service中缺失的CanEdit和CanDelete业务规则。

---

## 1. 规则对比分析

### 1.1 规则1：CanCreateNewCase（患者同时只能有一个进行中的医案）

**MedicalCaseRules定义** (`MedicalCaseRules.cs:17-20`):
```csharp
public static bool CanCreateNewCase(IEnumerable<MedicalCaseEntity> existingCases)
{
    return !existingCases.Any(c => c.Status == MedicalCaseStatus.Active);
}
```

**Service实现** (`MedicalCaseService.cs:52-61`):
```csharp
// 业务规则验证：BR-001（单患者仅一条未完成病案）
var existingActiveCases = await _repository.GetByPatientIdAsync(patientId);
var activeCase = existingActiveCases.FirstOrDefault(c => c.Status == MedicalCaseStatus.Active);

if (activeCase != null)
{
    _logger.LogWarning("患者已有未完成病案，PatientId: {PatientId}, ActiveCaseId: {CaseId}",
        patientId, activeCase.Id);
    throw new InvalidOperationException($"患者已有未完成病案（ID: {activeCase.Id}），请先完成或取消该病案");
}
```

**结论**：
- ✅ Service中**已实现**相同逻辑
- ⚠️ **重复代码**：逻辑几乎完全相同，应复用Rules中的方法
- 📌 **改进建议**：Service中调用`MedicalCaseRules.CanCreateNewCase`代替直接实现

---

### 1.2 规则2：CanEdit（当天可改、过期锁定机制）

**MedicalCaseRules定义** (`MedicalCaseRules.cs:29-39`):
```csharp
public static bool CanEdit(MedicalCaseEntity medicalCase, Guid currentUserId, bool isAdmin = false)
{
    // 管理员权限
    if (isAdmin) return true;

    // 非创建者无权编辑
    if (medicalCase.DoctorId != currentUserId) return false;

    // 当天创建可编辑
    return medicalCase.CreatedAt.Date == DateTime.Today;
}
```

**Service实现** (`MedicalCaseService.cs:124-129`):
```csharp
// 业务规则验证：BF-002（仅Active状态可编辑）
if (medicalCase.Status != MedicalCaseStatus.Active)
{
    _logger.LogWarning("病案状态不允许编辑，MedicalCaseId: {MedicalCaseId}, Status: {Status}",
        medicalCaseId, medicalCase.Status);
    throw new InvalidOperationException($"病案状态为{medicalCase.Status}，不允许编辑");
}
```

**CanEditAsync辅助方法** (`MedicalCaseService.cs:693-727`):
```csharp
public async Task<CanEditResponse> CanEditAsync(Guid id)
{
    // 仅检查Status == Active
    if (medicalCase.Status != MedicalCaseStatus.Active)
    {
        return new CanEditResponse
        {
            CanEdit = false,
            Reason = $"病案状态为{medicalCase.Status}，仅Active状态可编辑"
        };
    }
    return new CanEditResponse { CanEdit = true, Reason = null };
}
```

**结论**：
- ❌ Service中**缺失关键逻辑**：
  1. **时间检查缺失**：未实现"当天创建可编辑"规则
  2. **权限检查缺失**：未实现"非创建者无权编辑"规则
  3. **管理员权限缺失**：未实现管理员绕过检查
- 🔴 **严重问题**：当前任何用户可以随时编辑任何病案（只要Status == Active）
- 📌 **改进建议**：必须集成CanEdit规则，补充时间和权限验证

---

### 1.3 规则3：CanDelete（删除权限检查）

**MedicalCaseRules定义** (`MedicalCaseRules.cs:48-52`):
```csharp
public static bool CanDelete(MedicalCaseEntity medicalCase, Guid currentUserId, bool isAdmin = false)
{
    // 删除规则与编辑相同：当天创建的可以删除
    return CanEdit(medicalCase, currentUserId, isAdmin);
}
```

**Service实现**：
- ❌ **未找到**删除病案的方法
- ⚠️ 仅有删除处方的方法 (`DeletePrescriptionAsync`)

**结论**：
- ❌ Service中**完全未实现**删除病案功能
- 🔴 **功能缺失**：无法删除病案（可能导致数据堆积）
- 📌 **改进建议**：实现删除病案功能并集成CanDelete规则

---

### 1.4 规则4：CanComplete（完成医案的前置条件）

**MedicalCaseRules定义** (`MedicalCaseRules.cs:59-63`):
```csharp
public static bool CanComplete(MedicalCaseEntity medicalCase)
{
    // 简化逻辑：只有进行中的医案可以完成
    return medicalCase.Status == MedicalCaseStatus.Active;
}
```

**Service实现** (`MedicalCaseService.cs:458-473`):
```csharp
// 业务规则验证：BF-002（三步流程完整性）
if (medicalCase.Consultation?.Step1CompletedAt == null)
{
    _logger.LogWarning("Step1未完成，无法完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
    throw new InvalidOperationException("辨证信息未完成（Step1），无法完成病案");
}

// 如果标记需要开处方，验证处方存在
if (medicalCase.NeedsPrescription)
{
    if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
    {
        _logger.LogWarning("已标记需要开处方但处方不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        throw new InvalidOperationException("已标记需要开处方，但处方不存在，无法完成病案");
    }
}
```

**结论**：
- ⚠️ Service中**隐式验证**了Status（通过三步流程验证）
- ✅ Service实现**更严格**（验证三步流程完整性）
- 📌 **改进建议**：可保留Service现有逻辑，CanComplete规则作为补充验证

---

## 2. 综合验证方法分析

### 2.1 ValidateNewCaseCreation

**MedicalCaseRules定义** (`MedicalCaseRules.cs:83-91`):
```csharp
public static ValidationResult ValidateNewCaseCreation(Guid patientId, IEnumerable<MedicalCaseEntity> existingCases)
{
    if (!CanCreateNewCase(existingCases))
    {
        return ValidationResult.Failure("该患者已有进行中的医案，请先完成现有医案");
    }
    return ValidationResult.Success();
}
```

**Service实现**：
- ✅ 已在`CreateAsync`中实现（抛出异常方式）
- ⚠️ 未使用Rules的`ValidationResult`模式

### 2.2 ValidateCaseUpdate

**MedicalCaseRules定义** (`MedicalCaseRules.cs:100-115`):
```csharp
public static ValidationResult ValidateCaseUpdate(MedicalCaseEntity medicalCase, Guid currentUserId, bool isAdmin = false)
{
    if (!CanEdit(medicalCase, currentUserId, isAdmin))
    {
        if (medicalCase.IsLocked)
        {
            return ValidationResult.Failure("医案已锁定，无法修改");
        }
        else
        {
            return ValidationResult.Failure("无权限修改此医案");
        }
    }
    return ValidationResult.Success();
}
```

**Service实现**：
- ❌ **完全未实现**此验证逻辑
- 🔴 **严重问题**：缺少`IsLocked`字段检查和用户权限验证

---

## 3. 问题严重性评估

| 问题 | 严重性 | 影响范围 | 后果 |
|------|--------|---------|-----|
| **CanEdit规则缺失** | 🔴 高 | 所有编辑操作 | 任何用户可随时编辑任何病案 |
| **CanDelete规则缺失** | 🟡 中 | 删除功能 | 无法删除错误病案 |
| **ValidateCaseUpdate缺失** | 🔴 高 | UpdateConsultationAsync等 | 缺少权限和锁定检查 |
| **重复逻辑** | 🟢 低 | CreateAsync | 代码冗余，维护困难 |

---

## 4. 处理方案对比

### 方案A：集成使用MedicalCaseRules（⭐ 推荐）

**优点**：
- ✅ 补充Service中缺失的CanEdit和CanDelete逻辑
- ✅ 消除重复代码（CanCreateNewCase）
- ✅ 保留已有的业务规则定义
- ✅ 测试覆盖Rules类即可验证所有规则

**实施步骤**：
1. 在Service的`CreateAsync`中调用`MedicalCaseRules.CanCreateNewCase`
2. 在Service的`UpdateConsultationAsync`等方法中调用`MedicalCaseRules.CanEdit`
3. 实现`DeleteAsync`方法，调用`MedicalCaseRules.CanDelete`
4. 补充单元测试验证集成效果

**示例代码**：
```csharp
// CreateAsync中集成
var existingActiveCases = await _repository.GetByPatientIdAsync(patientId);
if (!MedicalCaseRules.CanCreateNewCase(existingActiveCases))
{
    throw new InvalidOperationException("该患者已有进行中的医案，请先完成现有医案");
}

// UpdateConsultationAsync中集成
if (!MedicalCaseRules.CanEdit(medicalCase, currentUserId, isAdmin))
{
    throw new UnauthorizedAccessException("无权限编辑此病案");
}
```

**工作量估算**：2-3小时

---

### 方案B：删除MedicalCaseRules（❌ 不推荐）

**缺点**：
- ❌ 丢失CanEdit和CanDelete关键业务规则
- ❌ 需要在Service中重新实现所有规则
- ❌ 无法解决当前权限检查缺失问题

**结论**：**不应删除**，因为Rules中定义了Service中缺失的关键逻辑。

---

### 方案C：迁移到Validators（⚠️ 部分适用）

**适用性分析**：
- ✅ CanCreateNewCase可迁移到`MedicalCaseCreateDtoValidator`
- ❌ CanEdit需要MedicalCase实体和当前用户，**无法在Validator中实现**
- ❌ CanDelete需要MedicalCase实体和当前用户，**无法在Validator中实现**

**结论**：仅部分规则适合迁移到Validators，不是完整解决方案。

---

## 5. 最终建议

**推荐方案**：**方案A（集成使用MedicalCaseRules）**

**理由**：
1. ✅ 解决Service中缺失的权限和时间检查
2. ✅ 消除重复代码
3. ✅ 保留已有业务规则定义
4. ✅ 最小改动成本（2-3小时）

**实施优先级**：
1. 🔴 **紧急**：集成CanEdit规则（修复权限漏洞）
2. 🔴 **紧急**：实现DeleteAsync并集成CanDelete规则
3. 🟡 **中等**：重构CreateAsync使用CanCreateNewCase（消除重复）
4. 🟢 **可选**：补充CanComplete规则验证

---

## 6. 后续任务清单

### Phase 1.2：实施MedicalCaseRules集成（2-3小时）

- [ ] **Task 1.2.1**：重构CreateAsync调用CanCreateNewCase
- [ ] **Task 1.2.2**：在UpdateConsultationAsync中集成CanEdit
- [ ] **Task 1.2.3**：在SetPrescriptionFlagAsync中集成CanEdit
- [ ] **Task 1.2.4**：在UpdatePrescriptionAsync中集成CanEdit
- [ ] **Task 1.2.5**：实现DeleteAsync方法并集成CanDelete
- [ ] **Task 1.2.6**：在Controller中添加currentUserId参数传递
- [ ] **Task 1.2.7**：编译验证（0 errors, 0 warnings）
- [ ] **Task 1.2.8**：补充单元测试验证Rules集成

### Phase 1.3：创建Auth模块Validators（1-2小时）

（见分析报告主文档）

---

## 7. 附录

### 7.1 相关文件路径

**MedicalCaseRules**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseRules.cs`

**MedicalCaseService**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`

**参考Issue**:
- Epic #1731: Server端校验体系完善

### 7.2 业务规则定义

| 规则ID | 业务描述 | 定义位置 | Service实现 |
|--------|---------|---------|------------|
| BR-001 | 单患者仅一条未完成病案 | MedicalCaseRules.CanCreateNewCase | CreateAsync:52-61 (重复) |
| BR-002 | 当天可改、过期锁定 | MedicalCaseRules.CanEdit | ❌ 缺失 |
| BR-003 | 非创建者无权编辑 | MedicalCaseRules.CanEdit | ❌ 缺失 |
| BR-004 | 仅Active状态可编辑 | Service实现 | UpdateConsultationAsync:124-129 ✅ |
| BR-005 | 删除权限检查 | MedicalCaseRules.CanDelete | ❌ 缺失 |
| BR-006 | 三步流程完整性验证 | Service实现 | CompleteAsync:458-473 ✅ |

---

**报告结束**

**下一步行动**：等待用户确认方案后实施集成工作
