# Proposal: refactor-admin-workspace

## Summary

**Admin工作台架构重构** - 将管理类MasterDetailView重构为可复用Control模式，实现"View在角色台，Control在业务模块"的架构统一。

## Background

### 当前问题

1. **架构不一致**: MasterDetailView分散在各业务模块，违背角色台架构原则
2. **职责混淆**: 业务模块同时包含Views和Controls，边界模糊
3. **复用困难**: 多个角色台导航到同一View，无法实现差异化

### 关键发现

**MasterDetailView使用情况分析** (2025-01-03):

| View | Admin使用位置 | Clinical使用位置 | 重构策略 |
|------|---------------|------------------|----------|
| PatientMasterDetailView | AdminHomeViewModel:125 | ClinicalHomeViewModel:182 | **重构为Control** |
| HerbMasterDetailView | AdminHomeViewModel:124 | ClinicalHomeViewModel:215 | **重构为Control** |
| FormulaMasterDetailView | AdminHomeViewModel:126 | ClinicalHomeViewModel:231 | **重构为Control** |
| UserMasterDetailView | AdminHomeViewModel:123 | (待添加) | **重构为Control** |
| MedicalCaseMasterDetailView | AdminHomeViewModel:128 | ClinicalHomeViewModel:199 | **重构为Control** |

**结论**: 5个MasterDetailView全部需要重构为Control模式，实现跨角色台复用。

> **注**: UserMasterDetailView当前仅Admin使用，但为保持架构一致性，仍采用Control模式重构。

### 架构原则

| 层级 | 位置 | 职责 | 示例 |
|------|------|------|------|
| **页面视图(View)** | Roles/*/Views/ | 页面布局、导航、角色操作 | AdminHerbManagementView |
| **对话框(Dialog)** | Modules/*/Dialogs/ | 模态交互、确认操作 | EditFormulaDialog |
| **控件(Control)** | Modules/*/Controls/ | 可复用业务组件 | HerbMasterDetailControl |

---

## Architecture Context

### 目标架构

**核心原则**: 业务模块提供可复用Control，角色台创建View组合这些Control

```
LYBT.Desktop.Admin/                       # 角色模块 - 管理员
├── Views/
│   ├── AdminHomeView.xaml                # 主页 (已存在)
│   ├── SystemSettingsView.xaml           # 系统设置 (已存在)
│   ├── HerbManagementView.xaml           # 药材管理 (使用Control)
│   ├── FormulaManagementView.xaml        # 验方管理 (使用Control)
│   ├── PatientManagementView.xaml        # 患者管理 (使用Control)
│   ├── UserManagementView.xaml           # 用户管理 (使用Control)
│   └── MedicalCaseManagementView.xaml    # 医案管理 (使用Control)
└── ViewModels/
    ├── AdminHomeViewModel.cs             # (已存在)
    ├── SystemSettingsViewModel.cs        # (已存在)
    └── (Views使用Prism AutoWireViewModel)

LYBT.Desktop.Clinical/                    # 角色模块 - 医生
├── Views/
│   ├── ClinicalHomeView.xaml             # 主页 (已存在)
│   ├── PatientSelectionView.xaml         # 患者选择 (已存在)
│   ├── HerbReferenceView.xaml            # 药材参考 (使用Control)
│   ├── FormulaReferenceView.xaml         # 验方参考 (使用Control)
│   ├── PatientHistoryView.xaml           # 患者历史 (使用Control)
│   └── MedicalCaseArchiveView.xaml       # 医案归档 (使用Control)
└── ViewModels/
    └── ...

LYBT.Desktop.Herbs/                       # 业务模块 - 药材
├── Controls/
│   ├── HerbMasterDetailControl.xaml      # 药材管理控件 (新建，核心复用)
│   ├── HerbDetailControl.xaml            # 药材详情控件 (已存在)
│   └── HerbListControl.xaml              # 药材列表控件 (已存在)
├── Dialogs/
│   └── (对话框)
└── Services/
```

### 控件复用场景

| Control | 所属模块 | Admin使用 | Clinical使用 |
|---------|----------|-----------|--------------|
| HerbMasterDetailControl | Herbs | HerbManagementView | HerbReferenceView |
| FormulaMasterDetailControl | Formula | FormulaManagementView | FormulaReferenceView |
| PatientMasterDetailControl | Patients | PatientManagementView | PatientHistoryView |
| MedicalCaseMasterDetailControl | MedicalCase | MedicalCaseManagementView | MedicalCaseArchiveView |

**好处**:
1. **复用性**: 同一Control可被多个角色台使用
2. **一致性**: 相同业务逻辑，统一的UI表现
3. **灵活性**: 各角色台可添加角色特定的操作按钮
4. **维护性**: 修改Control，所有使用处自动更新

### View与Control的职责划分

**Control职责** (业务模块):
- MasterDetail布局结构
- 列表加载和分页
- 详情展示
- CRUD核心逻辑
- 数据绑定属性暴露

**View职责** (角色台模块):
- 页面标题和导航栏
- 角色特定操作按钮
- 权限控制
- 返回导航
- 工具栏定制

---

## Scope

### In Scope

**Phase A: 重构为Control模式** (核心工作):
- HerbMasterDetailView → HerbMasterDetailControl
- FormulaMasterDetailView → FormulaMasterDetailControl
- PatientMasterDetailView → PatientMasterDetailControl
- MedicalCaseMasterDetailView → MedicalCaseMasterDetailControl
- UserMasterDetailView → UserMasterDetailControl

**Phase B: 创建角色台View**:
- Admin/Views/下创建各ManagementView（5个）
- Clinical/Views/下创建各ReferenceView（4个，Users暂无）

**Phase D: 清理与验证**:
- 删除业务模块中的旧MasterDetailView
- 更新模块注册
- 编译验证

### Out of Scope

- 登录视图(Auth模块，全局特殊)
- 用户自服务视图(ChangePasswordView等)
- 对话框类视图(保留在业务模块)
- 打印模板(特殊类型)
- Reception角色台(待后续规划)

---

## Refactoring Strategy

### Strategy 1: 渐进式重构 (推荐)

**步骤**:
1. 将现有MasterDetailView重命名为MasterDetailControl
2. 更新命名空间为Controls
3. 移动到Controls目录
4. 创建薄包装View在角色台
5. 更新导航注册

**优点**: 最小改动，风险低，可逐模块进行

### Strategy 2: 完全重写

**步骤**:
1. 从头创建MasterDetailControl
2. 抽取现有ViewModel逻辑
3. 创建新View

**缺点**: 工作量大，容易引入Bug

### 选择: Strategy 1

---

## Migration Plan

### Phase A-1: Herbs模块重构

| 操作 | 文件 |
|------|------|
| 重命名 | `HerbMasterDetailView.xaml` → `HerbMasterDetailControl.xaml` |
| 移动 | Views/ → Controls/ |
| 更新 | 命名空间为 `LYBT.Desktop.Herbs.Controls` |
| 更新 | ViewModel适配Control模式 |
| 更新 | HerbsModule.cs注册 |

### Phase A-2: Formula模块重构

| 操作 | 文件 |
|------|------|
| 重命名 | `FormulaMasterDetailView.xaml` → `FormulaMasterDetailControl.xaml` |
| 移动 | Views/ → Controls/ |
| 更新 | 命名空间为 `LYBT.Desktop.Formula.Controls` |

### Phase A-3: Patients模块重构

| 操作 | 文件 |
|------|------|
| 重命名 | `PatientMasterDetailView.xaml` → `PatientMasterDetailControl.xaml` |
| 移动 | Views/ → Controls/ |
| 更新 | 命名空间为 `LYBT.Desktop.Patients.Controls` |

### Phase A-4: MedicalCase模块重构

| 操作 | 文件 |
|------|------|
| 重命名 | `MedicalCaseMasterDetailView.xaml` → `MedicalCaseMasterDetailControl.xaml` |
| 移动 | Views/ → Controls/ |
| 更新 | 命名空间为 `LYBT.Desktop.MedicalCase.Controls` |

### Phase B-1: Admin角色台View创建

| 操作 | 文件 |
|------|------|
| 创建 | `Admin/Views/HerbManagementView.xaml` - 使用HerbMasterDetailControl |
| 创建 | `Admin/Views/FormulaManagementView.xaml` - 使用FormulaMasterDetailControl |
| 创建 | `Admin/Views/PatientManagementView.xaml` - 使用PatientMasterDetailControl |
| 创建 | `Admin/Views/MedicalCaseManagementView.xaml` - 使用MedicalCaseMasterDetailControl |
| 更新 | AdminModule.cs注册 |
| 更新 | AdminHomeViewModel导航目标 |

### Phase B-2: Clinical角色台View创建

| 操作 | 文件 |
|------|------|
| 创建 | `Clinical/Views/HerbReferenceView.xaml` - 使用HerbMasterDetailControl |
| 创建 | `Clinical/Views/FormulaReferenceView.xaml` - 使用FormulaMasterDetailControl |
| 创建 | `Clinical/Views/PatientHistoryView.xaml` - 使用PatientMasterDetailControl |
| 创建 | `Clinical/Views/MedicalCaseArchiveView.xaml` - 使用MedicalCaseMasterDetailControl |
| 更新 | ClinicalModule.cs注册 |
| 更新 | ClinicalHomeViewModel导航目标 |

### Phase A-5: Users模块重构

| 操作 | 文件 |
|------|------|
| 重命名 | `UserMasterDetailView.xaml` → `UserMasterDetailControl.xaml` |
| 移动 | Views/ → Controls/ |
| 更新 | 命名空间为 `LYBT.Desktop.Users.Controls` |
| 更新 | UsersModule.cs注册 |

> **架构一致性**: 虽然当前仅Admin使用，但采用Control模式为未来扩展提供便捷。

### Phase D: 清理与验证

- 删除业务模块Views/目录下的旧MasterDetailView
- 确保业务模块Views/仅保留Dialog和特殊View
- 编译验证 0 errors
- 导航功能测试

---

## Files to Create

| 新文件 | 说明 |
|--------|------|
| Herbs/Controls/HerbMasterDetailControl.xaml | 从View重构 |
| Formula/Controls/FormulaMasterDetailControl.xaml | 从View重构 |
| Patients/Controls/PatientMasterDetailControl.xaml | 从View重构 |
| MedicalCase/Controls/MedicalCaseMasterDetailControl.xaml | 从View重构 |
| Users/Controls/UserMasterDetailControl.xaml | 从View重构 |
| Admin/Views/HerbManagementView.xaml | 新建薄包装 |
| Admin/Views/FormulaManagementView.xaml | 新建薄包装 |
| Admin/Views/PatientManagementView.xaml | 新建薄包装 |
| Admin/Views/MedicalCaseManagementView.xaml | 新建薄包装 |
| Admin/Views/UserManagementView.xaml | 新建薄包装 |
| Clinical/Views/HerbReferenceView.xaml | 新建薄包装 |
| Clinical/Views/FormulaReferenceView.xaml | 新建薄包装 |
| Clinical/Views/PatientHistoryView.xaml | 新建薄包装 |
| Clinical/Views/MedicalCaseArchiveView.xaml | 新建薄包装 |

## Files to Delete

| 删除文件 | 说明 |
|----------|------|
| Herbs/Views/HerbMasterDetailView.xaml | 重构为Control后删除 |
| Formula/Views/FormulaMasterDetailView.xaml | 重构为Control后删除 |
| Patients/Views/PatientMasterDetailView.xaml | 重构为Control后删除 |
| MedicalCase/Views/MedicalCaseMasterDetailView.xaml | 重构为Control后删除 |
| Users/Views/UserMasterDetailView.xaml | 重构为Control后删除 |

## Files to Modify

| 文件 | 修改内容 |
|------|----------|
| AdminModule.cs | 注册新Views |
| ClinicalModule.cs | 注册新Views |
| HerbsModule.cs | 注册Control，删除View注册 |
| FormulaModule.cs | 注册Control，删除View注册 |
| PatientsModule.cs | 注册Control，删除View注册 |
| MedicalCaseModule.cs | 注册Control，删除View注册 |
| UsersModule.cs | 注册Control，删除View注册 |
| AdminHomeViewModel.cs | 更新导航目标 |
| ClinicalHomeViewModel.cs | 更新导航目标 |

---

## Dependencies

- `MasterDetailLayout` 控件 (Infrastructure)
- 各业务模块的Services (通过DI注入)
- 各业务模块的Controls (跨模块引用)

---

## Risks

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 跨模块引用增加 | Admin/Clinical需引用多个业务模块 | 已有的项目引用机制 |
| 命名空间变更 | 需更新所有引用 | 使用IDE重构工具 |
| 导航路径变更 | 其他模块导航可能失效 | 统一使用新视图名称导航 |
| ViewModel适配 | Control模式需要调整ViewModel | 渐进式重构减少影响 |

---

## Success Criteria

1. **架构一致**: 所有MasterDetailView都重构为Control+View模式
2. **复用实现**: 同一Control被Admin和Clinical共享
3. **职责清晰**: 业务模块仅保留Controls和Dialogs
4. **编译通过**: 无错误无警告
5. **导航正常**: 所有管理功能导航正常工作
6. **功能不变**: 管理功能不受影响

---

## 历史决策记录

| 日期 | 决策 | 原因 |
|------|------|------|
| 2025-01-03 | 从"迁移View"改为"重构为Control"模式 | PatientMasterDetailView等被多个角色台共用，单纯迁移无法解决复用问题 |
| 2025-01-03 | UserMasterDetailView也采用Control模式 | 虽当前仅Admin使用，但为保持架构一致性并为未来扩展提供便捷 |
