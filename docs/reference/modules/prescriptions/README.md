# Prescriptions 模块文档

> **版本**：v1.0.0
> **更新日期**：2025-01-29
> **维护团队**：LYBTZYZS 开发组
> **关联Epic**：[Epic #1343 - MVP基线建立](https://github.com/shouqitao/LYBTZYZS/issues/1343)

---

## 📦 模块定位

### Server端定位
- **命名空间**：`LYBT.Module.Prescriptions`
- **职责**：处方数据管理、处方编号生成、状态流转、验方集成、价格计算
- **核心实体**：`Prescription`（处方）、`PrescriptionItem`（处方药材条目）
- **主要服务**：`PrescriptionService`、`PrescriptionNumberService`
- **Repository**：`PrescriptionRepository`（7个查询方法）

### Client端定位
- **命名空间**：`LYBT.Desktop.Prescriptions`
- **职责**：处方录入、药材选择、验方模板加载、价格计算、打印预览
- **核心ViewModel**：`PrescriptionEditorDialogViewModel`（668行）、`PrescriptionManagementViewModel`（597行）
- **架构模式**：Dialog-based架构 + ISaveable接口契约
- **外部集成**：MedicalCase模块（通过ISaveable接口）、Herbs模块（药材选择）、Formula模块（验方模板）

---

## 🎯 功能概述

### 核心功能
1. ✅ **处方编号生成**：CF-YYYYMMDD-0001格式（17字符，按日期自动递增序号）
2. ✅ **处方状态管理**：Draft（草稿）→ Confirmed（已确认）→ Dispensed（已配药）状态机
3. ✅ **药材选择集成**：从Herbs模块选择药材，支持拼音快速搜索、重复检测
4. ✅ **验方模板集成**：从Formula模块加载验方，自动应用药材条目、用法医嘱
5. ✅ **价格自动计算**：TotalAmount = Σ(UnitPrice × Dosage) × DosageCount × Discount
6. ✅ **ISaveable接口契约**：与MedicalCase模块集成，支持三步工作流中的Step2处方开具
7. ✅ **处方打印服务**：生成FlowDocument文档，WPF打印对话框打印/预览

### 支撑功能
- 🔍 多维度查询（按处方编号、患者姓名、日期范围）
- 📊 分页列表管理（每页20条，支持搜索过滤）
- 📝 处方编辑对话框（Dialog-based，支持添加/删除/编辑药材）
- 📄 打印预览与打印（A4纸格式，药材表格、总价、用法医嘱）
- 📤 导出Excel功能（处方列表导出到桌面）

---

## 🏗️ 模块架构

### Server端架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    LYBT.Module.Prescriptions                     │
│                      (Server端处方模块)                          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 核心服务 (Core Services)                                         │
├─────────────────────────────────────────────────────────────────┤
│ PrescriptionService (5个方法)                                    │
│   ├── GetByIdAsync()                    // 按ID查询处方          │
│   ├── GetByMedicalCaseIdAsync()         // 按医案ID查询处方      │
│   ├── SearchPrescriptionsAsync()        // 多条件搜索            │
│   ├── GetPatientRecentPrescriptionsAsync() // 患者历史处方      │
│   └── CalculateTotalAmount()            // 计算总价             │
│                                                                  │
│ PrescriptionNumberService (3个方法)                              │
│   ├── GenerateNumberAsync()             // 生成编号(CF-日期-序号) │
│   ├── ValidateNumberFormat()            // 验证编号格式         │
│   └── GetMaxSequenceForDateAsync()      // 获取日期最大序号     │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ Repository层 (Data Access)                                       │
├─────────────────────────────────────────────────────────────────┤
│ PrescriptionRepository (7个方法)                                 │
│   ├── GetByIdWithItemsAsync()           // Include药材条目      │
│   ├── GetPagedWithDetailsAsync()        // 分页查询(含患者医生) │
│   ├── GetByPatientIdAsync()             // 按患者ID查询         │
│   ├── GetByMedicalCaseIdAsync()         // 按医案ID查询         │
│   ├── GetPrescriptionNumbersByPrefixAsync() // 按前缀查询编号   │
│   ├── GetAllAsync()                     // 查询全部            │
│   └── FindAsync()                       // 表达式查询          │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 核心实体 (Domain Entities)                                       │
├─────────────────────────────────────────────────────────────────┤
│ Prescription (处方主表)                                          │
│   ├── Id (Guid)                         // 主键                │
│   ├── PrescriptionNo (string)           // 处方编号(CF-日期-序号)│
│   ├── MedicalCaseId (Guid)              // 医案ID(外键)        │
│   ├── DosageCount (int)                 // 剂数                │
│   ├── Usage (string)                    // 用法                │
│   ├── MedicalAdvice (string)            // 医嘱                │
│   ├── Discount (decimal)                // 折扣(0.0-1.0)       │
│   ├── TotalAmount (decimal)             // 总金额              │
│   ├── Status (PrescriptionStatus)       // 状态(枚举)          │
│   ├── ConfirmedAt (DateTime?)           // 确认时间            │
│   ├── DispensedAt (DateTime?)           // 配药时间            │
│   └── Items (List<PrescriptionItem>)    // 药材条目集合        │
│                                                                  │
│ PrescriptionItem (处方药材条目)                                  │
│   ├── Id (Guid)                         // 主键                │
│   ├── PrescriptionId (Guid)             // 处方ID(外键)        │
│   ├── HerbId (Guid)                     // 药材ID(外键)        │
│   ├── Dosage (decimal)                  // 剂量                │
│   ├── Unit (string)                     // 单位(克/片/粒)      │
│   ├── UnitPrice (decimal)               // 单价                │
│   └── Notes (string)                    // 备注                │
│                                                                  │
│ PrescriptionStatus (处方状态枚举)                                │
│   ├── Draft = 1                         // 草稿(可编辑)        │
│   ├── Confirmed = 2                     // 已确认(不可编辑)    │
│   └── Dispensed = 3                     // 已配药(完成)        │
└─────────────────────────────────────────────────────────────────┘
```

### Client端架构

```
┌─────────────────────────────────────────────────────────────────┐
│                  LYBT.Desktop.Prescriptions                      │
│              (Client端处方模块 - Dialog-based架构)                │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 外部集成 (External Integration)                                  │
├─────────────────────────────────────────────────────────────────┤
│ LYBT.Desktop.MedicalCase                                         │
│   └── MedicalCaseFlowViewModel                                   │
│         └── Step2: 处方开具 (调用ISaveable接口)                  │
│             ├── _currentStepViewModel.Validate()  // IValidatable│
│             └── _currentStepViewModel.SaveAsync() // ISaveable   │
│                                                                  │
│ LYBT.Desktop.Herbs (药材选择)                                    │
│   └── HerbSelectionDialog → 返回List<HerbDto>                   │
│                                                                  │
│ LYBT.Desktop.Formula (验方模板)                                  │
│   └── FormulaTemplateDialog → 返回FormulaDto                    │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 展示层 (Presentation Layer) - ViewModels                         │
├─────────────────────────────────────────────────────────────────┤
│ PrescriptionManagementViewModel (597行) ★列表管理★               │
│   ├── 数据属性 (25个)                                            │
│   │   ├── Prescriptions (ObservableCollection)  // 处方列表     │
│   │   ├── SelectedPrescription (PrescriptionDto) // 选中处方    │
│   │   ├── SearchText (string)                   // 搜索关键字   │
│   │   ├── StartDate / EndDate (DateTime?)       // 日期范围    │
│   │   ├── CurrentPage / PageSize / TotalCount   // 分页参数    │
│   │   └── CanCreate / CanDelete / CanPrint ...  // 命令状态    │
│   ├── 命令 (20个)                                                │
│   │   ├── LoadDataCommand                       // 加载数据     │
│   │   ├── SearchCommand                         // 搜索处方     │
│   │   ├── CreateCommand                         // 创建处方     │
│   │   ├── EditCommand                           // 编辑处方     │
│   │   ├── DeleteCommand                         // 删除处方     │
│   │   ├── PrintCommand                          // 打印处方     │
│   │   ├── RefreshCommand                        // 刷新列表     │
│   │   ├── PreviousPageCommand / NextPageCommand // 分页导航    │
│   │   ├── ClearFiltersCommand                   // 清除过滤器   │
│   │   └── ExportPrescriptionsCommand            // 导出Excel   │
│   └── 方法 (20个)                                                │
│       ├── LoadDataAsync()                       // 分页查询API  │
│       ├── SearchAsync()                         // 重置到第一页 │
│       ├── Create()                              // 打开编辑对话框│
│       ├── Edit()                                // 打开编辑对话框│
│       ├── DeleteAsync()                         // 确认后删除   │
│       ├── Print()                               // 调用打印服务 │
│       ├── ExportPrescriptionsAsync()            // 导出到桌面   │
│       └── UpdateCommandStates()                 // 更新命令状态 │
│                                                                  │
│                      ↓ ShowDialog                                │
│                                                                  │
│ PrescriptionEditorDialogViewModel (668行) ★核心编辑器★           │
│   ├── 接口实现                                                    │
│   │   ├── ISaveable (Validate + SaveAsync)                      │
│   │   ├── IValidatable (Validate + ValidationMessage)           │
│   │   └── IDialogAware (OnDialogOpened + RequestClose)          │
│   ├── 数据属性 (30个)                                            │
│   │   ├── PrescriptionId (Guid?)                // 处方ID       │
│   │   ├── CurrentMedicalCaseId (Guid)           // 医案ID       │
│   │   ├── DosageCount (int)                     // 剂数         │
│   │   ├── Usage (string)                        // 用法         │
│   │   ├── MedicalAdvice (string)                // 医嘱         │
│   │   ├── Discount (decimal)                    // 折扣         │
│   │   ├── TotalAmount (decimal)                 // 总金额(只读) │
│   │   ├── SingleDoseAmount (decimal)            // 单剂金额(只读)│
│   │   ├── HerbItems (ObservableCollection)      // 药材条目集合 │
│   │   ├── HasChanges (bool)                     // 变更标记     │
│   │   └── CanSave / CanCancel ...               // 命令状态     │
│   ├── 命令 (9个)                                                 │
│   │   ├── SaveCommand                           // 保存处方     │
│   │   ├── CancelCommand                         // 取消编辑     │
│   │   ├── AddHerbCommand                        // 添加药材     │
│   │   ├── RemoveHerbCommand                     // 移除药材     │
│   │   ├── LoadFormulaCommand                    // 加载验方     │
│   │   ├── PreviewCommand                        // 打印预览     │
│   │   ├── CalculateCommand                      // 重新计算     │
│   │   └── ClearHerbsCommand                     // 清空药材     │
│   └── 方法 (15个)                                                │
│       ├── OnDialogOpened()                      // 加载处方数据 │
│       ├── Validate()                            // 验证必填项   │
│       ├── SaveAsync()                           // 保存处方     │
│       ├── AddHerbAsync()                        // 打开药材选择 │
│       ├── LoadFormulaAsync()                    // 打开验方选择 │
│       ├── CalculateTotalAmount()                // 计算总价     │
│       ├── OnPropertyChanged()                   // 监听变更     │
│       └── CanCloseDialog()                      // 关闭前检查   │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 对话框组件 (Dialog Components)                                   │
├─────────────────────────────────────────────────────────────────┤
│ HerbSelectionDialog (药材选择)                                   │
│   ├── Herbs (ObservableCollection)              // 药材列表     │
│   ├── SelectedHerbs (ObservableCollection)      // 选中药材     │
│   ├── SearchKeyword (string)                    // 支持拼音搜索 │
│   ├── LoadDataAsync()                           // 查询Herbs模块│
│   └── Confirm() → 返回List<HerbDto>                             │
│                                                                  │
│ FormulaTemplateDialog (验方模板)                                 │
│   ├── Formulas (ObservableCollection)           // 验方列表     │
│   ├── SelectedFormula (FormulaDto)              // 选中验方     │
│   ├── SelectedCategory (string)                 // 分类过滤     │
│   ├── LoadDataAsync()                           // 查询Formula模块│
│   └── Confirm() → 返回FormulaDto                                │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 服务层 (Service Layer)                                           │
├─────────────────────────────────────────────────────────────────┤
│ ViewModel Components (组件化设计)                                │
│   ├── PrescriptionCalculator         // 价格计算器              │
│   ├── PrescriptionCommandHandler     // 命令处理器              │
│   ├── PrescriptionDataManager        // 数据管理器              │
│   ├── PrescriptionEventCoordinator   // 事件协调器              │
│   └── PrescriptionValidator          // 验证器                 │
│                                                                  │
│ 打印服务 (Print Services)                                        │
│   ├── IPrescriptionPrintService       // 打印服务接口           │
│   ├── PrescriptionPrintService        // WPF打印实现            │
│   │   ├── PrintAsync()                // 打印处方               │
│   │   └── PreviewAsync()              // 打印预览               │
│   └── PrescriptionFlowDocumentBuilder // FlowDocument构建器     │
│       └── BuildAsync()                // 生成A4打印文档         │
└─────────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│ 数据访问层 (Data Access Layer)                                   │
├─────────────────────────────────────────────────────────────────┤
│ IPrescriptionRepository → BaseApiRepository<PrescriptionDto>     │
│   ├── GetPagedAsync()                         // 分页查询       │
│   ├── GetByIdAsync()                          // 按ID查询       │
│   ├── CreateAsync()                           // 创建处方       │
│   ├── UpdateAsync()                           // 更新处方       │
│   └── DeleteAsync()                           // 删除处方       │
│                     ↓                                            │
│ IApiService → HttpClient → LYBT.WebAPI                           │
│                     ↓                                            │
│          /api/v1/prescriptions/*                                 │
└─────────────────────────────────────────────────────────────────┘
```

### ISaveable接口契约

```csharp
// ISaveable接口定义(Shared.Interfaces)
public interface ISaveable
{
    bool Validate();                // 验证必填项
    Task SaveAsync();               // 保存数据
}

public interface IValidatable
{
    bool Validate();                // 验证数据完整性
    string ValidationMessage { get; set; }  // 验证错误信息
}

// PrescriptionEditorDialogViewModel实现ISaveable
public class PrescriptionEditorDialogViewModel : ISaveable, IValidatable
{
    public bool Validate()
    {
        if (DosageCount <= 0) { ValidationMessage = "剂数必须大于0"; return false; }
        if (HerbItems.Count == 0) { ValidationMessage = "至少需要1个药材"; return false; }
        foreach (var item in HerbItems)
        {
            if (item.Dosage <= 0) { ValidationMessage = $"{item.HerbName}剂量必须大于0"; return false; }
        }
        return true;
    }

    public async Task SaveAsync()
    {
        if (!Validate()) throw new ValidationException(ValidationMessage);

        if (PrescriptionId.HasValue)
            await _prescriptionApi.UpdateAsync(PrescriptionId.Value, updateDto);
        else
            await _prescriptionApi.CreateAsync(createDto);
    }
}

// MedicalCaseFlowViewModel通过ISaveable接口调用
public class MedicalCaseFlowViewModel
{
    private ISaveable? _currentStepViewModel;

    private async Task CompleteStep2Async()
    {
        if (_currentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate()) { SetWarningMessage(validatable.ValidationMessage); return; }
        }

        if (_currentStepViewModel != null)
        {
            await _currentStepViewModel.SaveAsync();  // 调用ISaveable接口
        }

        await _medicalCaseRepository.CompleteStep2Async(MedicalCaseId, DateTime.Now);
    }
}
```

---

## 🔧 核心功能

### 1. 处方编号生成 - CF-YYYYMMDD-0001格式

**Server端实现**：

```csharp
/// <summary>
/// 处方编号服务 - 生成唯一处方编号(格式:CF-YYYYMMDD-0001)
/// 核心逻辑:按日期递增序号,确保编号唯一性
/// </summary>
public class PrescriptionNumberService
{
    // 生成处方编号
    public async Task<string> GenerateNumberAsync(DateTime prescriptionDate)
    {
        var dateStr = prescriptionDate.ToString("yyyyMMdd");
        var prefix = $"CF-{dateStr}-";

        // 获取今日已有编号的最大序列号
        var maxSequence = await GetMaxSequenceForDateAsync(prescriptionDate);
        var nextSequence = maxSequence + 1;

        // 生成4位序列号(补0)
        var sequenceStr = nextSequence.ToString("D4");
        return $"{prefix}{sequenceStr}";
    }

    // 获取日期最大序号
    private async Task<int> GetMaxSequenceForDateAsync(DateTime date)
    {
        var dateStr = date.ToString("yyyyMMdd");
        var prefix = $"CF-{dateStr}-";

        // 查询今日所有处方编号
        var prescriptions = await _repository.GetPrescriptionNumbersByPrefixAsync(prefix);

        if (!prescriptions.Any())
            return 0;

        // 提取序列号并取最大值
        var maxSequence = prescriptions
            .Select(no => int.Parse(no.Substring(prefix.Length)))
            .Max();

        return maxSequence;
    }

    // 验证编号格式
    public bool ValidateNumberFormat(string prescriptionNo)
    {
        if (string.IsNullOrEmpty(prescriptionNo) || prescriptionNo.Length != 17)
            return false;

        var pattern = @"^CF-\d{8}-\d{4}$";
        return Regex.IsMatch(prescriptionNo, pattern);
    }
}

// 使用示例
var prescriptionNo = await _numberService.GenerateNumberAsync(DateTime.Now);
// 输出:CF-20250129-0001
```

### 2. 处方状态管理 - Draft → Confirmed → Dispensed状态机

**Server端实现**：

```csharp
/// <summary>
/// 处方状态枚举
/// </summary>
public enum PrescriptionStatus
{
    Draft = 1,          // 草稿(可编辑)
    Confirmed = 2,      // 已确认(不可编辑)
    Dispensed = 3       // 已配药(完成)
}

/// <summary>
/// 处方服务 - 状态流转控制
/// </summary>
public class PrescriptionService
{
    // 确认处方(Draft → Confirmed)
    public async Task ConfirmAsync(Guid prescriptionId)
    {
        var prescription = await _repository.GetByIdAsync(prescriptionId);

        // 验证状态迁移
        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new InvalidOperationException("只有草稿状态的处方可以确认");
        }

        prescription.Status = PrescriptionStatus.Confirmed;
        prescription.ConfirmedAt = DateTime.Now;
        await _repository.UpdateAsync(prescription);
    }

    // 标记配药完成(Confirmed → Dispensed)
    public async Task DispenseAsync(Guid prescriptionId)
    {
        var prescription = await _repository.GetByIdAsync(prescriptionId);

        // 验证状态迁移
        if (prescription.Status != PrescriptionStatus.Confirmed)
        {
            throw new InvalidOperationException("只有已确认的处方可以配药");
        }

        prescription.Status = PrescriptionStatus.Dispensed;
        prescription.DispensedAt = DateTime.Now;
        await _repository.UpdateAsync(prescription);
    }
}

// FluentValidation验证(Draft状态才可编辑)
public class PrescriptionEditDtoValidator : AbstractValidator<PrescriptionEditDto>
{
    public PrescriptionEditDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status == PrescriptionStatus.Draft)
            .WithMessage("只有草稿状态的处方可以编辑");
    }
}
```

### 3. 药材选择集成 - 从Herbs模块选择药材

**Client端实现**：

```csharp
/// <summary>
/// PrescriptionEditorDialogViewModel - 添加药材功能
/// 打开HerbSelectionDialog,选择药材后添加到HerbItems集合
/// </summary>
private async Task AddHerbAsync()
{
    var parameters = new DialogParameters();
    _dialogService.ShowDialog(
        "HerbSelectionDialog",
        parameters,
        result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");

                foreach (var herb in selectedHerbs)
                {
                    // 检查药材是否已存在
                    var existingItem = HerbItems.FirstOrDefault(x => x.HerbId == herb.Id);
                    if (existingItem != null)
                    {
                        existingItem.Dosage += 1;  // 药材已存在,剂量+1
                    }
                    else
                    {
                        // 添加新药材条目
                        HerbItems.Add(new PrescriptionItemRow
                        {
                            HerbId = herb.Id,
                            HerbName = herb.Name,
                            Dosage = herb.DefaultDosage ?? 10m,
                            Unit = herb.DefaultUnit ?? "克",
                            UnitPrice = herb.UnitPrice ?? 0m,
                            Notes = string.Empty
                        });
                    }
                }

                // 触发总价重新计算
                CalculateTotalAmount();
            }
        }
    );
}

/// <summary>
/// HerbSelectionDialogViewModel - 药材选择对话框
/// 从Herbs模块查询药材列表,支持拼音快速搜索
/// </summary>
public class HerbSelectionDialogViewModel : IDialogAware
{
    public ObservableCollection<HerbDto> Herbs { get; set; }            // 药材列表
    public ObservableCollection<HerbDto> SelectedHerbs { get; set; }    // 选中的药材
    public string SearchKeyword { get; set; }                           // 支持拼音搜索

    // 加载药材数据
    private async Task LoadDataAsync()
    {
        var result = await _herbApi.GetPagedAsync(
            pageIndex: CurrentPage,
            pageSize: PageSize,
            searchText: SearchKeyword  // 支持名称/拼音/功效搜索
        );

        Herbs.Clear();
        foreach (var herb in result.Items)
        {
            Herbs.Add(herb);
        }
    }

    // 确认选择(返回选中的药材)
    private void Confirm()
    {
        if (SelectedHerbs.Count == 0)
        {
            SetWarningMessage("请至少选择一个药材");
            return;
        }

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("SelectedHerbs", SelectedHerbs.ToList());
        RequestClose?.Invoke(result);
    }
}
```

### 4. 验方模板集成 - 从Formula模块加载验方

**Client端实现**：

```csharp
/// <summary>
/// PrescriptionEditorDialogViewModel - 加载验方功能
/// 打开FormulaTemplateDialog,选择验方后自动加载药材条目、用法医嘱
/// </summary>
private async Task LoadFormulaAsync()
{
    try
    {
        IsBusy = true;

        var parameters = new DialogParameters();
        _dialogService.ShowDialog(
            "FormulaTemplateDialog",
            parameters,
            async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    var selectedFormula = result.Parameters.GetValue<FormulaDto>("SelectedFormula");

                    // Step 1: 清空现有药材条目(避免混淆)
                    HerbItems.Clear();

                    // Step 2: 加载验方药材条目
                    foreach (var item in selectedFormula.HerbItems)
                    {
                        HerbItems.Add(new PrescriptionItemRow
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Dosage = item.Dosage,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Notes = item.Notes
                        });
                    }

                    // Step 3: 应用验方的用法医嘱
                    Usage = selectedFormula.UsageInstructions;
                    MedicalAdvice = selectedFormula.Description;

                    // Step 4: 重新计算总价
                    CalculateTotalAmount();

                    _logger.LogInformation($"已加载验方:{selectedFormula.Name}");
                }
            }
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载验方模板失败");
        SetErrorMessage($"加载验方模板失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

/// <summary>
/// FormulaTemplateDialogViewModel - 验方模板对话框
/// 从Formula模块查询验方列表,选择验方后返回FormulaDto
/// </summary>
public class FormulaTemplateDialogViewModel : IDialogAware
{
    public ObservableCollection<FormulaDto> Formulas { get; set; }    // 验方列表
    public FormulaDto? SelectedFormula { get; set; }                   // 选中的验方
    public string SelectedCategory { get; set; }                       // 分类过滤

    // 加载验方数据
    private async Task LoadDataAsync()
    {
        var result = await _formulaApi.GetPagedAsync(
            pageIndex: CurrentPage,
            pageSize: PageSize,
            searchText: SearchKeyword,
            category: SelectedCategory
        );

        Formulas.Clear();
        foreach (var formula in result.Items)
        {
            Formulas.Add(formula);
        }
    }

    // 确认选择(返回选中的验方)
    private void Confirm()
    {
        if (SelectedFormula == null)
        {
            SetWarningMessage("请选择一个验方");
            return;
        }

        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("SelectedFormula", SelectedFormula);
        RequestClose?.Invoke(result);
    }
}
```

### 5. 价格自动计算 - TotalAmount = Σ(UnitPrice × Dosage) × DosageCount × Discount

**Client端实现**：

```csharp
/// <summary>
/// PrescriptionEditorDialogViewModel - 价格计算器
/// 自动监听DosageCount、Discount、HerbItems变更,实时更新总价
/// </summary>
private void CalculateTotalAmount()
{
    if (HerbItems == null || HerbItems.Count == 0)
    {
        TotalAmount = 0m;
        SingleDoseAmount = 0m;
        return;
    }

    // 计算单剂金额(所有药材的 UnitPrice × Dosage)
    SingleDoseAmount = HerbItems.Sum(item => item.UnitPrice * item.Dosage);

    // 总金额 = 单剂金额 × 剂数 × 折扣
    TotalAmount = SingleDoseAmount * DosageCount * Discount;
}

// 监听属性变更 → 自动重新计算
protected override void OnPropertyChanged(PropertyChangedEventArgs e)
{
    base.OnPropertyChanged(e);

    if (e.PropertyName == nameof(DosageCount) ||
        e.PropertyName == nameof(Discount))
    {
        CalculateTotalAmount();  // 剂数或折扣变更 → 重新计算
    }
}

// 监听HerbItems集合变更 → 自动重新计算
private void OnHerbItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    CalculateTotalAmount();  // 药材条目变更 → 重新计算
}

// 默认值设置
public PrescriptionEditorDialogViewModel()
{
    DosageCount = 7;       // 默认7剂
    Discount = 1.0m;       // 默认无折扣
    HerbItems = new ObservableCollection<PrescriptionItemRow>();

    // 监听集合变更
    HerbItems.CollectionChanged += OnHerbItemsCollectionChanged;
}

// 药材条目数据模型
public class PrescriptionItemRow
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public decimal Dosage { get; set; }           // 剂量
    public string Unit { get; set; }              // 单位
    public decimal UnitPrice { get; set; }        // 单价
    public decimal Subtotal => UnitPrice * Dosage;  // 小计(只读属性)
    public string Notes { get; set; }
}
```

### 6. ISaveable接口契约 - 与MedicalCase集成

**Client端实现**：

```csharp
/// <summary>
/// PrescriptionEditorDialogViewModel - 实现ISaveable接口
/// 支持MedicalCaseFlowViewModel通过接口调用处方功能
/// </summary>
public class PrescriptionEditorDialogViewModel : ISaveable, IValidatable, IDialogAware
{
    // ISaveable接口:验证方法
    public bool Validate()
    {
        // 必填项验证
        if (DosageCount <= 0)
        {
            ValidationMessage = "剂数必须大于0";
            return false;
        }

        if (HerbItems == null || HerbItems.Count == 0)
        {
            ValidationMessage = "处方至少需要包含1个药材";
            return false;
        }

        // 验证药材条目剂量
        foreach (var item in HerbItems)
        {
            if (item.Dosage <= 0)
            {
                ValidationMessage = $"药材{item.HerbName}剂量必须大于0";
                return false;
            }
        }

        ValidationMessage = string.Empty;
        return true;
    }

    // ISaveable接口:保存方法
    public async Task SaveAsync()
    {
        try
        {
            if (!Validate())
            {
                throw new ValidationException(ValidationMessage);
            }

            if (PrescriptionId.HasValue)
            {
                // 更新现有处方
                var updateDto = new UpdatePrescriptionDto
                {
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                await _prescriptionApi.UpdateAsync(PrescriptionId.Value, updateDto);
            }
            else
            {
                // 创建新处方
                var createDto = new CreatePrescriptionDto
                {
                    MedicalCaseId = CurrentMedicalCaseId,
                    DosageCount = DosageCount,
                    Usage = Usage,
                    MedicalAdvice = MedicalAdvice,
                    Remark = Remark,
                    Discount = Discount,
                    HerbItems = HerbItems.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        Dosage = item.Dosage,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Notes = item.Notes
                    }).ToList()
                };

                var created = await _prescriptionApi.CreateAsync(createDto);
                PrescriptionId = created.Id;
            }

            HasChanges = false;
            _logger.LogInformation($"处方已保存:PrescriptionId={PrescriptionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存处方失败");
            throw;
        }
    }
}

/// <summary>
/// MedicalCaseFlowViewModel - 通过ISaveable接口调用处方功能
/// 实现Step2处方开具逻辑(辩证 → 施治 → 总结三步工作流)
/// </summary>
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private ISaveable? _currentStepViewModel;  // 当前步骤的ViewModel

    // 完成Step2处方开具(保存处方)
    private async Task CompleteStep2Async()
    {
        try
        {
            IsBusy = true;

            // 验证当前步骤ViewModel
            if (_currentStepViewModel is IValidatable validatable)
            {
                if (!validatable.Validate())
                {
                    SetWarningMessage(validatable.ValidationMessage);
                    return;
                }
            }

            // 保存当前步骤数据(调用ISaveable.SaveAsync)
            if (_currentStepViewModel != null)
            {
                await _currentStepViewModel.SaveAsync();
            }

            // 标记Step2完成
            await _medicalCaseRepository.CompleteStep2Async(MedicalCaseId, DateTime.Now);

            SetSuccessMessage("处方已开具,可以进入下一步");

            // 通知属性变更(更新UI)
            RaisePropertyChanged(nameof(Step3Enabled));
            RaisePropertyChanged(nameof(Step3Disabled));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成Step2失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 7. 处方打印服务 - FlowDocument生成与WPF打印

**Client端实现**：

```csharp
/// <summary>
/// 处方打印服务 - 生成FlowDocument并调用WPF打印
/// 核心功能:处方格式化、FlowDocument生成、打印预览、打印机打印
/// </summary>
public class PrescriptionPrintService : IPrescriptionPrintService
{
    private readonly PrescriptionFlowDocumentBuilder _documentBuilder;

    // 打印处方(调用WPF PrintDialog)
    public async Task PrintAsync(PrescriptionDto prescription)
    {
        try
        {
            // Step 1: 生成FlowDocument
            var document = await _documentBuilder.BuildAsync(prescription);

            // Step 2: 创建WPF打印对话框
            var printDialog = new PrintDialog();

            // Step 3: 显示打印对话框,用户选择打印机
            if (printDialog.ShowDialog() == true)
            {
                // 获取DocumentPaginator
                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;

                // 打印文档
                printDialog.PrintDocument(paginator, $"处方单-{prescription.PrescriptionNo}");

                _logger.LogInformation($"处方已发送到打印机:PrescriptionId={prescription.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"打印处方失败:PrescriptionId={prescription.Id}");
            throw;
        }
    }

    // 打印预览(显示FlowDocument)
    public async Task PreviewAsync(PrescriptionDto prescription)
    {
        try
        {
            // 生成FlowDocument
            var document = await _documentBuilder.BuildAsync(prescription);

            // 创建预览窗口
            var previewWindow = new Window
            {
                Title = $"打印预览 - {prescription.PrescriptionNo}",
                Width = 800,
                Height = 600,
                Content = new FlowDocumentScrollViewer
                {
                    Document = document
                }
            };

            previewWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"打开打印预览失败:PrescriptionId={prescription.Id}");
            throw;
        }
    }
}

/// <summary>
/// FlowDocument构建器 - 生成处方打印文档
/// 核心功能:格式化处方信息、药材条目列表、总价计算、医嘱显示
/// </summary>
public class PrescriptionFlowDocumentBuilder
{
    // 生成FlowDocument
    public async Task<FlowDocument> BuildAsync(PrescriptionDto prescription)
    {
        var document = new FlowDocument
        {
            PageWidth = 793.7,   // A4纸宽度(像素)
            PageHeight = 1122.5, // A4纸高度(像素)
            PagePadding = new Thickness(50),
            FontFamily = new FontFamily("微软雅黑"),
            FontSize = 14
        };

        // 标题
        var titleParagraph = new Paragraph(new Run("中医处方单"))
        {
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(titleParagraph);

        // 处方基础信息
        var infoParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
        infoParagraph.Inlines.Add(new Run($"处方编号:{prescription.PrescriptionNo}"));
        infoParagraph.Inlines.Add(new LineBreak());
        infoParagraph.Inlines.Add(new Run($"患者姓名:{prescription.PatientName}"));
        infoParagraph.Inlines.Add(new LineBreak());
        infoParagraph.Inlines.Add(new Run($"开方日期:{prescription.CreatedAt:yyyy-MM-dd HH:mm}"));
        infoParagraph.Inlines.Add(new LineBreak());
        infoParagraph.Inlines.Add(new Run($"医生:{prescription.DoctorName}"));
        document.Blocks.Add(infoParagraph);

        // 药材条目表格
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        // 定义列
        table.Columns.Add(new TableColumn { Width = new GridLength(50) });  // 序号
        table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // 药材名称
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 剂量
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 单价
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // 小计

        // 表头
        var headerRowGroup = new TableRowGroup();
        var headerRow = new TableRow();
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("序号"))) { FontWeight = FontWeights.Bold });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("药材名称"))) { FontWeight = FontWeights.Bold });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("剂量"))) { FontWeight = FontWeights.Bold });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("单价(元/克)"))) { FontWeight = FontWeights.Bold });
        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("小计(元)"))) { FontWeight = FontWeights.Bold });
        headerRowGroup.Rows.Add(headerRow);
        table.RowGroups.Add(headerRowGroup);

        // 药材条目数据
        var dataRowGroup = new TableRowGroup();
        int index = 1;
        foreach (var item in prescription.HerbItems)
        {
            var row = new TableRow();
            row.Cells.Add(new TableCell(new Paragraph(new Run(index.ToString()))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(item.HerbName))));
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.Dosage}{item.Unit}"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{item.UnitPrice:F2}"))));
            row.Cells.Add(new TableCell(new Paragraph(new Run($"{(item.UnitPrice * item.Dosage):F2}"))));
            dataRowGroup.Rows.Add(row);
            index++;
        }
        table.RowGroups.Add(dataRowGroup);
        document.Blocks.Add(table);

        // 总价信息
        var summaryParagraph = new Paragraph { Margin = new Thickness(0, 10, 0, 10) };
        summaryParagraph.Inlines.Add(new Run($"剂数:{prescription.DosageCount}剂"));
        summaryParagraph.Inlines.Add(new LineBreak());
        summaryParagraph.Inlines.Add(new Run($"折扣:{(prescription.Discount * 100):F0}%"));
        summaryParagraph.Inlines.Add(new LineBreak());
        summaryParagraph.Inlines.Add(new Run($"总金额:{prescription.TotalAmount:F2}元")
        {
            FontWeight = FontWeights.Bold,
            FontSize = 16
        });
        document.Blocks.Add(summaryParagraph);

        // 用法
        if (!string.IsNullOrEmpty(prescription.Usage))
        {
            var usageParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
            usageParagraph.Inlines.Add(new Run("用法:"));
            usageParagraph.Inlines.Add(new LineBreak());
            usageParagraph.Inlines.Add(new Run(prescription.Usage));
            document.Blocks.Add(usageParagraph);
        }

        // 医嘱
        if (!string.IsNullOrEmpty(prescription.MedicalAdvice))
        {
            var adviceParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
            adviceParagraph.Inlines.Add(new Run("医嘱:"));
            adviceParagraph.Inlines.Add(new LineBreak());
            adviceParagraph.Inlines.Add(new Run(prescription.MedicalAdvice));
            document.Blocks.Add(adviceParagraph);
        }

        return document;
    }
}
```

---

## 📋 业务规则

### 处方编号规则

| 规则项 | 说明 | 示例 |
|--------|------|------|
| 格式 | CF-YYYYMMDD-0001（17字符） | CF-20250129-0001 |
| 前缀 | CF（固定） | CF |
| 日期 | YYYYMMDD格式 | 20250129 |
| 序号 | 4位数字（补0） | 0001 |
| 唯一性 | 按日期递增序号 | 同日期不重复 |

### 处方状态规则

| 状态 | 说明 | 允许操作 | 禁止操作 |
|------|------|---------|---------|
| Draft（草稿） | 新建处方，可编辑 | 编辑、删除、确认 | 配药 |
| Confirmed（已确认） | 确认后不可编辑 | 查看、打印、配药 | 编辑、删除 |
| Dispensed（已配药） | 已完成 | 查看、打印 | 编辑、删除、配药 |

### 价格计算规则

| 计算项 | 公式 | 说明 |
|--------|------|------|
| 单药材小计 | UnitPrice × Dosage | 单价 × 剂量 |
| 单剂金额 | Σ(UnitPrice × Dosage) | 所有药材小计之和 |
| 总金额 | SingleDoseAmount × DosageCount × Discount | 单剂金额 × 剂数 × 折扣 |
| 折扣范围 | 0.0 - 1.0 | 1.0表示无折扣 |
| 默认剂数 | 7剂 | 可修改 |

### 验证规则

| 验证项 | 规则 | 错误信息 |
|--------|------|---------|
| 剂数 | DosageCount > 0 | 剂数必须大于0 |
| 药材条目 | HerbItems.Count > 0 | 处方至少需要包含1个药材 |
| 药材剂量 | item.Dosage > 0 | 药材{name}剂量必须大于0 |
| 处方编号 | 格式验证 | 编号格式错误 |
| 状态迁移 | 状态机规则 | 只有X状态可以Y操作 |

---

## 🔌 API 端点

### Server端API端点

| 方法 | 端点 | 说明 | 返回类型 |
|------|------|------|---------|
| GET | `/api/v1/prescriptions/{id}` | 按ID查询处方 | PrescriptionDto |
| GET | `/api/v1/prescriptions/paged` | 分页查询处方 | PagedResult<PrescriptionDto> |
| GET | `/api/v1/prescriptions/medical-case/{medicalCaseId}` | 按医案ID查询处方 | PrescriptionDto |
| GET | `/api/v1/prescriptions/patient/{patientId}` | 按患者ID查询处方列表 | List<PrescriptionDto> |
| POST | `/api/v1/prescriptions` | 创建处方 | PrescriptionDto |
| PUT | `/api/v1/prescriptions/{id}` | 更新处方 | void |
| DELETE | `/api/v1/prescriptions/{id}` | 删除处方 | void |
| POST | `/api/v1/prescriptions/{id}/confirm` | 确认处方 | void |
| POST | `/api/v1/prescriptions/{id}/dispense` | 标记配药完成 | void |
| GET | `/api/v1/prescriptions/export` | 导出Excel | byte[] |

### DTO定义

```csharp
// CreatePrescriptionDto - 创建处方请求
public class CreatePrescriptionDto
{
    public Guid MedicalCaseId { get; set; }              // 医案ID(必填)
    public int DosageCount { get; set; }                 // 剂数(必填)
    public string Usage { get; set; }                    // 用法(可选)
    public string MedicalAdvice { get; set; }            // 医嘱(可选)
    public string Remark { get; set; }                   // 备注(可选)
    public decimal Discount { get; set; } = 1.0m;        // 折扣(默认1.0)
    public List<PrescriptionItemDto> HerbItems { get; set; }  // 药材条目(必填)
}

// UpdatePrescriptionDto - 更新处方请求
public class UpdatePrescriptionDto
{
    public int DosageCount { get; set; }                 // 剂数
    public string Usage { get; set; }                    // 用法
    public string MedicalAdvice { get; set; }            // 医嘱
    public string Remark { get; set; }                   // 备注
    public decimal Discount { get; set; }                // 折扣
    public List<PrescriptionItemDto> HerbItems { get; set; }  // 药材条目
}

// PrescriptionItemDto - 处方药材条目
public class PrescriptionItemDto
{
    public Guid HerbId { get; set; }                     // 药材ID
    public decimal Dosage { get; set; }                  // 剂量
    public string Unit { get; set; }                     // 单位
    public decimal UnitPrice { get; set; }               // 单价
    public string Notes { get; set; }                    // 备注
}

// PrescriptionDto - 处方响应
public class PrescriptionDto
{
    public Guid Id { get; set; }                         // 处方ID
    public string PrescriptionNo { get; set; }           // 处方编号
    public Guid MedicalCaseId { get; set; }              // 医案ID
    public Guid PatientId { get; set; }                  // 患者ID
    public string PatientName { get; set; }              // 患者姓名
    public Guid DoctorId { get; set; }                   // 医生ID
    public string DoctorName { get; set; }               // 医生姓名
    public int DosageCount { get; set; }                 // 剂数
    public string Usage { get; set; }                    // 用法
    public string MedicalAdvice { get; set; }            // 医嘱
    public string Remark { get; set; }                   // 备注
    public decimal Discount { get; set; }                // 折扣
    public decimal TotalAmount { get; set; }             // 总金额
    public PrescriptionStatus Status { get; set; }       // 状态
    public DateTime? ConfirmedAt { get; set; }           // 确认时间
    public DateTime? DispensedAt { get; set; }           // 配药时间
    public DateTime CreatedAt { get; set; }              // 创建时间
    public DateTime UpdatedAt { get; set; }              // 更新时间
    public List<PrescriptionItemDto> HerbItems { get; set; }  // 药材条目
}

// PagedResult<T> - 分页结果
public class PagedResult<T>
{
    public List<T> Items { get; set; }                   // 数据集合
    public int TotalCount { get; set; }                  // 总记录数
    public int PageIndex { get; set; }                   // 当前页码
    public int PageSize { get; set; }                    // 每页数量
    public int TotalPages { get; set; }                  // 总页数
}
```

---

## 🎯 设计原则

### Server端设计原则（6条）

1. **处方编号唯一性保证**
   - ✅ 按日期生成编号（CF-YYYYMMDD-序号）
   - ✅ 获取日期最大序号后+1（确保不重复）
   - ✅ 格式验证（正则表达式`^CF-\d{8}-\d{4}$`）
   - ❌ 避免手动输入编号

2. **状态机严格控制**
   - ✅ 状态迁移验证（Draft → Confirmed → Dispensed）
   - ✅ 只有Draft状态可编辑
   - ✅ 确认后自动记录ConfirmedAt时间戳
   - ❌ 禁止状态回退

3. **验方集成支持**
   - ✅ CreateFromFormulaAsync加载验方药材条目
   - ✅ FormulaId关联验方模板
   - ✅ 自动计算总价
   - ❌ 避免手动复制验方数据

4. **Include预加载优化**
   - ✅ GetByIdWithItemsAsync预加载药材条目
   - ✅ GetPagedWithDetailsAsync预加载患者、医生信息
   - ✅ 避免N+1查询问题
   - ❌ 禁止延迟加载（可能导致性能问题）

5. **价格计算服务端验证**
   - ✅ CalculateTotalAmount方法验证价格正确性
   - ✅ Server端计算结果与Client端一致性检查
   - ✅ 防止客户端篡改价格
   - ❌ 不信任客户端传递的总价

6. **FluentValidation验证**
   - ✅ CreatePrescriptionDto验证（医案ID、药材条目、剂量）
   - ✅ UpdatePrescriptionDto验证（状态、数据完整性）
   - ✅ 自定义验证规则（状态迁移、剂量有效性）
   - ❌ 不允许空药材条目

### Client端设计原则（7条）

1. **ISaveable接口契约 - 与MedicalCase集成**
   - ✅ **接口解耦**：实现ISaveable接口，MedicalCaseFlowViewModel通过接口调用（无需依赖具体类型）
   - ✅ **Validate验证**：保存前验证必填项（剂数>0、药材条目>0、剂量有效）
   - ✅ **SaveAsync保存**：异步保存处方，支持新建和更新两种模式
   - ✅ **HasChanges标记**：数据变更检测，控制保存按钮启用与对话框关闭确认
   - ❌ **避免紧耦合**：不直接依赖具体类型
   - ❌ **避免返回Result<T>**：Repository直接返回DTO裸类型

2. **Dialog-based架构 - 对话框驱动的复杂交互**
   - ✅ **对话框封装**：复杂功能封装为对话框（PrescriptionEditorDialog、HerbSelectionDialog、FormulaTemplateDialog）
   - ✅ **参数传递**：通过DialogParameters传递参数，通过DialogResult.Parameters返回结果
   - ✅ **模态交互**：对话框模态显示，用户完成操作后关闭
   - ✅ **CanCloseDialog**：对话框关闭前检查HasChanges，如有未保存变更弹出确认对话框
   - ✅ **OnDialogOpened**：对话框打开时初始化数据
   - ❌ **避免Region导航**：处方编辑等复杂交互不适合Region导航

3. **价格计算器 - 自动计算总价与单价**
   - ✅ **总价计算公式**：TotalAmount = Σ(UnitPrice × Dosage) × DosageCount × Discount
   - ✅ **自动更新**：监听DosageCount、Discount、HerbItems.CollectionChanged变更，自动重新计算总价
   - ✅ **单价计算**：PrescriptionItemRow.Subtotal = UnitPrice × Dosage
   - ✅ **默认值设置**：DosageCount默认7剂，Discount默认1.0
   - ✅ **精度控制**：所有金额使用decimal类型，格式化时保留2位小数（F2）
   - ❌ **避免手动计算**：价格计算逻辑集中在CalculateTotalAmount方法

4. **验方模板支持 - 从Formula模块加载验方**
   - ✅ **验方模板加载**：通过FormulaTemplateDialog查询验方列表
   - ✅ **药材条目应用**：将FormulaDto.HerbItems转换为PrescriptionItemRow并添加到HerbItems集合
   - ✅ **用法医嘱应用**：自动填充到处方的Usage、MedicalAdvice字段
   - ✅ **清空现有条目**：加载验方前先清空HerbItems集合
   - ✅ **智能匹配**：Formula模块的HerbId与Herbs模块的HerbId一致
   - ❌ **避免手动添加**：使用验方模板时一次性加载所有药材条目

5. **打印服务 - FlowDocument生成与WPF打印**
   - ✅ **FlowDocument构建**：生成处方打印文档（标题、基础信息、药材表格、总价、用法、医嘱）
   - ✅ **WPF打印**：调用WPF PrintDialog显示打印对话框
   - ✅ **打印预览**：PreviewAsync方法显示FlowDocument预览窗口
   - ✅ **A4纸适配**：PageWidth=793.7、PageHeight=1122.5（A4纸像素尺寸）
   - ✅ **表格布局**：使用Table、TableRow、TableCell生成药材条目表格
   - ❌ **避免直接打印**：不直接调用Printer API，统一通过PrintDialog

6. **Repository模式与三层架构**
   - ✅ **三层分离**：ViewModel → IPrescriptionRepository → BaseApiRepository → IApiService → HttpClient
   - ✅ **依赖注入**：ViewModel通过构造函数注入IPrescriptionRepository
   - ✅ **Repository返回裸类型**：直接返回PrescriptionDto、PagedResult<PrescriptionDto>
   - ✅ **BaseApiRepository基类**：继承IBaseRepository<PrescriptionDto>，自动获得CRUD方法
   - ✅ **异常传播**：Repository层不捕获异常，直接抛出让ViewModel层处理
   - ❌ **避免直接调用Server Service**：Desktop端禁止直接依赖Server Service

7. **异步优先与UI响应性**
   - ✅ **全异步方法**：所有I/O操作使用async/await
   - ✅ **IsBusy模式**：异步操作前设置IsBusy=true，操作完成后设置IsBusy=false
   - ✅ **AsyncDelegateCommand**：使用Prism的AsyncDelegateCommand支持异步命令
   - ✅ **try-finally保证**：IsBusy在finally块中设置为false
   - ✅ **Task返回类型**：异步方法返回Task或Task<T>
   - ❌ **避免同步阻塞**：不使用.Result、.Wait()等同步阻塞方法

---

## 🛠 技术栈

### Server端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时框架 |
| ASP.NET Core | 8.0 | Web API框架 |
| Entity Framework Core | 8.0.0 | ORM框架 |
| FluentValidation | 11.x | 验证框架 |
| SQL Server | 2022 | 数据库 |

### Client端技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时框架 |
| WPF | .NET 8.0 | 桌面应用框架 |
| Prism | 9.0.x | MVVM框架 |
| ObservableCollection | .NET 8.0 | 数据绑定集合 |
| WPF PrintDialog | .NET 8.0 | 打印对话框 |
| FlowDocument | .NET 8.0 | 打印文档生成 |

---

## 🚀 快速开始

### Server端集成

```csharp
// Step 1: 注册服务(Program.cs)
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPrescriptionNumberService, PrescriptionNumberService>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

// Step 2: 创建处方
var createDto = new CreatePrescriptionDto
{
    MedicalCaseId = Guid.NewGuid(),
    DosageCount = 7,
    Usage = "水煎服,每日2次",
    Discount = 1.0m,
    HerbItems = new List<PrescriptionItemDto>
    {
        new() { HerbId = herbId1, Dosage = 10m, Unit = "克", UnitPrice = 0.5m },
        new() { HerbId = herbId2, Dosage = 15m, Unit = "克", UnitPrice = 0.3m }
    }
};

var prescription = await _prescriptionService.CreateAsync(createDto);

// Step 3: 确认处方
await _prescriptionService.ConfirmAsync(prescription.Id);

// Step 4: 标记配药完成
await _prescriptionService.DispenseAsync(prescription.Id);
```

### Client端集成

```csharp
// Step 1: 注册ViewModels和Services(PrescriptionsModule.cs)
public class PrescriptionsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ViewModels
        containerRegistry.RegisterSingleton<PrescriptionManagementViewModel>();
        containerRegistry.Register<PrescriptionEditorDialogViewModel>();

        // Services
        containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();
        containerRegistry.Register<PrescriptionFlowDocumentBuilder>();

        // Repositories
        containerRegistry.Register<IPrescriptionRepository, PrescriptionRepository>();

        // Dialogs
        containerRegistry.RegisterDialog<PrescriptionEditorDialog, PrescriptionEditorDialogViewModel>();
        containerRegistry.RegisterDialog<HerbSelectionDialog, HerbSelectionDialogViewModel>();
        containerRegistry.RegisterDialog<FormulaTemplateDialog, FormulaTemplateDialogViewModel>();
    }
}

// Step 2: 打开处方编辑对话框
private void Create()
{
    var parameters = new DialogParameters
    {
        { "MedicalCaseId", CurrentMedicalCaseId }
    };

    _dialogService.ShowDialog(
        "PrescriptionEditorDialog",
        parameters,
        async result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                await RefreshAsync();
            }
        }
    );
}

// Step 3: MedicalCase集成(通过ISaveable接口)
private async Task CompleteStep2Async()
{
    if (_currentStepViewModel is IValidatable validatable)
    {
        if (!validatable.Validate()) return;
    }

    if (_currentStepViewModel != null)
    {
        await _currentStepViewModel.SaveAsync();  // 调用ISaveable接口
    }

    await _medicalCaseRepository.CompleteStep2Async(MedicalCaseId, DateTime.Now);
}
```

---

## 📚 相关文档

### 架构设计
- [Server端三层架构指南](../../architecture/server/README.md)
- [Client端MVVM架构指南](../../architecture/client/README.md)
- [Shared层共享组件](../../architecture/shared/README.md)

### 模块文档
- [Auth模块](../auth/README.md) - 用户认证与权限管理
- [Users模块](../users/README.md) - 用户资料管理
- [Patients模块](../patients/README.md) - 患者档案管理
- [Consultation模块](../consultation/README.md) - 中医诊断录入
- [Herbs模块](../herbs/README.md) - 药材管理（药材选择集成）
- [Formula模块](../formula/README.md) - 验方管理（验方模板集成）

### API文档
- [Prescriptions API](../../api/prescriptions-api.md)

### 开发指南
- [Dialog-based架构开发](../../how-to-guides/client/dialog-based-architecture.md)
- [ISaveable接口集成](../../how-to-guides/client/isaveable-integration.md)
- [打印功能开发](../../how-to-guides/client/print-functionality.md)

---

**最后更新**：2025-01-29
