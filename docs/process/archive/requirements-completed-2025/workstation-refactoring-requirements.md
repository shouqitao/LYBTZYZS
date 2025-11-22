# Workstation架构重构需求文档

> **版本**: v1.0
> **创建日期**: 2025-10-21
> **相关Issue**: #1513 - [Epic] Workstation架构重构 - 角色业务模块化
> **状态**: ✅ 需求已确认
> **讨论文档**: `docs/explanation/architecture/client/workstation-refactoring-discussion.md`

---

## 📋 1. 需求概述

### 1.1 背景

**当前架构问题**：
- Workstation作为"UI容器"设计（ClinicalWorkstation、AdminWorkstation）
- HomeView位于Shell模块，但实际是医生角色的首页
- 导航层次不清晰：LoginView → HomeView → Workstation容器 → 业务视图
- 存在不必要的子Region嵌套（ClinicalContentRegion、AdminContentRegion）

**改进动机**：
- 简化导航层次，提升用户体验
- 明确角色模块边界，便于后续扩展（前台、药房角色）
- 符合 ADR-003 设计决策："取消Workstation作为'UI容器'的设计"

---

### 1.2 目标

**主要目标**：
1. ✅ 取消 Workstation 作为"UI容器"的设计
2. ✅ 改造为"角色业务模块"设计（Clinical、Admin）
3. ✅ 简化导航：LoginView → RoleHomeView → 业务视图（2层）
4. ✅ 只使用 Shell.ContentRegion，无子Region嵌套
5. ✅ 不允许新旧架构共存（一次性完成重构 + 删除旧代码）

**次要目标**：
- 为未来角色扩展（Reception、Pharmacy）预留清晰的架构基础
- 保持代码规范统一（命名、目录结构与业务模块对齐）

---

### 1.3 范围

**包含范围**：
- ✅ Clinical 模块重构（医生角色）
- ✅ Admin 模块重构（管理员角色）
- ✅ 角色路由服务（RoleNavigationService）
- ✅ 删除旧架构（ClinicalWorkstation、AdminWorkstation、Shell/HomeView）
- ✅ 配置检查与更新

**不包含范围**：
- ❌ Reception 模块（前台角色，MVP后期实施）
- ❌ Pharmacy 模块（药房角色，MVP后期实施）
- ❌ 统计功能实现（当前仅占位提示）
- ❌ 自动化测试（手工测试即可）

---

## 📐 2. 功能需求

### 2.1 Clinical 模块（医生角色）

#### FR-C-01: 模块结构创建
**需求描述**：创建 Clinical 角色模块的目录结构和项目配置。

**技术规格**：
```
src/Client/Desktop/Roles/
└─ LYBT.Desktop.Clinical/
   ├─ Views/
   │  └─ ClinicalHomeView.xaml
   ├─ ViewModels/
   │  └─ ClinicalHomeViewModel.cs
   └─ ClinicalModule.cs
```

**命名空间**：`LYBT.Desktop.Clinical`

**验收标准**：
- ✅ 项目在 Visual Studio 中正常加载
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ ClinicalModule 已注册到 Prism ModuleCatalog

---

#### FR-C-02: ClinicalHomeView 视图设计
**需求描述**：从 Shell/HomeView 迁移并调整为医生角色主页。

**UI布局**：
```
┌─────────────────────────────────┐
│ 凌隐宝堂中医诊所                  │
│ 临床工作站                       │
│                                 │
│   [开始接诊] 按钮（150×50px）    │
│                                 │
│ ┌───── 今日统计 ─────┐          │
│ │ 📊 统计功能开发中    │          │
│ │ （MVP后期实现）      │          │
│ └────────────────────┘          │
│                                 │
│ 💡 提示：点击【开始接诊】...     │
└─────────────────────────────────┘
```

**功能要求**：
1. 标题区域：
   - "凌隐宝堂中医诊所"（FontSize 32px, FontWeight Bold, 颜色 #2E86AB）
   - "临床工作站"（FontSize 18px, 颜色 #666）

2. 核心按钮：
   - "开始接诊"（150×50px, PrimaryButton样式）
   - 点击后导航到 MedicalCaseFlowView

3. 统计卡片：
   - 保留UI框架（Border + Grid布局）
   - 显示占位提示："📊 统计功能开发中（MVP后期实现）"
   - **不显示**具体数字（TodayConsultationCount/PendingCaseCount）

4. 底部提示：
   - "提示：点击【开始接诊】选择患者后进入就诊流程"

**验收标准**：
- ✅ UI布局与设计稿一致
- ✅ "开始接诊"按钮正常导航到 MedicalCaseFlowView
- ✅ 统计卡片显示占位提示（无具体数字）

---

#### FR-C-03: ClinicalHomeViewModel 逻辑迁移
**需求描述**：从 Shell/HomeViewModel 迁移核心逻辑。

**保留功能**：
- ✅ StartConsultationCommand（开始接诊命令）
- ✅ 导航到 MedicalCaseFlowView 的逻辑

**调整功能**：
- ❌ 删除 TodayConsultationCount / PendingCaseCount 属性
- ❌ 删除 LoadTodayStatistics() 方法
- ✅ 添加占位提示文本属性（如需要）

**验收标准**：
- ✅ StartConsultationCommand 正常工作
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 无废弃代码（统计相关逻辑已删除）

---

### 2.2 Admin 模块（管理员角色）

#### FR-A-01: 模块结构创建
**需求描述**：创建 Admin 角色模块的目录结构和项目配置。

**技术规格**：
```
src/Client/Desktop/Roles/
└─ LYBT.Desktop.Admin/
   ├─ Views/
   │  └─ AdminHomeView.xaml
   ├─ ViewModels/
   │  └─ AdminHomeViewModel.cs
   └─ AdminModule.cs
```

**命名空间**：`LYBT.Desktop.Admin`

**验收标准**：
- ✅ 项目在 Visual Studio 中正常加载
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ AdminModule 已注册到 Prism ModuleCatalog

---

#### FR-A-02: AdminHomeView 视图设计
**需求描述**：设计管理员角色主页，采用卡片网格布局。

**UI布局**：
```
┌─────────────────────────────────────────┐
│ 凌隐宝堂中医诊所                          │
│ 管理工作台                               │
│                                         │
│ ┌────────┐ ┌────────┐ ┌────────┐      │
│ │👤 用户  │ │🌿 药材  │ │🏥 患者  │      │
│ │  管理   │ │  管理   │ │  管理   │      │
│ └────────┘ └────────┘ └────────┘      │
│ ┌────────┐ ┌────────┐ ┌────────┐      │
│ │📋 验方  │ │📁 病历  │ │⚙️ 系统  │      │
│ │  管理   │ │  管理   │ │  设置   │      │
│ └────────┘ └────────┘ └────────┘      │
│                                         │
│ 💡 提示：点击功能卡片进入对应管理界面    │
└─────────────────────────────────────────┘
```

**功能卡片规格**：
| 卡片 | Icon | 标题 | 导航目标 |
|-----|------|------|---------|
| 1 | 👤 | 用户管理 | UserManagementView |
| 2 | 🌿 | 药材管理 | HerbManagementView |
| 3 | 🏥 | 患者管理 | PatientManagementView |
| 4 | 📋 | 验方管理 | FormulaManagementView |
| 5 | 📁 | 病历管理 | MedicalCaseManagementView |
| 6 | ⚙️ | 系统设置 | SystemSettingsView |

**样式要求**：
- 卡片布局：3列×2行网格（Grid.ColumnDefinitions="*,*,*"）
- 卡片样式：Border + CornerRadius="5" + 悬停效果
- 风格参考：与 ClinicalHomeView 保持一致

**验收标准**：
- ✅ 6个功能卡片正确显示
- ✅ 点击卡片正常导航到对应管理视图
- ✅ 风格与 ClinicalHomeView 一致

---

#### FR-A-03: AdminHomeViewModel 逻辑实现
**需求描述**：实现管理员主页的导航逻辑。

**核心功能**：
- ✅ 6个导航命令（DelegateCommand）
- ✅ 导航到对应管理视图（使用 Shell.ContentRegion）

**命令列表**：
```csharp
public DelegateCommand NavigateToUserManagementCommand { get; }
public DelegateCommand NavigateToHerbManagementCommand { get; }
public DelegateCommand NavigateToPatientManagementCommand { get; }
public DelegateCommand NavigateToFormulaManagementCommand { get; }
public DelegateCommand NavigateToMedicalCaseManagementCommand { get; }
public DelegateCommand NavigateToSystemSettingsCommand { get; }
```

**验收标准**：
- ✅ 所有命令正常执行
- ✅ 导航到正确的管理视图
- ✅ 编译通过（0 errors, 0 warnings）

---

### 2.3 角色路由服务

#### FR-R-01: RoleNavigationService 创建
**需求描述**：创建角色路由服务，根据用户角色导航到对应的 HomeView。

**技术规格**：
```
src/Client/Desktop/Core/Services/Navigation/
├─ IRoleNavigationService.cs
└─ RoleNavigationService.cs
```

**接口定义**：
```csharp
public interface IRoleNavigationService
{
    void NavigateToRoleHome(string roleName);
}
```

**实现逻辑**：
```csharp
public void NavigateToRoleHome(string roleName)
{
    var viewName = roleName switch
    {
        "Doctor" => "ClinicalHomeView",
        "Receptionist" => "ReceptionHomeView", // MVP后期
        "Pharmacist" => "PharmacyHomeView",    // MVP后期
        "Admin" => "AdminHomeView",
        _ => throw new ArgumentException($"未知角色: {roleName}")
    };

    _regionManager.RequestNavigate("ContentRegion", viewName);
}
```

**验收标准**：
- ✅ 服务已注册到 DI 容器
- ✅ 医生角色导航到 ClinicalHomeView
- ✅ 管理员角色导航到 AdminHomeView
- ✅ 未知角色抛出异常

---

#### FR-R-02: LoginViewModel 集成角色路由
**需求描述**：调整 LoginViewModel 的登录成功逻辑，使用 RoleNavigationService。

**修改位置**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

**修改内容**：
```csharp
// 旧逻辑（删除）：
// _regionManager.RequestNavigate("ContentRegion", "HomeView");

// 新逻辑（添加）：
private void OnLoginSuccess()
{
    var userRole = _currentUser.Role; // "Doctor" / "Admin"
    _roleNavigationService.NavigateToRoleHome(userRole);
}
```

**验收标准**：
- ✅ 医生登录后导航到 ClinicalHomeView
- ✅ 管理员登录后导航到 AdminHomeView
- ✅ 编译通过（0 errors, 0 warnings）

---

### 2.4 旧架构删除

#### FR-D-01: 删除 Workstation 容器模块
**需求描述**：删除旧的 Workstation 容器代码。

**删除目录**：
```
src/Client/Desktop/Workstations/
├─ ClinicalWorkstation/     ← 完全删除
├─ AdminWorkstation/        ← 完全删除
└─ [其他Workstation模块]    ← 如有则删除
```

**验收标准**：
- ✅ 目录已完全删除
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 无引用错误

---

#### FR-D-02: 删除 Shell/HomeView
**需求描述**：删除已迁移到 Clinical 模块的 HomeView。

**删除文件**：
```
src/Client/Desktop/Shell/
├─ Views/HomeView.xaml              ← 删除
└─ ViewModels/HomeViewModel.cs      ← 删除
```

**验收标准**：
- ✅ 文件已完全删除
- ✅ Shell 模块中无 HomeView 注册
- ✅ 编译通过（0 errors, 0 warnings）

---

#### FR-D-03: 更新模块注册
**需求描述**：更新 Prism 模块注册，移除旧模块，添加新模块。

**修改位置**：
- `src/Client/Desktop/Shell/App.xaml.cs` 或对应的 ModuleCatalog 配置

**修改内容**：
```csharp
// 删除：
// moduleCatalog.AddModule<ClinicalWorkstationModule>();
// moduleCatalog.AddModule<AdminWorkstationModule>();

// 添加：
moduleCatalog.AddModule<ClinicalModule>();
moduleCatalog.AddModule<AdminModule>();
```

**验收标准**：
- ✅ ClinicalModule / AdminModule 已注册
- ✅ 旧的 Workstation 模块注册已删除
- ✅ 编译通过（0 errors, 0 warnings）

---

### 2.5 配置检查与更新

#### FR-CF-01: 配置文件检查
**需求描述**：检查并更新所有硬编码的视图名称。

**检查范围**：
1. **App.config**（如有）
2. **导航相关配置**：
   - 检查是否有 "HomeView" 字符串
   - 检查是否有 "ClinicalWorkstationView" / "AdminWorkstationView" 字符串
3. **日志配置**：
   - 检查命名空间过滤器（如 `LYBT.Desktop.ClinicalWorkstation.*`）

**验收标准**：
- ✅ 所有硬编码视图名称已更新
- ✅ 命名空间引用已更新
- ✅ 无遗留旧架构引用

---

## 🛡️ 3. 非功能需求

### 3.1 性能要求
- **NFR-P-01**: 登录后导航到角色主页的响应时间 ≤ 500ms
- **NFR-P-02**: 角色主页到业务视图的导航响应时间 ≤ 300ms

### 3.2 兼容性要求
- **NFR-C-01**: 支持 Windows 10/11 操作系统
- **NFR-C-02**: 支持 .NET 8.0 运行时
- **NFR-C-03**: 向后兼容现有业务模块（Patients、Consultation、Prescriptions等）

### 3.3 可维护性要求
- **NFR-M-01**: 代码编译 0 errors, 0 warnings
- **NFR-M-02**: 命名规范与项目现有模块保持一致
- **NFR-M-03**: 架构文档同步更新

### 3.4 安全性要求
- **NFR-S-01**: 角色路由必须基于已认证用户的真实角色（不允许前端伪造）
- **NFR-S-02**: 未授权用户尝试访问角色主页时应跳转回登录页

---

## 🧪 4. 验收标准

### 4.1 功能验收（手工测试）

#### 测试场景1：医生登录流程
1. 启动应用
2. 在 LoginView 输入医生账号密码
3. 点击登录
4. **期望结果**：导航到 ClinicalHomeView
5. 点击"开始接诊"按钮
6. **期望结果**：导航到 MedicalCaseFlowView

#### 测试场景2：管理员登录流程
1. 启动应用
2. 在 LoginView 输入管理员账号密码
3. 点击登录
4. **期望结果**：导航到 AdminHomeView
5. 依次点击6个功能卡片
6. **期望结果**：正确导航到对应管理视图

#### 测试场景3：统计占位提示
1. 医生登录后查看 ClinicalHomeView
2. **期望结果**：统计卡片显示"📊 统计功能开发中（MVP后期实现）"
3. **期望结果**：不显示任何具体数字

---

### 4.2 技术验收

#### 编译验收
- ✅ `dotnet build LYBT.All.sln -c Release --no-restore` 通过
- ✅ 0 errors, 0 warnings

#### 架构验收
- ✅ 目录结构符合设计（Roles/ vs Modules/）
- ✅ 命名空间符合规范（LYBT.Desktop.Clinical / LYBT.Desktop.Admin）
- ✅ 旧代码完全删除（Workstations/ 目录不存在）

#### 代码审查
- ✅ 无 `[Obsolete]` 标记（旧代码已删除）
- ✅ 无硬编码的旧视图名称（"HomeView"、"ClinicalWorkstationView"）
- ✅ 日志输出使用新的命名空间

---

## 📅 5. 实施计划

### 5.1 Phase划分

**Phase 1（唯一）：角色模块化重构**
- 工作量：9-14小时
- 执行策略：一次性完成（不允许新旧架构共存）

---

### 5.2 任务清单（按执行顺序）

| 序号 | 任务 | 工作量估算 | 依赖项 |
|-----|------|-----------|-------|
| 1 | 创建 Clinical 模块结构 | 0.5h | 无 |
| 2 | 迁移 ClinicalHomeView/ViewModel | 1.5h | 任务1 |
| 3 | 调整 ClinicalHomeView 统计卡片（占位提示） | 0.5h | 任务2 |
| 4 | 创建 Admin 模块结构 | 0.5h | 无 |
| 5 | 设计 AdminHomeView（6个功能卡片） | 2h | 任务4 |
| 6 | 实现 AdminHomeViewModel 导航逻辑 | 1h | 任务5 |
| 7 | 创建 RoleNavigationService | 1h | 无 |
| 8 | 调整 LoginViewModel 集成角色路由 | 0.5h | 任务7 |
| 9 | 更新模块注册（添加 Clinical/Admin） | 0.5h | 任务1,4 |
| 10 | 删除 Workstations/ 目录 | 0.5h | 任务9 |
| 11 | 删除 Shell/HomeView | 0.5h | 任务2 |
| 12 | 检查配置文件（App.config等） | 1h | 任务10,11 |
| 13 | 手工测试（医生+管理员登录流程） | 1h | 任务1-12 |
| 14 | 更新架构文档 | 1h | 任务1-13 |

**总计**：约 12小时（在9-14小时估算范围内）

---

### 5.3 风险与缓解措施

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| 配置文件遗漏硬编码视图名称 | 中 | 低 | 使用全局搜索检查 "HomeView"、"Workstation" 关键词 |
| 旧代码删除后引用错误 | 高 | 中 | 删除前全局搜索命名空间引用 |
| 角色路由逻辑错误 | 高 | 低 | 手工测试两种角色登录流程 |
| AdminHomeView 样式不一致 | 低 | 中 | 参考 ClinicalHomeView 样式，复用资源字典 |

---

## 📚 6. 相关文档

### 6.1 架构文档
- `docs/explanation/architecture/client/README.md` - Client端架构总览
- `docs/explanation/architecture/client/workstation-refactoring-discussion.md` - 重构讨论文档

### 6.2 设计文档
- `docs/explanation/architecture/client/ui-standards.md` - UI设计规范
- ADR-003: Workstation架构重构决策

### 6.3 代码参考
- `src/Client/Desktop/Shell/Views/HomeView.xaml` - 迁移参考（删除前）
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/` - 模块结构参考

---

## 📝 7. 变更历史

| 版本 | 日期 | 变更内容 | 作者 |
|-----|------|---------|------|
| v1.0 | 2025-10-21 | 初始版本，基于讨论文档生成需求规格 | Claude Code |

---

## ✅ 8. 需求确认

**需求确认人**：用户
**确认日期**：2025-10-21
**确认方式**：逐项讨论（Q1-Q5）

**确认结果**：
- ✅ 所有功能需求已确认
- ✅ 非功能需求已确认
- ✅ 验收标准已确认
- ✅ 实施计划已确认

**下一步**：创建 GitHub Issue，开始实施 Phase 1
