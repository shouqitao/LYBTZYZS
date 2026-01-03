# desktop-architecture Delta: refactor-admin-workspace

## ADDED Requirements

### Requirement: ARCH-010 角色台与业务模块视图分离规范

Desktop层 **MUST** 遵循"View在角色台，Control在业务模块"的架构分离原则。

**视图分类**:
| 类型 | 位置 | 职责 | 示例 |
|------|------|------|------|
| 页面视图(View) | Roles/*/Views/ | 页面布局、导航、角色操作 | AdminHerbManagementView |
| 对话框(Dialog) | Modules/*/Dialogs/ | 模态交互、确认操作 | EditFormulaDialog |
| 控件(Control) | Modules/*/Controls/ | 可复用业务组件 | HerbMasterDetailControl |

**角色台模块**:
- `LYBT.Desktop.Clinical` - 医生工作台
- `LYBT.Desktop.Admin` - 管理员工作台
- `LYBT.Desktop.Reception` - 前台工作台

**业务模块**:
- `LYBT.Desktop.Patients` - 患者业务
- `LYBT.Desktop.MedicalCase` - 医案业务
- `LYBT.Desktop.Herbs` - 药材业务
- `LYBT.Desktop.Formula` - 验方业务
- `LYBT.Desktop.Users` - 用户业务

#### Scenario: MasterDetailControl复用模式

**Given** 一个管理类MasterDetailControl（如HerbMasterDetailControl）
**When** 该控件用于数据管理（CRUD操作）
**Then** 该控件必须位于业务模块的Controls/目录
**And** 对应的ViewModel也必须在业务模块
**And** 角色台创建薄包装View引用该Control

#### Scenario: 业务模块仅保留Dialog和Control

**Given** 一个业务模块（如LYBT.Desktop.Herbs）
**When** 检查其目录结构
**Then** 应包含Controls/目录存放可复用控件
**And** 应包含Dialogs/目录存放模态对话框
**And** 不应包含MasterDetailView类的页面视图

#### Scenario: 跨角色台Control复用

**Given** 一个MasterDetailControl（如PatientMasterDetailControl）
**When** 多个角色台需要相同功能
**Then** Admin角色台创建PatientManagementView使用该Control
**And** Clinical角色台创建PatientHistoryView使用该Control
**And** 两个View共享同一Control实现

---

### Requirement: ARCH-011 特殊视图例外规范

以下视图类型 **MUST** 作为例外保留在业务模块中，不受ARCH-010约束。

**例外类型**:
1. **登录视图** - 全局认证，位于Auth模块
2. **用户自服务视图** - 修改密码、个人设置等
3. **打印模板** - 报表打印专用
4. **验证视图** - 数据校验专用界面

#### Scenario: 登录视图保留在Auth模块

**Given** LoginView和LoginWindow
**When** 检查其位置
**Then** 应保留在LYBT.Desktop.Auth模块
**Because** 登录是全局功能，不属于任何角色台

#### Scenario: 用户自服务视图保留在Users模块

**Given** ChangePasswordView和UserProfileView
**When** 检查其位置
**Then** 可保留在LYBT.Desktop.Users模块
**Because** 这些是用户自服务功能，任何角色都可访问

---

### Requirement: ARCH-012 Control与View职责划分规范

业务模块Control与角色台View **MUST** 遵循明确的职责划分。

**Control职责** (业务模块):
- MasterDetail布局结构
- 列表数据加载和分页
- 详情展示和编辑
- CRUD核心业务逻辑
- 数据绑定属性暴露
- ViewModel绑定和管理

**View职责** (角色台模块):
- 页面标题和导航栏
- 角色特定操作按钮
- 权限控制和显示逻辑
- 返回导航
- 工具栏定制
- 角色特定样式覆盖

#### Scenario: 薄包装View模式

**Given** 一个角色台ManagementView
**When** 实现该View
**Then** 应仅包含Control引用和角色特定UI元素
**And** 业务逻辑应完全由Control处理
**And** View代码应保持最小化

#### Scenario: 角色特定扩展

**Given** Admin和Clinical都使用HerbMasterDetailControl
**When** Admin需要"批量导入"功能而Clinical不需要
**Then** Admin的HerbManagementView可添加额外工具栏按钮
**And** Control保持通用，不包含角色特定逻辑

---

### Requirement: ARCH-013 架构一致性优先原则

为保持架构一致性，**MUST** 对所有MasterDetailView采用Control模式重构，即使当前仅被单一角色台使用。

**原则**:
1. **一致性优先**: 统一的架构模式降低认知负担
2. **未来扩展**: 为后续角色台复用提供便捷
3. **维护性**: 统一的代码组织方式便于维护

#### Scenario: 单角色台使用的Control

**Given** UserMasterDetailView当前仅Admin使用
**When** 决定重构策略
**Then** 仍采用Control模式重构
**Because** 保持架构一致性，并为未来Clinical可能的用户管理需求预留
