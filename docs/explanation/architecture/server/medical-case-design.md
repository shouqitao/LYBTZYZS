# LYBT.Module.MedicalCase - Server端医案管理模块架构设计

## 文档元信息

| 属性 | 值 |
|------|-----|
| 文档类型 | 架构设计文档 |
| 目标读者 | Server端开发人员、架构师、技术负责人 |
| 层级范围 | Server端 - LYBT.Module.MedicalCase模块 |
| 最后更新 | 2025-10-29 |
| 文档版本 | v1.0 |
| 对齐文档 | [Client端医案管理设计](../client/medical-case-design.md) |

---

## 第1章：MedicalCase模块定位与职责

### 1.1 核心定位

**LYBT.Module.MedicalCase** 是Server端的**核心聚合根模块**，在三层架构中扮演以下角色：

```
核心定位：
┌─────────────────────────────────────────────────────────┐
│ MedicalCase（医案）                                     │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 📦 聚合根容器（Aggregate Root）                     │ │
│ │   - 1个MedicalCase = 1次完整看诊会话                │ │
│ │   - 1:1关联Consultation（诊断记录,必需）            │ │
│ │   - 0:1关联Prescription（处方,可选）                │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                          │
│ 🔐 状态管理中枢                                          │
│   - 状态迁移：Pending → InProgress → Completed/Closed   │
│   - 权限控制：CanEdit, CanComplete, CanDelete           │
│   - 业务规则验证：MedicalCaseRules（6个核心规则）       │
│                                                          │
│ 🔗 跨模块协作中心                                        │
│   - 关联Patient模块（患者基本信息）                      │
│   - 协调Consultation模块（诊断录入）                    │
│   - 协调Prescription模块（处方开具）                    │
└─────────────────────────────────────────────────────────┘
```

**三层架构定位**：
```
Controller层（MedicalCasesController）：
  ├── 11个API端点（创建、更新诊断、创建处方、状态迁移...）
  └── 参数验证、权限检查、HTTP响应封装

Service层（MedicalCaseService）：
  ├── 19个业务方法（完整诊疗流程生命周期管理）
  ├── MedicalCaseRules（6个业务规则验证）
  └── 状态机管理（IsValidStatusTransition）

Repository层（MedicalCaseRepository）：
  ├── 11个数据方法（基础查询、详情查询、动态查询）
  ├── Include策略（BaseQuery、DetailQuery）
  └── 隐私保护（MaskPhoneNumber）
```

### 1.2 核心职责

| 职责类别 | 具体职责 | 实现位置 |
|---------|---------|---------|
| **聚合根管理** | 统一管理Consultation和Prescription的生命周期 | MedicalCaseService |
| **状态机控制** | 控制医案状态迁移（Pending/InProgress/Completed/Closed） | IsValidStatusTransition |
| **业务规则验证** | 执行6个核心业务规则（CanCreateNewCase、CanEdit等） | MedicalCaseRules |
| **权限控制** | 检查编辑、删除、完成权限 | CanEditAsync, CanDeleteAsync |
| **诊断管理** | 通过聚合根更新Consultation | UpdateConsultationAsync |
| **处方管理** | 通过聚合根创建/更新/删除Prescription | Create/Update/DeletePrescriptionAsync |
| **数据查询** | 提供灵活的查询能力（分页、详情、动态查询） | MedicalCaseRepository |
| **隐私保护** | 手机号脱敏处理 | MaskPhoneNumber |

### 1.3 设计原则

**DDD聚合根原则**：
1. **边界清晰**：MedicalCase作为聚合根，外部模块不能直接操作Consultation/Prescription
2. **一致性保证**：所有关联实体的修改必须通过MedicalCase进行，确保事务一致性
3. **业务规则集中**：所有业务规则在MedicalCaseService和MedicalCaseRules中集中管理
4. **状态机管理**：通过IsValidStatusTransition确保状态迁移合法性

**示例代码**：
```csharp
// ❌ 错误：直接操作Consultation（违反聚合根边界）
var consultation = await _consultationRepository.GetByIdAsync(consultationId);
consultation.ChiefComplaint = "头痛";
await _consultationRepository.UpdateAsync(consultation);

// ✅ 正确：通过MedicalCase聚合根更新Consultation
await _medicalCaseService.UpdateConsultationAsync(caseId, new UpdateConsultationRequest
{
    ChiefComplaint = "头痛"
});
```

---

## 第2章：核心架构设计（三层架构）

### 2.1 架构层次图

```
┌─────────────────────────────────────────────────────────────────┐
│ Controller层 - MedicalCasesController                           │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ • 11个API端点（POST/GET/PUT/DELETE）                         │ │
│ │ • 参数验证（[FromBody]、[FromRoute]）                        │ │
│ │ • HTTP响应封装（CreatedAtAction、NoContent、Ok）             │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│ Service层 - MedicalCaseService                                  │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 核心业务逻辑（19个方法）                                     │ │
│ │ ├── CreateAsync() - 创建医案                                 │ │
│ │ ├── UpdateConsultationAsync() - 更新诊断                     │ │
│ │ ├── SetPrescriptionFlagAsync() - 设置处方标志                │ │
│ │ ├── Create/Update/DeletePrescriptionAsync() - 处方管理       │ │
│ │ ├── CompleteAsync() - 完成医案                               │ │
│ │ ├── UpdateStatusAsync() - 状态迁移                           │ │
│ │ └── Get...Async() - 查询方法（5个）                          │ │
│ │                                                               │ │
│ │ 业务规则验证（MedicalCaseRules）                              │ │
│ │ ├── CanCreateNewCase() - 患者是否可创建新医案                │ │
│ │ ├── CanEdit() - 医案是否可编辑                               │ │
│ │ ├── CanComplete() - 医案是否可完成                           │ │
│ │ ├── ValidateNewCaseCreation() - 验证新医案创建               │ │
│ │ └── ValidateCaseUpdate() - 验证医案更新                      │ │
│ │                                                               │ │
│ │ 状态机管理                                                     │ │
│ │ └── IsValidStatusTransition() - 状态迁移验证                 │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│ Repository层 - MedicalCaseRepository                            │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 数据访问层（11个方法）                                       │ │
│ │ ├── GetBaseQuery() - 基础查询（Include Consultation）        │ │
│ │ ├── GetDetailQuery() - 详情查询（+Prescription）             │ │
│ │ ├── GetByIdWithDetailsAsync() - 查询详情含关联               │ │
│ │ ├── GetPagedWithDetailsAsync() - 分页详情                    │ │
│ │ ├── QueryAsync() - 动态查询（支持多条件）                    │ │
│ │ ├── GetUnfinishedCaseByPatientIdAsync() - 未完成医案         │ │
│ │ └── MaskPhoneNumber() - 隐私脱敏                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                               ↓
┌─────────────────────────────────────────────────────────────────┐
│ 数据库层 - Entity Framework Core 8                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ MedicalCaseModel（聚合根实体）                                │ │
│ │ ├── Id (Guid, Primary Key)                                   │ │
│ │ ├── PatientId (Guid, Foreign Key)                            │ │
│ │ ├── DoctorId (Guid, Foreign Key)                             │ │
│ │ ├── Status (enum: Pending/InProgress/Completed/Closed)       │ │
│ │ ├── Consultation (1:1 Navigation Property)                   │ │
│ │ └── Prescription (0:1 Navigation Property)                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 IMedicalCaseService接口定义

**完整接口方法（19个）**：

```csharp
public interface IMedicalCaseService
{
    // ========== 基础CRUD（5个方法） ==========

    /// <summary>
    /// 创建医案（聚合根）
    /// </summary>
    /// <param name="dto">创建医案DTO（PatientId、DoctorId、Status）</param>
    /// <returns>创建成功的医案DTO</returns>
    Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto);

    /// <summary>
    /// 按ID查询医案详情（含Consultation + Prescription）
    /// </summary>
    Task<MedicalCaseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询医案列表
    /// </summary>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="patientId">可选：按患者ID过滤</param>
    /// <param name="doctorId">可选：按医生ID过滤</param>
    /// <param name="status">可选：按状态过滤</param>
    /// <returns>分页结果（包含总数、数据列表）</returns>
    Task<PagedResult<MedicalCaseDto>> GetListAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null,
        MedicalCaseStatus? status = null);

    /// <summary>
    /// 更新医案基本信息（非诊断/处方）
    /// </summary>
    Task<MedicalCaseDto> UpdateAsync(Guid id, UpdateMedicalCaseDto dto);

    /// <summary>
    /// 删除医案（仅草稿状态可删除）
    /// </summary>
    Task DeleteAsync(Guid id);

    // ========== 诊断管理（3个方法） ==========

    /// <summary>
    /// 更新诊断记录（通过聚合根）
    /// </summary>
    /// <param name="caseId">医案ID</param>
    /// <param name="request">诊断信息（四诊、中医诊断、治法）</param>
    Task UpdateConsultationAsync(Guid caseId, UpdateConsultationRequest request);

    /// <summary>
    /// 完成Step1诊断录入（状态迁移 + 数据保存）
    /// </summary>
    Task<ConsultationFlowResult> CompleteStep1Async(
        Guid caseId,
        UpdateConsultationRequest request);

    /// <summary>
    /// 重置诊断步骤（清除诊断数据）
    /// </summary>
    Task ResetConsultationStepsAsync(Guid caseId);

    // ========== 处方管理（5个方法） ==========

    /// <summary>
    /// 设置处方标志（是否需要开方）
    /// </summary>
    /// <param name="caseId">医案ID</param>
    /// <param name="request">包含HasPrescription标志</param>
    Task SetPrescriptionFlagAsync(Guid caseId, SetPrescriptionFlagRequest request);

    /// <summary>
    /// 创建处方（通过聚合根）
    /// </summary>
    /// <param name="caseId">医案ID</param>
    /// <param name="request">处方信息（药材列表、用法）</param>
    Task<PrescriptionDto> CreatePrescriptionAsync(
        Guid caseId,
        CreatePrescriptionRequest request);

    /// <summary>
    /// 更新处方
    /// </summary>
    Task<PrescriptionDto> UpdatePrescriptionAsync(
        Guid caseId,
        UpdatePrescriptionRequest request);

    /// <summary>
    /// 删除处方（需权限检查）
    /// </summary>
    Task DeletePrescriptionAsync(Guid caseId);

    /// <summary>
    /// 清空处方数据（保留处方记录但清空内容）
    /// </summary>
    Task ClearPrescriptionAsync(Guid caseId);

    // ========== 状态管理（3个方法） ==========

    /// <summary>
    /// 更新医案状态（带状态迁移验证）
    /// </summary>
    /// <param name="id">医案ID</param>
    /// <param name="newStatus">目标状态</param>
    Task UpdateStatusAsync(Guid id, MedicalCaseStatus newStatus);

    /// <summary>
    /// 完成医案（终态：Completed）
    /// 前置条件：必须有诊断记录
    /// </summary>
    Task CompleteAsync(Guid id);

    /// <summary>
    /// 关闭医案（终态：Closed）
    /// 可从任何非终态迁移
    /// </summary>
    Task CloseCaseAsync(Guid id);

    // ========== 查询方法（3个） ==========

    /// <summary>
    /// 查询诊断记录列表（按患者或医生过滤）
    /// </summary>
    Task<PagedResult<ConsultationDto>> GetConsultationListAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null);

    /// <summary>
    /// 查询处方列表（按患者或医生过滤）
    /// </summary>
    Task<PagedResult<PrescriptionDto>> GetPrescriptionListAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null);

    /// <summary>
    /// 获取患者未完成医案（用于"继续看诊"功能）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>未完成的医案（状态为InProgress或Pending）</returns>
    Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);

    // ========== 权限检查（2个方法） ==========

    /// <summary>
    /// 检查医案是否可编辑（非终态）
    /// </summary>
    Task<bool> CanEditAsync(Guid id);

    /// <summary>
    /// 检查处方是否可删除（需业务规则验证）
    /// </summary>
    Task<bool> CanDeletePrescriptionAsync(Guid caseId);
}
```

### 2.3 核心实体关系（Entity）

```csharp
// 聚合根实体
public class MedicalCaseModel : BaseEntity
{
    public Guid Id { get; set; }

    // 关联患者和医生
    public Guid PatientId { get; set; }
    public PatientModel Patient { get; set; } = null!;

    public Guid DoctorId { get; set; }
    public UserModel Doctor { get; set; } = null!;

    // 医案状态（状态机）
    public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Pending;

    // 1:1关联诊断记录（必需）
    public ConsultationModel? Consultation { get; set; }

    // 0:1关联处方（可选）
    public PrescriptionModel? Prescription { get; set; }

    // 审计字段
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// 状态枚举
public enum MedicalCaseStatus
{
    Pending = 0,      // 待接诊
    InProgress = 1,   // 诊疗中
    Completed = 2,    // 已完成（终态）
    Closed = 3        // 已关闭（终态）
}
```

---

## 第3章：聚合根模式与边界管理

### 3.1 聚合根边界定义

**核心原则**：MedicalCase作为聚合根，统一管理Consultation和Prescription的生命周期，外部模块不得直接修改关联实体。

```
聚合根边界：
┌─────────────────────────────────────────────────────────┐
│ MedicalCase（聚合根）                                    │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ Id: Guid                                             │ │
│ │ PatientId: Guid                                      │ │
│ │ DoctorId: Guid                                       │ │
│ │ Status: MedicalCaseStatus                            │ │
│ │                                                      │ │
│ │ ┌─────────────────────────────────────────────────┐ │ │
│ │ │ Consultation（1:1，必需）                        │ │ │
│ │ │ ├── ChiefComplaint（主诉）                       │ │ │
│ │ │ ├── PresentIllness（现病史）                     │ │ │
│ │ │ ├── Inspection（望诊）                           │ │ │
│ │ │ ├── Inquiry（问诊）                              │ │ │
│ │ │ ├── TcmDiagnosis（中医诊断）                     │ │ │
│ │ │ └── TreatmentMethod（治法）                      │ │ │
│ │ └─────────────────────────────────────────────────┘ │ │
│ │                                                      │ │
│ │ ┌─────────────────────────────────────────────────┐ │ │
│ │ │ Prescription（0:1，可选）                        │ │ │
│ │ │ ├── Items: List<PrescriptionItem>（药材列表）   │ │ │
│ │ │ ├── UsageInstructions（用法）                    │ │ │
│ │ │ ├── TotalDosage（总剂量）                        │ │ │
│ │ │ └── EstimatedPrice（估算价格）                   │ │ │
│ │ └─────────────────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘

边界规则：
1. ✅ 通过MedicalCaseService.UpdateConsultationAsync()更新诊断
2. ✅ 通过MedicalCaseService.CreatePrescriptionAsync()创建处方
3. ❌ 禁止直接调用ConsultationRepository.UpdateAsync()
4. ❌ 禁止直接调用PrescriptionRepository.CreateAsync()
```

### 3.2 聚合根操作示例

**正确的操作方式**（通过聚合根）：

```csharp
// ✅ 场景1：创建医案 → 更新诊断 → 创建处方 → 完成医案
public async Task<MedicalCaseDto> PerformFullConsultationFlow(
    Guid patientId,
    Guid doctorId)
{
    // Step 1: 创建医案（聚合根）
    var newCase = await _medicalCaseService.CreateAsync(new CreateMedicalCaseDto
    {
        PatientId = patientId,
        DoctorId = doctorId,
        Status = MedicalCaseStatus.InProgress
    });

    // Step 2: 更新诊断记录（通过聚合根）
    await _medicalCaseService.UpdateConsultationAsync(newCase.Id, new UpdateConsultationRequest
    {
        ChiefComplaint = "头痛三天",
        PresentIllness = "患者自述三天前开始出现头痛症状...",
        Inspection = "面色微红，舌质红，苔薄黄",
        Inquiry = "伴有发热、口干、咽痛",
        TcmDiagnosis = "外感风热证",
        TreatmentMethod = "疏风清热，宣肺止痛"
    });

    // Step 3: 设置处方标志
    await _medicalCaseService.SetPrescriptionFlagAsync(newCase.Id, new SetPrescriptionFlagRequest
    {
        HasPrescription = true
    });

    // Step 4: 创建处方（通过聚合根）
    await _medicalCaseService.CreatePrescriptionAsync(newCase.Id, new CreatePrescriptionRequest
    {
        Items = new List<PrescriptionItemDto>
        {
            new() { HerbId = herb1Id, HerbName = "桑叶", Dosage = 10, Unit = "克", Quantity = 7 },
            new() { HerbId = herb2Id, HerbName = "菊花", Dosage = 10, Unit = "克", Quantity = 7 },
            new() { HerbId = herb3Id, HerbName = "薄荷", Dosage = 6, Unit = "克", Quantity = 7 }
        },
        UsageInstructions = "水煎服，日一剂，早晚温服"
    });

    // Step 5: 完成医案（状态迁移到终态）
    await _medicalCaseService.CompleteAsync(newCase.Id);

    return await _medicalCaseService.GetByIdAsync(newCase.Id);
}
```

**错误的操作方式**（违反聚合根边界）：

```csharp
// ❌ 错误示例：直接操作Consultation（违反聚合根原则）
public async Task UpdateConsultationDirectly(Guid consultationId)
{
    // 直接获取Consultation实体
    var consultation = await _consultationRepository.GetByIdAsync(consultationId);

    // 直接修改Consultation
    consultation.ChiefComplaint = "头痛";
    consultation.TcmDiagnosis = "外感风热";

    // 直接保存（绕过了聚合根的业务规则验证）
    await _consultationRepository.UpdateAsync(consultation);

    // 问题：
    // 1. 没有通过MedicalCase聚合根，无法确保事务一致性
    // 2. 没有执行MedicalCaseRules业务规则验证
    // 3. 没有触发状态机检查
    // 4. 违反了DDD聚合根边界原则
}

// ❌ 错误示例：直接创建Prescription（违反聚合根边界）
public async Task CreatePrescriptionDirectly(Guid caseId)
{
    // 直接创建Prescription实体
    var prescription = new PrescriptionModel
    {
        MedicalCaseId = caseId,
        UsageInstructions = "水煎服"
    };

    // 直接保存（绕过聚合根）
    await _prescriptionRepository.AddAsync(prescription);

    // 问题：
    // 1. 没有检查MedicalCase状态（可能已完成或关闭）
    // 2. 没有验证CanEdit权限
    // 3. 没有设置HasPrescription标志
}
```

### 3.3 聚合根边界保护机制

**在Service层实施边界保护**：

```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;

    /// <summary>
    /// 更新诊断记录（聚合根边界保护）
    /// </summary>
    public async Task UpdateConsultationAsync(
        Guid caseId,
        UpdateConsultationRequest request)
    {
        // Step 1: 获取聚合根
        var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId);
        if (medicalCase == null)
            throw new NotFoundException($"医案不存在: {caseId}");

        // Step 2: 边界保护 - 检查是否可编辑（非终态）
        if (!MedicalCaseRules.CanEdit(medicalCase))
            throw new ValidationException("医案已完成或关闭，无法编辑");

        // Step 3: 边界保护 - 检查权限
        if (!await CanEditAsync(caseId))
            throw new UnauthorizedException("无权编辑此医案");

        // Step 4: 通过聚合根更新Consultation
        if (medicalCase.Consultation == null)
        {
            // 创建新的Consultation
            medicalCase.Consultation = new ConsultationModel
            {
                MedicalCaseId = caseId
            };
        }

        // 更新Consultation属性
        medicalCase.Consultation.ChiefComplaint = request.ChiefComplaint;
        medicalCase.Consultation.PresentIllness = request.PresentIllness;
        medicalCase.Consultation.Inspection = request.Inspection;
        medicalCase.Consultation.Inquiry = request.Inquiry;
        medicalCase.Consultation.TcmDiagnosis = request.TcmDiagnosis;
        medicalCase.Consultation.TreatmentMethod = request.TreatmentMethod;
        medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;

        // Step 5: 保存聚合根（确保事务一致性）
        await _repository.UpdateAsync(medicalCase);
    }

    /// <summary>
    /// 创建处方（聚合根边界保护）
    /// </summary>
    public async Task<PrescriptionDto> CreatePrescriptionAsync(
        Guid caseId,
        CreatePrescriptionRequest request)
    {
        // Step 1: 获取聚合根
        var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId);
        if (medicalCase == null)
            throw new NotFoundException($"医案不存在: {caseId}");

        // Step 2: 边界保护 - 检查是否可编辑
        if (!MedicalCaseRules.CanEdit(medicalCase))
            throw new ValidationException("医案已完成或关闭，无法创建处方");

        // Step 3: 边界保护 - 检查是否已有处方
        if (medicalCase.Prescription != null)
            throw new ValidationException("医案已有处方，请使用更新接口");

        // Step 4: 边界保护 - 检查是否有诊断记录
        if (medicalCase.Consultation == null ||
            string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint))
            throw new ValidationException("必须先完成诊断记录才能创建处方");

        // Step 5: 通过聚合根创建Prescription
        var prescription = new PrescriptionModel
        {
            MedicalCaseId = caseId,
            UsageInstructions = request.UsageInstructions,
            Items = request.Items.Select(item => new PrescriptionItemModel
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit,
                Quantity = item.Quantity
            }).ToList()
        };

        medicalCase.Prescription = prescription;

        // Step 6: 保存聚合根
        await _repository.UpdateAsync(medicalCase);

        return _mapper.Map<PrescriptionDto>(prescription);
    }
}
```

---

## 第4章：Service层业务逻辑实现

### 4.1 MedicalCaseService核心方法

**MedicalCaseService**包含19个方法，覆盖诊疗流程的完整生命周期管理：

#### 4.1.1 创建医案（CreateAsync）

```csharp
/// <summary>
/// 创建医案
/// 业务规则：患者只能有一个未完成医案（InProgress或Pending）
/// </summary>
public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
{
    // Step 1: 获取患者现有医案
    var existingCases = await _repository.GetByPatientIdAsync(dto.PatientId);

    // Step 2: 业务规则验证 - 检查是否已有未完成医案
    if (!MedicalCaseRules.CanCreateNewCase(existingCases))
    {
        throw new ValidationException("患者已有未完成医案，请先完成或关闭现有医案");
    }

    // Step 3: 创建医案实体
    var medicalCase = new MedicalCaseModel
    {
        Id = Guid.NewGuid(),
        PatientId = dto.PatientId,
        DoctorId = dto.DoctorId,
        Status = MedicalCaseStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    // Step 4: 业务规则验证 - 完整性检查
    var validation = MedicalCaseRules.ValidateNewCaseCreation(
        medicalCase,
        existingCases
    );

    if (!validation.IsValid)
    {
        throw new ValidationException(validation.ErrorMessage);
    }

    // Step 5: 保存医案
    await _repository.AddAsync(medicalCase);

    _logger.LogInformation(
        "创建医案成功: CaseId={CaseId}, PatientId={PatientId}, DoctorId={DoctorId}",
        medicalCase.Id, dto.PatientId, dto.DoctorId
    );

    return _mapper.Map<MedicalCaseDto>(medicalCase);
}
```

#### 4.1.2 更新诊断记录（UpdateConsultationAsync）

```csharp
/// <summary>
/// 更新诊断记录（通过聚合根）
/// </summary>
public async Task UpdateConsultationAsync(
    Guid caseId,
    UpdateConsultationRequest request)
{
    // Step 1: 获取聚合根（含Consultation）
    var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {caseId}");

    // Step 2: 权限检查 - 是否可编辑
    if (!MedicalCaseRules.CanEdit(medicalCase))
        throw new ValidationException("医案已完成或关闭，无法编辑诊断记录");

    // Step 3: 创建或更新Consultation
    if (medicalCase.Consultation == null)
    {
        medicalCase.Consultation = new ConsultationModel
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = caseId,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Step 4: 更新诊断字段（四诊 + 中医诊断 + 治法）
    medicalCase.Consultation.ChiefComplaint = request.ChiefComplaint;
    medicalCase.Consultation.PresentIllness = request.PresentIllness;
    medicalCase.Consultation.Inspection = request.Inspection;       // 望诊
    medicalCase.Consultation.Auscultation = request.Auscultation;   // 闻诊
    medicalCase.Consultation.Inquiry = request.Inquiry;             // 问诊
    medicalCase.Consultation.Palpation = request.Palpation;         // 切诊
    medicalCase.Consultation.TcmDiagnosis = request.TcmDiagnosis;
    medicalCase.Consultation.TreatmentMethod = request.TreatmentMethod;
    medicalCase.Consultation.UpdatedAt = DateTime.UtcNow;

    // Step 5: 状态迁移 - 如果是Pending状态，自动迁移到InProgress
    if (medicalCase.Status == MedicalCaseStatus.Pending)
    {
        medicalCase.Status = MedicalCaseStatus.InProgress;
    }

    // Step 6: 保存聚合根
    await _repository.UpdateAsync(medicalCase);

    _logger.LogInformation(
        "更新诊断记录成功: CaseId={CaseId}, ChiefComplaint={ChiefComplaint}",
        caseId, request.ChiefComplaint
    );
}
```

#### 4.1.3 创建处方（CreatePrescriptionAsync）

```csharp
/// <summary>
/// 创建处方（通过聚合根）
/// 前置条件：必须先完成诊断记录
/// </summary>
public async Task<PrescriptionDto> CreatePrescriptionAsync(
    Guid caseId,
    CreatePrescriptionRequest request)
{
    // Step 1: 获取聚合根（含Consultation + Prescription）
    var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {caseId}");

    // Step 2: 业务规则验证 - 是否可编辑
    if (!MedicalCaseRules.CanEdit(medicalCase))
        throw new ValidationException("医案已完成或关闭，无法创建处方");

    // Step 3: 业务规则验证 - 检查是否已有处方
    if (medicalCase.Prescription != null)
        throw new ValidationException("医案已有处方，请使用更新接口");

    // Step 4: 业务规则验证 - 必须先完成诊断
    if (medicalCase.Consultation == null ||
        string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint))
    {
        throw new ValidationException("必须先完成诊断记录才能创建处方");
    }

    // Step 5: 创建Prescription实体
    var prescription = new PrescriptionModel
    {
        Id = Guid.NewGuid(),
        MedicalCaseId = caseId,
        UsageInstructions = request.UsageInstructions,
        CreatedAt = DateTime.UtcNow,
        Items = request.Items.Select(item => new PrescriptionItemModel
        {
            Id = Guid.NewGuid(),
            HerbId = item.HerbId,
            HerbName = item.HerbName,
            Dosage = item.Dosage,
            Unit = item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList()
    };

    // Step 6: 计算总剂量和估算价格（使用HerbCalculatorBase）
    var calculator = new HerbCalculator();
    prescription.TotalDosage = calculator.CalculateTotalDosage(prescription.Items);
    prescription.EstimatedPrice = calculator.CalculateEstimatedTotalPrice(prescription.Items);

    // Step 7: 关联到聚合根
    medicalCase.Prescription = prescription;

    // Step 8: 保存聚合根
    await _repository.UpdateAsync(medicalCase);

    _logger.LogInformation(
        "创建处方成功: CaseId={CaseId}, PrescriptionId={PrescriptionId}, TotalDosage={TotalDosage}",
        caseId, prescription.Id, prescription.TotalDosage
    );

    return _mapper.Map<PrescriptionDto>(prescription);
}
```

#### 4.1.4 完成医案（CompleteAsync）

```csharp
/// <summary>
/// 完成医案（状态迁移到终态：Completed）
/// 前置条件：必须有诊断记录
/// </summary>
public async Task CompleteAsync(Guid id)
{
    // Step 1: 获取聚合根
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {id}");

    // Step 2: 状态迁移验证
    if (!IsValidStatusTransition(medicalCase.Status, MedicalCaseStatus.Completed))
    {
        throw new ValidationException(
            $"无效的状态迁移: {medicalCase.Status} → Completed"
        );
    }

    // Step 3: 业务规则验证 - 是否可完成
    if (!MedicalCaseRules.CanComplete(medicalCase))
    {
        throw new ValidationException("医案缺少必要的诊断信息，无法完成");
    }

    // Step 4: 状态迁移
    medicalCase.Status = MedicalCaseStatus.Completed;
    medicalCase.UpdatedAt = DateTime.UtcNow;

    // Step 5: 保存聚合根
    await _repository.UpdateAsync(medicalCase);

    _logger.LogInformation(
        "完成医案成功: CaseId={CaseId}, Status=Completed",
        id
    );
}

/// <summary>
/// 状态迁移验证
/// </summary>
private bool IsValidStatusTransition(
    MedicalCaseStatus from,
    MedicalCaseStatus to)
{
    return (from, to) switch
    {
        // 合法的状态迁移路径
        (MedicalCaseStatus.Pending, MedicalCaseStatus.InProgress) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Completed) => true,
        (MedicalCaseStatus.Pending, MedicalCaseStatus.Closed) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Closed) => true,

        // 其他迁移非法
        _ => false
    };
}
```

#### 4.1.5 获取未完成医案（GetUnfinishedCaseByPatientIdAsync）

```csharp
/// <summary>
/// 获取患者未完成医案（用于"继续看诊"功能）
/// </summary>
public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
{
    // Step 1: 从Repository查询未完成医案
    var medicalCase = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId);

    if (medicalCase == null)
        return null;

    // Step 2: 映射到DTO
    var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

    _logger.LogInformation(
        "查询未完成医案成功: PatientId={PatientId}, CaseId={CaseId}, Status={Status}",
        patientId, medicalCase.Id, medicalCase.Status
    );

    return dto;
}
```

---

## 第5章：MedicalCaseRules业务规则类

### 5.1 业务规则类定义

**MedicalCaseRules**是一个静态工具类，集中管理医案的所有业务规则验证逻辑：

```csharp
/// <summary>
/// 医案业务规则类
/// 包含6个核心业务规则的验证方法
/// </summary>
public static class MedicalCaseRules
{
    /// <summary>
    /// 规则1：患者是否可创建新医案
    /// 业务规则：患者只能有一个未完成医案（InProgress或Pending）
    /// </summary>
    /// <param name="existingCases">患者现有医案列表</param>
    /// <returns>true = 可创建新医案, false = 已有未完成医案</returns>
    public static bool CanCreateNewCase(IEnumerable<MedicalCaseModel> existingCases)
    {
        return !existingCases.Any(c =>
            c.Status == MedicalCaseStatus.InProgress ||
            c.Status == MedicalCaseStatus.Pending
        );
    }

    /// <summary>
    /// 规则2：医案是否可编辑
    /// 业务规则：只有非终态的医案可以编辑（Pending和InProgress）
    /// </summary>
    /// <param name="medicalCase">医案实体</param>
    /// <returns>true = 可编辑, false = 已终态（Completed/Closed）</returns>
    public static bool CanEdit(MedicalCaseModel medicalCase)
    {
        return medicalCase.Status != MedicalCaseStatus.Completed &&
               medicalCase.Status != MedicalCaseStatus.Closed;
    }

    /// <summary>
    /// 规则3：医案是否可删除
    /// 业务规则：只有Pending状态的医案可以删除（草稿）
    /// </summary>
    /// <param name="medicalCase">医案实体</param>
    /// <returns>true = 可删除, false = 已进入诊疗流程</returns>
    public static bool CanDelete(MedicalCaseModel medicalCase)
    {
        return medicalCase.Status == MedicalCaseStatus.Pending;
    }

    /// <summary>
    /// 规则4：医案是否可完成
    /// 业务规则：必须有诊断记录（至少有主诉）
    /// </summary>
    /// <param name="medicalCase">医案实体（含Consultation）</param>
    /// <returns>true = 可完成, false = 缺少诊断信息</returns>
    public static bool CanComplete(MedicalCaseModel medicalCase)
    {
        return medicalCase.Consultation != null &&
               !string.IsNullOrWhiteSpace(medicalCase.Consultation.ChiefComplaint);
    }

    /// <summary>
    /// 规则5：验证新医案创建
    /// 综合验证：患者是否可创建、医生是否有效
    /// </summary>
    /// <param name="newCase">待创建的医案</param>
    /// <param name="existingCases">患者现有医案列表</param>
    /// <returns>ValidationResult（IsValid + ErrorMessage）</returns>
    public static ValidationResult ValidateNewCaseCreation(
        MedicalCaseModel newCase,
        IEnumerable<MedicalCaseModel> existingCases)
    {
        // 检查1：患者是否有未完成医案
        if (!CanCreateNewCase(existingCases))
        {
            return ValidationResult.Failure(
                "患者已有未完成医案，请先完成或关闭现有医案"
            );
        }

        // 检查2：医生ID是否有效
        if (newCase.DoctorId == Guid.Empty)
        {
            return ValidationResult.Failure("医生ID无效");
        }

        // 检查3：患者ID是否有效
        if (newCase.PatientId == Guid.Empty)
        {
            return ValidationResult.Failure("患者ID无效");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// 规则6：验证医案更新
    /// 综合验证：是否可编辑、数据完整性
    /// </summary>
    /// <param name="medicalCase">待更新的医案</param>
    /// <returns>ValidationResult（IsValid + ErrorMessage）</returns>
    public static ValidationResult ValidateCaseUpdate(MedicalCaseModel medicalCase)
    {
        // 检查1：是否可编辑（非终态）
        if (!CanEdit(medicalCase))
        {
            return ValidationResult.Failure(
                "医案已完成或关闭，无法编辑"
            );
        }

        // 检查2：如果有处方，必须有诊断记录
        if (medicalCase.Prescription != null && medicalCase.Consultation == null)
        {
            return ValidationResult.Failure(
                "处方必须基于诊断记录，请先完成诊断"
            );
        }

        return ValidationResult.Success();
    }
}
```

### 5.2 业务规则使用示例

```csharp
// 示例1：创建医案前的业务规则验证
public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
{
    // 获取患者现有医案
    var existingCases = await _repository.GetByPatientIdAsync(dto.PatientId);

    // 使用规则1：检查是否可创建新医案
    if (!MedicalCaseRules.CanCreateNewCase(existingCases))
    {
        throw new ValidationException("患者已有未完成医案");
    }

    var newCase = new MedicalCaseModel
    {
        PatientId = dto.PatientId,
        DoctorId = dto.DoctorId
    };

    // 使用规则5：综合验证新医案创建
    var validation = MedicalCaseRules.ValidateNewCaseCreation(newCase, existingCases);
    if (!validation.IsValid)
    {
        throw new ValidationException(validation.ErrorMessage);
    }

    await _repository.AddAsync(newCase);
    return _mapper.Map<MedicalCaseDto>(newCase);
}

// 示例2：更新诊断前的业务规则验证
public async Task UpdateConsultationAsync(
    Guid caseId,
    UpdateConsultationRequest request)
{
    var medicalCase = await _repository.GetByIdAsync(caseId);

    // 使用规则2：检查是否可编辑
    if (!MedicalCaseRules.CanEdit(medicalCase))
    {
        throw new ValidationException("医案已终态，无法编辑");
    }

    // 更新诊断记录...
}

// 示例3：完成医案前的业务规则验证
public async Task CompleteAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

    // 使用规则4：检查是否可完成
    if (!MedicalCaseRules.CanComplete(medicalCase))
    {
        throw new ValidationException("缺少诊断信息，无法完成医案");
    }

    medicalCase.Status = MedicalCaseStatus.Completed;
    await _repository.UpdateAsync(medicalCase);
}
```

---

## 第6章：Repository层数据访问

### 6.1 IMedicalCaseRepository接口定义

```csharp
public interface IMedicalCaseRepository : IBaseRepository<MedicalCaseModel>
{
    // ========== 基础查询（2个方法） ==========

    /// <summary>
    /// 获取基础查询（Include Consultation）
    /// </summary>
    /// <returns>IQueryable（延迟执行）</returns>
    IQueryable<MedicalCaseModel> GetBaseQuery();

    /// <summary>
    /// 获取详情查询（Include Consultation + Prescription + Patient）
    /// </summary>
    /// <returns>IQueryable（延迟执行）</returns>
    IQueryable<MedicalCaseModel> GetDetailQuery();

    // ========== 详情查询（2个方法） ==========

    /// <summary>
    /// 按ID查询医案详情（含Consultation + Prescription）
    /// </summary>
    Task<MedicalCaseModel?> GetByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// 分页查询详情（含统计信息）
    /// </summary>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="patientId">可选：按患者过滤</param>
    /// <param name="doctorId">可选：按医生过滤</param>
    /// <param name="status">可选：按状态过滤</param>
    /// <returns>分页结果（包含总数、数据列表）</returns>
    Task<PagedResult<MedicalCaseModel>> GetPagedWithDetailsAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null,
        MedicalCaseStatus? status = null);

    // ========== 条件查询（4个方法） ==========

    /// <summary>
    /// 按患者ID查询医案列表
    /// </summary>
    Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 按医生ID查询医案列表
    /// </summary>
    Task<List<MedicalCaseModel>> GetByDoctorIdAsync(Guid doctorId);

    /// <summary>
    /// 获取待处理医案（Pending状态）
    /// </summary>
    Task<List<MedicalCaseModel>> GetPendingCasesAsync();

    /// <summary>
    /// 获取患者未完成医案（InProgress或Pending）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>未完成的医案（null表示无未完成医案）</returns>
    Task<MedicalCaseModel?> GetUnfinishedCaseByPatientIdAsync(Guid patientId);

    // ========== 动态查询（1个方法） ==========

    /// <summary>
    /// 动态查询（支持多条件组合）
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <param name="includeDetails">是否包含关联数据（Consultation + Prescription）</param>
    /// <returns>查询结果列表</returns>
    Task<List<MedicalCaseModel>> QueryAsync(
        Expression<Func<MedicalCaseModel, bool>> predicate,
        bool includeDetails = false);

    // ========== 隐私保护（1个方法） ==========

    /// <summary>
    /// 手机号脱敏处理
    /// </summary>
    /// <param name="phoneNumber">原始手机号</param>
    /// <returns>脱敏后的手机号（如：138****5678）</returns>
    string MaskPhoneNumber(string phoneNumber);
}
```

### 6.2 MedicalCaseRepository实现

```csharp
public class MedicalCaseRepository : BaseRepository<MedicalCaseModel>, IMedicalCaseRepository
{
    public MedicalCaseRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 基础查询（Include Consultation）
    /// </summary>
    public IQueryable<MedicalCaseModel> GetBaseQuery()
    {
        return _dbSet
            .Include(mc => mc.Consultation)
            .AsNoTracking();
    }

    /// <summary>
    /// 详情查询（Include Consultation + Prescription + Patient）
    /// </summary>
    public IQueryable<MedicalCaseModel> GetDetailQuery()
    {
        return _dbSet
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
                .ThenInclude(p => p.Items)
            .Include(mc => mc.Patient)
            .AsNoTracking();
    }

    /// <summary>
    /// 按ID查询医案详情（含关联数据）
    /// </summary>
    public async Task<MedicalCaseModel?> GetByIdWithDetailsAsync(Guid id)
    {
        return await GetDetailQuery()
            .FirstOrDefaultAsync(mc => mc.Id == id);
    }

    /// <summary>
    /// 分页查询详情（含统计）
    /// </summary>
    public async Task<PagedResult<MedicalCaseModel>> GetPagedWithDetailsAsync(
        int pageIndex,
        int pageSize,
        Guid? patientId = null,
        Guid? doctorId = null,
        MedicalCaseStatus? status = null)
    {
        var query = GetDetailQuery();

        // 应用过滤条件
        if (patientId.HasValue)
            query = query.Where(mc => mc.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(mc => mc.DoctorId == doctorId.Value);

        if (status.HasValue)
            query = query.Where(mc => mc.Status == status.Value);

        // 计算总数
        var totalCount = await query.CountAsync();

        // 分页查询
        var items = await query
            .OrderByDescending(mc => mc.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MedicalCaseModel>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 按患者ID查询医案列表
    /// </summary>
    public async Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId)
    {
        return await GetBaseQuery()
            .Where(mc => mc.PatientId == patientId)
            .OrderByDescending(mc => mc.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取患者未完成医案（InProgress或Pending）
    /// </summary>
    public async Task<MedicalCaseModel?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
    {
        return await GetDetailQuery()
            .Where(mc => mc.PatientId == patientId &&
                        (mc.Status == MedicalCaseStatus.InProgress ||
                         mc.Status == MedicalCaseStatus.Pending))
            .OrderByDescending(mc => mc.UpdatedAt ?? mc.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 动态查询（支持多条件组合）
    /// </summary>
    public async Task<List<MedicalCaseModel>> QueryAsync(
        Expression<Func<MedicalCaseModel, bool>> predicate,
        bool includeDetails = false)
    {
        var query = includeDetails ? GetDetailQuery() : GetBaseQuery();

        return await query
            .Where(predicate)
            .ToListAsync();
    }

    /// <summary>
    /// 手机号脱敏（隐私保护）
    /// </summary>
    /// <example>
    /// 输入：13812345678
    /// 输出：138****5678
    /// </example>
    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11)
            return phoneNumber;

        return $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(7)}";
    }
}
```

### 6.3 Repository使用示例

```csharp
// 示例1：查询患者未完成医案（用于"继续看诊"功能）
public async Task<MedicalCaseDto?> GetUnfinishedCase(Guid patientId)
{
    var unfinishedCase = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId);

    if (unfinishedCase == null)
        return null;

    // 手机号脱敏
    if (unfinishedCase.Patient != null)
    {
        unfinishedCase.Patient.PhoneNumber = _repository.MaskPhoneNumber(
            unfinishedCase.Patient.PhoneNumber
        );
    }

    return _mapper.Map<MedicalCaseDto>(unfinishedCase);
}

// 示例2：动态查询（多条件组合）
public async Task<List<MedicalCaseDto>> SearchCases(
    DateTime? startDate,
    DateTime? endDate,
    MedicalCaseStatus? status)
{
    var cases = await _repository.QueryAsync(
        mc => (!startDate.HasValue || mc.CreatedAt >= startDate.Value) &&
              (!endDate.HasValue || mc.CreatedAt <= endDate.Value) &&
              (!status.HasValue || mc.Status == status.Value),
        includeDetails: true
    );

    return _mapper.Map<List<MedicalCaseDto>>(cases);
}
```

---

## 第7章：状态机管理（State Machine）

### 7.1 状态迁移图

```
医案状态机（4个状态，6条迁移路径）：

┌─────────────┐
│  Pending    │ 待接诊（初始状态）
│  （0）      │
└──────┬──────┘
       │ ① 开始诊疗
       │ Trigger: UpdateConsultationAsync()
       ↓
┌─────────────┐
│ InProgress  │ 诊疗中（工作状态）
│  （1）      │
└──────┬──────┘
       │
       ├─────────② 完成诊疗──────────┐
       │ Trigger: CompleteAsync()    │
       │                             ↓
       │                       ┌─────────────┐
       │                       │  Completed  │ 已完成（终态）
       │                       │    （2）    │
       │                       └─────────────┘
       │
       ├─────────③ 关闭医案──────────┐
       │ Trigger: CloseCaseAsync()   │
       │                             ↓
       │                       ┌─────────────┐
       │                       │   Closed    │ 已关闭（终态）
       │                       │    （3）    │
       │                       └─────────────┘
       │
       └─────────④ 从Pending直接关闭─┘
         Trigger: CloseCaseAsync()

状态迁移规则：
✅ 合法迁移：
  - Pending → InProgress（开始诊疗）
  - InProgress → Completed（完成诊疗）
  - Pending → Closed（直接关闭）
  - InProgress → Closed（中止诊疗）

❌ 非法迁移：
  - Completed → InProgress（终态不可逆）
  - Closed → InProgress（终态不可逆）
  - Pending → Completed（跳过诊疗流程）
  - Completed → Closed（终态互不转换）
```

### 7.2 状态迁移验证实现

```csharp
/// <summary>
/// 状态迁移验证器
/// </summary>
private bool IsValidStatusTransition(
    MedicalCaseStatus from,
    MedicalCaseStatus to)
{
    return (from, to) switch
    {
        // ✅ 合法迁移路径（6条）
        (MedicalCaseStatus.Pending, MedicalCaseStatus.InProgress) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Completed) => true,
        (MedicalCaseStatus.Pending, MedicalCaseStatus.Closed) => true,
        (MedicalCaseStatus.InProgress, MedicalCaseStatus.Closed) => true,

        // ❌ 其他迁移非法
        _ => false
    };
}

/// <summary>
/// 更新医案状态（带状态迁移验证）
/// </summary>
public async Task UpdateStatusAsync(Guid id, MedicalCaseStatus newStatus)
{
    // Step 1: 获取医案
    var medicalCase = await _repository.GetByIdAsync(id);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {id}");

    // Step 2: 状态迁移验证
    if (!IsValidStatusTransition(medicalCase.Status, newStatus))
    {
        throw new ValidationException(
            $"无效的状态迁移: {medicalCase.Status} → {newStatus}"
        );
    }

    // Step 3: 业务规则验证（根据目标状态）
    if (newStatus == MedicalCaseStatus.Completed)
    {
        if (!MedicalCaseRules.CanComplete(medicalCase))
        {
            throw new ValidationException("医案缺少必要的诊断信息，无法完成");
        }
    }

    // Step 4: 执行状态迁移
    var oldStatus = medicalCase.Status;
    medicalCase.Status = newStatus;
    medicalCase.UpdatedAt = DateTime.UtcNow;

    // Step 5: 保存变更
    await _repository.UpdateAsync(medicalCase);

    _logger.LogInformation(
        "状态迁移成功: CaseId={CaseId}, {OldStatus} → {NewStatus}",
        id, oldStatus, newStatus
    );
}
```

### 7.3 状态迁移场景示例

```csharp
// 场景1：开始诊疗（Pending → InProgress）
// Trigger: UpdateConsultationAsync()
public async Task UpdateConsultationAsync(Guid caseId, UpdateConsultationRequest request)
{
    var medicalCase = await _repository.GetByIdAsync(caseId);

    // 自动状态迁移（如果是Pending）
    if (medicalCase.Status == MedicalCaseStatus.Pending)
    {
        medicalCase.Status = MedicalCaseStatus.InProgress;
    }

    // 更新诊断记录...
    await _repository.UpdateAsync(medicalCase);
}

// 场景2：完成诊疗（InProgress → Completed）
// Trigger: CompleteAsync()
public async Task CompleteAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

    // 验证状态迁移合法性
    if (!IsValidStatusTransition(medicalCase.Status, MedicalCaseStatus.Completed))
    {
        throw new ValidationException($"无效的状态迁移: {medicalCase.Status} → Completed");
    }

    // 验证业务规则（必须有诊断）
    if (!MedicalCaseRules.CanComplete(medicalCase))
    {
        throw new ValidationException("缺少诊断信息，无法完成");
    }

    // 状态迁移
    medicalCase.Status = MedicalCaseStatus.Completed;
    await _repository.UpdateAsync(medicalCase);
}

// 场景3：关闭医案（Pending/InProgress → Closed）
// Trigger: CloseCaseAsync()
public async Task CloseCaseAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdAsync(id);

    // 验证状态迁移合法性
    if (!IsValidStatusTransition(medicalCase.Status, MedicalCaseStatus.Closed))
    {
        throw new ValidationException($"无效的状态迁移: {medicalCase.Status} → Closed");
    }

    // 状态迁移（无额外业务规则验证）
    medicalCase.Status = MedicalCaseStatus.Closed;
    await _repository.UpdateAsync(medicalCase);
}

// 场景4：非法迁移示例（Completed → InProgress）
public async Task ReopenCompletedCase(Guid id)
{
    var medicalCase = await _repository.GetByIdAsync(id);

    // 尝试从终态迁移到工作状态（非法）
    if (!IsValidStatusTransition(medicalCase.Status, MedicalCaseStatus.InProgress))
    {
        throw new ValidationException(
            $"终态医案不可重新打开: {medicalCase.Status} → InProgress"
        );
    }
}
```

---

## 第8章：DTO设计与AutoMapper映射

### 8.1 核心DTO定义（8个）

#### 8.1.1 MedicalCaseDto（医案DTO）

```csharp
/// <summary>
/// 医案DTO（基础信息）
/// </summary>
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // 关联数据（可选）
    public ConsultationDetailDto? Consultation { get; set; }
    public PrescriptionDetailDto? Prescription { get; set; }
}
```

#### 8.1.2 ConsultationDetailDto（诊断详情DTO）

```csharp
/// <summary>
/// 诊断详情DTO
/// </summary>
public class ConsultationDetailDto
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }

    // 基本诊断
    public string ChiefComplaint { get; set; } = string.Empty;       // 主诉
    public string? PresentIllness { get; set; }                      // 现病史

    // 四诊（中医诊断方法）
    public string? Inspection { get; set; }                          // 望诊
    public string? Auscultation { get; set; }                        // 闻诊
    public string? Inquiry { get; set; }                             // 问诊
    public string? Palpation { get; set; }                           // 切诊

    // 中医诊断与治法
    public string? TcmDiagnosis { get; set; }                        // 中医诊断
    public string? TreatmentMethod { get; set; }                     // 治法

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### 8.1.3 PrescriptionDetailDto（处方详情DTO）

```csharp
/// <summary>
/// 处方详情DTO
/// </summary>
public class PrescriptionDetailDto
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }

    // 处方基本信息
    public List<PrescriptionItemDto> Items { get; set; } = new();    // 药材列表
    public string? UsageInstructions { get; set; }                   // 用法说明

    // 计算字段
    public decimal TotalDosage { get; set; }                         // 总剂量（克）
    public decimal EstimatedPrice { get; set; }                      // 估算价格

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### 8.1.4 PrescriptionItemDto（处方条目DTO）

```csharp
/// <summary>
/// 处方条目DTO（实现IHerbItem接口）
/// </summary>
public class PrescriptionItemDto : IHerbItem
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }

    // IHerbItem接口实现
    public int HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Dosage { get; set; }                              // 单剂剂量
    public string Unit { get; set; } = "克";
    public decimal Quantity { get; set; }                            // 剂数
    public decimal UnitPrice { get; set; }                           // 单价
}
```

#### 8.1.5 CreateMedicalCaseDto（创建医案请求）

```csharp
/// <summary>
/// 创建医案请求DTO
/// </summary>
public class CreateMedicalCaseDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public MedicalCaseStatus Status { get; set; } = MedicalCaseStatus.Pending;
}
```

#### 8.1.6 UpdateConsultationRequest（更新诊断请求）

```csharp
/// <summary>
/// 更新诊断请求DTO
/// </summary>
public class UpdateConsultationRequest
{
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? PresentIllness { get; set; }
    public string? Inspection { get; set; }
    public string? Auscultation { get; set; }
    public string? Inquiry { get; set; }
    public string? Palpation { get; set; }
    public string? TcmDiagnosis { get; set; }
    public string? TreatmentMethod { get; set; }
}
```

#### 8.1.7 CreatePrescriptionRequest（创建处方请求）

```csharp
/// <summary>
/// 创建处方请求DTO
/// </summary>
public class CreatePrescriptionRequest
{
    public List<PrescriptionItemDto> Items { get; set; } = new();
    public string? UsageInstructions { get; set; }
}
```

#### 8.1.8 SetPrescriptionFlagRequest（设置处方标志请求）

```csharp
/// <summary>
/// 设置处方标志请求DTO
/// </summary>
public class SetPrescriptionFlagRequest
{
    public bool HasPrescription { get; set; }
}
```

### 8.2 AutoMapper映射配置

```csharp
/// <summary>
/// 医案模块AutoMapper映射配置
/// </summary>
public class MedicalCaseMappingProfile : Profile
{
    public MedicalCaseMappingProfile()
    {
        // ========== MedicalCase映射 ==========

        CreateMap<MedicalCaseModel, MedicalCaseDto>()
            .ForMember(dest => dest.PatientName,
                opt => opt.MapFrom(src => src.Patient.Name))
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src => src.Doctor.UserName))
            .ForMember(dest => dest.Consultation,
                opt => opt.MapFrom(src => src.Consultation))
            .ForMember(dest => dest.Prescription,
                opt => opt.MapFrom(src => src.Prescription));

        CreateMap<CreateMedicalCaseDto, MedicalCaseModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ========== Consultation映射 ==========

        CreateMap<ConsultationModel, ConsultationDetailDto>();

        CreateMap<UpdateConsultationRequest, ConsultationModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

        // ========== Prescription映射 ==========

        CreateMap<PrescriptionModel, PrescriptionDetailDto>()
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.Items));

        CreateMap<CreatePrescriptionRequest, PrescriptionModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.Items));

        // ========== PrescriptionItem映射 ==========

        CreateMap<PrescriptionItemModel, PrescriptionItemDto>();

        CreateMap<PrescriptionItemDto, PrescriptionItemModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());
    }
}
```

### 8.3 DTO使用示例

```csharp
// 示例1：Entity → DTO（查询场景）
public async Task<MedicalCaseDto> GetByIdAsync(Guid id)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);
    if (medicalCase == null)
        throw new NotFoundException($"医案不存在: {id}");

    // AutoMapper自动映射（含Consultation + Prescription）
    return _mapper.Map<MedicalCaseDto>(medicalCase);
}

// 示例2：DTO → Entity（创建场景）
public async Task<MedicalCaseDto> CreateAsync(CreateMedicalCaseDto dto)
{
    // DTO映射到Entity
    var medicalCase = _mapper.Map<MedicalCaseModel>(dto);

    await _repository.AddAsync(medicalCase);

    // Entity映射回DTO
    return _mapper.Map<MedicalCaseDto>(medicalCase);
}

// 示例3：DTO → Entity（更新场景）
public async Task UpdateConsultationAsync(
    Guid caseId,
    UpdateConsultationRequest request)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId);

    if (medicalCase.Consultation == null)
    {
        // 创建新的Consultation
        medicalCase.Consultation = _mapper.Map<ConsultationModel>(request);
        medicalCase.Consultation.MedicalCaseId = caseId;
    }
    else
    {
        // 更新现有Consultation（覆盖非忽略字段）
        _mapper.Map(request, medicalCase.Consultation);
    }

    await _repository.UpdateAsync(medicalCase);
}
```

---

## 第9章：API端点设计（Controller层）

### 9.1 MedicalCasesController定义

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // 需要JWT认证
public class MedicalCasesController : ControllerBase
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ILogger<MedicalCasesController> _logger;

    public MedicalCasesController(
        IMedicalCaseService medicalCaseService,
        ILogger<MedicalCasesController> logger)
    {
        _medicalCaseService = medicalCaseService;
        _logger = logger;
    }

    // ========== 基础CRUD（5个端点） ==========

    /// <summary>
    /// 创建医案
    /// </summary>
    /// <remarks>
    /// POST /api/v1/medicalcases
    /// 业务规则：患者只能有一个未完成医案
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(MedicalCaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMedicalCase([FromBody] CreateMedicalCaseDto dto)
    {
        try
        {
            var medicalCase = await _medicalCaseService.CreateAsync(dto);
            return CreatedAtAction(
                nameof(GetMedicalCase),
                new { id = medicalCase.Id },
                medicalCase
            );
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 按ID查询医案详情
    /// </summary>
    /// <remarks>
    /// GET /api/v1/medicalcases/{id}
    /// 返回：医案基本信息 + Consultation + Prescription
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MedicalCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedicalCase(Guid id)
    {
        var medicalCase = await _medicalCaseService.GetByIdAsync(id);
        if (medicalCase == null)
            return NotFound(new { error = $"医案不存在: {id}" });

        return Ok(medicalCase);
    }

    /// <summary>
    /// 分页查询医案列表
    /// </summary>
    /// <remarks>
    /// GET /api/v1/medicalcases?pageIndex=1&amp;pageSize=10&amp;patientId=xxx&amp;status=InProgress
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MedicalCaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedicalCases(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] MedicalCaseStatus? status = null)
    {
        var result = await _medicalCaseService.GetListAsync(
            pageIndex, pageSize, patientId, doctorId, status
        );
        return Ok(result);
    }

    /// <summary>
    /// 更新医案基本信息
    /// </summary>
    /// <remarks>
    /// PUT /api/v1/medicalcases/{id}
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MedicalCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMedicalCase(
        Guid id,
        [FromBody] UpdateMedicalCaseDto dto)
    {
        try
        {
            var medicalCase = await _medicalCaseService.UpdateAsync(id, dto);
            return Ok(medicalCase);
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = $"医案不存在: {id}" });
        }
    }

    /// <summary>
    /// 删除医案（仅Pending状态可删除）
    /// </summary>
    /// <remarks>
    /// DELETE /api/v1/medicalcases/{id}
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMedicalCase(Guid id)
    {
        try
        {
            await _medicalCaseService.DeleteAsync(id);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========== 诊断管理（1个端点） ==========

    /// <summary>
    /// 更新诊断记录
    /// </summary>
    /// <remarks>
    /// PUT /api/v1/medicalcases/{id}/consultation
    /// 自动状态迁移：Pending → InProgress
    /// </remarks>
    [HttpPut("{id}/consultation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConsultation(
        Guid id,
        [FromBody] UpdateConsultationRequest request)
    {
        try
        {
            await _medicalCaseService.UpdateConsultationAsync(id, request);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ========== 处方管理（4个端点） ==========

    /// <summary>
    /// 设置处方标志（是否需要开方）
    /// </summary>
    /// <remarks>
    /// PUT /api/v1/medicalcases/{id}/prescription-flag
    /// </remarks>
    [HttpPut("{id}/prescription-flag")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPrescriptionFlag(
        Guid id,
        [FromBody] SetPrescriptionFlagRequest request)
    {
        await _medicalCaseService.SetPrescriptionFlagAsync(id, request);
        return NoContent();
    }

    /// <summary>
    /// 创建处方
    /// </summary>
    /// <remarks>
    /// POST /api/v1/medicalcases/{id}/prescription
    /// 前置条件：必须有诊断记录
    /// </remarks>
    [HttpPost("{id}/prescription")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePrescription(
        Guid id,
        [FromBody] CreatePrescriptionRequest request)
    {
        try
        {
            var prescription = await _medicalCaseService.CreatePrescriptionAsync(id, request);
            return CreatedAtAction(
                nameof(GetMedicalCase),
                new { id },
                prescription
            );
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新处方
    /// </summary>
    /// <remarks>
    /// PUT /api/v1/medicalcases/{id}/prescription
    /// </remarks>
    [HttpPut("{id}/prescription")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePrescription(
        Guid id,
        [FromBody] UpdatePrescriptionRequest request)
    {
        try
        {
            var prescription = await _medicalCaseService.UpdatePrescriptionAsync(id, request);
            return Ok(prescription);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 删除处方
    /// </summary>
    /// <remarks>
    /// DELETE /api/v1/medicalcases/{id}/prescription
    /// 需权限检查：CanDeletePrescription
    /// </remarks>
    [HttpDelete("{id}/prescription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePrescription(Guid id)
    {
        try
        {
            await _medicalCaseService.DeletePrescriptionAsync(id);
            return NoContent();
        }
        catch (UnauthorizedException ex)
        {
            return Forbid();
        }
    }

    // ========== 状态管理（3个端点） ==========

    /// <summary>
    /// 更新医案状态
    /// </summary>
    /// <remarks>
    /// PUT /api/v1/medicalcases/{id}/status
    /// 带状态迁移验证
    /// </remarks>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request)
    {
        try
        {
            await _medicalCaseService.UpdateStatusAsync(id, request.Status);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 完成医案（终态：Completed）
    /// </summary>
    /// <remarks>
    /// POST /api/v1/medicalcases/{id}/complete
    /// 前置条件：必须有诊断记录
    /// </remarks>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteMedicalCase(Guid id)
    {
        try
        {
            await _medicalCaseService.CompleteAsync(id);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 关闭医案（终态：Closed）
    /// </summary>
    /// <remarks>
    /// POST /api/v1/medicalcases/{id}/close
    /// </remarks>
    [HttpPost("{id}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CloseMedicalCase(Guid id)
    {
        await _medicalCaseService.CloseCaseAsync(id);
        return NoContent();
    }

    // ========== 查询方法（1个端点） ==========

    /// <summary>
    /// 获取患者未完成医案（用于"继续看诊"功能）
    /// </summary>
    /// <remarks>
    /// GET /api/v1/medicalcases/patient/{patientId}/unfinished
    /// </remarks>
    [HttpGet("patient/{patientId}/unfinished")]
    [ProducesResponseType(typeof(MedicalCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnfinishedCase(Guid patientId)
    {
        var medicalCase = await _medicalCaseService.GetUnfinishedCaseByPatientIdAsync(patientId);
        if (medicalCase == null)
            return NotFound(new { error = "患者无未完成医案" });

        return Ok(medicalCase);
    }
}
```

### 9.2 API端点总览

| 方法 | 路由 | 功能 | 权限 |
|-----|------|------|------|
| POST | /api/v1/medicalcases | 创建医案 | 需认证 |
| GET | /api/v1/medicalcases/{id} | 查询详情 | 需认证 |
| GET | /api/v1/medicalcases | 分页查询 | 需认证 |
| PUT | /api/v1/medicalcases/{id} | 更新基本信息 | 需认证 |
| DELETE | /api/v1/medicalcases/{id} | 删除医案 | 需认证 |
| PUT | /api/v1/medicalcases/{id}/consultation | 更新诊断 | 需认证 |
| PUT | /api/v1/medicalcases/{id}/prescription-flag | 设置处方标志 | 需认证 |
| POST | /api/v1/medicalcases/{id}/prescription | 创建处方 | 需认证 |
| PUT | /api/v1/medicalcases/{id}/prescription | 更新处方 | 需认证 |
| DELETE | /api/v1/medicalcases/{id}/prescription | 删除处方 | 需认证+权限 |
| PUT | /api/v1/medicalcases/{id}/status | 更新状态 | 需认证 |
| POST | /api/v1/medicalcases/{id}/complete | 完成医案 | 需认证 |
| POST | /api/v1/medicalcases/{id}/close | 关闭医案 | 需认证 |
| GET | /api/v1/medicalcases/patient/{patientId}/unfinished | 未完成医案 | 需认证 |

---

## 第10章：FluentValidation验证与异常处理

### 10.1 FluentValidation验证器

#### 10.1.1 CreateMedicalCaseDtoValidator

```csharp
/// <summary>
/// 创建医案DTO验证器
/// </summary>
public class CreateMedicalCaseDtoValidator : AbstractValidator<CreateMedicalCaseDto>
{
    public CreateMedicalCaseDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("医生ID不能为空");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("医案状态无效");
    }
}
```

#### 10.1.2 UpdateConsultationRequestValidator

```csharp
/// <summary>
/// 更新诊断请求验证器
/// </summary>
public class UpdateConsultationRequestValidator : AbstractValidator<UpdateConsultationRequest>
{
    public UpdateConsultationRequestValidator()
    {
        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .MaximumLength(500).WithMessage("主诉不能超过500字");

        RuleFor(x => x.PresentIllness)
            .MaximumLength(2000).WithMessage("现病史不能超过2000字")
            .When(x => !string.IsNullOrWhiteSpace(x.PresentIllness));

        RuleFor(x => x.TcmDiagnosis)
            .NotEmpty().WithMessage("中医诊断不能为空")
            .MaximumLength(200).WithMessage("中医诊断不能超过200字");

        RuleFor(x => x.TreatmentMethod)
            .MaximumLength(200).WithMessage("治法不能超过200字")
            .When(x => !string.IsNullOrWhiteSpace(x.TreatmentMethod));
    }
}
```

#### 10.1.3 CreatePrescriptionRequestValidator

```csharp
/// <summary>
/// 创建处方请求验证器
/// </summary>
public class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("处方药材列表不能为空")
            .Must(items => items.Count >= 1).WithMessage("至少需要一味药材");

        RuleForEach(x => x.Items).SetValidator(new PrescriptionItemDtoValidator());

        RuleFor(x => x.UsageInstructions)
            .MaximumLength(500).WithMessage("用法说明不能超过500字")
            .When(x => !string.IsNullOrWhiteSpace(x.UsageInstructions));
    }
}

/// <summary>
/// 处方条目验证器
/// </summary>
public class PrescriptionItemDtoValidator : AbstractValidator<PrescriptionItemDto>
{
    public PrescriptionItemDtoValidator()
    {
        RuleFor(x => x.HerbId)
            .GreaterThan(0).WithMessage("药材ID无效");

        RuleFor(x => x.HerbName)
            .NotEmpty().WithMessage("药材名称不能为空");

        RuleFor(x => x.Dosage)
            .GreaterThan(0).WithMessage("剂量必须大于0")
            .LessThanOrEqualTo(1000).WithMessage("剂量不能超过1000克");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("剂数必须大于0")
            .LessThanOrEqualTo(100).WithMessage("剂数不能超过100");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("单位不能为空")
            .Must(unit => new[] { "克", "g", "两" }.Contains(unit))
            .WithMessage("单位必须是：克、g、两");
    }
}
```

### 10.2 异常处理策略

#### 10.2.1 自定义异常类型

```csharp
/// <summary>
/// 验证异常（业务规则违反）
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// 未找到异常（资源不存在）
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// 未授权异常（权限不足）
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
```

#### 10.2.2 全局异常处理中间件

```csharp
/// <summary>
/// 全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Message = exception.Message,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                _logger.LogWarning(exception, "Validation error occurred");
                break;

            case NotFoundException:
                response.StatusCode = StatusCodes.Status404NotFound;
                _logger.LogWarning(exception, "Resource not found");
                break;

            case UnauthorizedException:
                response.StatusCode = StatusCodes.Status403Forbidden;
                _logger.LogWarning(exception, "Unauthorized access attempt");
                break;

            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                _logger.LogError(exception, "Unhandled exception occurred");
                errorResponse.Message = "服务器内部错误";
                break;
        }

        await response.WriteAsJsonAsync(errorResponse);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

---

## 第11章：依赖注入配置

### 11.1 MedicalCaseModule依赖注入

```csharp
/// <summary>
/// 医案模块依赖注入扩展
/// </summary>
public static class MedicalCaseModule
{
    /// <summary>
    /// 注册医案模块的所有服务
    /// </summary>
    public static IServiceCollection AddMedicalCaseModule(
        this IServiceCollection services)
    {
        // ========== Repository层 ==========
        services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();

        // ========== Service层 ==========
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();

        // ========== AutoMapper ==========
        services.AddAutoMapper(typeof(MedicalCaseMappingProfile));

        // ========== FluentValidation ==========
        services.AddValidatorsFromAssemblyContaining<CreateMedicalCaseDtoValidator>();

        return services;
    }
}
```

### 11.2 Startup.cs集成

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 基础设施配置
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"))
        );

        // 注册医案模块（自动注册仓储+服务+验证器）
        services.AddMedicalCaseModule();

        // 注册其他模块
        services.AddPatientsModule();
        services.AddConsultationModule();
        services.AddPrescriptionsModule();

        // JWT认证
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        // API Controllers
        services.AddControllers()
            .AddFluentValidation(); // FluentValidation集成

        // Swagger文档
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LYBT WebAPI - 医案管理模块",
                Version = "v1",
                Description = "医疗案例管理API接口文档"
            });

            // 包含XML注释
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API v1"));
        }

        // 全局异常处理中间件
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseHttpsRedirection();
        app.UseRouting();

        // 认证与授权
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

---

## 第12章：参考资料

### 12.1 内部文档

**架构设计**：
- [Server端三层架构](./README.md) - Server端架构总览
- [三层对齐架构](../../README.md) - v5.0三层对齐架构说明
- [Client端医案管理设计](../client/medical-case-design.md) - Client端对应设计（Task 7）

**接口层设计**：
- [Server端架构说明](./README.md) - 模块化接口设计（当前架构）
- [Server端Interfaces层设计（已归档）](../../../archive/v1.0/interfaces-layer-design.md) - 中心化接口架构（历史参考）

**DTO设计**：
- [DTO设计标准](../../shared/dto-design-standard.md) - DTO设计原则与命名规范

**业务规则**：
- [业务规则文档](../../../../business-rules.md) - 医案管理核心业务规则

**开发指南**：
- [Server端开发指南](../../../../development/server/) - 服务层开发规范

### 12.2 技术栈文档

**.NET 8**：
- [官方文档](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [ASP.NET Core 8](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0)

**Entity Framework Core 8**：
- [官方文档](https://learn.microsoft.com/en-us/ef/core/)
- [Include策略](https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager)

**AutoMapper 13.x**：
- [官方文档](https://docs.automapper.org/en/stable/)
- [映射配置](https://docs.automapper.org/en/stable/Configuration.html)

**FluentValidation 11.x**：
- [官方文档](https://docs.fluentvalidation.net/en/latest/)
- [ASP.NET Core集成](https://docs.fluentvalidation.net/en/latest/aspnet.html)

### 12.3 设计模式

**聚合根模式（DDD）**：
- [Domain-Driven Design Reference](https://www.domainlanguagecom/ddd/reference/)
- [Aggregate Pattern](https://martinfowler.com/bliki/DDD_Aggregate.html)

**Repository模式**：
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Microsoft Architecture Guide](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)

**状态机模式**：
- [State Pattern](https://refactoring.guru/design-patterns/state)
- [Finite State Machine](https://martinfowler.com/bliki/FiniteStateMachine.html)

---

**文档维护**：Server端开发组
**最后更新**：2025-10-29
**文档版本**：v1.0
