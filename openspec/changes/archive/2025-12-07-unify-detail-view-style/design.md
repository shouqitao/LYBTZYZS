# Design: unify-detail-view-style

## 设计原则

### 核心目标
1. **FHD优先**：主适配分辨率为1920x1080 (Full HD)
2. **一页显示**：详情页内容在FHD分辨率下无需滚动即可完整展示
3. **布局灵活**：根据内容复杂度灵活使用2-4列布局
4. **操作流畅**：编辑按钮放置右上角，减少页面跳转

### 参考案例
- **Master-Detail UI模式**：Side-by-Side风格，列表与详情同页展示
- **医疗管理系统最佳实践**：信息分组、卡片式布局、清晰的视觉层次
- **WPF MVVM架构**：数据绑定、命令模式、样式复用

---

## 1. 统一布局规范

### 1.1 FHD分辨率下的空间分配

```
┌─────────────────────────────────────────────────────────────────┐
│ 工具栏高度: 56px                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ 内容区域高度: ~920px (1080 - 56 - 56 - 48边距)                  │
│                                                                 │
│ 可用宽度: ~1840px (1920 - 40左 - 40右边距)                      │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ 底部操作栏高度: 56px                                            │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 标准详情页结构（无滚动设计）

```xml
<UserControl>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="56" />   <!-- 顶部工具栏：固定高度 -->
            <RowDefinition Height="*" />    <!-- 内容区域：自适应填充 -->
            <RowDefinition Height="56" />   <!-- 底部操作栏：固定高度 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 顶部工具栏 -->
        <Border Grid.Row="0" Style="{StaticResource DetailViewToolbarStyle}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />  <!-- 返回按钮 -->
                    <ColumnDefinition Width="*" />     <!-- 标题 -->
                    <ColumnDefinition Width="Auto" />  <!-- 编辑按钮 -->
                </Grid.ColumnDefinitions>

                <Button Grid.Column="0" Content="返回" Command="{Binding BackCommand}" />
                <TextBlock Grid.Column="1" Text="页面标题" />
                <!-- 关键：编辑按钮放置右上角 -->
                <Button Grid.Column="2" Content="编辑" Command="{Binding EditCommand}" />
            </Grid>
        </Border>

        <!-- Row 1: 内容区域（无滚动） -->
        <Grid Grid.Row="1" Style="{StaticResource DetailViewContentStyle}">
            <!-- 表单内容：灵活多列布局 -->
        </Grid>

        <!-- Row 2: 底部操作栏 -->
        <Border Grid.Row="2" Style="{StaticResource DetailViewFooterStyle}">
            <Grid>
                <TextBlock Text="最后更新时间" HorizontalAlignment="Left" />
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button Content="刷新" />
                    <Button Content="关闭" />
                </StackPanel>
            </Grid>
        </Border>

        <!-- Loading遮罩 -->
        <Grid Grid.RowSpan="3" Style="{StaticResource LoadingOverlayStyle}" ... />
    </Grid>
</UserControl>
```

### 1.3 灵活多列表单布局

根据字段数量和内容复杂度，灵活选择列数：

#### 2列布局（字段少，内容简单）
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- 每行2个字段 -->
    <StackPanel Grid.Column="0">
        <TextBlock Text="姓名" Style="{StaticResource FormLabelStyle}" />
        <TextBlock Text="{Binding Name}" Style="{StaticResource FormValueStyle}" />
    </StackPanel>
    <StackPanel Grid.Column="1">
        <TextBlock Text="性别" Style="{StaticResource FormLabelStyle}" />
        <TextBlock Text="{Binding Gender}" Style="{StaticResource FormValueStyle}" />
    </StackPanel>
</Grid>
```

#### 3列布局（字段中等）
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <!-- 每行3个字段 -->
</Grid>
```

#### 4列布局（字段多，内容紧凑）
```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <!-- 每行4个字段，适合简短信息如：姓名、性别、年龄、手机 -->
</Grid>
```

### 1.4 各实体详情页布局建议

| 实体 | 推荐列数 | 字段数 | 说明 |
|------|----------|--------|------|
| Patient | 4列 | 6-8个 | 姓名/性别/年龄/手机/身份证/地址 |
| User | 4列 | 6-8个 | 用户名/真实姓名/角色/手机/邮箱/状态 |
| Herb | 3列 | 8-10个 | 名称/拼音/产地/规格/单位/单价/功效/状态 |
| Formula | 3列 | 6-8个 | 名称/分类/功效/来源/药材数/状态 + 药材列表 |
| MedicalCase | 4列卡片 | 复杂 | 保持卡片式，多区域展开 |

---

## 2. 共享样式定义

### 2.1 添加到UnifiedComponents.xaml

```xml
<!-- ========== 详情页工具栏样式 ========== -->
<Style x:Key="DetailViewToolbarStyle" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource PrimaryHueMidBrush}" />
    <Setter Property="Height" Value="56" />
    <Setter Property="Padding" Value="16,0" />
</Style>

<Style x:Key="DetailViewTitleStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="20" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="VerticalAlignment" Value="Center" />
</Style>

<!-- 工具栏按钮（返回/编辑）-->
<Style x:Key="DetailViewToolbarButtonStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Cursor" Value="Hand" />
    <Setter Property="VerticalAlignment" Value="Center" />
</Style>

<!-- 编辑按钮特殊样式（右上角醒目） -->
<Style x:Key="DetailViewEditButtonStyle" TargetType="Button"
       BasedOn="{StaticResource DetailViewToolbarButtonStyle}">
    <Setter Property="Background" Value="#20FFFFFF" />
    <Setter Property="Padding" Value="20,8" />
    <Setter Property="FontWeight" Value="SemiBold" />
</Style>

<!-- ========== 详情页内容区域样式 ========== -->
<Style x:Key="DetailViewContentStyle" TargetType="Grid">
    <Setter Property="Margin" Value="24,20" />
</Style>

<!-- ========== 详情页底部操作栏样式 ========== -->
<Style x:Key="DetailViewFooterStyle" TargetType="Border">
    <Setter Property="Background" Value="{StaticResource SurfaceBrush}" />
    <Setter Property="BorderThickness" Value="0,1,0,0" />
    <Setter Property="BorderBrush" Value="{StaticResource DividerBrush}" />
    <Setter Property="Height" Value="56" />
    <Setter Property="Padding" Value="24,0" />
</Style>

<!-- ========== 表单样式 ========== -->
<!-- 表单字段容器 -->
<Style x:Key="FormFieldStyle" TargetType="StackPanel">
    <Setter Property="Margin" Value="0,0,24,16" />
</Style>

<!-- 表单标签 -->
<Style x:Key="FormLabelStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}" />
    <Setter Property="Margin" Value="0,0,0,6" />
</Style>

<!-- 表单值 -->
<Style x:Key="FormValueStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Foreground" Value="{StaticResource TextPrimaryBrush}" />
    <Setter Property="TextWrapping" Value="Wrap" />
</Style>

<!-- ========== 加载遮罩样式 ========== -->
<Style x:Key="LoadingOverlayStyle" TargetType="Grid">
    <Setter Property="Background" Value="#80000000" />
</Style>
```

---

## 3. 列表页操作列调整

### 3.1 移除编辑按钮

**核心改动：列表页操作列移除"编辑"按钮**

用户工作流优化：
```
旧流程: 列表 → 编辑（直接进入编辑模式，无法预览）
       或: 列表 → 查看 → 返回列表 → 编辑（繁琐）

新流程: 列表 → 查看 → 编辑（右上角按钮，无需返回）
```

### 3.2 修改前后对比

**PatientManagementView 修改前**：
```xml
<StackPanel Orientation="Horizontal">
    <Button Content="查看" ... />
    <Button Content="编辑" ... />   <!-- 移除 -->
    <Button Content="记录" ... />
    <Button Content="删除" ... />
</StackPanel>
```

**PatientManagementView 修改后**：
```xml
<StackPanel Orientation="Horizontal">
    <Button Content="查看" ... />
    <Button Content="记录" ... />
    <Button Content="删除" ... />
</StackPanel>
```

### 3.3 操作列宽度调整

| 视图 | 当前宽度 | 调整后宽度 | 按钮变化 |
|------|----------|------------|----------|
| PatientManagementView | 280 | 200 | 4→3 |
| HerbManagementView | 360 | 280 | 5→4 |
| UserManagementView | 420 | 340 | 6→5 |
| FormulaManagementView | 360 | 280 | 5→4 |
| MedicalCaseManagementView | 460 | 380 | 7→6 |

---

## 4. 各详情页具体设计

### 4.1 PatientDetailView（患者详情）

```
┌─────────────────────────────────────────────────────────────────┐
│ [←返回]  患者详情 - 张三                              [编辑]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  姓名: 张三        性别: 男          年龄: 45岁      手机: ... │
│  身份证: ...       地址: ...                                   │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  就诊次数: 12次    首次就诊: 2024-01-15    最近就诊: 2024-12-01│
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ 最后更新: 2024-12-06 14:30                    [刷新]  [关闭]   │
└─────────────────────────────────────────────────────────────────┘
```

**布局**: 4列，2行基本信息 + 1行就诊统计

### 4.2 UserDetailView（用户详情）

```
┌─────────────────────────────────────────────────────────────────┐
│ [←返回]  用户详情 - admin                             [编辑]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  用户名: admin     真实姓名: 管理员    角色: 系统管理员        │
│  手机: 138...      邮箱: admin@...     状态: [启用]            │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  创建时间: 2024-01-01    最后登录: 2024-12-06 10:30            │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ 最后更新: 2024-12-06 14:30            [重置密码] [刷新] [关闭] │
└─────────────────────────────────────────────────────────────────┘
```

**布局**: 3-4列，紧凑显示

### 4.3 HerbDetailView（药材详情）

```
┌─────────────────────────────────────────────────────────────────┐
│ [←返回]  药材详情 - 黄芪                              [编辑]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  药材名称: 黄芪       拼音码: HQ         产地: 甘肃            │
│  规格: 统货          单位: 克           单价: 0.15元           │
│  状态: [启用]                                                   │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  功效: 补气升阳，益卫固表，利水消肿，托疮生肌                   │
│  ─────────────────────────────────────────────────────────────  │
│  备注: 甘肃道地药材，品质优良                                   │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ 最后更新: 2024-12-06 14:30                    [刷新]  [关闭]   │
└─────────────────────────────────────────────────────────────────┘
```

**布局**: 3列基本信息 + 全宽功效/备注

### 4.4 FormulaDetailView（验方详情）

```
┌─────────────────────────────────────────────────────────────────┐
│ [←返回]  验方详情 - 四君子汤                          [编辑]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  验方名称: 四君子汤   分类: 补益剂     来源: 《太平惠民和剂局方》│
│  药材数: 4味         状态: [启用]                               │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  功效: 益气健脾                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  组成药材:                                                      │
│  ┌──────────┬────────┬────────┐                                │
│  │ 人参 9g  │ 白术 9g │ 茯苓 9g │ 炙甘草 6g                    │
│  └──────────┴────────┴────────┘                                │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ 最后更新: 2024-12-06 14:30               [复制]  [刷新] [关闭] │
└─────────────────────────────────────────────────────────────────┘
```

**布局**: 3列基本信息 + 药材卡片网格

### 4.5 MedicalCaseDetailView（医案详情）

医案详情较为复杂，保持卡片式布局但优化空间使用：

```
┌─────────────────────────────────────────────────────────────────┐
│ [←返回]  医案详情 - MC202412060001              [诊疗] [编辑]  │
├─────────────────────────────────────────────────────────────────┤
│ ┌─基本信息────────────────────────────────────────────────────┐ │
│ │ 患者: 张三    医生: 李医生    创建: 2024-12-06   状态: 进行中│ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─诊疗信息 ▼──────────────────────────────────────────────────┐ │
│ │ 主诉: 头痛3天        现病史: ...         诊断: ...          │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ┌─处方信息 ▼──────────────────────────────────────────────────┐ │
│ │ 处方号: RX001    剂数: 7剂    总价: ¥128.00                 │ │
│ └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│ 最后更新: 2024-12-06 14:30          [打印处方] [刷新]  [关闭] │
└─────────────────────────────────────────────────────────────────┘
```

**布局**: 卡片式，Expander可折叠，4列紧凑信息

---

## 5. 兼容性与实现

### 5.1 ViewModel层无需修改

所有详情页ViewModel已有EditCommand，仅调整XAML绑定位置。

### 5.2 导航参数兼容

详情页打开逻辑不变，通过ViewDetailsCommand传递实体ID。

### 5.3 响应式考虑

虽然主适配FHD，但应保持基本的响应能力：
- 使用 `*` 比例列宽而非固定像素
- 文本使用 `TextWrapping="Wrap"`
- 保留ScrollViewer作为内容区域的后备（仅在极端情况下启用）

---

## 6. 验收检查清单

- [ ] 所有详情页在1920x1080分辨率下无需滚动
- [ ] 所有详情页右上角有"编辑"按钮
- [ ] 所有列表页操作列无"编辑"按钮
- [ ] 表单布局合理（2-4列灵活使用）
- [ ] 统一使用共享样式
- [ ] 加载指示器样式一致
- [ ] 编译通过，功能正常
