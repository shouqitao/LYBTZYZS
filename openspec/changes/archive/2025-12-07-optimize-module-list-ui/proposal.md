# Change: optimize-module-list-ui

优化模块管理列表视图的UI设计，统一各模块列表的交互模式和视觉表现，并按照全局主题规范优化按钮样式。

## Why

当前各模块管理列表视图（用户、药材、验方、患者）存在以下问题：
1. **Checkbox对齐不一致** - DataGrid复选框列与其他列对齐不统一
2. **状态显示模式不统一** - 用户列表有状态切换按钮，其他模块仅有状态列或按钮混用
3. **缺少软删除恢复功能** - 软删除数据无法在UI上进行恢复操作
4. **列设计不科学** - 未根据实体属性特性合理设计列宽和显示优先级
5. **按钮样式不统一** - 多处样式文件定义冲突，颜色和悬停效果不一致

## What Changes

### UI组件层面
- 统一DataGrid复选框列的对齐方式（垂直居中）
- 移除所有管理列表的「状态」列，改用操作按钮触发状态变化
- 新增软删除状态显示及恢复按钮（仅管理员可见）

### 各模块列表重新设计
- **用户管理**: 保持现有状态切换按钮模式（作为标准参考）
- **药材管理**: 移除状态列，添加状态切换按钮
- **验方管理**: 移除状态列，添加状态切换按钮，整合ValidationStatus显示
- **患者管理**: 添加状态切换按钮

### 软删除恢复功能
- 在列表查询中支持显示软删除数据（管理员筛选）
- 在操作列添加「恢复」按钮（仅管理员角色可见）
- 恢复操作调用相应Service的Restore方法

### 按钮样式统一
- 统一使用UnifiedComponents.xaml中的按钮样式体系
- 统一主色调为Fluent Design蓝色(#0078D4)
- 统一悬停/按下效果使用具体颜色而非Opacity变化
- 清理冗余样式定义，消除Colors.xaml、CommonStyles.xaml、Controls.xaml中的重复定义

## Impact

- **Affected specs**: `ui-style-conventions`
- **Affected code**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserManagementView.xaml` (参考模板)
  - `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Views/HerbManagementView.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaManagementView.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientManagementView.xaml`
  - `src/Client/Desktop/Presentation/Themes/Controls/DataGridStyles.xaml`
  - `src/Client/Desktop/Shell/Styles/Colors.xaml` (颜色统一)
  - `src/Client/Desktop/Shell/Styles/CommonStyles.xaml` (清理冗余)
  - `src/Client/Desktop/Shell/Styles/Controls.xaml` (按钮样式统一)
  - `src/Client/Desktop/Infrastructure/Themes/UnifiedComponents.xaml` (参考标准)
  - 各模块ViewModel（添加Toggle/Restore命令）
  - 各模块Service（添加Restore方法）

## Design Principles

1. **UserManagementView作为参考标准** - 其状态切换按钮设计已符合预期
2. **基于实体属性设计列** - 核心属性优先显示，次要属性可折叠或省略
3. **操作优先于状态显示** - 用按钮触发状态变化比显示状态列更有交互价值
4. **角色权限控制** - 恢复操作仅管理员可执行
5. **UnifiedComponents.xaml作为样式标准** - 统一按钮样式、颜色和交互效果

## Column Design per Module

### 用户管理 (UserManagementView)
| 列 | 属性 | 宽度 | 说明 |
|----|------|------|------|
| Checkbox | - | 40 | 批量选择 |
| 用户名 | UserName | 150 | 核心标识 |
| 真实姓名 | RealName | 150 | 核心信息 |
| 角色 | Role | 130 | Badge显示 |
| 手机号 | PhoneNumber | 130 | 联系方式 |
| 邮箱 | Email | * | 自适应 |
| 操作 | - | 420 | 查看/重置密码/启用禁用/记录/删除 |

### 药材管理 (HerbManagementView)
| 列 | 属性 | 宽度 | 说明 |
|----|------|------|------|
| Checkbox | - | 40 | 批量选择 |
| 药材名 | Name | 150 | 核心标识 |
| 拼音码 | PinYinCode | 100 | 检索用 |
| 分类 | Category | 100 | 分类标签 |
| 产地 | Origin | 100 | 来源信息 |
| 规格 | Spec | 80 | 规格说明 |
| 单位 | Unit | 60 | 计量单位 |
| 单价 | Price | 80 | 价格 |
| 操作 | - | 300 | 查看/启用禁用/恢复(管理员)/删除 |

### 验方管理 (FormulaManagementView)
| 列 | 属性 | 宽度 | 说明 |
|----|------|------|------|
| Checkbox | - | 40 | 批量选择 |
| 验方名 | Name | 180 | 核心标识 |
| 分类 | Category | 100 | 分类标签 |
| 功效 | Effect | * | 自适应，主要描述 |
| 来源 | Source | 120 | 出处 |
| 药材数 | HerbCount | 80 | 组成药材数量 |
| 校验状态 | ValidationStatus | 100 | Badge显示 |
| 操作 | - | 300 | 查看/启用禁用/恢复(管理员)/删除 |

### 患者管理 (PatientManagementView)
| 列 | 属性 | 宽度 | 说明 |
|----|------|------|------|
| Checkbox | - | 40 | 批量选择 |
| 姓名 | Name | 120 | 核心标识 |
| 性别 | Gender | 60 | 基本信息 |
| 年龄 | Age | 60 | 基本信息 |
| 手机号 | PhoneNumber | 130 | 联系方式 |
| 身份证号 | IdNumber | 180 | 身份标识 |
| 就诊次数 | VisitCount | 80 | 统计信息 |
| 操作 | - | 300 | 查看/启用禁用/恢复(管理员)/删除 |
