# OpenSpec Proposal: 统一药材列表编辑控件

## Status: Approved
## Created: 2025-12-17
## Implemented: 2025-12-17
## Author: Claude Code

---

## 1. Why (问题与动机)

### 1.1 当前问题

1. **控件使用不一致**:
   - `PrescriptionEditorPanel.xaml` 和 `FormulaEditControl.xaml` 使用 `HerbListEditor`
   - `EditFormulaDialog.xaml` 直接使用 `ItemsControl + HerbCardControl`
   - `MedicalCaseWorkspaceView.xaml` 直接使用 `ItemsControl + HerbCardControl` (预览模式)

2. **代码重复**: `EditFormulaDialog.xaml` 重复实现了 `HerbListEditor` 已封装的布局逻辑

3. **药材单位Bug**: `HerbItemViewModelBase._unit` 默认值为 `"g"`，但应该从药材自身单位获取（如"条"、"枚"等）

4. **缺少只读预览控件**: 预览模式直接使用编辑控件，只通过 `IsEditMode=False` 控制，不够语义化

### 1.2 目标

- 建立清晰的控件层次：`HerbCardControl` → `HerbListEditor/HerbListView` → 业务视图
- 统一处方和经验方的药材编辑控件调用
- 修复药材单位自动匹配Bug
- 创建只读预览专用控件 `HerbListView`

### 1.3 范围

**包含:**
- HerbItemViewModelBase Unit默认值修复
- 新建 HerbListView 只读预览控件
- 重构 EditFormulaDialog 使用 HerbListEditor
- 重构 MedicalCaseWorkspaceView 药材预览区域使用 HerbListView

**排除:**
- HerbCardControl 核心逻辑变更
- ViewModel数据处理逻辑变更（仅修复Unit默认值）

---

## 2. What Changes (变更内容)

### 2.1 控件层次结构

```
HerbCardControl (基础药材快速匹配控件)
    ├── HerbListEditor (编辑模式 - 药材列表编辑)
    │       ├── PrescriptionEditorPanel (处方编辑 - ShowPrice=True)
    │       ├── FormulaEditControl (经验方编辑 - ShowPrice=False)
    │       └── EditFormulaDialog (验方对话框编辑 - ShowPrice=False)
    │
    └── HerbListView (预览模式 - 药材列表只读)
            ├── MedicalCaseWorkspaceView (医案预览)
            ├── MedicalCaseViewControl (医案详情预览)
            └── FormulaViewControl (经验方预览)
```

### 2.2 新增控件

| 控件 | 位置 | 说明 |
|------|------|------|
| HerbListView | LYBT.Desktop.Presentation/Components | 只读药材列表预览，4列UniformGrid布局 |

### 2.3 修改文件

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| HerbItemViewModelBase.cs | Bug修复 | `_unit = ""` 改为空字符串，由SelectedHerb赋值 |
| EditFormulaDialog.xaml | 重构 | 替换ItemsControl为HerbListEditor |
| MedicalCaseWorkspaceView.xaml | 重构 | 替换ItemsControl为HerbListView |
| MedicalCaseViewControl.xaml | 检查 | 确认使用HerbListView |

### 2.4 Unit Bug修复详情

**当前代码 (HerbItemViewModelBase.cs:21):**
```csharp
private string _unit = "g";  // 硬编码默认值
```

**修复后:**
```csharp
private string _unit = string.Empty;  // 空字符串，由药材数据赋值
```

**SelectedHerb setter已有正确逻辑:**
```csharp
set
{
    if (SetProperty(ref _selectedHerb, value) && value != null)
    {
        // ...
        Unit = value.Unit;  // 从药材DTO获取单位（如"克"、"条"、"枚"）
    }
}
```

---

## 3. Impact (影响评估)

### 3.1 代码影响

| 类型 | 文件数 | 说明 |
|------|--------|------|
| 新增 | 2 | HerbListView.xaml, HerbListView.xaml.cs |
| 修改 | 4 | HerbItemViewModelBase, EditFormulaDialog, MedicalCaseWorkspaceView, 可能的View控件 |
| 删除 | 0 | 无 |

### 3.2 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| Unit默认值变更 | 低 | 仅影响初始显示，SelectedHerb赋值后即正确 |
| 控件替换 | 低 | HerbListEditor已在多处使用，模式成熟 |
| 布局兼容性 | 低 | 保持相同的UniformGrid 4列布局 |

### 3.3 ViewModel差异处理

| 场景 | 处理方式 |
|------|----------|
| 处方编辑 | ShowPrice=True，加载药材单价 |
| 经验方编辑 | ShowPrice=False，单价可为0 |
| 预览模式 | 使用HerbListView，无交互 |

---

## 4. Implementation Plan (实施计划)

### Phase 1: Bug修复与新控件

1. 修复 `HerbItemViewModelBase._unit` 默认值
2. 创建 `HerbListView` 只读预览控件

### Phase 2: 控件统一

3. 重构 `EditFormulaDialog.xaml` 使用 `HerbListEditor`
4. 重构 `MedicalCaseWorkspaceView.xaml` 使用 `HerbListView`
5. 检查并更新其他预览场景

### Phase 3: 验证

6. 验证处方编辑功能
7. 验证经验方编辑功能
8. 验证预览显示功能
9. 编译测试

---

## 5. Technical Details (技术细节)

### 5.1 HerbListView 控件设计

```xml
<!-- HerbListView.xaml -->
<UserControl>
    <ItemsControl ItemsSource="{Binding Items, RelativeSource={RelativeSource AncestorType=UserControl}}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <UniformGrid Columns="4"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <local:HerbCardControl
                    IsEditMode="False"
                    ShowPrice="{Binding ShowPrice, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</UserControl>
```

### 5.2 依赖属性

| 控件 | 属性 | 类型 | 说明 |
|------|------|------|------|
| HerbListView | Items | IEnumerable | 药材项集合 |
| HerbListView | ShowPrice | bool | 是否显示价格 |
| HerbListEditor | Items | IEnumerable | 药材项集合 |
| HerbListEditor | ShowPrice | bool | 是否显示价格 |
| HerbListEditor | DeleteHerbCommand | ICommand | 删除药材命令 |
| HerbListEditor | AddNewRowCommand | ICommand | 新增行命令 |
| HerbListEditor | DosageCompletedCommand | ICommand | 剂量输入完成命令 |

---

## 6. Acceptance Criteria (验收标准)

- [ ] 药材单位默认值为空，选择药材后自动匹配药材自身单位
- [ ] EditFormulaDialog 使用 HerbListEditor 控件
- [ ] MedicalCaseWorkspaceView 预览使用 HerbListView 控件
- [ ] 处方编辑功能正常（ShowPrice=True）
- [ ] 经验方编辑功能正常（ShowPrice=False）
- [ ] 全量编译通过
