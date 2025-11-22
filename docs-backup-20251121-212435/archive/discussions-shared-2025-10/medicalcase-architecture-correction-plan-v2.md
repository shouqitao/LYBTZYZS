# MedicalCase架构纠正方案 v2.0（保留模块版）

> **文档版本**: v2.0（基于用户要求：保留Consultation和Prescription模块）
> **创建日期**: 2025-10-18
> **分析方法**: Sequential-thinking (15步深度分析)
> **优先级**: P0（关键架构调整，立即执行）
> **方案对比**: 对比v1.0（模块合并方案），v2.0更适合长期迭代项目

---

## 📋 执行摘要

### 用户核心要求

> **用户决策**："我的建议是调整MedicalCase为主框架，Consultation和Prescription两个模块不管前端还是后端都不删除。只需要简化MVP版本中的功能即可。因为后期会扩展这两个模块的功能。"

### v2方案核心思路

**架构原则**：
- ✅ **MedicalCase为聚合根**（DDD原则不变）
- ✅ **模块保留**（Server + Desktop两个模块都保留）
- ✅ **功能分层**（写入层 vs 查询层 vs 辅助层）
- ✅ **MVP控制**（通过配置控制功能启用/禁用）
- ✅ **长远规划**（为AI诊断、智能推荐等扩展预留空间）

**修正范围**：
- Server端：API层级调整（标记废弃 + 新增API）
- Desktop端：功能简化（通过FeatureToggle控制MVP启用范围）
- 文档：模块职责说明 + 扩展路线图

**执行时间**：4-5天
- Phase 1：Server端API调整（1-2天，P0）
- Phase 2：Desktop端功能简化（1-2天，P0）
- Phase 3：文档更新（0.5-1天，P1）
- Phase 4：扩展规划文档（0.5天，P2，可选）

---

## 1. 架构设计原则（v2版本）

### 1.1 DDD聚合根原则（不变）

**MedicalCase是唯一聚合根**：
- Consultation和Prescription仍然是MedicalCase的**组成部分**（实体或值对象）
- 外部对象只能持有MedicalCase.Id引用
- 所有写入操作必须通过MedicalCase聚合根进行

**1:1:1关系**：
```
MedicalCase (聚合根)
├─ Id (主键)
├─ Consultation (1:1, 共享主键: Consultation.Id == MedicalCase.Id)
└─ Prescription (1:1, 共享主键: Prescription.Id == MedicalCase.Id)
```

### 1.2 模块功能分层（v2核心设计）

**三层功能划分**：

1. **写入层（Write Side）**：
   - **原则**：只能通过MedicalCase聚合根
   - **Server端**：只有MedicalCaseController提供写入API
   - **Desktop端**：只有MedicalCaseEntryViewModel提供CRUD功能
   - **强制性**：Consultation/Prescription的独立写入API标记Obsolete（error=true）

2. **查询层（Read Side）**：
   - **原则**：可独立查询（只读），不违反聚合根原则
   - **Server端**：ConsultationController和PrescriptionsController提供只读查询API
   - **Desktop端**：ConsultationManagementViewModel和PrescriptionManagementViewModel提供只读查询
   - **功能**：历史记录查询、搜索、统计、详情展示

3. **辅助层（Helper Functions）**：
   - **原则**：工具函数、模板、推荐等，不修改聚合根状态
   - **Server端**：处方复用（Clone）、模板查询、配伍检查等
   - **Desktop端**：处方编辑器对话框、药材选择器、验方导入等
   - **后期扩展**：AI诊断、智能推荐、知识库查询

### 1.3 MVP与扩展分离

**MVP版本（当前阶段）**：
- 只启用核心功能：病案录入（CRUD）、历史查询、处方复用
- 通过FeatureToggle配置控制功能启用/禁用
- Consultation/Prescription模块的Create/Edit/Delete在MVP阶段禁用

**后期扩展（Post-MVP）**：
- 通过修改配置文件启用高级功能（无需重构代码）
- Consultation扩展：诊断知识库、症状分析、AI辅助诊断
- Prescription扩展：处方模板管理、智能推荐、配伍检查、成本估算

---

## 2. Server端调整方案

### 2.1 API层级重新分类

#### MedicalCaseController（主API，写入层）

**职责**：聚合根的CRUD操作

**现有API（保留）**：
- ✅ `POST /api/v1/medicalcases/with-details` - 创建完整病案（已实现）
- ✅ `GET /api/v1/medicalcases/{id}` - 查询病案
- ✅ `DELETE /api/v1/medicalcases/{id}` - 删除病案（级联删除）

**新增API**：
- 🆕 `PUT /api/v1/medicalcases/{id}/consultation` - 更新病案的诊断信息
- 🆕 `PUT /api/v1/medicalcases/{id}/prescription` - 更新病案的处方信息

**实现示例**：
```csharp
/// <summary>
/// 更新病案的诊断信息
/// </summary>
[HttpPut("{id}/consultation")]
[ProducesResponseType(typeof(ApiResponse<ConsultationDto>), 200)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(
    Guid id,
    [FromBody] ConsultationUpdateDto dto)
{
    try
    {
        var validationResult = ValidateGuid<ConsultationDto>(id, "病案ID");
        if (validationResult != null) return validationResult;

        var modelValidationResult = ValidateModel<ConsultationDto>();
        if (modelValidationResult != null) return modelValidationResult;

        // 通过聚合根服务更新
        var result = await _medicalCaseService.UpdateConsultationAsync(id, dto);
        
        if (result.IsSuccess)
        {
            LogOperation("更新病案诊断信息", dto, id);
        }
        
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<ConsultationDto>(ex, "更新病案诊断信息", new { MedicalCaseId = id, UpdateData = dto });
    }
}

/// <summary>
/// 更新病案的处方信息
/// </summary>
[HttpPut("{id}/prescription")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> UpdatePrescription(
    Guid id,
    [FromBody] PrescriptionUpdateDto dto)
{
    // 类似实现
}
```

---

#### ConsultationController（查询API，只读层）

**职责**：诊疗记录的只读查询

**保留的API（只读）**：
- ✅ `GET /api/v1/consultations` - 分页查询诊疗记录
- ✅ `GET /api/v1/consultations/{id}` - 获取诊疗详情
- ✅ `GET /api/v1/consultations/medicalcase/{medicalCaseId}` - 根据医案ID查询诊疗
- ✅ `GET /api/v1/consultations/search` - 搜索诊疗记录

**废弃的API（写入）**：
- ❌ `POST /api/v1/consultations` - CreateConsultation（标记Obsolete error=true）
- ❌ `PUT /api/v1/consultations/{id}` - UpdateConsultation（标记Obsolete error=true）
- ❌ `DELETE /api/v1/consultations/{id}` - DeleteConsultation（标记Obsolete error=true）

**废弃的API（MVP过度开发）**：
- ❌ `GET /api/v1/consultations/statistics` - 统计功能（标记Obsolete error=true，MVP阶段属于过度开发）

**修改示例**：
```csharp
/// <summary>
/// 创建诊疗记录
/// </summary>
/// <remarks>
/// ❌ 已废弃：请通过 POST /api/medicalcases/with-details 创建完整病案。
/// Consultation模块在v2架构中仅提供查询功能，所有写入操作必须通过MedicalCase聚合根。
/// </remarks>
[HttpPost]
[Obsolete("请使用 POST /api/medicalcases/with-details 创建完整病案。Consultation模块仅提供查询功能。", true)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> CreateConsultation([FromBody] ConsultationCreateDto dto)
{
    // 实现保留（向后兼容），但编译时报错
}

/// <summary>
/// 更新诊疗信息
/// </summary>
/// <remarks>
/// ❌ 已废弃：请通过 PUT /api/medicalcases/{id}/consultation 更新诊断信息。
/// </remarks>
[HttpPut("{id}")]
[Obsolete("请使用 PUT /api/medicalcases/{id}/consultation 更新诊断信息。Consultation模块仅提供查询功能。", true)]
public async Task<ActionResult<ApiResponse<ConsultationDto>>> UpdateConsultation(...)
{
    // 实现保留，但编译时报错
}

/// <summary>
/// 删除诊疗记录（软删除）
/// </summary>
/// <remarks>
/// ❌ 已废弃：请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除诊疗和处方）。
/// </remarks>
[HttpDelete("{id}")]
[Obsolete("请通过 DELETE /api/medicalcases/{id} 删除病案（级联删除）。Consultation模块仅提供查询功能。", true)]
public async Task<ActionResult<ApiResponse>> DeleteConsultation(...)
{
    // 实现保留，但编译时报错
}

/// <summary>
/// 获取诊疗统计数据
/// </summary>
/// <remarks>
/// ❌ 已废弃：统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。
/// </remarks>
[HttpGet("statistics")]
[Obsolete("统计功能在MVP版本中属于过度开发，暂不提供。Post-MVP阶段将重新评估需求。", true)]
public async Task<ActionResult<ApiResponse<ConsultationStatisticsDto>>> GetStatistics(...)
{
    // 实现保留，但编译时报错
}
```

---

#### PrescriptionsController（查询 + 辅助功能API）

**职责**：处方记录的只读查询和辅助功能

**保留的API（只读查询）**：
- ✅ `GET /api/v1/prescriptions` - 分页查询处方
- ✅ `GET /api/v1/prescriptions/{id}` - 获取处方详情
- ✅ `GET /api/v1/prescriptions/search` - 搜索处方
- ✅ `GET /api/v1/prescriptions/patient/{patientId}/recent` - 患者近期处方

**保留的API（辅助功能）**：
- ✅ `POST /api/v1/prescriptions/{sourcePrescriptionId}/clone-to-medicalcase/{targetMedicalCaseId}` - 处方复用（修改参数）

**废弃的API（写入）**：
- ❌ `POST /api/v1/prescriptions` - Add（标记Obsolete error=true）
- ❌ `PUT /api/v1/prescriptions/{id}` - Update（标记Obsolete error=true）
- ❌ `DELETE /api/v1/prescriptions/{id}` - Delete（标记Obsolete error=true）

**废弃的API（MVP过度开发）**：
- ❌ `GET /api/v1/prescriptions/statistics` - 统计功能（标记Obsolete error=true，MVP阶段属于过度开发）

**关键修改：Clone API参数调整**：
```csharp
/// <summary>
/// 处方复用：将历史处方克隆到指定病案
/// </summary>
/// <param name="sourcePrescriptionId">源处方ID</param>
/// <param name="targetMedicalCaseId">目标病案ID（修改：原为targetConsultationId）</param>
/// <remarks>
/// v2架构调整：目标参数从targetConsultationId改为targetMedicalCaseId，
/// 确保所有操作都通过MedicalCase聚合根进行。
/// </remarks>
[HttpPost("{sourcePrescriptionId}/clone-to-medicalcase/{targetMedicalCaseId}")]
[ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 200)]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ClonePrescriptionToMedicalCase(
    Guid sourcePrescriptionId,
    Guid targetMedicalCaseId)
{
    try
    {
        // 实现逻辑：
        // 1. 加载targetMedicalCase（聚合根）
        var targetMedicalCase = await _medicalCaseRepository.GetByIdAsync(targetMedicalCaseId);
        if (targetMedicalCase == null)
        {
            return NotFound(ApiResponse<PrescriptionDto>.CreateFail("目标病案不存在"));
        }
        
        // 2. 复制sourcePrescription
        var sourcePrescription = await _prescriptionRepository.GetByIdAsync(sourcePrescriptionId);
        if (sourcePrescription == null)
        {
            return NotFound(ApiResponse<PrescriptionDto>.CreateFail("源处方不存在"));
        }
        
        // 3. 通过MedicalCaseService更新处方（保持聚合根边界）
        var prescriptionDto = MapToPrescriptionDto(sourcePrescription);
        var result = await _medicalCaseService.UpdatePrescriptionAsync(targetMedicalCaseId, prescriptionDto);
        
        return HandleServiceResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<PrescriptionDto>(ex, "处方复用", new { sourcePrescriptionId, targetMedicalCaseId });
    }
}
```

---

### 2.2 服务层调整

**MedicalCaseService新增方法**：

```csharp
// src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs

public async Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto)
{
    try
    {
        // 1. 加载MedicalCase聚合根
        var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
        if (medicalCase == null)
        {
            return ServiceResult<ConsultationDto>.Fail("病案不存在");
        }
        
        // 2. 更新Consultation（聚合根内部操作）
        var consultation = await _consultationRepository.GetByIdAsync(medicalCaseId); // 共享主键
        if (consultation == null)
        {
            return ServiceResult<ConsultationDto>.Fail("诊疗记录不存在");
        }
        
        // 更新字段
        _mapper.Map(dto, consultation);
        consultation.UpdatedAt = DateTime.UtcNow;
        
        // 3. 保存（事务边界由聚合根控制）
        await _consultationRepository.UpdateAsync(consultation);
        
        // 4. 更新MedicalCase的UpdatedAt（聚合根时间戳）
        medicalCase.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(medicalCase);
        
        var result = _mapper.Map<ConsultationDto>(consultation);
        return ServiceResult<ConsultationDto>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新病案诊断信息失败: MedicalCaseId={MedicalCaseId}", medicalCaseId);
        return ServiceResult<ConsultationDto>.Fail($"更新失败: {ex.Message}");
    }
}

public async Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)
{
    // 类似实现
}
```

---

## 3. Desktop端调整方案

### 3.1 功能开关机制（FeatureToggle）

**实现简单的FeatureToggleService**：

```csharp
// src/Client/Desktop/LYBT.Desktop.Infrastructure/Services/FeatureToggleService.cs

public interface IFeatureToggleService
{
    bool IsEnabled(string featureKey);
}

public class FeatureToggleService : IFeatureToggleService
{
    private readonly Dictionary<string, bool> _features;

    public FeatureToggleService(IConfiguration configuration)
    {
        // 从appsettings.json读取配置
        _features = configuration.GetSection("FeatureToggles")
            .Get<Dictionary<string, bool>>() ?? new Dictionary<string, bool>();
    }

    public bool IsEnabled(string featureKey)
    {
        return _features.TryGetValue(featureKey, out var enabled) && enabled;
    }
}
```

**appsettings.json配置**：

```json
{
  "FeatureToggles": {
    // Consultation模块功能开关（MVP阶段）
    "Consultation.Create": false,              // MVP: 禁用独立创建
    "Consultation.Edit": false,                // MVP: 禁用独立编辑
    "Consultation.Delete": false,              // MVP: 禁用独立删除
    "Consultation.ViewDetail": true,           // MVP: 启用查看详情
    "Consultation.Search": true,               // MVP: 启用搜索

    // Prescription模块功能开关（MVP阶段）
    "Prescription.Create": false,              // MVP: 禁用独立创建
    "Prescription.Delete": false,              // MVP: 禁用独立删除
    "Prescription.Clone": true,                // MVP: 启用处方复用（重要）
    "Prescription.Export": true,               // MVP: 启用导出（实用）
    "Prescription.ViewDetail": true,           // MVP: 启用查看详情
    "Prescription.Search": true,               // MVP: 启用搜索

    // MedicalCase模块功能开关（MVP核心功能）
    "MedicalCase.Create": true,                // MVP: 启用创建
    "MedicalCase.Edit": true,                  // MVP: 启用编辑
    "MedicalCase.Delete": true,                // MVP: 启用删除
    "MedicalCase.ViewDetail": true,            // MVP: 启用查看详情
    "MedicalCase.Search": true                 // MVP: 启用搜索
  }
}
```

**注意**：
- ⚠️ **Statistics统计功能已直接废弃**（通过Obsolete error=true标记），不在FeatureToggle中配置
- MVP阶段不包含任何统计功能（属于过度开发）

**DI容器注册**：

```csharp
// src/Client/Desktop/App.xaml.cs

services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
```

---

### 3.2 LYBT.Desktop.Consultation模块调整

**职责重新定位**：
- **MVP阶段**：只读历史查询模块
- **Post-MVP**：诊断知识库、症状分析、AI辅助诊断

**ConsultationManagementViewModel调整**：

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs

/// <summary>
/// 诊疗记录管理视图模型 - v2架构调整
/// MVP阶段：只读查询功能
/// 后期扩展：诊断知识库、症状分析、AI辅助诊断
/// </summary>
public class ConsultationManagementViewModel : ViewModelBase
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IFeatureToggleService _featureToggle;
    private readonly INavigationService _navigationService;

    public ConsultationManagementViewModel(
        IConsultationRepository consultationRepository,
        IFeatureToggleService featureToggle,
        INavigationService navigationService)
    {
        _consultationRepository = consultationRepository;
        _featureToggle = featureToggle;
        _navigationService = navigationService;
    }

    // 功能可用性属性（基于FeatureToggle）
    public bool CanCreate => _featureToggle.IsEnabled("Consultation.Create");
    public bool CanEdit => _featureToggle.IsEnabled("Consultation.Edit");
    public bool CanDelete => _featureToggle.IsEnabled("Consultation.Delete");
    public bool CanViewDetail => _featureToggle.IsEnabled("Consultation.ViewDetail"); // MVP: true

    // 查询功能（MVP阶段启用）
    public ICommand LoadDataCommand => new RelayCommand(async () => await LoadDataAsync());
    public ICommand SearchCommand => new RelayCommand<string>(async (keyword) => await SearchAsync(keyword));
    public ICommand ViewDetailCommand => new RelayCommand<ConsultationDto>(
        (dto) => _navigationService.NavigateTo("ConsultationDetailView", dto),
        (dto) => dto != null && CanViewDetail
    );

    // 创建命令（MVP阶段禁用）
    public ICommand CreateCommand => new RelayCommand(
        () => throw new NotImplementedException("此功能在MVP版本中暂不可用，请通过【开始接诊】创建新病案。"),
        () => CanCreate
    );

    // 编辑命令（MVP阶段禁用）
    public ICommand EditCommand => new RelayCommand<ConsultationDto>(
        (dto) => throw new NotImplementedException("此功能在MVP版本中暂不可用，请通过病案管理编辑病案。"),
        (dto) => dto != null && CanEdit
    );

    // 删除命令（MVP阶段禁用）
    public ICommand DeleteCommand => new RelayCommand<ConsultationDto>(
        (dto) => throw new NotImplementedException("此功能在MVP版本中暂不可用，请通过病案管理删除病案。"),
        (dto) => dto != null && CanDelete
    );

    private async Task LoadDataAsync()
    {
        // 只读查询实现
        var result = await _consultationRepository.GetPagedAsync(CurrentPage, PageSize, Keyword);
        // ...
    }
}
```

**ConsultationManagementView.xaml UI调整**：

```xml
<!-- 添加Tooltip提示 -->
<Button Content="新增" 
        Command="{Binding CreateCommand}"
        IsEnabled="{Binding CanCreate}"
        ToolTip="MVP版本暂不可用，请通过【开始接诊】创建新病案" />

<Button Content="编辑" 
        Command="{Binding EditCommand}"
        IsEnabled="{Binding CanEdit}"
        ToolTip="MVP版本暂不可用，请通过病案管理编辑病案" />

<Button Content="删除" 
        Command="{Binding DeleteCommand}"
        IsEnabled="{Binding CanDelete}"
        ToolTip="MVP版本暂不可用，请通过病案管理删除病案" />

<Button Content="查看详情" 
        Command="{Binding ViewDetailCommand}"
        IsEnabled="{Binding CanViewDetail}" />
```

---

### 3.3 LYBT.Desktop.Prescriptions模块调整

**职责重新定位**：
- **MVP阶段**：只读查询 + 辅助功能（复用、编辑器工具）
- **Post-MVP**：处方模板管理、智能推荐、配伍检查、成本估算

**PrescriptionManagementViewModel调整**：

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionManagementViewModel.cs

/// <summary>
/// 处方管理视图模型 - v2架构调整
/// MVP阶段：只读查询 + 复用功能
/// 后期扩展：处方模板管理、智能推荐、配伍检查、成本估算
/// </summary>
public class PrescriptionManagementViewModel : ViewModelBase
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IFeatureToggleService _featureToggle;

    // 功能可用性属性
    public bool CanCreate => _featureToggle.IsEnabled("Prescription.Create");        // MVP: false
    public bool CanDelete => _featureToggle.IsEnabled("Prescription.Delete");        // MVP: false
    public bool CanClone => _featureToggle.IsEnabled("Prescription.Clone");          // MVP: true（重要功能）
    public bool CanExport => _featureToggle.IsEnabled("Prescription.Export");        // MVP: true（实用功能）
    public bool CanViewDetail => true;                                                // MVP: true

    // 查询功能（MVP启用）
    public ICommand LoadDataCommand => new RelayCommand(async () => await LoadDataAsync());
    public ICommand SearchCommand => new RelayCommand<string>(async (keyword) => await SearchAsync(keyword));
    public ICommand ViewDetailCommand => new RelayCommand<PrescriptionDto>(...);

    // 复用功能（MVP启用，重要）
    public ICommand CloneCommand => new RelayCommand<PrescriptionDto>(
        async (dto) => await CloneAsync(dto),
        (dto) => dto != null && CanClone
    );

    // 导出功能（MVP启用，实用）
    public ICommand ExportCommand => new RelayCommand<PrescriptionDto>(
        async (dto) => await ExportAsync(dto),
        (dto) => dto != null && CanExport
    );

    // 独立创建命令（MVP禁用）
    public ICommand CreateCommand => new RelayCommand(
        () => throw new NotImplementedException("此功能在MVP版本中暂不可用，请通过病案录入创建处方。"),
        () => CanCreate
    );

    // 独立删除命令（MVP禁用）
    public ICommand DeleteCommand => new RelayCommand<PrescriptionDto>(
        (dto) => throw new NotImplementedException("此功能在MVP版本中暂不可用，请通过病案管理删除病案。"),
        (dto) => dto != null && CanDelete
    );

    private async Task CloneAsync(PrescriptionDto dto)
    {
        // 重要：Clone必须有MedicalCase上下文
        var currentMedicalCase = _contextService.GetCurrentMedicalCase();
        if (currentMedicalCase == null)
        {
            _dialogService.ShowMessage("错误", "未找到当前病案上下文。请从病案录入界面调用处方复用功能。", MessageType.Error);
            return;
        }

        try
        {
            // 调用API：Clone到当前MedicalCase（注意参数调整）
            await _prescriptionRepository.ClonePrescriptionToMedicalCaseAsync(
                dto.Id,
                currentMedicalCase.Id);

            _dialogService.ShowMessage("成功", "处方复用成功", MessageType.Success);

            // 刷新MedicalCaseEntryViewModel的Prescription数据
            _eventAggregator.Publish(new PrescriptionUpdatedEvent(currentMedicalCase.Id));
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessage("错误", $"处方复用失败: {ex.Message}", MessageType.Error);
        }
    }
}
```

**工具功能保留（供MedicalCase模块调用）**：
- ✅ PrescriptionFormulationViewModel：药材配伍编辑器（从MedicalCaseEntryViewModel调用）
- ✅ PrescriptionSearchDialogViewModel：历史处方搜索对话框
- ✅ FormulaTemplateDialogViewModel：验方导入对话框
- ✅ HerbSelectionDialogViewModel：药材选择对话框

---

### 3.4 LYBT.Desktop.MedicalCase模块强化

**新增：MedicalCaseDetailViewModel（可选，Phase 2后期）**：

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs

/// <summary>
/// 病案详情视图模型 - 完整展示MedicalCase + Consultation + Prescription
/// </summary>
public class MedicalCaseDetailViewModel : ViewModelBase
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    // 完整病案数据
    public MedicalCaseDto MedicalCase { get; set; }
    public ConsultationDto Consultation { get; set; }
    public PrescriptionDto Prescription { get; set; }

    // 编辑命令（导航回MedicalCaseEntryView）
    public ICommand EditCommand => new RelayCommand(
        () => _navigationService.NavigateTo("MedicalCaseEntryView", MedicalCase.Id),
        () => MedicalCase != null
    );

    // 查看诊疗详情（使用Consultation模块的只读View）
    public ICommand ViewConsultationDetailCommand => new RelayCommand(
        () => _dialogService.ShowDialog("ConsultationDetailDialog", Consultation),
        () => Consultation != null
    );

    // 查看处方详情（使用Prescription模块的只读View）
    public ICommand ViewPrescriptionDetailCommand => new RelayCommand(
        () => _dialogService.ShowDialog("PrescriptionDetailDialog", Prescription),
        () => Prescription != null
    );

    public async Task LoadAsync(Guid medicalCaseId)
    {
        // 加载完整病案数据（聚合根完整加载）
        var result = await _medicalCaseRepository.GetWithDetailsAsync(medicalCaseId);
        if (result.IsSuccess && result.Data != null)
        {
            MedicalCase = result.Data.MedicalCase;
            Consultation = result.Data.Consultation;
            Prescription = result.Data.Prescription;
        }
    }
}
```

---

## 4. 后期扩展路线图

### 4.1 Consultation模块扩展规划

#### Phase A：诊断知识库（MVP+1季度）

**功能**：
- 症状词典管理（CRUD）
- 疾病知识库管理（CRUD）
- 四诊数据标准化录入
- 症状-疾病关联查询

**技术实现**：
- FeatureToggle启用："Consultation.SymptomDictionary", "Consultation.DiseasKnowledgeBase"
- 新增ViewModels：SymptomDictionaryViewModel, DiseaseKnowledgeBaseViewModel
- 新增Repository：ISymptomRepository, IDiseaseRepository

#### Phase B：AI辅助诊断（MVP+2季度）

**功能**：
- 基于症状的疾病推荐
- 证型分析（基于四诊数据）
- 治疗原则推荐
- 类似病案检索

**技术实现**：
- FeatureToggle启用："Consultation.AIAssist"
- 新增Service：IAIConsultationService（可选：接入第三方AI API）
- 新增ViewModels：AIConsultationAssistViewModel

#### Phase C：中医智能分析（MVP+3季度）

**功能**：
- 舌诊图像识别
- 脉诊数据分析
- 体质辨识
- 个性化诊疗建议

**技术实现**：
- FeatureToggle启用："Consultation.TongueAnalysis", "Consultation.PulseAnalysis"
- 新增Service：ITongueAnalysisService, IPulseAnalysisService（可能需要AI模型）
- 新增ViewModels：TongueAnalysisViewModel, PulseAnalysisViewModel

---

### 4.2 Prescription模块扩展规划

#### Phase A：处方模板管理（MVP+1季度）

**功能**：
- 验方模板CRUD
- 经典方剂库管理
- 个人常用方管理
- 方剂分类与检索

**技术实现**：
- FeatureToggle启用："Prescription.TemplateManagement"
- 新增ViewModels：PrescriptionTemplateManagementViewModel
- 新增Repository：IPrescriptionTemplateRepository

#### Phase B：智能推荐系统（MVP+2季度）

**功能**：
- 基于诊断的方剂推荐
- 药材替代推荐（缺货时）
- 剂量智能计算（基于患者年龄、体重）
- 配伍优化建议

**技术实现**：
- FeatureToggle启用："Prescription.IntelligentRecommendation"
- 新增Service：IPrescriptionRecommendationService
- 新增ViewModels：PrescriptionRecommendationViewModel

#### Phase C：安全与成本优化（MVP+3季度）

**功能**：
- 配伍禁忌实时检查
- 药物相互作用警告
- 成本估算与优化
- 医保目录对照

**技术实现**：
- FeatureToggle启用："Prescription.SafetyCheck", "Prescription.CostOptimization"
- 新增Service：IPrescriptionSafetyService, IPrescriptionCostService
- 新增ViewModels：PrescriptionSafetyCheckViewModel, PrescriptionCostViewModel

---

### 4.3 扩展原则与架构保障

**模块独立性**：
- Consultation和Prescription作为独立DLL，可独立版本升级
- 扩展功能通过插件化加载（可选：MEF或自定义插件框架）

**接口契约稳定**：
- 核心接口（只读查询、辅助功能）保持稳定
- 扩展功能通过新接口添加（ISymptomRepository, IAIConsultationService等）

**配置驱动**：
- 所有扩展功能通过FeatureToggle控制
- 支持灰度发布（不同客户启用不同功能）

**架构边界保持**：
- 即使后期扩展，仍严格遵守：写入操作只能通过MedicalCase聚合根
- 扩展功能（AI诊断、智能推荐）可以查询Consultation/Prescription，但不能直接修改

---

## 5. v1方案 vs v2方案对比

### 5.1 方案差异对比

| 维度 | v1方案（模块合并） | v2方案（模块保留） |
|------|-------------------|-------------------|
| **架构纯度** | 高（完全符合DDD） | 中（DDD + 实用主义） |
| **扩展性** | 低（需重构） | 高（配置驱动） |
| **代码量** | 少（合并后减少） | 多（保留独立模块） |
| **开发规范** | 简单（唯一入口） | 复杂（需功能分层） |
| **长远规划** | 不适合扩展 | 适合长期迭代 |
| **MVP实施** | 快（删除即可） | 中（需配置控制） |
| **后期成本** | 高（重新创建模块） | 低（启用配置） |
| **模块独立性** | 差（合并到MedicalCase） | 好（独立DLL） |
| **灰度发布** | 不支持 | 支持（FeatureToggle） |
| **插件化** | 不支持 | 支持（后期可加载插件） |

### 5.2 方案选择建议

**选择v1方案（模块合并）的场景**：
- ✅ 确定不会扩展Consultation/Prescription功能
- ✅ 追求架构纯粹性，完全符合DDD原则
- ✅ 团队规模小，不需要模块独立升级
- ✅ MVP快速上线，后期不迭代

**选择v2方案（模块保留）的场景**（推荐）：
- ✅ 计划长期迭代，后期会扩展功能
- ✅ 需要模块独立升级（不同模块不同版本）
- ✅ 需要灰度发布（不同客户启用不同功能）
- ✅ 团队规模大，多人协作开发
- ✅ **用户明确表示"后期会扩展这两个模块的功能"**

**本项目推荐**：基于用户明确要求，**选择v2方案**。

---

## 6. 执行计划（4-5天）

### Phase 1：Server端API层级调整（1-2天，P0）

**Day 1上午**：
- [ ] 标记ConsultationController写入API为Obsolete（error=true）
  - POST /api/consultations
  - PUT /api/consultations/{id}
  - DELETE /api/consultations/{id}
- [ ] 标记ConsultationController统计API为Obsolete（error=true）
  - GET /api/consultations/statistics（MVP过度开发）
- [ ] 编译验证，确保无编译错误

**Day 1下午**：
- [ ] 标记PrescriptionsController写入API为Obsolete（error=true）
  - POST /api/prescriptions
  - PUT /api/prescriptions/{id}
  - DELETE /api/prescriptions/{id}
- [ ] 标记PrescriptionsController统计API为Obsolete（error=true）
  - GET /api/prescriptions/statistics（MVP过度开发）
- [ ] 修改Clone API参数（targetConsultationId → targetMedicalCaseId）
- [ ] 编译验证

**Day 2上午**：
- [ ] 扩展MedicalCaseController
  - 新增PUT /api/medicalcases/{id}/consultation
  - 新增PUT /api/medicalcases/{id}/prescription
- [ ] 实现MedicalCaseService对应方法
- [ ] 单元测试

**Day 2下午**：
- [ ] Desktop端调用点修正（如有直接调用旧API）
- [ ] 集成测试
- [ ] 创建PR：【架构纠正】Server端API层级调整（v2方案）

---

### Phase 2：Desktop端功能简化（1-2天，P0）

**Day 1上午**：
- [ ] 实现简单的FeatureToggleService
  ```csharp
  public interface IFeatureToggleService
  {
      bool IsEnabled(string featureKey);
  }
  ```
- [ ] 在appsettings.json中配置MVP功能开关
- [ ] DI容器注册FeatureToggleService

**Day 1下午**：
- [ ] 调整ConsultationManagementViewModel
  - 添加CanCreate/CanEdit/CanDelete属性（基于FeatureToggle）
  - 禁用相应Command
  - 添加Tooltip提示
- [ ] 测试ConsultationManagementView

**Day 2上午**：
- [ ] 调整PrescriptionManagementViewModel
  - 添加CanCreate/CanDelete属性
  - 保留CanClone（MVP启用）
  - 禁用相应Command
- [ ] 测试PrescriptionManagementView

**Day 2下午**：
- [ ] 强化MedicalCaseModule
  - 新增MedicalCaseDetailViewModel（可选，如MVP不需要可延后）
  - 确保所有CRUD操作都通过MedicalCase入口
- [ ] 完整UI测试
- [ ] 创建PR：【架构纠正】Desktop端功能简化（v2方案）

---

### Phase 3：文档更新（0.5-1天，P1）

**Day 1上午**：
- [ ] 更新`clinical-workflow-ux-design-discussion.md`
  - 明确MedicalCase为主框架
  - 标记Consultation/Prescription为辅助模块
- [ ] 更新`consultation-view-architecture-clarification.md`
  - 记录v2方案决策

**Day 1下午**：
- [ ] 更新`docs/explanation/architecture/client/README.md`
  - 明确模块职责（主模块 vs 辅助模块）
- [ ] 更新`docs/explanation/architecture/server/README.md`
  - 明确API层级（写入 vs 查询）
- [ ] 创建PR：【架构纠正】文档更新（v2方案）

---

### Phase 4：后期扩展规划文档（0.5天，P2，可选）

**Day 1**：
- [ ] 创建`consultation-prescription-expansion-roadmap.md`
- [ ] 列出Consultation模块扩展清单（Phase A/B/C）
- [ ] 列出Prescription模块扩展清单（Phase A/B/C）
- [ ] 定义扩展接口契约
- [ ] 创建PR：【架构纠正】扩展规划文档

---

## 7. 验收标准

### 7.1 Phase 1验收标准（Server端）

- [ ] ConsultationController所有写入方法标记Obsolete（error=true）
- [ ] PrescriptionsController所有写入方法标记Obsolete（error=true）
- [ ] 查询API保持可用（GET、Search、Statistics）
- [ ] Clone API参数修改为targetMedicalCaseId
- [ ] MedicalCaseController新增两个子实体更新API
- [ ] Desktop端项目编译通过（无Obsolete错误）
- [ ] 单元测试覆盖新增API
- [ ] 集成测试验证完整流程

### 7.2 Phase 2验收标准（Desktop端）

- [ ] FeatureToggleService实现并注册到DI容器
- [ ] appsettings.json包含MVP功能开关配置
- [ ] ConsultationManagementViewModel的Create/Edit/Delete按钮在MVP模式下禁用
- [ ] PrescriptionManagementViewModel的Create/Delete按钮在MVP模式下禁用
- [ ] PrescriptionManagementViewModel的Clone/Export按钮保持启用
- [ ] 禁用按钮显示Tooltip提示
- [ ] MedicalCaseEntryView仍是创建病案的唯一入口
- [ ] 完整UI测试通过（病案录入 → 诊断录入 → 处方录入 → 保存 → 查询历史）

### 7.3 Phase 3验收标准（文档）

- [ ] v2设计文档创建完成
- [ ] clinical-workflow-ux-design-discussion.md更新完成
- [ ] consultation-view-architecture-clarification.md记录v2决策
- [ ] Client端架构文档更新（模块职责说明）
- [ ] Server端架构文档更新（API层级说明）
- [ ] 全局搜索无遗漏的错误术语

### 7.4 Phase 4验收标准（扩展规划，可选）

- [ ] 扩展路线图文档创建完成
- [ ] Consultation模块扩展清单（Phase A/B/C）明确
- [ ] Prescription模块扩展清单（Phase A/B/C）明确
- [ ] 扩展接口契约定义清晰

### 7.5 UAT验收标准（用户验收）

- [ ] 用户能够顺利完成病案录入（MVP核心流程）
- [ ] 用户能够查询历史病案（包含诊断和处方）
- [ ] 用户能够使用历史处方复用功能
- [ ] Consultation/Prescription模块的禁用功能有清晰提示
- [ ] 用户理解新的模块职责划分

---

## 8. 风险评估与缓解措施

### 风险1：配置复杂度增加（中风险）

**影响**：引入FeatureToggleService增加系统复杂度

**缓解措施**：
- 使用简单的Dictionary<string, bool>实现，避免过度设计
- 配置文件清晰注释每个功能开关的用途
- 提供默认配置模板

### 风险2：用户混淆（低风险）

**影响**：用户可能不理解为什么Consultation模块的Create按钮禁用

**缓解措施**：
- UI上添加Tooltip："MVP版本暂不可用，请从【开始接诊】创建新病案"
- 用户手册明确说明MVP功能范围
- 培训文档包含模块职责说明

### 风险3：后期扩展破坏聚合根边界（中风险）

**影响**：后期开发可能绕过MedicalCase直接调用Consultation.Create

**缓解措施**：
- 明确文档标注API层级
- Obsolete标记的API即使后期也不应移除
- Code Review时重点检查
- 定期架构审查（每季度）

### 风险4：Clone API参数变更影响现有客户端（低风险）

**影响**：如果已有客户端调用旧的Clone API，参数变更后会失败

**缓解措施**：
- 检查Desktop端是否有调用点，及时修正
- API文档明确标注变更历史
- 如需向后兼容，可保留旧API但标记Obsolete，新增新API

---

## 9. 下一步行动

### 立即行动（今天）

1. **用户确认v2方案**：
   - 是否同意模块保留 + 功能分层的设计思路？
   - FeatureToggle实现方式是否接受（简单Dictionary vs 完整框架）？
   - MedicalCaseDetailViewModel是否在MVP阶段需要？
   - 执行时机：立即执行还是先完成Q2-Q4讨论？

2. **创建GitHub Issues**：
   - Epic Issue：【架构纠正v2】MedicalCase聚合根强势修正（保留模块版）
   - Issue 1：Server端API层级调整（P0，1-2天）
   - Issue 2：Desktop端功能简化（P0，1-2天）
   - Issue 3：文档更新（P1，0.5-1天）
   - Issue 4：扩展规划文档（P2，可选，0.5天）

3. **开始Phase 1执行**：
   - 如果用户同意，立即开始标记Obsolete和扩展API
   - 创建功能分支：`feature/architecture-correction-v2-medicalcase-aggregate`

### 后续行动（本周内）

4. **完成Phase 1-2**：
   - Server端API修正（Day 1-2）
   - Desktop端功能简化（Day 3-4）

5. **文档更新**：
   - Phase 3文档更新（Day 5）

6. **UAT验收**：
   - Phase 4验收与测试（Day 6-7）

---

## 10. 附录

### 附录A：关键代码位置清单

**Server端需修改文件**：
1. `src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs` - 标记Obsolete
2. `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs` - 标记Obsolete + 修改Clone参数
3. `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` - 新增子实体更新API
4. `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 实现UpdateConsultation/UpdatePrescription
5. `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseService.cs` - 接口定义

**Desktop端需修改文件**：
1. `src/Client/Desktop/LYBT.Desktop.Infrastructure/Services/FeatureToggleService.cs` - 新增功能开关服务
2. `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs` - 功能简化
3. `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionManagementViewModel.cs` - 功能简化
4. `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseDetailViewModel.cs` - 新增（可选）
5. `src/Client/Desktop/appsettings.json` - 添加FeatureToggles配置

**文档需更新文件**：
1. `docs/explanation/architecture/client/clinical-workflow-ux-design-discussion.md` - 明确模块职责
2. `docs/explanation/architecture/client/consultation-view-architecture-clarification.md` - 记录v2决策
3. `docs/explanation/architecture/client/README.md` - 更新模块说明
4. `docs/explanation/architecture/server/README.md` - 更新API层级说明
5. `docs/explanation/architecture/shared/consultation-prescription-expansion-roadmap.md` - 新增扩展规划（可选）

---

### 附录B：FeatureToggle配置完整示例

```json
{
  "FeatureToggles": {
    // ==================== MVP阶段功能开关 ====================
    
    // Consultation模块（MVP阶段）
    "Consultation.Create": false,              // MVP: 禁用独立创建
    "Consultation.Edit": false,                // MVP: 禁用独立编辑
    "Consultation.Delete": false,              // MVP: 禁用独立删除
    "Consultation.ViewDetail": true,           // MVP: 启用查看详情
    "Consultation.Search": true,               // MVP: 启用搜索

    // Prescription模块（MVP阶段）
    "Prescription.Create": false,              // MVP: 禁用独立创建
    "Prescription.Delete": false,              // MVP: 禁用独立删除
    "Prescription.Clone": true,                // MVP: 启用处方复用（重要）
    "Prescription.Export": true,               // MVP: 启用导出（实用）
    "Prescription.ViewDetail": true,           // MVP: 启用查看详情
    "Prescription.Search": true,               // MVP: 启用搜索

    // MedicalCase模块（MVP核心功能）
    "MedicalCase.Create": true,                // MVP: 启用创建
    "MedicalCase.Edit": true,                  // MVP: 启用编辑
    "MedicalCase.Delete": true,                // MVP: 启用删除
    "MedicalCase.ViewDetail": true,            // MVP: 启用查看详情
    "MedicalCase.Search": true,                // MVP: 启用搜索

    // ==================== Post-MVP扩展功能 ====================
    
    // Consultation模块扩展功能（Post-MVP，全部禁用）
    "Consultation.SymptomDictionary": false,   // Post-MVP: 症状词典
    "Consultation.DiseaseKnowledgeBase": false,// Post-MVP: 疾病知识库
    "Consultation.AIAssist": false,            // Post-MVP: AI辅助诊断
    "Consultation.TongueAnalysis": false,      // Post-MVP: 舌诊分析
    "Consultation.PulseAnalysis": false,       // Post-MVP: 脉诊分析

    // Prescription模块扩展功能（Post-MVP，全部禁用）
    "Prescription.TemplateManagement": false,  // Post-MVP: 处方模板管理
    "Prescription.IntelligentRecommendation": false, // Post-MVP: 智能推荐
    "Prescription.SafetyCheck": false,         // Post-MVP: 配伍禁忌检查
    "Prescription.CostOptimization": false     // Post-MVP: 成本估算
  }
}
```

**重要说明**：
- ⚠️ **Statistics统计功能已直接废弃**（通过Obsolete error=true标记），不在FeatureToggle中配置
- 原因：Statistics在MVP阶段属于过度开发，不符合"够用即好"原则
- Post-MVP阶段将重新评估统计需求，如需要会设计专门的统计模块

---

## 📌 报告总结

**v2方案核心优势**：
1. ✅ **符合用户长远规划**：保留模块，后期可扩展
2. ✅ **架构原则不变**：MedicalCase仍是DDD聚合根
3. ✅ **功能分层清晰**：写入/查询/辅助三层分离
4. ✅ **灵活部署**：配置驱动，支持灰度发布
5. ✅ **扩展性强**：后期只需改配置，无需重构

**执行时间**：4-5天

**下一步**：用户确认v2方案 → 创建GitHub Issues → 开始Phase 1执行

---

**报告结束**
