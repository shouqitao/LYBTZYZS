# ViewModel Conventions Delta

本delta记录对viewmodel-conventions规范的合规性修复实施。

## MODIFIED Requirements

### Requirement: VM-001 ViewModel Size Guidelines (Comprehensive Compliance Fix)

所有ViewModel和DataManager MUST 符合500行限制。

**变更内容**:
本次重构识别并修复18个违规文件：

| 文件 | 当前行数 | 目标行数 | 重构方式 |
|------|---------|---------|---------|
| PatientSelectionViewModel | 1347 | <500 | 提取3个Handler |
| PrescriptionPanelViewModel | 1335 | <500 | 提取3个Handler |
| MedicalCaseWorkspaceViewModel | 1278 | <500 | 提取3个Handler |
| MedicalCaseDataManager | 1004 | <500 | 提取2个Handler |
| FormulaDetailViewModel | 983 | <500 | 提取2个Handler |
| UserManagementViewModel | 901 | <500 | 提取2个Handler |
| PrescriptionEditorDialogViewModel | 682 | <500 | 提取1个Handler |
| HerbManagementViewModel | 650 | <500 | 提取1个Handler |
| HerbDetailViewModel | 629 | <500 | 代码整理 |
| EditFormulaDialogViewModel | 621 | <500 | 代码整理 |
| PatientManagementViewModel | 601 | <500 | 提取1个Handler |
| ConsultationFormViewModel | 589 | <500 | 代码整理 |
| SelectFormulaDialogViewModel | 587 | <500 | 代码整理 |
| LoginViewModel | 582 | <500 | 代码整理 |
| PatientDetailViewModel | 576 | <500 | 代码整理 |
| PrescriptionDataManager | 554 | <500 | 代码整理 |
| UserDetailViewModel | 540 | <500 | 代码整理 |
| FormulaManagementViewModel | 516 | <500 | 代码整理 |

**修复方式**: 遵循既有VM-002 Components Pattern规范。

#### Scenario: All ViewModels comply with size limit
- **GIVEN** 18个ViewModel/DataManager超过500行限制
- **WHEN** 通过提取Handler组件进行重构
- **THEN** 所有ViewModel/DataManager行数 SHALL 小于500行
- **AND** 新Handler SHALL 位于ViewModels/Components/目录
- **AND** Handler SHALL 通过接口注入到ViewModel
- **AND** 功能行为不变

### Requirement: VM-002 Components Pattern (Extended Application)

所有超大ViewModel所在模块 SHALL 采用Components模式进行功能拆分。

**变更内容**:
以下模块需要新增或扩展Components目录：

| 模块 | 当前状态 | 目标状态 |
|------|---------|---------|
| Patients | 无Components | 新增6个Handler |
| Prescriptions | 有基础Components | 扩展6个Handler |
| MedicalCase | 已有Components | 扩展6个Handler |
| Formula | 无Components | 新增4个Handler |
| Users | 有Components | 扩展4个Handler |
| Consultation | 有Components | 扩展Handler |
| Herbs | 无Components | 新增Handler |
| Auth | 无Components | 代码整理即可 |

#### Scenario: All modules adopt Components pattern
- **GIVEN** 多个模块当前没有或不完整的Components目录
- **WHEN** 创建Handler组件进行功能拆分
- **THEN** SHALL 创建/扩展ViewModels/Components/目录
- **AND** SHALL 创建对应的IXxxHandler接口
- **AND** SHALL 在Module.RegisterTypes中注册为Scoped生命周期
- **AND** ViewModel SHALL 通过构造函数注入Handler

### Requirement: VM-003 XAML Size Guidelines (NEW)

所有XAML视图文件 SHALL 控制在300行以内（特殊复杂视图可适当放宽至400行）。

**变更内容**:
识别10个超大XAML文件，通过控件化重构：

| 文件 | 当前行数 | 目标行数 |
|------|---------|---------|
| UserDetailView.xaml | 621 | <300 |
| PatientSelectionView.xaml | 485 | <300 |
| LoginView.xaml | 450 | <300 |
| UserProfileView.xaml | 433 | <300 |
| HerbDetailView.xaml | 413 | <300 |
| ChangePasswordView.xaml | 393 | <300 |
| FormulaValidationView.xaml | 392 | <300 |
| PatientDetailView.xaml | 381 | <300 |
| MedicalCaseDetailView.xaml | 378 | <300 |
| ConsultationFormView.xaml | 373 | <300 |

**修复方式**: 提取可复用UserControl和全局样式。

#### Scenario: XAML files reduced through componentization
- **GIVEN** 多个XAML文件超过300行
- **WHEN** 创建可复用的UserControl组件
- **THEN** FormFieldControl SHALL 用于统一表单字段模式
- **AND** CardContainer SHALL 用于统一卡片容器样式
- **AND** LoadingOverlay SHALL 用于统一加载遮罩
- **AND** EmptyStateView SHALL 用于统一空状态展示
- **AND** PaginationControl SHALL 用于统一分页控件
- **AND** SearchBox SHALL 用于统一搜索框

### Requirement: VM-004 Code Cleanup (NEW)

项目 SHALL 保持代码整洁，无冗余文件和代码。

**变更内容**:
- 删除3个备份文件
- 处理~30处TODO注释
- 清理~88处注释代码

#### Scenario: Project maintains clean codebase
- **GIVEN** 项目存在备份文件、TODO注释、注释代码
- **WHEN** 执行代码清理
- **THEN** 所有.Backup.tmp文件 SHALL 被删除
- **AND** 过时TODO SHALL 被删除
- **AND** 未完成功能的TODO SHALL 创建Issue跟踪
- **AND** 无用注释代码 SHALL 被删除

## Cross-Reference

- **viewmodel-conventions**: 主规范文档，本delta为全面合规修复
- **service-conventions**: Handler调用Service时遵循
- **naming-conventions**: Handler命名遵循
- **testing-conventions**: 新Handler需添加单元测试
