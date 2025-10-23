# 待看诊队列功能需求讨论

> **创建时间**：2025-10-23
> **状态**：✅ 需求分析完成，Epic方案已确定
> **相关Issue**：将替代 #1568, #1569, #1570, #1571, #1572, #1573

---

## 📋 需求概述

> **📌 核心术语说明**：
> - **Consultation（诊断）**：MedicalCase的子实体，记录诊断信息（主诉、诊断结果等）
> - **Prescription（处方）**：MedicalCase的子实体，记录处方信息（药材、剂量等）
> - **MedicalCase（医案）**：聚合根，管理Consultation和Prescription的完整生命周期
> - **看诊流程**：医生接诊患者 → 创建/继续病案 → 记录诊断 → 开具处方 → 完成/暂存

### 用户描述的核心需求

**主要功能**：
1. ✅ 患者选择界面增加"待看诊列表"
2. ✅ 显示有未完成病案的患者
3. 🔮 **未来扩展**：显示已挂号患者（预留接口）
4. ✅ 医生直接选择患者开始看诊
5. ✅ 智能判断：
   - 有未完成病案 → 打开旧病案（继续看诊）
   - 无病案 → 新建病案
6. ✅ 完整的医案生命周期管理：
   - 同步创建：MedicalCase + Consultation（诊断） + Prescription（处方）
   - 同步更新：暂存时三者同步保存
   - 同步删除：MedicalCase状态变Closed时级联删除Consultation/Prescription
   - 暂存恢复：继续看诊时正确加载诊断和处方数据

### 问题场景（待解决的痛点）

**当前问题**：
1. ❌ 医生需要在患者列表中搜索，找到有未完成医案的患者
2. ❌ 无法一眼看出哪些患者有待看诊的医案
3. ❌ 继续看诊时，旧的诊断和处方数据未加载（Issue #1570）
4. ❌ 暂存后导航错误（Issue #1569）
5. ❌ 新建医案时旧医案的Consultation/Prescription未删除（Issue #1571）
6. ❌ 返回按钮导航错误（Issue #1573）

**期望效果**：
1. ✅ 待看诊列表优先显示，医生快速选择
2. ✅ 患者信息旁显示"未完成医案"标记
3. ✅ 一键继续看诊或开始新病案
4. ✅ 数据完整性保证（创建/更新/删除同步）
5. ✅ 流畅的用户体验（导航正确，数据加载完整）

---

## 🎯 Epic目标

### 业务目标

1. **提升工作效率**：待看诊队列让医生快速找到需要继续看诊的患者
2. **数据完整性**：确保医案、诊断、处方三者生命周期同步
3. **流程连续性**：医生可以随时暂停和恢复看诊，无数据丢失
4. **用户体验**：清晰的UI提示，正确的导航流程

### 技术目标

1. **UI改造**：PatientSelectionView增加双列表布局
   - 左侧：待看诊列表（优先显示）
   - 右侧：全部患者列表（搜索功能）
2. **逻辑优化**：智能判断医案状态，自动路由
3. **数据同步**：聚合根模式管理医案生命周期
4. **扩展性**：为未来挂号功能预留接口

---

## 📐 架构设计方案

### ✅ UI布局：双列表布局（已确定）

#### UI结构

```
┌─────────────────────────────────────────────────────┐
│  患者选择 - 待看诊与患者管理                         │
├─────────────────────────────────────────────────────┤
│  ┌─待看诊队列────┐  ┌─全部患者列表─────────┐      │
│  │ 🔴 张三        │  │ [搜索框]              │      │
│  │    未完成医案   │  │ ┌─────────────────┐  │      │
│  │    创建时间     │  │ │ 患者列表DataGrid │  │      │
│  │                │  │ │                 │  │      │
│  │ 🔴 李四        │  │ │                 │  │      │
│  │    未完成医案   │  │ │                 │  │      │
│  │                │  │ └─────────────────┘  │      │
│  └───────────────┘  └─────────────────────┘      │
│                                                    │
│  [继续看诊]  [开始新病案]                          │
└─────────────────────────────────────────────────────┘
```

#### 设计决策

**Q1: UI布局方案** → ✅ 双列表布局
- 左侧：待看诊队列（300px固定宽度）
- 右侧：全部患者列表（自适应宽度）
- 中间：10px分隔

**理由**：
1. ✅ 信息分层清晰，扩展性好
2. ✅ 符合医疗工作流（优先处理待诊患者）
3. ✅ 为未来挂号功能预留空间

---

## 🔮 挂号模块衔接设计（未来扩展）

### Q2: 挂号模块集成方案（默认推荐）

#### Q2.1: 显示策略 → ✅ 合并显示（推荐）

**方案**：
```
待看诊队列：
├─ 🔴 张三（未完成医案，创建于15:30）
├─ 🟡 李四（已挂号，预约16:00）
├─ 🔴 王五（未完成医案，创建于14:20）
└─ 🟡 赵六（已挂号，预约16:30）
```

**优势**：
- ✅ 统一视图，医生一眼看到所有待处理患者
- ✅ 支持灵活排序（可按时间、类型）

#### Q2.2: 排序规则 → ✅ 优先级分类 + 时间排序（推荐）

**方案**：
```
排序逻辑：
1. 未完成医案优先（Priority = High）
2. 已挂号患者其次（Priority = Normal）
3. 组内按时间排序（早的在前）
```

**理由**：
- ✅ 符合医疗业务规则（优先完成未完成医案）
- ✅ 避免患者等待时间过长

#### Q2.3: DTO设计 → ✅ 现在扩展（推荐）

**扩展后的DTO**：

```csharp
public class PendingConsultationDto
{
    // 基础信息
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public string PatientPhone { get; set; }

    // 类型标识（新增）⭐
    public PendingType Type { get; set; } // Incomplete | Appointment

    // 未完成医案信息
    public Guid? MedicalCaseId { get; set; }
    public DateTime? CaseCreatedAt { get; set; }
    public int? CurrentStep { get; set; }

    // 挂号信息（预留字段，当前为null）
    public Guid? AppointmentId { get; set; }
    public DateTime? AppointmentTime { get; set; }
    public string? AppointmentType { get; set; } // 初诊/复诊

    // 显示用（计算属性）
    public string DisplayTime => Type == PendingType.Incomplete
        ? CaseCreatedAt?.ToString("HH:mm") ?? ""
        : AppointmentTime?.ToString("HH:mm") ?? "";

    public string DisplayStatus => Type == PendingType.Incomplete
        ? "未完成"
        : "已挂号";
}

public enum PendingType
{
    Incomplete,  // 未完成医案
    Appointment  // 已挂号（预留）
}
```

**理由**：
- ✅ 当前实现只使用Incomplete类型，Appointment字段为null
- ✅ 未来挂号功能开发时，无需修改DTO结构
- ✅ 保持向前兼容，降低后期重构成本

### 数据库设计预留

**当前查询**（只支持未完成医案）：
```sql
SELECT
    MedicalCaseId,
    PatientId,
    PatientName,
    CreatedAt,
    CurrentStep
FROM MedicalCases
WHERE Status = 'Active'
ORDER BY CreatedAt DESC;
```

**未来扩展**（支持挂号，预留）：
```sql
-- 新增Appointment表
CREATE TABLE Appointments (
    Id GUID PRIMARY KEY,
    PatientId GUID NOT NULL,
    AppointmentTime DATETIME NOT NULL,
    AppointmentType VARCHAR(20), -- 初诊/复诊
    Status VARCHAR(20), -- Pending/Confirmed/Cancelled/Completed
    MedicalCaseId GUID NULL, -- 关联的医案（看诊后创建）
    CreatedAt DATETIME,
    FOREIGN KEY (PatientId) REFERENCES Patients(Id)
);

-- 合并查询（Union）
SELECT
    'Incomplete' AS Type,
    MedicalCaseId,
    PatientId,
    PatientName,
    CreatedAt AS Time,
    NULL AS AppointmentId,
    NULL AS AppointmentTime
FROM MedicalCases WHERE Status = 'Active'

UNION ALL

SELECT
    'Appointment' AS Type,
    NULL AS MedicalCaseId,
    PatientId,
    PatientName,
    AppointmentTime AS Time,
    Id AS AppointmentId,
    AppointmentTime
FROM Appointments
WHERE CAST(AppointmentTime AS DATE) = CAST(GETDATE() AS DATE)
  AND Status = 'Pending'

ORDER BY
    CASE WHEN Type = 'Incomplete' THEN 1 ELSE 2 END, -- 优先级
    Time ASC; -- 时间排序
```

---

## 🔧 技术实施方案

### Phase 1: UI改造（PatientSelectionView）- 2-3小时

#### 新增UI元素

```xaml
<Grid Grid.Row="1" Margin="20">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300"/>  <!-- 待看诊队列 -->
        <ColumnDefinition Width="10"/>   <!-- 分隔 -->
        <ColumnDefinition Width="*"/>    <!-- 全部患者 -->
    </Grid.ColumnDefinitions>

    <!-- 左侧：待看诊队列 -->
    <Border Grid.Column="0" Background="White" Padding="10" CornerRadius="5">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <TextBlock Text="待看诊队列" FontWeight="Bold" FontSize="16"
                      Foreground="#FF2C3E50" Margin="0,0,0,10"/>

            <DataGrid Grid.Row="1"
                     ItemsSource="{Binding PendingConsultations}"
                     SelectedItem="{Binding SelectedPendingPatient}"
                     AutoGenerateColumns="False"
                     IsReadOnly="True"
                     SelectionMode="Single">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="患者" Binding="{Binding PatientName}" Width="100"/>
                    <DataGridTextColumn Header="时间" Binding="{Binding CaseCreatedAt, StringFormat='{}{0:HH:mm}'}" Width="80"/>
                    <DataGridTextColumn Header="步骤" Binding="{Binding CurrentStep}" Width="60"/>
                </DataGrid.Columns>
            </DataGrid>
        </Grid>
    </Border>

    <!-- 右侧：全部患者（原有的患者列表） -->
    <Border Grid.Column="2" Background="White" Padding="10" CornerRadius="5">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- 搜索框 -->
                <RowDefinition Height="*"/>     <!-- 患者列表 -->
            </Grid.RowDefinitions>

            <!-- 搜索框（保持原有） -->
            <Grid Grid.Row="0" Margin="0,0,0,10">
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         .../>
                <Button Command="{Binding SearchCommand}" .../>
            </Grid>

            <!-- 患者列表（保持原有） -->
            <DataGrid Grid.Row="1"
                     ItemsSource="{Binding Patients}"
                     SelectedItem="{Binding SelectedPatient}"
                     .../>
        </Grid>
    </Border>
</Grid>
```

#### ViewModel新增属性

```csharp
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    // 新增：待看诊队列
    private ObservableCollection<PendingConsultationDto> _pendingConsultations = new();
    public ObservableCollection<PendingConsultationDto> PendingConsultations
    {
        get => _pendingConsultations;
        set => SetProperty(ref _pendingConsultations, value);
    }

    // 新增：选中的待看诊患者
    private PendingConsultationDto? _selectedPendingPatient;
    public PendingConsultationDto? SelectedPendingPatient
    {
        get => _selectedPendingPatient;
        set
        {
            if (SetProperty(ref _selectedPendingPatient, value))
            {
                // 清空右侧患者选择
                SelectedPatient = null;
                ContinueConsultationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // 修改：原有的SelectedPatient属性，添加互斥逻辑
    public PatientDto? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value))
            {
                // 清空左侧待诊选择
                SelectedPendingPatient = null;
                StartConsultationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // 新增命令：从待看诊队列继续看诊
    public DelegateCommand ContinueConsultationCommand { get; }
}
```

### Phase 2: 智能路由 - 2小时

#### 继续看诊逻辑

```csharp
/// <summary>
/// 从待看诊队列继续看诊
/// </summary>
private async Task ExecuteContinueConsultationAsync()
{
    if (SelectedPendingPatient == null) return;

    try
    {
        SetIsBusy(true, "正在加载医案...");

        Logger.LogInformation("继续看诊，MedicalCaseId: {MedicalCaseId}",
            SelectedPendingPatient.MedicalCaseId);

        // 查询患者信息
        var patient = await _patientRepository.GetByIdAsync(SelectedPendingPatient.PatientId);
        if (patient == null)
        {
            await ShowErrorMessageAsync("患者信息不存在");
            return;
        }

        // 导航到看诊流程，传递MedicalCaseId和CurrentPatient
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", SelectedPendingPatient.MedicalCaseId },
            { "CurrentPatient", patient }
        };

        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);

        Logger.LogInformation("已导航到看诊流程，MedicalCaseId: {MedicalCaseId}",
            SelectedPendingPatient.MedicalCaseId);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "继续看诊时发生异常");
        await ShowErrorMessageAsync($"继续看诊失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

private bool CanExecuteContinueConsultation()
{
    return SelectedPendingPatient != null && !IsBusy;
}
```

#### 新建医案逻辑（保持原有）

保持 `ExecuteStartConsultationAsync` 方法不变，继续支持：
1. 检查未完成医案
2. 询问用户"继续看诊"或"新建医案"
3. 新建医案时关闭旧医案（级联删除Consultation/Prescription）

### Phase 3: 数据同步（修复Issue #1570, #1571） - 2小时

#### 3.1 修复Issue #1570：继续看诊时加载诊断和处方数据

**MedicalCaseFlowViewModel.cs**（OnNavigatedTo方法）：

```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    base.OnNavigatedTo(navigationContext);

    // 获取MedicalCaseId参数
    if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
    {
        var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
        _currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");

        Logger.LogInformation("继续看诊，加载MedicalCase: {MedicalCaseId}", medicalCaseId);

        try
        {
            SetIsBusy(true, "正在加载医案数据...");

            // ⭐ 修复：加载MedicalCase + Consultation + Prescription
            var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase == null)
            {
                await ShowErrorMessageAsync("医案不存在");
                return;
            }

            _currentMedicalCaseId = medicalCase.Id;

            // ⭐ 修复：加载诊断数据到Step1ViewModel
            if (medicalCase.Consultation != null)
            {
                Logger.LogInformation("加载诊断数据，ConsultationId: {ConsultationId}",
                    medicalCase.Consultation.Id);

                Step1ViewModel.ChiefComplaint = medicalCase.Consultation.ChiefComplaint ?? "";
                Step1ViewModel.Diagnosis = medicalCase.Consultation.Diagnosis ?? "";
                // ... 其他诊断字段
            }

            // ⭐ 修复：加载处方数据到Step3ViewModel
            if (medicalCase.Prescription != null)
            {
                Logger.LogInformation("加载处方数据，PrescriptionId: {PrescriptionId}",
                    medicalCase.Prescription.Id);

                Step3ViewModel.LoadPrescription(medicalCase.Prescription);
            }

            Logger.LogInformation("医案数据加载完成");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载医案数据失败");
            await ShowErrorMessageAsync($"加载医案失败：{ex.Message}");
        }
        finally
        {
            SetIsBusy(false);
        }
    }
}
```

#### 3.2 修复Issue #1571：新建医案时级联删除（Server端已实现）

**验证Server端实现**（MedicalCaseRepository.cs）：

```csharp
public override async Task<MedicalCaseEntity> UpdateAsync(MedicalCaseEntity entity)
{
    var existingEntity = await _dbSet
        .Include(m => m.Consultation)
        .Include(m => m.Prescription)
        .FirstOrDefaultAsync(m => m.Id == entity.Id);

    // ✅ 已实现：检测状态变更：从Active变为Closed
    if (existingEntity.Status != MedicalCaseStatus.Closed &&
        entity.Status == MedicalCaseStatus.Closed)
    {
        _logger?.LogInformation("检测到医案状态变更为Closed，准备级联删除关联数据");

        // ✅ 已实现：删除关联的Consultation（诊断）
        if (existingEntity.Consultation != null)
        {
            _logger?.LogInformation("删除关联的Consultation，ConsultationId: {ConsultationId}",
                existingEntity.Consultation.Id);
            _context.Set<ConsultationEntity>().Remove(existingEntity.Consultation);
        }

        // ✅ 已实现：删除关联的Prescription（处方）
        if (existingEntity.Prescription != null)
        {
            _logger?.LogInformation("删除关联的Prescription，PrescriptionId: {PrescriptionId}",
                existingEntity.Prescription.Id);
            _context.Set<PrescriptionEntity>().Remove(existingEntity.Prescription);
        }
    }

    return await base.UpdateAsync(entity);
}
```

**Client端逻辑**（PatientSelectionViewModel.cs - CloseOldMedicalCaseAsync）：

```csharp
/// <summary>
/// 关闭旧医案并删除关联数据
/// Issue #1571 - Server端自动级联删除Consultation和Prescription
/// </summary>
private async Task CloseOldMedicalCaseAsync(MedicalCaseDto oldCase)
{
    try
    {
        Logger.LogInformation("开始关闭旧医案，MedicalCaseId: {MedicalCaseId}", oldCase.Id);

        // 更新医案状态为Closed（Server端会自动级联删除关联的Consultation和Prescription）
        var updateDto = new MedicalCaseUpdateDto
        {
            Id = oldCase.Id,
            PatientId = oldCase.PatientId,
            DoctorId = oldCase.DoctorId,
            Status = MedicalCaseStatus.Closed.ToString()
        };

        Logger.LogInformation("更新MedicalCase状态为Closed（Server端将级联删除Consultation和Prescription）");
        await _medicalCaseRepository.UpdateAsync(updateDto);

        Logger.LogInformation("旧医案关闭成功，MedicalCaseId: {MedicalCaseId}", oldCase.Id);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "关闭旧医案失败，MedicalCaseId: {MedicalCaseId}", oldCase.Id);
        throw;
    }
}
```

### Phase 4: 导航修复（修复Issue #1569, #1573） - 1小时

#### 4.1 修复Issue #1569：暂存后停留在当前界面

**MedicalCaseFlowViewModel.cs**（暂存命令）：

```csharp
/// <summary>
/// 暂存医案（修复Issue #1569 - 暂存后停留当前界面）
/// </summary>
private async Task ExecuteSaveDraftAsync()
{
    try
    {
        SetIsBusy(true, "正在暂存...");

        Logger.LogInformation("开始暂存医案，MedicalCaseId: {MedicalCaseId}", _currentMedicalCaseId);

        // 1. 保存MedicalCase
        await SaveMedicalCaseAsync();

        // 2. 保存Consultation（诊断）
        await SaveConsultationAsync();

        // 3. 保存Prescription（处方）
        await SavePrescriptionAsync();

        await ShowSuccessMessageAsync("暂存成功");

        Logger.LogInformation("医案暂存成功，MedicalCaseId: {MedicalCaseId}", _currentMedicalCaseId);

        // ⭐ 修复Issue #1569：暂存后不导航，停留在当前界面
        // 不执行导航操作，医生可以继续编辑或手动返回
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "暂存医案失败");
        await ShowErrorMessageAsync($"暂存失败：{ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

#### 4.2 修复Issue #1573：返回按钮导航修复（已修复）

**验证现有实现**（PatientSelectionViewModel.cs - ExecuteBackToHome）：

```csharp
/// <summary>
/// 返回主页（Issue #1573 - 修复返回按钮导航）
/// </summary>
private void ExecuteBackToHome()
{
    try
    {
        // ✅ 已修复：根据用户角色导航到正确的主页
        var homeViewName = SessionManager?.CurrentUser?.Role switch
        {
            UserRole.Admin => "AdminHomeView",
            UserRole.Doctor => "ClinicalHomeView",
            _ => "ClinicalHomeView"
        };

        Logger.LogInformation("返回主页，导航到：{HomeView}", homeViewName);
        _regionManager.RequestNavigate("ContentRegion", homeViewName);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "返回主页时发生异常");
    }
}
```

### Phase 5: Server端API - 2小时

#### 5.1 API端点定义

**IMedicalCaseApi.cs**（Client接口）：

```csharp
[Get("/api/medicalcases/pending")]
Task<ApiResponse<IEnumerable<PendingConsultationDto>>> GetPendingConsultationsAsync();
```

**MedicalCaseController.cs**（Server端）：

```csharp
/// <summary>
/// 获取待看诊队列（未完成医案）
/// </summary>
[HttpGet("pending")]
[ProducesResponseType(typeof(IEnumerable<PendingConsultationDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetPendingConsultationsAsync()
{
    try
    {
        _logger.LogInformation("获取待看诊队列");

        var pendingCases = await _medicalCaseRepository.GetPendingCasesAsync();
        var dtos = pendingCases.Select(c => new PendingConsultationDto
        {
            PatientId = c.PatientId,
            PatientName = c.PatientName,
            Type = PendingType.Incomplete,
            MedicalCaseId = c.Id,
            CaseCreatedAt = c.CreatedAt,
            CurrentStep = DetermineCurrentStep(c), // 根据Consultation/Prescription状态判断步骤

            // 未来扩展字段（当前为null）
            AppointmentId = null,
            AppointmentTime = null,
            AppointmentType = null
        }).ToList();

        _logger.LogInformation("成功获取{Count}个待看诊患者", dtos.Count);
        return Ok(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取待看诊队列失败");
        return StatusCode(500, "获取待看诊队列失败");
    }
}

/// <summary>
/// 判断当前步骤
/// </summary>
private int DetermineCurrentStep(MedicalCaseEntity medicalCase)
{
    if (medicalCase.Prescription != null) return 3; // 已到处方步骤
    if (medicalCase.Consultation != null) return 2; // 已到诊断步骤
    return 1; // 医案刚创建
}
```

#### 5.2 Repository方法

**IMedicalCaseRepository.cs**：

```csharp
Task<List<MedicalCaseEntity>> GetPendingCasesAsync();
```

**MedicalCaseRepository.cs**：

```csharp
/// <summary>
/// 获取所有未完成医案（用于待看诊队列）
/// </summary>
public async Task<List<MedicalCaseEntity>> GetPendingCasesAsync()
{
    return await GetDetailQuery() // 包含Consultation和Prescription
        .Where(m => m.Status == MedicalCaseStatus.Active)
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();
}
```

---

## ✅ 验收标准

### 功能验收

**待看诊队列**：
- [ ] 左侧显示所有有未完成医案的患者
- [ ] 按创建时间降序排列
- [ ] 显示患者姓名、创建时间、当前步骤
- [ ] 双击患者 → 打开医案继续看诊

**全部患者列表**：
- [ ] 右侧显示所有患者（原有功能）
- [ ] 搜索功能正常
- [ ] 选择患者 → 智能判断有无医案

**智能路由**：
- [ ] 从待看诊队列选择 → 直接继续看诊
- [ ] 从全部患者选择：
  - 有未完成医案 → 弹窗询问"继续看诊"或"新建医案"
  - 无医案 → 直接新建医案

**数据同步**：
- [ ] 继续看诊时正确加载Consultation（诊断）和Prescription（处方）数据
- [ ] 暂存时MedicalCase/Consultation/Prescription同步保存
- [ ] 新建医案时旧医案的Consultation/Prescription级联删除
- [ ] 数据库无冗余数据

**导航流程**：
- [ ] 暂存后停留在当前界面（不返回患者选择）
- [ ] "返回主页"按钮根据用户角色正确导航
- [ ] 完成看诊后返回待看诊队列

### 质量验收

- [ ] 编译通过：0 errors, 0 warnings
- [ ] 运行时验证：启动应用测试完整流程
- [ ] 性能：待看诊列表加载 < 200ms
- [ ] 代码规范：符合MVVM架构和三层对齐原则
- [ ] 文档更新：架构文档、开发指南同步更新

---

## 📊 实施计划

### Epic拆分（Phase划分）

**Phase 1: UI改造（2-3小时）**
- PatientSelectionView双列表布局
- PatientSelectionViewModel新增属性和命令
- 待看诊队列基本展示

**Phase 2: 智能路由（2小时）**
- 继续看诊逻辑
- 新建医案逻辑（保持原有）
- 弹窗询问UI

**Phase 3: 数据同步（2小时）**
- 继续看诊时加载Consultation/Prescription（修复Issue #1570）
- 暂存时同步保存三者
- 新建医案时级联删除（修复Issue #1571，Server端已实现）

**Phase 4: 导航修复（1小时）**
- 暂存后停留当前界面（修复Issue #1569）
- 返回按钮导航修复（修复Issue #1573，已修复）

**Phase 5: Server端API（2小时）**
- GetPendingConsultationsAsync端点
- PendingConsultationDto定义
- GetPendingCasesAsync Repository方法

**总计：9-10小时**

### 估算依据

- **UI改造**：双列表布局 + DataGrid配置 + XAML样式调整（2-3小时）
- **智能路由**：继续看诊命令 + 互斥选择逻辑（2小时）
- **数据同步**：修复加载逻辑 + 验证Server端实现（2小时）
- **导航修复**：暂存逻辑调整 + 返回按钮验证（1小时）
- **Server端API**：Repository方法 + Controller端点 + DTO扩展（2小时）

---

## 🔗 整合的现有Issues

以下Issues将被此Epic整合和关闭：

1. **Issue #1568** - 患者选择时自动检测并恢复未完成医案
   - 整合到：Phase 2 智能路由

2. **Issue #1569** - 暂存后应停留在当前界面
   - 整合到：Phase 4 导航修复

3. **Issue #1570** - 继续看诊时未加载旧的诊断和处方数据
   - 整合到：Phase 3 数据同步

4. **Issue #1571** - 新建医案时旧医案的关联数据未删除
   - 整合到：Phase 3 数据同步（Server端已实现）

5. **Issue #1572** - 讨论：取消看诊应关闭记录还是删除记录
   - 决策：关闭记录（Status=Closed），不删除（保留历史）
   - 整合到：聚合根模式设计

6. **Issue #1573** - 返回按钮导航错误
   - 整合到：Phase 4 导航修复（已修复）

---

## ❓ 设计决策总结

### ✅ 已确定的决策

| 问题 | 决策 | 理由 |
|-----|------|------|
| Q1: UI布局 | 双列表布局 | 信息分层清晰，扩展性好 |
| Q2.1: 挂号集成-显示策略 | 合并显示（推荐） | 统一视图，避免视图切换 |
| Q2.2: 挂号集成-排序规则 | 优先级分类 + 时间排序 | 符合医疗业务规则 |
| Q2.3: 挂号集成-DTO设计 | 现在扩展 | 向前兼容，降低后期重构成本 |
| Q3: 允许新建医案 | 是（带提示） | 保持灵活性，明确提示用户后果 |
| Q4: 暂存后导航 | 停留当前界面 | 符合医生工作流 |

### 📋 实施策略

1. **当前版本（Epic实施）**：
   - 实现双列表布局
   - 支持未完成医案队列（PendingType.Incomplete）
   - 预留挂号功能扩展接口（DTO字段为null）

2. **未来版本（挂号模块）**：
   - 无需修改DTO结构
   - 添加Appointment表和查询逻辑
   - PendingType.Appointment开始使用

---

## 📚 参考资料

**相关文档**：
- `docs/architecture/client/README.md` - Client端MVVM架构
- `docs/architecture/server/README.md` - Server端三层架构
- `docs/development/client/mvvm-patterns.md` - MVVM模式规范

**现有代码**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/PatientSelectionView.xaml`
- `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`

**Constitution约束**：
- MVVM架构：ViewModel不操作UI，使用Command和绑定
- 三层对齐：Client(五层) + Server(三层) + Shared(DTOs)
- 聚合根模式：MedicalCase管理Consultation/Prescription生命周期
- 编译标准：0 errors, 0 warnings
- 运行时验证：必须测试完整流程

---

**状态**：✅ 需求分析完成，Epic方案已确定
**下一步**：创建Epic Issue，开始Phase 1实施
