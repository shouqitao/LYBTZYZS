# LYBTZYZS Desktop端UI重构 Phase 3 - UX流程优化 PRD

**文档版本**: v1.0
**创建日期**: 2025-11-04
**Epic Issue**: #1810 (待创建)
**优先级**: 🟡 P1
**预计工期**: 2-3周（80小时）
**依赖**: Phase 2完成（#1805）

---

## 📋 执行摘要

### 目标
优化核心业务流程，提升用户体验，减少操作步骤和界面跳转。

### 范围
1. **三步看诊流程Wizard化** - 将跨4个界面的流程整合为单一Wizard界面
2. **处方打印流程优化** - 快速打印、批量打印、模板选择
3. **药材/验方批量导入** - 参考患者导入向导，实现Excel导入功能

### 成功指标
- ✅ 三步流程操作步骤减少40%
- ✅ 用户完成时间减少30%
- ✅ 用户满意度提升50%
- ✅ 处方打印效率提升60%
- ✅ 批量导入成功率≥95%

---

## 1. 需求详情

### 1.1 三步看诊流程Wizard化

#### 当前问题
**现状流程**（需要跨4个界面）:
```
1. PatientSelectionView → 选择患者
2. MedicalCaseFlowView → 创建医案
3. MedicalCaseConsultationView → 填写辨证（Step 1）
4. MedicalCaseFlowView → 标记处方需求（Step 2）
5. PrescriptionEditorView → 开处方（Step 3a）或 CompletionView → 完成（Step 3b）
```

**问题**:
- 需要5次导航跳转
- 用户容易迷失当前步骤
- 无法快速回退修改
- 学习曲线陡峭

#### 解决方案 - MedicalCaseWizardView

**Wizard界面设计**:
```
┌─────────────────────────────────────────────────────────────┐
│  新建医案向导                                    [×]          │
├─────────────────────────────────────────────────────────────┤
│  ① 患者信息  →  ② 四诊辨证  →  ③ 处方决策  →  ④ 开具处方   │
│  ════════                                                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  [Step 1 内容区域]                                           │
│                                                              │
│  患者姓名: 张三                                               │
│  就诊日期: 2025-11-04                                        │
│  主诉: ____________________________________                  │
│                                                              │
│                                                              │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                              [上一步]  [下一步]  [取消]      │
└─────────────────────────────────────────────────────────────┘
```

**技术实现**:
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseWizardView.xaml.cs

public class MedicalCaseWizardViewModel : BindableBase, IDialogAware
{
    private readonly MedicalCaseFlowManager _flowManager;
    private readonly IRegionManager _regionManager;

    private int _currentStep = 1;
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            SetProperty(ref _currentStep, value);
            RaisePropertyChanged(nameof(IsStep1));
            RaisePropertyChanged(nameof(IsStep2));
            RaisePropertyChanged(nameof(IsStep3));
            RaisePropertyChanged(nameof(IsStep4));
            UpdateNavigationCommands();
        }
    }

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;

    // Step 1: 患者信息确认
    public Patient SelectedPatient { get; set; }
    public DateTime VisitDate { get; set; } = DateTime.Now;
    public string ChiefComplaint { get; set; }

    // Step 2: 四诊辨证
    public Consultation Consultation { get; set; } = new Consultation();

    // Step 3: 处方决策
    public bool NeedsPrescription { get; set; }

    // Step 4: 开具处方（可选）
    public ObservableCollection<PrescriptionItem> PrescriptionItems { get; set; }

    // Commands
    public DelegateCommand NextCommand { get; }
    public DelegateCommand PreviousCommand { get; }
    public DelegateCommand FinishCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public MedicalCaseWizardViewModel(MedicalCaseFlowManager flowManager, IRegionManager regionManager)
    {
        _flowManager = flowManager;
        _regionManager = regionManager;

        PrescriptionItems = new ObservableCollection<PrescriptionItem>();

        NextCommand = new DelegateCommand(async () => await NextStepAsync(), CanGoNext);
        PreviousCommand = new DelegateCommand(PreviousStep, CanGoPrevious);
        FinishCommand = new DelegateCommand(async () => await FinishAsync(), CanFinish);
        CancelCommand = new DelegateCommand(Cancel);
    }

    private async Task NextStepAsync()
    {
        if (CurrentStep == 1)
        {
            // 验证Step 1
            if (string.IsNullOrWhiteSpace(ChiefComplaint))
            {
                // 显示错误
                return;
            }

            // 创建医案
            var result = await _flowManager.InitializeFlowAsync(SelectedPatient.Id);
            if (!result.IsSuccess)
            {
                // 显示错误
                return;
            }

            CurrentMedicalCaseId = result.MedicalCaseId.Value;
        }
        else if (CurrentStep == 2)
        {
            // 保存辨证数据
            var result = await _flowManager.CompleteConsultationAsync(CurrentMedicalCaseId, Consultation);
            if (!result.IsSuccess)
            {
                // 显示错误
                return;
            }
        }
        else if (CurrentStep == 3)
        {
            // 标记处方需求
            var result = await _flowManager.MarkPrescriptionFlagAsync(CurrentMedicalCaseId, NeedsPrescription);
            if (!result.IsSuccess)
            {
                // 显示错误
                return;
            }

            // 如果不需要处方，直接跳到完成
            if (!NeedsPrescription)
            {
                await FinishAsync();
                return;
            }
        }

        CurrentStep++;
    }

    private void PreviousStep()
    {
        CurrentStep--;
    }

    private async Task FinishAsync()
    {
        // 如果需要处方且Step 4有数据，保存处方
        if (NeedsPrescription && PrescriptionItems.Any())
        {
            var prescription = new Prescription
            {
                MedicalCaseId = CurrentMedicalCaseId,
                Items = PrescriptionItems.ToList()
            };
            await _prescriptionRepository.CreateAsync(prescription);
        }

        // 完成医案
        await _flowManager.CompleteMedicalCaseAsync(CurrentMedicalCaseId);

        // 关闭Wizard
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
    }

    private bool CanGoNext()
    {
        return CurrentStep switch
        {
            1 => SelectedPatient != null && !string.IsNullOrWhiteSpace(ChiefComplaint),
            2 => Consultation.IsValid(),
            3 => true, // 处方决策总是可以进入下一步
            _ => false
        };
    }

    private bool CanGoPrevious()
    {
        return CurrentStep > 1;
    }

    private bool CanFinish()
    {
        if (CurrentStep == 3 && !NeedsPrescription)
            return true; // Step 3不需要处方可以直接完成

        if (CurrentStep == 4 && NeedsPrescription && PrescriptionItems.Any())
            return true; // Step 4有处方项可以完成

        return false;
    }

    private void Cancel()
    {
        // 确认对话框
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    private void UpdateNavigationCommands()
    {
        NextCommand.RaiseCanExecuteChanged();
        PreviousCommand.RaiseCanExecuteChanged();
        FinishCommand.RaiseCanExecuteChanged();
    }
}
```

#### 效果评估
- **操作步骤**: 5次导航 → 单一Wizard（减少80%导航）
- **用户完成时间**: 约5分钟 → 约3分钟（减少40%）
- **用户体验**: 流程清晰，可随时回退修改
- **学习曲线**: 陡峭 → 平缓（Wizard引导）

---

### 1.2 处方打印流程优化

#### 当前问题
**现状流程**:
```
PrescriptionManagementView → 选择处方 → PrescriptionView（预览）→ 打印
```

**问题**:
- 打印前必须预览，增加操作步骤
- 无批量打印功能，效率低
- 无打印模板选择，灵活性差

#### 解决方案

**功能1: 快速打印**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionManagementViewModel.cs

public DelegateCommand<Prescription> QuickPrintCommand { get; }

private async Task QuickPrintAsync(Prescription prescription)
{
    // 跳过预览，直接调用打印对话框
    var printService = _container.Resolve<IPrintService>();
    await printService.PrintPrescriptionAsync(prescription, useDefaultPrinter: true);
}
```

**功能2: 批量打印**
```csharp
public DelegateCommand BatchPrintCommand { get; }

private async Task BatchPrintAsync()
{
    if (SelectedPrescriptions == null || !SelectedPrescriptions.Any())
    {
        // 显示错误：请选择至少一个处方
        return;
    }

    // 显示批量打印配置对话框
    _dialogService.ShowDialog("BatchPrintConfigDialog", new DialogParameters
    {
        { "prescriptions", SelectedPrescriptions }
    }, async result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            var config = result.Parameters.GetValue<BatchPrintConfig>("config");
            var printService = _container.Resolve<IPrintService>();

            foreach (var prescription in SelectedPrescriptions)
            {
                await printService.PrintPrescriptionAsync(prescription, config);
            }
        }
    });
}
```

**功能3: 打印模板选择**
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrintService.cs

public enum PrescriptionPrintTemplate
{
    Simple,      // 简洁版（仅处方信息）
    Detailed,    // 详细版（包含患者信息、诊断）
    WithMedicalCase // 病案带处方（完整医案+处方）
}

public async Task PrintPrescriptionAsync(Prescription prescription, PrescriptionPrintTemplate template)
{
    var printDocument = template switch
    {
        PrescriptionPrintTemplate.Simple => GenerateSimpleDocument(prescription),
        PrescriptionPrintTemplate.Detailed => GenerateDetailedDocument(prescription),
        PrescriptionPrintTemplate.WithMedicalCase => GenerateWithMedicalCaseDocument(prescription),
        _ => throw new ArgumentException("Unknown template")
    };

    await PrintDocumentAsync(printDocument);
}
```

#### 效果评估
- **快速打印**: 减少1次预览步骤（节省30秒/次）
- **批量打印**: 支持一次性打印多个处方（效率提升80%）
- **模板选择**: 支持3种打印模板，灵活性提升

---

### 1.3 药材/验方批量导入

#### 当前问题
**现状**:
- 药材管理：无批量导入功能，只能手动逐个创建
- 验方管理：无批量导入功能，只能手动逐个创建

**问题**:
- 初始数据录入效率低（数百条药材数据）
- 易出错（手动输入）
- 无标准化模板

#### 解决方案

**参考实例**: PatientImportWizardView（已实现）

**实现思路**:
```
1. 创建HerbImportWizardView（参考PatientImportWizardView）
   - Step 1: 下载Excel模板
   - Step 2: 上传Excel文件
   - Step 3: 数据预览和验证
   - Step 4: 导入执行

2. 创建FormulaImportWizardView（类似）
   - 支持验方+药材组成的关联导入
```

**Excel模板设计（药材）**:
```
| 药材名称 | 拼音 | 分类 | 功效 | 价格(元/g) | 备注 |
|---------|------|------|------|-----------|------|
| 当归    | danggui | 补虚药 | 补血活血 | 0.5 | |
| 党参    | dangshen | 补虚药 | 补气 | 0.8 | |
```

**技术实现**:
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbImportWizardViewModel.cs

public class HerbImportWizardViewModel : BindableBase
{
    private readonly IHerbRepository _herbRepository;
    private readonly IExcelService _excelService;

    private int _currentStep = 1;
    public int CurrentStep { get; set; }

    public ObservableCollection<HerbImportRow> ImportData { get; set; }
    public ObservableCollection<ValidationError> ValidationErrors { get; set; }

    public DelegateCommand DownloadTemplateCommand { get; }
    public DelegateCommand UploadFileCommand { get; }
    public DelegateCommand ValidateDataCommand { get; }
    public DelegateCommand ImportCommand { get; }

    private async Task DownloadTemplateAsync()
    {
        var template = _excelService.GenerateHerbImportTemplate();
        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Excel文件|*.xlsx",
            FileName = "药材导入模板.xlsx"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            await _excelService.SaveAsync(template, saveFileDialog.FileName);
        }
    }

    private async Task UploadFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel文件|*.xlsx"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ImportData = await _excelService.ReadHerbDataAsync(openFileDialog.FileName);
            CurrentStep = 3; // 跳转到预览步骤
        }
    }

    private async Task ValidateDataAsync()
    {
        ValidationErrors.Clear();

        foreach (var row in ImportData)
        {
            var errors = await _herbRepository.ValidateAsync(row);
            if (errors.Any())
            {
                ValidationErrors.AddRange(errors);
            }
        }

        if (!ValidationErrors.Any())
        {
            CurrentStep = 4; // 跳转到导入步骤
        }
    }

    private async Task ImportAsync()
    {
        var successCount = 0;
        var failCount = 0;

        foreach (var row in ImportData)
        {
            try
            {
                var herb = new Herb
                {
                    Name = row.Name,
                    Pinyin = row.Pinyin,
                    Category = row.Category,
                    Efficacy = row.Efficacy,
                    Price = row.Price,
                    Notes = row.Notes
                };

                await _herbRepository.CreateAsync(herb);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;
                ValidationErrors.Add(new ValidationError
                {
                    Row = row.RowNumber,
                    Message = ex.Message
                });
            }
        }

        // 显示导入结果
        _dialogService.ShowDialog("ImportResultDialog", new DialogParameters
        {
            { "successCount", successCount },
            { "failCount", failCount },
            { "errors", ValidationErrors }
        }, null);
    }
}
```

#### 效果评估
- **初始数据录入**: 手动录入1小时 → Excel导入5分钟（效率提升92%）
- **数据准确性**: 手动输入错误率10% → Excel导入错误率<1%
- **标准化**: 提供官方模板，确保数据格式统一

---

## 2. 实施计划

### 2.1 Phase 3 Timeline

| 周次 | 任务 | 工作量 | 依赖 |
|------|------|--------|------|
| Week 1 | MedicalCaseWizardView实现 | 32小时 | Phase 2完成 |
| Week 2 | 处方打印流程优化（快速打印、批量打印、模板） | 24小时 | Week 1完成 |
| Week 3 | 药材/验方批量导入（2个Wizard） | 24小时 | Week 2完成 |

**总工期**: 3周（80小时）

### 2.2 风险缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| Wizard流程回退导致数据丢失 | 中 | 高 | 每步保存草稿，支持恢复 |
| Excel解析失败 | 中 | 中 | 严格验证模板格式，提供详细错误提示 |
| 批量打印性能问题 | 低 | 中 | 异步打印，显示进度条 |
| 用户习惯新流程需要时间 | 中 | 低 | 提供新旧流程切换开关（Feature Toggle） |

---

## 3. 验收标准

### 3.1 功能完整性
- [ ] MedicalCaseWizardView实现4步流程
- [ ] Wizard支持前进、后退、取消操作
- [ ] Wizard每步数据验证完整
- [ ] 快速打印功能实现
- [ ] 批量打印功能实现（支持多选）
- [ ] 打印模板选择功能实现（3种模板）
- [ ] HerbImportWizardView实现（4步流程）
- [ ] FormulaImportWizardView实现（4步流程）
- [ ] Excel模板自动生成和下载
- [ ] Excel数据解析和验证
- [ ] 批量导入错误处理和日志

### 3.2 用户体验
- [ ] 三步流程操作步骤减少≥40%
- [ ] 用户完成时间减少≥30%（通过用户测试验证）
- [ ] Wizard流程清晰，用户无迷失感
- [ ] 打印速度≤5秒/张
- [ ] 批量打印进度条实时更新
- [ ] Excel导入成功率≥95%
- [ ] 用户满意度调查得分≥4.0/5.0

### 3.3 性能指标
- [ ] Wizard界面响应时间<100ms
- [ ] 批量打印10张处方<30秒
- [ ] Excel解析1000条数据<5秒
- [ ] 批量导入1000条数据<30秒

### 3.4 测试覆盖
- [ ] Wizard ViewModel单元测试覆盖率≥80%
- [ ] 打印服务单元测试覆盖率≥85%
- [ ] Excel服务单元测试覆盖率≥90%
- [ ] 端到端测试覆盖所有用户场景

---

## 4. 相关文档

- **Phase 1 PRD**: `docs/requirements/ui-refactoring-phase1-prd.md`
- **Phase 2 PRD**: `docs/requirements/ui-refactoring-phase2-prd.md`
- **重构计划**: `docs/reports/ui-ux-refactoring-plan-2025-11-04.md`
- **业务规则**: `docs/explanation/business-rules.md` (BF-002三步流程)
- **患者导入参考**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientImportWizardView.xaml`

---

**文档状态**: ✅ 待创建GitHub Issues
**下一步**: 创建Phase 3的GitHub Issues（#1810 Epic + 3个子Issues）

**最后更新**: 2025-11-04
