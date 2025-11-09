# 医案/诊断/处方三模块增强功能设计文档

**文档版本**：v1.0
**创建时间**：2025-10-24
**需求来源**：`docs/explanation/requirements/medicalcase-consultation-prescription-enhancement-requirements.md`
**讨论记录**：`docs/explanation/architecture/shared/medicalcase-consultation-prescription-enhancement-discussion.md`
**状态分析**：`docs/reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md`
**差距分析**：`docs/explanation/design/medicalcase-consultation-prescription-gap-analysis.md` - 现有代码与设计的差距及修改计划 ⭐

---

## 📋 目录

1. [架构设计总览](#1-架构设计总览)
2. [数据库设计](#2-数据库设计)
3. [Server端API设计](#3-server端api设计)
4. [Desktop端UI/UX设计](#4-desktop端uiux设计)
5. [Service层设计](#5-service层设计)
6. [状态机设计](#6-状态机设计)
7. [Phase拆分与实施计划](#7-phase拆分与实施计划)
8. [质量标准](#8-质量标准)

---

## 1. 架构设计总览

### 1.1 三层对齐架构

```
┌─────────────────────────────────────────────────────────────┐
│                    Desktop Client (WPF)                      │
├─────────────────────────────────────────────────────────────┤
│  Workstation: Consultation (看诊工作台)                      │
│  ├─ Views:                                                   │
│  │   ├─ ConsultationWorkstationView.xaml (主容器)           │
│  │   ├─ Step1ConsultationView.xaml (辩证界面)               │
│  │   ├─ Step2TreatmentView.xaml (施治界面)                  │
│  │   └─ Step3SummaryView.xaml (总结界面)                    │
│  │                                                            │
│  ├─ ViewModels:                                              │
│  │   ├─ ConsultationWorkstationViewModel (工作台VM)         │
│  │   ├─ Step1ConsultationViewModel (辩证VM)                 │
│  │   ├─ Step2TreatmentViewModel (施治VM)                    │
│  │   ├─ Step3SummaryViewModel (总结VM)                      │
│  │   └─ OtherCasesQueryViewModel (其他病案查询VM)           │
│  │                                                            │
│  └─ Components:                                              │
│      ├─ PrescriptionToggleRadioBox (处方开关RadioBox)        │
│      ├─ FloatingQueryMenu (悬浮查询菜单)                     │
│      └─ OtherCasesPopup (其他病案弹窗)                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP/REST API
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Server (ASP.NET Core)                     │
├─────────────────────────────────────────────────────────────┤
│  Application Layer:                                          │
│  ├─ Services:                                                │
│  │   ├─ MedicalCaseService (医案聚合根服务)                 │
│  │   ├─ ConsultationService (诊断服务)                      │
│  │   ├─ PrescriptionService (处方服务)                      │
│  │   └─ OtherCasesQueryService (其他病案查询服务)           │
│  │                                                            │
│  ├─ DTOs:                                                    │
│  │   ├─ ConsultationStepDto (诊断步骤DTO)                   │
│  │   ├─ PrescriptionToggleDto (处方开关DTO)                 │
│  │   ├─ OtherCasesQueryDto (其他病案查询DTO)                │
│  │   └─ CompletionStatusDto (完成状态DTO)                   │
│  │                                                            │
│  └─ Controllers:                                             │
│      ├─ ConsultationsController (新增端点)                   │
│      └─ PrescriptionsController (新增端点)                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ EF Core
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Database (SQL Server)                     │
├─────────────────────────────────────────────────────────────┤
│  Tables:                                                     │
│  ├─ MedicalCases (医案表 - 聚合根)                          │
│  ├─ Consultations (诊断表 - 新增字段)                       │
│  │   └─ + Step1CompletedAt, Step2CompletedAt, ...          │
│  ├─ Prescriptions (处方表 - 新增字段)                       │
│  │   └─ + IsActive (软删除标记)                             │
│  └─ ConsultationPrescriptions (诊断-处方关联表)             │
│      └─ + DeletionType (删除类型：Soft/Physical)            │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 核心设计原则

1. **DDD聚合根原则**：MedicalCase作为聚合根，Consultation/Prescription作为实体
2. **1:1:1关系约束**：MedicalCase : Consultation : Prescription = 1:1:1
3. **三层对齐**：Desktop-Server-Database三层严格对应
4. **状态机驱动**：Step1/Step2/Step3完成状态通过时间戳管理
5. **软删除优先**：提供用户选择，默认软删除（IsActive=false）

---

## 2. 数据库设计

### 2.1 Consultations表（新增字段）

```sql
-- 现有字段保持不变
ALTER TABLE Consultations ADD COLUMN Step1CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD COLUMN Step2CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD COLUMN Step3CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD COLUMN PrescriptionEnabled BIT NOT NULL DEFAULT 1;

-- 索引优化
CREATE INDEX IX_Consultations_CompletionStatus 
ON Consultations(Step1CompletedAt, Step2CompletedAt, Step3CompletedAt);
```

**字段说明**：
- `Step1CompletedAt`：辩证完成时间（NULL=未完成）
- `Step2CompletedAt`：施治完成时间（NULL=未完成）
- `Step3CompletedAt`：总结完成时间（NULL=未完成，代表整个诊断流程完成）
- `PrescriptionEnabled`：处方开关（true=开处方，false=不开处方，默认true）

### 2.2 Prescriptions表（新增字段）

```sql
ALTER TABLE Prescriptions ADD COLUMN IsActive BIT NOT NULL DEFAULT 1;
ALTER TABLE Prescriptions ADD COLUMN DeletedAt DATETIME2 NULL;
ALTER TABLE Prescriptions ADD COLUMN DeletionType NVARCHAR(20) NULL; -- 'Soft' | 'Physical'

-- 索引优化
CREATE INDEX IX_Prescriptions_IsActive ON Prescriptions(IsActive);
```

**字段说明**：
- `IsActive`：软删除标记（true=有效，false=已删除）
- `DeletedAt`：删除时间戳
- `DeletionType`：删除类型（'Soft'=软删除，'Physical'=物理删除前标记）

### 2.3 新增表：ConsultationPrescriptions（关联表）

> **注意**：REQ-006（三表共享主键）为长期Epic，本期暂用关联表方案

```sql
CREATE TABLE ConsultationPrescriptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ConsultationId INT NOT NULL,
    PrescriptionId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT FK_ConsultationPrescriptions_Consultations 
        FOREIGN KEY (ConsultationId) REFERENCES Consultations(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ConsultationPrescriptions_Prescriptions 
        FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ConsultationPrescription 
        UNIQUE (ConsultationId, PrescriptionId)
);

CREATE INDEX IX_ConsultationPrescriptions_ConsultationId 
ON ConsultationPrescriptions(ConsultationId);
```

**约束说明**：
- `UQ_ConsultationPrescription`：保证1:1关系（一个诊断最多一个有效处方）
- `FK_*_CASCADE`：级联删除（医案删除时自动清理关联）

### 2.4 数据迁移脚本

```sql
-- Migration_AddConsultationStepTracking.sql
BEGIN TRANSACTION;

-- 1. 为现有Consultations添加新字段
ALTER TABLE Consultations ADD Step1CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD Step2CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD Step3CompletedAt DATETIME2 NULL;
ALTER TABLE Consultations ADD PrescriptionEnabled BIT NOT NULL DEFAULT 1;

-- 2. 为现有Prescriptions添加软删除字段
ALTER TABLE Prescriptions ADD IsActive BIT NOT NULL DEFAULT 1;
ALTER TABLE Prescriptions ADD DeletedAt DATETIME2 NULL;
ALTER TABLE Prescriptions ADD DeletionType NVARCHAR(20) NULL;

-- 3. 创建关联表
CREATE TABLE ConsultationPrescriptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ConsultationId INT NOT NULL,
    PrescriptionId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_ConsultationPrescriptions_Consultations 
        FOREIGN KEY (ConsultationId) REFERENCES Consultations(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ConsultationPrescriptions_Prescriptions 
        FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ConsultationPrescription 
        UNIQUE (ConsultationId, PrescriptionId)
);

-- 4. 迁移现有Consultation-Prescription关系（假设现有通过MedicalCaseId关联）
INSERT INTO ConsultationPrescriptions (ConsultationId, PrescriptionId, CreatedAt)
SELECT c.Id, p.Id, p.CreatedAt
FROM Consultations c
INNER JOIN Prescriptions p ON c.MedicalCaseId = p.MedicalCaseId;

-- 5. 创建索引
CREATE INDEX IX_Consultations_CompletionStatus 
ON Consultations(Step1CompletedAt, Step2CompletedAt, Step3CompletedAt);

CREATE INDEX IX_Prescriptions_IsActive ON Prescriptions(IsActive);

CREATE INDEX IX_ConsultationPrescriptions_ConsultationId 
ON ConsultationPrescriptions(ConsultationId);

COMMIT TRANSACTION;
```

---

## 3. Server端API设计

### 3.1 ConsultationsController（新增端点）

#### 3.1.1 完成辩证（Step 1）

```csharp
/// <summary>
/// 完成辩证步骤（Step 1）
/// </summary>
/// <remarks>
/// 验证逻辑：
/// - PrescriptionEnabled=true + 处方为空 → 返回400错误
/// - PrescriptionEnabled=true + 处方不为空 → 标记Step1完成
/// - PrescriptionEnabled=false → 标记Step1完成，直接允许进入Step3
/// </remarks>
[HttpPost("consultations/{id}/complete-step1")]
[ProducesResponseType(typeof(ConsultationStepDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ConsultationStepDto>> CompleteStep1(
    int id, 
    [FromBody] CompleteStep1Request request)
{
    var consultation = await _consultationService.GetByIdAsync(id);
    if (consultation == null)
        return NotFound();

    // 验证逻辑
    if (request.PrescriptionEnabled)
    {
        var prescription = await _prescriptionService.GetActiveByConsultationIdAsync(id);
        if (prescription == null || !prescription.Items.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "处方为空",
                Detail = "处方为空，如果不需要开处方请关闭处方按钮",
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    // 更新完成状态
    consultation.PrescriptionEnabled = request.PrescriptionEnabled;
    consultation.Step1CompletedAt = DateTime.UtcNow;
    await _consultationService.UpdateAsync(consultation);

    return Ok(_mapper.Map<ConsultationStepDto>(consultation));
}

// DTO定义
public class CompleteStep1Request
{
    public bool PrescriptionEnabled { get; set; } = true;
}

public class ConsultationStepDto
{
    public int Id { get; set; }
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public DateTime? Step3CompletedAt { get; set; }
    public bool PrescriptionEnabled { get; set; }
    public string CurrentStep { get; set; } // "Step1" | "Step2" | "Step3" | "Completed"
}
```

#### 3.1.2 重置步骤状态

```csharp
/// <summary>
/// 重置步骤完成状态（用于Step 3返回编辑）
/// </summary>
[HttpPost("consultations/{id}/reset-steps")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> ResetSteps(
    int id, 
    [FromBody] ResetStepsRequest request)
{
    var consultation = await _consultationService.GetByIdAsync(id);
    if (consultation == null)
        return NotFound();

    // 根据目标步骤重置状态
    switch (request.TargetStep)
    {
        case "Step1":
            consultation.Step1CompletedAt = null;
            consultation.Step2CompletedAt = null;
            consultation.Step3CompletedAt = null;
            break;
        case "Step2":
            consultation.Step2CompletedAt = null;
            consultation.Step3CompletedAt = null;
            break;
        default:
            return BadRequest("Invalid target step");
    }

    await _consultationService.UpdateAsync(consultation);
    return NoContent();
}

public class ResetStepsRequest
{
    public string TargetStep { get; set; } // "Step1" | "Step2"
}
```

#### 3.1.3 查询其他病案

```csharp
/// <summary>
/// 查询其他患者的病案（用于辩证/施治阶段参考）
/// </summary>
[HttpGet("consultations/other-cases")]
[ProducesResponseType(typeof(PagedResult<OtherCaseDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<PagedResult<OtherCaseDto>>> QueryOtherCases(
    [FromQuery] OtherCasesQueryRequest request)
{
    var result = await _otherCasesQueryService.QueryAsync(request);
    return Ok(result);
}

public class OtherCasesQueryRequest
{
    public string? PatientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DiagnosisKeyword { get; set; } // 辩证结果关键词（模糊匹配）
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class OtherCaseDto
{
    public int MedicalCaseId { get; set; }
    public string PatientName { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime VisitDate { get; set; }
    public string ChiefComplaint { get; set; }
    public string DiagnosisResult { get; set; }
    public bool HasPrescription { get; set; }
}
```

### 3.2 PrescriptionsController（新增端点）

#### 3.2.1 删除处方（软删除/物理删除）

```csharp
/// <summary>
/// 删除处方（支持软删除和物理删除）
/// </summary>
[HttpDelete("prescriptions/{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> DeletePrescription(
    int id, 
    [FromQuery] bool physicalDelete = false)
{
    var prescription = await _prescriptionService.GetByIdAsync(id);
    if (prescription == null)
        return NotFound();

    if (physicalDelete)
    {
        // 物理删除
        await _prescriptionService.PhysicalDeleteAsync(id);
    }
    else
    {
        // 软删除
        prescription.IsActive = false;
        prescription.DeletedAt = DateTime.UtcNow;
        prescription.DeletionType = "Soft";
        await _prescriptionService.UpdateAsync(prescription);
    }

    return NoContent();
}
```

#### 3.2.2 导入验方（重复药材检测）

```csharp
/// <summary>
/// 从验方导入药材（自动检测重复并取大值）
/// </summary>
[HttpPost("prescriptions/{id}/import-from-formula")]
[ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
public async Task<ActionResult<ImportResult>> ImportFromFormula(
    int id, 
    [FromBody] ImportFormulaRequest request)
{
    var prescription = await _prescriptionService.GetByIdAsync(id);
    if (prescription == null)
        return NotFound();

    var result = await _prescriptionService.ImportFromFormulaAsync(
        id, 
        request.FormulaId, 
        detectDuplicates: true);

    return Ok(result);
}

public class ImportResult
{
    public int ImportedCount { get; set; }
    public List<DuplicateHerbDto> Duplicates { get; set; } = new();
}

public class DuplicateHerbDto
{
    public string HerbName { get; set; }
    public decimal ExistingDosage { get; set; }
    public decimal ImportedDosage { get; set; }
    public decimal FinalDosage { get; set; } // Max(ExistingDosage, ImportedDosage)
}
```

---

## 4. Desktop端UI/UX设计

### 4.1 看诊工作台主界面（ConsultationWorkstationView.xaml）

```xml
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationWorkstationView"
             xmlns:prism="http://prismlibrary.com/">
    <Grid>
        <!-- 顶部步骤导航 -->
        <StackPanel Orientation="Horizontal" DockPanel.Dock="Top" Margin="10">
            <RadioButton Content="辩证" 
                         IsChecked="{Binding IsStep1Active}"
                         Command="{Binding NavigateToStep1Command}"
                         Style="{StaticResource StepNavigationRadioButtonStyle}"/>
            <RadioButton Content="施治" 
                         IsChecked="{Binding IsStep2Active}"
                         Command="{Binding NavigateToStep2Command}"
                         IsEnabled="{Binding IsStep1Completed}"
                         Style="{StaticResource StepNavigationRadioButtonStyle}"/>
            <RadioButton Content="总结" 
                         IsChecked="{Binding IsStep3Active}"
                         Command="{Binding NavigateToStep3Command}"
                         IsEnabled="{Binding CanEnterStep3}"
                         Style="{StaticResource StepNavigationRadioButtonStyle}"/>
        </StackPanel>

        <!-- 内容区域（动态切换View） -->
        <ContentControl prism:RegionManager.RegionName="ConsultationStepRegion" />
    </Grid>
</UserControl>
```

### 4.2 Step 1 辩证界面（Step1ConsultationView.xaml）

```xml
<UserControl x:Class="LYBT.Desktop.Consultation.Views.Step1ConsultationView">
    <DockPanel>
        <!-- 右下角悬浮查询菜单 -->
        <Button DockPanel.Dock="Bottom" 
                HorizontalAlignment="Right" 
                VerticalAlignment="Bottom"
                Margin="0,0,20,20"
                Command="{Binding ShowOtherCasesQueryCommand}"
                ToolTip="查询其他病案"
                Style="{StaticResource FloatingActionButtonStyle}">
            <Path Data="{StaticResource SearchIcon}" Fill="White"/>
        </Button>

        <ScrollViewer>
            <StackPanel Margin="20">
                <!-- 四诊信息 -->
                <GroupBox Header="四诊合参">
                    <StackPanel>
                        <TextBox Text="{Binding Observation}" PlaceholderText="望诊..." Height="80"/>
                        <TextBox Text="{Binding Auscultation}" PlaceholderText="闻诊..." Height="80"/>
                        <TextBox Text="{Binding Inquiry}" PlaceholderText="问诊..." Height="80"/>
                        <TextBox Text="{Binding Palpation}" PlaceholderText="切诊..." Height="80"/>
                    </StackPanel>
                </GroupBox>

                <!-- 主诉 -->
                <GroupBox Header="主诉">
                    <TextBox Text="{Binding ChiefComplaint}" Height="60"/>
                </GroupBox>

                <!-- 辩证结果 -->
                <GroupBox Header="辩证结果">
                    <TextBox Text="{Binding DiagnosisResult}" Height="100"/>
                </GroupBox>

                <!-- 处方开关（关键设计） -->
                <GroupBox Header="处方设置" Margin="0,20,0,0">
                    <StackPanel>
                        <RadioButton Content="开处方" 
                                     IsChecked="{Binding PrescriptionEnabled, Mode=TwoWay}"
                                     GroupName="PrescriptionToggle"
                                     Margin="0,5"/>
                        <RadioButton Content="不开处方" 
                                     IsChecked="{Binding PrescriptionDisabled, Mode=TwoWay}"
                                     GroupName="PrescriptionToggle"
                                     Margin="0,5"/>
                    </StackPanel>
                </GroupBox>

                <!-- 底部按钮 -->
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,20,0,0">
                    <Button Content="暂存" 
                            Command="{Binding SaveDraftCommand}"
                            Margin="0,0,10,0"/>
                    <Button Content="完成辩证" 
                            Command="{Binding CompleteStep1Command}"
                            Style="{StaticResource PrimaryButtonStyle}"/>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

### 4.3 Step 2 施治界面（Step2TreatmentView.xaml）

```xml
<UserControl x:Class="LYBT.Desktop.Consultation.Views.Step2TreatmentView">
    <DockPanel>
        <!-- 右下角悬浮查询菜单 -->
        <Button DockPanel.Dock="Bottom" 
                HorizontalAlignment="Right" 
                VerticalAlignment="Bottom"
                Margin="0,0,20,20"
                Command="{Binding ShowOtherCasesQueryCommand}"
                Style="{StaticResource FloatingActionButtonStyle}">
            <Path Data="{StaticResource SearchIcon}" Fill="White"/>
        </Button>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="3*"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧：治法方案 -->
            <GroupBox Header="治法方案" Grid.Column="0" Margin="10">
                <StackPanel>
                    <TextBox Text="{Binding TreatmentPrinciple}" 
                             PlaceholderText="治则..." 
                             Height="100"/>
                    <TextBox Text="{Binding TreatmentMethod}" 
                             PlaceholderText="治法..." 
                             Height="100" 
                             Margin="0,10,0,0"/>
                </StackPanel>
            </GroupBox>

            <!-- 右侧：处方编辑区 -->
            <GroupBox Header="处方" Grid.Column="1" Margin="10">
                <DockPanel>
                    <!-- 工具栏 -->
                    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,10">
                        <Button Content="从验方导入" Command="{Binding ImportFromFormulaCommand}"/>
                        <Button Content="从历史导入" Command="{Binding ImportFromHistoryCommand}" Margin="10,0,0,0"/>
                        <Button Content="删除处方" 
                                Command="{Binding DeletePrescriptionCommand}" 
                                Margin="10,0,0,0"
                                Foreground="Red"/>
                    </StackPanel>

                    <!-- 处方明细 -->
                    <DataGrid ItemsSource="{Binding PrescriptionItems}" 
                              AutoGenerateColumns="False">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="药材" Binding="{Binding HerbName}" Width="*"/>
                            <DataGridTextColumn Header="剂量(g)" Binding="{Binding Dosage}" Width="80"/>
                            <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60"/>
                            <DataGridTemplateColumn Width="50">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <Button Content="删除" 
                                                Command="{Binding DataContext.RemoveItemCommand, 
                                                         RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                CommandParameter="{Binding}"/>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                        </DataGrid.Columns>
                    </DataGrid>
                </DockPanel>
            </GroupBox>
        </Grid>
    </DockPanel>
</UserControl>
```

### 4.4 其他病案查询弹窗（OtherCasesPopup.xaml）

```xml
<Window x:Class="LYBT.Desktop.Consultation.Views.OtherCasesPopup"
        Title="查询其他病案"
        Width="800" Height="600">
    <DockPanel>
        <!-- 查询条件 -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="10">
            <TextBox Text="{Binding PatientNameFilter, UpdateSourceTrigger=PropertyChanged}" 
                     PlaceholderText="患者姓名" 
                     Width="150" Margin="0,0,10,0"/>
            <TextBox Text="{Binding PhoneNumberFilter, UpdateSourceTrigger=PropertyChanged}" 
                     PlaceholderText="电话" 
                     Width="150" Margin="0,0,10,0"/>
            <TextBox Text="{Binding DiagnosisKeywordFilter, UpdateSourceTrigger=PropertyChanged}" 
                     PlaceholderText="辩证结果关键词" 
                     Width="200" Margin="0,0,10,0"/>
            <Button Content="查询" Command="{Binding SearchCommand}"/>
        </StackPanel>

        <!-- 查询结果列表 -->
        <DataGrid ItemsSource="{Binding SearchResults}" 
                  SelectedItem="{Binding SelectedCase}"
                  AutoGenerateColumns="False"
                  Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="患者姓名" Binding="{Binding PatientName}" Width="100"/>
                <DataGridTextColumn Header="电话" Binding="{Binding PhoneNumber}" Width="120"/>
                <DataGridTextColumn Header="就诊日期" Binding="{Binding VisitDate, StringFormat=yyyy-MM-dd}" Width="100"/>
                <DataGridTextColumn Header="主诉" Binding="{Binding ChiefComplaint}" Width="*"/>
                <DataGridTextColumn Header="辩证结果" Binding="{Binding DiagnosisResult}" Width="*"/>
                <DataGridCheckBoxColumn Header="有处方" Binding="{Binding HasPrescription}" Width="80"/>
                <DataGridTemplateColumn Header="操作" Width="150">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="查看详情" 
                                        Command="{Binding DataContext.ViewDetailsCommand, 
                                                 RelativeSource={RelativeSource AncestorType=Window}}"
                                        CommandParameter="{Binding}"/>
                                <Button Content="导入处方" 
                                        Command="{Binding DataContext.ImportPrescriptionCommand, 
                                                 RelativeSource={RelativeSource AncestorType=Window}}"
                                        CommandParameter="{Binding}"
                                        IsEnabled="{Binding HasPrescription}"
                                        Margin="5,0,0,0"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </DockPanel>
</Window>
```

### 4.5 处方删除确认对话框

```xml
<Window x:Class="LYBT.Desktop.Consultation.Views.PrescriptionDeletionDialog"
        Title="删除处方"
        Width="400" Height="200">
    <StackPanel Margin="20">
        <TextBlock Text="请选择删除方式：" FontSize="14" Margin="0,0,0,20"/>
        
        <RadioButton Content="软删除（保留数据，标记为已删除）" 
                     IsChecked="{Binding IsSoftDelete}"
                     GroupName="DeletionType"
                     Margin="0,5"/>
        
        <RadioButton Content="物理删除（永久删除，无法恢复）" 
                     IsChecked="{Binding IsPhysicalDelete}"
                     GroupName="DeletionType"
                     Margin="0,5"/>
        
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,30,0,0">
            <Button Content="取消" 
                    Command="{Binding CancelCommand}" 
                    Width="80" 
                    Margin="0,0,10,0"/>
            <Button Content="确认删除" 
                    Command="{Binding ConfirmCommand}" 
                    Width="80"
                    Style="{StaticResource DangerButtonStyle}"/>
        </StackPanel>
    </StackPanel>
</Window>
```

---

## 5. Service层设计

### 5.1 ConsultationService（新增方法）

```csharp
public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPrescriptionService _prescriptionService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// 完成Step 1（辩证）
    /// </summary>
    public async Task<ServiceResult<Consultation>> CompleteStep1Async(
        int consultationId, 
        bool prescriptionEnabled)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId);
        if (consultation == null)
            return ServiceResult<Consultation>.NotFound("诊断记录不存在");

        // 验证处方
        if (prescriptionEnabled)
        {
            var prescription = await _prescriptionService
                .GetActiveByConsultationIdAsync(consultationId);
            
            if (prescription == null || !prescription.Items.Any())
            {
                return ServiceResult<Consultation>.ValidationError(
                    "处方为空，如果不需要开处方请关闭处方按钮");
            }
        }

        // 更新状态
        consultation.PrescriptionEnabled = prescriptionEnabled;
        consultation.Step1CompletedAt = DateTime.UtcNow;
        
        await _unitOfWork.CommitAsync();
        
        return ServiceResult<Consultation>.Success(consultation);
    }

    /// <summary>
    /// 重置步骤状态（用于Step 3返回编辑）
    /// </summary>
    public async Task<ServiceResult> ResetStepsAsync(
        int consultationId, 
        string targetStep)
    {
        var consultation = await _consultationRepository.GetByIdAsync(consultationId);
        if (consultation == null)
            return ServiceResult.NotFound("诊断记录不存在");

        switch (targetStep)
        {
            case "Step1":
                consultation.Step1CompletedAt = null;
                consultation.Step2CompletedAt = null;
                consultation.Step3CompletedAt = null;
                break;
            case "Step2":
                consultation.Step2CompletedAt = null;
                consultation.Step3CompletedAt = null;
                break;
            default:
                return ServiceResult.ValidationError("无效的目标步骤");
        }

        await _unitOfWork.CommitAsync();
        return ServiceResult.Success();
    }

    /// <summary>
    /// 判断是否可以进入Step 3
    /// </summary>
    public bool CanEnterStep3(Consultation consultation)
    {
        // 情况1：不开处方 → Step 1完成即可进入Step 3
        if (!consultation.PrescriptionEnabled && consultation.Step1CompletedAt.HasValue)
            return true;

        // 情况2：开处方 → 必须Step 2完成
        if (consultation.PrescriptionEnabled && consultation.Step2CompletedAt.HasValue)
            return true;

        return false;
    }
}
```

### 5.2 PrescriptionService（新增方法）

```csharp
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IFormulaRepository _formulaRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// 从验方导入药材（自动检测重复并取大值）
    /// </summary>
    public async Task<ImportResult> ImportFromFormulaAsync(
        int prescriptionId, 
        int formulaId, 
        bool detectDuplicates = true)
    {
        var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
        var formula = await _formulaRepository.GetByIdAsync(formulaId);

        if (prescription == null || formula == null)
            throw new InvalidOperationException("处方或验方不存在");

        var result = new ImportResult();

        foreach (var formulaItem in formula.Items)
        {
            var existingItem = prescription.Items
                .FirstOrDefault(i => i.HerbName == formulaItem.HerbName);

            if (existingItem != null && detectDuplicates)
            {
                // 检测到重复，记录并取大值
                var finalDosage = Math.Max(existingItem.Dosage, formulaItem.Dosage);
                
                result.Duplicates.Add(new DuplicateHerbDto
                {
                    HerbName = formulaItem.HerbName,
                    ExistingDosage = existingItem.Dosage,
                    ImportedDosage = formulaItem.Dosage,
                    FinalDosage = finalDosage
                });

                existingItem.Dosage = finalDosage;
            }
            else
            {
                // 新增药材
                prescription.Items.Add(new PrescriptionItem
                {
                    HerbName = formulaItem.HerbName,
                    Dosage = formulaItem.Dosage,
                    Unit = formulaItem.Unit
                });
                result.ImportedCount++;
            }
        }

        await _unitOfWork.CommitAsync();
        return result;
    }

    /// <summary>
    /// 删除处方（支持软删除和物理删除）
    /// </summary>
    public async Task<ServiceResult> DeletePrescriptionAsync(
        int prescriptionId, 
        bool physicalDelete)
    {
        var prescription = await _prescriptionRepository.GetByIdAsync(prescriptionId);
        if (prescription == null)
            return ServiceResult.NotFound("处方不存在");

        if (physicalDelete)
        {
            // 物理删除
            _prescriptionRepository.Remove(prescription);
        }
        else
        {
            // 软删除
            prescription.IsActive = false;
            prescription.DeletedAt = DateTime.UtcNow;
            prescription.DeletionType = "Soft";
        }

        await _unitOfWork.CommitAsync();
        return ServiceResult.Success();
    }
}
```

### 5.3 OtherCasesQueryService（新增服务）

```csharp
public class OtherCasesQueryService : IOtherCasesQueryService
{
    private readonly IConsultationRepository _consultationRepository;

    public async Task<PagedResult<OtherCaseDto>> QueryAsync(
        OtherCasesQueryRequest request)
    {
        var query = _consultationRepository.Query()
            .Include(c => c.MedicalCase)
                .ThenInclude(mc => mc.Patient)
            .Include(c => c.Prescription)
            .Where(c => c.Step3CompletedAt.HasValue); // 只查询已完成的病案

        // 过滤条件
        if (!string.IsNullOrWhiteSpace(request.PatientName))
        {
            query = query.Where(c => c.MedicalCase.Patient.Name.Contains(request.PatientName));
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            query = query.Where(c => c.MedicalCase.Patient.PhoneNumber.Contains(request.PhoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(request.DiagnosisKeyword))
        {
            query = query.Where(c => c.DiagnosisResult.Contains(request.DiagnosisKeyword));
        }

        // 分页
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.MedicalCase.VisitDate)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new OtherCaseDto
            {
                MedicalCaseId = c.MedicalCaseId,
                PatientName = c.MedicalCase.Patient.Name,
                PhoneNumber = c.MedicalCase.Patient.PhoneNumber,
                VisitDate = c.MedicalCase.VisitDate,
                ChiefComplaint = c.ChiefComplaint,
                DiagnosisResult = c.DiagnosisResult,
                HasPrescription = c.Prescription != null && c.Prescription.IsActive
            })
            .ToListAsync();

        return new PagedResult<OtherCaseDto>(items, totalCount, request.PageIndex, request.PageSize);
    }
}
```

---

## 6. 状态机设计

### 6.1 诊断流程状态转换图

```
┌─────────────────────────────────────────────────────────────┐
│                    诊断流程状态机                            │
└─────────────────────────────────────────────────────────────┘

状态定义（通过时间戳判断）：
  - Step1Pending: Step1CompletedAt == NULL
  - Step1Completed: Step1CompletedAt != NULL && Step2CompletedAt == NULL
  - Step2Completed: Step2CompletedAt != NULL && Step3CompletedAt == NULL
  - Completed: Step3CompletedAt != NULL

┌──────────────┐
│ Step1Pending │ (初始状态)
└──────┬───────┘
       │
       │ [用户点击"完成辩证"]
       │ ↓ 验证：
       │   ├─ PrescriptionEnabled=true + 处方为空 → 报错，停留
       │   ├─ PrescriptionEnabled=true + 处方不为空 → 继续
       │   └─ PrescriptionEnabled=false → 继续
       ▼
┌────────────────┐
│ Step1Completed │
└────┬────┬──────┘
     │    │
     │    └─── [PrescriptionEnabled=false] ────┐
     │                                         │
     │ [PrescriptionEnabled=true]              │
     │ ↓ 进入Step 2编辑                        │
     │                                         │
     ▼                                         │
┌────────────────┐                            │
│ Step2Completed │                            │
└────────┬───────┘                            │
         │                                     │
         └─────────────┬─────────────────────┘
                       │
                       │ [进入Step 3]
                       ▼
                  ┌───────────┐
                  │ Completed │ (最终状态)
                  └─────┬─────┘
                        │
                        │ [返回编辑]
                        │ ↓ 选择目标步骤：
                        │   ├─ 返回Step 1 → 清空Step1/2/3时间戳
                        │   └─ 返回Step 2 → 清空Step2/3时间戳
                        ▼
                  [重新进入对应步骤]
```

### 6.2 状态转换规则表

| 当前状态 | 触发事件 | 前置条件 | 后置状态 | 副作用 |
|---------|---------|---------|---------|--------|
| **Step1Pending** | CompleteStep1 | PrescriptionEnabled=false OR (PrescriptionEnabled=true AND 处方不为空) | Step1Completed | 设置 Step1CompletedAt |
| **Step1Completed** | NavigateToStep2 | PrescriptionEnabled=true | (保持) | - |
| **Step1Completed** | NavigateToStep3 | PrescriptionEnabled=false | (保持) | - |
| **Step2Completed** | CompleteStep2 | 治法方案已填写 | (保持) | 设置 Step2CompletedAt |
| **Step2Completed** | NavigateToStep3 | Step2CompletedAt != NULL | (保持) | - |
| **Completed** | ResetToStep1 | - | Step1Pending | 清空 Step1/2/3CompletedAt |
| **Completed** | ResetToStep2 | - | Step1Completed | 清空 Step2/3CompletedAt |

---

## 7. Phase拆分与实施计划

### 7.1 Phase 1：核心流程框架（5天）⭐ P0

**目标**：建立三步工作流基础框架

**任务清单**：
- [ ] 数据库迁移脚本（Step1/2/3CompletedAt字段）
- [ ] ConsultationsController.CompleteStep1端点
- [ ] ConsultationsController.ResetSteps端点
- [ ] ConsultationService.CompleteStep1Async方法
- [ ] ConsultationService.ResetStepsAsync方法
- [ ] ConsultationWorkstationViewModel状态管理
- [ ] Step1ConsultationView处方RadioBox UI
- [ ] Step1ConsultationViewModel验证逻辑
- [ ] 单元测试（Service层验证逻辑）

**验收标准**：
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ Step 1完成后可正确跳转Step 2或Step 3
- ✅ 处方为空时正确显示错误提示
- ✅ Step 3可返回Step 1/2编辑

### 7.2 Phase 2：处方管理增强（4天）⭐ P0

**目标**：实现处方软删除和验方导入功能

**任务清单**：
- [ ] 数据库迁移脚本（Prescriptions.IsActive字段）
- [ ] PrescriptionsController.DeletePrescription端点
- [ ] PrescriptionsController.ImportFromFormula端点
- [ ] PrescriptionService.DeletePrescriptionAsync方法
- [ ] PrescriptionService.ImportFromFormulaAsync方法（重复检测）
- [ ] PrescriptionDeletionDialog对话框UI
- [ ] Step2TreatmentViewModel删除/导入命令
- [ ] 重复药材弹窗提示逻辑
- [ ] 单元测试（重复检测算法）

**验收标准**：
- ✅ 删除处方时可选择软删除/物理删除
- ✅ 从验方导入时正确检测重复药材
- ✅ 重复药材自动取大值，并弹窗提示

### 7.3 Phase 3：其他病案查询（3天）⭐ P1

**目标**：实现辩证/施治阶段的病案参考功能

**任务清单**：
- [ ] ConsultationsController.QueryOtherCases端点
- [ ] OtherCasesQueryService服务实现
- [ ] OtherCasesPopup弹窗UI
- [ ] OtherCasesQueryViewModel查询逻辑
- [ ] FloatingQueryMenu悬浮菜单UI
- [ ] Step1/Step2界面集成悬浮菜单
- [ ] 病案详情查看功能
- [ ] 处方导入功能（从其他病案）

**验收标准**：
- ✅ 可通过姓名/电话/辩证结果查询其他病案
- ✅ 查询结果正确分页
- ✅ 可查看病案详情
- ✅ 可从其他病案导入处方

### 7.4 Phase 4：1:1关系强化（2天）⭐ P1

**目标**：保证诊断-处方严格1:1关系

**任务清单**：
- [ ] 数据库迁移脚本（ConsultationPrescriptions关联表）
- [ ] ConsultationPrescriptionRepository实现
- [ ] Service层1:1约束验证
- [ ] 现有数据迁移脚本
- [ ] 唯一索引验证
- [ ] 集成测试（并发创建检测）

**验收标准**：
- ✅ 数据库层面保证1:1约束（UQ_ConsultationPrescription）
- ✅ Service层创建处方前检查现有关联
- ✅ 现有数据正确迁移到关联表

### 7.5 Phase 5：文档与测试完善（2天）⭐ P2

**目标**：完善文档和测试覆盖率

**任务清单**：
- [ ] 更新API文档（Swagger注释）
- [ ] 更新架构文档（clinical-workflow-entity-relationships.md）
- [ ] 更新业务规则文档（business-rules.md）
- [ ] 更新快速参考文档（api-reference.md）
- [ ] 集成测试（E2E流程测试）
- [ ] 性能测试（查询其他病案）

**验收标准**：
- ✅ 所有新增API有完整Swagger注释
- ✅ 架构文档反映最新设计
- ✅ 集成测试覆盖主要业务流程

---

## 8. 质量标准

### 8.1 编译标准

- ✅ **0 errors, 0 warnings**
- ✅ 所有引用正确
- ✅ 类型检查通过

### 8.2 运行时验证标准

- ✅ 启动应用（Desktop + Server）
- ✅ 执行完整三步工作流
- ✅ 验证数据库状态（时间戳、软删除标记）
- ✅ 从用户视角确认功能可用

### 8.3 代码规范

- ✅ 遵循C#命名规范（PascalCase、_camelCase）
- ✅ 所有公开API有XML文档注释
- ✅ 异步方法正确使用async/await
- ✅ 依赖注入仅使用构造函数注入

### 8.4 测试覆盖率

- ✅ Service层核心逻辑单元测试覆盖率 ≥80%
- ✅ 验证逻辑100%覆盖
- ✅ 状态转换逻辑100%覆盖
- ✅ 重复检测算法100%覆盖

### 8.5 性能标准

- ✅ 其他病案查询响应时间 <500ms（1000条数据）
- ✅ 处方导入响应时间 <200ms（50条药材）
- ✅ Step切换响应时间 <100ms

### 8.6 文档同步标准

- ✅ 代码变更后立即更新对应文档
- ✅ API变更同步更新Swagger和api-reference.md
- ✅ 数据库变更同步更新entity-relationships.md
- ✅ 业务规则变更同步更新business-rules.md

---

## 附录A：DTO完整定义

```csharp
// ConsultationStepDto.cs
public class ConsultationStepDto
{
    public int Id { get; set; }
    public int MedicalCaseId { get; set; }
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public DateTime? Step3CompletedAt { get; set; }
    public bool PrescriptionEnabled { get; set; }
    public string CurrentStep { get; set; } // "Step1" | "Step2" | "Step3" | "Completed"
}

// CompleteStep1Request.cs
public class CompleteStep1Request
{
    public bool PrescriptionEnabled { get; set; } = true;
}

// ResetStepsRequest.cs
public class ResetStepsRequest
{
    public string TargetStep { get; set; } // "Step1" | "Step2"
}

// OtherCasesQueryRequest.cs
public class OtherCasesQueryRequest
{
    public string? PatientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DiagnosisKeyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// OtherCaseDto.cs
public class OtherCaseDto
{
    public int MedicalCaseId { get; set; }
    public string PatientName { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime VisitDate { get; set; }
    public string ChiefComplaint { get; set; }
    public string DiagnosisResult { get; set; }
    public bool HasPrescription { get; set; }
}

// ImportFormulaRequest.cs
public class ImportFormulaRequest
{
    public int FormulaId { get; set; }
}

// ImportResult.cs
public class ImportResult
{
    public int ImportedCount { get; set; }
    public List<DuplicateHerbDto> Duplicates { get; set; } = new();
}

// DuplicateHerbDto.cs
public class DuplicateHerbDto
{
    public string HerbName { get; set; }
    public decimal ExistingDosage { get; set; }
    public decimal ImportedDosage { get; set; }
    public decimal FinalDosage { get; set; }
}
```

---

## 附录B：ViewModel完整定义

```csharp
// Step1ConsultationViewModel.cs
public class Step1ConsultationViewModel : BindableBase
{
    private readonly IConsultationService _consultationService;
    private readonly IDialogService _dialogService;

    // 四诊信息
    private string _observation;
    private string _auscultation;
    private string _inquiry;
    private string _palpation;
    private string _chiefComplaint;
    private string _diagnosisResult;

    // 处方开关
    private bool _prescriptionEnabled = true;
    public bool PrescriptionEnabled
    {
        get => _prescriptionEnabled;
        set => SetProperty(ref _prescriptionEnabled, value);
    }

    public bool PrescriptionDisabled
    {
        get => !_prescriptionEnabled;
        set => PrescriptionEnabled = !value;
    }

    // 命令
    public DelegateCommand CompleteStep1Command { get; }
    public DelegateCommand SaveDraftCommand { get; }
    public DelegateCommand ShowOtherCasesQueryCommand { get; }

    public Step1ConsultationViewModel(
        IConsultationService consultationService,
        IDialogService dialogService)
    {
        _consultationService = consultationService;
        _dialogService = dialogService;

        CompleteStep1Command = new DelegateCommand(OnCompleteStep1);
        SaveDraftCommand = new DelegateCommand(OnSaveDraft);
        ShowOtherCasesQueryCommand = new DelegateCommand(OnShowOtherCasesQuery);
    }

    private async void OnCompleteStep1()
    {
        var result = await _consultationService.CompleteStep1Async(
            CurrentConsultationId, 
            PrescriptionEnabled);

        if (!result.IsSuccess)
        {
            await _dialogService.ShowMessageAsync("错误", result.ErrorMessage);
            return;
        }

        // 根据PrescriptionEnabled决定跳转
        if (PrescriptionEnabled)
        {
            NavigateToStep2();
        }
        else
        {
            NavigateToStep3();
        }
    }

    private async void OnSaveDraft()
    {
        // 暂存逻辑（不验证处方）
        await _consultationService.SaveDraftAsync(CurrentConsultationId, GetCurrentData());
        await _dialogService.ShowMessageAsync("提示", "暂存成功");
    }

    private void OnShowOtherCasesQuery()
    {
        _dialogService.ShowDialog<OtherCasesPopup>();
    }
}

// Step2TreatmentViewModel.cs
public class Step2TreatmentViewModel : BindableBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IDialogService _dialogService;

    private string _treatmentPrinciple;
    private string _treatmentMethod;
    private ObservableCollection<PrescriptionItemViewModel> _prescriptionItems;

    public DelegateCommand ImportFromFormulaCommand { get; }
    public DelegateCommand DeletePrescriptionCommand { get; }
    public DelegateCommand<PrescriptionItemViewModel> RemoveItemCommand { get; }

    private async void OnImportFromFormula()
    {
        var formulaId = await _dialogService.ShowFormulaSelectionDialogAsync();
        if (formulaId == null) return;

        var result = await _prescriptionService.ImportFromFormulaAsync(
            CurrentPrescriptionId, 
            formulaId.Value);

        if (result.Duplicates.Any())
        {
            var message = string.Join("\n", result.Duplicates.Select(d =>
                $"{d.HerbName}: {d.ExistingDosage}g → {d.FinalDosage}g"));
            await _dialogService.ShowMessageAsync("重复药材已取大值", message);
        }

        await ReloadPrescriptionItems();
    }

    private async void OnDeletePrescription()
    {
        var deletionType = await _dialogService.ShowDialog<PrescriptionDeletionDialog>();
        if (deletionType == null) return;

        var physicalDelete = deletionType == "Physical";
        await _prescriptionService.DeletePrescriptionAsync(
            CurrentPrescriptionId, 
            physicalDelete);

        await _dialogService.ShowMessageAsync("提示", "处方已删除");
        NavigateBack();
    }
}
```

---

**文档结束**

**版本历史**：
- v1.0 (2025-10-24)：初始版本，完整设计规格
