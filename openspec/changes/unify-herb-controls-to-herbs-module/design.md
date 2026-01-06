# 设计文档: unify-herb-controls-to-herbs-module

## 1. 控件对比分析

### 1.1 老方案 (HerbListEditor)

**接口定义**:
```xml
<sharedControls:HerbListEditor
    HerbItems="{Binding HerbItems}"
    IsEditMode="True"
    DeleteHerbCommand="{Binding DeleteHerbCommand}"
    DosageCompletedCommand="{Binding DosageCompletedCommand}"
    AddNewRowCommand="{Binding AddNewRowCommand}"/>
```

**特点**:
- 命令驱动模式
- 需要ViewModel提供3个命令
- HerbItems为单向绑定

### 1.2 新方案 (HerbListControl)

**接口定义**:
```xml
<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4"
    DuplicateStrategy="{Binding DuplicateStrategy}"/>
```

**特点**:
- 属性绑定模式
- 内部处理药材选择、删除、新增
- HerbItems为双向绑定，自动同步
- 支持重复药材策略配置

## 2. 迁移设计

### 2.1 MedicalCaseEditControl迁移

**当前代码** (`MedicalCaseEditControl.xaml`):
```xml
xmlns:sharedControls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure"

<sharedControls:HerbListEditor
    HerbItems="{Binding HerbItems}"
    IsEditMode="True"
    DeleteHerbCommand="{Binding DeleteHerbCommand}"
    DosageCompletedCommand="{Binding DosageCompletedCommand}"
    AddNewRowCommand="{Binding AddNewRowCommand}"/>
```

**迁移后**:
```xml
xmlns:herbList="clr-namespace:LYBT.Desktop.Herbs.Controls.HerbList;assembly=LYBT.Desktop.Herbs"

<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4"/>
```

**代码后置变更** (`MedicalCaseEditControl.xaml.cs`):
- 移除: `DeleteHerbCommand`, `DosageCompletedCommand`, `AddNewRowCommand` 属性
- 添加: `AllHerbs` 属性 (IEnumerable)

### 2.2 FormulaEditControl迁移

**当前代码** (`FormulaEditControl.xaml`):
```xml
xmlns:sharedControls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure"

<sharedControls:HerbListEditor
    HerbItems="{Binding HerbItems}"
    IsEditMode="True"
    DeleteHerbCommand="{Binding DeleteHerbCommand}"
    DosageCompletedCommand="{Binding DosageCompletedCommand}"
    AddNewRowCommand="{Binding AddNewRowCommand}"/>
```

**迁移后**:
```xml
xmlns:herbList="clr-namespace:LYBT.Desktop.Herbs.Controls.HerbList;assembly=LYBT.Desktop.Herbs"

<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4"/>
```

**代码后置变更** (`FormulaEditControl.xaml.cs`):
- 移除: `DeleteHerbCommand`, `DosageCompletedCommand`, `AddNewRowCommand` 属性
- 添加: `AllHerbs` 属性 (IEnumerable)

## 3. 模块依赖

### 3.1 当前依赖
```
MedicalCase模块 --> Infrastructure模块
Formula模块 --> Infrastructure模块
```

### 3.2 迁移后依赖
```
MedicalCase模块 --> Infrastructure模块
                --> Herbs模块 (新增)
Formula模块 --> Infrastructure模块
            --> Herbs模块 (新增)
```

### 3.3 csproj配置

需要在以下项目中添加引用：

**LYBT.Desktop.MedicalCase.csproj**:
```xml
<ProjectReference Include="..\..\Modules\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
```

**LYBT.Desktop.Formula.csproj**:
```xml
<ProjectReference Include="..\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
```

## 4. AllHerbs属性来源

HerbListControl需要 `AllHerbs` 属性提供可选药材列表。分析调用链：

### 4.1 MedicalCaseMasterDetailControl调用链
```
MedicalCaseMasterDetailControl
  └── MedicalCaseMasterDetailViewModel
        └── AllHerbs (需检查是否存在)
```

### 4.2 FormulaMasterDetailControl调用链
```
FormulaMasterDetailControl
  └── FormulaMasterDetailViewModel
        └── AllHerbs (需检查是否存在)
```

如果ViewModel中不存在AllHerbs，需要添加该属性并从HerbService获取数据。

## 5. 删除清单

### 5.1 Infrastructure模块删除文件
- `Controls/HerbListEditor.xaml`
- `Controls/HerbListEditor.xaml.cs`
- `Controls/HerbCardControl.xaml`
- `Controls/HerbCardControl.xaml.cs`
- `Controls/HerbListView.xaml` (已删除)
- `Controls/HerbListView.xaml.cs` (已删除)

## 6. 验证检查点

每个Phase完成后执行：
1. `dotnet build` 验证编译通过
2. 运行时测试药材编辑功能
3. 确认药材选择、删除、新增功能正常
