# unify-herb-controls-to-herbs-module

## Why

当前项目中存在**新老两套药材编辑控件**并存的问题：

### 老方案 (Infrastructure模块)
- `HerbListEditor` -> `HerbCardControl` -> 命令模式交互
- 位于 `LYBT.Desktop.Infrastructure.Controls`
- 使用命令属性：`DeleteHerbCommand`, `DosageCompletedCommand`, `AddNewRowCommand`

### 新方案 (Herbs模块)
- `HerbListControl` -> `HerbItemControl` -> 属性绑定模式
- 位于 `LYBT.Desktop.Herbs.Controls`
- 使用属性绑定：`AllHerbs`, `HerbItems (TwoWay)`, `DuplicateStrategy`

### 问题
1. **架构不统一**: 两套控件并存，维护成本高
2. **职责混乱**: 药材控件应归属Herbs业务模块，而非Infrastructure
3. **API不一致**: 老方案用命令，新方案用属性绑定，使用方式不统一

## What Changes

### 调用链分析

```
新方案 (已完成迁移):
Clinical角色
  └── MedicalCaseWorkspaceView
        └── PrescriptionEditorPanel
              └── HerbListControl -> HerbItemControl

老方案 (待迁移):
Admin角色 - 医案管理
  └── MedicalCaseMasterDetailControl
        └── MedicalCaseEditControl
              └── HerbListEditor -> HerbCardControl

Admin角色 - 经验方管理
  └── FormulaMasterDetailControl
        └── FormulaEditControl
              └── HerbListEditor -> HerbCardControl
```

### Phase 1: 迁移MedicalCaseEditControl

将 `MedicalCaseEditControl` 中的 `HerbListEditor` 替换为 `HerbListControl`：
- 添加 xmlns 引用 Herbs 模块
- 替换控件使用
- 更新属性绑定方式
- 移除老方案的命令属性

### Phase 2: 迁移FormulaEditControl

将 `FormulaEditControl` 中的 `HerbListEditor` 替换为 `HerbListControl`：
- 添加 xmlns 引用 Herbs 模块
- 替换控件使用
- 更新属性绑定方式
- 移除老方案的命令属性

### Phase 3: 删除老控件

从 Infrastructure 模块删除废弃控件：
- `HerbListEditor.xaml(.cs)` - 老版药材列表编辑器
- `HerbCardControl.xaml(.cs)` - 老版药材卡片控件
- `HerbListView.xaml(.cs)` - 已删除

### Phase 4: 清理与验证

- 清理 MedicalCaseEditControl/FormulaEditControl 中的命令属性
- 全量编译验证
- 运行时测试

## Architecture

### 目标架构

```
Herbs模块 (LYBT.Desktop.Herbs)
├── Controls/
│   ├── HerbListControl.xaml(.cs)      药材列表编辑控件(新方案)
│   ├── HerbItemControl.xaml(.cs)      单个药材项控件
│   ├── HerbListView.xaml(.cs)         药材列表只读预览
│   └── HerbCardControl.xaml(.cs)      药材卡片控件
├── Models/
│   ├── HerbItemDto.cs                 药材项输出DTO
│   └── DuplicateDosageStrategy.cs     重复剂量策略

Infrastructure模块 (清理后)
├── Controls/
│   └── (不再包含任何药材相关控件)
```

### 统一API模式

所有使用药材编辑功能的控件统一采用新方案API：

```xml
<herbControls:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4"
    DuplicateStrategy="{Binding DuplicateStrategy}" />
```

## Impact

- **文件变更**: ~10个文件
  - 修改: 2个EditControl (XAML+CS)
  - 删除: 4个老控件文件
- **模块依赖**: MedicalCase/Formula模块需添加对Herbs模块的引用
- **编译验证**: 每个Phase完成后需编译验证

## Risks

| 风险 | 缓解措施 |
|------|----------|
| AllHerbs属性缺失 | 检查ViewModel是否已有，必要时添加 |
| 绑定模式不兼容 | HerbItems改为TwoWay绑定 |
| 遗漏引用 | 每个Phase编译验证 |

## References

- OpenSpec: herb-editor-control-refactoring (HerbListControl实现)
- OpenSpec: unify-herb-list-controls (控件统一规范)
