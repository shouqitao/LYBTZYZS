## NEW Specification: workbench-layout

# Workbench Layout Specification

本规范定义首页工作台（AdminHomeView、ClinicalHomeView）的布局和视觉设计标准。

---

## ADDED Requirements

### Requirement: REQ-WORKBENCH-001 工作台标题区域

工作台页面 SHALL 使用紧凑标题区域，最大化功能区空间。

**Acceptance Criteria:**
- 标题区域高度不超过**60px**
- 标题格式："凌隐宝堂 - {工作台名称}"（如"系统管理工作台"、"医生工作台"）
- 标题字号16-18px，居中显示
- 不在工作台内显示用户信息（已移至Header用户菜单）
- 不在工作台内显示"修改个人信息"/"修改密码"按钮（已移至Header用户菜单）

#### Scenario: 管理员工作台标题
- **GIVEN** 管理员用户已登录
- **WHEN** 用户进入管理员工作台
- **THEN** 标题显示"凌隐宝堂 - 系统管理工作台"
- **AND** 标题区域高度不超过60px
- **AND** 无用户信息和账户操作按钮

#### Scenario: 医生工作台标题
- **GIVEN** 医生用户已登录
- **WHEN** 用户进入医生工作台
- **THEN** 标题显示"凌隐宝堂 - 医生工作台"
- **AND** 标题区域高度不超过60px
- **AND** 无用户信息和账户操作按钮

---

### Requirement: REQ-WORKBENCH-002 功能卡片视觉设计

工作台功能卡片 SHALL 使用统一的MaterialDesign风格。

**Acceptance Criteria:**
- 图标使用MaterialDesignThemes图标包，不使用emoji
- 卡片圆角12px
- 卡片阴影elevation 2
- 卡片间距20px
- 图标大小56px
- 卡片最小宽度160px
- hover状态有明显视觉反馈（背景色变化或轻微缩放）

#### Scenario: 功能卡片hover交互
- **GIVEN** 用户在工作台页面
- **WHEN** 鼠标悬停在功能卡片上
- **THEN** 卡片显示hover状态（背景色变浅或轻微阴影增强）
- **AND** 鼠标指针变为手型

---

### Requirement: REQ-WORKBENCH-003 功能卡片图标映射

功能卡片 SHALL 使用以下MaterialDesign图标。

**图标映射表:**
| 功能 | MaterialDesign图标名 |
|------|---------------------|
| 用户管理 | Account |
| 病患管理 | AccountGroup |
| 中药管理 | Leaf |
| 方剂管理 | Flask |
| 看诊管理 | Stethoscope |
| 处方管理 | PillMultiple |
| 开始接诊 | PlayCircle |
| 患者列表 | FormatListBulleted |
| 今日接诊 | CalendarToday |
| 查看报表 | ChartLine |

#### Scenario: 管理员工作台图标
- **GIVEN** 管理员用户在管理员工作台
- **WHEN** 用户查看功能卡片
- **THEN** 用户管理卡片显示Account图标
- **AND** 病患管理卡片显示AccountGroup图标
- **AND** 中药管理卡片显示Leaf图标
- **AND** 方剂管理卡片显示Flask图标
- **AND** 看诊管理卡片显示Stethoscope图标
- **AND** 处方管理卡片显示PillMultiple图标

---

### Requirement: REQ-WORKBENCH-004 工作台布局一致性

管理员工作台和医生工作台 SHALL 使用统一的设计语言。

**Acceptance Criteria:**
- 两个工作台使用相同的卡片样式（FunctionCardStyle）
- 两个工作台使用相同的标题区域样式（WorkbenchTitleStyle）
- 两个工作台使用相同的配色方案
- 两个工作台的卡片间距、圆角、阴影参数一致

#### Scenario: 样式一致性验证
- **GIVEN** 用户分别查看管理员工作台和医生工作台
- **WHEN** 对比两个工作台的视觉效果
- **THEN** 卡片样式完全一致（除图标和文字内容外）
- **AND** 标题区域样式完全一致
- **AND** 整体视觉风格统一

---

### Requirement: REQ-WORKBENCH-005 共享样式资源

工作台样式 SHALL 定义在共享资源字典中，避免重复定义。

**Acceptance Criteria:**
- 创建`WorkbenchStyles.xaml`资源字典
- 定义`FunctionCardStyle`（功能卡片样式）
- 定义`WorkbenchTitleStyle`（工作台标题样式）
- 定义`CardIconStyle`（卡片图标样式）
- 资源字典在App.xaml中合并
- AdminHomeView和ClinicalHomeView引用共享样式

#### Scenario: 样式资源复用
- **GIVEN** 开发者需要修改功能卡片样式
- **WHEN** 开发者修改`FunctionCardStyle`
- **THEN** AdminHomeView和ClinicalHomeView同时生效
- **AND** 无需分别修改两个视图文件
