# Design: refactor-medicalcase-management

## Overview

本设计文档描述医案管理模块重构为Master-Detail布局的技术方案。

## Current State Analysis

### 现有医案模块结构

```
LYBT.Desktop.MedicalCase/
├── Views/
│   ├── MedicalCaseManagementView.xaml      # 列表视图（独立页面）
│   ├── MedicalCaseDetailView.xaml          # 详情视图（独立页面）
│   └── MedicalCaseWorkspaceView.xaml       # 看诊工作区（保留）
├── ViewModels/
│   ├── MedicalCaseManagementViewModel.cs   # 列表ViewModel
│   ├── MedicalCaseDetailViewModel.cs       # 详情ViewModel
│   ├── MedicalCaseWorkspaceViewModel.cs    # 看诊工作区ViewModel（保留）
│   ├── ConsultationPanelViewModel.cs       # 诊断面板ViewModel
│   └── PrescriptionPanelViewModel.cs       # 处方面板ViewModel
└── Models/
    └── MedicalCaseItem.cs                  # 列表项模型
```

### 当前问题

1. **页面跳转模式**: 列表页点击进入详情页，与其他模块不一致
2. **重复导航逻辑**: 管理和看诊两套导航路径
3. **组件共用**: ConsultationPanelView在看诊和管理中可能共用，需分离

## Target Architecture

### 新的模块结构

```
LYBT.Desktop.MedicalCase/
├── Views/
│   ├── MedicalCaseMasterDetailView.xaml    # 新: Master-Detail合并视图
│   └── MedicalCaseWorkspaceView.xaml       # 保留: 看诊工作区
├── ViewModels/
│   ├── MedicalCaseMasterDetailViewModel.cs # 新: 合并ViewModel
│   ├── MedicalCaseWorkspaceViewModel.cs    # 保留: 看诊工作区
│   ├── ConsultationPanelViewModel.cs       # 保留: 看诊用诊断面板
│   └── PrescriptionPanelViewModel.cs       # 保留: 看诊用处方面板
└── Models/
    ├── MedicalCaseItem.cs                  # 保留: 列表项
    └── MedicalCaseDetailModel.cs           # 新: 详情区域模型
```

## Component Design

### 1. MedicalCaseMasterDetailView

使用`refactor-master-detail-layout`提供的通用控件:

```xml
<controls:MasterDetailLayout HasSelection="{Binding HasSelection}">
    <!-- Master: 医案列表 -->
    <controls:MasterDetailLayout.MasterContent>
        <!-- DataGridToolbar: 仅刷新按钮，无新建 -->
        <!-- SearchBox -->
        <!-- DataGrid -->
        <!-- 分页控件 -->
    </controls:MasterDetailLayout.MasterContent>

    <!-- Detail: 医案详情 -->
    <controls:MasterDetailLayout.DetailContent>
        <!-- DetailToolbar -->
        <!-- 医案详情表单 -->
    </controls:MasterDetailLayout.DetailContent>

    <!-- Empty: 未选中状态 -->
    <controls:MasterDetailLayout.EmptyContent>
        <controls:EmptyState Title="请选择医案"/>
    </controls:MasterDetailLayout.EmptyContent>
</controls:MasterDetailLayout>
```

### 2. MedicalCaseMasterDetailViewModel

继承`MasterDetailViewModelBase<MedicalCaseItem, MedicalCaseDetailModel>`:

```csharp
public class MedicalCaseMasterDetailViewModel
    : MasterDetailViewModelBase<MedicalCaseItem, MedicalCaseDetailModel>
{
    // 继承自基类的属性:
    // - Items: ObservableCollection<MedicalCaseItem>
    // - SelectedItem: MedicalCaseItem
    // - HasSelection: bool
    // - IsEditMode: bool
    // - ViewDetail / EditDetail: MedicalCaseDetailModel

    // 继承自基类的命令:
    // - RefreshCommand (有)
    // - EditCommand, SaveCommand, CancelCommand (有)
    // - AddCommand (禁用/不实现 - 无新建功能)

    protected override async Task LoadItemsAsync() { /* 加载医案列表 */ }
    protected override async Task LoadDetailAsync(MedicalCaseItem item) { /* 加载详情 */ }
    protected override async Task<bool> SaveDetailAsync() { /* 保存详情 */ }
}
```

**关键设计**: 不实现`AddCommand`，工具栏不显示新建按钮。

### 3. MedicalCaseDetailModel

详情区域数据模型:

```csharp
public class MedicalCaseDetailModel : BindableBase
{
    // 基本信息
    public Guid Id { get; set; }
    public string PatientName { get; set; }      // 只读
    public DateTime ConsultationDate { get; set; }
    public MedicalCaseStatus Status { get; set; }
    public string Remark { get; set; }           // 可编辑

    // 诊断摘要 (只读)
    public string DiagnosisSummary { get; set; } // 格式化的诊断信息

    // 处方摘要 (只读)
    public string PrescriptionSummary { get; set; } // 格式化的处方信息
    public int? HerbCount { get; set; }
    public int? DoseCount { get; set; }

    // 审计信息
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 4. DataGrid列配置

Master区域列表显示:

| 列 | 字段 | 宽度 | 说明 |
|----|------|------|------|
| 患者 | PatientName | 100 | 患者姓名 |
| 日期 | ConsultationDate | 90 | 就诊日期 |
| 诊断 | Diagnosis | * | 中医诊断(TCMDiagnosis) |
| 状态 | Status | 70 | StatusBadge组件 |

### 5. 详情区域布局

```
┌─────────────────────────────────────────────────────────────┐
│ 工具栏: [编辑] [保存] [取消]                                 │
├─────────────────────────────────────────────────────────────┤
│ 患者信息                                                    │
│ ┌─────────────┬─────────────────────────────────────────┐   │
│ │ 患者姓名    │ 张三                                     │   │
│ ├─────────────┼─────────────────────────────────────────┤   │
│ │ 就诊日期    │ 2024-12-17                               │   │
│ └─────────────┴─────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ 诊断信息 (只读)                                             │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ 现病史: xxxxxx                                         │   │
│ │ 舌诊: xxxxxx                                           │   │
│ │ 脉诊: xxxxxx                                           │   │
│ │ 中医诊断: xxxxxx                                       │   │
│ └───────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ 处方信息 (只读)                                             │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ 药材: 12味   剂数: 7剂   来源: 自拟方                   │   │
│ └───────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ 备注 (可编辑)                                               │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ [文本框]                                               │   │
│ └───────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│ 状态: Active     创建时间: 2024-12-17 10:30                 │
└─────────────────────────────────────────────────────────────┘
```

## Navigation Flow

### 管理模式导航

```
ClinicalHomeView / AdminHomeView
    ↓ 点击"医案管理"
MedicalCaseMasterDetailView
    ↓ 选择列表项
    → 右侧显示详情
    ↓ 点击"编辑"
    → 右侧切换为编辑模式
    ↓ 点击"保存"/"取消"
    → 返回查看模式
```

### 看诊模式导航（保持不变）

```
ClinicalHomeView
    ↓ 选择患者 → 点击"开始看诊"
MedicalCaseWorkspaceView
    → 诊断面板(ConsultationPanelView)
    → 处方面板(PrescriptionPanelView)
    ↓ 完成看诊
ClinicalHomeView
```

## Data Flow

### 列表加载

```
ViewModel.LoadItemsAsync()
    ↓
IMedicalCaseRepository.GetPagedAsync(page, pageSize, keyword)
    ↓
API: GET /api/medicalcases?page=1&pageSize=20&keyword=xxx
    ↓
Items = result.Items.Select(MedicalCaseItem.FromDto)
```

### 详情加载

```
ViewModel.LoadDetailAsync(selectedItem)
    ↓
IMedicalCaseRepository.GetByIdAsync(id)
    ↓
API: GET /api/medicalcases/{id}
    ↓
ViewDetail = MedicalCaseDetailModel.FromDto(result)
```

### 保存详情

```
ViewModel.SaveDetailAsync()
    ↓
IMedicalCaseRepository.UpdateAsync(id, dto)
    ↓
API: PUT /api/medicalcases/{id}
    ↓
刷新列表 / 更新当前项
```

## Integration Points

### 与看诊工作区的关系

- **独立运行**: MedicalCaseMasterDetailView和MedicalCaseWorkspaceView是独立的视图
- **数据共享**: 共用同一个Repository和API
- **组件隔离**: 管理视图使用只读摘要，看诊视图使用编辑面板

### 与其他模块的一致性

| 模块 | 视图 | 新建入口 |
|------|------|----------|
| 验方 | FormulaMasterDetailView | 管理视图工具栏 |
| 药材 | HerbMasterDetailView | 管理视图工具栏 |
| 患者 | PatientMasterDetailView | 管理视图工具栏 |
| 用户 | UserMasterDetailView | 管理视图工具栏 |
| **医案** | **MedicalCaseMasterDetailView** | **看诊入口（非管理视图）** |

医案模块的特殊性: 新建医案必须通过看诊流程，不在管理视图中提供新建功能。

## Testing Strategy

### ViewModel Tests

```csharp
public class MedicalCaseMasterDetailViewModelTests
{
    [Fact]
    public async Task LoadItemsAsync_ShouldPopulateItems();

    [Fact]
    public async Task SelectItem_ShouldLoadDetail();

    [Fact]
    public async Task EditAndSave_ShouldUpdateDetail();

    [Fact]
    public void AddCommand_ShouldNotBeAvailable(); // 验证无新建功能
}
```

### Integration Tests

- 导航到新视图正常
- 列表加载正确
- 详情加载正确
- 编辑保存正常
- 与看诊工作区互不影响
