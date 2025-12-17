# Design: optimize-preview-layout

## Architecture Overview

本设计聚焦于前端XAML布局优化，不涉及后端或数据层变更。

```
LYBT.Desktop.Infrastructure
└── Themes/
    └── PreviewStyles.xaml  [NEW] 统一预览样式资源

LYBT.Desktop.Patients
└── Controls/
    └── PatientViewControl.xaml  [MODIFY]

LYBT.Desktop.Users  
└── Controls/
    └── UserViewControl.xaml  [MODIFY]

LYBT.Desktop.Herbs
└── Controls/
    └── HerbViewControl.xaml  [MODIFY]

LYBT.Desktop.Formula
└── Controls/
    └── FormulaViewControl.xaml  [MODIFY]

LYBT.Desktop.MedicalCase
└── Controls/
    └── MedicalCaseViewControl.xaml  [MODIFY]
```

## Design Decisions

### D1: 统一网格系统

**决策**: 采用12列虚拟网格系统，实际使用2/3/4列变体

**理由**:
- 12是2、3、4、6的公倍数，便于灵活划分
- 与现有InfoCard组件兼容
- 符合现代UI设计规范

**实现**:
```xml
<!-- 2列布局: 每列6格 -->
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="40"/> <!-- 间距 -->
    <ColumnDefinition Width="*"/>
</Grid.ColumnDefinitions>

<!-- 3列布局: 每列4格 -->
<Grid.ColumnDefinitions>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="24"/>
    <ColumnDefinition Width="*"/>
    <ColumnDefinition Width="24"/>
    <ColumnDefinition Width="*"/>
</Grid.ColumnDefinitions>
```

### D2: 字段优先级分类

**决策**: 将字段分为四个优先级

| 优先级 | 说明 | 示例 |
|--------|------|------|
| P0-标识 | 唯一标识实体的字段 | 姓名、编号、用户名 |
| P1-核心 | 业务核心属性 | 性别、年龄、角色、价格 |
| P2-辅助 | 补充信息 | 拼音码、地址、备注 |
| P3-元数据 | 系统字段 | 状态、创建时间、更新时间 |

**排列规则**:
- P0字段: 卡片顶部，大字体醒目显示
- P1字段: 主体区域，标准2列布局
- P2字段: 主体区域下方，可折叠或淡化
- P3字段: 底部或右上角Badge

### D3: 视觉层次增强

**决策**: 通过以下方式增强视觉层次

1. **标题区域**:
   - 实体名称使用`FontSizeSubtitle`(20px)
   - 状态使用彩色Badge(成功/警告/信息)

2. **标签-值对比**:
   - 标签: `SecondaryTextBrush`(#605E5C), 12px, SemiBold
   - 值: `PrimaryTextBrush`(#201F1E), 14px, Normal

3. **分组卡片**:
   - 使用现有InfoCard组件
   - 组间距16px
   - 组内行间距12px

### D4: 长文本处理

**决策**: 长文本字段使用全宽单列展示

**适用字段**:
- Patient: 地址
- Herb: 功效、用法用量、备注
- Formula: 性味归经、功效、用法、备注
- MedicalCase: 主诉、现病史、诊断、治疗方案等

**实现**:
```xml
<StackPanel Grid.Row="N" Grid.ColumnSpan="3">
    <TextBlock Text="字段名" Style="{StaticResource FormLabelStyle}"/>
    <TextBlock Text="{Binding Value}" 
               Style="{StaticResource ValueDisplayStyle}" 
               TextWrapping="Wrap"/>
</StackPanel>
```

## Component Design

### PreviewStyles.xaml (新增)

```xml
<!-- 预览标题样式 - 实体名称 -->
<Style x:Key="PreviewTitleStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{StaticResource FontSizeSubtitle}"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="Foreground" Value="{StaticResource PrimaryTextBrush}"/>
    <Setter Property="Margin" Value="0,0,0,16"/>
</Style>

<!-- 预览字段行样式 -->
<Style x:Key="PreviewFieldRowStyle" TargetType="StackPanel">
    <Setter Property="Margin" Value="0,0,0,12"/>
</Style>

<!-- 状态Badge基础样式 -->
<Style x:Key="StatusBadgeStyle" TargetType="Border">
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Padding" Value="8,4"/>
    <Setter Property="HorizontalAlignment" Value="Left"/>
</Style>
```

### 各模块布局优化

#### PatientViewControl 优化后结构

```
┌─────────────────────────────────────────────────┐
│ 患者姓名 (20px)                    [状态Badge]   │
├─────────────────────────────────────────────────┤
│ ┌─────────────────┐  ┌─────────────────┐       │
│ │ 性别            │  │ 出生日期        │       │
│ │ 男              │  │ 1990-01-01      │       │
│ └─────────────────┘  └─────────────────┘       │
│ ┌─────────────────┐  ┌─────────────────┐       │
│ │ 年龄            │  │ 身份证号        │       │
│ │ 35岁            │  │ 110...          │       │
│ └─────────────────┘  └─────────────────┘       │
├─────────────────────────────────────────────────┤
│ 联系方式                                         │
├─────────────────────────────────────────────────┤
│ ┌─────────────────┐  ┌─────────────────────────┐│
│ │ 手机号码        │  │ 地址                    ││
│ │ 138...          │  │ 北京市...               ││
│ └─────────────────┘  └─────────────────────────┘│
└─────────────────────────────────────────────────┘
```

#### MedicalCaseViewControl 优化后结构

```
┌─────────────────────────────────────────────────┐
│ 病历号: MC202412001           [已诊疗] [已开方]  │
│ 患者: 张三 | 医生: 李医生 | 2024-12-16 10:00    │
├─────────────────────────────────────────────────┤
│ 诊疗信息                                   [展开]│
├─────────────────────────────────────────────────┤
│ 主诉: xxxxxx                                     │
│ 现病史: xxxxxx                                   │
│ 中医诊断: xxxxxx                                 │
│ 治疗原则: xxxxxx                                 │
├─────────────────────────────────────────────────┤
│ 处方信息                                   [展开]│
├─────────────────────────────────────────────────┤
│ 处方编号 | 配方来源 | 剂数 | 总价                │
│ ┌─────────────────────────────────────────────┐ │
│ │ 药材名称 | 剂量 | 单位 | 单价 | 小计        │ │
│ │ ...                                         │ │
│ └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

## Migration Strategy

1. **Phase 1**: 创建PreviewStyles.xaml并合并到资源字典
2. **Phase 2**: 逐个优化ViewControl (按复杂度从低到高)
   - UserViewControl (最简单)
   - PatientViewControl
   - HerbViewControl
   - FormulaViewControl
   - MedicalCaseViewControl (最复杂)
3. **Phase 3**: 视觉审查和微调

## Testing Approach

- 手动UI测试: 验证各预览界面布局正确
- 绑定测试: 确保数据正确显示
- 响应式测试: 不同窗口大小下布局适应性
