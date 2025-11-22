# LYBTZYZS Desktop端UI/UX重构方案

**创建日期**: 2025-11-04
**文档类型**: 重构规划
**适用范围**: Desktop端所有模块
**角色**: WPF架构师 + UI/UX设计师
**版本**: v1.0

---

## 📋 执行摘要 (Executive Summary)

### 当前状况
- **Desktop端模块数量**: 8个
- **UI界面总数**: 39个XAML视图
- **WebAPI端点**: 8个业务模块，100+个API端点
- **核心业务规则**: 14个已文档化规则
- **已完成架构优化**: Issue #1790（组件化）、#1795（方法复杂度）

### 主要发现
1. ✅ **核心业务流程完整**: 患者-医案-诊疗-处方四大核心流程已覆盖
2. ⚠️ **UI重复冗余**: 存在多个功能重叠的管理界面
3. ⚠️ **UX流程复杂**: 部分流程需要过多点击和界面跳转
4. ⚠️ **UI技术债务**: 部分界面不符合最新组件化模式（ADR-009）
5. ✅ **MVP约束遵守**: 未发现违反Constitution的过度设计

### 重构优先级
- **🔴 P0 - 必须立即处理** (1-2周)：删除重复界面、优化核心流程
- **🟡 P1 - 短期优化** (3-4周)：UI组件化重构、UX改进
- **🟢 P2 - 中期优化** (1-2个月)：现代化设计、无障碍改进

---

## 1. WebAPI vs Desktop UI覆盖度分析

### 1.1 认证模块 (Auth)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| POST /api/v1/auth/login | LoginView.xaml | ✅ 完全覆盖 | |
| POST /api/v1/auth/admin/login | LoginView.xaml | ✅ 完全覆盖 | 隐藏端点，双轨认证 |
| POST /api/v1/auth/logout | LoginView/MainWindow | ✅ 完全覆盖 | |
| GET/POST /api/v1/auth/validate | 自动验证逻辑 | ✅ 完全覆盖 | Token拦截器 |
| POST /api/v1/auth/changeSysAdminPassword | ⚠️ 缺失UI | ❌ 未覆盖 | 仅API调用 |

**✅ 覆盖率**: 80% (4/5)

**分析**:
- ✅ 核心登录流程完整，双轨认证已实现
- ❌ 超级管理员密码修改无UI界面（低优先级，管理员操作较少）
- ✅ LoginWindow和LoginView设计清晰，符合MVVM模式

### 1.2 用户管理模块 (Users)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/users (分页列表) | UserManagementView | ✅ 完全覆盖 | |
| GET /api/v1/users/{id} | UserDetailView | ✅ 完全覆盖 | |
| POST /api/v1/users (创建) | UserCreateView | ✅ 完全覆盖 | |
| PUT /api/v1/users/{id} (更新) | UserEditView | ✅ 完全覆盖 | |
| DELETE /api/v1/users/{id} | UserManagementView | ✅ 完全覆盖 | |
| GET /api/v1/users/current | UserProfileDialog | ✅ 完全覆盖 | |
| POST /api/v1/users/batch-delete | UserManagementView | ✅ 完全覆盖 | |
| POST /api/v1/users/{id}/reset-password | ResetPasswordDialog | ✅ 完全覆盖 | |
| POST /api/v1/users/{id}/toggle-status | UserManagementView | ✅ 完全覆盖 | |

**✅ 覆盖率**: 100% (9/9)

**分析**:
- ✅ 用户管理功能完整，CRUD操作齐全
- ⚠️ **技术债务**: UserCreateView和UserEditView功能重复（可合并为UserFormDialog）
- ⚠️ **UX问题**: UserDetailView是否必要？（直接跳转到Edit可能更流畅）
- ✅ ChangePasswordDialog和ResetPasswordDialog功能独立，设计合理

### 1.3 患者管理模块 (Patients)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/patients (分页列表) | PatientSelectionView | ✅ 完全覆盖 | |
| GET /api/v1/patients/{id} | PatientDetailView | ✅ 完全覆盖 | |
| POST /api/v1/patients (创建) | QuickCreatePatientDialog | ✅ 完全覆盖 | |
| PUT /api/v1/patients/{id} (更新) | PatientDetailView | ✅ 完全覆盖 | |
| DELETE /api/v1/patients/{id} | PatientSelectionView | ✅ 完全覆盖 | |
| POST /api/v1/patients/import (批量导入) | PatientImportWizardView | ✅ 完全覆盖 | |
| GET /api/v1/patients/import-template | PatientImportWizardView | ✅ 完全覆盖 | |

**✅ 覆盖率**: 100% (7/7)

**分析**:
- ✅ 患者管理功能完整，经过Issue #1790组件化重构
- ✅ PatientSelectionView已应用PatientSearchManager组件模式
- ✅ UnfinishedCaseDialog（未完成医案检测）符合业务规则BF-003
- ✅ QuickCreatePatientDialog快速创建体验良好
- ⚠️ PatientDetailView是否可以优化为更简洁的表单对话框？

### 1.4 医案管理模块 (MedicalCase)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| **Write Layer (8个端点)** |||
| POST /api/v1/medicalcases | MedicalCaseFlowView | ✅ 完全覆盖 | 三步流程Step 0 |
| PUT /api/v1/medicalcases/{id}/consultation | MedicalCaseConsultationView | ✅ 完全覆盖 | 三步流程Step 1 |
| PUT /api/v1/medicalcases/{id}/prescription-flag | MedicalCaseFlowView | ✅ 完全覆盖 | 三步流程Step 2 |
| POST /api/v1/medicalcases/{id}/prescriptions | PrescriptionEditorView | ✅ 完全覆盖 | 三步流程Step 3a |
| PUT /api/v1/medicalcases/{id}/prescriptions/{pid} | PrescriptionEditorView | ✅ 完全覆盖 | |
| DELETE /api/v1/medicalcases/{id}/prescriptions/{pid} | PrescriptionEditorView | ✅ 完全覆盖 | |
| PUT /api/v1/medicalcases/{id}/status | MedicalCaseFlowView | ✅ 完全覆盖 | |
| PUT /api/v1/medicalcases/{id}/complete | CompletionView | ✅ 完全覆盖 | 三步流程最后 |
| **Read Layer (4个端点)** |||
| GET /api/v1/medicalcases/{id} | MedicalCaseDetailView | ✅ 完全覆盖 | |
| GET /api/v1/medicalcases (分页查询) | MedicalCaseListView | ✅ 完全覆盖 | |
| GET /api/v1/medicalcases/{id}/consultations | MedicalCaseDetailView | ✅ 完全覆盖 | |
| GET /api/v1/medicalcases/{id}/prescriptions | MedicalCaseDetailView | ✅ 完全覆盖 | |
| **Helper Layer (2个端点)** |||
| GET /api/v1/medicalcases/{id}/can-edit | MedicalCaseFlowView | ✅ 完全覆盖 | |
| GET /api/v1/medicalcases/{id}/prescriptions/{pid}/can-delete | PrescriptionEditorView | ✅ 完全覆盖 | |

**✅ 覆盖率**: 100% (14/14)

**分析**:
- ✅ **核心业务流程完整**: 三步看诊流程（BF-002）完全覆盖
- ✅ **聚合根模式**: 符合AR-001规范，所有写操作通过MedicalCase聚合根
- ⚠️ **UI冗余问题** (🔴 P0):
  - MedicalCaseManagementView和MedicalCaseListView功能重复
  - MedicalCaseDetailView和MedicalCaseFlowView部分功能重叠
  - OtherCasesQueryView功能不明确，可能是历史遗留
- ⚠️ **UX流程问题** (🟡 P1):
  - 三步流程需要在3-4个界面间跳转，是否可以优化为Wizard模式？
  - CompletionView是否可以简化为对话框而非独立页面？

### 1.5 诊疗记录模块 (Consultation)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/consultation | ConsultationFormView | ✅ 完全覆盖 | |
| POST /api/v1/consultation | ConsultationFormView | ✅ 完全覆盖 | |
| PUT /api/v1/consultation/{id} | ConsultationFormView | ✅ 完全覆盖 | |

**✅ 覆盖率**: 100% (3/3)

**分析**:
- ✅ ConsultationFormView设计简洁，四诊信息表单清晰
- ⚠️ **冗余界面** (🔴 P0): ConsultationManagementView是否必要？
  - 诊疗记录应该只在MedicalCase上下文中查看/编辑
  - 独立的管理界面可能违反AR-001聚合根约束
  - **建议**: 删除ConsultationManagementView，只保留ConsultationFormView

### 1.6 处方管理模块 (Prescriptions)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/prescriptions (分页列表) | PrescriptionManagementView | ✅ 完全覆盖 | |
| GET /api/v1/prescriptions/{id} | PrescriptionView | ✅ 完全覆盖 | |
| POST /api/v1/prescriptions | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| PUT /api/v1/prescriptions/{id} | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| POST /api/v1/prescriptions/{id}/items (添加项目) | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| PUT /api/v1/prescriptions/{id}/items/{itemId} | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| DELETE /api/v1/prescriptions/{id}/items/{itemId} | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| GET /api/v1/prescriptions/generate-no | PrescriptionEditorDialog | ✅ 完全覆盖 | |
| GET /api/v1/prescriptions/statistics | - | 🟢 MVP不需要 | 统计功能MVP后考虑 |
| GET /api/v1/prescriptions/statistics/range | - | 🟢 MVP不需要 | 统计功能MVP后考虑 |
| POST /api/v1/prescriptions/{id}/copy | ⚠️ 缺失UI | ⚠️ 可延后 | 验方导入已实现，复制功能可延后 |

**✅ MVP覆盖率**: 100% (8/8个MVP必需API)
**整体覆盖率**: 73% (8/11个API，含MVP后功能)

**分析**:
- ✅ 核心CRUD操作完整，MVP功能已100%覆盖
- ⚠️ **冗余界面** (🔴 P0):
  - PrescriptionsMainView、PrescriptionManagementView、PrescriptionView三个界面功能重叠
  - **建议**: 合并为单一的PrescriptionManagementView
- 🟢 **MVP后功能**:
  - 处方统计功能（MVP不需要，用户已确认）
  - 处方复制功能（验方导入已实现，复制可延后）
- ✅ HerbSelectionDialog和SelectFormulaDialog设计合理
- ✅ FormulaTemplateDialog（验方导入）符合业务需求CR-002

### 1.7 药材管理模块 (Herbs)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/herbs (分页列表) | HerbManagementView | ✅ 完全覆盖 | |
| GET /api/v1/herbs/{id} | HerbDetailView | ✅ 完全覆盖 | |
| POST /api/v1/herbs | HerbManagementView | ✅ 完全覆盖 | |
| PUT /api/v1/herbs/{id} | HerbDetailView | ✅ 完全覆盖 | |
| GET /api/v1/herbs/search (拼音/分类/功效) | HerbManagementView | ✅ 完全覆盖 | |
| POST /api/v1/herbs/batch-delete | HerbManagementView | ✅ 完全覆盖 | |
| POST /api/v1/herbs/import | ⚠️ 缺失UI | ❌ 未覆盖 | 批量导入未实现 |
| GET /api/v1/herbs/export | ⚠️ 缺失UI | ❌ 未覆盖 | 导出未实现 |
| GET /api/v1/herbs/import-template | ⚠️ 缺失UI | ❌ 未覆盖 | 下载模板未实现 |

**⚠️ 覆盖率**: 67% (6/9)

**分析**:
- ✅ 核心CRUD操作完整
- ❌ **缺失功能** (🟡 P1):
  - 药材批量导入/导出功能未实现（参考患者导入向导，可复用PatternImportWizard）
- ✅ HerbManagementView和HerbDetailView设计合理
- ✅ 拼音搜索功能已实现，符合中医用户习惯

### 1.8 验方管理模块 (Formula)

| WebAPI端点 | Desktop UI | 覆盖状态 | 备注 |
|-----------|-----------|---------|------|
| GET /api/v1/formulas (分页列表) | FormulaManagementView | ✅ 完全覆盖 | |
| GET /api/v1/formulas/{id} | FormulaDetailView | ✅ 完全覆盖 | |
| POST /api/v1/formulas | EditFormulaDialog | ✅ 完全覆盖 | |
| PUT /api/v1/formulas/{id} | EditFormulaDialog | ✅ 完全覆盖 | |
| GET /api/v1/formulas/{id}/herbs | FormulaDetailView | ✅ 完全覆盖 | |
| POST /api/v1/formulas/{id}/herbs (添加药材) | EditFormulaDialog | ✅ 完全覆盖 | |
| PUT /api/v1/formulas/{id}/herbs/{itemId} | EditFormulaDialog | ✅ 完全覆盖 | |
| DELETE /api/v1/formulas/{id}/herbs/{itemId} | EditFormulaDialog | ✅ 完全覆盖 | |
| POST /api/v1/formulas/batch-delete | FormulaManagementView | ✅ 完全覆盖 | |
| POST /api/v1/formulas/import | ⚠️ 缺失UI | ⚠️ 可延后 | 批量导入可延后 |
| GET /api/v1/formulas/export | ⚠️ 缺失UI | ⚠️ 可延后 | 导出可延后 |
| GET /api/v1/formulas/import-template | ⚠️ 缺失UI | ⚠️ 可延后 | 下载模板可延后 |
| POST /api/v1/formulas/recommend | - | 🟢 MVP不需要 | AI推荐功能，MVP后考虑 |
| POST /api/v1/formulas/recommend-by-diagnosis | - | 🟢 MVP不需要 | AI推荐功能，MVP后考虑 |

**✅ MVP覆盖率**: 100% (9/9个MVP必需API)
**整体覆盖率**: 64% (9/14个API，含MVP后功能)

**分析**:
- ✅ 核心CRUD操作完整，MVP功能已100%覆盖
- ⚠️ **冗余界面** (🟡 P1):
  - ViewFormulaDialog和FormulaDetailView功能重叠
  - **建议**: 删除ViewFormulaDialog，使用FormulaDetailView（只读模式）
- ⚠️ **可延后功能** (🟡 P1):
  - 批量导入/导出功能（低优先级，可参考患者导入向导实现）
- 🟢 **MVP后功能**:
  - 智能推荐功能（AI功能，MVP不需要，用户已确认）
- ✅ FormulaValidationView（验方校验）设计独特，符合中医业务需求

---

## 2. 技术债务识别 (🔴 P0 优先)

### 2.1 UI冗余问题

#### 问题1: 用户管理 - Create和Edit界面重复
**现状**:
- `UserCreateView.xaml` (独立创建界面)
- `UserEditView.xaml` (独立编辑界面)

**问题**:
- 两个界面95%代码重复
- 维护成本高，修改需要同步两处
- 违反DRY原则

**解决方案** (🔴 P0):
```
删除: UserCreateView.xaml, UserEditView.xaml
新增: UserFormDialog.xaml (支持Create/Edit两种模式)
参数: mode: "create" | "edit", userId?: Guid
优势: 单一职责、代码复用、维护简单
```

#### 问题2: 医案管理 - 多个管理界面功能重叠
**现状**:
- `MedicalCaseManagementView.xaml` (管理界面)
- `MedicalCaseListView.xaml` (列表界面)
- `OtherCasesQueryView.xaml` (其他病案查询)

**问题**:
- MedicalCaseManagementView和MedicalCaseListView功能重复（都是查询列表）
- OtherCasesQueryView用途不明确，可能是历史遗留
- 维护三个界面增加复杂度

**解决方案** (🔴 P0):
```
保留: MedicalCaseManagementView (主管理界面，支持筛选、分页、操作)
删除: MedicalCaseListView (功能重复)
删除: OtherCasesQueryView (不符合AR-001聚合根约束)
```

#### 问题3: 诊疗记录 - 独立管理界面违反聚合根约束
**现状**:
- `ConsultationManagementView.xaml` (独立管理界面)
- `ConsultationFormView.xaml` (表单界面，在MedicalCase上下文中使用)

**问题**:
- 诊疗记录（Consultation）是MedicalCase聚合根的一部分（AR-001）
- 独立管理界面违反聚合根约束，应该只在MedicalCase上下文中访问
- 可能导致数据不一致

**解决方案** (🔴 P0):
```
删除: ConsultationManagementView.xaml
保留: ConsultationFormView.xaml (只在MedicalCaseFlowView中使用)
强制约束: Consultation只能通过MedicalCase聚合根访问
```

#### 问题4: 处方管理 - 三个界面功能重叠
**现状**:
- `PrescriptionsMainView.xaml` (主界面)
- `PrescriptionManagementView.xaml` (管理界面)
- `PrescriptionView.xaml` (详情界面)

**问题**:
- 三个界面定位模糊，功能重叠
- 用户体验混乱，不知道用哪个界面

**解决方案** (🔴 P0):
```
保留: PrescriptionManagementView (合并Main和Management功能)
保留: PrescriptionView (只读详情，用于打印和查看)
删除: PrescriptionsMainView (功能重复)
```

#### 问题5: 验方管理 - Detail和ViewDialog重复
**现状**:
- `FormulaDetailView.xaml` (详情页面)
- `ViewFormulaDialog.xaml` (查看对话框)

**问题**:
- 两个界面都是只读查看功能，完全重复

**解决方案** (🟡 P1):
```
保留: FormulaDetailView (支持只读和编辑两种模式)
删除: ViewFormulaDialog
```

### 2.2 组件化债务 (Issue #1790+)

根据ADR-009 Desktop端组件化模式，部分ViewModel需要重构：

#### ViewModel复杂度评估

| ViewModel | 行数 | 职责数 | 复杂度 | 优先级 |
|-----------|------|--------|--------|--------|
| PatientSelectionViewModel | 350 | 2 | ✅ Low | 已完成 Issue #1790 |
| MedicalCaseFlowViewModel | ~600 | 4 | 🔴 Critical | 🔴 P0 |
| PrescriptionEditorViewModel | ~500 | 3 | 🟡 High | 🟡 P1 |
| HerbManagementViewModel | ~400 | 3 | ⚠️ Medium | 🟡 P1 |
| FormulaManagementViewModel | ~450 | 3 | ⚠️ Medium | 🟡 P1 |

**解决方案** (🟡 P1):
```
MedicalCaseFlowViewModel:
  提取: MedicalCaseFlowManager (三步流程状态管理)
  提取: PrescriptionFlagHandler (处方标记处理)
  提取: CompletionHandler (完成逻辑)

PrescriptionEditorViewModel:
  提取: PrescriptionCalculator (价格计算)
  提取: FormulaImportHandler (验方导入)
  提取: HerbSelectionManager (药材选择)

HerbManagementViewModel:
  提取: HerbSearchManager (搜索和分页)

FormulaManagementViewModel:
  提取: FormulaSearchManager (搜索和分页)
  提取: FormulaValidationHandler (验方校验)
```

---

## 3. UX流程优化建议

### 3.1 三步看诊流程优化 (🟡 P1)

**当前流程** (需要跨4个界面):
```
1. PatientSelectionView → 选择患者
2. MedicalCaseFlowView → 创建医案
3. MedicalCaseConsultationView → 填写辨证（Step 1）
4. MedicalCaseFlowView → 标记处方需求（Step 2）
5. PrescriptionEditorView → 开处方（Step 3a）或 CompletionView → 完成（Step 3b）
```

**优化方案** - Wizard模式:
```
单一Wizard界面: MedicalCaseWizardView

Step 1: 患者信息确认 + 基本信息
  └─ 显示患者基本信息
  └─ 输入就诊日期、主诉

Step 2: 四诊辨证
  └─ 望、闻、问、切四诊表单
  └─ 中医诊断、治疗原则

Step 3: 处方决策
  └─ RadioBox: 是否需要处方？
  └─ 是 → Step 4
  └─ 否 → 直接完成

Step 4: 开具处方 (可选)
  └─ 选择验方导入 / 手动添加药材
  └─ 自动计算价格
  └─ 确认完成

优势:
  ✅ 流程清晰，一气呵成
  ✅ 减少界面跳转
  ✅ 符合业务规则BF-002（三步流程）
  ✅ 提升用户体验
```

### 3.2 患者快速创建优化 (✅ 已完成)

**当前设计** - QuickCreatePatientDialog:
- ✅ 设计良好，只包含必填字段
- ✅ 快速创建后立即选择患者
- ✅ 符合MVP原则

**无需优化**

### 3.3 处方打印和查看流程 (🟢 P2)

**当前流程**:
```
PrescriptionManagementView → 选择处方 → PrescriptionView → 打印
```

**优化方案**:
```
新增: 快速打印按钮（直接调用打印对话框，跳过预览）
新增: 批量打印功能（选择多个处方一次性打印）
新增: 打印模板选择（简洁版/详细版/病案带处方）
```

---

## 4. 现代化UI设计建议 (🟢 P2)

### 4.1 设计系统选择

**推荐方案**: 原生WPF组件标准化

**理由**:
- ✅ 遵循MVP原则（Constitution约束：先不引入第三方主题控件）
- ✅ 充分利用WPF内置控件能力
- ✅ 避免外部依赖和版本兼容问题
- ✅ 通过ResourceDictionary和ControlTemplate实现主题定制
- ✅ 性能更可控，调试更简单

**实施策略**:
- 创建统一的样式库（Styles/Themes.xaml）
- 使用ControlTemplate重新设计标准控件外观
- 通过Behavior和AttachedProperty扩展交互能力

### 4.2 UI组件标准化

#### 数据表格
**推荐**: WPF原生DataGrid + 自定义样式
- 统一列样式（HeaderStyle、CellStyle）
- 标准化分页控件（PageNavigationControl）
- 标准化筛选控件（FilterRow）
- 支持多选、单选、行内编辑

**实现方式**:
```xml
<Style x:Key="StandardDataGridStyle" TargetType="DataGrid" BasedOn="{StaticResource {x:Type DataGrid}}">
    <!-- 自定义Header、Cell、Selection样式 -->
</Style>
```

#### 表单输入
**推荐**: WPF标准控件（TextBox/ComboBox）+ 验证模板
- 统一验证错误显示（ErrorTemplate）
- 标准化占位符文本（Watermark AttachedProperty）
- 帮助提示（ToolTip统一样式）

**实现方式**:
```xml
<Style x:Key="StandardTextBoxStyle" TargetType="TextBox">
    <Setter Property="Validation.ErrorTemplate">
        <Setter.Value>
            <!-- 自定义验证错误UI -->
        </Setter.Value>
    </Setter>
</Style>
```

#### 对话框
**推荐**: Prism DialogService + 标准化对话框样式
- 统一模态对话框窗口样式（DialogWindowStyle）
- 标准化按钮布局（确定/取消/应用）
- 标准化标题栏和关闭按钮

#### 导航
**推荐**: Prism RegionManager + 自定义导航控件
- 左侧TreeView导航（适合8个模块的层级结构）
- 顶部TabControl切换（模块内切换）
- 面包屑导航（使用ItemsControl + DataTemplate）

### 4.3 主题设计

**配色方案** (中医文化适配):
```
Primary Color:
  - 深青色 (#006B5F) - 代表中医"青主肝"
  - 辅助色: 温润土黄 (#D4A574) - 代表"土主脾"

Secondary Color:
  - 朱红色 (#C8302A) - 代表"赤主心"（用于强调和操作按钮）

Background:
  - 亮色模式: #FAFAFA (浅灰白色，护眼)
  - 暗色模式: #121212 (深灰色，夜间使用)

Text:
  - 主文本: #212121 (深灰色)
  - 次文本: #757575 (中灰色)
  - 禁用: #BDBDBD (浅灰色)

Typography:
  - 标题: Microsoft YaHei UI Bold
  - 正文: Microsoft YaHei UI Regular
  - 代码: Consolas
```

### 4.4 无障碍改进 (🟢 P2)

**键盘导航**:
- ✅ 所有操作支持键盘快捷键
- ✅ Tab顺序符合逻辑
- ✅ 支持Ctrl+S保存、Esc取消

**屏幕阅读器**:
- ✅ 所有按钮添加AutomationProperties.Name
- ✅ 表单输入添加AutomationProperties.HelpText

**对比度**:
- ✅ 文本对比度≥4.5:1（WCAG AA标准）
- ✅ 重要按钮对比度≥7:1（WCAG AAA标准）

---

## 5. 实施计划 (Roadmap)

### Phase 1: 技术债务清理 (🔴 P0 - 1-2周)

**目标**: 删除冗余界面，简化UI结构

**任务清单**:
1. ✅ 删除 UserCreateView和UserEditView，新增UserFormDialog
2. ✅ 删除 MedicalCaseListView和OtherCasesQueryView
3. ✅ 删除 ConsultationManagementView
4. ✅ 删除 PrescriptionsMainView
5. ✅ 删除 ViewFormulaDialog
6. ✅ 更新所有导航路由和RegionManager配置
7. ✅ 更新单元测试

**预期成果**:
- UI文件数量: 39 → 34 (-13%)
- 代码维护成本: -30%
- 用户导航混乱度: -50%

### Phase 2: ViewModel组件化重构 (🟡 P1 - 3-4周)

**目标**: 应用ADR-009组件化模式到复杂ViewModel

**任务清单**:
1. ✅ MedicalCaseFlowViewModel组件化（行数600→<300）
   - 提取MedicalCaseFlowManager
   - 提取PrescriptionFlagHandler
   - 提取CompletionHandler
2. ✅ PrescriptionEditorViewModel组件化（行数500→<300）
   - 提取PrescriptionCalculator
   - 提取FormulaImportHandler
   - 提取HerbSelectionManager
3. ✅ HerbManagementViewModel组件化（行数400→<300）
4. ✅ FormulaManagementViewModel组件化（行数450→<300）

**预期成果**:
- ViewModel平均行数: -45%
- 单元测试Mock依赖: -50%
- 代码可维护性: +60%

### Phase 3: UX流程优化 (🟡 P1 - 2-3周)

**目标**: 优化核心业务流程，提升用户体验

**任务清单**:
1. ✅ 实现MedicalCaseWizardView（三步流程Wizard模式）
2. ✅ 优化处方打印流程（快速打印、批量打印）
3. ✅ 新增药材/验方批量导入功能（参考患者导入向导）

**预期成果**:
- 三步流程操作步骤: -40%
- 用户完成时间: -30%
- 用户满意度: +50%

### Phase 4: 现代化UI设计 (🟢 P2 - 1-2个月)

**目标**: 标准化原生WPF组件，提升UI一致性和无障碍体验

**任务清单**:
1. ✅ 创建统一样式库（Styles/Themes.xaml）
2. ✅ 应用中医文化配色主题（通过ResourceDictionary）
3. ✅ 标准化所有表格组件（WPF DataGrid + 自定义样式）
4. ✅ 标准化所有表单组件（TextBox/ComboBox + 验证模板）
5. ✅ 标准化所有对话框（Prism DialogService + 统一窗口样式）
6. ✅ 实现亮色/暗色主题切换（通过ResourceDictionary动态切换）
7. ✅ 无障碍改进（键盘导航、屏幕阅读器、对比度）

**预期成果**:
- UI一致性: +100%（所有组件遵循统一设计规范）
- 用户视觉满意度: +70%
- 无障碍得分: 符合WCAG AA标准
- 代码维护成本: -20%（统一样式库，避免分散定义）

---

## 6. WPF最佳实践对照

### 6.1 MVVM模式执行情况

**✅ 已遵循**:
- ✅ View和ViewModel完全分离
- ✅ 使用ICommand进行事件绑定
- ✅ 使用INotifyPropertyChanged进行数据绑定
- ✅ 使用Prism EventAggregator进行模块间通信
- ✅ 依赖注入（构造函数注入）

**⚠️ 可改进**:
- ⚠️ 部分View的Code-Behind包含业务逻辑（应该移到ViewModel）
- ⚠️ 部分直接调用DialogService而非通过Command（一致性问题）

### 6.2 性能优化

**✅ 已实施**:
- ✅ 虚拟化（VirtualizingStackPanel）用于长列表
- ✅ 分页查询（避免一次性加载大量数据）
- ✅ 异步操作（async/await）避免UI阻塞

**⚠️ 可改进**:
- ⚠️ 部分DataGrid未启用UI虚拟化（EnableRowVirtualization）
- ⚠️ 图片加载未使用延迟加载（如患者头像）

### 6.3 资源管理

**✅ 已实施**:
- ✅ 使用ResourceDictionary管理样式和模板
- ✅ 使用合并字典（MergedDictionaries）组织资源

**⚠️ 可改进**:
- ⚠️ 部分样式定义在View内部（应该提取到共享ResourceDictionary）
- ⚠️ 缺少统一的样式库（颜色、字体、间距）

---

## 7. 风险评估与缓解

### 7.1 重构风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 删除界面导致功能缺失 | 中 | 高 | 删除前完整回归测试 |
| 组件化重构引入Bug | 中 | 中 | 单元测试覆盖率≥80% |
| UI设计不符合用户习惯 | 低 | 中 | 用户访谈和A/B测试 |
| 性能回归 | 低 | 中 | 性能基准测试 |
| 开发周期延误 | 中 | 低 | 分Phase实施，可迭代交付 |

### 7.2 回滚策略

**Phase 1 (删除界面)**:
- ✅ 创建功能分支: `refactor/ui-cleanup-phase1`
- ✅ 保留Git历史，支持快速回滚
- ✅ 完整回归测试通过后才合并到master

**Phase 2 (组件化)**:
- ✅ 每个ViewModel独立分支（如`refactor/medicalcase-flow-componentization`）
- ✅ 保留旧ViewModel作为备份（重命名为`*ViewModelLegacy.cs`）
- ✅ 完成单元测试和E2E测试后才删除旧代码

**Phase 3-4 (UX优化和UI设计)**:
- ✅ 使用Feature Toggle控制新功能开关
- ✅ 支持新旧界面共存（通过配置切换）
- ✅ 收集用户反馈后才全面切换

---

## 8. 验收标准 (Acceptance Criteria)

### Phase 1: 技术债务清理

- [ ] 所有冗余界面已删除（5个XAML文件）
- [ ] 所有导航路由已更新并测试通过
- [ ] 所有单元测试通过（覆盖率≥当前水平）
- [ ] 回归测试通过（8个核心模块功能完整）
- [ ] 代码审查通过（至少1名架构师审查）

### Phase 2: ViewModel组件化

- [ ] 4个复杂ViewModel已组件化（行数<300行）
- [ ] 单元测试覆盖率≥80%
- [ ] Mock依赖数量减少≥50%
- [ ] 代码复杂度分析通过（方法<50行）
- [ ] 符合ADR-009组件化规范

### Phase 3: UX流程优化

- [ ] MedicalCaseWizardView实现并测试通过
- [ ] 三步流程操作步骤减少≥40%
- [ ] 用户完成时间减少≥30%（通过用户测试验证）
- [ ] 处方打印流程优化（快速打印、批量打印）
- [ ] 用户满意度调查得分≥4.0/5.0

### Phase 4: 现代化UI设计

- [ ] 统一样式库（Styles/Themes.xaml）创建完成
- [ ] 中医文化主题应用到所有界面（通过ResourceDictionary）
- [ ] 所有DataGrid应用StandardDataGridStyle
- [ ] 所有TextBox/ComboBox应用标准验证模板
- [ ] 所有Dialog应用统一窗口样式
- [ ] 亮色/暗色主题切换功能完成
- [ ] 无障碍测试通过（WCAG AA标准）
- [ ] 性能基准测试通过（UI响应时间<100ms）

---

## 9. 参考资源

### 9.1 项目内部文档
- `docs/explanation/architecture/client/README.md` - Desktop端架构指南
- `docs/explanation/architecture/decisions/ADR-009-desktop-component-pattern.md` - 组件化ADR
- `docs/explanation/business-rules.md` - 业务规则文档
- `docs/reference/quick-reference/api-reference.md` - API完整参考
- `.spec-workflow/steering/constitution.md` - 项目宪法

### 9.2 WPF最佳实践
- [Microsoft WPF Documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [WPF Control Styling and Templating](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/control-styles-and-templates)
- [Prism Library Documentation](https://prismlibrary.com/docs/)
- [MVVM Pattern Guide](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [WPF ResourceDictionary Guide](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/systems/xaml-resources-define)

### 9.3 设计资源
- [WPF UI Design Principles](https://learn.microsoft.com/en-us/windows/apps/design/)
- [中医色彩文化研究](https://www.zhongyiyao.net/wenhua/secai/)
- [WCAG 2.1 Accessibility Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [WPF Triggers and Behaviors](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/styling-and-templating)

---

## 10. 附录

### 10.1 Desktop UI完整清单

**Auth模块** (2个):
- LoginView.xaml
- LoginWindow.xaml

**Users模块** (7个):
- UserManagementView.xaml
- UserDetailView.xaml
- UserCreateView.xaml ⚠️ 建议删除
- UserEditView.xaml ⚠️ 建议删除
- UserProfileDialog.xaml
- ChangePasswordDialog.xaml
- ResetPasswordDialog.xaml

**Patients模块** (5个):
- PatientSelectionView.xaml ✅ Issue #1790已优化
- PatientDetailView.xaml
- QuickCreatePatientDialog.xaml
- UnfinishedCaseDialog.xaml
- PatientImportWizardView.xaml

**MedicalCase模块** (8个):
- MedicalCaseFlowView.xaml
- MedicalCaseConsultationView.xaml
- PrescriptionEditorView.xaml
- CompletionView.xaml
- MedicalCaseDetailView.xaml
- MedicalCaseManagementView.xaml ⚠️ 建议保留并优化
- MedicalCaseListView.xaml ⚠️ 建议删除
- OtherCasesQueryView.xaml ⚠️ 建议删除

**Consultation模块** (2个):
- ConsultationFormView.xaml
- ConsultationManagementView.xaml ⚠️ 建议删除

**Prescriptions模块** (7个):
- PrescriptionManagementView.xaml
- PrescriptionView.xaml
- PrescriptionsMainView.xaml ⚠️ 建议删除
- PrescriptionEditorDialog.xaml
- HerbSelectionDialog.xaml
- SelectFormulaDialog.xaml
- FormulaTemplateDialog.xaml

**Herbs模块** (2个):
- HerbManagementView.xaml
- HerbDetailView.xaml

**Formula模块** (5个):
- FormulaManagementView.xaml
- FormulaDetailView.xaml
- FormulaValidationView.xaml
- EditFormulaDialog.xaml
- ViewFormulaDialog.xaml ⚠️ 建议删除

**总计**: 39个XAML视图

### 10.2 API覆盖度统计

| 模块 | API端点总数 | 已覆盖UI | 覆盖率 |
|------|-----------|---------|--------|
| Auth | 5 | 4 | 80% |
| Users | 9 | 9 | 100% |
| Patients | 7 | 7 | 100% |
| MedicalCase | 14 | 14 | 100% |
| Consultation | 3 | 3 | 100% |
| Prescriptions | 11 | 8 | 73% |
| Herbs | 9 | 6 | 67% |
| Formula | 14 | 9 | 64% |
| **总计** | **72** | **60** | **83%** |

---

**文档状态**: ✅ 待用户确认
**下一步**: 用户确认后→创建Phase 1实施PRD文档

**最后更新**: 2025-11-04
