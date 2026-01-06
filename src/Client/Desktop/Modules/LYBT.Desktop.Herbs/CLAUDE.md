# LYBT.Desktop.Herbs 模块说明

## 控件架构 (2026-01-04统一)

### 当前架构

**处方/验方药材编辑** (新架构):
```
Controls/
├── HerbList/                    # 药材列表控件
│   ├── HerbListControl.xaml     # 管理多个HerbItemControl
│   ├── HerbListControlViewModel.cs
│   └── HerbListChangedEventArgs.cs
├── HerbItem/                    # 单个药材项控件
│   ├── HerbItemControl.xaml     # 药材名+剂量+煎法
│   ├── HerbItemControlViewModel.cs
│   └── HerbItemChangedEventArgs.cs
└── Shared/                      # 共享组件
```

**药材管理MasterDetail**:
```
Controls/
├── HerbMasterDetailControl.xaml  # 药材管理主控件
├── HerbEditControl.xaml          # 编辑表单
└── HerbViewControl.xaml          # 只读预览
```

### 已删除的过期控件

- `HerbCardControl` - 旧版药材卡片，被 `HerbItemControl` 替代
- `HerbListView` - 旧版只读列表，被 `HerbListControl(IsEditMode=False)` 替代

### 使用方式

**编辑模式** (处方/验方编辑):
```xml
<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4" />
```

**只读模式** (医案预览):
```xml
<herbList:HerbListControl
    HerbItems="{Binding HerbItems}"
    AllHerbs="{Binding AllHerbs}"
    IsEditMode="False"
    Columns="4" />
```

### 相关OpenSpec

- `unify-herb-controls-to-herbs-module` - 统一药材控件到Herbs模块
- `herb-editor-control-refactoring` - HerbListControl/HerbItemControl重构
