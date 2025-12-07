# Design: optimize-module-list-ui

## Context

当前LYBTZYZS项目有四个模块管理列表视图：
- UserManagementView（用户管理）
- HerbManagementView（药材管理）
- FormulaManagementView（验方管理）
- PatientManagementView（患者管理）

这些视图都使用 `BaseMasterDataListView` 模板组件，包含三行布局（工具栏、数据表格、分页栏），但各模块的列设计和交互模式存在不一致。

### 现状分析

| 模块 | 状态列 | 状态切换按钮 | CheckBox对齐 | 恢复功能 |
|------|--------|--------------|--------------|----------|
| 用户 | 无 | 有(DataTrigger) | 需验证 | 无 |
| 药材 | 有 | 有(通用按钮) | 需验证 | 无 |
| 验方 | 有 | 无 | 需验证 | 无 |
| 患者 | 无 | 无 | 需验证 | 无 |

## Goals / Non-Goals

### Goals
- 统一所有模块列表的CheckBox列对齐方式
- 统一状态切换交互：用按钮触发，不用状态列显示
- 添加软删除数据的恢复功能（管理员权限）
- 根据各实体属性科学设计列布局
- 统一按钮样式，消除多处样式定义冲突

### Non-Goals
- 不修改BaseMasterDataListView模板的核心结构
- 不修改后端API（仅调用现有方法或添加简单Restore方法）
- 不修改数据库Schema

## Decisions

### Decision 1: 以UserManagementView为参考标准

**What**: 用户管理视图的状态切换按钮设计模式作为统一标准
**Why**:
- UserManagementView已实现DataTrigger动态切换按钮文本（启用/禁用）
- 这种模式直观，用户一眼就能看到可执行的操作

**Implementation Pattern**:
```xml
<Button Style="{StaticResource PrimaryButton}"
        Command="{Binding DataContext.ToggleStatusCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}">
    <TextBlock>
        <TextBlock.Style>
            <Style TargetType="TextBlock">
                <Setter Property="Text" Value="启用" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Status}" Value="Enabled">
                        <Setter Property="Text" Value="禁用" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </TextBlock.Style>
    </TextBlock>
</Button>
```

### Decision 2: CheckBox列垂直居中对齐

**What**: 修改DataGridCheckBoxColumn的CellStyle确保垂直居中
**Why**: WPF DataGrid默认CheckBox可能顶部对齐，与其他列不一致

**Implementation**:
```xml
<DataGridCheckBoxColumn.CellStyle>
    <Style TargetType="DataGridCell">
        <Setter Property="VerticalAlignment" Value="Center" />
        <Setter Property="HorizontalAlignment" Value="Center" />
    </Style>
</DataGridCheckBoxColumn.CellStyle>
```

### Decision 3: 恢复按钮权限控制

**What**: 恢复按钮仅管理员可见，通过Visibility绑定实现
**Why**:
- 恢复软删除数据是敏感操作
- 使用Visibility绑定比后端权限检查更直观

**Implementation**:
```xml
<Button Content="恢复"
        Style="{StaticResource WarningButton}"
        Visibility="{Binding DataContext.IsAdmin, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BoolToVisibilityConverter}}"
        Command="{Binding DataContext.RestoreCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}" />
```

ViewModel需要添加:
```csharp
public bool IsAdmin => _currentUserRole == UserRole.Admin || _currentUserRole == UserRole.SuperAdmin;
```

### Decision 4: 移除状态列，保留特殊状态列

**What**:
- 移除通用「状态」列（启用/禁用）
- 保留特殊状态列如ValidationStatus（验方校验状态）

**Why**:
- 通用状态通过按钮操作比显示更有价值
- 特殊状态（如ValidationStatus）有独立语义，需要显示

### Decision 5: 以UnifiedComponents.xaml为按钮样式标准

**What**: 统一全局按钮样式，以UnifiedComponents.xaml中的Fluent Design风格为准

**Why**:
- 当前存在多处样式定义冲突：
  - `Colors.xaml`: Material Design Blue #2196F3
  - `CommonStyles.xaml`: 不同主色 #2E86AB
  - `Controls.xaml`: 使用Opacity作为悬停效果
  - `UnifiedComponents.xaml`: Fluent Design #0078D4，使用具体颜色作为悬停效果
- UnifiedComponents.xaml提供最完整、最现代的按钮样式体系

**Implementation**:
- 主色调统一为Fluent Design蓝色: `#0078D4`
- 悬停效果使用具体颜色而非Opacity变化
- 按钮样式包括：PrimaryButton、SecondaryButton、DangerButton、SuccessButton、WarningButton

**Color Palette** (from UnifiedComponents.xaml):
```xml
<Color x:Key="PrimaryColor">#0078D4</Color>
<Color x:Key="PrimaryHoverColor">#106EBE</Color>
<Color x:Key="PrimaryPressedColor">#005A9E</Color>
<Color x:Key="DangerColor">#D32F2F</Color>
<Color x:Key="DangerHoverColor">#B71C1C</Color>
<Color x:Key="SuccessColor">#388E3C</Color>
<Color x:Key="SuccessHoverColor">#2E7D32</Color>
<Color x:Key="WarningColor">#F57C00</Color>
<Color x:Key="WarningHoverColor">#E65100</Color>
```

## Risks / Trade-offs

### Risk 1: Service层可能缺少Restore方法
- **Mitigation**: 检查各Service是否有Restore方法，若没有则需添加

### Risk 2: 管理员筛选软删除数据需要API支持
- **Mitigation**: 检查现有分页查询API是否支持includeDeleted参数

### Trade-off: 按钮数量增加导致操作列变宽
- **Accept**: 增加「恢复」按钮会使操作列更宽，但功能完整性优先

### Risk 3: 按钮样式迁移可能影响其他视图
- **Mitigation**: 保持样式Key名称不变，仅更新样式实现；逐步验证各视图显示效果

### Trade-off: 多处样式文件需要同步修改
- **Accept**: 为确保一致性，需要同时更新Colors.xaml、CommonStyles.xaml、Controls.xaml

## Migration Plan

1. **Phase 1**: 基础设施（DataGrid样式、按钮样式）
2. **Phase 2**: 各模块视图修改（按药材、验方、患者顺序）
3. **Phase 3**: ViewModel命令添加
4. **Phase 4**: Service层支持（如需要）
5. **Phase 5**: 测试验证

无需回滚计划，修改均为UI层面，不影响数据。

## Open Questions

1. 是否需要添加筛选器让管理员查看软删除数据？（当前假设软删除数据默认不显示，恢复按钮仅在特定筛选条件下可见）
2. 验方的ValidationStatus是否也需要按钮切换？（当前假设保留Badge显示）

## References

- WPF DataGrid CheckBox Alignment: [Microsoft Docs - DataGrid Styles and Templates](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid-styles-and-templates)
- 现有代码参考: `UserManagementView.xaml:98-116`（状态切换按钮实现）
- UI规范: `openspec/specs/ui-style-conventions/spec.md`
