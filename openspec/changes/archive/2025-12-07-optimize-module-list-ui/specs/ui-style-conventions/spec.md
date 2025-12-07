## ADDED Requirements

### Requirement: UI-020 Management List Checkbox Alignment

管理列表的DataGrid复选框列 MUST 垂直居中对齐。

**规范**:
- CheckBox列使用CellStyle设置VerticalAlignment="Center"
- CheckBox列使用CellStyle设置HorizontalAlignment="Center"
- 所有使用BaseMasterDataListView的视图自动继承此样式

#### Scenario: Checkbox column alignment
- **GIVEN** 管理列表使用DataGrid显示数据
- **WHEN** 启用ShowCheckBoxColumn="True"
- **THEN** CheckBox MUST 垂直居中对齐
- **AND** CheckBox MUST 水平居中对齐
- **AND** 与其他列内容对齐一致

### Requirement: UI-021 Status Toggle Button Pattern

有状态（启用/禁用）的实体列表 MUST 使用按钮触发状态变化，NOT 使用状态列显示。

**规范**:
- 状态切换使用Button控件，NOT 使用UnifiedStatusBadge显示状态
- 按钮文本根据当前状态动态切换（启用↔禁用）
- 使用DataTrigger绑定Status属性实现文本切换

#### Scenario: Status toggle button display
- **GIVEN** 实体有Status属性（CommonStatus枚举）
- **WHEN** 实体状态为Enabled
- **THEN** 按钮显示文本「禁用」
- **AND** 点击按钮将状态切换为Disabled

#### Scenario: Status toggle button for disabled entity
- **GIVEN** 实体有Status属性（CommonStatus枚举）
- **WHEN** 实体状态为Disabled
- **THEN** 按钮显示文本「启用」
- **AND** 点击按钮将状态切换为Enabled

#### Scenario: Status toggle button implementation
- **GIVEN** 需要实现状态切换按钮
- **WHEN** 编写XAML代码
- **THEN** MUST 使用以下模式:
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

### Requirement: UI-022 Soft Delete Restore Button

软删除数据的恢复操作 MUST 通过操作列按钮实现，且仅管理员可见。

**规范**:
- 恢复按钮放在操作列中
- 使用Visibility绑定IsAdmin属性控制显示
- 恢复按钮使用WarningButton样式

#### Scenario: Restore button visibility for admin
- **GIVEN** 当前登录用户是Admin或SuperAdmin角色
- **WHEN** 查看软删除数据列表
- **THEN** 操作列显示「恢复」按钮
- **AND** 按钮使用WarningButton样式

#### Scenario: Restore button hidden for non-admin
- **GIVEN** 当前登录用户是Doctor角色
- **WHEN** 查看数据列表
- **THEN** 操作列NOT显示「恢复」按钮

#### Scenario: Restore button implementation
- **GIVEN** 需要实现恢复按钮
- **WHEN** 编写XAML代码
- **THEN** MUST 使用以下模式:
```xml
<Button Content="恢复"
        Style="{StaticResource WarningButton}"
        Visibility="{Binding DataContext.IsAdmin, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BoolToVisibilityConverter}}"
        Command="{Binding DataContext.RestoreCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
        CommandParameter="{Binding}" />
```

### Requirement: UI-023 Entity-Specific Column Design

各模块管理列表的列设计 MUST 根据实体属性科学合理设计。

**规范**:
- 核心标识属性（Name, UserName等）优先显示，宽度适中
- 联系方式（PhoneNumber, Email）使用固定宽度
- 描述性属性（Effect, Description）使用自适应宽度(*)
- 操作列放在最后，宽度根据按钮数量设定

#### Scenario: User list columns
- **GIVEN** 用户管理列表
- **WHEN** 显示用户数据
- **THEN** 列顺序为: Checkbox(40) → 用户名(150) → 真实姓名(150) → 角色(130,Badge) → 手机号(130) → 邮箱(*) → 操作(420)

#### Scenario: Herb list columns
- **GIVEN** 药材管理列表
- **WHEN** 显示药材数据
- **THEN** 列顺序为: Checkbox(40) → 药材名(150) → 拼音码(100) → 分类(100) → 产地(100) → 规格(80) → 单位(60) → 单价(80) → 操作(300)

#### Scenario: Formula list columns
- **GIVEN** 验方管理列表
- **WHEN** 显示验方数据
- **THEN** 列顺序为: Checkbox(40) → 验方名(180) → 分类(100) → 功效(*) → 来源(120) → 药材数(80) → 校验状态(100,Badge) → 操作(300)

#### Scenario: Patient list columns
- **GIVEN** 患者管理列表
- **WHEN** 显示患者数据
- **THEN** 列顺序为: Checkbox(40) → 姓名(120) → 性别(60) → 年龄(60) → 手机号(130) → 身份证号(180) → 就诊次数(80) → 操作(300)

### Requirement: UI-024 Special Status Column Preservation

特殊状态属性（非通用启用/禁用）MUST 使用状态列显示而非按钮切换。

**规范**:
- ValidationStatus（校验状态）MUST 使用UnifiedStatusBadge显示
- 特殊状态列不受UI-021约束
- 特殊状态有独立的业务流程，NOT 通过简单按钮切换

#### Scenario: ValidationStatus display
- **GIVEN** 验方有ValidationStatus属性
- **WHEN** 显示验方列表
- **THEN** MAY 使用UnifiedStatusBadge显示校验状态
- **AND** NOT 使用按钮切换校验状态（校验状态有独立流程）

### Requirement: UI-025 Unified Button Style System

全局按钮样式 MUST 统一使用Fluent Design风格，颜色和交互效果保持一致。

**规范**:
- 主色调使用Fluent Design蓝色: #0078D4
- 悬停效果使用具体颜色变化，NOT 使用Opacity变化
- 按钮样式统一定义在Controls.xaml中，参考UnifiedComponents.xaml实现

#### Scenario: Primary button style
- **GIVEN** 使用PrimaryButton样式
- **WHEN** 按钮渲染
- **THEN** 背景色 MUST 为 #0078D4
- **AND** 悬停时背景色 MUST 变为 #106EBE
- **AND** 按下时背景色 MUST 变为 #005A9E

#### Scenario: Danger button style
- **GIVEN** 使用DangerButton样式
- **WHEN** 按钮渲染
- **THEN** 背景色 MUST 为 #D32F2F
- **AND** 悬停时背景色 MUST 变为 #B71C1C

#### Scenario: Warning button style
- **GIVEN** 使用WarningButton样式
- **WHEN** 按钮渲染
- **THEN** 背景色 MUST 为 #F57C00
- **AND** 悬停时背景色 MUST 变为 #E65100

#### Scenario: Success button style
- **GIVEN** 使用SuccessButton样式
- **WHEN** 按钮渲染
- **THEN** 背景色 MUST 为 #388E3C
- **AND** 悬停时背景色 MUST 变为 #2E7D32

#### Scenario: Button style implementation
- **GIVEN** 需要定义按钮样式
- **WHEN** 编写XAML代码
- **THEN** MUST 使用ControlTemplate实现悬停效果
- **AND** MUST 使用Trigger而非Opacity变化
- **AND** 按钮高度 MUST 为 36px
- **AND** 按钮圆角 MUST 为 4px
