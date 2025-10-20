# ADR-003: Workstation模块架构重构

## 状态
已接受 (Accepted)

## 日期
2025-01-20

## 背景

### 当前架构问题
在实施Issue #1512（HomeView显示问题）时，发现当前的Workstation架构存在以下问题：

1. **导航层次过深**
   ```
   LoginView → HomeView → ClinicalWorkstationView → MedicalCaseFlowView (4层)
   ```

2. **职责重叠**
   - HomeView：提供【开始看诊】入口 + 快速搜索 + 次要功能导航
   - ClinicalWorkstationView：提供侧边栏导航 + ClinicalContentRegion容器
   - 两者功能重复，都在做导航

3. **过度抽象**
   - ClinicalWorkstation和AdminWorkstation只是UI布局容器（侧边栏 + ContentRegion）
   - 没有业务逻辑价值
   - 引入了额外的Region（ClinicalContentRegion），增加复杂度

4. **违背MVP原则**
   - 不符合`.spec-workflow/steering/constitution.md`中的"够用就好"原则
   - 引入了不必要的抽象层
   - 未带来实质性的业务价值

### 未来扩展需求
系统将扩展以下角色和场景：
- **医生**（当前）：看诊流程、病历查询、个人统计
- **前台/收费员**（未来）：挂号管理、收费结算、排队叫号
- **药房**（未来）：处方调配、库存管理、发药记录
- **管理员**（当前）：用户管理、系统设置、数据统计

## 决策

### 核心决策

#### 1. 取消Workstation作为"UI容器"的设计 ❌

**理由**：
- Workstation只提供侧边栏布局，没有业务逻辑
- Region嵌套增加复杂度（LoginRegion → ContentRegion → ClinicalContentRegion）
- 与HomeView功能重叠

**替代方案**：
- 每个角色模块自己管理HomeView
- 所有业务视图注册到Shell的ContentRegion
- 无需子Region嵌套

#### 2. 保留"角色业务模块"的概念 ✅

**理由**：
- 符合领域驱动设计（DDD）
- 按角色聚合业务功能，职责清晰
- 利于未来扩展新角色

**实施方式**：
```
Workstation ≠ UI容器
Workstation = 角色业务模块（包含HomeView + 业务视图 + 业务逻辑）
```

#### 3. 简化Region设计 ✅

**当前设计**：
```
Shell:
  ├─ LoginRegion（登录视图）
  └─ ContentRegion（主内容）
      └─ ClinicalWorkstation:
           └─ ClinicalContentRegion（医生业务内容）
```

**简化后设计**：
```
Shell:
  ├─ LoginRegion（登录视图）
  └─ ContentRegion（所有角色的业务视图）
```

**收益**：
- 导航路径统一：`_regionManager.RequestNavigate("ContentRegion", viewName)`
- 无需管理多个Region
- Prism导航更简单

### 推荐架构：插件式角色业务模块

```
Shell（通用容器）
  ├─ MainWindow（ContentRegion + 顶部工具栏 + 状态栏）
  └─ LoginView

角色业务模块（按需加载）：
  ├─ Clinical模块（医生业务）
  │    ├─ ClinicalHomeView（医生主页）
  │    ├─ MedicalCaseFlowView（看诊流程）
  │    ├─ PatientQueryView（病历查询）
  │    └─ ClinicalModule.cs（注册视图和服务）
  │
  ├─ Reception模块（前台业务，未来）
  │    ├─ ReceptionHomeView（前台主页）
  │    ├─ RegistrationView（挂号登记）
  │    ├─ BillingView（收费结算）
  │    └─ ReceptionModule.cs
  │
  ├─ Pharmacy模块（药房业务，未来）
  │    ├─ PharmacyHomeView（药房主页）
  │    ├─ DispensingView（处方调配）
  │    └─ PharmacyModule.cs
  │
  └─ Admin模块（管理业务）
       ├─ AdminHomeView（管理主页）
       ├─ UserManagementView（用户管理）
       └─ AdminModule.cs
```

### 导航流程

**登录后导航**：
```
MainWindowViewModel.LoadMainContent()
  → 判断角色
    → 医生：导航到 ClinicalHomeView
    → 前台：导航到 ReceptionHomeView
    → 药房：导航到 PharmacyHomeView
    → 管理员：导航到 AdminHomeView
```

**角色内导航**（以医生为例）：
```
ClinicalHomeView
  ├─ 【开始看诊】→ MedicalCaseFlowView
  ├─ 快速搜索 → MedicalCaseFlowView（携带搜索参数）
  ├─ 病历查询 → PatientQueryView
  └─ 个人统计 → StatisticsView
```

## 实施方案

### MVP阶段的过渡方案（Phase 0）

**当前状态**：
- ✅ Shell/HomeView临时作为医生主页
- ✅ 导航路径：LoginView → HomeView → MedicalCaseFlowView
- ✅ ClinicalWorkstation和AdminWorkstation模块保留但不使用

**修复内容**：
1. `MainWindowViewModel.cs:566`：导航目标从`ClinicalWorkstationView`改为`HomeView`
2. `App.xaml.cs:104`：添加`containerRegistry.RegisterForNavigation<HomeView>()`

**编译验证**：
```
✅ 0 errors, 0 warnings
✅ 编译时间: 21.40秒
```

### 未来重构路径

#### Phase 1：重命名和迁移（MVP完成后）
```
1. 将Shell/HomeView重命名为ClinicalHomeView
2. 移动到Clinical模块（新建或改造现有MedicalCase模块）
3. 更新导航逻辑
```

#### Phase 2：扩展前台模块（前台功能开发时）
```
1. 创建LYBT.Desktop.Reception模块
2. 创建ReceptionHomeView（前台主页）
3. 实现挂号、收费、排队等业务视图
4. 注册ReceptionModule到Prism
```

#### Phase 3：架构优化（所有角色实现后）
```
1. 按角色聚合业务模块
2. 实现动态模块加载（根据用户角色加载对应模块）
3. 移除或重构ClinicalWorkstation/AdminWorkstation模块
```

### 推荐目录结构

```
src/Client/Desktop/Modules/
  ├─ LYBT.Desktop.Clinical/          # 医生业务模块
  │    ├─ Views/
  │    │    ├─ ClinicalHomeView.xaml  # 医生主页
  │    │    ├─ MedicalCaseFlowView.xaml
  │    │    └─ PatientQueryView.xaml
  │    ├─ ViewModels/
  │    └─ ClinicalModule.cs           # 模块注册
  │
  ├─ LYBT.Desktop.Reception/         # 前台业务模块（未来）
  │    ├─ Views/
  │    │    ├─ ReceptionHomeView.xaml # 前台主页
  │    │    ├─ RegistrationView.xaml  # 挂号登记
  │    │    └─ BillingView.xaml       # 收费结算
  │    ├─ ViewModels/
  │    └─ ReceptionModule.cs
  │
  ├─ LYBT.Desktop.Pharmacy/          # 药房业务模块（未来）
  │    ├─ Views/
  │    │    ├─ PharmacyHomeView.xaml  # 药房主页
  │    │    └─ DispensingView.xaml    # 处方调配
  │    ├─ ViewModels/
  │    └─ PharmacyModule.cs
  │
  └─ LYBT.Desktop.Admin/             # 管理员模块
       ├─ Views/
       │    ├─ AdminHomeView.xaml     # 管理主页
       │    └─ UserManagementView.xaml
       ├─ ViewModels/
       └─ AdminModule.cs
```

## 收益

### 立即收益（Phase 0）
1. ✅ 修复HomeView显示问题（Issue #1512）
2. ✅ 简化导航层次（从4层降到3层）
3. ✅ 符合MVP原则（够用就好）
4. ✅ 保持编译通过（0 errors, 0 warnings）

### 长期收益（Phase 1-3）
1. ✅ 架构清晰：按角色聚合业务模块
2. ✅ 易于扩展：新增角色只需新建模块
3. ✅ 职责单一：每个模块只关注自己的业务
4. ✅ 符合DDD：领域驱动设计
5. ✅ 减少复杂度：无需管理多个嵌套Region

## 风险与缓解

### 风险1：大规模重构影响MVP进度
**缓解措施**：
- Phase 0采用过渡方案，不阻塞当前开发
- Phase 1-3在MVP完成后逐步实施

### 风险2：现有代码依赖ClinicalWorkstation
**缓解措施**：
- 保留ClinicalWorkstation模块代码，只是不使用
- 逐步迁移依赖，确保兼容性

### 风险3：Region导航变更可能影响现有业务视图
**缓解措施**：
- 所有业务视图统一注册到ContentRegion
- 导航逻辑保持一致：`_regionManager.RequestNavigate("ContentRegion", viewName)`

## 备注

### 相关Issue
- Issue #1512: [Bug] Shell启动后无法显示HomeView医生主页

### 相关Task
- Task #1495: HomeView极简化改造（Epic #1494）
- Task #1496: MedicalCaseFlowView核心框架
- Task #1497: Step 1 - PatientSelectionView
- Task #1498: Step 2 - ConsultationFormView

### 相关文档
- `.spec-workflow/steering/constitution.md`: MVP原则、够用就好
- `docs/architecture/client/README.md`: Client端架构指南
- `docs/architecture/shared/clinical-workflow-current-process.md`: 就诊流程讨论

### 架构决策原则
本决策遵循以下原则：
1. **MVP优先**：够用即好，避免过度设计
2. **架构前瞻**：考虑未来扩展需求（前台、药房等）
3. **渐进重构**：分Phase实施，不影响当前开发
4. **DDD指导**：按业务领域聚合模块，而非UI布局

## 结论

**取消Workstation作为"UI容器"的设计，保留"角色业务模块"的概念。**

- ❌ Workstation ≠ 侧边栏容器
- ✅ Workstation = 角色业务模块（如Clinical、Reception、Admin）
- ✅ MVP阶段用Shell/HomeView过渡
- ✅ 未来按角色聚合业务模块
- ✅ 简化Region设计（只用ContentRegion）

这个决策既满足MVP阶段的"够用就好"，又为未来扩展（挂号、前台、药房等）提供了清晰的架构路径。
