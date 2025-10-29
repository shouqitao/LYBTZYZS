# Consultation 模块肃清分析报告

**分析日期**：2025-10-21
**分析范围**：Consultation 模块完整代码（Server + Client）
**分析方法**：UltraThink 深度分析（25步推理）

---

## 📋 执行摘要

### 核心定位（用户要求）
> "Consultation 就单纯的是一个数据结构。当下除了增删查改以外，增加还得关联医案，目前没有设计其他功能。"

### 核心发现
1. **Controller 已明确废弃独立 CRUD**：注释标注"Consultation模块仅提供查询功能"，CUD方法已标记 `[Obsolete]`
2. **Service 层仍保留完整 CRUD 实现**：与 Controller 声明矛盾
3. **DTO 过度设计**：包含 Entity 中不存在的字段（StartTime/EndTime/ConsultationStatus）
4. **大量扩展功能**：统计、打印、复制、草稿、事件系统等，偏离"简单数据结构"定位
5. **状态管理混乱**：Entity 使用 CommonStatus，DTO 强行映射为 ConsultationStatus
6. **⭐ 违反聚合根边界**：Consultation 独立实现"当天可改"规则，应该由 MedicalCase 聚合根统一管理

### 肃清目标
- **删除**：统计功能、工作流机制、时间追踪、独立 CRUD、扩展功能、越权业务规则
- **简化**：DTO 设计、状态管理、验证机制
- **保留**：基础四诊字段、通过 MedicalCase 聚合根创建的机制
- **重构**：所有更新操作必须通过 MedicalCase 聚合根，规则检查统一在聚合根层面

---

## 🔍 分析范围

### Server 端（8个文件）
- `LYBT.Entities/Consultation/ConsultationModel.cs` ✅ 实体定义
- `LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs` ⚠️ DTO定义
- `LYBT.Module.Consultation/Services/ConsultationService.cs` ⚠️ 服务层
- `LYBT.Module.Consultation/Repositories/ConsultationRepository.cs` ⚠️ 仓储层
- `LYBT.Module.Consultation/Mapping/ConsultationMappingProfile.cs` ⚠️ 映射配置
- `LYBT.Module.Consultation/Validators/ConsultationCreateDtoValidator.cs` ✅ 验证器
- `LYBT.Module.Consultation/Validators/ConsultationUpdateDtoValidator.cs` ✅ 验证器
- `LYBT.WebAPI/Controllers/ConsultationController.cs` ⚠️ API控制器

### Client 端（6个文件）
- `LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs` ⚠️ 表单视图模型
- `LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs` ⚠️ 管理视图模型
- `LYBT.Desktop.Consultation/Models/ConsultationItem.cs` ✅ 客户端模型
- `LYBT.Desktop.Consultation/Repositories/ConsultationRepository.cs` ✅ 客户端仓储
- `LYBT.Desktop.Infrastructure/Events/ConsultationCompletedEvent.cs` ❌ 事件定义
- `LYBT.Desktop.Infrastructure/Events/ConsultationCompletedPayload.cs` ❌ 事件载荷

### Shared（2个文件）
- `LYBT.Shared.Models/Enums/RecordEnums.cs` ⚠️ ConsultationStatus枚举
- `LYBT.Desktop.Contracts/Api/IConsultationApi.cs` ⚠️ API接口定义

**图例**：✅ 符合定位 | ⚠️ 部分偏离 | ❌ 严重偏离

---

## 🚨 核心矛盾

### Controller 的明确声明 vs 实际实现

**ConsultationController.cs 注释**：
```csharp
/// <summary>
/// 创建诊疗记录（已废弃）
/// </summary>
/// <remarks>
/// ⚠️ 已废弃：请使用 POST /api/medicalcases/with-details 创建完整病案。
/// Consultation模块仅提供查询功能。
/// </remarks>
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案。Consultation模块仅提供查询功能。", true)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation(...)
```

**但实际情况**：
- ❌ Service 层仍保留完整 `CreateAsync`/`UpdateAsync`/`DeleteAsync` 实现
- ❌ Client 端仍有 `ConsultationFormViewModel.SaveAsync`（独立创建）
- ❌ IConsultationApi 仍定义完整 CRUD 接口
- ❌ Repository 提供完整数据访问能力

**结论**：Controller 已明确废弃独立 CRUD，但下层实现未同步清理。

---

## 📊 偏离项详细清单

### P0 - 严重偏离（必须删除）

#### 1. 统计功能（已被标记为 MVP 过度开发）

**涉及文件**：
- `ConsultationService.cs:271` - `GetStatisticsAsync` 方法
- `ConsultationController.cs:230` - `GetStatistics` endpoint（已标记 Obsolete）
- `ConsultationDtos.cs:308` - `ConsultationStatisticsDto` 类型
- `ConsultationManagementViewModel.cs:135` - `StatisticsCommand`

**偏离内容**：
```csharp
// ConsultationStatisticsDto
public class ConsultationStatisticsDto
{
    public int TotalCount { get; set; }
    public int TodayCount { get; set; }
    public double AvgDuration { get; set; }  // 平均诊疗时长
    public Dictionary<string, int> ByStatus { get; set; }  // 按状态统计
    public Dictionary<string, int> ByDoctor { get; set; }  // 按医生统计
}
```

**Controller 注释**：
```csharp
[Obsolete("统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。", true)]
```

**建议**：完全删除统计功能相关代码。

---

#### 2. 工作流机制（事件驱动设计）

**涉及文件**：
- `ConsultationService.cs:236` - `StartAsync` 方法（启动诊疗会话）
- `ConsultationFormViewModel.cs:194` - `PublishConsultationCompletedEvent`
- `ConsultationCompletedEvent.cs` - 事件定义
- `ConsultationCompletedPayload.cs` - 事件载荷（包含 IsDraft 字段）
- `IConsultationApi.cs:49` - `StartConsultationAsync` 接口

**偏离内容**：
```csharp
// StartAsync - 启动诊疗会话
public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
{
    var consultation = new ConsultationEntity
    {
        Id = Guid.NewGuid(),
        // 创建基础诊疗记录
    };
    // ...
}

// 事件发布
private void PublishConsultationCompletedEvent(Guid consultationId, bool isDraft)
{
    var payload = new ConsultationCompletedPayload
    {
        ConsultationId = consultationId,
        IsDraft = isDraft,  // 草稿机制
        // ...
    };
    EventAggregator.GetEvent<ConsultationCompletedEvent>().Publish(payload);
}
```

**建议**：删除工作流相关代码，Consultation 的创建应完全通过 MedicalCase 聚合根。

---

#### 3. 时间追踪字段（Entity 中不存在）

**涉及文件**：
- `ConsultationDtos.cs:71-78` - `ConsultationDto.StartTime/EndTime`
- `ConsultationDtos.cs:153-164` - `ConsultationDetailDto.StartTime/EndTime/Duration`
- `ConsultationMappingProfile.cs:22-23` - 映射配置中 Ignore 这些字段

**偏离内容**：
```csharp
// ConsultationDto 中定义
public DateTime StartTime { get; set; }
public DateTime? EndTime { get; set; }

// ConsultationDetailDto 中计算属性
public int Duration => EndTime.HasValue ? (int)(EndTime.Value - StartTime).TotalMinutes : 0;

// 但 Consultation 实体中没有这些字段！
// ConsultationModel.cs 中只有 BaseEntity 的 CreatedAt/UpdatedAt
```

**问题**：
- Entity 中没有 StartTime/EndTime 字段
- Mapping 时使用 `opt.Ignore()`，无法映射
- Service 中统计功能注释："实体中暂无时间字段"

**建议**：从 DTO 中删除 StartTime/EndTime/Duration，使用 CreatedAt/UpdatedAt 替代。

---

#### 4. 独立 CRUD API（已废弃但代码仍存在）

**涉及文件**：
- `ConsultationController.cs:85` - `CreateConsultation`（Obsolete）
- `ConsultationController.cs:123` - `UpdateConsultation`（Obsolete）
- `ConsultationController.cs:163` - `DeleteConsultation`（Obsolete）
- `ConsultationService.cs:105` - `CreateAsync`
- `ConsultationService.cs:143` - `UpdateAsync`
- `ConsultationService.cs:172` - `DeleteAsync`

**建议**：删除 Service 层的 CUD 方法，仅保留查询方法（GetByIdAsync/GetPagedAsync/GetByMedicalCaseIdAsync）。

---

### P1 - 中度偏离（需要简化）

#### 5. 状态管理（概念混乱）

**涉及文件**：
- `RecordEnums.cs:12` - `ConsultationStatus` 枚举
- `ConsultationMappingProfile.cs:16-17` - 状态映射逻辑
- `ConsultationDto.cs:81` - `ConsultationStatus` 属性

**偏离内容**：
```csharp
// 定义了专门的 ConsultationStatus
public enum ConsultationStatus
{
    Pending = 0,      // 等待开始
    InProgress = 1,   // 诊疗中
    Completed = 2,    // 已完成
    Cancelled = 3     // 已取消（代码中未找到）
}

// 但 Entity 使用的是 CommonStatus（Enabled/Disabled）
public CommonStatus Status { get; set; } = CommonStatus.Enabled;

// Mapping 中强行映射
.ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src =>
    src.Status == CommonStatus.Disabled ? ConsultationStatus.Completed : ConsultationStatus.InProgress))
```

**问题**：
- Entity 和 DTO 使用不同的状态枚举
- 强行映射逻辑（Disabled=Completed, Enabled=InProgress）
- Pending/Cancelled 状态无法从 Entity 映射

**建议**：
- 删除 `ConsultationStatus` 枚举
- DTO 直接使用 `CommonStatus`
- 或在 Entity 中明确添加状态字段（但需评估是否必要）

---

#### 6. DTO 过度设计

**涉及文件**：
- `ConsultationDtos.cs:14` - `ConsultationDto`
- `ConsultationDtos.cs:91` - `ConsultationDetailDto`
- `ConsultationDtos.cs:180` - `ConsultationInputBaseDto`（抽象基类）
- `ConsultationDtos.cs:294` - `ConsultationValidationResult`

**偏离内容**：

**a) 两个功能重叠的 DTO**：
```csharp
// ConsultationDto - 包含所有字段
public class ConsultationDto : StatusDto, IRemarkable { ... }

// ConsultationDetailDto - 继承 TimestampDto，字段几乎一样，多了 Duration/IsCompleted
public class ConsultationDetailDto : TimestampDto, IRemarkable
{
    public int Duration => EndTime.HasValue ? (int)(EndTime.Value - StartTime).TotalMinutes : 0;
    public bool IsCompleted => ConsultationStatus == ConsultationStatus.Completed;
}
```

**b) 冗余字段**（应从 MedicalCase 获取）：
```csharp
public Guid PatientId { get; set; }        // MedicalCase.PatientId
public Guid UserId { get; set; }           // MedicalCase.UserId
public string? PatientName { get; set; }   // MedicalCase.PatientName
public string? DoctorName { get; set; }    // MedicalCase.DoctorName
```

**c) 验证结果类型**（可能冗余）：
```csharp
public class ConsultationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}
```

**建议**：
- 合并 `ConsultationDto` 和 `ConsultationDetailDto`
- 删除冗余字段（PatientId/UserId/PatientName/DoctorName）
- 评估 `ConsultationValidationResult` 是否必要（可能直接使用 FluentValidation 的结果）

---

#### 7. 扩展功能（超出基础 CRUD）

**涉及文件**：
- `ConsultationManagementViewModel.cs:120-140` - 扩展命令
- `ConsultationFormViewModel.cs:368` - `ImportFromHistoryCommand`

**偏离内容**：
```csharp
// 查看处方
public DelegateCommand<ConsultationDto> ViewPrescriptionCommand { get; }

// 打印
public DelegateCommand<ConsultationDto> PrintCommand { get; }

// 复制记录
public DelegateCommand<ConsultationDto> CopyRecordCommand { get; }

// 从历史导入
public DelegateCommand ImportFromHistoryCommand { get; }

// 统计
public ICommand StatisticsCommand { get; }
```

**实现状态**：所有方法都是"功能开发中"，仅打印日志。

**建议**：删除这些扩展功能，仅保留基础的查看和列表功能。

---

#### 8. 功能开关机制

**涉及文件**：
- `ConsultationManagementViewModel.cs:67-76` - 功能开关属性

**偏离内容**：
```csharp
/// <summary>
/// 是否允许查看详情
/// </summary>
public bool CanViewDetail => _featureToggleService.IsEnabled("Consultation.ViewDetail");

/// <summary>
/// 是否允许搜索
/// </summary>
public bool CanSearch => _featureToggleService.IsEnabled("Consultation.Search");
```

**建议**：评估是否必要。如果 Consultation 只是简单数据结构，可能不需要功能开关。

---

### P0 - 严重偏离（必须删除）续

#### 5. 业务规则越权实现（违反聚合根边界）⭐

**涉及文件**：
- `ConsultationService.cs:155-162` - UpdateAsync 中的日期检查

**偏离内容**：
```csharp
// RULE-3: 当天可改隔日锁定 - 只能修改创建当天的记录
if (entity.CreatedAt.Date != DateTime.Today)
{
    _logger.LogWarning("更新诊疗记录失败：记录 {ConsultationId} 创建于 {CreatedDate}，已过可修改期限", id, entity.CreatedAt.Date);
    return ServiceResult<ConsultationDto>.Failure("该诊疗记录已超过可修改期限（仅限创建当天可修改）");
}
```

**问题分析**：
1. ❌ **规则所有者错误**："当天可改"是 MedicalCase（病案）的规则，不是 Consultation 的规则
2. ❌ **依赖关系倒置**：Consultation 的可修改性应该依赖于 MedicalCase 的可修改性
3. ❌ **违反聚合根边界**：既然 Consultation 通过 MedicalCase 聚合根创建，修改规则也应该由聚合根统一管理

**用户澄清**：
> "当天可改是关联病案的。病案当天可修改。所以在病案可修改的前提下，诊断结果当天也是可以修改的。"

**正确设计**：
```csharp
// ❌ 错误：Consultation 独立判断
public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto)
{
    if (entity.CreatedAt.Date != DateTime.Today)  // 不应该在这里检查
        return Failure("已超过可修改期限");
}

// ✅ 正确：通过 MedicalCase 聚合根更新
public async Task<ServiceResult> UpdateConsultation(Guid medicalCaseId, ConsultationUpdateDto dto)
{
    var medicalCase = await GetByIdAsync(medicalCaseId);

    // 统一在聚合根层面检查规则
    if (medicalCase.CreatedAt.Date != DateTime.Today)
        return Failure("病案已超过可修改期限");

    // 更新 Consultation（作为 MedicalCase 的一部分）
    medicalCase.Consultation.Update(dto);
    await UpdateAsync(medicalCase);
}
```

**建议**：
- 删除 `ConsultationService.UpdateAsync` 中的日期检查逻辑
- 所有更新必须通过 `MedicalCaseService.UpdateConsultation`
- 规则检查统一在 MedicalCase 聚合根层面

---

## 🎯 肃清建议

### 保留的内容

#### ✅ Entity 层（无需修改）
- `ConsultationModel.cs` - 完全符合定位，仅包含四诊字段
- 与 MedicalCase 的一对一关系（共享主键）
- 基础审计字段（继承自 BaseEntity）

#### ✅ 基础查询功能
- `GetByIdAsync` - 根据 ID 查询
- `GetPagedAsync` - 分页查询
- `GetByMedicalCaseIdAsync` - 根据医案 ID 查询
- `SearchAsync` - 关键字搜索

#### ✅ 验证器
- `ConsultationCreateDtoValidator` - 创建时验证
- `ConsultationUpdateDtoValidator` - 更新时验证

---

### 删除的内容

#### ❌ 统计功能（完全删除）
- `ConsultationService.GetStatisticsAsync`
- `ConsultationController.GetStatistics`
- `ConsultationStatisticsDto`
- `ConsultationManagementViewModel.StatisticsCommand`

#### ❌ 工作流机制（完全删除）
- `ConsultationService.StartAsync`
- `ConsultationCompletedEvent`
- `ConsultationCompletedPayload`
- `IConsultationApi.StartConsultationAsync`
- `ConsultationFormViewModel.PublishConsultationCompletedEvent`

#### ❌ 独立 CRUD（删除 Service 实现）
- `ConsultationService.CreateAsync`（保留接口，内部调用 MedicalCaseService）
- `ConsultationService.UpdateAsync`（保留接口，内部调用 MedicalCaseService）
- `ConsultationService.DeleteAsync`（完全删除）

#### ❌ 扩展功能（完全删除）
- `ViewPrescriptionCommand`
- `PrintCommand`
- `CopyRecordCommand`
- `ImportFromHistoryCommand`
- 分页命令（FirstPage/LastPage/Previous/Next）

---

### 简化的内容

#### 🔧 DTO 设计
**当前**：
- `ConsultationDto`（包含冗余字段）
- `ConsultationDetailDto`（功能重叠）
- `ConsultationInputBaseDto`（抽象基类）

**建议**：
```csharp
// 简化为单一 DTO
public class ConsultationDto : StatusDto, IRemarkable
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }  // 关联医案

    // 基础诊断信息（必填）
    public string? ChiefComplaint { get; set; }
    public string? TCMDiagnosis { get; set; }

    // 四诊信息（可选）
    public string? Inspection { get; set; }
    public string? AuscultationOlfaction { get; set; }
    public string? Inquiry { get; set; }
    public string? Palpation { get; set; }

    // 其他信息（可选）
    public string? PresentIllness { get; set; }
    public string? TreatmentPrinciple { get; set; }
    public string? MedicalAdvice { get; set; }
    public string? Remark { get; set; }

    // 删除：PatientId/UserId/PatientName/DoctorName（从 MedicalCase 获取）
    // 删除：StartTime/EndTime/Duration（使用 CreatedAt/UpdatedAt）
    // 删除：ConsultationStatus（使用 CommonStatus）
}
```

#### 🔧 状态管理
**删除**：`ConsultationStatus` 枚举
**使用**：`CommonStatus`（Enabled/Disabled）
**Mapping**：直接映射，无需转换逻辑

---

## 📝 肃清执行清单

### Phase 1: 删除过度功能（3个Task）

#### Task 1.1: 删除统计功能
- [ ] `ConsultationService.cs:271` - 删除 `GetStatisticsAsync` 方法
- [ ] `ConsultationController.cs:230` - 删除 `GetStatistics` endpoint
- [ ] `ConsultationDtos.cs:308` - 删除 `ConsultationStatisticsDto` 类型
- [ ] `ConsultationManagementViewModel.cs:135` - 删除 `StatisticsCommand`
- [ ] 删除相关测试用例

#### Task 1.2: 删除工作流机制
- [ ] `ConsultationService.cs:236` - 删除 `StartAsync` 方法
- [ ] `ConsultationCompletedEvent.cs` - 删除整个文件
- [ ] `ConsultationCompletedPayload.cs` - 删除整个文件
- [ ] `IConsultationApi.cs:49` - 删除 `StartConsultationAsync` 接口
- [ ] `ConsultationFormViewModel.cs:194` - 删除 `PublishConsultationCompletedEvent` 方法
- [ ] 删除相关测试用例

#### Task 1.3: 删除扩展功能
- [ ] `ConsultationManagementViewModel.cs` - 删除以下命令：
  - `ViewPrescriptionCommand`
  - `PrintCommand`
  - `CopyRecordCommand`
  - `StatisticsCommand`
  - `FirstPageCommand/LastPageCommand/PreviousPageCommand/NextPageCommand`
- [ ] `ConsultationFormViewModel.cs:368` - 删除 `ImportFromHistoryCommand`
- [ ] 删除对应的命令实现方法

---

### Phase 2: 简化 DTO 设计（2个Task）

#### Task 2.1: 合并和简化 DTO
- [ ] `ConsultationDtos.cs` - 合并 `ConsultationDto` 和 `ConsultationDetailDto`
- [ ] 删除 `ConsultationDetailDto` 类型
- [ ] 删除 `ConsultationInputBaseDto` 抽象基类
- [ ] 从 DTO 中删除字段：
  - `StartTime`
  - `EndTime`
  - `Duration`（计算属性）
  - `ConsultationStatus`
  - `PatientId`/`UserId`
  - `PatientName`/`DoctorName`
- [ ] 更新 Mapping 配置

#### Task 2.2: 统一状态管理
- [ ] 删除 `ConsultationStatus` 枚举（`RecordEnums.cs:12`）
- [ ] DTO 直接使用 `CommonStatus`
- [ ] 删除 Mapping 中的状态转换逻辑
- [ ] 更新所有引用 `ConsultationStatus` 的代码

---

### Phase 3: 重构 Service 层（2个Task）

#### Task 3.1: 调整 CRUD 方法
- [ ] 删除 `ConsultationService.CreateAsync`（完全删除或改为调用 MedicalCaseService）
- [ ] 删除 `ConsultationService.UpdateAsync`（改为调用 MedicalCaseService）
- [ ] 删除 `ConsultationService.DeleteAsync`
- [ ] 保留查询方法：
  - `GetByIdAsync`
  - `GetPagedAsync`
  - `GetByMedicalCaseIdAsync`
  - `SearchAsync`
- [ ] 更新 `IConsultationService` 接口

#### Task 3.2: 删除越权业务规则 ⭐
- [ ] `ConsultationService.cs:155-162` - 删除"当天可改隔日锁定"检查逻辑
- [ ] 确认 `MedicalCaseService` 中已有统一的日期检查
- [ ] 文档说明：所有修改规则由 MedicalCase 聚合根统一管理

---

### Phase 4: 清理 API 层（1个Task）

#### Task 4.1: 清理 Controller
- [ ] `ConsultationController.cs` - 完全删除已标记 Obsolete 的方法：
  - `CreateConsultation`
  - `UpdateConsultation`
  - `DeleteConsultation`
  - `GetStatistics`
- [ ] 保留查询方法：
  - `GetConsultations`（分页）
  - `GetById`
  - `GetByMedicalCaseId`
  - `Search`

---

### Phase 5: 更新测试用例（1个Task）

#### Task 5.1: 调整测试用例
- [ ] 删除统计功能测试
- [ ] 删除工作流测试
- [ ] 删除扩展功能测试
- [ ] 更新 DTO 映射测试
- [ ] 更新 Service 测试（仅测试查询功能）
- [ ] 更新 Controller 测试

---

### Phase 6: 文档同步（1个Task）

#### Task 6.1: 更新文档
- [ ] `README.md` - 更新 Consultation 模块说明
- [ ] `docs/explanation/architecture/server/consultation-module.md` - 更新架构文档
- [ ] `docs/reference/api/consultation-api.md` - 删除废弃的 API 文档
- [ ] `docs/how-to-guides/consultation-development-guide.md` - 更新开发指南

---

## 📈 预期成果

### 代码行数减少
- **删除文件**：2个（Event 相关）
- **简化 DTO**：约减少 200 行
- **删除 Service 方法**：约减少 150 行
- **删除 ViewModel 命令**：约减少 100 行
- **总计**：约减少 450-500 行代码

### 复杂度降低
- **状态管理**：从 2 套状态系统简化为 1 套
- **DTO 数量**：从 5 个简化为 3 个
- **Service 方法**：从 9 个简化为 4 个
- **API Endpoint**：从 8 个简化为 4 个

### 清晰度提升
- ✅ 明确 Consultation 仅作为数据结构
- ✅ 所有创建/更新通过 MedicalCase 聚合根
- ✅ 无独立工作流和状态机
- ✅ 无统计和扩展功能

---

## ⚠️ 风险评估

### 低风险
- **删除统计功能**：Controller 已标记 Obsolete，确认无依赖
- **删除工作流**：事件仅在创建时发布，可通过 MedicalCase 流程替代
- **删除扩展功能**：所有方法都是"功能开发中"，无实际实现

### 中风险
- **删除独立 CRUD**：需确认 Client 端是否直接调用 ConsultationApi
- **简化 DTO**：需确认 PatientName/DoctorName 的获取方式（可能需要调整 Mapping）

### 高风险
- **删除时间字段**：需确认是否有报表或统计依赖 StartTime/EndTime（但统计功能已确认为过度开发，可以删除）

---

## 🔄 后续行动

### 优先级 P0（本周完成）
1. 创建 GitHub Issue 跟踪肃清计划
2. 执行 Phase 1（删除过度功能）
3. 执行 Phase 2（简化 DTO）

### 优先级 P1（下周完成）
1. 执行 Phase 3（重构 Service）
2. 执行 Phase 4（清理 API）
3. 执行 Phase 5（更新测试）

### 优先级 P2（两周内完成）
1. 执行 Phase 6（文档同步）
2. 代码审查和合并
3. 部署验证

---

## 📎 附录

### 文件清单（完整路径）

#### Server 端
```
src/Server/Core/LYBT.Entities/Consultation/ConsultationModel.cs
src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDtos.cs
src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs
src/Server/Modules/LYBT.Module.Consultation/Repositories/ConsultationRepository.cs
src/Server/Modules/LYBT.Module.Consultation/Mapping/ConsultationMappingProfile.cs
src/Server/Modules/LYBT.Module.Consultation/Validators/ConsultationCreateDtoValidator.cs
src/Server/Modules/LYBT.Module.Consultation/Validators/ConsultationUpdateDtoValidator.cs
src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs
```

#### Client 端
```
src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs
src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs
src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Models/ConsultationItem.cs
src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Repositories/ConsultationRepository.cs
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/ConsultationCompletedEvent.cs
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/ConsultationCompletedPayload.cs
```

#### Shared
```
src/Shared/LYBT.Shared.Models/Enums/RecordEnums.cs
src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IConsultationApi.cs
```

---

**报告生成时间**：2025-10-21
**分析工具**：Claude Code + Sequential-Thinking
**审核状态**：待用户确认
