# 处方管理系统架构设计

**文档类型**: Explanation - Architecture Overview
**最后更新**: 2025-11-22
**维护者**: Claude Code + lybtzyzs-doc-sync
**相关文档**: [处方管理教程](../../../tutorials/modules/prescriptions/prescription-management-tutorial.md) | [Prescriptions API](../../../reference/api/prescriptions-api.md)

---

## 1. 模块概述

### 1.1 业务定位

处方管理系统是LYBTZYZS的**核心业务模块**，承接着中医诊断（Consultation）和药材管理（Herbs）的关键环节，是诊疗工作流的核心交付物。

```
[患者档案] → [医疗案例] → [中医诊断] → [处方管理] → [药材库存/收费管理/处方打印]
                                           ↑
                                    核心业务模块
```

**模块特点**：
- **业务核心**：中医诊所的最终交付物，处方是诊疗流程的核心输出
- **数据聚合**：整合诊断信息、药材信息、验方知识、价格体系
- **合规要求**：处方编号、打印版本、医师签名、审核流程等合规功能
- **经验积累**：通过处方统计分析积累临床用药经验

### 1.2 核心职责

Prescription模块承担以下8项核心职责：

| 职责编号 | 职责名称 | 说明 | 相关功能 |
|---------|---------|------|---------|
| **R1** | 处方创建管理 | 创建、更新、删除处方（通过聚合根） | CreatePrescription, UpdatePrescription |
| **R2** | 验方集成应用 | 经典方剂、经验方剂、协定方剂导入 | CreateFromFormula, ApplyModifications |
| **R3** | 价格计算体系 | 自动计算总价、折扣管理、收费统计 | CalculateTotalPrice, SuggestDiscount |
| **R4** | 处方打印管理 | 打印版本控制、打印日志、格式生成 | PrintPrescription, GeneratePrintData |
| **R5** | 处方审核流程 | 药材数量、剂量、配伍禁忌、价格审核 | AuditPrescription, ValidateRules |
| **R6** | 处方查询服务 | 按患者、病案、病症关键字查询 | GetById, Search, GetPatientRecent |
| **R7** | 处方统计分析 | 按状态、日期、药材、验方统计 | GetStatistics, AnalyzeTrends |
| **R8** | 历史记录追踪 | 患者用药历史、处方作废、修改记录 | GetPatientHistory, VoidPrescription |

### 1.3 在系统中的地位

**三步诊疗工作流**：
1. **Step 1 - 辨证信息采集**（Consultation模块）：望闻问切四诊合参
2. **Step 2 - 处方需求确认**（MedicalCase模块）：NeedsPrescription标记
3. **Step 3 - 处方具体开具**（Prescription模块）：创建和管理具体处方

**核心价值主张**：
- ✅ **验方集成**：支持经典方剂快速导入和个性化调整
- ✅ **智能定价**：自动计算处方总价，支持折扣设置
- ✅ **版本管理**：处方打印版本控制，支持修改重印
- ✅ **合规保障**：处方编号、医师签名、打印日志等合规功能

---

## 2. 三层架构设计

### 2.1 Desktop Layer（WPF客户端）

#### 2.1.1 MVVM架构

**PrescriptionViewModel**（处方编辑视图模型）：
```csharp
public class PrescriptionViewModel : ViewModelBase
{
    private readonly IPrescriptionBusinessService _businessService;
    private readonly IFormulaBusinessService _formulaService;

    // 处方基础信息
    public Guid MedicalCaseId { get; set; }
    public string PrescriptionNumber { get; set; }
    public string Indication { get; set; }
    public int DosageCount { get; set; }
    public decimal Discount { get; set; }

    // 处方明细（ObservableCollection支持UI绑定）
    public ObservableCollection<PrescriptionItemViewModel> Items { get; set; }

    // 验方集成
    public ICommand SelectFormulaCommand { get; }
    public ICommand ApplyFormulaCommand { get; }

    // 处方操作
    public ICommand SavePrescriptionCommand { get; }
    public ICommand PrintPrescriptionCommand { get; }
    public ICommand AuditPrescriptionCommand { get; }

    // 价格计算（实时更新）
    public decimal PerDosePrice => Items.Sum(item => item.Amount);
    public decimal TotalPrice => PerDosePrice * DosageCount * Discount;
}
```

**PrescriptionItemViewModel**（处方项视图模型）：
```csharp
public class PrescriptionItemViewModel : ViewModelBase
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public decimal UnitPrice { get; set; }

    // 自动计算小计（响应式）
    public decimal Amount => UnitPrice * Quantity;
}
```

#### 2.1.2 Service Layer

**PrescriptionBusinessService**（业务服务）：
```csharp
public class PrescriptionBusinessService : IPrescriptionBusinessService
{
    private readonly IMedicalCaseApi _medicalCaseApi;  // 通过聚合根创建处方
    private readonly IPrescriptionApi _prescriptionApi; // 处方查询
    private readonly IFormulaApi _formulaApi;          // 验方查询

    // Write操作：通过MedicalCase聚合根
    public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionRequest request)
    {
        // AR-001: 通过聚合根创建
        return await _medicalCaseApi.CreatePrescriptionAsync(request.MedicalCaseId, request);
    }

    public async Task<PrescriptionDto> UpdatePrescriptionAsync(UpdatePrescriptionRequest request)
    {
        // AR-001: 通过聚合根更新
        return await _medicalCaseApi.UpdatePrescriptionAsync(request.MedicalCaseId, request);
    }

    // Read操作：直接查询
    public async Task<PrescriptionDto> GetPrescriptionByIdAsync(Guid id)
    {
        return await _prescriptionApi.GetByIdAsync(id);
    }

    public async Task<List<PrescriptionSearchResultDto>> SearchPrescriptionsAsync(string keyword)
    {
        return await _prescriptionApi.SearchAsync(keyword);
    }

    // 验方集成
    public async Task<PrescriptionDto> CreateFromFormulaAsync(CreateFromFormulaRequest request)
    {
        var formula = await _formulaApi.GetByIdAsync(request.FormulaId);
        request.Items = MapFormulaItemsToPrescriptionItems(formula.Items);
        return await CreatePrescriptionAsync(request);
    }
}
```

### 2.2 Server Layer（ASP.NET Core）

#### 2.2.1 Controller Layer（两个Controller）

**PrescriptionsController**（Read-only查询接口）：
```csharp
[ApiController]
[Route("api/v1/prescriptions")]
public class PrescriptionsController : ControllerBase
{
    // Read Layer - 4个查询端点

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> GetById(Guid id)
    {
        // 获取处方详情（包含处方项）
    }

    [HttpGet("medicalcase/{medicalCaseId}")]
    public async Task<ActionResult<ApiResponse<List<PrescriptionDto>>>> GetByMedicalCase(Guid medicalCaseId)
    {
        // 获取病案的所有处方
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> Search(
        [FromQuery] string? patientName,
        [FromQuery] string? symptomKeyword)
    {
        // REQ-2: 按病症关键字搜索处方
    }

    [HttpGet("patient/{patientId}/recent")]
    public async Task<ActionResult<ApiResponse<List<PrescriptionSearchResultDto>>>> GetPatientRecent(
        Guid patientId,
        [FromQuery] int count = 5)
    {
        // REQ-1: 获取患者最近处方
    }
}
```

**MedicalCaseController**（Write操作通过聚合根）：
```csharp
[ApiController]
[Route("api/v1/medicalcases")]
public class MedicalCaseController : ControllerBase
{
    // Write Layer - 3个处方写操作端点（AR-001聚合根约束）

    [HttpPost("{id}/prescriptions")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription(
        Guid id,
        [FromBody] CreatePrescriptionRequest request)
    {
        // AR-001: 通过聚合根创建处方
        // AR-003: 验证一诊一方约束
    }

    [HttpPut("{id}/prescriptions/{prescriptionId}")]
    public async Task<ActionResult<ApiResponse<PrescriptionDto>>> UpdatePrescription(
        Guid id,
        Guid prescriptionId,
        [FromBody] UpdatePrescriptionRequest request)
    {
        // AR-001: 通过聚合根更新处方
    }

    [HttpDelete("{id}/prescriptions/{prescriptionId}")]
    public async Task<ActionResult<ApiResponse>> DeletePrescription(
        Guid id,
        Guid prescriptionId)
    {
        // AR-001: 通过聚合根删除处方（软删除）
    }
}
```

#### 2.2.2 Service Layer

**PrescriptionService**（处方业务服务）：
```csharp
public class PrescriptionService : IPrescriptionService
{
    private readonly IRepository<Prescription> _repository;
    private readonly IRepository<Herb> _herbRepository;
    private readonly IRepository<Formula> _formulaRepository;

    // Write操作（通过MedicalCaseService调用）
    public async Task<PrescriptionDto> CreatePrescriptionAsync(
        Guid medicalCaseId,
        CreatePrescriptionRequest request)
    {
        // 1. 验证前置条件
        await ValidateCanCreatePrescriptionAsync(medicalCaseId);

        // 2. 生成处方编号（BR-001）
        var prescriptionNumber = await GeneratePrescriptionNumberAsync();

        // 3. 创建处方实体
        var prescription = new Prescription
        {
            MedicalCaseId = medicalCaseId,
            PrescriptionNumber = prescriptionNumber,
            Indication = request.Indication,
            DosageCount = request.DosageCount,
            Discount = request.Discount,
            Status = PrescriptionStatus.Draft
        };

        // 4. 添加处方项（验证药材存在性）
        await AddPrescriptionItemsAsync(prescription, request.Items);

        // 5. 计算总价（BR-002）
        prescription.CalculateTotalPrice();

        // 6. 保存处方
        await _repository.AddAsync(prescription);
        await _repository.SaveChangesAsync();

        return MapToDto(prescription);
    }

    // Read操作（直接查询）
    public async Task<PrescriptionDto> GetByIdAsync(Guid id)
    {
        var prescription = await _repository
            .GetByConditionAsync(p => p.Id == id,
                                include: p => p.Include(p => p.Items));

        return MapToDto(prescription);
    }

    // 验方集成
    public async Task<PrescriptionDto> CreateFromFormulaAsync(
        Guid medicalCaseId,
        CreateFromFormulaRequest request)
    {
        // 1. 获取验方信息
        var formula = await _formulaRepository
            .GetByConditionAsync(f => f.Id == request.FormulaId,
                                include: f => f.Include(f => f.Items));

        // 2. 创建处方（使用验方信息）
        var prescription = new Prescription
        {
            MedicalCaseId = medicalCaseId,
            Indication = formula.Indication,
            FormulaSource = formula.Name,
            ReferencedFormulas = formula.Name
        };

        // 3. 添加验方药材（更新为当前价格）
        foreach (var formulaItem in formula.Items)
        {
            var currentHerb = await _herbRepository.GetByIdAsync(formulaItem.HerbId);
            prescription.Items.Add(new PrescriptionItem
            {
                HerbId = currentHerb.Id,
                HerbName = currentHerb.Name,
                Quantity = formulaItem.Quantity,
                UnitPrice = currentHerb.UnitPrice  // 使用当前价格
            });
        }

        // 4. 应用个性化调整
        await ApplyFormulaModificationsAsync(prescription, request.Modifications);

        return await CreatePrescriptionAsync(medicalCaseId, MapToCreateRequest(prescription));
    }

    // 价格计算（BR-002）
    public decimal CalculateTotalPrice(Prescription prescription)
    {
        var perDosePrice = prescription.Items.Sum(item => item.Amount);
        return perDosePrice * prescription.DosageCount * prescription.Discount;
    }
}
```

### 2.3 Database Layer（SQL Server）

#### 2.3.1 核心表结构

**Prescriptions表**（处方主表）：
```sql
CREATE TABLE Prescriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    MedicalCaseId UNIQUEIDENTIFIER NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    PrescriptionNumber NVARCHAR(50) NOT NULL UNIQUE,  -- RX-YYYYMMDD-NNNN

    -- 处方内容
    Indication NVARCHAR(500),                         -- 主治
    DosageCount INT NOT NULL DEFAULT 7,               -- 帖数
    Usage NVARCHAR(500),                              -- 用法
    Advice NVARCHAR(1000),                            -- 医嘱

    -- 验方关联
    FormulaSource NVARCHAR(200),                      -- 验方来源
    ReferencedFormulas NVARCHAR(500),                 -- 引用验方列表

    -- 价格管理
    Discount DECIMAL(5,2) NOT NULL DEFAULT 1.0,       -- 折扣（0-1）
    TotalAmount DECIMAL(10,2) NOT NULL,               -- 总金额

    -- 状态管理
    Status INT NOT NULL DEFAULT 0,                    -- Draft/Active/Printed/Completed/Cancelled
    IsPrinted BIT NOT NULL DEFAULT 0,                 -- 是否已打印

    -- 打印版本控制
    PrintVersion INT NOT NULL DEFAULT 1,              -- 打印版本号
    LastPrintedAt DATETIME2,                          -- 最后打印时间
    PrintCount INT NOT NULL DEFAULT 0,                -- 打印次数

    -- 审计字段
    Remark NVARCHAR(1000),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedBy UNIQUEIDENTIFIER,

    CONSTRAINT FK_Prescriptions_MedicalCases FOREIGN KEY (MedicalCaseId)
        REFERENCES MedicalCases(Id),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId)
        REFERENCES Patients(Id),
    CONSTRAINT FK_Prescriptions_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
);

-- 索引优化
CREATE INDEX IX_Prescriptions_MedicalCaseId ON Prescriptions(MedicalCaseId);
CREATE INDEX IX_Prescriptions_PatientId ON Prescriptions(PatientId);
CREATE INDEX IX_Prescriptions_PrescriptionNumber ON Prescriptions(PrescriptionNumber);
CREATE INDEX IX_Prescriptions_CreatedAt ON Prescriptions(CreatedAt DESC);
```

**PrescriptionItems表**（处方项）：
```sql
CREATE TABLE PrescriptionItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    HerbId UNIQUEIDENTIFIER NOT NULL,
    HerbName NVARCHAR(100) NOT NULL,

    -- 数量和价格
    Specification NVARCHAR(50),                       -- 规格（如"10g"）
    Quantity INT NOT NULL,                            -- 数量
    Unit NVARCHAR(20) NOT NULL DEFAULT 'g',           -- 单位
    UnitPrice DECIMAL(10,2) NOT NULL,                 -- 单价
    Amount DECIMAL(10,2) NOT NULL,                    -- 小计 = 单价 × 数量

    -- 用法说明
    Usage NVARCHAR(200),                              -- 特殊用法（如"先煎"）
    Remark NVARCHAR(500),

    CONSTRAINT FK_PrescriptionItems_Prescriptions FOREIGN KEY (PrescriptionId)
        REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PrescriptionItems_Herbs FOREIGN KEY (HerbId)
        REFERENCES Herbs(Id)
);

-- 索引优化
CREATE INDEX IX_PrescriptionItems_PrescriptionId ON PrescriptionItems(PrescriptionId);
CREATE INDEX IX_PrescriptionItems_HerbId ON PrescriptionItems(HerbId);
```

**PrescriptionPrintLogs表**（打印日志）：
```sql
CREATE TABLE PrescriptionPrintLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    PrintVersion INT NOT NULL,
    PrintedAt DATETIME2 NOT NULL,
    PrintedBy UNIQUEIDENTIFIER NOT NULL,
    PrinterName NVARCHAR(100),
    PrintReason NVARCHAR(200),                        -- 打印原因（重印时必填）

    CONSTRAINT FK_PrintLogs_Prescriptions FOREIGN KEY (PrescriptionId)
        REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PrintLogs_Users FOREIGN KEY (PrintedBy)
        REFERENCES Users(Id)
);

-- 索引优化
CREATE INDEX IX_PrintLogs_PrescriptionId ON PrescriptionPrintLogs(PrescriptionId);
CREATE INDEX IX_PrintLogs_PrintedAt ON PrescriptionPrintLogs(PrintedAt DESC);
```

---

## 3. 核心领域模型

### 3.1 Prescription实体（处方）

**完整实体定义**：
```csharp
public class Prescription : BaseEntity
{
    // ========== 基础关联 ==========
    public Guid MedicalCaseId { get; set; }           // 医疗案例ID
    public Guid PatientId { get; set; }               // 患者ID
    public Guid UserId { get; set; }                  // 医生ID
    public string PrescriptionNumber { get; set; }    // 处方编号：RX-YYYYMMDD-NNNN

    // ========== 处方内容 ==========
    public string? Indication { get; set; }           // 主治（适应症）
    public int DosageCount { get; set; }              // 处方帖数（默认7帖）
    public string? Usage { get; set; }                // 用法（如"水煎服，每日一剂"）
    public string? Advice { get; set; }               // 医嘱

    // ========== 验方关联 ==========
    public string? FormulaSource { get; set; }        // 验方来源（如"伤寒论"）
    public string? ReferencedFormulas { get; set; }   // 引用验方列表（逗号分隔）

    // ========== 价格管理 ==========
    public decimal Discount { get; set; } = 1.0m;     // 折扣（0-1之间）
    public decimal TotalAmount { get; set; }          // 总金额（冗余字段，加速查询）

    // ========== 状态管理 ==========
    public PrescriptionStatus Status { get; set; }    // 处方状态
    public bool IsPrinted { get; set; }               // 是否已打印

    // ========== 打印版本控制 ==========
    public int PrintVersion { get; set; } = 1;        // 当前打印版本号
    public DateTime? LastPrintedAt { get; set; }      // 最后打印时间
    public int PrintCount { get; set; } = 0;          // 打印次数

    // ========== 导航属性 ==========
    public virtual MedicalCase? MedicalCase { get; set; }
    public virtual Patient? Patient { get; set; }
    public virtual User? Doctor { get; set; }
    public virtual ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    public virtual ICollection<PrescriptionPrintLog> PrintLogs { get; set; } = new List<PrescriptionPrintLog>();

    // ========== 业务方法 ==========

    /// <summary>
    /// 计算处方总价（BR-002）
    /// </summary>
    public decimal CalculateTotalPrice()
    {
        var perDosePrice = Items.Sum(item => item.Amount);
        TotalAmount = perDosePrice * DosageCount * Discount;
        return TotalAmount;
    }

    /// <summary>
    /// 验证是否可以修改
    /// </summary>
    public bool CanEdit()
    {
        return Status == PrescriptionStatus.Draft || Status == PrescriptionStatus.Active;
    }

    /// <summary>
    /// 验证是否可以打印
    /// </summary>
    public bool CanPrint()
    {
        return Status != PrescriptionStatus.Cancelled && Items.Any();
    }

    /// <summary>
    /// 记录打印操作（BR-003）
    /// </summary>
    public void RecordPrint(Guid printedBy, string? printerName, string? reason)
    {
        PrintVersion++;
        LastPrintedAt = DateTime.UtcNow;
        PrintCount++;
        IsPrinted = true;
        Status = PrescriptionStatus.Printed;

        PrintLogs.Add(new PrescriptionPrintLog
        {
            Id = Guid.NewGuid(),
            PrescriptionId = Id,
            PrintVersion = PrintVersion,
            PrintedAt = DateTime.UtcNow,
            PrintedBy = printedBy,
            PrinterName = printerName,
            PrintReason = reason
        });
    }
}
```

### 3.2 PrescriptionItem实体（处方项）

```csharp
public class PrescriptionItem
{
    public Guid Id { get; set; }                       // 处方项ID
    public Guid PrescriptionId { get; set; }           // 所属处方ID
    public Guid HerbId { get; set; }                   // 药材ID
    public string HerbName { get; set; }               // 药材名称（冗余字段）

    // ========== 数量和价格 ==========
    public string? Specification { get; set; }         // 规格（如"10g"）
    public int Quantity { get; set; }                  // 用量（整数）
    public string Unit { get; set; } = "g";            // 单位（默认g）
    public decimal UnitPrice { get; set; }             // 单价（来自Herb）
    public decimal Amount { get; set; }                // 小计 = 单价 × 用量

    // ========== 用法说明 ==========
    public string? Usage { get; set; }                 // 特殊用法（如"先煎"、"后下"）
    public string? Remark { get; set; }

    // ========== 导航属性 ==========
    public virtual Prescription? Prescription { get; set; }
    public virtual Herb? Herb { get; set; }

    // ========== 业务方法 ==========

    /// <summary>
    /// 计算小计（响应式）
    /// </summary>
    public void CalculateAmount()
    {
        Amount = UnitPrice * Quantity;
    }
}
```

### 3.3 PrescriptionStatus枚举

```csharp
/// <summary>
/// 处方状态枚举
/// </summary>
public enum PrescriptionStatus
{
    /// <summary>
    /// 草稿 - 正在编辑中
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 激活 - 可用于取药
    /// </summary>
    Active = 1,

    /// <summary>
    /// 已打印 - 已生成正式处方单
    /// </summary>
    Printed = 2,

    /// <summary>
    /// 已完成 - 已取药或结束
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 已取消 - 作废处方
    /// </summary>
    Cancelled = 4
}
```

**状态流转图**：
```
Draft → Active → Printed → Completed
  ↓       ↓         ↓
Cancelled ← Cancelled ← Cancelled
```

---

## 4. 业务规则体系

### 4.1 AR-001: 聚合根约束（Write操作必须通过MedicalCase）

**定义**: 所有对Prescription的**写操作**必须通过MedicalCase聚合根完成。

**Read vs Write分离**:
- ✅ **Read操作**: 4个查询接口可以直接调用PrescriptionsController
- ⚠️ **Write操作**: 创建、更新、删除处方必须通过MedicalCaseController的聚合根端点

**Write操作端点（聚合根）**:
```http
POST   /api/v1/medicalcases/{id}/prescriptions           - 创建处方
PUT    /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}  - 更新处方
DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}  - 删除处方
```

**实现示例**:
```csharp
// ✅ 正确：通过MedicalCase聚合根
public async Task<PrescriptionDto> CreatePrescriptionAsync(
    Guid medicalCaseId,
    CreatePrescriptionRequest request)
{
    // 1. 获取聚合根
    var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);

    // 2. 验证聚合根约束
    if (medicalCase.Prescription != null)
        throw new BusinessException("该病案已存在处方"); // AR-003

    // 3. 通过聚合根创建处方
    var prescription = medicalCase.CreatePrescription(request);

    // 4. 保存聚合根
    await _medicalCaseRepository.UpdateAsync(medicalCase);
    await _medicalCaseRepository.SaveChangesAsync();

    return MapToDto(prescription);
}

// ❌ 错误：绕过聚合根直接操作
await _prescriptionRepository.AddAsync(prescription);  // 违反AR-001
```

### 4.2 AR-003: 一诊一方约束

**定义**: 一个病案只能有一个有效处方。

**验证时机**:
- 创建处方时：检查MedicalCase.Prescription是否为null
- 更新处方时：检查PrescriptionId是否匹配MedicalCase.Prescription.Id

**实现示例**:
```csharp
public async Task ValidateOnePrescripePerCaseAsync(Guid medicalCaseId, Guid? existingPrescriptionId = null)
{
    var medicalCase = await _medicalCaseRepository
        .GetByConditionAsync(m => m.Id == medicalCaseId,
                            include: m => m.Include(m => m.Prescription));

    // 创建时：病案不能已有处方
    if (existingPrescriptionId == null && medicalCase.Prescription != null)
    {
        throw new BusinessException("该病案已存在处方，不能重复创建"); // AR-003
    }

    // 更新时：只能更新病案当前的处方
    if (existingPrescriptionId.HasValue &&
        medicalCase.Prescription?.Id != existingPrescriptionId)
    {
        throw new BusinessException("处方不属于该病案"); // AR-003
    }
}
```

### 4.3 REQ-1: 按患者查询处方

**定义**: 支持查询指定患者的历史处方记录。

**实现端点**:
```http
GET /api/v1/prescriptions/patient/{patientId}/recent?count=10
```

**查询逻辑**:
```csharp
public async Task<List<PrescriptionSearchResultDto>> GetPatientRecentPrescriptionsAsync(
    Guid patientId,
    int count = 5)
{
    // 参数验证
    if (count < 1 || count > 20)
        throw new ArgumentException("返回数量必须在1-20之间");

    // 查询患者最近处方
    var prescriptions = await _repository.GetQueryable()
        .Where(p => p.PatientId == patientId && !p.IsDeleted)
        .OrderByDescending(p => p.CreatedAt)
        .Take(count)
        .ToListAsync();

    return prescriptions.Select(MapToSearchResultDto).ToList();
}
```

**使用场景**:
- 患者复诊时查看历史用药
- 处方历史记录追踪
- 生成患者用药报告

### 4.4 REQ-2: 按病症查询处方

**定义**: 支持按病症关键字搜索处方（用于经验方查询、病症统计等）。

**实现端点**:
```http
GET /api/v1/prescriptions/search?symptomKeyword=风寒
```

**查询逻辑**:
```csharp
public async Task<List<PrescriptionSearchResultDto>> SearchPrescriptionsAsync(
    string? patientName,
    string? symptomKeyword)
{
    // 参数验证
    if (string.IsNullOrEmpty(patientName) && string.IsNullOrEmpty(symptomKeyword))
        throw new ArgumentException("请至少提供一个搜索条件");

    var query = _repository.GetQueryable()
        .Include(p => p.Patient)
        .Where(p => !p.IsDeleted);

    // 按患者姓名搜索
    if (!string.IsNullOrEmpty(patientName))
    {
        query = query.Where(p => p.Patient.Name.Contains(patientName));
    }

    // 按病症关键字搜索（搜索主治或药材名称）
    if (!string.IsNullOrEmpty(symptomKeyword))
    {
        query = query.Where(p =>
            p.Indication.Contains(symptomKeyword) ||
            p.Items.Any(item => item.HerbName.Contains(symptomKeyword)));
    }

    var results = await query
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return results.Select(MapToSearchResultDto).ToList();
}
```

**使用场景**:
- 查询某病症的常用处方
- 经验方统计分析
- 药材用量分析

### 4.5 BR-001: 处方编号生成规则

**定义**: 处方编号格式为 `RX-YYYYMMDD-NNNN`

**格式说明**:
- **RX**: 固定前缀（Prescription的缩写）
- **YYYYMMDD**: 8位日期（如20251122）
- **NNNN**: 4位顺序号（从0001开始，每日重置）

**实现示例**:
```csharp
public async Task<string> GeneratePrescriptionNumberAsync()
{
    var today = DateTime.Today;
    var prefix = $"RX-{today:yyyyMMdd}";

    // 查询今日最大序号
    var lastNumber = await _repository.GetQueryable()
        .Where(p => p.PrescriptionNumber.StartsWith(prefix))
        .OrderByDescending(p => p.PrescriptionNumber)
        .Select(p => p.PrescriptionNumber)
        .FirstOrDefaultAsync();

    int sequence = 1;
    if (!string.IsNullOrEmpty(lastNumber))
    {
        var lastSequence = int.Parse(lastNumber.Substring(prefix.Length + 1));
        sequence = lastSequence + 1;
    }

    return $"{prefix}-{sequence:D4}";
}
```

**示例**:
- `RX-20251122-0001` - 2025年11月22日第1个处方
- `RX-20251122-0025` - 2025年11月22日第25个处方

### 4.6 BR-002: 价格计算规则

**定义**: 处方总价 = ∑(药材小计) × 帖数 × 折扣

**计算公式**:
```
每帖价格 = ∑(单价 × 用量)
处方总价 = 每帖价格 × 帖数 × 折扣
```

**实现示例**:
```csharp
public decimal CalculateTotalPrice(Prescription prescription)
{
    // Step 1: 计算每帖价格
    decimal perDosePrice = prescription.Items.Sum(item => item.Amount);

    // Step 2: 计算总价（每帖价格 × 帖数）
    decimal subtotal = perDosePrice * prescription.DosageCount;

    // Step 3: 应用折扣
    decimal finalPrice = subtotal * prescription.Discount;

    return finalPrice;
}

// 药材小计计算
public void CalculatePrescriptionItemAmount(PrescriptionItem item)
{
    item.Amount = item.UnitPrice * item.Quantity;
}
```

**价格更新机制**:
- 创建处方时：获取药材当前价格
- 处方未打印时：可动态更新药材价格（药材价格变化时）
- 处方已打印后：价格锁定，不再更新

### 4.7 BR-003: 打印版本控制

**定义**: 处方每次打印时递增PrintVersion，记录打印日志。

**版本控制字段**:
- `PrintVersion`: 当前打印版本号（从1开始）
- `PrintCount`: 打印次数（重印计数）
- `LastPrintedAt`: 最后打印时间
- `PrintLogs`: 打印日志集合（记录每次打印）

**实现示例**:
```csharp
public async Task<PrescriptionPrintResult> PrintPrescriptionAsync(PrintPrescriptionRequest request)
{
    var prescription = await _repository.GetByIdAsync(request.PrescriptionId);

    // 验证打印权限
    if (prescription.PrintCount >= 3)
        throw new BusinessException("处方打印次数已达上限（3次）");

    // 更新打印版本信息（BR-003）
    prescription.RecordPrint(
        printedBy: _currentUser.Id,
        printerName: request.PrinterName,
        reason: request.Reason
    );

    // 保存更新
    await _repository.UpdateAsync(prescription);
    await _repository.SaveChangesAsync();

    // 生成打印数据
    var printData = await GeneratePrescriptionPrintDataAsync(prescription);

    return new PrescriptionPrintResult
    {
        PrescriptionId = prescription.Id,
        PrescriptionNumber = prescription.PrescriptionNumber,
        PrintVersion = prescription.PrintVersion,
        PrintData = printData
    };
}
```

### 4.8 BR-004: 处方审核规则

**定义**: 处方打印前需通过审核规则验证。

**审核规则清单**:
1. **药材数量检查**: 1 ≤ 药材数量 ≤ 30
2. **单味药剂量检查**: 剂量不超过安全上限（如甘草≤30g）
3. **配伍禁忌检查**: 十八反、十九畏
4. **价格合理性检查**: 10元 ≤ 总价 ≤ 5000元

**实现示例**:
```csharp
public class PrescriptionAuditService
{
    public async Task<PrescriptionAuditResult> AuditPrescriptionAsync(Guid prescriptionId)
    {
        var prescription = await _repository
            .GetByConditionAsync(p => p.Id == prescriptionId,
                                include: p => p.Include(p => p.Items));

        var results = new List<AuditResult>();

        // 规则1: 药材数量检查
        results.Add(ValidateHerbCount(prescription));

        // 规则2: 单味药剂量检查
        results.Add(ValidateDosage(prescription));

        // 规则3: 配伍禁忌检查
        results.Add(ValidateHerbConflicts(prescription));

        // 规则4: 价格合理性检查
        results.Add(ValidatePriceRange(prescription));

        return new PrescriptionAuditResult
        {
            OverallPassed = results.All(r => r.Passed),
            AuditResults = results
        };
    }

    private AuditResult ValidateHerbCount(Prescription prescription)
    {
        int count = prescription.Items.Count;
        return new AuditResult
        {
            RuleName = "药材数量检查",
            Passed = count >= 1 && count <= 30,
            Message = count < 1 ? "处方不能为空" :
                     count > 30 ? "处方药材过多（超过30味）" : null
        };
    }
}
```

---

## 5. 数据流与交互

### 5.1 处方创建流程（通过MedicalCase聚合根）

```
Desktop → MedicalCaseApi.CreatePrescription → MedicalCaseController → MedicalCaseService
                                                                            ↓
                                                                    PrescriptionService
                                                                            ↓
[AR-001聚合根验证] → [AR-003一诊一方验证] → [生成处方编号 BR-001] → [添加处方项]
                                                                            ↓
                                            [获取药材当前价格] → [计算总价 BR-002]
                                                                            ↓
                                            [保存聚合根] → [返回PrescriptionDto]
```

**关键步骤**:
1. Desktop调用MedicalCaseApi.CreatePrescription（通过聚合根）
2. 验证AR-001聚合根约束（必须通过MedicalCase）
3. 验证AR-003一诊一方约束（病案不能已有处方）
4. 生成处方编号（BR-001：RX-YYYYMMDD-NNNN）
5. 添加处方项（验证药材存在性，获取当前价格）
6. 计算总价（BR-002：Items.Sum(amount) * DosageCount * Discount）
7. 保存聚合根（MedicalCase + Prescription）

### 5.2 验方导入流程

```
Desktop → SelectFormula → CreatePrescriptionFromFormula
                                    ↓
                    [查询Formula信息] → [复制FormulaItems]
                                    ↓
                    [更新为当前药材价格] → [应用个性化调整]
                                    ↓
                    [创建处方（通过聚合根）] → [记录ReferencedFormulas]
```

**关键步骤**:
1. Desktop用户选择验方（经典方剂、经验方剂、协定方剂）
2. 查询Formula实体（包含FormulaItems）
3. 复制FormulaItems到PrescriptionItems
4. 更新为当前药材价格（使用Herb.UnitPrice）
5. 应用个性化调整（添加、删除、修改剂量）
6. 创建处方（通过MedicalCase聚合根）
7. 记录ReferencedFormulas（追溯验方来源）

### 5.3 处方打印流程

```
Desktop → PrintPrescription → PrescriptionService.PrintAsync
                                        ↓
            [验证打印权限] → [更新打印版本 BR-003] → [记录打印日志]
                                        ↓
            [生成打印数据] → [调用打印驱动] → [返回打印结果]
```

**关键步骤**:
1. 验证打印权限（打印次数限制、时间限制）
2. 更新打印版本（BR-003：PrintVersion++, PrintCount++）
3. 记录打印日志（PrescriptionPrintLog）
4. 生成打印数据（患者信息、诊断信息、处方内容、医嘱、签名）
5. 调用打印驱动（Desktop端打印）
6. 更新处方状态（Status = Printed）

### 5.4 处方查询流程（Read Layer）

```
Desktop → PrescriptionApi.GetById → PrescriptionsController.GetById
                                                ↓
                                    PrescriptionService.GetByIdAsync
                                                ↓
                                    [查询处方] → [预加载Items]
                                                ↓
                                    [返回PrescriptionDto（包含Items）]
```

**查询端点**:
- `GET /api/v1/prescriptions/{id}` - 获取处方详情
- `GET /api/v1/prescriptions/medicalcase/{medicalCaseId}` - 获取病案处方列表
- `GET /api/v1/prescriptions/search` - 按病症关键字搜索（REQ-2）
- `GET /api/v1/prescriptions/patient/{patientId}/recent` - 获取患者最近处方（REQ-1）

**预加载策略**:
```csharp
// 自动预加载Items关联数据
var prescription = await _repository
    .GetByConditionAsync(p => p.Id == id,
                        include: p => p.Include(p => p.Items));
```

---

## 6. 技术决策

### 6.1 AR-001聚合根模式（Write通过MedicalCase，Read独立查询）

**决策**: 所有Prescription的Write操作必须通过MedicalCase聚合根完成，Read操作可以独立查询。

**原因**:
- **聚合一致性**: 保证MedicalCase + Prescription的聚合一致性（事务边界）
- **业务约束**: AR-003一诊一方约束需要通过聚合根验证
- **查询性能**: Read操作独立查询，避免聚合根查询性能损耗

**实现**:
- Write端点在MedicalCaseController：`POST /medicalcases/{id}/prescriptions`
- Read端点在PrescriptionsController：`GET /prescriptions/{id}`

**优势**:
- ✅ 保证聚合一致性（MedicalCase + Prescription）
- ✅ 简化业务逻辑（通过聚合根统一处理约束）
- ✅ 查询性能优化（Read操作独立查询）

### 6.2 验方集成模式（经典方剂、经验方剂、协定方剂）

**决策**: 支持三类验方导入，允许个性化调整。

**验方分类**:
1. **经典方剂**: 伤寒论、金匮要略等经典方剂（FormulaSource标记）
2. **经验方剂**: 医院或医生个人经验方（ReferencedFormulas记录）
3. **协定方剂**: 科室协定方、医院协定方（FormulaSource标记）

**导入流程**:
```csharp
CreateFromFormula(FormulaId)
    → CopyFormulaItems
    → UpdateHerbPrices
    → ApplyModifications(Add/Remove/ModifyQuantity)
    → CreatePrescription
```

**个性化调整**:
- **AddHerb**: 添加新药材（加味）
- **Remove**: 移除药材（去味）
- **ModifyQuantity**: 调整剂量（增减剂量）

**优势**:
- ✅ 快速开具处方（基于经验方剂）
- ✅ 个性化调整（符合辨证施治原则）
- ✅ 追溯验方来源（ReferencedFormulas记录）

### 6.3 打印版本控制（PrintVersion递增机制）

**决策**: 每次打印时递增PrintVersion，记录打印日志。

**版本控制字段**:
- `PrintVersion`: 当前打印版本号（从1开始，每次打印递增）
- `PrintCount`: 打印次数（重印计数）
- `LastPrintedAt`: 最后打印时间
- `PrintLogs`: 打印日志集合（记录每次打印详情）

**实现机制**:
```csharp
public void RecordPrint(Guid printedBy, string? printerName, string? reason)
{
    PrintVersion++;          // 版本号递增
    PrintCount++;            // 打印次数递增
    LastPrintedAt = DateTime.UtcNow;
    IsPrinted = true;
    Status = PrescriptionStatus.Printed;

    // 记录打印日志
    PrintLogs.Add(new PrescriptionPrintLog
    {
        PrintVersion = PrintVersion,
        PrintedAt = DateTime.UtcNow,
        PrintedBy = printedBy,
        PrinterName = printerName,
        PrintReason = reason
    });
}
```

**优势**:
- ✅ 追溯打印历史（每次打印有日志）
- ✅ 版本控制（支持修改后重印）
- ✅ 合规要求（打印日志审计）

### 6.4 价格计算策略（自动计算总价）

**决策**: 处方总价自动计算，不允许手动修改。

**计算公式**:
```
每帖价格 = ∑(单价 × 用量)
处方总价 = 每帖价格 × 帖数 × 折扣
```

**计算时机**:
- 创建处方时：获取药材当前价格，计算总价
- 修改处方项时：重新计算总价
- 修改帖数或折扣时：重新计算总价

**价格更新策略**:
- **未打印处方**: 药材价格变化时可动态更新
- **已打印处方**: 价格锁定，不再更新

**优势**:
- ✅ 价格准确（基于当前药材价格）
- ✅ 防止人为错误（自动计算，不可手动修改）
- ✅ 价格追溯（已打印处方价格锁定）

---

## 7. 模块依赖关系

### 7.1 上游依赖

**MedicalCase（聚合根）**:
- AR-001: 所有Write操作必须通过MedicalCase聚合根
- AR-003: 一诊一方约束验证（MedicalCase.Prescription唯一性）

**Consultation（诊断信息）**:
- Indication字段来源：Consultation.TCMDiagnosis（中医诊断）
- TreatmentPrinciple参考：Consultation.TreatmentPrinciple（治疗原则）

**Patients（患者信息）**:
- 患者基本信息：PatientId, PatientName
- 打印处方需要：患者姓名、性别、年龄、联系方式

**Herbs（药材信息）**:
- 药材基础信息：HerbId, HerbName, Unit
- 价格信息：UnitPrice（创建处方项时获取）
- 库存验证：IsActive（验证药材是否可用）

**Formula（验方信息）**:
- 验方导入：FormulaId, FormulaName, FormulaItems
- 验方追溯：FormulaSource, ReferencedFormulas

### 7.2 下游依赖

**Billing（收费模块）**:
- 收费金额计算：TotalAmount
- 收费明细：PrescriptionItems

**Printing（打印模块）**:
- 打印数据生成：PrescriptionPrintData
- 打印版本控制：PrintVersion, PrintCount

**Statistics（统计模块）**:
- 处方统计：按状态、日期、药材、验方统计
- 趋势分析：处方数量、金额趋势

### 7.3 依赖关系图

```
        [MedicalCase] ← (AR-001聚合根) ← [Prescription]
                ↓
        [Consultation] ← (Indication) ← [Prescription]
                ↓
        [Patients] ← (PatientInfo) ← [Prescription]
                ↓
        [Herbs] ← (HerbInfo + Price) ← [PrescriptionItems]
                ↓
        [Formula] ← (FormulaImport) ← [Prescription]
                ↓
        [Prescription] → (TotalAmount) → [Billing]
                ↓
        [Prescription] → (PrintData) → [Printing]
                ↓
        [Prescription] → (Statistics) → [Analytics]
```

---

## 8. 扩展性设计

### 8.1 处方审核规则扩展

**当前实现**: 4个基础审核规则（药材数量、剂量、配伍禁忌、价格合理性）

**扩展机制**:
```csharp
public interface IPrescriptionAuditRule
{
    string RuleName { get; }
    string RuleDescription { get; }
    Task<AuditResult> ValidateAsync(Prescription prescription);
}

// 注册新规则
public class PrescriptionAuditService
{
    private readonly List<IPrescriptionAuditRule> _auditRules;

    public PrescriptionAuditService()
    {
        _auditRules = new List<IPrescriptionAuditRule>
        {
            new HerbCountRule(),
            new DosageRule(),
            new HerbConflictRule(),
            new PriceRangeRule(),
            // 扩展：添加新规则
            new PregnancyWarningRule(),     // 孕妇禁用药检查
            new AllergyCheckRule(),         // 过敏史检查
            new DrugInteractionRule()       // 药物相互作用检查
        };
    }
}
```

**扩展场景**:
- ✅ 孕妇禁用药检查
- ✅ 过敏史检查
- ✅ 药物相互作用检查
- ✅ 特殊人群用药限制

### 8.2 验方来源扩展

**当前实现**: 经典方剂、经验方剂、协定方剂

**扩展机制**:
```csharp
public enum FormulaSource
{
    Classic = 1,      // 经典方剂（伤寒论、金匮要略等）
    Experience = 2,   // 经验方剂（医生个人经验方）
    Agreement = 3,    // 协定方剂（科室协定方）

    // 扩展：新增验方来源
    ModernResearch = 4,   // 现代研究方剂
    InternationalTCM = 5, // 国际中医方剂
    PatentFormula = 6     // 中成药处方
}
```

**扩展场景**:
- ✅ 现代研究方剂（基于RCT研究）
- ✅ 国际中医方剂（WHO标准方）
- ✅ 中成药处方（颗粒剂、丸剂等）

### 8.3 价格计算策略扩展

**当前实现**: 固定折扣模式（0-1之间）

**扩展机制**:
```csharp
public interface IPriceCalculationStrategy
{
    decimal CalculatePrice(Prescription prescription);
}

// 策略1: 固定折扣
public class FixedDiscountStrategy : IPriceCalculationStrategy
{
    public decimal CalculatePrice(Prescription prescription)
    {
        return prescription.Items.Sum(i => i.Amount) *
               prescription.DosageCount *
               prescription.Discount;
    }
}

// 策略2: 阶梯定价
public class TieredPricingStrategy : IPriceCalculationStrategy
{
    public decimal CalculatePrice(Prescription prescription)
    {
        var subtotal = prescription.Items.Sum(i => i.Amount) *
                      prescription.DosageCount;

        // 阶梯定价逻辑
        if (subtotal > 500) return subtotal * 0.8m;  // 8折
        if (subtotal > 200) return subtotal * 0.9m;  // 9折
        return subtotal;                             // 无折扣
    }
}

// 策略3: 会员等级定价
public class MembershipPricingStrategy : IPriceCalculationStrategy
{
    public decimal CalculatePrice(Prescription prescription)
    {
        // 根据患者会员等级计算折扣
    }
}
```

**扩展场景**:
- ✅ 阶梯定价（按金额分段折扣）
- ✅ 会员等级定价（VIP、普通会员）
- ✅ 季节性定价（时令药材价格波动）

### 8.4 打印格式扩展

**当前实现**: 标准A4纸打印格式

**扩展机制**:
```csharp
public interface IPrescriptionPrintFormatter
{
    PrescriptionPrintData Format(Prescription prescription);
}

// 格式1: 标准A4格式
public class StandardA4Formatter : IPrescriptionPrintFormatter
{
    public PrescriptionPrintData Format(Prescription prescription)
    {
        return new PrescriptionPrintData
        {
            PageWidth = 210,  // A4宽度(mm)
            PageHeight = 297, // A4高度(mm)
            FontSize = 12,
            // ... 标准格式
        };
    }
}

// 格式2: 小票格式
public class ReceiptFormatter : IPrescriptionPrintFormatter
{
    public PrescriptionPrintData Format(Prescription prescription)
    {
        return new PrescriptionPrintData
        {
            PageWidth = 80,   // 小票宽度(mm)
            FontSize = 10,
            // ... 小票格式
        };
    }
}

// 格式3: 电子处方格式（PDF/图片）
public class ElectronicFormatter : IPrescriptionPrintFormatter
{
    public PrescriptionPrintData Format(Prescription prescription)
    {
        // 生成PDF或图片格式
    }
}
```

**扩展场景**:
- ✅ 小票格式（热敏打印机）
- ✅ 电子处方格式（PDF、图片）
- ✅ 多语言格式（中英文双语处方）
- ✅ 二维码集成（处方追溯码）

---

## 总结

Prescription模块作为LYBTZYZS的**核心业务模块**，实现了完整的中医处方管理功能：

### 核心特性
1. **聚合根模式**：Write操作通过MedicalCase聚合根（AR-001），Read操作独立查询
2. **验方集成**：支持经典方剂、经验方剂、协定方剂快速导入和个性化调整
3. **智能定价**：自动计算总价，支持折扣设置，价格锁定机制
4. **打印管理**：版本控制、打印日志、格式生成、重印限制
5. **审核流程**：药材数量、剂量、配伍禁忌、价格合理性4项审核规则
6. **查询服务**：按患者（REQ-1）、病症关键字（REQ-2）、病案查询
7. **统计分析**：按状态、日期、药材、验方统计，趋势分析

### 技术亮点
- ✅ **三层对齐架构**：Desktop MVVM + Server三层 + Database优化
- ✅ **聚合根约束**：保证MedicalCase + Prescription聚合一致性
- ✅ **业务规则驱动**：8项业务规则（AR-001/AR-003/REQ-1/REQ-2/BR-001~BR-004）
- ✅ **扩展性设计**：审核规则、验方来源、价格策略、打印格式可扩展

### 下一步
- 📖 学习[处方管理教程](../../../tutorials/modules/prescriptions/prescription-management-tutorial.md)
- 📖 查阅[Prescriptions API参考](../../../reference/api/prescriptions-api.md)
- 📖 理解[MedicalCase API参考](../../../reference/api/medicalcase-api.md)（聚合根Write操作）

---

**文档状态**: ✅ 已完成
**覆盖模块**: Prescription
**相关Issue**: 无
**维护周期**: 随模块更新同步
