# OpenSpec Proposal: unify-vm-view-binding-patterns

## 概述

统一Desktop层VM-View绑定模式，消除模块间不一致性，建立最佳实践规范。

## 当前状态分析

### 已发现的不一致性

#### 1. Detail属性命名不一致

| 模块 | View绑定属性 | ViewModel属性 | 一致性 |
|------|-------------|---------------|--------|
| **Users** | `CurrentDetail.UserName` | `CurrentDetail` (基类) | ✅ 一致 |
| **Patients** | `CurrentDetail.Name` | `CurrentDetail` (基类) | ✅ 一致 |
| **Herbs** | `CurrentDetail.Name` | `CurrentDetail` (基类) | ✅ 一致 |
| **MedicalCase** | `CurrentDetail.PatientName` | `CurrentDetail` (基类) | ✅ 一致 |
| **Formula** | `EditDetail.Name` | `EditDetail` (派生属性，实际返回CurrentDetail) | ⚠️ 不一致 |

**问题**: Formula模块使用 `EditDetail` 和 `ViewFormulaDto` 作为额外的包装属性，与其他模块的 `CurrentDetail` 直接绑定模式不一致。

#### 2. ViewControl绑定模式不一致

| 模块 | ViewControl绑定源 | 说明 |
|------|------------------|------|
| **Users, Patients, Herbs, MedicalCase** | `CurrentDetail.X` | 直接绑定Detail属性 |
| **Formula** | `ViewFormulaDto` | 额外的Dto转换层 |

### 已确认的一致性（良好模式）

#### 1. MasterDetailControl架构模式 - 100%一致

所有5个模块均遵循相同的结构：
```xml
<controls:MasterDetailLayout HasSelection="{Binding HasSelection}">
    <controls:MasterDetailLayout.MasterContent>
        <!-- 工具栏 + 搜索 + DataGrid + 分页 -->
    </controls:MasterDetailLayout.MasterContent>
    <controls:MasterDetailLayout.DetailContent>
        <!-- DetailToolbar + ViewControl + EditControl -->
    </controls:MasterDetailLayout.DetailContent>
    <controls:MasterDetailLayout.EmptyContent>
        <!-- EmptyState -->
    </controls:MasterDetailLayout.EmptyContent>
</controls:MasterDetailLayout>
```

#### 2. ViewControl/EditControl内部绑定模式 - 100%一致

所有10个Control文件均使用 `DataContext="{Binding ElementName=Root}"` 模式：
- HerbEditControl.xaml, HerbViewControl.xaml
- UserEditControl.xaml, UserViewControl.xaml
- PatientEditControl.xaml, PatientViewControl.xaml
- FormulaEditControl.xaml, FormulaViewControl.xaml
- MedicalCaseEditControl.xaml, MedicalCaseViewControl.xaml

#### 3. DataTemplate内的AncestorType绑定 - 100%正确

所有DataTemplate中的RelativeSource绑定均使用具体类型而非泛型UserControl：
```xml
<DataTemplate>
    <herbItem:HerbItemControl
        IsEditMode="{Binding RelativeSource={RelativeSource AncestorType={x:Type local:HerbListControl}}, Path=IsEditMode}"/>
</DataTemplate>
```

## 最佳实践方案

### 模式1: MasterDetail绑定模式（推荐统一）

**原则**: 所有MasterDetailViewModel派生类应直接使用基类的 `CurrentDetail` 属性。

**标准模式**:
```xml
<!-- MasterDetailControl.xaml -->
<xxxControls:XXXViewControl
    PropertyA="{Binding CurrentDetail.PropertyA}"
    PropertyB="{Binding CurrentDetail.PropertyB}"
    .../>

<xxxControls:XXXEditControl
    PropertyA="{Binding CurrentDetail.PropertyA, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    .../>
```

**禁止模式**:
- 不应创建 `EditDetail`, `ViewDetail`, `ViewXxxDto` 等额外包装属性
- 如需转换，应在Control内部或Mapper中处理

### 模式2: ViewControl/EditControl内部绑定模式（已统一）

**标准模式**:
```xml
<UserControl x:Name="Root" x:Class="LYBT.Desktop.XXX.Controls.XXXEditControl">
    <Grid DataContext="{Binding ElementName=Root}">
        <TextBox Text="{Binding PropertyName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
    </Grid>
</UserControl>
```

**说明**: 
- 使用 `x:Name="Root"` 定义控件名
- 内部Grid通过 `ElementName=Root` 绑定到控件自身
- 控件内部属性绑定不需要指定DataContext路径

### 模式3: DataTemplate中的RelativeSource绑定（已统一）

**标准模式**:
```xml
<DataTemplate>
    <local:ChildControl
        Property="{Binding RelativeSource={RelativeSource AncestorType={x:Type local:ParentControl}}, Path=SomeProperty}"/>
</DataTemplate>
```

**禁止模式**:
- `AncestorType=UserControl` - 可能导致类型歧义
- `ElementName=Root` - DataTemplate内NameScope隔离会导致失败

## 重构任务

### Phase 1: Formula模块统一 ✅ 已完成

**任务1.1**: 移除 `EditDetail` 派生属性 ✅
- 文件: `FormulaMasterDetailViewModel.cs`
- 变更: 删除 `EditDetail` 属性，XAML改用 `CurrentDetail`
- 状态: **已完成** (2026-01-15)

**任务1.2**: 重构 `FormulaViewControl` 消除 `ViewFormulaDto` ✅
- **结论: 已重构移除**
- 变更: 将 `FormulaViewControl.Formula` 属性类型从 `FormulaDetailDto` 改为 `FormulaDetailModel`
- 前提: 为 `FormulaDetailModel` 添加缺失的 `Source` 属性
- 状态: **已完成** (2026-01-15)

**任务1.3**: 更新XAML绑定 ✅
- 文件: `FormulaMasterDetailControl.xaml`
- 变更: 将 `EditDetail.X` 改为 `CurrentDetail.X`
- 添加OpenSpec注释标记变更来源
- 状态: **已完成** (2026-01-15)

### Phase 2: 文档规范化

**任务2.1**: 更新 `LYBT.Desktop.Infrastructure/CLAUDE.md`
- 添加"MasterDetail绑定规范"章节
- 记录三种绑定模式的使用场景

**任务2.2**: 创建绑定模式速查表
- 位置: `.claude/skills/wpf-desktop-dev/references/binding-patterns.md`
- 内容: 快速诊断和最佳实践参考

## 影响评估

### 代码影响

| 文件 | 变更类型 | 风险 |
|------|---------|------|
| `FormulaMasterDetailViewModel.cs` | 删除ViewFormulaDto属性及相关引用 | 低 |
| `FormulaMasterDetailControl.xaml` | 属性名替换(EditDetail→CurrentDetail, ViewFormulaDto→CurrentDetail) | 低 |
| `FormulaViewControl.xaml.cs` | Formula属性类型从FormulaDetailDto改为FormulaDetailModel | 低 |
| `FormulaDetailModel.cs` | 添加Source属性，更新Clone方法 | 低 |
| `FormulaDetailModelMapper.cs` | 移除Source忽略指令 | 低 |

### 测试要求

1. 验方模块功能测试
   - 列表加载
   - 查看详情
   - 编辑保存
   - 新建验方

### 回滚计划

如出现问题，可通过git revert恢复，因变更范围有限且独立。

## 决策记录

| 决策点 | 选项 | 选择 | 理由 |
|--------|------|------|------|
| Detail属性命名 | CurrentDetail vs EditDetail | CurrentDetail | 遵循基类设计，减少额外包装 |
| ViewFormulaDto | 移除 vs 保留 | **移除** | 重构FormulaViewControl接受FormulaDetailModel类型，消除转换层 |
| FormulaDetailModel | 添加Source属性 | **已添加** | 保持与FormulaDetailDto的属性对等，支持View绑定 |

## 时间估算

- Phase 1: 1小时
- Phase 2: 0.5小时
- 测试验证: 0.5小时
- **总计: 2小时**
