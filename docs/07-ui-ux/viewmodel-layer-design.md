# ViewModel 层设计文档
> 版本: v1.2 | 日期: 2026-09-14 | 状态: Phase 2 进行中（Shell + Auth + Patients 已完成，见 §八 迁移记录）

## 一、当前状态分析

### 1.1 文件统计
| 指标 | 数量 |
|------|------|
| ViewModel 文件总数 | 55 |
| 使用基类的 ViewModel | 38 (69%) |
| 独立 ViewModel | 17 (31%) |

### 1.2 基类分布
| 基类 | 使用数 | 用途 |
|------|--------|------|
| NavigableViewModelBase | 25 | 导航视图（主业务页面） |
| DialogViewModelBase | 8 | 对话框 |
| MasterDetailViewModelBase | 5 | 主从详情页 |
| ChildViewModelBase | 5 | 子组件 |
| EditorViewModelBase | 4 | 编辑器 |
| ConnectionTestViewModelBase | 2 | 连接测试 |
| HerbItemViewModelBase | 1 | 药材项 |

### 1.3 代码模式统计（2026-09-14 实测口径，Patients 批次后复测）

| 模式 | 计数 | 度量口径（grep 排除 bin/obj） | 状态 |
|------|------|------------------------------|------|
| `[ObservableProperty]` | 242 | `\[ObservableProperty\]` 行数 | ✅ 已采用 |
| `[RelayCommand]` | 174 | `\[RelayCommand` 行数 | ✅ 已采用 |
| `OnPropertyChanged(` / `SetProperty(` 调用 | 828 | 调用点行数（**含**代理属性广播与派生属性通知，非全是「手写属性」） | ⚠️ 逐模块评估 |
| `public ICommand` / `IAsyncRelayCommand` 声明 | 60 | 属性声明行数（含 15 处 Shell 菜单命令代理、3 处 Auth 子 VM 命令代理等**有意的委托**） | ⚠️ 逐模块评估 |
| 手写命令实例化（`new RelayCommand`/`new AsyncRelayCommand`/`new DelegateCommand`） | 34 | 实例化行数（**真正的迁移目标**：MedicalCase 2 个 VM、Shell 2 个 Service、Controls 2 个 code-behind） | ⚠️ 待迁移 |

> **口径说明（v1.1）**：v1.0 表中「手写 INPC 700 / 手写 ICommand 132」为设计阶段估计值，未写度量口径，
> 本次按上表口径实测复核（`SetProperty(` 与代理广播被计入，故不能等同于「待迁移量」）。
> **判断迁移价值的标准是「是否有本 VM 自己的字段 + 手写 setter/命令实例化」，而不是调用点数量**——
> 代理属性（状态 SSOT 在子 VM/Service）与派生属性通知必须保留手写通知，迁移反而会引入第二份状态。

### 1.4 模块分布
| 模块 | ViewModel 数 | 复杂度 |
|------|-------------|--------|
| MedicalCase | 10 | 高（医案/问诊/处方） |
| Admin | 10 | 中（用户/配置/备份） |
| Clinical | 7 | 高（患者选择/读卡器） |
| Catalog | 6 | 中（药材/验方） |
| Shell | 5+3 | 中（导航/对话框） |
| Auth | 5 | 中（登录/连接） |
| Patients | 3 | 中（CRUD/导入导出） |
| Users | 2 | 低 |
| Registrations | 2 | 低 |

---

## 二、技术栈最佳实践

### 2.1 CommunityToolkit.Mvvm（已采用）
**最佳实践：**
- 使用 `[ObservableProperty]` 替代手写 INotifyPropertyChanged
- 使用 `[RelayCommand]` 替代手写 ICommand
- 使用 `[NotifyDataErrorInfo]` 替代手写验证
- 使用 `[NotifyPropertyChangedFor]` 联动属性
- 使用 `[NotifyCanExecuteChangedFor]` 联动命令

**当前差距：** 700 处手写 INPC + 132 处手写 ICommand

### 2.2 WPF + Prism
**最佳实践：**
- ViewModel 继承 Prism 基类（BindableBase）
- 使用 IEventAggregator 跨模块通信
- 使用 RegionManager 管理视图区域
- 使用 IDialogService 管理对话框
- ViewModel 注册在 Module 中

**当前状态：** ✅ 已遵循（Prism 基类 + EventAggregator + RegionManager）

### 2.3 MVVM 分层
**最佳实践：**
- View → ViewModel → Service → Repository
- ViewModel 不直接访问 DbContext
- Service 层封装业务逻辑
- Repository 层封装数据访问

**当前状态：** ✅ 已遵循（ViewModel → Service → Repository）

---

## 三、差距分析

### 3.1 高优先级差距

| 差距 | 影响 | 优先级 |
|------|------|--------|
| 手写 INPC 未迁移为 [ObservableProperty] | 代码冗余、维护成本高 | ⚠️ 高 |
| 手写 ICommand 未迁移为 [RelayCommand] | 代码冗余、维护成本高 | ⚠️ 高 |
| 部分 ViewModel 未使用基类 | 重复代码 | 中 |

### 3.2 中优先级差距

| 差距 | 影响 | 优先级 |
|------|------|--------|
| 部分 ViewModel 属性命名不规范 | 可读性 | 中 |
| 部分命令缺少 CanExecute 逻辑 | 用户体验 | 中 |
| 部分 ViewModel 缺少单元测试 | 质量保障 | 中 |

### 3.3 低优先级差距

| 差距 | 影响 | 优先级 |
|------|------|--------|
| 部分 ViewModel 文档不完整 | 可维护性 | 低 |
| 部分属性缺少变更通知 | 绑定失效 | 低 |

---

## 四、设计建议

### 4.1 渐进式迁移策略

**原则：** 合理性优先，不是改动大小。逐模块迁移，每模块独立可验证。

**迁移顺序（含完成状态，2026-09-14）：**
1. ✅ **Shell 模块**（5 个 ViewModel）— 核查完成：**无需迁移**（5 个 VM 已全量使用 `[ObservableProperty]`/`[RelayCommand]`；`OnPropertyChanged` 仅用于代理/派生属性广播，属 SSOT 设计；15 个 `public ICommand` 全部委托 `IShellServices.Menu`）——详见 §八
2. ✅ **Auth 模块**（5 个 ViewModel + 1 基类）— 已迁移：`LoginViewModel` 4 处手写命令实例化 → `[RelayCommand]`（含 `CanExecute = nameof(CanLogin)`）；其余 4 个 VM 与 `ConnectionTestViewModelBase` 已符合最佳实践——详见 §八
3. ✅ **Patients 模块**（3 个 ViewModel）— 已迁移/核查：`PatientEditorViewModel`（手写 `SetProperty` 属性 → `[ObservableProperty]`）、`PatientCardReaderViewModel`（private-set 手写属性 → `[ObservableProperty]`）；`PatientMasterDetailViewModel` 核查后**无需改动**（6 个 `[RelayCommand]` 源生成，属性全部来自基类与子 VM 代理）——详见 §八
4. ⬜ **Catalog 模块**（6 个 ViewModel）— 药材/验方，中等复杂度
5. ⬜ **MedicalCase 模块**（10 个 ViewModel）— 最复杂，最后迁移（现存 2 处手写命令实例化：`Workspace/MedicalCaseCommandsViewModel`、Clinical `MedicalCaseWorkspaceViewModel`）
6. ⬜ **Shell 服务层**（`MenuManager`/`NavigationManager` 2 处手写命令实例化）与 Controls code-behind（2 处）— 非 ViewModel，按需并入

### 4.2 迁移规则

**INPC 迁移：**
```csharp
// Before
private string _name;
public string Name
{
    get => _name;
    set { _name = value; OnPropertyChanged(); }
}

// After
[ObservableProperty]
private string _name;
```

**ICommand 迁移：**
```csharp
// Before
public ICommand SaveCommand { get; }
public ViewModel()
{
    SaveCommand = new RelayCommand(Save, CanSave);
}

// After
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { ... }
private bool CanSave() => ...;
```

### 4.3 测试策略

**每个迁移的 ViewModel 需要：**
- 单元测试覆盖所有公共属性
- 单元测试覆盖所有命令
- 集成测试验证 ViewModel → Service 链路

---

## 五、实施计划

### Phase 1：设计文档（当前）
- ✅ 现状分析
- ✅ 差距识别
- ✅ 设计建议

### Phase 2：代码迁移（OMP 执行）— 进行中（Shell ✅ / Auth ✅ / Patients ✅，2026-09-14）
- ✅ 逐模块核查 + 迁移 INPC/ICommand（Shell 核查无需迁移；Auth 迁移 `LoginViewModel` 4 处命令；Patients 迁移 2 处手写属性）
- ✅ 每模块独立验证（build + test，见 §八）
- ✅ 文档同步更新（本文件 §1.3/§4.1/§五/§八）
- ⬜ 剩余模块（Catalog → MedicalCase + Shell 服务层与 Controls）按 §4.1 顺序推进

### Phase 3：测试补充
- 为迁移的 ViewModel 补充单元测试
- 运行全量测试确认无回归

---

## 六、风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 迁移引入 bug | 中 | 高 | 每模块独立验证 + 回归测试 |
| 性能下降 | 低 | 中 | 性能测试对比 |
| 兼容性问题 | 低 | 低 | CommunityToolkit.Mvvm 已稳定 |

---

## 七、附录

### 7.1 技术栈版本
- .NET 8.0
- CommunityToolkit.Mvvm 8.4.2
- Prism.Wpf 8.1.97
- MaterialDesignThemes 5.3.2

### 7.2 参考文档
- CommunityToolkit.Mvvm 文档：https://learn.microsoft.com/zh-cn/dotnet/communitytoolkit/mvvm/
- Prism 文档：https://prismlibrary.com/docs/
- MVVM 模式指南：https://learn.microsoft.com/zh-cn/dotnet/architecture/mvvm/

---

## 八、迁移记录

### 8.1 批次 1-2：Shell + Auth（2026-09-14）

**Shell 模块（5 个 ViewModel）— 核查结论：无需迁移（0 处改动）**

| 文件 | 核查结果 | 依据 |
|------|----------|------|
| `MainWindowViewModel` | 无手写属性/命令 | 状态全为代理：`ILoginStateManager`（Title/CurrentUser/IsLoggedIn…）、`IShellServices.Sidebar`（IsSidebarExpanded/SidebarWidth/IsNavTextVisible）、`INavigationManager`（SelectedNavItem）；15 个 `public ICommand` 全部 `=> _shell.Menu.*Command`；`OnPropertyChanged(...)` 为代理/派生属性广播（`OnLoginStateChanged`/`OnSidebarStateChanged`/`OnLoginSuccessHandled`） |
| `SideNavViewModel` | 无手写属性/命令 | `IsSidebarExpanded`/`IsDarkMode`/`SelectedNavItem` 均为 SSOT 代理（侧栏状态/主题服务/导航管理器）；`NavigationItems`/`GroupedNavigationItems` 为派生只读；`LogoutCommand` 已是 `[RelayCommand]` |
| `HeaderViewModel` | 无手写属性/命令 | 3 个属性代理 `ILoginStateManager`；`EditProfileCommand` 代理 `_shell.Menu` |
| `FooterViewModel` | 无手写属性/命令 | 唯一自有状态 `CurrentTimeDisplay` 已用 `[ObservableProperty]`；其余代理 `IStatusBarManager`（含 `ApiStatusText` 派生文案） |
| `AccountSettingsViewModel` | 已符合最佳实践 | 9 处 `[ObservableProperty]` + 3 处 `[RelayCommand]`（含 `CanExecute = nameof(CanSaveProfile/CanChangePassword)`） |

> **为什么不迁**（合理性优先）：这些 VM 的手写 `OnPropertyChanged` 是「子 VM/服务状态变更 → 宿主绑定面重新广播」的必要动作；
> 若改成 `[ObservableProperty]` 字段，相当于在本 VM 复制一份状态，违反 #134 确立的 SSOT（侧栏/主题/登录状态单一真相源），
> 且会让 `Ctrl+M`、汉堡按钮、深链登录态等出现双份状态不同步的回归风险。

**Auth 模块（5 个 ViewModel + 1 基类）— 迁移 1 个文件**

| 文件 | 变更 | 说明 |
|------|------|------|
| `LoginViewModel` | **4 处手写命令 → `[RelayCommand]`** | `LoginCommand`（原 `new AsyncRelayCommand(ExecuteLoginAsync, predicate)` → `[RelayCommand(CanExecute = nameof(CanLogin))] LoginAsync`，谓词逐字保留 `用户名/密码非空 && !IsLoading`，防重入语义不变）；`CloseApplicationCommand`、`OpenSettingsCommand`、`ForgotPasswordCommand` 同理（命名保持 XAML 绑定面不变） |
| `LoginViewModel` | 保留 3 处命令代理 | `RetryApiCheck/SwitchToLocal/SwitchToRemote` 由 `ConnectionStatus` 子 VM 生成，改为表达式体属性（`=> ConnectionStatus.XxxCommand`），保持**同一命令实例**（子 VM 的 `CanExecute` 通知仍直达 UI） |
| `LoginViewModel` | 保留代理属性（未迁 `[ObservableProperty]`） | `Username/Password/RememberUsername/RememberPassword/HasSavedPassword/ApiStatus/…` 状态唯一真相源在 `LoginCredentialsViewModel`/`ConnectionStatusViewModel`，迁字段会造成双份状态；已在代码注释中写明依据 |
| `LoginCredentialsViewModel`、`ConnectionStatusViewModel`、`ConnectionStatusViewModelTests`（配套） | 已符合最佳实践 | 6+6 处 `[ObservableProperty]`、3 处 `[RelayCommand]` |
| `ConnectionTestViewModelBase`、`FirstRunSetupViewModel`、`ServerConfigViewModel` | 已符合最佳实践 | `[RelayCommand(CanExecute = …)]` + `OnRemoteUrlChangedCore`/`OnIsLoadingChangedCore` 钩子通知；手写 `OnPropertyChanged` 仅用于派生属性（`IsNotTesting`/`ShouldShowFallbackHint`） |

**验证证据**

| 检查 | 命令 | 结果 |
|------|------|------|
| 全量编译 | `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告（`src/Client/Desktop/` 目录下无独立 sln，按仓库门禁用根 sln 覆盖同等范围） |
| Auth 回归 | `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Auth"` | ✅ 89/89（含 `LoginViewModelTests` 全部命令/CanExecute 用例） |
| Shell 回归 | `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Shell"` | ✅ 35/35（含 `ShellViewModelBindingTests`、`ShellViewViewModelBindingTests`、`ShellFlow/ModeSwitchE2ETests`） |
| 绑定面一致性 | `LoginView.xaml` 命令绑定名核对 | ✅ `LoginCommand`/`CloseApplicationCommand`/`OpenSettingsCommand`/`ForgotPasswordCommand`/`SwitchToLocal|RemoteCommand` 全部保持不变 |

**未迁移项（有意保留，登记）**：`LoginViewModel.IsAutoLogin`（纯 UI 状态自动属性，无消费者订阅其变更通知，转 `[ObservableProperty]` 无功能收益）；`ClinicName`（构造期只读注入）。

### 8.2 批次 3：Patients（2026-09-14）

| 文件 | 核查/变更 | 说明 |
|------|-----------|------|
| `PatientEditorViewModel` | **迁移 1 处手写属性** | `public PatientEditContext Patient { get => _patient; set => SetProperty(ref _patient, value); }` → `[ObservableProperty] private PatientEditContext _patient = PatientEditContext.CreateNew();`（属性名/可写性不变，`Context` 重写与 `SubscribeContext()` 调用点不变） |
| `PatientCardReaderViewModel` | **迁移 1 处手写属性** | `IsReadingCard`（`private set => SetProperty(...)`）→ `[ObservableProperty] private bool _isReadingCard;`；写入点仍仅 `ReadCardAsync`（`true`/`false`）+ `CanReadCard()` 读取，通知语义与 `ReadCardCommand` 的 CanExecute 时机保持原样（**未**追加 `NotifyCanExecuteChangedFor`，避免改变运行期行为——原实现亦无该通知） |
| `PatientMasterDetailViewModel` | **无需改动** | 6 个命令全部 `[RelayCommand]` 源生成（`ReadCardCommand` 带 `CanExecute`）；属性一律来自 `MasterDetailViewModelBase` 或子 VM/服务代理（`PatientEditor`/`CardReaderViewModel`/`IsCardReaderConnected`/`IsReadingCard`）；无手写命令实例化 |

> **未纳入迁移（有意保留）**：`Models/PatientDetailModel.cs` 与 `Models/Items/PatientEditContext.cs` 共 12 处 `SetProperty(...)`——
> 它们是 **Model**（非 ViewModel），且多处带**联动副作用**（如 `if (SetProperty(ref _birthDate, value)) { …计算 Age… }`），
> 迁 `[ObservableProperty]` 会把联动逻辑改写成 `OnXxxChanged` 分部方法，收益低、风险高；如需统一，另立批次评估。

**验证证据（批次 3）**

| 检查 | 命令 | 结果 |
|------|------|------|
| 模块编译 | `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Patients/ --no-incremental` | ✅ 0 错误 0 警告 |
| Patients 回归 | `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Patients"` | ⚠️ 30/31——唯一失败 `Integration/RemoteApi/DoctorRoleTests.SearchPatients_ByKeyword`（远程患者关键词检索 500），**既有缺陷**：`git stash` 摘除本批次改动后同一用例仍失败（对照运行已留证），根因见 13c #137③a/#139；与 ViewModel 迁移无关 |
| 全量编译 | `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告 |

### 8.3 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.2 | 2026-09-14 | Phase 2 批次 3：Patients 模块迁移（2 属性）+ §1.3 复测计数 + §八.2 记录 | 渐进式迁移落地 |
| v1.1 | 2026-09-14 | Phase 2 批次 1-2：Shell 核查（无需迁移）+ Auth `LoginViewModel` 命令迁移；§1.3 计数改为可复算口径；补 §八 迁移记录 | 渐进式迁移落地，数字与结论须可核验 |
| v1.0 | 2026-09-14 | 建立 ViewModel 层设计文档（现状/差距/迁移策略/风险） | Phase 1 设计 |
