# Workstation架构重构需求讨论

> **Issue**: #1513 - [Epic] Workstation架构重构 - 角色业务模块化
> **讨论日期**: 2025-10-21
> **状态**: 🔄 讨论中

---

## 📋 背景概述

**当前架构问题**：
- Workstation作为"UI容器"设计（ClinicalWorkstation、AdminWorkstation）
- HomeView位于Shell模块，但实际是医生角色的首页
- 导航层次不清晰：LoginView → HomeView → Workstation容器 → 业务视图

**目标架构**：
- ❌ 取消Workstation作为"UI容器"的设计
- ✅ 改造为"角色业务模块"设计（Clinical、Reception、Pharmacy、Admin）
- ✅ 简化导航：LoginView → RoleHomeView → 业务视图
- ✅ 只使用Shell的ContentRegion，无子Region嵌套

---

## 🎯 核心设计决策

### Q1: 角色模块命名规范

🔄 **讨论中**

**当前状态**：
- 现有模块：ClinicalWorkstation、AdminWorkstation
- 现有视图：HomeView（实际是医生首页）

**项目现有命名规范**：
```
src/Client/Desktop/Modules/
├─ AuthenticationModule.cs
├─ ConsultationModule.cs
├─ FormulaModule.cs
├─ HerbsModule.cs
├─ PatientsModule.cs
└─ UsersModule.cs
```
➡️ 项目统一使用 `{功能名}Module` 简洁命名

**方案对比**：

#### 方案A: 保持 Workstation 命名
```
ClinicalWorkstationModule
ReceptionWorkstationModule
PharmacyWorkstationModule
AdminWorkstationModule
```

**优点**：
- ✅ 语义明确，"Workstation"暗示工作台/工作区
- ✅ 与当前命名保持一致，迁移成本低
- ✅ 对用户更友好（一看就知道是工作台）

**缺点**：
- ❌ 名称较长（22-29字符）
- ❌ **与ADR-003设计目标冲突**："取消Workstation作为'UI容器'的设计"
- ❌ **架构语义不准确**：如果不再是"工作台容器"，名称会产生误导
- ❌ **与项目命名规范不一致**：其他业务模块都用简洁命名（如 `PatientsModule`）

---

#### 方案B: 简化为 {Role}Module
```
ClinicalModule
ReceptionModule
PharmacyModule
AdminModule
```

**优点**：
- ✅ **符合项目命名规范**：与 `PatientsModule`、`ConsultationModule` 等保持一致
- ✅ **符合ADR-003目标**："改造为'角色业务模块'设计"
- ✅ **简洁明了**（14-17字符）
- ✅ **更通用、更灵活**：不绑定特定设计概念（Workstation），未来架构调整不需要改名
- ✅ **Prism最佳实践**：模块类以 `Module` 结尾是Prism推荐命名

**缺点**：
- ❌ Module 这个词比较通用，可能需要从上下文理解具体含义
- ❌ 需要重命名现有模块（但这是本次重构的目标之一）

---

**我的建议**：

推荐 **方案B（{Role}Module）**，理由：

1. **架构一致性**：Issue #1513的核心目标就是"取消Workstation作为UI容器的设计"，继续使用Workstation命名会产生概念混淆
2. **代码规范统一**：与项目现有8个业务模块（`PatientsModule`、`ConsultationModule`等）保持命名一致性
3. **可维护性**：简洁命名减少代码冗余，提升可读性
4. **未来扩展性**：如果未来角色模块的职责发生变化，`{Role}Module` 这个通用名称仍然适用

---

**用户反馈（2025-10-21）**：

> "我采纳简洁的出发点。但是我在结构上也做了区分。一边是业务。一边其实是权限。单纯从简洁统一来说我也愿意采纳你的建议。但是最好要保证用户能区分模块的大分类就行。"

**核心需求**：
- ✅ 采纳简洁命名（{Role}Module）
- ✅ 但需要明确区分：**业务模块** vs **角色/权限模块**

**解决方案：通过目录结构区分大分类**

```
src/Client/Desktop/
├─ Modules/                    # 【业务模块】- 通用功能
│  ├─ Patients/               # 患者管理
│  │  └─ PatientsModule.cs
│  ├─ Consultation/           # 诊疗记录
│  │  └─ ConsultationModule.cs
│  ├─ Prescriptions/          # 处方管理
│  │  └─ PrescriptionsModule.cs
│  └─ Herbs/                  # 药材管理
│     └─ HerbsModule.cs
│
└─ Roles/                      # 【角色模块】- 角色权限相关
   ├─ Clinical/               # 医生角色
   │  └─ ClinicalModule.cs
   ├─ Reception/              # 前台角色
   │  └─ ReceptionModule.cs
   ├─ Pharmacy/               # 药房角色
   │  └─ PharmacyModule.cs
   └─ Admin/                  # 管理角色
      └─ AdminModule.cs
```

**优点**：
- ✅ **类名保持简洁统一**：`ClinicalModule`、`PatientsModule` 命名风格一致
- ✅ **目录名明确分类**：`Modules/` vs `Roles/` 一看就知道大分类
- ✅ **路径即语义**：
  - `LYBT.Desktop.Modules.Patients` → 业务模块
  - `LYBT.Desktop.Roles.Clinical` → 角色模块
- ✅ **符合ADR-003目标**：目录名用 `Roles` 而非 `Workstations`，避免"工作台容器"的误导

**用户补充说明（2025-10-21）**：

> "因为后期肯定会根据权限开发一些其他功能的。"

**关键洞察**：
- `Roles/` 目录不仅容纳4个角色模块
- **后期会扩展权限相关的其他功能**（如权限验证组件、角色切换服务、权限缓存等）
- 需要一个清晰的目录作为**权限功能的统一入口**

**最终方案确认**：
1. ✅ 模块类名采用简洁命名：`ClinicalModule`、`ReceptionModule`、`PharmacyModule`、`AdminModule`
2. ✅ 目录名从 `Workstations/` 改为 `Roles/`
3. ✅ 视图命名：`ClinicalHomeView`、`ReceptionHomeView`、`PharmacyHomeView`、`AdminHomeView`

**扩展空间示例**：
```
src/Client/Desktop/Roles/
├─ Clinical/               # 医生角色模块
├─ Reception/              # 前台角色模块
├─ Pharmacy/               # 药房角色模块
├─ Admin/                  # 管理角色模块
├─ Shared/                 # 【未来】角色共享组件
│  ├─ RoleSwitcher/       # 角色切换服务
│  ├─ PermissionGuard/    # 权限守卫组件
│  └─ RoleCache/          # 角色缓存服务
└─ [其他权限相关功能...]
```

---

### Q2: 目录结构详细设计

✅ **Q1确认结果**：目录名 `Workstations/` → `Roles/`

🔄 **Q2讨论内容**：具体目录结构与命名空间设计

---

**项目标准模块结构（参考）**：
```
src/Client/Desktop/Modules/
└─ LYBT.Desktop.Patients/
   ├─ Interfaces/
   ├─ Models/
   ├─ Repositories/
   ├─ ViewModels/
   ├─ Views/
   └─ PatientsModule.cs
```
➡️ 命名空间：`LYBT.Desktop.Patients`

---

**当前Workstation结构**：
```
src/Client/Desktop/Workstations/
├─ ClinicalWorkstation/
│  ├─ Navigation/
│  │  ├─ ClinicalNavigator.cs
│  │  └─ IClinicalNavigator.cs
│  ├─ ViewModels/
│  │  └─ ClinicalWorkstationViewModel.cs
│  ├─ Views/
│  │  ├─ ClinicalWorkstationView.xaml
│  │  ├─ Management/
│  │  │  └─ DataManagementView.xaml
│  │  └─ Workflow/
│  │     └─ ClinicalWorkflowView.xaml
│  └─ ClinicalWorkstationModule.cs
│
└─ AdminWorkstation/
   ├─ ViewModels/
   │  └─ AdminWorkstationViewModel.cs
   ├─ Views/
   │  └─ AdminWorkstationView.xaml
   └─ AdminWorkstationModule.cs
```

➡️ 命名空间：`LYBT.Desktop.ClinicalWorkstation`

---

**方案A：保持 LYBT.Desktop.{Role} 独立命名空间**

```
src/Client/Desktop/
└─ Roles/
   ├─ LYBT.Desktop.Clinical/          # 独立项目
   │  ├─ Navigation/
   │  ├─ ViewModels/
   │  ├─ Views/
   │  │  ├─ ClinicalHomeView.xaml    # 医生首页
   │  │  └─ [其他视图...]
   │  └─ ClinicalModule.cs
   │
   ├─ LYBT.Desktop.Reception/         # 独立项目
   │  └─ [同上结构]
   │
   ├─ LYBT.Desktop.Pharmacy/          # 独立项目
   │  └─ [同上结构]
   │
   └─ LYBT.Desktop.Admin/             # 独立项目
      └─ [同上结构]
```

**命名空间**：
- `LYBT.Desktop.Clinical`
- `LYBT.Desktop.Reception`
- `LYBT.Desktop.Pharmacy`
- `LYBT.Desktop.Admin`

**优点**：
- ✅ 与业务模块命名风格一致（`LYBT.Desktop.Patients`）
- ✅ 简洁明了，命名空间扁平化
- ✅ 每个角色独立项目，便于模块化管理

**缺点**：
- ❌ 从命名空间看不出是"角色模块"还是"业务模块"

---

**方案B：统一 LYBT.Desktop.Roles.{Role} 命名空间**

```
src/Client/Desktop/
└─ Roles/
   ├─ LYBT.Desktop.Roles.Clinical/    # 独立项目
   │  └─ [同方案A结构]
   │
   ├─ LYBT.Desktop.Roles.Reception/   # 独立项目
   ├─ LYBT.Desktop.Roles.Pharmacy/    # 独立项目
   └─ LYBT.Desktop.Roles.Admin/       # 独立项目
```

**命名空间**：
- `LYBT.Desktop.Roles.Clinical`
- `LYBT.Desktop.Roles.Reception`
- `LYBT.Desktop.Roles.Pharmacy`
- `LYBT.Desktop.Roles.Admin`

**优点**：
- ✅ **命名空间明确分类**：一看就知道是角色模块
- ✅ **路径与命名空间对齐**：`Roles/LYBT.Desktop.Roles.Clinical`
- ✅ **支持未来扩展**：如 `LYBT.Desktop.Roles.Shared`

**缺点**：
- ❌ 命名空间较长（多一层 `.Roles`）
- ❌ 与业务模块命名风格不一致

---

**我的建议**：

推荐 **方案A（LYBT.Desktop.{Role}）**，理由：

1. **命名简洁性**：用户已明确希望"简洁统一"，方案A更符合这一原则
2. **代码一致性**：与业务模块命名保持一致（`LYBT.Desktop.Patients`）
3. **目录已区分**：`Roles/` vs `Modules/` 目录已经明确分类，命名空间无需再重复
4. **易用性**：开发者使用时更直观（`using LYBT.Desktop.Clinical` vs `using LYBT.Desktop.Roles.Clinical`）

**用户确认（2025-10-21）**：
1. ✅ **采用方案A**：命名空间 `LYBT.Desktop.Clinical`、`LYBT.Desktop.Reception` 等
2. ❓ HomeView 是否迁移到 Clinical 模块（改名为 `ClinicalHomeView`）？
3. ❓ 是否删除 ClinicalWorkstationView/AdminWorkstationView 容器视图？

---

**Q2-2：HomeView 迁移与重命名**

**当前状态**：
- `HomeView` 位于 `Shell` 模块（`src/Client/Desktop/Shell/Views/HomeView.xaml`）
- 命名空间：`LYBT.Desktop.Shell.Views`
- **实际用途**：医生角色的首页（而非通用首页）

**提议方案**：
```
迁移路径：
  Shell/Views/HomeView.xaml
    → Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml

命名空间：
  LYBT.Desktop.Shell.Views.HomeView
    → LYBT.Desktop.Clinical.Views.ClinicalHomeView

导航键：
  "HomeView"
    → "ClinicalHomeView"
```

**理由**：
- ✅ **语义准确**：HomeView 实际是医生首页，应该属于 Clinical 模块
- ✅ **架构清晰**：Shell 只负责框架容器，不包含业务首页
- ✅ **符合ADR-003**："简化导航层次，LoginView → RoleHomeView → 业务视图"
- ✅ **未来扩展**：其他角色也可有自己的 HomeView（ReceptionHomeView、PharmacyHomeView）

**用户确认（2025-10-21）**：
✅ **需要迁移** HomeView → ClinicalHomeView（迁移到 Clinical 模块）

**用户补充需求**：
> "需要增加用户登录角色判断逻辑。每个角色都有自己的主页。"

**架构影响**：
1. **每个角色模块都需要自己的 HomeView**：
   - `ClinicalHomeView`（医生主页）
   - `ReceptionHomeView`（前台主页）
   - `PharmacyHomeView`（药房主页）
   - `AdminHomeView`（管理主页）

2. **登录后需要角色判断逻辑**：
   - LoginViewModel 根据用户角色导航到对应的 RoleHomeView
   - 需要角色路由服务/策略模式

**用户确认实现方式（2025-10-21）**：
✅ **采用独立的 RoleNavigationService**

**设计要点**：
```csharp
// 服务接口
public interface IRoleNavigationService
{
    void NavigateToRoleHome(string roleName);
}

// 使用示例（LoginViewModel）
private void OnLoginSuccess()
{
    var userRole = _currentUser.Role; // "Doctor"/"Receptionist"/"Pharmacist"/"Admin"
    _roleNavigationService.NavigateToRoleHome(userRole);
}
```

**服务位置**：
- 推荐：`src/Client/Desktop/Core/Services/Navigation/RoleNavigationService.cs`
- 或：`src/Client/Desktop/Roles/Shared/Services/RoleNavigationService.cs`

**路由映射**：
```
"Doctor"       → ClinicalHomeView
"Receptionist" → ReceptionHomeView
"Pharmacist"   → PharmacyHomeView
"Admin"        → AdminHomeView
```

---

**Q2-3：容器视图删除确认**

**当前状态**：
- `ClinicalWorkstationView`（诊疗工作台视图）
- `AdminWorkstationView`（管理工作台视图）
- 这些视图作为"UI容器"，包含导航区域（ContentRegion）

**ADR-003 设计目标**：
- ❌ 取消 Workstation 作为"UI容器"的设计
- ✅ 只使用 Shell 的 ContentRegion，无子 Region 嵌套
- ✅ 简化导航：LoginView → RoleHomeView → 业务视图

**提议方案**：
- **Phase 3 删除**：ClinicalWorkstationView、AdminWorkstationView
- **替代方案**：直接导航到 ClinicalHomeView，使用 Shell.ContentRegion

**示例导航流程**：
```
旧架构：
  LoginView → HomeView → ClinicalWorkstationView（容器） → 业务视图

新架构：
  LoginView → ClinicalHomeView → 业务视图
             ↑ 直接在 Shell.ContentRegion 中显示
```

**容器视图内容分析（2025-10-21）**：

### ClinicalWorkstationView 可用内容

**✅ 需要迁移到 ClinicalHomeView**：
1. **导航菜单UI**（侧边栏200px + 4个菜单项）：
   - 📋 诊断录入 (Diagnosis)
   - 💊 处方开具 (Prescription)
   - 🏥 患者管理 (PatientManagement)
   - 📚 历史记录 (History)
2. **菜单按钮样式** (`MenuRadioButtonStyle`) - 可复用
3. **导航逻辑** (`ExecuteNavigate` + 路由映射)
4. **ContentRegion** (`ClinicalContentRegion`) - 改为使用 Shell.ContentRegion

**❌ 不迁移（已废弃）**：
- DiagnosisData（诊断数据模型 - 测试数据）
- PrescriptionGrid（处方网格 - 已有专门模块）
- FormulaTemplates（验方模板 - 已有专门模块）
- SelectPatientCommand（Issue #1547已废弃）

---

### AdminWorkstationView 可用内容

**✅ 需要迁移到 AdminHomeView**：
1. **导航菜单UI**（侧边栏200px + 6个菜单项）：
   - 👤 用户管理 (UserManagement)
   - 🌿 药材管理 (HerbManagement)
   - 🏥 患者管理 (PatientManagement)
   - 📋 验方管理 (FormulaManagement)
   - 📁 病历管理 (MedicalCaseManagement)
   - ⚙️ 系统设置 (SystemSettings)
2. **菜单按钮样式** (`MenuRadioButtonStyle`) - 可复用
3. **导航逻辑** (`ExecuteNavigate` + 路由映射)
4. **ContentRegion** (`AdminContentRegion`) - 改为使用 Shell.ContentRegion

**✅ 完全可用**：无废弃代码，所有逻辑都需要迁移

---

**迁移策略**：
```
Phase 1:
  1. 创建 ClinicalHomeView/AdminHomeView
  2. 迁移导航菜单UI + 样式 + 逻辑
  3. 将 ContentRegion 改为使用 Shell.ContentRegion

Phase 3:
  删除 ClinicalWorkstationView/AdminWorkstationView
```

**用户确认（2025-10-21）**：
1. ✅ **ClinicalWorkstationView 删除**
2. ✅ **AdminWorkstationView 删除**
3. ✅ **重新设计主页**，不直接迁移容器视图的导航菜单

**用户需求说明**：
> "ClinicalWorkstationView 删除，但是当前的诊疗主页中除了'开始诊疗'以外还可以增加一些医生对应的入口。例如'系统设置'，MVP后期会开发。AdminWorkstationView 也删除。重新设计一个风格和医生主页类似的主页。"

**新设计方向**：

### ClinicalHomeView（医生主页）
**核心入口**：
- ✅ 开始诊疗（当前已有）
- 🔄 系统设置（MVP后期开发）
- 🔄 其他医生相关功能入口（待明确）

**设计风格**：
- 卡片式布局（类似 Shell/Views/HomeView 当前风格）
- 快速入口 + 统计信息 + 快捷操作

---

### AdminHomeView（管理主页）
**核心入口**：
- 用户管理
- 药材管理
- 患者管理
- 验方管理
- 病历管理
- 系统设置

**设计风格**：
- 与 ClinicalHomeView 风格类似
- 卡片式布局

---

### ReceptionHomeView/PharmacyHomeView（MVP后期）
**设计原则**：
- 与 ClinicalHomeView/AdminHomeView 风格统一
- 根据角色特点设计专属入口

---

**实施策略调整**：
```
Phase 1:
  1. 重新设计 ClinicalHomeView（基于 Shell/HomeView 风格，增加扩展入口）
  2. 迁移 Shell/HomeView 到 Roles/LYBT.Desktop.Clinical/
  3. 设计 AdminHomeView（风格与 ClinicalHomeView 类似）

Phase 3:
  直接删除 ClinicalWorkstationView/AdminWorkstationView（不迁移导航菜单）
```

---

### Q3: 导航架构设计

🔄 **讨论中**

**当前 Shell Region 结构**（MainWindow.xaml）：
```xml
<!-- 登录前 -->
<ContentControl prism:RegionManager.RegionName="LoginRegion" />

<!-- 登录后 -->
<ContentControl prism:RegionManager.RegionName="ContentRegion" />
```

**当前导航流程**：
```
启动
  → LoginRegion: LoginView
    → 登录成功 → ContentRegion: HomeView（Shell模块）
      → ContentRegion: ClinicalWorkstationView（包含 ClinicalContentRegion）
        → ClinicalContentRegion: 业务视图
```

---

**新架构导航流程**：
```
启动
  → LoginRegion: LoginView
    → 登录成功 → 角色判断（RoleNavigationService）
      → 医生角色 → ContentRegion: ClinicalHomeView
        → ContentRegion: 业务视图（MedicalCaseFlowView/PatientManagementView等）

      → 管理角色 → ContentRegion: AdminHomeView
        → ContentRegion: 业务视图（UserManagementView/HerbManagementView等）

      → 前台角色（MVP后期）→ ContentRegion: ReceptionHomeView
      → 药房角色（MVP后期）→ ContentRegion: PharmacyHomeView
```

**核心变化**：
1. ✅ **取消子Region嵌套**：删除 `ClinicalContentRegion`、`AdminContentRegion`
2. ✅ **统一使用 Shell.ContentRegion**：所有视图导航都在此Region进行
3. ✅ **登录后角色路由**：RoleNavigationService 根据用户角色导航到对应 HomeView
4. ✅ **导航层次扁平化**：LoginView → RoleHomeView → 业务视图（2层）

---

**Q3-1：Region 导航策略**

**当前问题**：
- RoleHomeView 中的导航按钮（如"开始诊疗"）应该如何导航？
- 是否仍然使用 Shell.ContentRegion？

**方案A：完全使用 Shell.ContentRegion（推荐）**
```
ClinicalHomeView（在 Shell.ContentRegion）
  → 点击"开始诊疗" → 导航到 MedicalCaseFlowView（替换 Shell.ContentRegion）
  → 点击"返回主页" → 导航回 ClinicalHomeView
```

**优点**：
- ✅ 架构最简单，只有一个导航层次
- ✅ 符合ADR-003："只使用 Shell 的 ContentRegion"
- ✅ 导航逻辑统一

**缺点**：
- ❌ 每次导航都会完全替换视图（包括 HomeView 的头部/导航菜单）
- ❌ 无法保留 HomeView 的导航栏

---

**方案B：HomeView 包含子 Region**
```
ClinicalHomeView（在 Shell.ContentRegion）
  → 包含 ClinicalHomeContentRegion
    → 点击"开始诊疗" → 导航到 MedicalCaseFlowView（在 ClinicalHomeContentRegion）
    → ClinicalHomeView 的导航菜单保持可见
```

**优点**：
- ✅ 可以保留 HomeView 的导航菜单/快捷入口
- ✅ 类似传统桌面应用的侧边栏导航

**缺点**：
- ❌ 违反ADR-003："无子 Region 嵌套"
- ❌ 增加导航复杂度

---

**我的建议**：

采用 **方案A（完全使用 Shell.ContentRegion）**，理由：
1. 符合ADR-003设计目标
2. 架构最简单，易于理解和维护
3. HomeView 作为"快速入口页"，进入业务视图后完全替换

**用户补充（2025-10-21）**：

> "当前的 HomeView 就应该是医生的主页。"

**当前 HomeView 设计分析**（Shell/Views/HomeView.xaml）：

✅ **已有完整设计**：
1. **标题区域**："凌隐宝堂中医诊所" + "临床工作站"
2. **核心功能按钮**："开始接诊"（导航到 MedicalCaseFlowView）
3. **今日统计卡片**：今日接诊数 + 待完成医案数
4. **底部提示**：操作指引

**设计风格**：卡片式布局、居中对齐、统计数据可视化

---

**重新讨论 Q3-1：Region 导航策略** 🔄

**核心问题**：
- ClinicalHomeView 中的"开始接诊"按钮应该如何导航？
- 是否需要保留 HomeView 的导航菜单/统计信息？

**方案A：完全替换 Shell.ContentRegion（推荐）**

```
导航流程：
ClinicalHomeView（在 Shell.ContentRegion）
  → 点击"开始接诊"
    → 导航到 MedicalCaseFlowView（完全替换 Shell.ContentRegion）
    → ClinicalHomeView 完全被替换，标题、统计信息、按钮全部消失

返回方式：
  → 业务视图中添加"返回主页"按钮
  → 或使用浏览器式"后退"导航
  → 或通过顶部工具栏的"主页"按钮
```

**优点**：
- ✅ 符合ADR-003："只使用 Shell 的 ContentRegion，无子Region嵌套"
- ✅ 架构最简单，导航逻辑统一
- ✅ 当前 HomeView 已经采用这种方式

**缺点**：
- ❌ 进入业务视图后，HomeView 的标题、统计信息完全消失
- ❌ 需要额外的"返回主页"机制

---

**方案B：HomeView 包含子 Region**

```
导航流程：
ClinicalHomeView（在 Shell.ContentRegion）
  ├─ 顶部：标题 + 统计信息（始终可见）
  └─ 底部：ClinicalHomeContentRegion（动态内容区）
      → 默认显示："开始接诊"按钮
      → 点击后导航到 MedicalCaseFlowView（在 ClinicalHomeContentRegion）
      → 标题和统计信息保持可见

返回方式：
  → 业务视图中添加"返回主页"按钮
  → 导航回默认的"开始接诊"按钮视图
```

**优点**：
- ✅ 可以保留 HomeView 的标题和统计信息
- ✅ 用户始终知道当前在哪个角色主页
- ✅ 类似传统桌面应用的导航体验

**缺点**：
- ❌ 违反ADR-003："无子 Region 嵌套"
- ❌ 增加导航层次和复杂度
- ❌ 需要维护两个 Region（ContentRegion + ClinicalHomeContentRegion）

---

**我的建议**：

优先考虑 **方案A**，理由：
1. 当前 HomeView 已经使用方案A
2. 符合ADR-003设计目标
3. 架构最简单

**但需要解决用户体验问题**：
- 在业务视图（如 MedicalCaseFlowView）的顶部添加面包屑导航或"返回主页"按钮
- 或在 Shell 的顶部工具栏添加"主页"图标按钮（全局可用）

---

**用户确认（2025-10-21 - 基于截图说明）**：

> "顶部蓝色部分有'凌隐宝堂中医诊所诊疗系统'部分为整个系统的标题。下面整个区域是每个角色的主页。每个角色不同的功能可以从自己的主页出发进行导航。"

**架构理解确认**：

```
┌─────────────────────────────────────────────────────┐
│ 凌隐宝堂中医诊所诊疗系统    用户名 [退出登录]        │ ← Shell 顶部蓝色标题栏（固定）
├─────────────────────────────────────────────────────┤
│                                                     │
│                ContentRegion                        │ ← Shell.ContentRegion
│          （可完全切换内容）                          │
│                                                     │
│  方式1：显示 ClinicalHomeView（医生主页）            │
│    - 凌隐宝堂中医诊所                               │
│    - 临床工作站                                     │
│    - [开始接诊]                                     │
│    - 今日统计卡片                                   │
│                                                     │
│  方式2：显示 MedicalCaseFlowView（业务视图）        │
│    - 医案流程 Step 1/2/3/4                         │
│    - 患者信息、诊断、处方、完成                     │
│    - [← 返回主页] 按钮                              │
│                                                     │
└─────────────────────────────────────────────────────┘
底部状态栏（固定）
```

**✅ 确认采用方案A：完全替换 Shell.ContentRegion**

**架构特点**：
1. ✅ **Shell 层固定元素**：
   - 顶部蓝色标题栏（系统标题 + 用户信息 + 退出按钮）
   - 底部状态栏（API状态 + 时间）

2. ✅ **ContentRegion 可完全切换**：
   - 显示角色主页（ClinicalHomeView/AdminHomeView）
   - 或显示业务视图（MedicalCaseFlowView/UserManagementView 等）

3. ✅ **导航流程**：
   ```
   LoginView
     → RoleNavigationService（角色判断）
       → ClinicalHomeView（医生主页）
         → 点击"开始接诊" → MedicalCaseFlowView（完全替换）
           → 点击"返回主页" → ClinicalHomeView（完全替换回来）
   ```

4. ✅ **符合ADR-003**：只使用 Shell.ContentRegion，无子Region嵌套

---

**用户补充确认（2025-10-21）**：

> "不同的角色登录中的固定区域显示各自的主页。目前医生登录后到主页，主页中有'开始就诊'进入4步就诊逻辑。"

**架构完整确认** ✅：

```
Shell 固定框架：
  ┌─────────────────────────────────────────┐
  │ 顶部蓝色标题栏（全局固定）                │
  │ - 凌隐宝堂中医诊所诊疗系统                │
  │ - 用户名 + [退出登录]                    │
  ├─────────────────────────────────────────┤
  │                                         │
  │      ContentRegion（可完全切换）         │
  │                                         │
  │  医生登录 → ClinicalHomeView            │
  │    - [开始接诊] → MedicalCaseFlowView   │
  │                                         │
  │  管理员登录 → AdminHomeView             │
  │    - [用户管理] → UserManagementView    │
  │    - [药材管理] → HerbManagementView    │
  │    - [患者管理] → PatientManagementView │
  │    - ...                                │
  │                                         │
  │  前台登录（MVP后期）→ ReceptionHomeView  │
  │  药房登录（MVP后期）→ PharmacyHomeView   │
  │                                         │
  ├─────────────────────────────────────────┤
  │ 底部状态栏（全局固定）                   │
  │ - API状态 + 时间                        │
  └─────────────────────────────────────────┘
```

**导航机制**：
- ✅ 登录成功 → RoleNavigationService 判断角色 → 导航到对应 RoleHomeView
- ✅ 从 RoleHomeView 点击功能入口 → 导航到业务视图（完全替换 ContentRegion）
- ✅ 业务视图中"← 返回主页"链接 → 导航回 RoleHomeView

---

**用户补充说明**：

> "这里我要补充一点讨论点。还是关于UI设计、容器问题。我会分几次向你阐述我的疑问。"

**准备讨论** 🔄

我已准备好接收您关于UI设计和容器问题的疑问。请分次向我说明，我会逐一回答。

**请提出您的第一个疑问。**

---

**用户疑问1（2025-10-21）**：

> "这个三段式shell 是不是算容器？"

**回答**：

### **是的，Shell 是容器，但是不同类型的容器** ✅

---

**容器分类对比**：

| 对比维度 | Shell（应用框架容器） | WorkstationView（业务模块容器） |
|---------|---------------------|-------------------------------|
| **性质** | ✅ 应用程序根容器 | ❌ 业务模块中间层容器 |
| **必要性** | ✅ 必需（提供全局框架） | ❌ 不必需（增加嵌套） |
| **职责** | 全局布局、导航框架、状态栏 | 仅提供子导航区域 |
| **是否违反ADR-003** | ❌ 不违反 | ✅ 违反 |
| **层级** | 应用级（第0层） | 业务模块级（第2层） |

---

**架构层次对比**：

**❌ 旧架构（违反ADR-003）**：
```
第0层：Shell（应用框架容器） ← 必需
  ├─ 顶部标题栏
  ├─ ContentRegion
  │   └─ 第1层：HomeView
  │       └─ 导航到 第2层：WorkstationView（业务模块容器） ← ❌ 不必要的嵌套
  │           ├─ 侧边栏导航菜单
  │           └─ ClinicalContentRegion
  │               └─ 第3层：业务视图（MedicalCaseFlowView）
  └─ 底部状态栏
```
**问题**：有3层嵌套（Shell → WorkstationView → 业务视图）

---

**✅ 新架构（符合ADR-003）**：
```
第0层：Shell（应用框架容器） ← 必需
  ├─ 顶部标题栏
  ├─ ContentRegion
  │   └─ 第1层：ClinicalHomeView 或 业务视图
  │       - 直接显示 ClinicalHomeView（医生主页）
  │       - 或直接显示 MedicalCaseFlowView（业务视图）
  └─ 底部状态栏
```
**优势**：只有2层（Shell → 业务视图），取消了中间的 WorkstationView 容器

---

**核心区别**：

1. **Shell 是"应用框架容器"**：
   - ✅ 提供全局框架（标题栏、导航区、状态栏）
   - ✅ 所有桌面应用都需要这样的根容器
   - ✅ 类似于 Web 应用的 `<body>` 或 Layout 组件

2. **WorkstationView 是"业务模块容器"**：
   - ❌ 仅提供子导航区域（ClinicalContentRegion）
   - ❌ 增加了不必要的嵌套层次
   - ❌ 违反了"只使用 Shell.ContentRegion"的原则

---

**ADR-003 的真正含义**：

- ✅ **保留**：Shell 应用框架容器
- ❌ **取消**：WorkstationView 等业务模块容器
- ✅ **目标**：所有业务视图都直接在 Shell.ContentRegion 中导航，无中间层

---

**总结**：
- Shell 确实是容器，但它是**必需的应用框架容器**
- ADR-003 反对的是**不必要的业务模块容器**（如 WorkstationView）
- 新架构保留 Shell，但取消了 WorkstationView 这一中间层

**您的疑问是否已解答？还有其他疑问吗？**

---

**用户确认（2025-10-21）**：

> "我看了一下就是mainwindow。所以说mainwindow是容器。是根窗口？"

**回答：完全正确！** ✅

---

**准确的说法**：

```
MainWindow（根窗口）
  = Shell 模块的主窗口
  = 应用程序的根容器
  = WPF Window 对象（最外层窗口）
```

**代码位置**：
- `src/Client/Desktop/Shell/Views/MainWindow.xaml`
- `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**三段式布局**：
```
MainWindow (WPF Window)
  ├─ 顶部蓝色标题栏（60px 高度）
  │  └─ 系统标题 + 用户名 + 退出按钮
  │
  ├─ 中间内容区（ContentRegion）
  │  ├─ 登录前：LoginRegion → LoginView
  │  └─ 登录后：ContentRegion → RoleHomeView 或 业务视图
  │
  └─ 底部状态栏（30px 高度）
     └─ API状态 + 时间
```

---

**核心理解** ✅：

1. **MainWindow = 根窗口**
   - WPF 应用的最外层 Window 对象
   - 整个应用只有这一个窗口
   - 所有其他视图都在这个窗口内切换

2. **MainWindow 属于 Shell 模块**
   - Shell 负责提供应用框架
   - MainWindow 是 Shell 的主窗口

3. **ADR-003 保留的就是 MainWindow**
   - ✅ 保留：MainWindow（必需的根容器）
   - ❌ 取消：WorkstationView（不必要的业务模块容器）

---

**请提出您的下一个疑问。**

---

**用户疑问2（2025-10-21）**：

> "ClinicalHomeView 这个是控件？"

**回答：是的，是 UserControl（用户控件）** ✅

---

**WPF 控件分类**：

| 类型 | 说明 | 示例 | 在本项目中 |
|-----|------|------|-----------|
| **Window** | 窗口对象（根容器） | `MainWindow` | `Shell/Views/MainWindow.xaml` |
| **UserControl** | 用户控件（可复用视图组件） | `HomeView`, `LoginView`, `MedicalCaseFlowView` | 所有业务视图 |
| **Control** | 基础控件 | `Button`, `TextBox`, `Grid` | 构成 UI 的基本元素 |

---

**当前 HomeView 的定义**：

```xml
<!-- Shell/Views/HomeView.xaml -->
<UserControl x:Class="LYBT.Desktop.Shell.Views.HomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    ...
</UserControl>
```

**代码后台**：
```csharp
// Shell/Views/HomeView.xaml.cs
public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }
}
```

---

**ClinicalHomeView 的定义（迁移后）**：

```xml
<!-- Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml -->
<UserControl x:Class="LYBT.Desktop.Clinical.Views.ClinicalHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    ...
</UserControl>
```

**代码后台**：
```csharp
// Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml.cs
public partial class ClinicalHomeView : UserControl
{
    public ClinicalHomeView()
    {
        InitializeComponent();
    }
}
```

---

**核心理解** ✅：

1. **MainWindow = Window 对象**
   - 整个应用的根窗口
   - 继承自 `System.Windows.Window`
   - 只有一个实例

2. **ClinicalHomeView = UserControl 对象**
   - 可复用的视图组件
   - 继承自 `System.Windows.Controls.UserControl`
   - 在 MainWindow 的 ContentRegion 中显示

3. **所有业务视图都是 UserControl**：
   - ClinicalHomeView（医生主页）
   - AdminHomeView（管理主页）
   - MedicalCaseFlowView（医案流程）
   - UserManagementView（用户管理）
   - 等等...

---

**层级关系**：

```
MainWindow (Window)
  └─ ContentRegion (ContentControl)
      └─ ClinicalHomeView (UserControl) ← 这是控件
          └─ 包含 Button, TextBlock, Grid 等基础控件
```

---

**总结**：
- ✅ ClinicalHomeView 是 **UserControl（用户控件）**
- ✅ 它是一个**可复用的视图组件**
- ✅ 在 MainWindow 的 ContentRegion 中动态加载和切换

**您的疑问是否已解答？请继续提出下一个疑问。**

---

**用户疑问3（2025-10-21）**：

> "MedicalCaseFlowView.xaml 这个是什么？"

**回答：也是 UserControl（用户控件）** ✅

---

**代码验证**：

```xml
<!-- src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml -->
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseFlowView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    ...
</UserControl>
```

**位置**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.xaml`

---

**所有视图都是 UserControl** ✅：

| 视图名称 | 类型 | 用途 | 所属模块 |
|---------|------|------|---------|
| `MainWindow` | **Window** | 应用程序根窗口 | Shell |
| `HomeView` | UserControl | 医生主页（迁移后→ClinicalHomeView） | Shell → Clinical |
| `ClinicalHomeView` | UserControl | 医生主页（迁移后） | Clinical |
| `AdminHomeView` | UserControl | 管理员主页（新建） | Admin |
| `MedicalCaseFlowView` | UserControl | 医案流程（4步） | MedicalCase |
| `LoginView` | UserControl | 登录页面 | Auth |
| `UserManagementView` | UserControl | 用户管理 | Users |
| `HerbManagementView` | UserControl | 药材管理 | Herbs |
| ... | UserControl | 所有业务视图 | 各业务模块 |

---

**核心理解** ✅：

1. **只有 MainWindow 是 Window**
   - 整个应用唯一的窗口对象

2. **所有其他视图都是 UserControl**
   - ClinicalHomeView（医生主页）
   - MedicalCaseFlowView（医案流程）
   - LoginView（登录页面）
   - 所有业务视图...

3. **UserControl 的特点**：
   - 可复用的视图组件
   - 在 MainWindow 的 ContentRegion 中动态加载和切换
   - 通过 Prism 导航系统进行切换

---

**导航流程示例**：

```
MainWindow (Window)
  └─ ContentRegion
      ├─ ClinicalHomeView (UserControl) ← 医生登录后显示
      │   └─ 点击"开始接诊" → 导航到...
      │
      └─ MedicalCaseFlowView (UserControl) ← 替换 ClinicalHomeView
          └─ 医案流程（Step 1-4）
```

---

**总结**：
- ✅ MedicalCaseFlowView 也是 **UserControl（用户控件）**
- ✅ 和 ClinicalHomeView 一样，都是在 MainWindow 的 ContentRegion 中切换的视图
- ✅ **唯一的 Window 是 MainWindow**，所有其他视图都是 UserControl

**您的疑问是否已解答？请继续提出下一个疑问。**

---

**用户疑问4（2025-10-21）**：

> "所以我的下一个问题是 医生登录后先加载 ClinicalHomeView（点击上面的"开始诊断"后控件换成"MedicalCaseFlowView"）是这样吗？然后 PatientSelectionView.xaml 是 MedicalCaseFlowView的一个组成部分？"

**回答：两个问题都是"是的"** ✅

---

### Part 1: 导航流程确认 ✅

**完全正确！** 导航流程如下：

```
医生登录
  ↓
Shell.ContentRegion 显示 HomeView (当前设计)
  ↓ [点击"开始接诊"按钮]
Shell.ContentRegion 切换为 MedicalCaseFlowView
```

**重构后的流程**（Issue #1513）：
```
医生登录
  ↓
Shell.ContentRegion 显示 ClinicalHomeView (迁移后)
  ↓ [点击"开始接诊"按钮]
Shell.ContentRegion 切换为 MedicalCaseFlowView
```

**代码证据**（`HomeViewModel.cs:101`）：
```csharp
private void ExecuteStartConsultation()
{
    Logger.LogInformation("开始看诊，导航到医案流程视图");

    // Epic #1494: 直接导航到医案流程视图（包含Step 1-4完整流程）
    _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", navigationResult =>
    {
        if (navigationResult.Result == true)
        {
            Logger.LogInformation("导航成功：MedicalCaseFlowView");
        }
    });
}
```

---

### Part 2: PatientSelectionView 是 MedicalCaseFlowView 的组成部分 ✅

**是的！** PatientSelectionView 是 MedicalCaseFlowView 的组成部分，但它不是静态嵌入，而是**动态加载**。

---

#### 架构机制：ContentControl + ViewModel 绑定

**MedicalCaseFlowView.xaml (line 253-266)**：
```xml
<!-- Row 3: 主内容区（动态内容区域） -->
<Border Grid.Row="3" Background="White" Margin="0">
    <Grid>
        <!-- ContentControl 绑定到 CurrentStepViewModel -->
        <ContentControl Content="{Binding CurrentStepViewModel}" />

        <!-- 占位文本（各Step View未实现时显示） -->
        <TextBlock Text="{Binding CurrentStep}"
                  FontSize="24"
                  Foreground="#999"
                  HorizontalAlignment="Center"
                  VerticalAlignment="Center"
                  Visibility="{Binding CurrentStepViewModel, Converter={StaticResource BoolToVisibility}}" />
    </Grid>
</Border>
```

**关键点**：
- `ContentControl` 绑定到 `CurrentStepViewModel` 属性
- ViewModel 切换时，WPF 会自动通过 ViewModelLocator 找到对应的 View

---

#### ViewModel 动态加载逻辑

**MedicalCaseFlowViewModel.cs (lines 402-429)**：
```csharp
private void NavigateToStep(FlowStep step)
{
    CurrentStep = step;

    switch (step)
    {
        case FlowStep.SelectPatient:
            Logger.LogInformation("导航到患者选择步骤");

            // 1. 通过IoC容器动态解析PatientSelectionViewModel
            var patientSelectionViewModel = _containerProvider.Resolve<PatientSelectionViewModel>();

            // 2. 监听患者选择事件
            patientSelectionViewModel.PatientSelected += async (sender, selectedPatient) =>
            {
                Logger.LogInformation("患者选择事件触发，患者：{PatientName}（ID: {PatientId}）",
                    selectedPatient.Name, selectedPatient.Id);

                // 更新CurrentPatient
                CurrentPatient = selectedPatient;

                // 更新患者信息条
                SelectedPatientName = selectedPatient.Name;
                SelectedPatientInfo = $"{selectedPatient.Gender} | {selectedPatient.Age}岁 | {selectedPatient.PhoneNumber}";

                // 自动创建MedicalCase并跳转到Step 2
                await ExecuteNextStepAsync();
            };

            // 3. 赋值给CurrentStepViewModel → ContentControl自动显示对应View
            CurrentStepViewModel = patientSelectionViewModel;
            Logger.LogInformation("PatientSelectionViewModel已创建");
            break;

        case FlowStep.FillConsultation:
            // Step 2: ConsultationFormViewModel
            // ...
            break;

        case FlowStep.FillPrescription:
            // Step 3: PrescriptionEditorViewModel
            // ...
            break;

        case FlowStep.CompleteMedicalCase:
            // Step 4: CompletionViewModel
            // ...
            break;
    }
}
```

---

#### 工作原理：WPF ViewModelLocator 机制

```
Step 1: CurrentStepViewModel = PatientSelectionViewModel
  ↓ (WPF DataTemplate + ViewModelLocator)
ContentControl 自动渲染 PatientSelectionView.xaml

Step 2: CurrentStepViewModel = ConsultationFormViewModel
  ↓
ContentControl 自动渲染 ConsultationFormView.xaml

Step 3: CurrentStepViewModel = PrescriptionEditorViewModel
  ↓
ContentControl 自动渲染 PrescriptionEditorView.xaml

Step 4: CurrentStepViewModel = CompletionViewModel
  ↓
ContentControl 自动渲染 CompletionView.xaml
```

---

#### 4步流程架构图

```
MedicalCaseFlowView (UserControl) - 医案流程容器
├─ Row 0: 顶部导航栏 (固定)
│   └─ [← 返回主页] 按钮
│
├─ Row 1: 流程进度条 (固定)
│   └─ [Step 1] → [Step 2] → [Step 3] → [Step 4]
│
├─ Row 2: 患者信息条 (Step 2-4可见)
│   └─ 患者：张三 | 男 | 35岁 | 13800000000
│
├─ Row 3: 主内容区 (动态切换) ⭐
│   └─ ContentControl {Binding CurrentStepViewModel}
│       ├─ Step 1: PatientSelectionView (动态加载) ⭐
│       ├─ Step 2: ConsultationFormView (动态加载)
│       ├─ Step 3: PrescriptionEditorView (动态加载)
│       └─ Step 4: CompletionView (动态加载)
│
└─ Row 4: 底部操作栏 (固定)
    └─ [取消] [保存草稿] | [上一步] [下一步/完成看诊]
```

---

#### 关系总结

| 组件 | 类型 | 角色 | 动态/静态 |
|------|------|------|----------|
| `MedicalCaseFlowView` | UserControl | 流程容器（5行Grid） | 静态（在ContentRegion） |
| `PatientSelectionView` | UserControl | Step 1内容 | **动态**（通过ContentControl加载） |
| `ConsultationFormView` | UserControl | Step 2内容 | **动态**（通过ContentControl加载） |
| `PrescriptionEditorView` | UserControl | Step 3内容 | **动态**（通过ContentControl加载） |
| `CompletionView` | UserControl | Step 4内容 | **动态**（通过ContentControl加载） |

---

#### 完整导航流程

```
ClinicalHomeView (医生主页)
  ↓ [点击"开始接诊"]
MedicalCaseFlowView (医案流程容器)
  └─ 自动导航到 FlowStep.SelectPatient
      └─ CurrentStepViewModel = PatientSelectionViewModel
          └─ ContentControl 自动显示 PatientSelectionView ⭐
              ↓ [患者选择完成事件触发]
          CurrentStepViewModel = ConsultationFormViewModel
          └─ ContentControl 自动显示 ConsultationFormView
              ↓ [点击"下一步"]
          CurrentStepViewModel = PrescriptionEditorViewModel
          └─ ContentControl 自动显示 PrescriptionEditorView
              ↓ [点击"下一步"]
          CurrentStepViewModel = CompletionViewModel
          └─ ContentControl 自动显示 CompletionView
              ↓ [点击"完成看诊"]
              返回 ClinicalHomeView
```

---

**核心理解** ✅：

1. ✅ **PatientSelectionView 是 MedicalCaseFlowView 的组成部分**
   - 但不是静态嵌入（XAML中看不到 `<PatientSelectionView>` 标签）
   - 而是动态加载（通过 ContentControl + ViewModel 绑定）

2. ✅ **MedicalCaseFlowView 是一个"流程容器"**
   - 包含固定框架（进度条、患者信息条、操作栏）
   - 包含动态内容区（ContentControl 绑定 CurrentStepViewModel）

3. ✅ **导航机制：ViewModel 切换驱动 View 切换**
   - `CurrentStepViewModel` 变化 → WPF 自动找到对应 View → ContentControl 显示新 View

---

### Q4: Phase拆分与实施顺序

🔄 **讨论中**

**基于前述讨论的调整后 Phase 方案**：

---

### **Phase 1: Clinical 模块重构** (#1514)

**目标**：完成医生角色模块化

**主要任务**：
1. **创建 Clinical 模块结构**：
   ```
   src/Client/Desktop/Roles/LYBT.Desktop.Clinical/
   ├─ Views/
   │  └─ ClinicalHomeView.xaml（迁移自 Shell/HomeView）
   ├─ ViewModels/
   │  └─ ClinicalHomeViewModel.cs（迁移自 Shell/HomeViewModel）
   └─ ClinicalModule.cs
   ```

2. **创建 RoleNavigationService**：
   - 位置：`src/Client/Desktop/Core/Services/Navigation/RoleNavigationService.cs`
   - 功能：根据用户角色导航到对应 HomeView

3. **调整登录逻辑**：
   - LoginViewModel 集成 RoleNavigationService
   - 登录成功后根据角色导航

4. **更新导航注册**：
   - 注册 ClinicalHomeView
   - 删除 Shell/HomeView 注册

**验收标准**：
- ✅ 医生登录后导航到 ClinicalHomeView
- ✅ "开始接诊"功能正常
- ✅ 今日统计显示正常
- ✅ 编译0错误0警告

**工作量**：3-5小时

---

### **Phase 1.5: Admin 模块重构** (#新建Issue)

**目标**：完成管理员角色模块化

**主要任务**：
1. **创建 Admin 模块结构**：
   ```
   src/Client/Desktop/Roles/LYBT.Desktop.Admin/
   ├─ Views/
   │  └─ AdminHomeView.xaml（新建，参考 ClinicalHomeView 风格）
   ├─ ViewModels/
   │  └─ AdminHomeViewModel.cs
   └─ AdminModule.cs
   ```

2. **设计 AdminHomeView**：
   - 卡片式布局（与 ClinicalHomeView 风格一致）
   - 核心功能入口：
     - 用户管理
     - 药材管理
     - 患者管理
     - 验方管理
     - 病历管理
     - 系统设置

3. **更新 RoleNavigationService**：
   - 添加 Admin 角色路由

**验收标准**：
- ✅ 管理员登录后导航到 AdminHomeView
- ✅ 所有管理功能入口正常导航
- ✅ 风格与 ClinicalHomeView 一致
- ✅ 编译0错误0警告

**工作量**：4-6小时

---

### **Phase 2: Reception 模块** (#1515)

**目标**：创建前台角色模块

**前置条件**：
- ⏸️ **等待MVP完成**
- ⏸️ **等待前台需求明确**

**主要任务**：
1. 创建 Reception 模块结构
2. 设计 ReceptionHomeView
3. 实现前台业务功能

**工作量**：20-30小时

---

### **Phase 3: 清理 Workstation 容器** (#1516)

**目标**：删除旧架构代码

**前置条件**：
- ✅ Phase 1 完成
- ✅ Phase 1.5 完成

**主要任务**：
1. **删除容器视图**：
   - `src/Client/Desktop/Workstations/ClinicalWorkstation/`
   - `src/Client/Desktop/Workstations/AdminWorkstation/`

2. **删除 Shell/HomeView**：
   - `src/Client/Desktop/Shell/Views/HomeView.xaml`
   - `src/Client/Desktop/Shell/ViewModels/HomeViewModel.cs`

3. **更新文档**：
   - 更新架构文档
   - 更新导航说明

**验收标准**：
- ✅ 旧代码完全删除
- ✅ 编译0错误0警告
- ✅ 所有功能正常

**工作量**：2-3小时

---

**实施顺序建议**：
```
Phase 1 (Clinical) → Phase 1.5 (Admin) → Phase 3 (清理)
                                       ↓
                                   Phase 2 (Reception - MVP后期)
```

**待用户确认**：
1. ❓ 是否新增 Phase 1.5（Admin 模块重构）？
2. ❓ Phase 拆分是否合理？
3. ❓ 是否需要调整实施顺序？

---

### Q5: 向后兼容与迁移策略

❓ **待讨论**

**需确认**：
1. 重构过程中是否需要保持现有功能可用？
2. 是否允许短期内同时存在旧架构（Workstation容器）和新架构（Role模块）？
3. 迁移过程中的测试策略：
   - 是否需要为每个 Phase 编写集成测试？
   - 如何验证导航逻辑的正确性？
4. 是否需要数据迁移或配置迁移？

---

## 📊 决策记录

| 问题编号 | 状态 | 决策内容 | 确认时间 |
|---------|------|---------|---------|
| Q1 | ✅ 已确认 | 采用简洁命名（{Role}Module）+ 目录区分（Roles/） | 2025-10-21 |
| Q2 | ✅ 已确认 | 命名空间 LYBT.Desktop.{Role} + HomeView迁移 + 容器删除 | 2025-10-21 |
| Q3 | ✅ 已确认 | 完全使用Shell.ContentRegion（无子Region嵌套） | 2025-10-21 |
| Q4 | ✅ 已确认 | Phase 1+2合并为唯一Phase（9-14小时） | 2025-10-21 |
| Q5 | ✅ 已确认 | 手工测试 + 配置检查 + 不允许新旧共存 | 2025-10-21 |

---

## 📝 Q4/Q5 详细决策（2025-10-21）

### Q4: Phase拆分与实施顺序

**✅ 最终方案：Phase 1+2 合并为唯一Phase**

**原因**：用户明确要求"不允许新旧架构共存"

**Phase 1（唯一）：角色模块化重构**

**任务清单**（9项）：
1. ✅ 创建 `Roles/LYBT.Desktop.Clinical/` 模块
   - ClinicalHomeView（迁移自 Shell/HomeView）
   - 统计卡片改为占位提示："📊 统计功能开发中（MVP后期实现）"
2. ✅ 创建 `Roles/LYBT.Desktop.Admin/` 模块
   - AdminHomeView（6个功能卡片网格布局，无统计信息）
3. ✅ 创建 RoleNavigationService（角色路由服务）
4. ✅ 调整 LoginViewModel（集成角色路由）
5. ✅ **删除 `Workstations/ClinicalWorkstation/`**
6. ✅ **删除 `Workstations/AdminWorkstation/`**
7. ✅ **删除 `Shell/Views/HomeView.xaml`**
8. ✅ 检查配置文件（App.config、硬编码视图名称）
9. ✅ 更新架构文档

**工作量估算**：9-14小时

**验收标准**：
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 医生登录 → 导航到 ClinicalHomeView
- ✅ 点击"开始接诊" → 导航到 MedicalCaseFlowView
- ✅ 管理员登录 → 导航到 AdminHomeView
- ✅ 管理员点击6个功能卡片 → 导航到对应管理视图
- ✅ **旧代码完全删除**

---

### AdminHomeView 设计细节

**布局方向**：卡片网格布局（3×2，共6个功能卡片）

**功能卡片**：
1. 👤 用户管理 → UserManagementView
2. 🌿 药材管理 → HerbManagementView
3. 🏥 患者管理 → PatientManagementView
4. 📋 验方管理 → FormulaManagementView
5. 📁 病历管理 → MedicalCaseManagementView
6. ⚙️ 系统设置 → SystemSettingsView

**统计信息**：无（MVP不涉及统计）

**风格参考**：与 ClinicalHomeView 保持一致（卡片式布局）

---

### ClinicalHomeView 调整

**保留内容**：
- 标题区域："凌隐宝堂中医诊所" + "临床工作站"
- 核心按钮："开始接诊"
- 底部提示

**统计卡片处理**：
- ✅ 保留统计卡片UI
- ✅ 改为占位提示："📊 统计功能开发中（MVP后期实现）"
- ✅ 不显示具体数字（TodayConsultationCount/PendingCaseCount）

---

### Q5: 向后兼容与迁移策略

**Q5-1: 功能可用性策略**
- ✅ Phase 完成后保证能运行即可
- ✅ 中间过程不强制要求每次提交都可运行

**Q5-2: 测试验证策略**
- ✅ 手工测试（5项验收标准，见上文）
- ❌ 不编写自动化测试（符合MVP原则）

**Q5-3: 数据/配置迁移**
- ✅ 数据无需迁移
- ✅ 配置需要检查：
  - App.config
  - 硬编码的视图名称（如 "HomeView" → "ClinicalHomeView"）
  - 命名空间变化影响

**Q5-4: 新旧架构共存**
- ✅ **不允许新旧架构共存**
- ✅ **Phase 1 必须一次性完成：创建新架构 + 删除旧架构**

---

## 📝 讨论记录

### 2025-10-21 初始讨论
- 创建讨论文档
- 确认 Q1-Q3（命名规范、目录结构、导航架构）

### 2025-10-21 Q4/Q5 讨论
- 确认 Phase 拆分方案（合并为唯一Phase）
- 确认 AdminHomeView 设计（6个功能卡片，无统计）
- 确认 ClinicalHomeView 调整（统计改为占位）
- 确认迁移策略（不允许新旧共存）

---

## 🔗 相关资源

- Issue #1513: [Epic] Workstation架构重构 - 角色业务模块化
- ~~Issue #1514: Phase 1 - 重命名HomeView为ClinicalHomeView并迁移~~（废弃，合并到Phase 1）
- ~~Issue #1515: Phase 2 - 创建Reception模块~~（延后到MVP后期）
- ~~Issue #1516: Phase 3 - 移除Workstation容器模块~~（合并到Phase 1）
- Issue #1512: HomeView显示问题（已修复）

**新建Issue**：
- 待创建：Phase 1 - 角色模块化重构（Clinical + Admin + 删除Workstation）

---

## 下一步行动

✅ Q1-Q5 讨论完成，所有决策已确认
🔄 下一步：
1. 生成需求文档（基于讨论结果）
2. 创建 GitHub Issue（Phase 1 实施Issue）
3. 开始实施 Phase 1
