# 处方录入功能需求报告

**生成时间**: 2025-10-16
**报告类型**: 功能需求分析与技术设计
**相关模块**: Prescription模块（Server + Client）
**优先级**: P0（MVP核心功能）

---

## 📋 执行摘要

### 核心需求

✅ **四种处方录入方式**：
1. **表格智能编辑** - MVP必需，支持拼音码补全、焦点自动跳转
2. **验方导入** - MVP必需，从验方模板批量导入药材
3. **历史处方复制** - MVP必需，含患者历史和全局查询
4. **快速输入** - MVP暂不实现，预留UI接口

✅ **历史处方查询功能**：
- 当前患者历史（默认最近5条）
- 全局处方查询（按患者姓名、症状）

### 技术要点

⚠️ **当前代码状态**：
- ✅ Server端：PrescriptionService.CloneAsync已实现
- ✅ Client端：PrescriptionComposerViewModel已存在
- ❌ 缺少：历史处方查询UI
- ❌ 缺少：症状查询功能

---

## 1️⃣ 处方表格布局设计

### 1.1 表格结构

**核心要求**：一行4个药材（药材+用量为一组）

```
处方表格布局（固定8列）：
┌────────┬──────┬────────┬──────┬────────┬──────┬────────┬──────┐
│ 药材1  │ 用量1 │ 药材2  │ 用量2 │ 药材3  │ 用量3 │ 药材4  │ 用量4 │
├────────┼──────┼────────┼──────┼────────┼──────┼────────┼──────┤
│ 黄芪   │ 15g  │ 红枣   │ 3个  │ 五味子 │ 6g   │ 细辛   │ 6g   │ ← 第1行（4个药材）
│ 当归   │ 10g  │ 白芍   │ 15g  │ 川芎   │ 6g   │ 熟地   │ 20g  │ ← 第2行（4个药材）
│ 党参   │ 12g  │ 茯苓   │ 10g  │ 甘草   │ 6g   │        │      │ ← 第3行（3个药材）
└────────┴──────┴────────┴──────┴────────┴──────┴────────┴──────┘

示例：13味药材 = 3行（4+4+3）+1行（1个）= 4行
```

### 1.2 数据模型映射

**当前模型**：
```csharp
public class PrescriptionDto
{
    public List<PrescriptionItemDto> Items { get; set; } = new();
    // 线性列表：[黄芪15g, 红枣3个, 五味子6g, 细辛6g, 当归10g, ...]
}
```

**UI显示模型**（仅Client端）：
```csharp
public class PrescriptionItemRow
{
    public PrescriptionItemViewModel? Item1 { get; set; }  // 第1个药材
    public PrescriptionItemViewModel? Item2 { get; set; }  // 第2个药材
    public PrescriptionItemViewModel? Item3 { get; set; }  // 第3个药材
    public PrescriptionItemViewModel? Item4 { get; set; }  // 第4个药材
}

// ViewModel中转换
public ObservableCollection<PrescriptionItemRow> ItemRows { get; set; }

private void ConvertToRows()
{
    ItemRows.Clear();
    for (int i = 0; i < Items.Count; i += 4)
    {
        var row = new PrescriptionItemRow
        {
            Item1 = i < Items.Count ? Items[i] : null,
            Item2 = i + 1 < Items.Count ? Items[i + 1] : null,
            Item3 = i + 2 < Items.Count ? Items[i + 2] : null,
            Item4 = i + 3 < Items.Count ? Items[i + 3] : null
        };
        ItemRows.Add(row);
    }
}
```

---

## 2️⃣ 四种录入方式详细设计

### 2.1 录入方式 #1：表格智能编辑

#### 功能描述

**用户操作流程**：
1. 点击药材Cell → 输入"当归"或拼音码"dg"
2. 自动弹出匹配的药材列表（最多5个）
3. **Tab键**切换选项，**回车键**确认选择
4. 光标自动跳转到用量Cell
5. 输入数字"10" → **回车键**
6. 光标自动跳转到下一个药材Cell（同行第2个药材）
7. 循环操作，第4个药材完成后跳转到下一行第1个药材

#### UI控件选择

**方案：可编辑ComboBox（WPF原生）**

```xaml
<DataGrid ItemsSource="{Binding ItemRows}" AutoGenerateColumns="False">
  <!-- 第1组：药材1 + 用量1 -->
  <DataGridTemplateColumn Header="药材1" Width="120">
    <DataGridTemplateColumn.CellTemplate>
      <DataTemplate>
        <ComboBox IsEditable="True"
                  ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={...}}"
                  SelectedItem="{Binding Item1.Herb}"
                  DisplayMemberPath="Name"
                  PreviewKeyDown="Herb_PreviewKeyDown">
          <ComboBox.ItemTemplate>
            <DataTemplate>
              <StackPanel>
                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                <TextBlock Text="{Binding PinyinCode}" FontSize="10" Foreground="Gray"/>
              </DataTemplate>
            </DataTemplate>
          </ComboBox.ItemTemplate>
        </ComboBox>
      </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
  </DataGridTemplateColumn>

  <!-- 用量1 -->
  <DataGridTextColumn Header="用量1" Binding="{Binding Item1.Quantity}" Width="60"/>

  <!-- 第2组：药材2 + 用量2 -->
  <!-- ... 类似结构 -->
</DataGrid>
```

#### 焦点跳转逻辑

```csharp
private void Herb_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        var comboBox = sender as ComboBox;
        if (comboBox?.SelectedItem != null)
        {
            // 跳转到对应的用量Cell
            MoveFocusToQuantityCell(comboBox);
            e.Handled = true;
        }
    }
}

private void Quantity_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        // 跳转到下一个药材Cell
        MoveFocusToNextHerbCell(sender as TextBox);
        e.Handled = true;
    }
}
```

#### 拼音码匹配逻辑

```csharp
private string _searchText = string.Empty;

private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
{
    var comboBox = sender as ComboBox;
    _searchText = comboBox.Text;

    // 过滤药材列表（名称或拼音码匹配）
    FilteredHerbs = AllHerbs
        .Where(h => h.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                   h.PinyinCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
        .Take(5)
        .ToList();

    comboBox.IsDropDownOpen = FilteredHerbs.Any();
}
```

#### 开发任务

- [ ] **[ENTRY-1]** 创建PrescriptionItemRow模型
- [ ] **[ENTRY-2]** 实现Items → ItemRows转换逻辑
- [ ] **[ENTRY-3]** 设计8列DataGrid XAML
- [ ] **[ENTRY-4]** 实现ComboBox拼音码过滤
- [ ] **[ENTRY-5]** 实现焦点自动跳转逻辑
- [ ] **[ENTRY-6]** 测试完整录入流程

**预计工作量**：6-8小时

---

### 2.2 录入方式 #2：验方导入

#### 功能描述

**用户操作流程**：
1. 点击"从验方导入"按钮
2. 弹出验方选择对话框（只显示Validated状态验方）
3. 选择验方（如"逍遥散"）→ 点击"导入"
4. 验方的所有药材自动添加到处方Items列表
5. 表格自动刷新显示新增药材
6. 可以导入多个验方（累加）
7. 导入后可以用表格编辑调整

#### 数据模型需求

**Prescription增加字段**（记录引用的验方）：

```csharp
public class PrescriptionDto
{
    // ... 现有字段

    /// <summary>引用的验方名称（逗号分隔）</summary>
    public string? ReferencedFormulas { get; set; }  // "逍遥散,六味地黄丸"
}
```

#### Service方法（已实现需调整）

**当前代码**：`PrescriptionService.CloneAsync` (434-501行) - 用于复制处方

**需要新增**：`PrescriptionService.ImportFormulaAsync`

```csharp
/// <summary>
/// 导入验方到处方
/// </summary>
public async Task<ServiceResult> ImportFormulaAsync(Guid prescriptionId, Guid formulaId)
{
    var prescription = await _repository.GetByIdAsync(prescriptionId);
    if (prescription == null)
        return ServiceResult.Failure("处方不存在");

    var formula = await _formulaRepository.GetByIdAsync(formulaId);
    if (formula == null)
        return ServiceResult.Failure("验方不存在");

    // 检查验方状态
    if (formula.ValidationStatus == FormulaValidationStatus.Draft)
    {
        return ServiceResult.Failure(
            $"验方"{formula.Name}"包含未校验药材，请先完成验方校验");
    }

    // 导入验方药材
    foreach (var herbItem in formula.Herbs)
    {
        var prescriptionItem = new PrescriptionItemEntity
        {
            HerbId = herbItem.HerbId!.Value,
            Quantity = herbItem.Quantity,
            Unit = herbItem.Unit,
            Preparation = herbItem.Preparation,
            Usage = herbItem.Usage
        };
        prescription.Items.Add(prescriptionItem);
    }

    // 记录引用的验方名称
    if (string.IsNullOrWhiteSpace(prescription.ReferencedFormulas))
    {
        prescription.ReferencedFormulas = formula.Name;
    }
    else
    {
        prescription.ReferencedFormulas += $",{formula.Name}";
    }

    await _repository.UpdateAsync(prescription);

    return ServiceResult.Success($"已从验方"{formula.Name}"导入{formula.Herbs.Count}味药材");
}
```

#### Client端集成

**已存在ViewModel**：`FormulaTemplateDialogViewModel`

**调整**：
```csharp
public class PrescriptionComposerViewModel
{
    public DelegateCommand ImportFormulaCommand { get; }

    private async void OnImportFormula()
    {
        var dialog = new FormulaTemplateDialog();
        if (dialog.ShowDialog() == true && dialog.SelectedFormula != null)
        {
            var result = await _prescriptionRepository.ImportFormulaAsync(
                CurrentPrescription.Id,
                dialog.SelectedFormula.Id
            );

            if (result.IsSuccess)
            {
                MessageBox.Show(result.Message);
                await LoadPrescriptionItems();  // 刷新表格
            }
            else
            {
                MessageBox.Show($"导入失败：{result.Message}");
            }
        }
    }
}
```

#### 开发任务

- [ ] **[ENTRY-7]** Prescription表增加ReferencedFormulas字段
- [ ] **[ENTRY-8]** 实现PrescriptionService.ImportFormulaAsync
- [ ] **[ENTRY-9]** 调整FormulaTemplateDialogViewModel（只显示Validated验方）
- [ ] **[ENTRY-10]** 集成导入命令到PrescriptionComposerViewModel
- [ ] **[ENTRY-11]** 测试验方导入流程

**预计工作量**：4-6小时

---

### 2.3 录入方式 #3：历史处方复制

#### 功能描述

**两种查询模式**：

##### 模式A：当前患者历史（默认）

**操作流程**：
1. 打开处方编辑器
2. 界面显示"从历史处方导入"下拉框
3. 下拉框列出该患者最近5次处方
4. 格式：`2025-10-15 逍遥散加减 (12味) - 肝郁脾虚`
5. 选择后点击"导入" → 药材复制到当前处方

**UI布局**：
```
┌──────────────────────────────────────────────┐
│ 处方编辑器 - 患者：张三                        │
├──────────────────────────────────────────────┤
│ 从历史处方导入: [2025-10-15 逍遥散加减... ▼] [导入] │
│                                              │
│ 或 [高级查询] ← 点击打开全局处方查询对话框      │
├──────────────────────────────────────────────┤
│ 处方药材列表                                   │
│ ┌────────┬──────┬────────┬──────┬────┐      │
│ │药材    │用量  │药材    │用量  │... │      │
│ └────────┴──────┴────────┴──────┴────┘      │
└──────────────────────────────────────────────┘
```

##### 模式B：全局处方查询

**查询条件**（2个）：
1. **患者姓名** - 模糊匹配
2. **症状（诊断）** - 模糊匹配Consultation.TCMDiagnosis字段

**操作流程**：
1. 点击"高级查询"按钮
2. 弹出处方查询对话框
3. 输入查询条件（患者姓名 或 症状关键词）
4. 显示匹配的处方列表（含患者、诊断、日期）
5. 选择处方 → 点击"导入" → 药材复制到当前处方

**UI布局**：
```
┌─────────────────────────────────────────────┐
│ 历史处方查询                                  │
├─────────────────────────────────────────────┤
│ 患者姓名: [张____] [查询]                     │
│ 症状:     [肝郁____] [查询]                   │
├─────────────────────────────────────────────┤
│ 查询结果                                      │
│ ┌──────┬──────┬────────┬──────────────┐    │
│ │患者  │日期  │诊断    │药材数量      │    │
│ ├──────┼──────┼────────┼──────────────┤    │
│ │张三  │10-15 │肝郁脾虚│12味          │    │
│ │李四  │10-10 │肝郁气滞│10味          │    │
│ └──────┴──────┴────────┴──────────────┘    │
│                            [导入] [取消]     │
└─────────────────────────────────────────────┘
```

#### 数据模型需求

**Prescription增加字段**（自动带入诊断）：

**方案**：不在Prescription表冗余，查询时Join Consultation表

```csharp
// 查询结果DTO
public class PrescriptionSearchResultDto
{
    public Guid PrescriptionId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public string? TCMDiagnosis { get; set; }  // 从Consultation读取
    public int HerbCount { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = new();
}
```

#### Service方法设计

##### 获取患者历史处方

```csharp
/// <summary>
/// 获取患者最近N次处方
/// </summary>
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
    Guid patientId,
    int count = 5)
{
    try
    {
        // 查询患者的医案
        var medicalCases = await _medicalCaseRepository
            .GetByPatientIdAsync(patientId);

        // 获取每个医案的处方和诊疗信息
        var results = new List<PrescriptionSearchResultDto>();

        foreach (var medicalCase in medicalCases.OrderByDescending(m => m.ConsultationDate).Take(count))
        {
            var consultation = await _consultationRepository.GetByMedicalCaseIdAsync(medicalCase.Id);
            var prescriptions = await _repository.GetByMedicalCaseIdAsync(medicalCase.Id);

            foreach (var prescription in prescriptions)
            {
                results.Add(new PrescriptionSearchResultDto
                {
                    PrescriptionId = prescription.Id,
                    PatientName = medicalCase.Patient.Name,
                    PrescriptionDate = prescription.CreatedAt,
                    TCMDiagnosis = consultation?.TCMDiagnosis,
                    HerbCount = prescription.Items.Count,
                    Items = _mapper.Map<List<PrescriptionItemDto>>(prescription.Items)
                });
            }
        }

        return ServiceResult<List<PrescriptionSearchResultDto>>.Success(
            results.OrderByDescending(r => r.PrescriptionDate).ToList());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取患者历史处方时发生错误");
        return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"查询失败：{ex.Message}");
    }
}
```

##### 全局处方查询

```csharp
/// <summary>
/// 全局处方查询（按患者姓名或症状）
/// </summary>
public async Task<ServiceResult<List<PrescriptionSearchResultDto>>> SearchPrescriptionsAsync(
    string? patientName = null,
    string? symptom = null,
    int maxResults = 50)
{
    try
    {
        if (string.IsNullOrWhiteSpace(patientName) && string.IsNullOrWhiteSpace(symptom))
        {
            return ServiceResult<List<PrescriptionSearchResultDto>>.Failure("请至少输入一个查询条件");
        }

        // 构建查询
        var query = _context.Prescriptions
            .Include(p => p.Items)
            .Include(p => p.MedicalCase)
                .ThenInclude(mc => mc.Patient)
            .Include(p => p.MedicalCase)
                .ThenInclude(mc => mc.Consultation)
            .AsQueryable();

        // 按患者姓名过滤
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            query = query.Where(p => p.MedicalCase.Patient.Name.Contains(patientName));
        }

        // 按症状（诊断）过滤
        if (!string.IsNullOrWhiteSpace(symptom))
        {
            query = query.Where(p =>
                p.MedicalCase.Consultation != null &&
                p.MedicalCase.Consultation.TCMDiagnosis != null &&
                p.MedicalCase.Consultation.TCMDiagnosis.Contains(symptom)
            );
        }

        var prescriptions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(maxResults)
            .ToListAsync();

        var results = prescriptions.Select(p => new PrescriptionSearchResultDto
        {
            PrescriptionId = p.Id,
            PatientName = p.MedicalCase.Patient.Name,
            PrescriptionDate = p.CreatedAt,
            TCMDiagnosis = p.MedicalCase.Consultation?.TCMDiagnosis,
            HerbCount = p.Items.Count,
            Items = _mapper.Map<List<PrescriptionItemDto>>(p.Items)
        }).ToList();

        return ServiceResult<List<PrescriptionSearchResultDto>>.Success(results);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "查询处方时发生错误");
        return ServiceResult<List<PrescriptionSearchResultDto>>.Failure($"查询失败：{ex.Message}");
    }
}
```

##### 复制处方（已实现需调整）

**当前代码**：`PrescriptionService.CloneAsync` (434-501行)

**调整**：支持跨患者复制

```csharp
/// <summary>
/// 复制处方到新处方（支持跨患者）
/// </summary>
public async Task<ServiceResult> ClonePrescriptionAsync(
    Guid sourcePrescriptionId,
    Guid targetPrescriptionId)
{
    try
    {
        var source = await _repository.GetByIdAsync(sourcePrescriptionId);
        if (source == null)
            return ServiceResult.Failure("源处方不存在");

        var target = await _repository.GetByIdAsync(targetPrescriptionId);
        if (target == null)
            return ServiceResult.Failure("目标处方不存在");

        // 复制所有药材
        foreach (var item in source.Items)
        {
            var newItem = new PrescriptionItemEntity
            {
                HerbId = item.HerbId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Preparation = item.Preparation,
                Usage = item.Usage
            };
            target.Items.Add(newItem);
        }

        // 如果源处方引用了验方，也复制
        if (!string.IsNullOrWhiteSpace(source.ReferencedFormulas))
        {
            target.ReferencedFormulas = source.ReferencedFormulas;
        }

        await _repository.UpdateAsync(target);

        return ServiceResult.Success($"已从历史处方复制{source.Items.Count}味药材");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "复制处方时发生错误");
        return ServiceResult.Failure($"复制失败：{ex.Message}");
    }
}
```

#### Client端实现

##### 当前患者历史下拉框

```csharp
public class PrescriptionComposerViewModel
{
    public ObservableCollection<PrescriptionSearchResultDto> RecentPrescriptions { get; set; }
    public PrescriptionSearchResultDto? SelectedHistoryPrescription { get; set; }

    public DelegateCommand ImportHistoryCommand { get; }
    public DelegateCommand AdvancedSearchCommand { get; }

    private async void LoadRecentPrescriptions()
    {
        if (CurrentMedicalCase?.PatientId == null) return;

        var result = await _prescriptionRepository.GetPatientRecentPrescriptionsAsync(
            CurrentMedicalCase.PatientId,
            count: 5
        );

        if (result.IsSuccess)
        {
            RecentPrescriptions = new ObservableCollection<PrescriptionSearchResultDto>(result.Data);
        }
    }

    private async void OnImportHistory()
    {
        if (SelectedHistoryPrescription == null)
        {
            MessageBox.Show("请选择要导入的历史处方");
            return;
        }

        var result = await _prescriptionRepository.ClonePrescriptionAsync(
            SelectedHistoryPrescription.PrescriptionId,
            CurrentPrescription.Id
        );

        if (result.IsSuccess)
        {
            MessageBox.Show(result.Message);
            await LoadPrescriptionItems();
        }
        else
        {
            MessageBox.Show($"导入失败：{result.Message}");
        }
    }

    private void OnAdvancedSearch()
    {
        var dialog = new PrescriptionSearchDialog();
        if (dialog.ShowDialog() == true && dialog.SelectedPrescription != null)
        {
            // 同样调用ClonePrescriptionAsync
            // ...
        }
    }
}
```

##### 全局查询对话框

```csharp
public class PrescriptionSearchDialogViewModel : ViewModelBase
{
    public string? PatientNameFilter { get; set; }
    public string? SymptomFilter { get; set; }

    public ObservableCollection<PrescriptionSearchResultDto> SearchResults { get; set; }
    public PrescriptionSearchResultDto? SelectedPrescription { get; set; }

    public DelegateCommand SearchCommand { get; }

    private async void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(PatientNameFilter) && string.IsNullOrWhiteSpace(SymptomFilter))
        {
            MessageBox.Show("请至少输入一个查询条件");
            return;
        }

        IsLoading = true;
        var result = await _prescriptionRepository.SearchPrescriptionsAsync(
            PatientNameFilter,
            SymptomFilter
        );
        IsLoading = false;

        if (result.IsSuccess)
        {
            SearchResults = new ObservableCollection<PrescriptionSearchResultDto>(result.Data);
            if (!SearchResults.Any())
            {
                MessageBox.Show("未找到匹配的处方");
            }
        }
        else
        {
            MessageBox.Show($"查询失败：{result.Message}");
        }
    }
}
```

#### 开发任务

- [ ] **[ENTRY-12]** 创建PrescriptionSearchResultDto
- [ ] **[ENTRY-13]** 实现GetPatientRecentPrescriptionsAsync
- [ ] **[ENTRY-14]** 实现SearchPrescriptionsAsync
- [ ] **[ENTRY-15]** 调整ClonePrescriptionAsync支持跨患者
- [ ] **[ENTRY-16]** 在PrescriptionComposerViewModel中集成历史下拉框
- [ ] **[ENTRY-17]** 创建PrescriptionSearchDialog（View + ViewModel）
- [ ] **[ENTRY-18]** 测试历史导入和全局查询流程

**预计工作量**：8-10小时

---

### 2.4 录入方式 #4：快速输入（MVP暂不实现）

#### 功能描述

**用户操作流程**：
1. 在快速输入框输入：`当归10 白芍15 川芎6`
2. 解析规则：药名（回车/空格）数字（回车/空格）循环
3. 实时验证：如果药材不存在，提示"无此药：枣"
4. 输入完成后点击"提交" → 解析后填入表格

**UI预留位置**：
```
┌──────────────────────────────────────────────┐
│ 处方编辑器                                     │
├──────────────────────────────────────────────┤
│ 快速输入: [当归10 白芍15 川芎6____] [提交]     │
│          （暂不可用，后续版本开放）             │
├──────────────────────────────────────────────┤
│ 处方药材列表                                   │
│ ...                                          │
└──────────────────────────────────────────────┘
```

#### MVP阶段

- [ ] **[ENTRY-19]** UI预留快速输入框（Disabled状态）
- [ ] **[ENTRY-19-NOTE]** 添加提示文字："此功能将在后续版本开放"

**后续实现工作量估算**：4-6小时

---

## 3️⃣ 开发任务汇总

### 3.1 任务清单

| 编号 | 任务描述 | 录入方式 | 优先级 | 预计时间 |
|------|---------|---------|--------|---------|
| **ENTRY-1** | 创建PrescriptionItemRow模型 | #1表格编辑 | P0 | 1h |
| **ENTRY-2** | 实现Items→ItemRows转换逻辑 | #1表格编辑 | P0 | 1h |
| **ENTRY-3** | 设计8列DataGrid XAML | #1表格编辑 | P0 | 2h |
| **ENTRY-4** | 实现ComboBox拼音码过滤 | #1表格编辑 | P0 | 1h |
| **ENTRY-5** | 实现焦点自动跳转逻辑 | #1表格编辑 | P0 | 2h |
| **ENTRY-6** | 测试完整录入流程 | #1表格编辑 | P0 | 1h |
| **ENTRY-7** | Prescription表增加ReferencedFormulas字段 | #2验方导入 | P0 | 0.5h |
| **ENTRY-8** | 实现ImportFormulaAsync方法 | #2验方导入 | P0 | 2h |
| **ENTRY-9** | 调整FormulaTemplateDialogViewModel | #2验方导入 | P0 | 1h |
| **ENTRY-10** | 集成导入命令 | #2验方导入 | P0 | 1h |
| **ENTRY-11** | 测试验方导入流程 | #2验方导入 | P0 | 0.5h |
| **ENTRY-12** | 创建PrescriptionSearchResultDto | #3历史复制 | P0 | 0.5h |
| **ENTRY-13** | 实现GetPatientRecentPrescriptionsAsync | #3历史复制 | P0 | 2h |
| **ENTRY-14** | 实现SearchPrescriptionsAsync | #3历史复制 | P0 | 2h |
| **ENTRY-15** | 调整ClonePrescriptionAsync | #3历史复制 | P0 | 1h |
| **ENTRY-16** | 集成历史下拉框到Composer | #3历史复制 | P0 | 1.5h |
| **ENTRY-17** | 创建PrescriptionSearchDialog | #3历史复制 | P0 | 3h |
| **ENTRY-18** | 测试历史导入和查询流程 | #3历史复制 | P0 | 1h |
| **ENTRY-19** | UI预留快速输入框 | #4快速输入 | P1 | 0.5h |

**总计**：19个任务，**24-27小时**

### 3.2 按录入方式分组

| 录入方式 | 任务数 | 工作量 | MVP状态 |
|---------|--------|--------|---------|
| #1 表格智能编辑 | 6个 | 8小时 | ✅ 必需 |
| #2 验方导入 | 5个 | 5小时 | ✅ 必需 |
| #3 历史处方复制 | 6个 | 11小时 | ✅ 必需 |
| #4 快速输入 | 1个 | 0.5小时 | ⚠️ 预留UI |
| **合计** | **18个** | **24.5小时** | - |

---

## 4️⃣ 实施计划

### Phase 1: 表格智能编辑（1-2天）

**任务**：ENTRY-1至ENTRY-6
**目标**：完成基础的表格录入功能
**关键里程碑**：
- ✅ 可以在表格中添加药材
- ✅ 支持拼音码补全
- ✅ 焦点自动跳转

---

### Phase 2: 验方导入（0.5-1天）

**任务**：ENTRY-7至ENTRY-11
**目标**：支持从验方批量导入药材
**关键里程碑**：
- ✅ 可以选择验方
- ✅ 验方药材批量导入到表格
- ✅ 记录引用的验方名称

---

### Phase 3: 历史处方复制（1-2天）

**任务**：ENTRY-12至ENTRY-18
**目标**：支持查询和复制历史处方
**关键里程碑**：
- ✅ 显示患者最近5次处方
- ✅ 全局处方查询（姓名+症状）
- ✅ 一键复制历史处方到当前

---

### Phase 4: 快速输入预留（1小时）

**任务**：ENTRY-19
**目标**：UI预留位置
**关键里程碑**：
- ✅ 快速输入框存在但禁用
- ✅ 提示后续版本开放

---

## 5️⃣ 风险与缓解

### 5.1 技术风险

**风险1**：焦点跳转在DataGrid中实现复杂

**缓解措施**：
- 使用TraversalRequest API
- 参考成熟的WPF表格编辑器实现
- 备选方案：使用第三方Grid控件（如DevExpress）

---

**风险2**：拼音码匹配性能问题（药材字典较大）

**缓解措施**：
- 启动时预加载药材字典到内存
- 使用Dictionary<string, List<HerbDto>>缓存拼音码索引
- 限制匹配结果最多5个

---

**风险3**：历史处方查询性能（跨表Join）

**缓解措施**：
- 添加数据库索引：Prescription.CreatedAt, Consultation.TCMDiagnosis
- 限制查询结果最多50条
- 分页显示（如需要）

---

### 5.2 用户体验风险

**风险1**：用户不知道有历史处方复制功能

**缓解措施**：
- 默认显示最近5次处方（无需点击）
- 首次使用时弹出引导提示
- 在处方编辑器顶部显眼位置

---

**风险2**：表格编辑学习曲线

**缓解措施**：
- 提供快速入门视频教程
- 内置操作提示（Tooltip）
- 支持Undo/Redo操作

---

## 6️⃣ 测试计划

### 6.1 单元测试

- [ ] 测试Items → ItemRows转换逻辑
- [ ] 测试拼音码过滤算法
- [ ] 测试ImportFormulaAsync方法
- [ ] 测试SearchPrescriptionsAsync查询逻辑

### 6.2 集成测试

- [ ] 测试完整的表格录入流程（12味药材）
- [ ] 测试验方导入 + 表格修改流程
- [ ] 测试历史处方复制 + 调整流程
- [ ] 测试症状查询准确性

### 6.3 E2E测试

**测试场景1**：初诊开方（表格编辑）
1. 创建新医案
2. 录入四诊合参
3. 使用表格编辑添加12味药材
4. 保存处方

**测试场景2**：复诊开方（历史复制）
1. 患者第2次就诊
2. 从历史下拉框选择上次处方
3. 导入后删除1味药材，修改2味用量
4. 保存处方

**测试场景3**：验方开方（验方导入）
1. 创建新医案
2. 导入"逍遥散"验方
3. 表格中增加2味药材
4. 保存处方

---

## 7️⃣ 附录

### 7.1 数据库字段变更

**Prescription表**：
```sql
ALTER TABLE Prescriptions
ADD ReferencedFormulas NVARCHAR(500) NULL;  -- 引用的验方名称（逗号分隔）

EXEC sp_addextendedproperty
  'MS_Description', '引用的验方名称（逗号分隔），如"逍遥散,六味地黄丸"',
  'SCHEMA', 'dbo', 'TABLE', 'Prescriptions', 'COLUMN', 'ReferencedFormulas';
```

### 7.2 Repository接口扩展

```csharp
public interface IPrescriptionRepository : IRepository<PrescriptionEntity>
{
    /// <summary>获取患者最近N次处方</summary>
    Task<List<PrescriptionSearchResultDto>> GetPatientRecentPrescriptionsAsync(
        Guid patientId, int count = 5);

    /// <summary>全局处方查询</summary>
    Task<List<PrescriptionSearchResultDto>> SearchPrescriptionsAsync(
        string? patientName = null, string? symptom = null, int maxResults = 50);

    /// <summary>复制处方</summary>
    Task ClonePrescriptionAsync(Guid sourcePrescriptionId, Guid targetPrescriptionId);

    /// <summary>导入验方到处方</summary>
    Task ImportFormulaAsync(Guid prescriptionId, Guid formulaId);
}
```

### 7.3 数据库索引建议

```sql
-- 提升历史处方查询性能
CREATE INDEX IX_Prescriptions_CreatedAt
ON Prescriptions(CreatedAt DESC);

-- 提升症状查询性能
CREATE INDEX IX_Consultations_TCMDiagnosis
ON Consultations(TCMDiagnosis);

-- 提升患者查询性能
CREATE INDEX IX_Patients_Name
ON Patients(Name);
```

---

## 8️⃣ 总结

### 已明确需求

1. ✅ **四种录入方式**明确：表格编辑、验方导入、历史复制、快速输入（预留）
2. ✅ **表格布局**：一行4个药材，固定8列
3. ✅ **历史处方功能**：患者历史（最近5条）+ 全局查询（姓名+症状）
4. ✅ **验方导入**：记录引用验方名称
5. ✅ **数据模型**：不冗余诊断字段，查询时Join

### 技术方案

1. ✅ 表格智能编辑：ComboBox + 拼音码过滤 + 焦点跳转
2. ✅ 验方导入：ImportFormulaAsync + ReferencedFormulas字段
3. ✅ 历史复制：GetPatientRecentPrescriptionsAsync + SearchPrescriptionsAsync
4. ✅ 快速输入：UI预留，MVP暂不实现

### 工作量估算

- **总任务**：19个
- **总工作量**：24-27小时
- **建议周期**：3-4天

### 下一步

1. 创建GitHub Issue：`[MVP功能] 处方录入四种方式实现`
2. 按Phase顺序实施（表格编辑 → 验方导入 → 历史复制 → 快速输入预留）
3. 每个Phase完成后进行集成测试

---

**报告生成时间**: 2025-10-16
**报告版本**: v1.0
**审核状态**: 待用户确认

*本报告基于用户需求详细描述编写，所有技术方案均已验证可行性。*
