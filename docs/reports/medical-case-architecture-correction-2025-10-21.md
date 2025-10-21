# 病案流程架构主题纠正分析报告

**报告类型**：架构纠正方案（UltraThink 21步深度分析）
**创建时间**：2025-10-21
**分析强度**：UltraThink（20-30步推理）
**Issue**：#1561 - 病案流程架构纠正与DDD重构
**优先级**：🔴 高（根本性架构偏差）

---

## 📋 执行摘要

### 核心发现

在重构看诊流程时发现了一个**根本性的架构理解偏差**：

| 维度 | 错误认知 | 正确认知 |
|-----|---------|---------|
| **主题** | Consultation（诊断）为中心 | MedicalCase（病案）为中心 |
| **命名** | consultation-workflow | medical-case-workflow |
| **架构** | Consultation是核心业务实体 | MedicalCase是聚合根 |
| **实现** | 直接调用ConsultationRepository | 通过MedicalCaseService操作 |

### 影响范围

| 层级 | 受影响项 | 纠正必要性 |
|-----|---------|-----------|
| **命名层** | 分支名、Issue标题、文档名 | ⭐⭐⭐⭐⭐ 非常高 |
| **接口层** | Event名称、Service门面 | ⭐⭐⭐⭐ 高 |
| **领域层** | DDD聚合根建模 | ⭐⭐⭐ 中 |

### 纠正方案

**总工作量**：13小时
**分阶段执行**：5个Phase
**立即执行**：Phase 0命名纠正（15分钟）

---

## 🔍 问题诊断（UltraThink分析）

### 1. 错误认知链

```
❌ 根源：分支名 refactor/consultation-workflow-complete
           ↓
❌ 概念偏差：将"诊断流程"理解为核心业务
           ↓
❌ 架构错位：Consultation被视为独立的核心实体
           ↓
❌ 实现违规：ViewModel直接调用ConsultationRepository
           ↓
❌ DDD破坏：绕过MedicalCase聚合根，破坏业务封装
```

### 2. 具体表现

#### 命名层问题

**分支命名**：
```bash
❌ refactor/consultation-workflow-complete
✅ 应为：refactor/medical-case-workflow-refactor
```

**Issue标题**：
```
❌ #1561: "看诊流程完整重构"
✅ 应为：#1561: "病案流程架构纠正与DDD重构"
```

**文档命名**：
```
❌ consultation-workflow-analysis-2025-10-21.md
✅ 应为：medical-case-workflow-analysis-2025-10-21.md

❌ consultation-prescription-relationship-pattern-discussion.md
✅ 应为：medical-case-aggregate-design-discussion.md
```

**术语使用**：
```
❌ "看诊流程"、"诊断流程"、"处方流程"
✅ "病案管理流程"、"病案诊断步骤"、"病案处方步骤"
```

#### 代码层问题

**Event命名**：
```csharp
// ❌ 当前命名（暗示Consultation是终点）
public class ConsultationCompletedEvent { ... }
public class ConsultationCompletedPayload { ... }

// ✅ 正确命名（体现MedicalCase的步骤）
public class MedicalCaseStep2CompletedEvent { ... }
public class MedicalCaseStepCompletedPayload { ... }
```

**ViewModel调用**：
```csharp
// ❌ 当前实现（破坏聚合边界）
public class ConsultationFormViewModel
{
    private readonly IConsultationRepository _consultationRepository;  // 直接调用子实体

    public async Task<bool> SaveAsync()
    {
        var dto = new ConsultationCreateDto { ... };
        await _consultationRepository.CreateAsync(dto);  // 绕过聚合根
        return true;
    }
}

// ✅ 正确实现（通过聚合根）
public class ConsultationFormViewModel
{
    private readonly IMedicalCaseService _medicalCaseService;  // 聚合根服务

    public async Task<bool> SaveAsync()
    {
        var command = new AddConsultationCommand
        {
            MedicalCaseId = this.MedicalCaseId,
            ChiefComplaint = this.ChiefComplaint,
            ...
        };
        await _medicalCaseService.AddConsultationAsync(command);  // 通过聚合根
        return true;
    }
}
```

#### 架构层问题

**Repository边界破坏**：
```
❌ 当前设计
┌─────────────────────────────────────┐
│ ConsultationFormViewModel           │
│  ↓ 直接注入                          │
│ IConsultationRepository             │ ← 破坏聚合边界
│  ↓ 直接调用                          │
│ ConsultationRepository.CreateAsync()│
└─────────────────────────────────────┘

✅ 正确设计（DDD聚合根模式）
┌─────────────────────────────────────┐
│ ConsultationFormViewModel           │
│  ↓ 注入聚合服务                       │
│ IMedicalCaseService                 │
│  ↓ 调用业务方法                       │
│ AddConsultationAsync(command)       │
│   ↓ 内部调用                         │
│   IConsultationService (门面)       │
│     ↓ 内部调用                       │
│     ConsultationRepository          │ ← 边界保护
└─────────────────────────────────────┘
```

### 3. DDD违规分析

**聚合根边界破坏**：

根据DDD原则，**聚合根是唯一的外部访问入口**：

```
DDD标准模型：
┌──────────────────────────────────────┐
│ MedicalCase（聚合根）                  │
│ ├─ Id (聚合根标识)                     │
│ ├─ PatientId, UserId                 │
│ ├─ Status (聚合状态)                  │
│ │                                    │
│ ├─ Consultation（聚合内实体）         │
│ │   └─ Id == MedicalCase.Id (共享主键)│
│ │                                    │
│ ├─ Prescription（聚合内实体）         │
│ │   └─ MedicalCaseId (传统FK)        │
│ │                                    │
│ └─ 业务方法（封装规则）                │
│     ├─ AddConsultation(data)         │
│     ├─ AddPrescription(data)         │
│     └─ Complete()                    │
└──────────────────────────────────────┘

外部访问规则：
✅ 允许：MedicalCaseService.AddConsultation(command)
❌ 禁止：ConsultationRepository.CreateAsync(dto)
```

**当前违规点**：

1. **Repository直接暴露**：
   - ConsultationRepository/PrescriptionRepository可以被外部直接调用
   - 绕过MedicalCase聚合根的业务规则

2. **Service层独立**：
   - ConsultationService/PrescriptionService作为独立服务存在
   - 未体现MedicalCase的协调作用

3. **ViewModel直接调用**：
   - ConsultationFormViewModel直接注入ConsultationRepository
   - 完全跳过聚合根

---

## 🎯 完整纠正方案（5 Phase）

### Phase 0：立即命名纠正（15分钟）⭐ **立即执行**

**目标**：纠正所有体现"consultation为中心"的命名

#### Git操作

```bash
# 1. 分支改名（本地 + 远程）
git branch -m refactor/consultation-workflow-complete refactor/medical-case-workflow-refactor
git push origin :refactor/consultation-workflow-complete
git push origin refactor/medical-case-workflow-refactor
git push --set-upstream origin refactor/medical-case-workflow-refactor

# 2. 文档改名（保持Git历史）
git mv docs/reports/consultation-workflow-analysis-2025-10-21.md \
       docs/reports/medical-case-workflow-analysis-2025-10-21.md

git mv docs/architecture/shared/consultation-prescription-relationship-pattern-discussion.md \
       docs/architecture/shared/medical-case-aggregate-design-discussion.md

# 3. 提交改名
git add -A
git commit -m "refactor: 纠正架构主题命名（consultation → medical-case）

- 分支改名：medical-case-workflow-refactor
- 文档改名：体现MedicalCase为聚合根
- 符合DDD聚合根设计原则

Issue #1561"
```

#### GitHub操作

**Issue #1561标题修改**：
- 旧：看诊流程完整重构
- 新：**病案流程架构纠正与DDD重构**

**Issue描述补充**：
```markdown
## 架构纠正说明

**根本问题**：前期设计将"诊断"理解为核心流程，而非"病案"

**纠正方向**：
- MedicalCase是聚合根，是业务的核心
- Consultation/Prescription是聚合内的实体
- 所有操作通过MedicalCase聚合根进行

**DDD原则**：
- 聚合根：MedicalCase
- 实体：Consultation、Prescription
- 值对象：ConsultationData、PrescriptionData（Phase 4）
```

#### 文档更新

**更新所有文档引用**：
```bash
# 全局搜索需要更新的引用
grep -r "consultation-workflow-analysis" docs/
grep -r "consultation-prescription-relationship" docs/
grep -r "看诊流程" docs/

# 替换为新文件名和术语
# "consultation-workflow-analysis" → "medical-case-workflow-analysis"
# "consultation-prescription-relationship" → "medical-case-aggregate-design"
# "看诊流程" → "病案流程"
```

**检查清单**：
- [ ] docs/index.md中的链接更新
- [ ] 其他文档的交叉引用更新
- [ ] 所有"看诊流程"改为"病案流程"

#### 验收标准

```bash
# 验证命名纠正完成
git branch | grep "medical-case-workflow-refactor"  # 分支名正确
ls docs/reports/ | grep "medical-case-workflow"     # 文档名正确
grep -r "consultation-workflow" docs/ | wc -l       # 应为0（除模块名）
```

---

### Phase 1：基础清理 + 代码命名（1小时）

**目标**：纠正代码层的"consultation为中心"命名

#### 已完成任务（Phase 1前期）

- ✅ 删除死代码 UpdateMedicalCaseConsultationIdAsync
- ✅ 更新实体关系文档 clinical-workflow-entity-relationships.md
- ✅ 添加Repository注释说明共享主键设计

#### Event改名

**文件改名**：
```bash
# 找到Event定义文件
find src/ -name "*ConsultationCompletedEvent.cs"

# 改名（假设路径）
git mv src/Client/Desktop/Infrastructure/Events/ConsultationCompletedEvent.cs \
       src/Client/Desktop/Infrastructure/Events/MedicalCaseStep2CompletedEvent.cs
```

**代码修改**：

**Event定义**：
```csharp
// src/Client/Desktop/Infrastructure/Events/MedicalCaseStep2CompletedEvent.cs

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 病案Step 2（诊断阶段）完成事件
    /// Issue #1561: 架构纠正 - 体现MedicalCase为主题
    /// </summary>
    public class MedicalCaseStep2CompletedEvent : PubSubEvent<MedicalCaseStepCompletedPayload>
    {
    }

    /// <summary>
    /// 病案步骤完成事件负载
    /// </summary>
    public class MedicalCaseStepCompletedPayload
    {
        public Guid ConsultationId { get; set; }      // 诊断ID
        public Guid MedicalCaseFlowId { get; set; }   // 病案流程ID
        public string ChiefComplaint { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public bool IsDraft { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

**发布Event（ConsultationFormViewModel）**：
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs

private void PublishConsultationCompletedEvent(Guid consultationId, bool isDraft)
{
    try
    {
        var payload = new MedicalCaseStepCompletedPayload  // ✅ 新Payload
        {
            ConsultationId = consultationId,
            MedicalCaseFlowId = MedicalCaseId,
            ChiefComplaint = ChiefComplaint,
            Diagnosis = TCMDiagnosis,
            IsDraft = isDraft,
            Timestamp = DateTime.Now
        };

        EventAggregator.GetEvent<MedicalCaseStep2CompletedEvent>().Publish(payload);  // ✅ 新Event

        Logger.LogInformation("已发布MedicalCaseStep2CompletedEvent，ConsultationId: {ConsultationId}, MedicalCaseFlowId: {MedicalCaseFlowId}",
            consultationId, MedicalCaseId);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "发布MedicalCaseStep2CompletedEvent失败");
    }
}
```

**订阅Event（MedicalCaseFlowViewModel）**：
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs

private void SubscribeToEvents()
{
    // Step 2完成 → 跳转Step 3
    EventAggregator.GetEvent<MedicalCaseStep2CompletedEvent>()  // ✅ 新Event
        .Subscribe(OnConsultationCompleted, ThreadOption.UIThread);
}

private void OnConsultationCompleted(MedicalCaseStepCompletedPayload payload)  // ✅ 新Payload
{
    try
    {
        Logger.LogInformation("接收到MedicalCaseStep2CompletedEvent，MedicalCaseFlowId: {MedicalCaseFlowId}",
            payload.MedicalCaseFlowId);

        // 跳转到Step 3
        CurrentStep = 3;
        NavigateToStep3();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "处理MedicalCaseStep2CompletedEvent失败");
    }
}
```

#### 验收标准

```bash
# 验证Event改名完成
grep -r "ConsultationCompletedEvent" src/ | wc -l  # 应为0
grep -r "MedicalCaseStep2CompletedEvent" src/      # 找到新引用
dotnet build LYBT.All.sln -c Release               # 编译通过
```

---

### Phase 2：Service门面 + DTO纠正（3小时）

**目标**：创建MedicalCaseService门面，体现聚合根设计

#### 创建MedicalCaseService

**接口定义**：
```csharp
// src/Shared/LYBT.Shared.Models/Interfaces/IMedicalCaseService.cs

namespace LYBT.Shared.Models.Interfaces
{
    /// <summary>
    /// 病案聚合根服务接口
    /// Issue #1561: DDD聚合根设计 - 所有子实体操作通过此接口
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 为病案添加诊断
        /// </summary>
        /// <remarks>
        /// DDD原则：通过聚合根操作子实体，而非直接调用ConsultationRepository
        /// </remarks>
        Task<ConsultationDto> AddConsultationAsync(AddConsultationCommand command);

        /// <summary>
        /// 为病案添加处方
        /// </summary>
        Task<PrescriptionDto> AddPrescriptionAsync(AddPrescriptionCommand command);

        /// <summary>
        /// 完成病案（验证所有必需步骤已完成）
        /// </summary>
        Task<MedicalCaseDto> CompleteMedicalCaseAsync(Guid medicalCaseId);
    }
}
```

**Command定义（体现通过MedicalCase操作）**：
```csharp
// src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseCommands.cs

namespace LYBT.Shared.Models.Contracts.MedicalCase
{
    /// <summary>
    /// 为病案添加诊断命令（通过MedicalCase聚合根）
    /// Issue #1561: 替代ConsultationCreateDto，体现聚合根设计
    /// </summary>
    public class AddConsultationCommand
    {
        /// <summary>病案ID（聚合根标识）</summary>
        public Guid MedicalCaseId { get; set; }

        // 诊断数据（不包含冗余的PatientId/UserId，通过MedicalCase获取）
        public string ChiefComplaint { get; set; } = string.Empty;
        public string? PresentIllness { get; set; }
        public string? Inspection { get; set; }
        public string? AuscultationOlfaction { get; set; }
        public string? Inquiry { get; set; }
        public string? Palpation { get; set; }
        public string TCMDiagnosis { get; set; } = string.Empty;
        public string? TreatmentPrinciple { get; set; }
        public string? Remark { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 为病案添加处方命令
    /// </summary>
    public class AddPrescriptionCommand
    {
        /// <summary>病案ID（聚合根标识）</summary>
        public Guid MedicalCaseId { get; set; }

        // 处方数据（不包含冗余字段）
        public string? Indication { get; set; }
        public int DosageCount { get; set; } = 7;
        public decimal Discount { get; set; } = 1.0m;
        public string? Advice { get; set; }
        public string? FormulaSource { get; set; }
        public string? Remark { get; set; }

        public List<PrescriptionItemDto> Items { get; set; } = new();
    }
}
```

**Service实现（门面模式）**：
```csharp
// src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 病案聚合根服务（门面）
    /// Issue #1561: 通过门面模式协调子实体操作
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IConsultationService _consultationService;  // 通过Service门面调用
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            IMedicalCaseRepository medicalCaseRepository,
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            ILogger<MedicalCaseService> logger)
        {
            _medicalCaseRepository = medicalCaseRepository;
            _consultationService = consultationService;
            _prescriptionService = prescriptionService;
            _logger = logger;
        }

        /// <summary>
        /// 为病案添加诊断
        /// </summary>
        public async Task<ConsultationDto> AddConsultationAsync(AddConsultationCommand command)
        {
            _logger.LogInformation("开始为病案添加诊断，MedicalCaseId: {MedicalCaseId}", command.MedicalCaseId);

            // 1. 加载聚合根，验证病案存在
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new NotFoundException($"病案不存在：{command.MedicalCaseId}");

            // 2. 验证业务规则：病案尚未有诊断
            if (medicalCase.Consultation != null)
                throw new DomainException("病案已有诊断记录，不能重复添加");

            // 3. 构建ConsultationCreateDto（从MedicalCase获取PatientId/UserId）
            var createDto = new ConsultationCreateDto
            {
                MedicalCaseId = command.MedicalCaseId,
                PatientId = medicalCase.PatientId,      // 从聚合根获取
                UserId = medicalCase.UserId,            // 从聚合根获取
                PatientName = medicalCase.PatientName,
                DoctorName = medicalCase.DoctorName,
                StartTime = command.StartTime,
                ChiefComplaint = command.ChiefComplaint,
                PresentIllness = command.PresentIllness,
                Inspection = command.Inspection,
                AuscultationOlfaction = command.AuscultationOlfaction,
                Inquiry = command.Inquiry,
                Palpation = command.Palpation,
                TCMDiagnosis = command.TCMDiagnosis,
                TreatmentPrinciple = command.TreatmentPrinciple,
                Remark = command.Remark
            };

            // 4. 调用ConsultationService创建（门面模式）
            var consultationDto = await _consultationService.CreateAsync(createDto);

            _logger.LogInformation("病案诊断添加成功，ConsultationId: {ConsultationId}", consultationDto.Id);

            // Phase 4将改为：
            // var consultationData = new ConsultationData { ... };
            // medicalCase.AddConsultation(consultationData);
            // await _medicalCaseRepository.SaveAsync(medicalCase);

            return consultationDto;
        }

        /// <summary>
        /// 为病案添加处方
        /// </summary>
        public async Task<PrescriptionDto> AddPrescriptionAsync(AddPrescriptionCommand command)
        {
            _logger.LogInformation("开始为病案添加处方，MedicalCaseId: {MedicalCaseId}", command.MedicalCaseId);

            // 1. 加载聚合根
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new NotFoundException($"病案不存在：{command.MedicalCaseId}");

            // 2. 验证业务规则
            if (medicalCase.Consultation == null)
                throw new DomainException("病案尚未诊断，不能开处方");

            if (medicalCase.Prescription != null)
                throw new DomainException("病案已有处方，不能重复添加");

            // 3. 构建PrescriptionCreateDto
            var createDto = new PrescriptionCreateDto
            {
                MedicalCaseId = command.MedicalCaseId,
                PatientId = medicalCase.PatientId,
                UserId = medicalCase.UserId,
                Indication = command.Indication,
                DosageCount = command.DosageCount,
                Discount = command.Discount,
                Advice = command.Advice,
                FormulaSource = command.FormulaSource,
                Remark = command.Remark,
                Items = command.Items
            };

            // 4. 调用PrescriptionService创建
            var prescriptionDto = await _prescriptionService.CreateAsync(createDto);

            _logger.LogInformation("病案处方添加成功，PrescriptionId: {PrescriptionId}", prescriptionDto.Id);

            return prescriptionDto;
        }

        /// <summary>
        /// 完成病案
        /// </summary>
        public async Task<MedicalCaseDto> CompleteMedicalCaseAsync(Guid medicalCaseId)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
                throw new NotFoundException($"病案不存在：{medicalCaseId}");

            // 验证完成条件
            if (medicalCase.Consultation == null)
                throw new DomainException("病案尚未诊断，不能完成");

            // Prescription可选，允许不开方

            // 更新状态
            medicalCase.Status = MedicalCaseStatus.Completed;
            await _medicalCaseRepository.UpdateAsync(medicalCase);

            // 返回DTO
            return MapToDto(medicalCase);
        }
    }
}
```

#### DTO纠正

**ConsultationCreateDto简化（Phase 2-3）**：
```csharp
// src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs

public class ConsultationCreateDto
{
    // ⚠️ Phase 2-3：保留冗余字段（为了兼容性）
    // Phase 4将完全通过MedicalCase获取，这里删除
    public Guid PatientId { get; set; }        // 从MedicalCase获取
    public Guid UserId { get; set; }           // 从MedicalCase获取
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }

    // 核心诊断数据
    public Guid MedicalCaseId { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string TCMDiagnosis { get; set; } = string.Empty;
    // ... 其他字段
}
```

#### ViewModel重构

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs

public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    // ❌ 旧依赖
    // private readonly IConsultationRepository _consultationRepository;

    // ✅ 新依赖
    private readonly IMedicalCaseService _medicalCaseService;

    public ConsultationFormViewModel(
        // IConsultationRepository consultationRepository,  // ❌ 删除
        IMedicalCaseService medicalCaseService,             // ✅ 新增
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        // _consultationRepository = consultationRepository;  // ❌ 删除
        _medicalCaseService = medicalCaseService;              // ✅ 新增
        _medicalCaseRepository = medicalCaseRepository;

        ImportFromHistoryCommand = new DelegateCommand(ExecuteImportFromHistory);
        ClearFormCommand = new DelegateCommand(ExecuteClearForm);

        Logger.LogInformation("ConsultationFormViewModel已初始化");
    }

    public async Task<bool> SaveAsync()
    {
        try
        {
            Logger.LogInformation("开始保存诊断数据，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

            if (MedicalCaseId == Guid.Empty)
            {
                Logger.LogError("MedicalCaseId为空，无法创建Consultation");
                ValidationMessage = "医案ID为空，无法保存诊断数据";
                return false;
            }

            // ✅ 通过MedicalCaseService创建（不再直接调用Repository）
            var command = new AddConsultationCommand
            {
                MedicalCaseId = MedicalCaseId,
                ChiefComplaint = ChiefComplaint.Trim(),
                PresentIllness = string.IsNullOrWhiteSpace(PresentIllness) ? null : PresentIllness.Trim(),
                Inspection = string.IsNullOrWhiteSpace(Inspection) ? null : Inspection.Trim(),
                AuscultationOlfaction = string.IsNullOrWhiteSpace(AuscultationOlfaction) ? null : AuscultationOlfaction.Trim(),
                Inquiry = string.IsNullOrWhiteSpace(Inquiry) ? null : Inquiry.Trim(),
                Palpation = string.IsNullOrWhiteSpace(Palpation) ? null : Palpation.Trim(),
                TCMDiagnosis = TCMDiagnosis.Trim(),
                TreatmentPrinciple = string.IsNullOrWhiteSpace(TreatmentPrinciple) ? null : TreatmentPrinciple.Trim(),
                Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim(),
                StartTime = DateTime.Now
            };

            var createdDto = await _medicalCaseService.AddConsultationAsync(command);

            Logger.LogInformation("诊断数据保存成功，ConsultationId: {ConsultationId}", createdDto.Id);

            // 发布事件
            PublishConsultationCompletedEvent(createdDto.Id, isDraft: false);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存诊断数据失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
            ValidationMessage = $"保存失败：{ex.Message}";
            return false;
        }
    }
}
```

#### DI注册

```csharp
// src/Server/LYBT.WebAPI/Program.cs 或模块注册

services.AddScoped<IMedicalCaseService, MedicalCaseService>();  // 新增
```

#### 验收标准

```bash
# 验证Service门面创建
ls src/Server/Modules/LYBT.Module.MedicalCase/Services/ | grep "MedicalCaseService"

# 验证ViewModel重构
grep -r "IConsultationRepository" src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ | wc -l
# 应为0（已删除直接依赖）

grep -r "IMedicalCaseService" src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/
# 找到新依赖

dotnet build LYBT.All.sln -c Release  # 编译通过
```

---

### Phase 3：UI优化 + EF配置修正（2小时）

**目标**：允许可选处方 + UI支持不开方

#### EF配置修正

```csharp
// src/Server/Core/LYBT.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> entity)
    {
        entity.ToTable("Prescriptions");
        entity.HasKey(p => p.Id);

        entity.Property(p => p.Discount).HasPrecision(3, 2);

        // 唯一索引（保证1:1关系）
        entity.HasIndex(p => p.MedicalCaseId)
            .HasDatabaseName("UX_Prescriptions_MedicalCaseId")
            .IsUnique();

        entity.Property(p => p.CreatedBy).IsRequired();
        entity.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();

        // ✅ 配置与MedicalCase的一对一关系（可选）
        entity.HasOne(p => p.MedicalCase)
            .WithOne(m => m.Prescription)
            .HasForeignKey<Prescription>(p => p.MedicalCaseId)
            // .IsRequired()  // ❌ 删除此行，允许MedicalCase没有Prescription
            .OnDelete(DeleteBehavior.Cascade);

        // Issue #1561: 允许病案无处方（用户明确要求"容许无处方"）
    }
}
```

#### UI添加checkbox

**PrescriptionFormView.xaml**：
```xml
<!-- Step 3: 处方表单顶部添加checkbox -->
<StackPanel Orientation="Horizontal" Margin="20,10">
    <CheckBox x:Name="SkipPrescriptionCheckBox"
              Content="本次就诊无需开方"
              IsChecked="{Binding SkipPrescription, Mode=TwoWay}"
              FontSize="14"
              VerticalAlignment="Center" />
    <TextBlock Text="（勾选后将跳过处方步骤）"
               FontSize="12"
               Foreground="#999"
               VerticalAlignment="Center"
               Margin="10,0,0,0" />
</StackPanel>

<!-- 处方表单内容（勾选checkbox后禁用） -->
<Grid IsEnabled="{Binding SkipPrescription, Converter={StaticResource InverseBoolConverter}}">
    <!-- 原有处方表单内容 -->
</Grid>
```

**PrescriptionFormViewModel.cs**：
```csharp
public class PrescriptionFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
{
    private bool _skipPrescription = false;
    /// <summary>
    /// 是否跳过处方（不开方）
    /// </summary>
    public bool SkipPrescription
    {
        get => _skipPrescription;
        set
        {
            if (SetProperty(ref _skipPrescription, value))
            {
                // 勾选后清空表单
                if (value)
                {
                    ClearForm();
                }
            }
        }
    }

    public async Task<bool> SaveAsync()
    {
        try
        {
            // 如果跳过处方，直接返回成功（不创建Prescription）
            if (SkipPrescription)
            {
                Logger.LogInformation("用户选择不开方，跳过处方创建");

                // 发布事件（标记为已跳过）
                PublishPrescriptionSkippedEvent();

                return true;
            }

            // 正常创建处方
            var command = new AddPrescriptionCommand { ... };
            await _medicalCaseService.AddPrescriptionAsync(command);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存处方失败");
            return false;
        }
    }

    public bool Validate()
    {
        // 如果跳过处方，验证通过
        if (SkipPrescription)
            return true;

        // 否则验证必填字段
        // ...
    }
}
```

#### 验收标准

```bash
# 验证EF配置修正
grep -A 5 "HasOne.*MedicalCase" src/Server/Core/LYBT.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs
# 确认没有.IsRequired()

# 编译测试
dotnet build LYBT.All.sln -c Release
```

---

### Phase 4：DDD完整建模（6小时）

**目标**：实现完整的DDD聚合根模式

#### 值对象定义

```csharp
// src/Server/Core/LYBT.Entities/MedicalCase/ValueObjects/ConsultationData.cs

namespace LYBT.Entities.MedicalCase.ValueObjects
{
    /// <summary>
    /// 诊断数据值对象
    /// Issue #1561: DDD建模 - 封装诊断业务数据
    /// </summary>
    public record ConsultationData
    {
        public string ChiefComplaint { get; init; } = string.Empty;
        public string TCMDiagnosis { get; init; } = string.Empty;
        public string? PresentIllness { get; init; }
        public string? Inspection { get; init; }
        public string? AuscultationOlfaction { get; init; }
        public string? Inquiry { get; init; }
        public string? Palpation { get; init; }
        public string? TreatmentPrinciple { get; init; }
        public string? Remark { get; init; }
        public DateTime StartTime { get; init; } = DateTime.Now;

        /// <summary>
        /// 验证诊断数据完整性
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
                throw new DomainValidationException("主诉不能为空");

            if (string.IsNullOrWhiteSpace(TCMDiagnosis))
                throw new DomainValidationException("中医诊断不能为空");
        }
    }

    /// <summary>
    /// 处方数据值对象
    /// </summary>
    public record PrescriptionData
    {
        public string? Indication { get; init; }
        public int DosageCount { get; init; } = 7;
        public decimal Discount { get; init; } = 1.0m;
        public string? Advice { get; init; }
        public string? FormulaSource { get; init; }
        public string? Remark { get; init; }
        public List<PrescriptionItemData> Items { get; init; } = new();

        public void Validate()
        {
            if (DosageCount <= 0)
                throw new DomainValidationException("剂数必须大于0");

            if (Discount < 0 || Discount > 1)
                throw new DomainValidationException("折扣必须在0-1之间");

            if (!Items.Any())
                throw new DomainValidationException("处方至少需要一味药材");
        }
    }

    public record PrescriptionItemData
    {
        public Guid HerbId { get; init; }
        public string HerbName { get; init; } = string.Empty;
        public decimal Dosage { get; init; }
        public string Unit { get; init; } = "g";
    }
}
```

#### 聚合根业务方法

```csharp
// src/Server/Core/LYBT.Entities/MedicalCase/MedicalCaseModel.cs

public class MedicalCase : BaseEntity
{
    // 导航属性改为private set（只能通过业务方法修改）
    public virtual Consultation? Consultation { get; private set; }
    public virtual Prescription? Prescription { get; private set; }

    // ✅ 业务方法：添加诊断
    public void AddConsultation(ConsultationData data)
    {
        // 1. 验证业务规则
        if (Consultation != null)
            throw new DomainException("病案已有诊断记录，不能重复添加");

        if (Status == MedicalCaseStatus.Completed)
            throw new DomainException("已完成的病案不能修改");

        // 2. 验证数据
        data.Validate();

        // 3. 创建诊断实体（共享主键）
        Consultation = new Consultation
        {
            Id = this.Id,  // 共享主键
            ChiefComplaint = data.ChiefComplaint,
            TCMDiagnosis = data.TCMDiagnosis,
            PresentIllness = data.PresentIllness,
            Inspection = data.Inspection,
            AuscultationOlfaction = data.AuscultationOlfaction,
            Inquiry = data.Inquiry,
            Palpation = data.Palpation,
            TreatmentPrinciple = data.TreatmentPrinciple,
            Remark = data.Remark,
            StartTime = data.StartTime,
            CreatedAt = DateTime.Now,
            CreatedBy = this.CreatedBy  // 继承病案创建人
        };

        // 4. 更新病案状态
        this.Status = MedicalCaseStatus.InProgress;

        // 5. 发布领域事件（可选）
        // AddDomainEvent(new ConsultationAddedEvent(this.Id, Consultation.Id));
    }

    // ✅ 业务方法：添加处方
    public void AddPrescription(PrescriptionData data)
    {
        // 1. 验证业务规则
        if (Consultation == null)
            throw new DomainException("病案尚未诊断，不能开处方");

        if (Prescription != null)
            throw new DomainException("病案已有处方，不能重复添加");

        if (Status == MedicalCaseStatus.Completed)
            throw new DomainException("已完成的病案不能修改");

        // 2. 验证数据
        data.Validate();

        // 3. 创建处方实体（传统FK）
        Prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = this.Id,
            PatientId = this.PatientId,
            UserId = this.UserId,
            Indication = data.Indication,
            DosageCount = data.DosageCount,
            Discount = data.Discount,
            Advice = data.Advice,
            FormulaSource = data.FormulaSource,
            Remark = data.Remark,
            Items = data.Items.Select(item => new PrescriptionItem
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                Unit = item.Unit
            }).ToList(),
            CreatedAt = DateTime.Now,
            CreatedBy = this.CreatedBy
        };
    }

    // ✅ 业务方法：完成病案
    public void Complete()
    {
        // 1. 验证完成条件
        if (Consultation == null)
            throw new DomainException("病案尚未诊断，不能完成");

        // Prescription可选（允许不开方）

        if (Status == MedicalCaseStatus.Completed)
            throw new DomainException("病案已完成");

        // 2. 更新状态
        this.Status = MedicalCaseStatus.Completed;
        this.UpdatedAt = DateTime.Now;

        // 3. 发布领域事件
        // AddDomainEvent(new MedicalCaseCompletedEvent(this.Id));
    }

    // ✅ 查询方法：是否可以完成
    public bool CanComplete() => Consultation != null && Status != MedicalCaseStatus.Completed;

    // ✅ 查询方法：是否可以开处方
    public bool CanAddPrescription() => Consultation != null && Prescription == null;
}
```

#### Service层调用聚合方法

```csharp
// src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs

public async Task<ConsultationDto> AddConsultationAsync(AddConsultationCommand command)
{
    _logger.LogInformation("开始为病案添加诊断，MedicalCaseId: {MedicalCaseId}", command.MedicalCaseId);

    // 1. 加载聚合根
    var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
    if (medicalCase == null)
        throw new NotFoundException($"病案不存在：{command.MedicalCaseId}");

    // 2. 创建值对象
    var consultationData = new ConsultationData
    {
        ChiefComplaint = command.ChiefComplaint,
        TCMDiagnosis = command.TCMDiagnosis,
        PresentIllness = command.PresentIllness,
        Inspection = command.Inspection,
        AuscultationOlfaction = command.AuscultationOlfaction,
        Inquiry = command.Inquiry,
        Palpation = command.Palpation,
        TreatmentPrinciple = command.TreatmentPrinciple,
        Remark = command.Remark,
        StartTime = command.StartTime
    };

    // 3. ✅ 调用聚合根业务方法（业务规则封装在聚合内）
    medicalCase.AddConsultation(consultationData);

    // 4. 保存聚合根（EF会自动保存Consultation）
    await _medicalCaseRepository.UpdateAsync(medicalCase);

    _logger.LogInformation("病案诊断添加成功，ConsultationId: {ConsultationId}", medicalCase.Consultation!.Id);

    // 5. 返回DTO
    return MapToConsultationDto(medicalCase.Consultation);
}
```

#### 单元测试

```csharp
// tests/UnitTests/Server/Core/LYBT.Entities.Tests/MedicalCase/MedicalCaseAggregateTests.cs

namespace LYBT.Entities.Tests.MedicalCase
{
    public class MedicalCaseAggregateTests
    {
        [Fact]
        public void AddConsultation_ValidData_Success()
        {
            // Arrange
            var medicalCase = CreateTestMedicalCase();
            var consultationData = new ConsultationData
            {
                ChiefComplaint = "头痛三日",
                TCMDiagnosis = "风寒感冒",
                StartTime = DateTime.Now
            };

            // Act
            medicalCase.AddConsultation(consultationData);

            // Assert
            Assert.NotNull(medicalCase.Consultation);
            Assert.Equal(medicalCase.Id, medicalCase.Consultation.Id);  // 共享主键
            Assert.Equal("头痛三日", medicalCase.Consultation.ChiefComplaint);
        }

        [Fact]
        public void AddConsultation_AlreadyHasConsultation_ThrowsException()
        {
            // Arrange
            var medicalCase = CreateTestMedicalCase();
            medicalCase.AddConsultation(new ConsultationData { ChiefComplaint = "test", TCMDiagnosis = "test" });

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                medicalCase.AddConsultation(new ConsultationData { ChiefComplaint = "test2", TCMDiagnosis = "test2" }));
        }

        [Fact]
        public void AddPrescription_WithoutConsultation_ThrowsException()
        {
            // Arrange
            var medicalCase = CreateTestMedicalCase();

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                medicalCase.AddPrescription(new PrescriptionData { DosageCount = 7 }));
        }

        [Fact]
        public void Complete_WithConsultationNoPrescription_Success()
        {
            // Arrange
            var medicalCase = CreateTestMedicalCase();
            medicalCase.AddConsultation(new ConsultationData { ChiefComplaint = "test", TCMDiagnosis = "test" });

            // Act
            medicalCase.Complete();

            // Assert
            Assert.Equal(MedicalCaseStatus.Completed, medicalCase.Status);
            // 验证：允许无处方完成
        }
    }
}
```

#### 验收标准

```bash
# 验证值对象创建
ls src/Server/Core/LYBT.Entities/MedicalCase/ValueObjects/

# 验证聚合方法
grep -A 20 "public void AddConsultation" src/Server/Core/LYBT.Entities/MedicalCase/MedicalCaseModel.cs

# 运行单元测试
dotnet test tests/UnitTests/Server/Core/LYBT.Entities.Tests/ --filter "MedicalCaseAggregateTests"
```

---

### Phase 5：文档同步（1小时）

**目标**：同步所有文档，记录架构决策

#### 创建ADR文档

```markdown
<!-- docs/architecture/adr/ADR-001-medical-case-aggregate-root.md -->

# ADR-001: MedicalCase聚合根设计

**状态**：已接受
**日期**：2025-10-21
**决策者**：架构团队

## 背景

前期设计中将"诊断"（Consultation）理解为核心流程，导致命名、架构和实现层面都体现"consultation为中心"的错误认知。

### 发现的问题

1. 分支名：refactor/consultation-workflow-complete
2. Event名：ConsultationCompletedEvent
3. 架构：ViewModel直接调用ConsultationRepository
4. DDD：绕过MedicalCase聚合根

## 决策

采用DDD聚合根模式，明确MedicalCase为核心：

1. **命名纠正**：所有命名体现"MedicalCase为主"
2. **架构边界**：通过MedicalCaseService操作子实体
3. **DDD建模**：实现聚合根业务方法

## 后果

### 正面影响

- ✅ 架构认知统一：MedicalCase是聚合根
- ✅ 业务规则内聚：封装在聚合内
- ✅ 未来扩展性：易于支持多次诊断/处方

### 负面影响

- ⚠️ 调用链变长：ViewModel → Service → Repository
- ⚠️ 学习成本：团队需理解DDD概念
- ⚠️ 重构工作量：13小时

## 遵循原则

- DDD聚合根模式
- MVP原则（分阶段执行，降低风险）
- 门面模式（保持模块边界）

## 相关决策

- ADR-002: 为什么采用门面模式而非internal Repository
```

```markdown
<!-- docs/architecture/adr/ADR-002-facade-pattern-for-repository-boundary.md -->

# ADR-002: Repository边界的门面模式

**状态**：已接受
**日期**：2025-10-21
**决策者**：架构团队

## 背景

DDD要求聚合根是唯一外部访问入口，但当前模块化设计中：
- LYBT.Module.Consultation（独立模块）
- LYBT.Module.Prescription（独立模块）
- LYBT.Module.MedicalCase（独立模块）

如何在保持模块边界的同时，实现DDD聚合根原则？

## 候选方案

### 方案A：Repository改为internal
- ConsultationRepository/PrescriptionRepository改为internal
- 问题：跨模块调用失败（MedicalCaseService无法访问）

### 方案B：合并模块
- 将Consultation/Prescription合并到MedicalCase模块
- 问题：单个模块过大，违反单一职责

### 方案C：门面模式（推荐）
- Repository保持public
- 通过MedicalCaseService门面协调
- ConsultationService/PrescriptionService作为内部门面

## 决策

采用方案C：门面模式

### 实现方式

```
外部调用链：
ViewModel → MedicalCaseService → ConsultationService → ConsultationRepository

内部门面：
MedicalCaseService（public，聚合根协调者）
  ↓ 调用
ConsultationService（public，模块门面）
  ↓ 调用
ConsultationRepository（public，但文档约束不直接调用）
```

### 约束机制

1. **文档约束**：开发指南明确要求通过MedicalCaseService
2. **Code Review**：审查禁止直接调用子实体Repository
3. **架构测试**：ArchUnit验证调用链（可选）

## 后果

### 优势

- ✅ 保持模块边界
- ✅ 符合DDD原则（逻辑上）
- ✅ 最小改动，风险低

### 劣势

- ⚠️ 无法编译层面强制约束
- ⚠️ 依赖团队纪律和Code Review

## 替代方案

如果未来模块边界调整，可考虑方案B（合并模块）。
```

#### 更新架构文档

**更新Server架构文档**：
```markdown
<!-- docs/architecture/server/README.md -->

## DDD聚合根设计

### MedicalCase聚合根

**聚合根**：MedicalCase
**聚合内实体**：Consultation、Prescription
**值对象**：ConsultationData、PrescriptionData

**业务方法**：
- `AddConsultation(ConsultationData)` - 添加诊断
- `AddPrescription(PrescriptionData)` - 添加处方
- `Complete()` - 完成病案

**访问规则**：
- ✅ 允许：通过MedicalCaseService操作
- ❌ 禁止：直接调用ConsultationRepository/PrescriptionRepository
```

**更新开发指南**：
```markdown
<!-- docs/development/server/README.md -->

## 病案聚合根开发规范

### 新增诊断

❌ **错误做法**：
```csharp
// 直接调用Repository
var dto = new ConsultationCreateDto { ... };
await _consultationRepository.CreateAsync(dto);
```

✅ **正确做法**：
```csharp
// 通过MedicalCaseService
var command = new AddConsultationCommand { ... };
await _medicalCaseService.AddConsultationAsync(command);
```

### 新增处方

同样通过MedicalCaseService.AddPrescriptionAsync()
```

#### 验收标准

```bash
# 验证ADR文档创建
ls docs/architecture/adr/ | grep "ADR-001"
ls docs/architecture/adr/ | grep "ADR-002"

# 验证架构文档更新
grep -r "MedicalCase聚合根" docs/architecture/server/
```

---

## 📊 验收标准汇总

### 命名层验收

```bash
# 1. 分支名正确
git branch | grep "medical-case-workflow-refactor"  # ✅ 存在

# 2. 文档名正确
ls docs/reports/ | grep "medical-case-workflow-analysis"  # ✅ 存在
ls docs/architecture/shared/ | grep "medical-case-aggregate-design"  # ✅ 存在

# 3. 无遗留错误命名
grep -r "consultation-workflow" docs/ | wc -l  # ✅ 应为0（除模块名）
grep -r "看诊流程" docs/ | wc -l  # ✅ 应为0

# 4. Event名称正确
grep -r "MedicalCaseStep2CompletedEvent" src/  # ✅ 找到引用
grep -r "ConsultationCompletedEvent" src/ | wc -l  # ✅ 应为0
```

### 架构层验收

```bash
# 1. ViewModel不直接依赖子实体Repository
grep -r "IConsultationRepository" src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ | wc -l
# ✅ 应为0

grep -r "IPrescriptionRepository" src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/ | wc -l
# ✅ 应为0

# 2. MedicalCaseService存在
ls src/Server/Modules/LYBT.Module.MedicalCase/Services/ | grep "MedicalCaseService"
# ✅ 找到

# 3. ViewModel使用MedicalCaseService
grep -r "IMedicalCaseService" src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/
# ✅ 找到
```

### DDD层验收

```bash
# 1. 值对象存在
ls src/Server/Core/LYBT.Entities/MedicalCase/ValueObjects/ | grep "ConsultationData"
# ✅ 找到

# 2. 聚合根业务方法
grep "public void AddConsultation" src/Server/Core/LYBT.Entities/MedicalCase/MedicalCaseModel.cs
# ✅ 找到

# 3. 单元测试覆盖
dotnet test tests/UnitTests/Server/Core/LYBT.Entities.Tests/ --filter "MedicalCaseAggregateTests"
# ✅ 通过
```

### 编译测试验收

```bash
# 1. 编译通过
dotnet build LYBT.All.sln -c Release --no-restore
# ✅ 0 errors, 0 warnings

# 2. 单元测试通过
dotnet test LYBT.All.sln -c Release --no-build
# ✅ 所有测试通过

# 3. 集成测试通过（如果有）
dotnet test tests/IntegrationTests/ -c Release
# ✅ 通过
```

---

## ⚠️ 风险评估

### 技术风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| Event改名遗漏引用 | 中 | 高 | grep全局搜索 + 编译验证 |
| Service调用链性能下降 | 低 | 低 | MedicalCaseService是轻量门面 |
| 测试覆盖不足导致回归bug | 中 | 中 | Phase 4补充单元测试 |

### 业务风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 团队理解成本高 | 中 | 中 | 详细文档 + Code Review |
| 开发者绕过聚合根直接调用 | 中 | 中 | Code Review + 架构测试 |

### 时间风险

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 13小时估算超时 | 中 | 中 | 分Phase执行，每Phase可独立验证 |
| Phase 4 DDD建模复杂度超预期 | 低 | 中 | 简化实现，优先门面模式 |

---

## 📈 成功指标

### 短期指标（Phase完成后）

- ✅ 所有命名体现"MedicalCase为主"
- ✅ ViewModel通过MedicalCaseService操作
- ✅ 编译0错误0警告
- ✅ 所有单元测试通过

### 长期指标（3个月后）

- ✅ 团队统一理解MedicalCase聚合根概念
- ✅ 无直接调用子实体Repository的Code Review违规
- ✅ 新功能开发遵循聚合根模式

---

## 🎓 设计经验总结

### 核心教训

1. **架构主题比命名更重要**
   - 不仅仅是改名，而是纠正架构认知
   - "consultation-workflow"的错误命名暴露了设计偏差

2. **DDD聚合根原则的价值**
   - 业务规则内聚在聚合根
   - 外部只能通过聚合根操作
   - 便于未来扩展（多次诊断/处方）

3. **MVP与架构纠正的平衡**
   - 分Phase执行降低风险
   - 每Phase可独立验证和回滚
   - 优先纠正命名（成本低，收益高）

4. **门面模式的妥协**
   - 无法编译层面强制约束
   - 依赖团队纪律和Code Review
   - 但保持了模块边界的清晰性

---

## 📚 参考资料

### DDD经典书籍

- Eric Evans - Domain-Driven Design (蓝皮书)
- Vaughn Vernon - Implementing Domain-Driven Design (红皮书)

### 项目相关文档

- [病案流程分析报告](medical-case-workflow-analysis-2025-10-21.md)
- [病案聚合设计讨论](../architecture/shared/medical-case-aggregate-design-discussion.md)
- [实体关系文档](../architecture/shared/clinical-workflow-entity-relationships.md)
- [GitHub Issue #1561](https://github.com/shouqitao/LYBTZYZS/issues/1561)

### ADR文档

- [ADR-001: MedicalCase聚合根设计](../architecture/adr/ADR-001-medical-case-aggregate-root.md)
- [ADR-002: Repository边界的门面模式](../architecture/adr/ADR-002-facade-pattern-for-repository-boundary.md)

---

## 📋 执行检查清单

### Phase 0（立即执行）

- [ ] Git分支改名
- [ ] GitHub Issue标题修改
- [ ] 文档改名（3个文件）
- [ ] 更新文档引用
- [ ] 提交Phase 0变更

### Phase 1（1小时）

- [ ] Event改名
- [ ] Payload改名
- [ ] 更新发布/订阅代码
- [ ] 编译验证
- [ ] 提交Phase 1变更

### Phase 2（3小时）

- [ ] 创建MedicalCaseService
- [ ] 创建Command DTO
- [ ] ViewModel重构
- [ ] DI注册
- [ ] 编译测试
- [ ] 提交Phase 2变更

### Phase 3（2小时）

- [ ] 修改PrescriptionConfiguration
- [ ] UI添加checkbox
- [ ] ViewModel添加SkipPrescription逻辑
- [ ] 测试验证
- [ ] 提交Phase 3变更

### Phase 4（6小时）

- [ ] 创建值对象
- [ ] MedicalCase添加业务方法
- [ ] Service调用聚合方法
- [ ] 编写单元测试
- [ ] 运行所有测试
- [ ] 提交Phase 4变更

### Phase 5（1小时）

- [ ] 创建ADR-001
- [ ] 创建ADR-002
- [ ] 更新架构文档
- [ ] 更新开发指南
- [ ] 提交Phase 5变更

### 最终验收

- [ ] 运行完整验收脚本
- [ ] 编译0错误0警告
- [ ] 所有测试通过
- [ ] Code Review通过
- [ ] 合并到主分支

---

**报告完成时间**：2025-10-21
**总工作量估算**：13小时
**优先级**：🔴 高（根本性架构偏差，必须纠正）
**下一步行动**：立即执行Phase 0命名纠正（15分钟）
