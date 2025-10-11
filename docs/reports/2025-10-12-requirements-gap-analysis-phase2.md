# Desktop端详细实现清单 - Phase 2

**生成时间**: 2025-10-12
**分析范围**: 8个Desktop模块，共43个ViewModels + 9个组件类
**相关Issue**: #1149 代码实现盘点与差异分析

---

## 📊 执行概览

### 分析统计
- **扫描模块**: 8个 (Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula)
- **ViewModels总数**: 43个
- **组件类总数**: 9个 (Prescriptions: 5, Formula: 4)
- **代码总量**: 约15,000行+
- **架构代际**:
  - **Gen 1 (基础)**: 8个 (Auth, Consultation, 部分对话框)
  - **Gen 2 (列表基类)**: 6个 (Users, Patients, MedicalCase, Herbs, Formula, Prescriptions管理)
  - **Gen 3 (组件化)**: 2个 (Prescriptions, Formula - Issue #1153完成)

---

## 🏗️ 模块详细清单

### 1. Auth模块

**文件数**: 8
**复杂度**: ⭐ 低
**架构**: Gen 1 (UnifiedViewModelBase)

#### ViewModels

##### 1.1 LoginViewModel (316行)
**描述**: 登录认证视图模型
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**依赖服务**:
- ILocalAuthService (Issue #1008)
- IApiHealthCheckService (可选)
- IUsernameStorageService (可选, Issue #861)

**关键功能**:
```
✅ 用户登录验证
  - Username: string (支持记住用户名)
  - Password: string
  - RememberMe: bool

✅ API健康检查
  - ApiStatus: ApiHealthStatus
  - CheckApiHealthAsync()

✅ 角色导航 (Issue #877)
  - NavigateBasedOnRole(UserRole, UserDto, token)
  - 医生/管理员/前台分流
```

**Commands**:
- LoginCommand: ICommand (主登录)

**业务规则**:
- 支持本地认证（LocalAuthService）
- 登录后根据角色导航
- 可选记住用户名功能

**相关Issue**:
- #1008: 使用LocalAuthService
- #861: 记住用户名
- #877: 角色导航

---

### 2. Users模块

**文件数**: 23
**复杂度**: ⭐⭐⭐ 中高
**架构**: Gen 2 (UnifiedListViewModelBase<UserDto>)

#### ViewModels

##### 2.1 UserManagementViewModel (503行)
**描述**: 用户管理主视图模型
**基类**: UnifiedListViewModelBase<UserDto>
**MVP等级**: 🔴 核心

**依赖服务**:
- IUserRepository

**数据属性**:
```
✅ 筛选条件
  - SelectedRole: UserRole? (角色筛选)
  - SelectedStatus: CommonStatus? (状态筛选)
  - ShowInactiveUsers: bool

✅ 列表数据
  - Items: ObservableCollection<UserDto> (继承自基类)
  - CurrentPage, PageSize, TotalCount (继承自基类)
```

**Commands**:
- EditCommand: DelegateCommand<UserDto>
- ResetPasswordCommand: DelegateCommand<UserDto>
- ToggleUserStatusCommand: DelegateCommand<UserDto>
- ViewDetailsCommand: DelegateCommand<UserDto>
- ClearFiltersCommand: DelegateCommand
- FirstPageCommand, LastPageCommand: DelegateCommand

**核心方法**:
```csharp
protected override async Task<IEnumerable<UserDto>> GetItemsAsync(
    int page, int pageSize, string? searchText)
{
    var result = await _userRepository.GetPagedAsync(page, pageSize, searchText);
    // 应用筛选器（SelectedRole, SelectedStatus）
    return filteredResult;
}
```

**业务规则**:
- 支持分页、搜索、筛选
- 支持角色和状态多维筛选
- 用户状态切换（启用/禁用）
- 密码重置功能

**❓需确认**:
- 文件存在编码问题（中文注释显示为乱码），需统一为UTF-8 with BOM

##### 2.2 UserDetailViewModel
**描述**: 用户详情视图
**MVP等级**: 🔴 核心

**功能**:
- 查看用户详细信息
- 编辑用户资料

##### 2.3 UserCreateViewModel
**描述**: 创建用户视图
**MVP等级**: 🔴 核心

**功能**:
- 新增用户
- 角色分配
- 初始密码设置

##### 2.4 UserEditViewModel
**描述**: 编辑用户视图
**MVP等级**: 🔴 核心

**功能**:
- 修改用户信息
- 角色调整

##### 2.5 ChangePasswordDialogViewModel
**描述**: 修改密码对话框
**MVP等级**: 🔴 核心

**功能**:
- 当前用户修改密码
- 旧密码验证

##### 2.6 ResetPasswordDialogViewModel
**描述**: 重置密码对话框
**MVP等级**: 🔴 核心

**功能**:
- 管理员重置用户密码
- 生成临时密码

##### 2.7 UserProfileDialogViewModel
**描述**: 用户资料对话框
**MVP等级**: 🟡 扩展

**功能**:
- 查看个人资料
- 修改个人设置

---

### 3. Patients模块

**文件数**: 14
**复杂度**: ⭐⭐ 中
**架构**: Gen 2 (UnifiedViewModelBase, UnifiedListViewModelBase未使用)

#### ViewModels

##### 3.1 PatientDetailViewModel (367行)
**描述**: 患者详情视图模型
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**依赖服务**:
- IPatientRepository
- IPrescriptionPrintService

**导航参数**:
```
✅ 输入参数
  - PatientId: Guid (必需)
  - IsReadOnly: bool (可选)
```

**数据属性**:
```
✅ 患者信息
  - Patient: PatientDto?
  - PatientName: string (computed)
  - Gender: string (computed, 格式化)
  - Age: int (computed)
```

**Commands**:
- LoadDataCommand: ICommand
- EditCommand: ICommand
- SaveCommand: ICommand
- CancelEditCommand: ICommand
- PrintCommand: ICommand (Epic P0-03, 开发中)
- ViewMedicalHistoryCommand: ICommand

**核心方法**:
```csharp
private async Task LoadDataAsync()
{
    Patient = await _patientRepository.GetByIdAsync(PatientId);
}

private async Task SaveAsync()
{
    var updateDto = Patient.ToUpdateDto(); // Issue #1152: 扩展方法
    var updated = await _patientRepository.UpdateAsync(updateDto);
}
```

**业务规则**:
- 支持只读/编辑模式切换
- 患者信息CRUD
- 打印功能开发中（Epic P0-03）

**相关Issue**:
- #1114: Direct Repository调用
- #1152: 使用Extension方法转换DTO
- Epic P0-03: 打印功能

##### 3.2 PatientListViewModel
**描述**: 患者列表视图
**MVP等级**: 🔴 核心

**功能**:
- 患者列表展示
- 搜索、分页

**❓需确认**:
- 是否应该迁移到UnifiedListViewModelBase<PatientDto>

##### 3.3 PatientImportWizardViewModel (1079行)
**描述**: 患者批量导入向导
**MVP等级**: 🟡 扩展

**功能**:
- Excel批量导入
- 数据验证
- 错误处理

**❓需重构**:
- 文件过大（1079行），建议拆分为多个组件或步骤类

---

### 4. MedicalCase模块

**文件数**: 18
**复杂度**: ⭐⭐⭐ 中高
**架构**: Gen 2 (主导航 + 子视图)

#### ViewModels

##### 4.1 MedicalCaseManagementViewModel (391行)
**描述**: 病历管理主视图模型（导航容器）
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**依赖服务**:
- IMedicalCaseRepository

**导航属性**:
```
✅ 当前视图
  - ActiveView: string (当前激活的子视图名称)
```

**Commands**:
- ShowListCommand: DelegateCommand (显示病历列表)
- CreateNewCommand: DelegateCommand (创建新病历)
- RefreshCommand: DelegateCommand (刷新数据)
- BackToHomeCommand: DelegateCommand (返回主页)
- SearchCommand: DelegateCommand (搜索病历)
- AddCommand: DelegateCommand (添加病历，别名)
- ViewDetailsCommand: DelegateCommand<Guid> (查看详情)
- ViewConsultationCommand: DelegateCommand<Guid> (查看会诊)
- EditCommand: DelegateCommand<Guid> (编辑病历)
- CreatePrescriptionCommand: DelegateCommand<Guid> (创建处方)
- PrintCommand: DelegateCommand<Guid> (打印病历)
- DeleteCommand: DelegateCommand<Guid> (删除病历)
- FirstPageCommand, PreviousPageCommand, NextPageCommand, LastPageCommand

**导航方法**:
```csharp
private void ShowList()
{
    NavigateTo("MedicalCaseContentRegion", "MedicalCaseListView");
    ActiveView = "MedicalCaseListView";
}

private void CreateNew()
{
    NavigateTo("MedicalCaseContentRegion", "CreateMedicalCaseDialogView");
    ActiveView = "CreateMedicalCaseDialogView";
}

public void NavigateToDetail(Guid caseId, bool isReadOnly = false)
{
    var parameters = new NavigationParameters
    {
        { "MedicalCaseId", caseId },
        { "IsReadOnly", isReadOnly }
    };
    NavigateTo("MedicalCaseContentRegion", "MedicalCaseDetailView", parameters);
}
```

**业务规则**:
- 作为病历模块的主导航容器
- 管理子视图切换（列表、详情、创建）
- 提供全局刷新和返回功能

##### 4.2 MedicalCaseListViewModel
**描述**: 病历列表视图
**MVP等级**: 🔴 核心

**功能**:
- 病历列表展示
- 搜索、筛选、分页

##### 4.3 MedicalCaseDetailViewModel
**描述**: 病历详情视图
**MVP等级**: 🔴 核心

**功能**:
- 查看病历详细信息
- 编辑病历
- 关联处方和会诊记录

##### 4.4 CreateMedicalCaseDialogViewModel
**描述**: 创建病历对话框
**MVP等级**: 🔴 核心

**功能**:
- 新建病历
- 关联患者
- 初诊信息录入

---

### 5. Consultation模块

**文件数**: 9
**复杂度**: ⭐ 低
**架构**: Gen 1 (基础实现)

#### ViewModels

##### 5.1 ConsultationManagementViewModel (244行)
**描述**: 会诊管理视图模型
**基类**: UnifiedViewModelBase
**MVP等级**: 🟡 扩展

**依赖服务**:
- IConsultationRepository

**数据属性**:
```
✅ 会诊列表
  - Consultations: ObservableCollection<ConsultationDto>
  - SelectedConsultation: ConsultationDto?

✅ 筛选条件
  - SearchKeyword: string

✅ 状态
  - IsLoading: bool
```

**Commands**:
- LoadDataCommand: DelegateCommand (加载数据)
- SearchCommand: DelegateCommand (搜索)
- RefreshCommand: DelegateCommand (刷新)
- ViewDetailsCommand: DelegateCommand<ConsultationDto> (查看详情)
- ViewPrescriptionCommand: DelegateCommand<ConsultationDto> (查看处方)
- PrintCommand: DelegateCommand<ConsultationDto> (打印)
- CopyRecordCommand: DelegateCommand<ConsultationDto> (复制记录)
- StatisticsCommand: DelegateCommand (统计)
- FirstPageCommand, PreviousPageCommand, NextPageCommand, LastPageCommand

**核心方法**:
```csharp
private async Task LoadDataAsync()
{
    SetIsBusy(true, "正在加载会诊记录...");
    var result = await _consultationRepository.GetPagedAsync(page, pageSize, SearchKeyword);
    Consultations.Clear();
    foreach (var item in result.Items)
    {
        Consultations.Add(item);
    }
}
```

**业务规则**:
- 会诊记录管理
- 支持搜索和分页
- 关联处方查看
- 打印和统计功能

**❓需确认**:
- 是否需要迁移到UnifiedListViewModelBase<ConsultationDto>

---

### 6. Prescriptions模块 ⭐⭐⭐⭐⭐

**文件数**: 41
**复杂度**: ⭐⭐⭐⭐⭐ 极高（最复杂模块）
**架构**: Gen 3 (组件化架构, Issue #1153完成)

#### 组件架构 (Issue #1153)

##### 共享接口
**文件**: `src/Shared/LYBT.Shared.Components/IHerbItem.cs`

```csharp
public interface IHerbItem
{
    Guid HerbId { get; }
    string HerbName { get; }
    decimal Dosage { get; }
    string Unit { get; }
    decimal Quantity { get; }
    decimal UnitPrice { get; }
}
```

##### 共享基类
1. **HerbCalculatorBase<TItem>**: 药材计算器基类
   - CalculateTotalDosage(): 总剂量
   - CalculateTotalWeight(): 总重量（克）
   - CalculateTotalPrice(): 总价
   - ValidateDosageReasonableness(): 剂量合理性验证

2. **HerbValidatorBase<TItem>**: 药材验证器基类
   - GetDuplicateHerbs(): 重复检测
   - ValidateRequiredFields(): 必填项验证
   - ValidateHerbList(): 列表验证

##### Prescriptions专用组件 (5个)

**1. PrescriptionCalculator**
**文件**: `ViewModels/Components/PrescriptionCalculator.cs`

**职责**: 处方价格和用量计算

```csharp
public class CalculationResult
{
    decimal SingleDosagePrice { get; }  // 单剂价格
    decimal TotalPrice { get; }         // 总价
    decimal DiscountedPrice { get; }    // 折后价
    decimal ActualTotal { get; }        // 实付
    int ItemCount { get; }              // 药材数量
}

public class PrescriptionDosageAnalysis
{
    decimal MinDosage { get; }
    decimal MaxDosage { get; }
    decimal AverageDosage { get; }
    decimal StandardDeviation { get; }
}
```

**2. PrescriptionValidator**
**文件**: `ViewModels/Components/PrescriptionValidator.cs`

**职责**: 处方验证逻辑

```csharp
public class HerbContraindication
{
    string HerbName { get; }
    string[] ConflictingHerbs { get; }
    string Reason { get; }
}

public class PrescriptionValidator
{
    ValidationResult ValidatePrescription();
    List<HerbContraindication> CheckContraindications();
}
```

**3. PrescriptionDataManager**
**文件**: `ViewModels/Components/PrescriptionDataManager.cs`

**职责**: 数据管理和持久化

**4. PrescriptionCommandHandler**
**文件**: `ViewModels/Components/PrescriptionCommandHandler.cs`

**职责**: 命令执行协调

```csharp
public class CommandResult
{
    bool Success { get; }
    string Message { get; }
    object? Data { get; }
}
```

**5. PrescriptionEventCoordinator**
**文件**: `ViewModels/Components/PrescriptionEventCoordinator.cs`

**职责**: 事件协调和发布

#### ViewModels

##### 6.1 PrescriptionComposerViewModel (669行) ⭐⭐⭐⭐⭐
**描述**: 处方编辑器（最核心、最复杂）
**基类**: UnifiedViewModelBase
**架构**: Gen 3 组件化
**MVP等级**: 🔴 核心

**依赖服务**:
- IPrescriptionRepository
- IMedicalCaseRepository

**组件依赖**:
```
✅ 5大组件
  - _calculator: PrescriptionCalculator (计算)
  - _validator: PrescriptionValidator (验证)
  - _dataManager: PrescriptionDataManager (数据)
  - _commandHandler: PrescriptionCommandHandler (命令)
  - _eventCoordinator: PrescriptionEventCoordinator (事件)
```

**导航参数**:
```
✅ 输入参数
  - MedicalCaseId: Guid (必需，关联病历)
```

**数据属性**:
```
✅ 患者和医生信息
  - PatientInfo: string (患者基本信息)
  - DoctorInfo: string (医生信息)
  - CurrentMedicalCase: MedicalCaseDto? (当前病历)

✅ 处方基本信息
  - PrescriptionNo: string (处方编号)
  - DosageCount: int (剂数)
  - Usage: string (用法)
  - MedicalAdvice: string (医嘱)
  - Remark: string (备注)
  - Discount: decimal (折扣 0-1)

✅ 药材列表
  - PrescriptionItems: ObservableCollection<PrescriptionItemViewModel>
  - SelectedItem: PrescriptionItemViewModel?

✅ 计算结果 (来自PrescriptionCalculator)
  - CalculationResult: CalculationResult?
  - SingleDosagePrice: decimal (单剂价格)
  - TotalPrice: decimal (总价)
  - DiscountedPrice: decimal (折后价)
  - TotalSaved: decimal (节省金额)
  - ActualTotal: decimal (实付)
  - DiscountAmount: decimal (折扣金额)
  - ItemCount: int (药材数量)
```

**Commands** (25个):
```
✅ 核心操作
  - SaveCommand: 保存处方
  - SaveDraftCommand: 保存草稿
  - SavePrescriptionCommand: 保存处方（别名）
  - ClearCommand: 清空处方
  - ClearAllCommand: 清空所有
  - CloseCommand: 关闭
  - BackCommand: 返回

✅ 药材管理
  - AddHerbCommand: 添加药材
  - RemoveHerbCommand: 移除药材
  - EditHerbCommand: 编辑药材

✅ 配方操作
  - ImportFormulaCommand: 导入验方
  - GeneratePrescriptionNoCommand: 生成处方号

✅ 计算和验证
  - RecalculateCommand: 重新计算
  - ValidateCommand: 验证处方

✅ 打印预览
  - PrintPreviewCommand: 打印预览
```

**核心方法**:
```csharp
protected override async Task OnNavigatedToAsync(NavigationContext context)
{
    // 1. 从导航参数获取MedicalCaseId
    if (context.Parameters.ContainsKey("MedicalCaseId"))
    {
        MedicalCaseId = context.Parameters.GetValue<Guid>("MedicalCaseId");
    }

    // 2. 初始化
    await InitializeAsync();
}

private async Task InitializeAsync()
{
    // 1. 加载病历信息
    await LoadMedicalCaseAsync();

    // 2. 生成处方编号
    GeneratePrescriptionNo();

    // 3. 订阅事件
    SubscribeToEvents();
}

private async Task LoadMedicalCaseAsync()
{
    CurrentMedicalCase = await _medicalCaseRepository.GetByIdAsync(MedicalCaseId);
    UpdatePatientInfo();
    UpdateDoctorInfo();
}

private void RecalculatePrice()
{
    // 使用组件进行计算
    CalculationResult = _calculator.Calculate(
        PrescriptionItems,
        DosageCount,
        Discount);
}

private async Task ExecuteSaveDraft()
{
    // 使用组件进行验证
    var validationResult = _validator.ValidatePrescription(this);
    if (!validationResult.IsValid)
    {
        await ShowErrorMessageAsync(validationResult.GetErrorSummary());
        return;
    }

    // 使用组件进行保存
    await _dataManager.SaveDraftAsync(this);
}
```

**业务规则**:
- 必须关联病历（MedicalCaseId）
- 支持草稿保存
- 自动计算价格和折扣
- 验证药材合理性
- 支持导入验方模板
- 组件化架构，职责分离

**相关Issue**:
- #1153: 组件化架构重构

##### 6.2 PrescriptionManagementViewModel (555行)
**描述**: 处方管理列表视图
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**依赖服务**:
- IPrescriptionRepository

**数据属性**:
```
✅ 列表和筛选
  - Prescriptions: ObservableCollection<PrescriptionDto>
  - SelectedPrescription: PrescriptionDto?
  - SearchText: string
  - StartDate, EndDate: DateTime?

✅ 分页
  - CurrentPage, PageSize, TotalCount: int
```

**Commands**:
- AddPrescriptionCommand: 新建处方
- EditPrescriptionCommand: 编辑处方
- DeletePrescriptionCommand: 删除处方
- ViewPrescriptionCommand: 查看处方
- ViewPatientHistoryCommand: 查看患者历史
- CopyPrescriptionCommand: 复制处方
- ClearFiltersCommand: 清除筛选
- ExportPrescriptionsCommand: 导出处方
- SearchCommand, RefreshCommand, LoadDataCommand
- PreviousPageCommand, NextPageCommand

**核心方法**:
```csharp
private async Task LoadDataAsync()
{
    var result = await _prescriptionRepository.GetPagedAsync(
        CurrentPage, PageSize, SearchText);

    // 应用日期筛选
    var filtered = result.Items;
    if (StartDate.HasValue)
    {
        filtered = filtered.Where(p => p.CreatedAt >= StartDate.Value);
    }
    if (EndDate.HasValue)
    {
        filtered = filtered.Where(p => p.CreatedAt <= EndDate.Value);
    }

    Prescriptions.Clear();
    foreach (var item in filtered)
    {
        Prescriptions.Add(item);
    }
}
```

**业务规则**:
- 处方列表展示
- 支持搜索、日期筛选、分页
- 支持复制处方
- 支持导出功能
- 查看患者历史处方

##### 6.3 PrescriptionsMainViewModel (362行)
**描述**: 处方模块主导航容器
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**依赖服务**:
- IPrescriptionRepository

**导航属性**:
```
✅ 当前视图
  - ActiveView: string (默认 "PrescriptionManagementView")
```

**统计属性**:
```
✅ 统计信息
  - TotalPrescriptionsCount: int (总处方数)
  - TodayPrescriptionsCount: int (今日处方数)
  - TodayTotalAmount: decimal (今日总金额)
```

**Commands**:
- ShowManagementCommand: 显示处方管理
- CreateNewCommand: 创建新处方
- ShowReportsCommand: 显示统计报表
- RefreshCommand: 刷新数据
- BackToHomeCommand: 返回主页
- CreateNewPrescriptionCommand, ReturnToSourceCommand, SwitchToManagementCommand (别名)

**导航方法**:
```csharp
public void NavigateToDetail(Guid prescriptionId, bool isReadOnly = false)
{
    var parameters = new NavigationParameters
    {
        { "PrescriptionId", prescriptionId },
        { "IsReadOnly", isReadOnly }
    };
    NavigateTo("PrescriptionContentRegion", "PrescriptionDetailView", parameters);
}

public void NavigateToEdit(Guid prescriptionId)
{
    NavigateTo("PrescriptionContentRegion", "PrescriptionComposerView",
        new NavigationParameters { { "PrescriptionId", prescriptionId } });
}
```

**业务规则**:
- 处方模块主入口和导航容器
- 显示统计信息
- 管理子视图切换

##### 6.4 HerbSelectionDialogViewModel (466行)
**描述**: 药材选择对话框
**基类**: UnifiedViewModelBase, IDialogAware
**MVP等级**: 🔴 核心

**依赖服务**:
- IHerbRepository

**数据属性**:
```
✅ 药材列表
  - AvailableHerbs: ObservableCollection<HerbDto>
  - SelectedHerbs: ObservableCollection<HerbDto>

✅ 筛选条件
  - SearchText: string
  - CategoryFilter: string

✅ 选择模式
  - AllowMultipleSelection: bool
```

**Commands**:
- SearchCommand: 搜索药材
- AddHerbCommand: 添加药材
- RemoveHerbCommand: 移除药材
- ClearSelectionCommand: 清空选择
- ConfirmCommand: 确认选择
- CancelCommand: 取消
- RefreshCommand: 刷新列表

**核心方法**:
```csharp
public void OnDialogOpened(IDialogParameters parameters)
{
    // 获取参数
    if (parameters.ContainsKey("AllowMultiple"))
    {
        AllowMultipleSelection = parameters.GetValue<bool>("AllowMultiple");
    }

    // 加载数据
    Task.Run(async () => await LoadDataAsync());
}

private void Confirm()
{
    var parameters = new DialogParameters
    {
        { "SelectedHerbs", SelectedHerbs.ToList() }
    };
    RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
}
```

**业务规则**:
- 支持单选/多选模式
- 支持搜索和分类筛选
- 通过IDialogAware接口集成

##### 6.5 FormulaTemplateDialogViewModel
**描述**: 验方模板选择对话框
**MVP等级**: 🟡 扩展

**依赖服务**:
- IFormulaRepository

**功能**:
- 从验方库选择模板
- 预览验方详情
- 导入到处方

##### 6.6 SelectFormulaDialogViewModel
**描述**: 验方选择对话框（扩展版）
**MVP等级**: 🟡 扩展

**依赖服务**:
- IFormulaRepository

**功能**:
- 验方搜索和筛选（分类、功效）
- 验方详情展示
- 多维筛选支持

##### 6.7 PrescriptionViewModel (141行)
**描述**: 处方视图模型（骨架实现）
**基类**: UnifiedViewModelBase
**MVP等级**: 🔴 核心

**状态**: Phase 4B 骨架实现

**Commands**:
- AddHerbCommand, RemoveHerbCommand
- ClearCommand, SaveCommand
- ImportFormulaCommand, ImportHistoryCommand
- PrintPreviewCommand
- SetDiscountCommand, SetDosageCommand

**❓需确认**:
- Phase 4C待补充业务服务依赖（IPrescriptionService）

##### 6.8 PrescriptionItemViewModel (178行)
**描述**: 处方药材项视图模型
**基类**: UnifiedViewModelBase
**接口**: IHerbItem (Issue #1153)
**MVP等级**: 🔴 核心

**数据属性**:
```csharp
[Required] Guid HerbId { get; set; }
[Required, StringLength(100)] string HerbName { get; set; }
[Required, Range(0.1, 999.9)] decimal Dosage { get; set; }
[Required] string Unit { get; set; } = "g";
[Range(0.1, 999.9)] decimal Quantity { get; set; } = 1;
[Range(0, 99999.99)] decimal UnitPrice { get; set; }
[StringLength(500)] string? Notes { get; set; }
```

**核心方法**:
```csharp
public PrescriptionItemDto ToDto()
{
    return new PrescriptionItemDto
    {
        HerbId = HerbId,
        HerbName = HerbName,
        Dosage = Dosage,
        Unit = Unit,
        Notes = Notes
    };
}

public void LoadFromDto(PrescriptionItemDto dto)
{
    HerbId = dto.HerbId;
    HerbName = dto.HerbName ?? string.Empty;
    Dosage = dto.Dosage;
    Unit = dto.Unit ?? "g";
    Notes = dto.Notes;
}

public void LoadFromHerb(HerbDto herb, decimal dosage = 10m)
{
    HerbId = herb.Id;
    HerbName = herb.Name ?? string.Empty;
    Dosage = dosage;
    Unit = "g";
}
```

**业务规则**:
- 实现IHerbItem接口以支持共享组件
- 数据验证特性完备
- 支持DTO转换

**相关Issue**:
- #1153: 实现IHerbItem接口

##### 6.9 PrescriptionEditorDialogViewModel
**描述**: 处方编辑对话框
**MVP等级**: 🔴 核心

**功能**:
- 快速编辑处方
- 对话框模式

---

### 7. Herbs模块

**文件数**: 12
**复杂度**: ⭐⭐⭐ 中高
**架构**: Gen 2 (UnifiedListViewModelBase<HerbDto>)

#### ViewModels

##### 7.1 HerbManagementViewModel (490行)
**描述**: 药材管理列表视图
**基类**: UnifiedListViewModelBase<HerbDto>
**MVP等级**: 🔴 核心

**依赖服务**:
- IHerbRepository

**Commands** (30+):
- AddCommand, EditCommand, DeleteCommand
- SearchCommand, RefreshCommand
- NextPageCommand, PreviousPageCommand, FirstPageCommand, LastPageCommand
- EditHerbCommand, CopyHerbCommand: DelegateCommand<HerbDto>
- ViewDetailCommand: DelegateCommand<HerbDto>
- ToggleStatusCommand: DelegateCommand<HerbDto> (启用/禁用)
- SearchByCategoryCommand: DelegateCommand<string> (分类搜索)
- ImportHerbsCommand, ExportHerbsCommand, ExportTemplateCommand

**核心方法**:
```csharp
protected override async Task<IEnumerable<HerbDto>> GetItemsAsync(
    int page, int pageSize, string? searchText)
{
    var result = await _herbRepository.GetPagedAsync(page, pageSize, searchText);
    return result.Items;
}

private async Task ToggleStatusAsync(HerbDto herb)
{
    herb.Status = herb.Status == CommonStatus.Active
        ? CommonStatus.Inactive
        : CommonStatus.Active;
    await _herbRepository.UpdateAsync(herb.ToUpdateDto());
}
```

**业务规则**:
- 药材CRUD管理
- 支持分页、搜索、分类筛选
- 支持状态切换（启用/禁用）
- 支持导入导出
- 复制药材功能

##### 7.2 HerbDetailViewModel
**描述**: 药材详情视图
**MVP等级**: 🔴 核心

**功能**:
- 查看药材详细信息
- 编辑药材属性
- 价格管理

---

### 8. Formula模块 ⭐⭐⭐⭐

**文件数**: 18
**复杂度**: ⭐⭐⭐⭐ 高
**架构**: Gen 3 (组件化架构, Issue #1153完成)

#### 组件架构 (Issue #1153)

##### Formula专用组件 (4个)

**1. FormulaCalculator**
**文件**: `ViewModels/Components/FormulaCalculator.cs`

**职责**: 验方计算逻辑
**继承**: HerbCalculatorBase<FormulaHerbItemViewModel>

**2. FormulaValidator**
**文件**: `ViewModels/Components/FormulaValidator.cs`

**职责**: 验方验证逻辑
**继承**: HerbValidatorBase<FormulaHerbItemViewModel>

**3. FormulaDataManager**
**文件**: `ViewModels/Components/FormulaDataManager.cs`

**职责**: 验方数据管理

**4. FormulaCommandHandler**
**文件**: `ViewModels/Components/FormulaCommandHandler.cs`

**职责**: 验方命令处理

#### ViewModels

##### 8.1 FormulaManagementViewModel (461行)
**描述**: 验方管理列表视图
**基类**: UnifiedListViewModelBase<FormulaDto>
**MVP等级**: 🔴 核心

**依赖服务**:
- IFormulaRepository

**Commands** (30+):
- AddCommand, EditCommand, DeleteCommand
- SearchCommand, RefreshCommand
- NextPageCommand, PreviousPageCommand, FirstPageCommand, LastPageCommand
- AddFormulaCommand: DelegateCommand (添加验方)
- ViewDetailsCommand, ViewDetailCommand: DelegateCommand<FormulaDto> (查看详情)
- EditCommand, CopyCommand: DelegateCommand<FormulaDto>
- SearchByCategoryCommand: DelegateCommand<string> (分类搜索)
- ImportFormulasCommand, ExportFormulasCommand, ExportTemplateCommand
- ClearFiltersCommand

**核心方法**:
```csharp
protected override async Task<IEnumerable<FormulaDto>> GetItemsAsync(
    int page, int pageSize, string? searchText)
{
    var result = await _formulaRepository.GetPagedAsync(page, pageSize, searchText);
    return result.Items;
}

private async Task OnExecuteDeleteAsync(FormulaDto formula)
{
    var confirmed = await ShowConfirmDialogAsync(
        "确认删除",
        $"确定要删除验方 \"{formula.Name}\" 吗？");

    if (confirmed)
    {
        await _formulaRepository.DeleteAsync(formula.Id);
        await RefreshAsync();
    }
}
```

**业务规则**:
- 验方CRUD管理
- 支持分页、搜索、分类筛选
- 支持导入导出
- 复制验方功能
- 批量删除功能

##### 8.2 FormulaDetailViewModel
**描述**: 验方详情视图
**MVP等级**: 🔴 核心

**功能**:
- 查看验方详细信息
- 编辑验方和药材组成
- 使用组件进行计算和验证

##### 8.3 EditFormulaDialogViewModel
**描述**: 编辑验方对话框
**MVP等级**: 🔴 核心

**功能**:
- 对话框模式编辑验方
- 集成组件进行验证

##### 8.4 ViewFormulaDialogViewModel
**描述**: 查看验方对话框
**MVP等级**: 🟡 扩展

**功能**:
- 只读模式查看验方
- 预览验方详情

##### 8.5 FormulaHerbItemViewModel
**描述**: 验方药材项视图模型
**接口**: IHerbItem (Issue #1153)
**MVP等级**: 🔴 核心

**功能**:
- 实现IHerbItem接口
- 支持共享组件计算和验证

**相关Issue**:
- #1153: 实现IHerbItem接口

---

## 📈 架构分析

### 架构代际演进

#### Gen 1: 基础架构 (8个ViewModels)
**基类**: UnifiedViewModelBase
**特点**:
- 直接继承UnifiedViewModelBase
- 手动实现所有功能
- 无列表管理基类支持

**模块**:
- Auth: LoginViewModel
- Consultation: ConsultationManagementViewModel
- Patients: PatientDetailViewModel
- 部分对话框ViewModels

**优点**:
- 灵活性高
- 适合简单场景

**缺点**:
- 代码重复
- 分页逻辑需手动实现

#### Gen 2: 列表基类架构 (6个主ViewModels)
**基类**: UnifiedListViewModelBase<T>
**特点**:
- 继承列表管理基类
- 自动分页、搜索、筛选
- 只需实现GetItemsAsync()

**模块**:
- Users: UserManagementViewModel
- MedicalCase: MedicalCaseManagementViewModel
- Herbs: HerbManagementViewModel
- Formula: FormulaManagementViewModel
- Prescriptions: PrescriptionManagementViewModel

**优点**:
- 大幅减少重复代码
- 统一列表管理模式
- 易于维护

**缺点**:
- 灵活性稍弱
- 需遵循基类约定

**示例**:
```csharp
public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
{
    protected override async Task<IEnumerable<UserDto>> GetItemsAsync(
        int page, int pageSize, string? searchText)
    {
        var result = await _userRepository.GetPagedAsync(page, pageSize, searchText);
        // 应用额外筛选
        return FilteredResult;
    }
}
```

#### Gen 3: 组件化架构 (2个模块, Issue #1153) ⭐⭐⭐⭐⭐
**基类**: UnifiedViewModelBase + 组件类
**特点**:
- 职责分离（计算、验证、数据、命令、事件）
- 共享基类和接口（IHerbItem）
- 可复用组件

**模块**:
- Prescriptions (5个组件)
- Formula (4个组件)

**组件架构**:
```
Shared Components (跨模块)
├── IHerbItem (接口)
├── HerbCalculatorBase<T> (计算基类)
└── HerbValidatorBase<T> (验证基类)

Prescriptions Components
├── PrescriptionCalculator (继承HerbCalculatorBase)
├── PrescriptionValidator (继承HerbValidatorBase)
├── PrescriptionDataManager
├── PrescriptionCommandHandler
└── PrescriptionEventCoordinator

Formula Components
├── FormulaCalculator (继承HerbCalculatorBase)
├── FormulaValidator (继承HerbValidatorBase)
├── FormulaDataManager
└── FormulaCommandHandler
```

**优点**:
- 职责高度分离
- 组件高度可复用
- 易于测试
- 易于扩展

**缺点**:
- 初始设计复杂度高
- 需要更多类文件

**相关Issue**:
- #1153: Desktop端組件化架构标准化

### 复杂度排名

| 排名 | 模块 | 文件数 | 复杂度 | 架构代际 | 说明 |
|-----|------|-------|--------|---------|------|
| 1 | Prescriptions | 41 | ⭐⭐⭐⭐⭐ | Gen 3 | 最复杂，组件化架构 |
| 2 | Formula | 18 | ⭐⭐⭐⭐ | Gen 3 | 高复杂度，组件化架构 |
| 3 | Users | 23 | ⭐⭐⭐ | Gen 2 | 中高复杂度，7个ViewModels |
| 4 | MedicalCase | 18 | ⭐⭐⭐ | Gen 2 | 中高复杂度，主导航+子视图 |
| 5 | Patients | 14 | ⭐⭐ | Gen 2 | 中等，含1079行导入向导 |
| 6 | Herbs | 12 | ⭐⭐⭐ | Gen 2 | 中高复杂度，完善的CRUD |
| 7 | Consultation | 9 | ⭐ | Gen 1 | 低复杂度，基础实现 |
| 8 | Auth | 8 | ⭐ | Gen 1 | 低复杂度，单一登录 |

---

## 🔍 关键发现

### ✅ 优点

1. **组件化架构成功** (Issue #1153)
   - Prescriptions和Formula模块已完成组件化
   - 共享基类（HerbCalculatorBase, HerbValidatorBase）
   - 共享接口（IHerbItem）
   - 职责分离清晰

2. **架构演进清晰**
   - Gen 1 → Gen 2 → Gen 3 逐步优化
   - UnifiedListViewModelBase大幅减少重复代码
   - 组件化架构提升可维护性

3. **依赖注入规范**
   - 所有ViewModels遵循构造函数注入
   - 服务依赖声明清晰
   - 可选依赖统一处理

4. **命名规范统一**
   - ViewModels命名一致
   - Commands命名清晰（ExecuteXxx, CanExecuteXxx）
   - Repository接口统一

### ⚠️ 问题与建议

#### 1. 架构不一致
**问题**:
- Consultation模块仍使用Gen 1架构（基础ObservableCollection）
- Patients模块部分ViewModels未使用UnifiedListViewModelBase

**建议**:
```
🔧 迁移优先级
  1. ConsultationManagementViewModel → UnifiedListViewModelBase<ConsultationDto>
  2. PatientListViewModel → UnifiedListViewModelBase<PatientDto>
```

#### 2. 文件过大
**问题**:
- PatientImportWizardViewModel: 1079行

**建议**:
```
🔧 重构建议
  拆分为多步骤：
  1. ImportStep1ViewModel: 文件选择和上传
  2. ImportStep2ViewModel: 数据验证和预览
  3. ImportStep3ViewModel: 导入执行和结果
  4. PatientImportService: 业务逻辑提取
```

#### 3. 编码问题
**问题**:
- UserManagementViewModel.cs 存在编码问题（中文注释乱码）

**建议**:
```
🔧 批量修复
  使用脚本统一所有文件编码为 UTF-8 with BOM
  参考: docs/development/standards.md
```

#### 4. 骨架实现待完成
**问题**:
- PrescriptionViewModel: Phase 4B骨架实现，待补充业务服务

**建议**:
```
🔧 Phase 4C任务
  补充依赖: IPrescriptionService
  实现所有Commands的业务逻辑
```

#### 5. 组件化扩展机会
**问题**:
- 仅Prescriptions和Formula模块使用组件化架构
- 其他模块可能受益于组件化

**建议**:
```
🔧 扩展组件化
  候选模块:
  1. MedicalCase: 复杂的病历管理逻辑
  2. Herbs: 库存和价格计算逻辑
```

---

## 📝 MVP等级分布

### 🔴 核心功能 (MVP Required)
- **Auth**: LoginViewModel
- **Users**: 全部7个ViewModels
- **Patients**: PatientDetailViewModel, PatientListViewModel
- **MedicalCase**: 全部4个ViewModels
- **Prescriptions**: 9个ViewModels中的7个核心
- **Herbs**: HerbManagementViewModel, HerbDetailViewModel
- **Formula**: FormulaManagementViewModel, FormulaDetailViewModel

### 🟡 扩展功能 (MVP Extended)
- **Patients**: PatientImportWizardViewModel (批量导入)
- **Users**: UserProfileDialogViewModel (个人资料)
- **Consultation**: ConsultationManagementViewModel (会诊管理)
- **Prescriptions**: FormulaTemplateDialogViewModel, SelectFormulaDialogViewModel
- **Formula**: ViewFormulaDialogViewModel

### 🟢 高级功能 (MVP Advanced)
- 统计报表
- 高级筛选
- 批量操作
- 导入导出

---

## 🎯 下一步行动 (Phase 3-5)

### Phase 3: Server端结构扫描 (1小时)
**目标**: 获取Server端模块结构和API概览

**任务**:
1. 扫描Server端8个模块目录
2. 获取Controller和Service清单
3. 统计API端点数量
4. 识别已实现的功能

**产出**:
- Server端模块结构报告
- API端点清单

### Phase 4: Server端详细分析 (3-4小时)
**目标**: 详细分析Server端实现

**任务**:
1. 读取各模块Controller代码
2. 分析Service层业务逻辑
3. 提取API方法签名和功能
4. 标记MVP等级

**产出**:
- Server端完整实现清单
- API详细功能描述

### Phase 5: Desktop-Server差异分析 (2-3小时)
**目标**: 生成差异报告和补充建议

**任务**:
1. 对比Desktop需求与Server实现
2. 识别未实现的功能
3. 识别不匹配的接口
4. 生成补充计划

**产出**:
- Desktop-Server差异分析报告
- 功能补充优先级列表
- API调整建议

---

## 附录

### A. ViewModel命名规范

| 模式 | 示例 | 用途 |
|------|------|------|
| \*ManagementViewModel | UserManagementViewModel | 管理类主视图 |
| \*DetailViewModel | PatientDetailViewModel | 详情视图 |
| \*ListViewModel | MedicalCaseListViewModel | 列表视图 |
| \*DialogViewModel | HerbSelectionDialogViewModel | 对话框视图 |
| \*ItemViewModel | PrescriptionItemViewModel | 列表项视图模型 |

### B. Commands命名规范

| 模式 | 示例 | 说明 |
|------|------|------|
| \*Command | LoadDataCommand | 标准命令 |
| Execute\* | ExecuteLoadData() | 命令执行方法 |
| Can\* | CanLoadData() | 命令可执行检查 |

### C. 组件类命名规范

| 模式 | 示例 | 职责 |
|------|------|------|
| \*Calculator | PrescriptionCalculator | 计算逻辑 |
| \*Validator | PrescriptionValidator | 验证逻辑 |
| \*DataManager | PrescriptionDataManager | 数据管理 |
| \*CommandHandler | PrescriptionCommandHandler | 命令处理 |
| \*EventCoordinator | PrescriptionEventCoordinator | 事件协调 |

---

**报告生成**: Phase 2完成
**下一阶段**: Phase 3 - Server端结构扫描
**预计时间**: 1小时
