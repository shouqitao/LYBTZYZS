# Desktop端管理界面UI统一化需求规格说明

**文档类型**: Requirements（需求规格）
**创建时间**: 2025-11-06
**文档版本**: v1.0
**作者**: Claude Code
**关联文档**:
- 分析报告: `docs/explanation/design/ui-unification-analysis.md`
- 设计文档: `docs/explanation/design/ui-unification-design.md` (待创建)
- Constitution: `.spec-workflow/steering/constitution.md`

---

## 1. 项目概述

### 1.1 项目背景

当前LYBTZYZS项目的Desktop端包含6个主要管理界面（用户、中药材、病案、验方、处方、患者管理）。虽然大部分界面已迁移至统一设计系统（UnifiedDesignSystem.xaml），但仍存在以下问题：

- **结构碎片化**: 每个界面独立实现工具栏、表格、分页等通用结构，重复代码比例高
- **风格不一致**: 处方管理界面仍使用过时的ToolBarTray和MaterialDesignFlatButton样式
- **功能分散**: 相似功能（搜索、筛选、分页）在各界面单独实现，缺少统一组件
- **维护成本高**: 修改一个通用功能需要改动6个文件

### 1.2 项目目标

**核心目标**: 在不引入第三方UI库的前提下，通过组件化和规范化实现6个管理界面的UI统一

**具体目标**:
1. **代码复用率提升40-60%**: 提取通用组件，减少重复代码
2. **设计一致性达到95%**: 统一按钮、布局、间距、字体规范
3. **可维护性提升**: 修改通用功能只需改动1个组件文件
4. **1920x1080优化**: 针对主流分辨率优化视觉体验
5. **符合Constitution**: 不引入技术黑名单中的技术（第三方UI库）

### 1.3 项目范围

**包含范围**:
- 6个管理界面的UI组件提取与统一
- 通用工具栏、表格、分页、状态标签组件开发
- 排版规范（字体、间距、颜色）制定与应用
- 处方管理界面的过时设计改造
- 患者管理界面的Phase 2功能补全

**不包含范围**:
- 业务逻辑改动（仅限UI层面）
- 新增管理界面
- 跨模块的架构重构
- 第三方UI库引入（如ModernWPF、MaterialDesignInXAML等）

---

## 2. 功能需求

### FR-01: 通用组件提取

**优先级**: P0（必须）
**需求描述**: 提取并实现8个核心通用组件，满足6个管理界面的通用需求

#### FR-01.1 UnifiedManagementToolBar（工具栏组件）

**功能**:
- 支持搜索框绑定（SearchText、SearchCommand）
- 支持自定义筛选区域插槽（FilterContent）
- 支持操作按钮集合插槽（ActionButtons）
- 应用UnifiedDesignSystem.xaml的ToolBarContainer样式

**接口定义**:
```xaml
<local:UnifiedManagementToolBar
    SearchText="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
    SearchCommand="{Binding SearchCommand}"
    SearchPlaceholder="请输入关键字搜索..."
    SearchTooltip="支持按名称、编号等多字段搜索">

    <!-- 筛选区插槽 -->
    <local:UnifiedManagementToolBar.FilterContent>
        <ComboBox ItemsSource="{Binding StatusList}" />
        <DatePicker SelectedDate="{Binding StartDate}" />
    </local:UnifiedManagementToolBar.FilterContent>

    <!-- 操作按钮插槽 -->
    <local:UnifiedManagementToolBar.ActionButtons>
        <Button Content="➕ 新增" Command="{Binding AddCommand}" Style="{StaticResource PrimaryButton}" />
        <Button Content="🔄 刷新" Command="{Binding RefreshCommand}" Style="{StaticResource SecondaryButton}" />
    </local:UnifiedManagementToolBar.ActionButtons>
</local:UnifiedManagementToolBar>
```

**属性规格**:
| 属性名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| SearchText | string | 是 | "" | 搜索文本双向绑定 |
| SearchCommand | ICommand | 是 | null | 搜索命令 |
| SearchPlaceholder | string | 否 | "搜索..." | 搜索框占位符 |
| SearchTooltip | string | 否 | "" | 搜索框工具提示 |
| FilterContent | UIElement | 否 | null | 筛选区域内容 |
| ActionButtons | IEnumerable<Button> | 否 | null | 操作按钮集合 |

#### FR-01.2 UnifiedManagementTable（数据表格组件）

**功能**:
- 支持数据源绑定（ItemsSource）
- 支持单选/多选（SelectedItem、SelectedItems）
- 支持自定义列定义（Columns）
- 支持行级操作按钮集合（RowActions）
- 应用UnifiedDesignSystem.xaml的BaseDataGrid样式

**接口定义**:
```xaml
<local:UnifiedManagementTable
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem}"
    AutoGenerateColumns="False">

    <!-- 行级操作集合 -->
    <local:UnifiedManagementTable.RowActions>
        <local:RowActionButton Label="查看" Command="{Binding ViewDetailsCommand}" StyleKey="InfoButton" />
        <local:RowActionButton Label="编辑" Command="{Binding EditCommand}" StyleKey="SuccessButton" />
        <local:RowActionButton Label="删除" Command="{Binding DeleteCommand}" StyleKey="DangerButton" />
    </local:UnifiedManagementTable.RowActions>

    <!-- 自定义列 -->
    <local:UnifiedManagementTable.Columns>
        <DataGridTextColumn Header="姓名" Binding="{Binding Name}" Width="120" />
        <DataGridTextColumn Header="手机号" Binding="{Binding Phone}" Width="140" />
    </local:UnifiedManagementTable.Columns>
</local:UnifiedManagementTable>
```

**属性规格**:
| 属性名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| ItemsSource | IEnumerable | 是 | null | 数据源 |
| SelectedItem | object | 否 | null | 选中项 |
| SelectedItems | ObservableCollection | 否 | null | 多选项集合 |
| RowActions | IEnumerable<RowActionButton> | 否 | null | 行级操作集合 |
| Columns | DataGridColumn[] | 否 | null | 列定义 |
| AutoGenerateColumns | bool | 否 | false | 是否自动生成列 |

#### FR-01.3 UnifiedStatusBadge（状态标签组件）

**功能**:
- 支持枚举值绑定
- 支持自定义背景色/前景色
- 支持枚举描述转换器
- 圆角、内边距符合设计规范

**接口定义**:
```xaml
<local:UnifiedStatusBadge
    Status="{Binding Status}"
    Converter="{StaticResource EnumDescriptionConverter}"
    BackgroundColor="#10B981"
    ForegroundColor="White" />
```

**属性规格**:
| 属性名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| Status | Enum | 是 | null | 状态值 |
| Converter | IValueConverter | 否 | null | 枚举描述转换器 |
| BackgroundColor | Brush | 否 | #10B981 | 背景色 |
| ForegroundColor | Brush | 否 | White | 前景色 |
| CornerRadius | CornerRadius | 否 | {StaticResource CornerRadiusSmall} | 圆角半径 |
| Padding | Thickness | 否 | 12,6 | 内边距 |

#### FR-01.4 UnifiedPaginationBar（分页控件组件）

**功能**:
- 支持当前页/总页数显示
- 支持首页、上一页、下一页、末页命令
- 支持可选显示首页/末页按钮（适配Phase 2限制）
- 应用UnifiedDesignSystem.xaml的分页样式

**接口定义**:
```xaml
<local:UnifiedPaginationBar
    CurrentPage="{Binding CurrentPage}"
    TotalPages="{Binding TotalPages}"
    FirstPageCommand="{Binding FirstPageCommand}"
    PreviousPageCommand="{Binding PreviousPageCommand}"
    NextPageCommand="{Binding NextPageCommand}"
    LastPageCommand="{Binding LastPageCommand}"
    ShowFirstLast="True" />
```

**属性规格**:
| 属性名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| CurrentPage | int | 是 | 1 | 当前页码 |
| TotalPages | int | 是 | 1 | 总页数 |
| FirstPageCommand | ICommand | 否 | null | 首页命令 |
| PreviousPageCommand | ICommand | 是 | null | 上一页命令 |
| NextPageCommand | ICommand | 是 | null | 下一页命令 |
| LastPageCommand | ICommand | 否 | null | 末页命令 |
| ShowFirstLast | bool | 否 | true | 是否显示首页/末页按钮 |

#### FR-01.5 UnifiedStatusBar（底部状态栏组件）

**功能**:
- 支持左侧状态摘要显示
- 支持右侧内容插槽（通常放置分页控件）
- 应用UnifiedDesignSystem.xaml的StatusBarContainer样式

**接口定义**:
```xaml
<local:UnifiedStatusBar
    StatusMessage="{Binding StatusMessage}"
    LeftContent="{Binding CustomSummary}"
    RightContent="{local:UnifiedPaginationBar ...}" />
```

**属性规格**:
| 属性名 | 类型 | 必填 | 默认值 | 说明 |
|-------|------|------|--------|------|
| StatusMessage | string | 否 | "" | 默认状态消息 |
| LeftContent | UIElement | 否 | null | 左侧内容 |
| RightContent | UIElement | 否 | null | 右侧内容（通常为分页控件） |

#### FR-01.6 RowActionButton（行级操作按钮）

**功能**:
- 支持标签、命令、样式键配置
- 支持工具提示
- 支持分隔符显示
- 支持内边距配置

**接口定义**:
```csharp
public class RowActionButton
{
    public string Label { get; set; }           // 按钮文本
    public ICommand Command { get; set; }       // 绑定命令
    public string StyleKey { get; set; }        // 样式资源键（如"SuccessButton"）
    public int Padding { get; set; } = 8;       // 内边距
    public bool ShowDivider { get; set; }       // 是否显示分隔符
    public string ToolTip { get; set; }         // 工具提示
}
```

#### FR-01.7 FilterPanel（高级筛选面板）

**功能**:
- 支持动态筛选控件集合
- 支持ComboBox、DatePicker等标准筛选控件
- 支持筛选条件重置
- 支持筛选条件持久化（可选）

**接口定义**:
```xaml
<local:FilterPanel>
    <local:FilterDefinition Label="状态筛选" Type="ComboBox" ItemsSource="{Binding StatusList}" />
    <local:FilterDefinition Label="开始日期" Type="DatePicker" SelectedDate="{Binding StartDate}" />
    <local:FilterDefinition Label="结束日期" Type="DatePicker" SelectedDate="{Binding EndDate}" />
</local:FilterPanel>
```

#### FR-01.8 SpecialActionButtons（特殊操作按钮集合）

**功能**:
- 支持导入/导出等特殊操作
- 支持分隔符
- 支持图标+文本
- 应用统一样式

**接口定义**:
```xaml
<local:SpecialActionButtons>
    <local:ActionButton Icon="📥" Label="导入" Command="{Binding ImportCommand}" />
    <Separator />
    <local:ActionButton Icon="📤" Label="导出" Command="{Binding ExportCommand}">
        <local:ActionButton.SubActions>
            <MenuItem Header="导出为Excel" Command="{Binding ExportExcelCommand}" />
            <MenuItem Header="导出为CSV" Command="{Binding ExportCsvCommand}" />
            <MenuItem Header="导出为PDF" Command="{Binding ExportPdfCommand}" />
        </local:ActionButton.SubActions>
    </local:ActionButton>
</local:SpecialActionButtons>
```

### FR-02: 界面改造需求

#### FR-02.1 处方管理界面改造

**优先级**: P0（必须）
**问题现状**:
- 使用过时的ToolBarTray和ToolBar控件
- 使用过时的MaterialDesignFlatButton样式
- 未完全继承UnifiedDesignSystem.xaml

**改造要求**:
- [ ] 替换ToolBarTray为UnifiedManagementToolBar组件
- [ ] 替换MaterialDesignFlatButton为统一样式（PrimaryButton、SuccessButton等）
- [ ] 应用UnifiedStatusBar替换原生StatusBar
- [ ] 验证所有功能无回归

#### FR-02.2 患者管理界面补全

**优先级**: P1（应该）
**问题现状**:
- Phase 2仅实现删除功能，缺少查看/编辑
- 分页仅提供上一页/下一页，缺少首页/末页

**改造要求**:
- [ ] 补全查看、编辑功能（ViewModel + View）
- [ ] 补全首页、末页分页功能
- [ ] 使用UnifiedPaginationBar组件

#### FR-02.3 其他4个界面验证

**优先级**: P1（应该）
**界面**: 用户、中药材、病案、验方管理

**验证要求**:
- [ ] 迁移至新组件体系
- [ ] 验证UI一致性（按钮、间距、字体）
- [ ] 验证功能无回归
- [ ] 代码复用率提升至40%以上

---

## 3. 非功能需求

### NFR-01: 性能要求

**NFR-01.1 响应时间**
- 搜索操作响应时间 ≤ 500ms
- 分页切换响应时间 ≤ 300ms
- 首次加载时间 ≤ 2s

**NFR-01.2 资源占用**
- 单个管理界面内存占用 ≤ 50MB
- DataGrid渲染1000行数据流畅度 ≥ 60fps

### NFR-02: 可用性要求

**NFR-02.1 1920x1080分辨率优化**
- 所有管理界面在1920x1080分辨率下无需水平/垂直滚动即可看到关键操作区
- 字体大小适中，易于阅读（Body 14/20 epx）
- 按钮点击区域 ≥ 32x32 epx

**NFR-02.2 键盘导航**
- 支持Tab键导航所有可交互元素
- 搜索框支持Enter键触发搜索
- 数据表格支持方向键导航

**NFR-02.3 无障碍支持**
- 所有交互控件提供AutomationProperties.Name
- 高对比度模式下文字清晰可读
- 屏幕阅读器可正确读取状态信息

### NFR-03: 可维护性要求

**NFR-03.1 代码复用**
- 通用UI结构代码复用率 ≥ 60%
- 修改通用组件功能时，改动文件数 ≤ 1个

**NFR-03.2 可扩展性**
- 新增管理界面时，可直接使用组件库，开发时间 ≤ 2小时
- 组件支持通过Properties和Behaviors扩展，无需修改源码

**NFR-03.3 可测试性**
- 所有组件提供单元测试覆盖率 ≥ 80%
- 提供设计文档和使用示例

### NFR-04: 兼容性要求

**NFR-04.1 .NET版本**
- 目标框架: .NET 8.0
- 最低运行环境: Windows 10 1809+

**NFR-04.2 浏览器兼容性**
- 不适用（Desktop应用）

---

## 4. 设计约束

### DC-01: Constitution技术黑名单

**禁止技术**（MVP阶段）:
- ❌ 第三方UI库: ModernWPF, MaterialDesignInXAML, MahApps.Metro等
- ❌ 第三方主题控件: HandyControl, Kino.Toolkit.Wpf等
- ❌ 组件库: Telerik, DevExpress, Syncfusion等商业组件

**允许技术**:
- ✅ WPF原生控件（DataGrid, Button, TextBox等）
- ✅ Prism框架（已引入）
- ✅ 自定义UserControl和Behavior
- ✅ UnifiedDesignSystem.xaml样式资源

### DC-02: 架构约束

**三层架构对齐**:
- Desktop端遵循MVVM架构
- ViewModel继承UnifiedListViewModelBase<T>
- View仅负责UI渲染，不包含业务逻辑

**依赖注入规范**:
- 组件通过Prism的IContainerRegistry注册
- 组件仅接受构造函数注入

### DC-03: 文件组织约束

**组件位置**:
- 组件源码: `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Components/`
- 组件样式: `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Themes/UnifiedComponents.xaml`
- 单元测试: `tests/UnitTests/Client/LYBT.Desktop.Infrastructure.Tests/Components/`

**命名规范**:
- 组件类名: `Unified{功能}Component` 或 `Unified{功能}`
- 样式ResourceKey: `Unified{功能}Style`
- 命名空间: `LYBT.Desktop.Infrastructure.Components`

---

## 5. 排版与间距规范（1920x1080优化）

### 5.1 字体规范（Type Ramp）

**基于WPF最佳实践的字体大小系统**（单位: epx - Effective Pixels）

| 级别 | 字号/行高 | 字重 | 使用场景 | 示例 |
|------|----------|------|---------|------|
| Caption | 12/16 epx | Regular | 辅助性文本、时间戳 | "创建于 2025-11-06 14:30" |
| Body | 14/20 epx | Regular | 正文、表格内容、标签 | DataGrid单元格内容 |
| Body Strong | 14/20 epx | SemiBold | 强调性正文 | 搜索框占位符 |
| Subtitle | 20/28 epx | SemiBold | 分组标题、卡片标题 | "筛选条件" |
| Title | 28/36 epx | SemiBold | 页面标题、模块标题 | "用户管理" |
| Display | 40/52 epx | SemiBold | 特大标题（少用） | Dashboard数据展示 |

**字体族**:
- 优先使用: `Segoe UI Variable`（Windows 11）
- 回退字体: `Segoe UI`（Windows 10）
- 中文字体: `Microsoft YaHei UI`

**ResourceDictionary定义示例**:
```xaml
<FontFamily x:Key="PrimaryFontFamily">Segoe UI Variable, Segoe UI, Microsoft YaHei UI</FontFamily>
<sys:Double x:Key="FontSizeCaption">12</sys:Double>
<sys:Double x:Key="FontSizeBody">14</sys:Double>
<sys:Double x:Key="FontSizeSubtitle">20</sys:Double>
<sys:Double x:Key="FontSizeTitle">28</sys:Double>
```

### 5.2 间距规范（4 epx增量规则）

**基本原则**: 所有间距（Margin、Padding、Gutter）必须是4的倍数

#### 间距标准值定义

| 名称 | 值 | 使用场景 |
|------|------|---------|
| SpacingXSmall | 4 epx | 行内元素间隙（按钮之间） |
| SpacingSmall | 8 epx | 相关控件间隙（标签+输入框） |
| SpacingMedium | 12 epx | 模块内部间隙（搜索区+操作区） |
| SpacingLarge | 16 epx | 模块之间间隙（工具栏+表格） |
| SpacingXLarge | 24 epx | 容器边缘到内容的距离 |
| SpacingXXLarge | 32 epx | 大板块间隙（少用） |

**ResourceDictionary定义示例**:
```xaml
<Thickness x:Key="SpacingXSmall">4</Thickness>
<Thickness x:Key="SpacingSmall">8</Thickness>
<Thickness x:Key="SpacingMedium">12</Thickness>
<Thickness x:Key="SpacingLarge">16</Thickness>
<Thickness x:Key="SpacingXLarge">24</Thickness>
```

#### 组件级间距应用

**工具栏内部间距**:
- Border.Padding: 12 epx（SpacingMedium）
- 搜索框与筛选控件间隙: 8 epx（SpacingSmall）
- 操作按钮之间间隙: 4 epx（SpacingXSmall）

**DataGrid间距**:
- 列头Padding: 8 epx（SpacingSmall）
- 单元格Padding: 8 epx（SpacingSmall）
- 行高: 40 epx（确保触摸友好）

**分页控件间距**:
- 按钮之间间隙: 4 epx（SpacingXSmall）
- 页码显示区Padding: 8 epx（SpacingSmall）

**状态标签间距**:
- Padding: 12,6 epx（水平12、垂直6）
- CornerRadius: 4 epx

### 5.3 布局规范（1920x1080优化）

#### Grid布局优先级

**推荐**: Grid > StackPanel > WrapPanel
**禁止**: Canvas（仅特殊情况使用）

**标准三行布局定义**:
```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />      <!-- 工具栏，固定高度~80 epx -->
        <RowDefinition Height="*" />         <!-- 数据区，自适应 -->
        <RowDefinition Height="Auto" />      <!-- 状态栏，固定高度~48 epx -->
    </Grid.RowDefinitions>
</Grid>
```

#### 1920x1080分辨率布局计算

**可用区域**（减去窗口边框和标题栏）:
- 宽度: ~1880 epx
- 高度: ~1010 epx

**区域分配**:
- 工具栏: 80 epx（高度）
- 数据区: 882 epx（高度） = 1010 - 80 - 48
- 状态栏: 48 epx（高度）

**DataGrid列宽设计原则**（总宽度1880 epx）:
- 操作列（固定）: 200-280 epx
- 状态列（固定）: 80-100 epx
- 其他列: 按权重分配剩余宽度，使用`Width="*"`或`Width="2*"`
- 禁止所有列固定宽度（会导致水平滚动条）

**示例**（用户管理界面）:
```xaml
<DataGrid.Columns>
    <DataGridTextColumn Header="用户名" Width="*" />       <!-- 权重1 -->
    <DataGridTextColumn Header="真实姓名" Width="1.5*" />  <!-- 权重1.5 -->
    <DataGridTextColumn Header="角色" Width="100" />       <!-- 固定100 -->
    <DataGridTextColumn Header="手机号" Width="140" />     <!-- 固定140 -->
    <DataGridTextColumn Header="邮箱" Width="2*" />        <!-- 权重2 -->
    <DataGridTemplateColumn Header="操作" Width="200" />   <!-- 固定200 -->
</DataGrid.Columns>
```

**计算**:
- 固定宽度总和: 100 + 140 + 200 = 440 epx
- 剩余宽度: 1880 - 440 = 1440 epx
- 权重总和: 1 + 1.5 + 2 = 4.5
- 用户名列: 1440 × (1/4.5) ≈ 320 epx
- 真实姓名列: 1440 × (1.5/4.5) ≈ 480 epx
- 邮箱列: 1440 × (2/4.5) ≈ 640 epx

### 5.4 颜色规范

**继承UnifiedDesignSystem.xaml的颜色方案**，不做修改：

| 用途 | 资源键 | 十六进制值 | 使用场景 |
|------|--------|-----------|---------|
| 背景色 | BackgroundBrush | #F8F9FA | 页面背景 |
| 表面色 | SurfaceBrush | #FFFFFF | 卡片、Border背景 |
| 边框色 | BorderBrush | #E9ECEF | Border边框 |
| 主色 | PrimaryBrush | #10B981 | 主要操作按钮 |
| 成功色 | SuccessBrush | #059669 | 编辑按钮 |
| 警告色 | WarningBrush | #F59E0B | 警告按钮 |
| 危险色 | DangerBrush | #DC2626 | 删除按钮 |
| 信息色 | InfoBrush | #3B82F6 | 信息按钮 |
| 次要色 | SecondaryBrush | #6B7280 | 次要操作按钮 |

---

## 6. 成功标准

### 6.1 代码质量标准

**代码复用率**:
- 通用UI结构代码复用率 ≥ 60%（当前~25%）
- 相似功能模块（搜索、分页）代码复用率 ≥ 80%

**代码行数减少**:
- 6个管理界面总代码行数减少 ≥ 40%
- 单个界面XAML代码行数 ≤ 150行

### 6.2 设计一致性标准

**视觉一致性评分**: ≥ 95%

**评分细则**（每项10分，总分100分）:
- [ ] 工具栏结构一致（高度、内边距、布局）
- [ ] 搜索框样式一致（字体、占位符、图标）
- [ ] 操作按钮样式一致（颜色、圆角、间距）
- [ ] DataGrid样式一致（行高、列头、边框）
- [ ] 分页控件样式一致（按钮、页码显示）
- [ ] 状态标签样式一致（背景色、圆角、字体）
- [ ] 间距符合4 epx规则（100%符合）
- [ ] 字体大小符合Type Ramp（100%符合）
- [ ] 1920x1080无滚动条（关键操作可见）
- [ ] 无过时样式引用（MaterialDesignFlatButton等）

### 6.3 性能标准

**响应时间**:
- 搜索操作: ≤ 500ms（当前~800ms）
- 分页切换: ≤ 300ms（当前~500ms）
- 首次加载: ≤ 2s（当前~3s）

**资源占用**:
- 单界面内存占用: ≤ 50MB（当前~60MB）
- 组件渲染时间: ≤ 16ms（60fps）

### 6.4 功能完整性标准

**界面改造验收**:
- [ ] 处方管理界面完全迁移，无过时样式
- [ ] 患者管理界面补全查看/编辑/首页/末页功能
- [ ] 其他4个界面迁移至新组件体系
- [ ] 所有界面通过手动测试，无功能回归

**组件库验收**:
- [ ] 8个核心组件全部实现
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 提供组件使用文档和示例

### 6.5 可维护性标准

**新增界面成本**:
- 使用组件库开发新管理界面时间 ≤ 2小时（当前~8小时）

**修改通用功能成本**:
- 修改工具栏/分页等通用功能时，改动文件数 ≤ 1个（当前~6个）

---

## 7. 验收标准

### 7.1 Phase 1验收（第1-2周）

**交付物**:
- [ ] UnifiedManagementToolBar组件（含单元测试）
- [ ] UnifiedManagementTable组件（含单元测试）
- [ ] UnifiedStatusBadge组件（含单元测试）
- [ ] UnifiedPaginationBar组件（含单元测试）
- [ ] 组件使用文档（README.md）

**验收标准**:
- [ ] 编译通过，0 errors, 0 warnings
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 手动测试通过（Demo界面可正常运行）
- [ ] 代码审查通过（Code Review）

### 7.2 Phase 2验收（第2-3周）

**交付物**:
- [ ] 处方管理界面改造完成
- [ ] 患者管理界面补全完成
- [ ] 用户管理界面迁移至新组件
- [ ] 中药材管理界面迁移至新组件
- [ ] 病案管理界面迁移至新组件
- [ ] 验方管理界面迁移至新组件

**验收标准**:
- [ ] 所有界面编译通过，0 errors, 0 warnings
- [ ] 所有界面手动测试通过，无功能回归
- [ ] 代码复用率 ≥ 50%（中期目标）
- [ ] 视觉一致性评分 ≥ 85%

### 7.3 Phase 3验收（第3-4周）

**交付物**:
- [ ] UnifiedStatusBar组件（含单元测试）
- [ ] FilterPanel组件（含单元测试）
- [ ] SpecialActionButtons组件（含单元测试）
- [ ] RowActionButton集合框架（含单元测试）
- [ ] 组件库完整文档

**验收标准**:
- [ ] 所有8个组件全部实现
- [ ] 代码复用率 ≥ 60%
- [ ] 视觉一致性评分 ≥ 95%
- [ ] 性能标准全部达标
- [ ] 文档完整且准确

---

## 8. 风险与缓解措施

### 风险1: 组件抽象过度，灵活性不足

**风险等级**: 中
**影响**: 特殊功能界面无法使用通用组件

**缓解措施**:
- 提供充足的插槽（Slot）和模板（Template）机制
- 支持通过AttachedProperty扩展组件功能
- 保留"逃生舱口"机制（允许界面不使用组件，直接使用原生控件）

### 风险2: 改造导致功能回归

**风险等级**: 高
**影响**: 用户无法使用已有功能

**缓解措施**:
- 每个界面改造后必须通过手动测试清单
- 提供单元测试覆盖关键业务逻辑
- 使用Git分支隔离改造工作，改造完成后再合并主分支
- Phase 2优先改造处方管理（问题最严重）和患者管理（功能最简单），建立信心

### 风险3: 性能下降

**风险等级**: 低
**影响**: 组件嵌套层级增加，渲染性能下降

**缓解措施**:
- 组件内部避免过深的视觉树嵌套
- 使用VirtualizingStackPanel优化DataGrid渲染
- 提供性能测试（渲染1000行数据的帧率）
- 必要时使用WPF性能分析工具（PerfView）

### 风险4: 设计系统不完善

**风险等级**: 中
**影响**: UnifiedDesignSystem.xaml缺少必要的样式资源

**缓解措施**:
- Phase 1先补全UnifiedDesignSystem.xaml的样式资源
- 定义完整的Type Ramp和Spacing System
- 提供样式资源查询文档

---

## 9. 附录

### 附录A: 参考资料

**WPF官方文档**:
- [WPF Layout](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout)
- [WPF Typography](https://learn.microsoft.com/en-us/windows/apps/design/style/typography)
- [WPF Spacing](https://learn.microsoft.com/en-us/windows/apps/design/layout/layout-spacing)

**项目内部文档**:
- Constitution: `.spec-workflow/steering/constitution.md`
- 架构指南: `docs/explanation/architecture/client/README.md`
- MVVM规范: `docs/explanation/architecture/client/mvvm-guide.md`

### 附录B: 术语表

| 术语 | 定义 |
|------|------|
| epx | Effective Pixels，有效像素，DPI无关的逻辑像素单位 |
| Type Ramp | 字体大小阶梯系统，定义了不同层级的字体大小和行高 |
| 4 epx增量规则 | 所有间距必须是4的倍数，确保像素完美对齐 |
| UnifiedDesignSystem | 项目统一设计系统，定义了颜色、字体、样式资源 |
| Constitution | 项目宪法，定义了技术栈约束和MVP原则 |
| Phase 2 | 项目迭代阶段标识，表示功能未完全实现 |

### 附录C: 组件优先级矩阵

| 组件 | 重复代码占比 | 实现难度 | 优先级 | 计划Phase |
|------|-------------|---------|--------|----------|
| UnifiedManagementToolBar | 30% | 中 | P0 | Phase 1 |
| UnifiedManagementTable | 25% | 高 | P0 | Phase 1 |
| UnifiedPaginationBar | 15% | 低 | P0 | Phase 1 |
| UnifiedStatusBadge | 10% | 低 | P0 | Phase 1 |
| UnifiedStatusBar | 8% | 中 | P1 | Phase 3 |
| RowActionButton集合 | 7% | 中 | P1 | Phase 3 |
| FilterPanel | 3% | 高 | P1 | Phase 3 |
| SpecialActionButtons | 2% | 低 | P2 | Phase 3 |

---

**文档状态**: ✅ 待用户确认
**下一步**: 创建设计文档 `docs/explanation/design/ui-unification-design.md`
**关联Issue**: 待创建
**最后更新**: 2025-11-06
