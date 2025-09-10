# LYBT.Desktop.Shell - 类与方法详细文档

> **生成时间**: 2025-01-10  
> **项目路径**: src/Client/Desktop/LYBT.Desktop.Shell  
> **项目类型**: WPF Shell Application  
> **目标框架**: net8.0-windows  

## App (src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:1-220)

### 1) 元信息

- **类型**: partial class, public
- **命名空间**: LYBT.Desktop.Shell
- **基类**: PrismApplication
- **实现接口**: (none)
- **修饰符**: public partial
- **归属层角色**: WPF Application Entry Point

### 2) 特性与注解

- **Prism应用程序**: 基于Prism.DryIoc框架的模块化WPF应用
- **XAML支持**: 配套App.xaml资源定义

### 3) 方法清单

| 可见性                | async | 返回类型   | 方法名(参数列表)                                                                                                   | 源码行号    |
| ------------------ | ----- | ------ | ----------------------------------------------------------------------------------------------------------- | ------- |
| protected override | -     | Window | CreateShell()                                                                                               | 25-35   |
| protected override | -     | void   | RegisterTypes(IContainerRegistry containerRegistry)                                                         | 37-67   |
| protected override | -     | void   | ConfigureModuleCatalog(IModuleCatalog moduleCatalog)                                                        | 69-109  |
| protected override | async | void   | OnInitialized()                                                                                             | 111-145 |
| public             | async | Task   | LoadRoleBasedModulesAsync(string userRole)                                                                  | 147-175 |
| private static     | -     | void   | AddCoreModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType)                             | 177-185 |
| private static     | -     | void   | AddRoleBasedModule(IModuleCatalog moduleCatalog, string moduleName, Type moduleType, string[] allowedRoles) | 187-200 |
| private            | async | Task   | InitializeServicesAsync()                                                                                   | 202-220 |

#### CreateShell()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:25-35`
- **返回类型**: `Window`
- **内部调用**: `Container.Resolve<MainWindow>()`
- **备注**: 创建主窗体实例，Prism应用程序生命周期的关键环节
- **关键特性**: 自动装配MainWindow和其ViewModel

#### RegisterTypes(IContainerRegistry containerRegistry)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:37-67`
- **内部调用**: 
  - `ServiceCollectionExtensions.RegisterAllServices()`
  - `containerRegistry.RegisterForNavigation<MainWindow>()`
  - `RegisterUltraThinkServices()`
- **备注**: 注册所有服务和导航视图，统一依赖注入配置
- **服务类型**:
  - 8个业务模块服务（UltraThink双层架构）
  - API客户端服务（Refit）
  - 错误处理和日志服务
  - 对话框和UI服务

#### ConfigureModuleCatalog(IModuleCatalog moduleCatalog)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:69-109`

- **模块注册**:
  
  ```csharp
  // 核心模块（所有角色）
  AddCoreModule(moduleCatalog, "AuthModule", typeof(AuthModule));
  AddCoreModule(moduleCatalog, "UsersModule", typeof(UsersModule));
  
  // 角色模块（按需加载）
  AddRoleBasedModule(moduleCatalog, "PatientsModule", typeof(PatientsModule), ["Doctor", "Admin"]);
  AddRoleBasedModule(moduleCatalog, "MedicalCaseModule", typeof(MedicalCaseModule), ["Doctor"]);
  AddRoleBasedModule(moduleCatalog, "ConsultationModule", typeof(ConsultationModule), ["Doctor"]);
  ```

- **备注**: 配置8个业务模块，支持角色驱动的模块加载策略

- **模块分类**:
  
  - **核心模块**: Auth, Users（所有角色必需）
  - **医生模块**: Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula
  - **管理员模块**: Users, Patients（管理功能）

#### OnInitialized()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:111-145`
- **初始化步骤**:
  1. 调用基类OnInitialized()
  2. 异步初始化服务
  3. 检查登录状态
  4. 根据角色加载模块
  5. 错误处理和恢复
- **错误处理**: 全局异常捕获，记录日志，显示友好错误信息
- **性能优化**: 关键服务同步初始化，非关键服务异步预热

#### LoadRoleBasedModulesAsync(string userRole)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/App.xaml.cs:147-175`
- **参数**: userRole - 用户角色（"Doctor", "Admin"等）
- **加载策略**:
  
  ```csharp
  switch (userRole)
  {
      case "Doctor":
          // 加载医生相关模块
          await LoadModuleAsync("PatientsModule");
          await LoadModuleAsync("MedicalCaseModule");
          await LoadModuleAsync("ConsultationModule");
          await LoadModuleAsync("PrescriptionsModule");
          await LoadModuleAsync("HerbsModule");
          await LoadModuleAsync("FormulaModule");
          break;
      case "Admin":
          // 加载管理员相关模块  
          await LoadModuleAsync("PatientsModule");
          await LoadModuleAsync("UsersModule");
          break;
  }
  ```
- **备注**: 动态模块加载，提升启动性能和内存效率

### 4) UltraThink架构特点

- **模块化设计**: 8个业务模块独立加载，支持按需初始化
- **角色驱动**: 基于用户角色动态加载功能模块
- **性能优化**: 异步初始化，减少启动时间
- **错误恢复**: 完整的异常处理和用户友好提示

---

## MainWindow (src/Client/Desktop/LYBT.Desktop.Shell/Views/MainWindow.xaml.cs:1-50)

### 1) 元信息

- **类型**: partial class, public
- **命名空间**: LYBT.Desktop.Shell.Views
- **基类**: Window
- **实现接口**: (none)
- **修饰符**: public partial
- **归属层角色**: Main UI Window

### 2) 构造函数

| 可见性    | 参数列表 | 源码行号  |
| ------ | ---- | ----- |
| public | ()   | 25-35 |

#### MainWindow()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/Views/MainWindow.xaml.cs:25-35`
- **内部调用**: `InitializeComponent()`
- **备注**: 极简构造函数，仅初始化UI组件
- **MVVM配置**: 通过Prism ViewModelLocator自动装配ViewModel

### 3) XAML配置特点

- **自动ViewModel装配**: `prism:ViewModelLocator.AutoWireViewModel="True"`
- **区域管理**: 支持LoginRegion和ContentRegion双区域切换
- **键盘快捷键**: 全局快捷键绑定支持

### 4) UI架构设计

- **状态切换**: 登录界面⇔主界面动态切换
- **区域容器**: ContentControl作为模块内容容器
- **响应式布局**: 支持不同分辨率适配

---

## MainWindowViewModel (src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:1-350)

### 1) 元信息

- **类型**: class, public
- **命名空间**: LYBT.Desktop.Shell.ViewModels
- **基类**: ServiceViewModel
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Main Window Controller

### 2) C# 12主构造函数

```csharp
public class MainWindowViewModel(
    IRegionManager regionManager,
    IEventAggregator eventAggregator,
    IMainWindowServicesFacade servicesFacade,
    IErrorHandlingService errorHandlingService) : ServiceViewModel
```

### 3) 属性清单

| 属性名                  | 类型      | 可空  | 绑定方向   | 说明           |
| -------------------- | ------- | --- | ------ | ------------ |
| Title                | string  | 否   | OneWay | 窗口标题         |
| CurrentUser          | UserDto | 是   | OneWay | 当前登录用户       |
| IsLoggedIn           | bool    | 否   | OneWay | 登录状态         |
| IsNotLoggedIn        | bool    | 否   | OneWay | 登录状态取反(UI绑定) |
| CurrentTime          | string  | 否   | OneWay | 实时时钟显示       |
| StatusBarText        | string  | 否   | OneWay | 状态栏文本        |
| IsMainContentVisible | bool    | 否   | OneWay | 主内容区域可见性     |
| IsLoginVisible       | bool    | 否   | OneWay | 登录区域可见性      |

### 4) 命令清单

| 命令名                           | 类型              | 快捷键          | 说明      |
| ----------------------------- | --------------- | ------------ | ------- |
| LogoutCommand                 | DelegateCommand | -            | 退出登录    |
| TestApiCommand                | DelegateCommand | -            | API连接测试 |
| QuickAddPatientCommand        | DelegateCommand | Ctrl+N       | 快速添加患者  |
| QuickStartConsultationCommand | DelegateCommand | Ctrl+Shift+C | 快速开始看诊  |
| ShowHelpCommand               | DelegateCommand | F1           | 显示帮助    |
| ToggleThemeCommand            | DelegateCommand | -            | 切换主题    |

### 5) 方法清单

| 可见性                | async | 返回类型 | 方法名(参数列表)                            | 源码行号    |
| ------------------ | ----- | ---- | ------------------------------------ | ------- |
| private            | async | Task | CheckLoginStatusAsync()              | 85-115  |
| private            | -     | void | LoadMainContent()                    | 117-135 |
| private            | -     | void | OnLoginSuccess()                     | 137-155 |
| private            | async | Task | ExecuteLogoutAsync()                 | 157-185 |
| private            | async | Task | ExecuteToggleThemeAsync()            | 187-205 |
| private            | async | Task | ExecuteQuickAddPatientAsync()        | 207-225 |
| private            | async | Task | ExecuteQuickStartConsultationAsync() | 227-245 |
| private            | -     | void | StartRealTimeClock()                 | 247-265 |
| private            | -     | void | UpdateStatusBar(string message)      | 267-275 |
| protected override | -     | void | OnDisposing()                        | 277-295 |

#### CheckLoginStatusAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:85-115`
- **内部调用**: `servicesFacade.AuthService.GetCurrentUserAsync()`
- **业务逻辑**:
  1. 检查本地token有效性
  2. 验证API连接状态
  3. 获取当前用户信息
  4. 设置登录状态和UI可见性
- **错误处理**: token失效自动跳转登录界面

#### LoadMainContent()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:117-135`
- **导航逻辑**:
  
  ```csharp
  var viewName = CurrentUser?.Role switch
  {
      UserRole.Admin => "SystemWorkbench",
      UserRole.Doctor => "ConsultationWorkbench", 
      _ => "HomeView"
  };
  regionManager.RequestNavigate("ContentRegion", viewName);
  ```
- **备注**: 角色驱动的界面导航，Admin显示系统管理界面，Doctor显示诊疗工作台

#### OnLoginSuccess()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:137-155`
- **处理步骤**:
  1. 更新IsLoggedIn状态
  2. 设置窗口标题（包含用户名）
  3. 加载主要内容区域
  4. 启动实时时钟
  5. 发布登录成功事件
- **事件发布**: `eventAggregator.GetEvent<LoginSuccessEvent>().Publish()`

#### ExecuteLogoutAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:157-185`
- **注销流程**:
  1. 用户确认对话框
  2. 调用API注销接口
  3. 清除本地token和用户信息
  4. 重置UI状态到登录界面
  5. 发布登录状态变更事件
- **安全特性**: 服务器端会话同步注销

#### ExecuteQuickAddPatientAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:207-225`
- **快捷操作**: Ctrl+N快捷键触发
- **导航**: 直接跳转到患者添加界面
- **权限检查**: 验证当前用户是否有患者管理权限

#### StartRealTimeClock()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs:247-265`
- **定时器配置**: 
  
  ```csharp
  _clockTimer = new DispatcherTimer
  {
      Interval = TimeSpan.FromSeconds(1)
  };
  _clockTimer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
  ```
- **备注**: 实时显示系统时间，1秒更新频率

### 6) MVVM绑定模式

#### 属性绑定

- **Title**: 绑定到窗口标题，格式为"凌隐宝堂中医诊所系统 - {用户名}"
- **IsLoggedIn/IsNotLoggedIn**: 控制登录界面和主界面的可见性切换
- **CurrentTime**: 绑定到状态栏，显示实时系统时间

#### 命令绑定

- **全局快捷键**: 通过Window.InputBindings实现
- **按钮命令**: 工具栏和菜单按钮绑定
- **右键菜单**: 上下文菜单命令支持

### 7) 事件聚合模式

- **LoginSuccessEvent**: 登录成功通知其他模块
- **LogoutEvent**: 注销时清理各模块状态
- **UserChangedEvent**: 用户信息变更广播

### 8) 内存管理（DT-013修复）

- **定时器清理**: OnDisposing中停止并释放DispatcherTimer
- **事件取消订阅**: 清理EventAggregator订阅
- **服务释放**: 释放ServicesFacade资源

---

## HomeViewModel (src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/HomeViewModel.cs:1-280)

### 1) 元信息

- **类型**: class, public
- **命名空间**: LYBT.Desktop.Shell.ViewModels
- **基类**: ServiceViewModel
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Home Workbench Controller

### 2) 字段与属性

| 属性名                  | 类型                                    | 可空  | 说明      |
| -------------------- | ------------------------------------- | --- | ------- |
| IsDoctorRole         | bool                                  | 否   | 医生角色标识  |
| IsAdminRole          | bool                                  | 否   | 管理员角色标识 |
| TodayCompletedCount  | int                                   | 否   | 今日完成数   |
| TodayInProgressCount | int                                   | 否   | 今日进行中数  |
| TodayTotalAmount     | decimal                               | 否   | 今日总收入   |
| TodayPatients        | ObservableCollection<TodayPatientDto> | 否   | 今日患者列表  |
| SelectedPatient      | TodayPatientDto                       | 是   | 选中的患者   |
| IsLoading            | bool                                  | 否   | 数据加载状态  |

### 3) 命令清单

#### 医生角色命令

| 命令名                                | 类型              | 说明   |
| ---------------------------------- | --------------- | ---- |
| StartConsultationCommand           | DelegateCommand | 开始看诊 |
| NavigateToPatientReceptionCommand  | DelegateCommand | 患者接待 |
| NavigateToMedicalCaseCommand       | DelegateCommand | 医疗案例 |
| NavigateToPrescriptionQueryCommand | DelegateCommand | 处方查询 |

#### 管理员角色命令

| 命令名                             | 类型              | 说明   |
| ------------------------------- | --------------- | ---- |
| EnterSystemManagementCommand    | DelegateCommand | 系统管理 |
| NavigateToUserManagementCommand | DelegateCommand | 用户管理 |

#### 患者操作命令

| 命令名                                | 类型                               | 说明        |
| ---------------------------------- | -------------------------------- | --------- |
| StartConsultationForPatientCommand | DelegateCommand<TodayPatientDto> | 为特定患者开始看诊 |
| ViewPatientDetailsCommand          | DelegateCommand<TodayPatientDto> | 查看患者详情    |

### 4) 方法清单

| 可见性                | async | 返回类型 | 方法名(参数列表)                                                        | 源码行号    |
| ------------------ | ----- | ---- | ---------------------------------------------------------------- | ------- |
| private            | async | Task | LoadTodayDataAsync()                                             | 85-115  |
| private            | async | Task | LoadTodayStatisticsAsync()                                       | 117-145 |
| private            | async | Task | LoadTodayPatientsAsync()                                         | 147-175 |
| private            | async | Task | ExecuteStartConsultationAsync()                                  | 177-195 |
| private            | async | Task | ExecuteNavigateToPatientReceptionAsync()                         | 197-210 |
| private            | async | Task | ExecuteStartConsultationForPatientAsync(TodayPatientDto patient) | 212-235 |
| private            | async | Task | ExecuteViewPatientDetailsAsync(TodayPatientDto patient)          | 237-255 |
| protected override | -     | void | Dispose(bool disposing)                                          | 257-280 |

#### LoadTodayDataAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/HomeViewModel.cs:85-115`
- **加载流程**:
  1. 设置加载状态
  2. 并行加载统计数据和患者列表
  3. 异常处理和用户提示
  4. 重置加载状态
- **性能优化**: 使用Task.WhenAll并行加载，减少等待时间

#### LoadTodayStatisticsAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/HomeViewModel.cs:117-145`
- **统计API调用**:
  
  ```csharp
  var today = DateTime.Today;
  var completedCases = await _medicalCaseService.GetTodayCompletedCountAsync(today);
  var inProgressCases = await _medicalCaseService.GetTodayInProgressCountAsync(today);
  var totalAmount = await _prescriptionService.GetTodayTotalAmountAsync(today);
  ```
- **数据绑定**: 直接更新UI绑定属性

#### ExecuteStartConsultationForPatientAsync(TodayPatientDto patient)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/HomeViewModel.cs:212-235`
- **业务流程**:
  1. 验证患者信息
  2. 检查是否已有未完成医案
  3. 创建新医案或继续现有医案
  4. 导航到看诊界面
- **导航参数**: 传递患者ID和医案ID到看诊模块

### 5) 角色驱动UI设计

- **IsDoctorRole**: 控制医生功能区域可见性
- **IsAdminRole**: 控制管理员功能区域可见性
- **统一会话管理**: 基于当前用户角色自动配置界面

### 6) 数据模型

#### TodayPatientDto

```csharp
public class TodayPatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime VisitTime { get; set; }
    public string Status { get; set; }  // "等待", "进行中", "已完成"
    public bool HasMedicalCase { get; set; }
    public Guid? MedicalCaseId { get; set; }
}
```

### 7) 内存管理

- **定时器清理**: 清理数据刷新定时器
- **事件取消订阅**: 取消用户状态变更事件订阅
- **集合清理**: 清空ObservableCollection

---

## ServiceCollectionExtensions (src/Client/Desktop/LYBT.Desktop.Shell/Extensions/ServiceCollectionExtensions.cs:1-180)

### 1) 元信息

- **类型**: static class, public
- **命名空间**: LYBT.Desktop.Shell.Extensions
- **基类**: (none)
- **实现接口**: (none)
- **修饰符**: public static
- **归属层角色**: Dependency Injection Configuration

### 2) 扩展方法清单

| 可见性            | 返回类型 | 方法名(参数列表)                                                               | 源码行号    |
| -------------- | ---- | ----------------------------------------------------------------------- | ------- |
| public static  | void | RegisterAllServices(IContainerRegistry containerRegistry)               | 25-45   |
| private static | void | RegisterLayer1BasicModules(IContainerRegistry containerRegistry)        | 47-65   |
| private static | void | RegisterLayer2AuthModules(IContainerRegistry containerRegistry)         | 67-85   |
| private static | void | RegisterLayer3BusinessDataModules(IContainerRegistry containerRegistry) | 87-105  |
| private static | void | RegisterLayer4ProcessModules(IContainerRegistry containerRegistry)      | 107-125 |
| private static | void | RegisterLayer5AggregationModules(IContainerRegistry containerRegistry)  | 127-145 |
| private static | void | RegisterUltraThinkServices(IContainerRegistry containerRegistry)        | 147-165 |
| private static | void | RegisterApiServices(IContainerRegistry containerRegistry)               | 167-180 |

#### RegisterAllServices(IContainerRegistry containerRegistry)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/Extensions/ServiceCollectionExtensions.cs:25-45`
- **注册策略**: 按依赖层级顺序注册，防止循环依赖
- **调用顺序**:
  1. Layer1 基础模块
  2. Layer2 认证模块  
  3. Layer3 业务数据模块
  4. Layer4 流程协调模块
  5. Layer5 聚合服务模块
  6. UltraThink服务
  7. API服务

#### RegisterLayer1BasicModules(IContainerRegistry containerRegistry)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/Extensions/ServiceCollectionExtensions.cs:47-65`
- **模块**: Herbs, Formula
- **依赖特点**: 无外部业务依赖，仅依赖基础设施
- **注册模式**: 
  
  ```csharp
  containerRegistry.RegisterScoped<IHerbService, HerbService>();
  containerRegistry.RegisterScoped<IFormulaService, FormulaService>();
  ```

#### RegisterLayer5AggregationModules(IContainerRegistry containerRegistry)

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/Extensions/ServiceCollectionExtensions.cs:127-145`
- **模块**: Prescriptions（聚合服务）
- **依赖特点**: 依赖所有下层模块，提供高级业务功能
- **聚合职责**: 处方管理需要协调患者、医案、药材、验方等多个模块

### 3) 5层架构注册策略

#### 依赖关系图

```
Layer 5: Prescriptions (聚合服务层)
   ↑
Layer 4: MedicalCase, Consultation (流程协调层)
   ↑  
Layer 3: Patients (业务数据层)
   ↑
Layer 2: Auth, Users (认证层)
   ↑
Layer 1: Herbs, Formula (基础层)
```

#### 防循环依赖设计

- **自底向上注册**: 基础层先注册，聚合层最后注册
- **单向依赖**: 高层可依赖低层，低层不依赖高层
- **接口隔离**: 通过接口注入避免具体实现依赖

### 4) UltraThink架构特点

- **分层注册**: 5层架构确保依赖关系清晰
- **服务隔离**: 每层服务职责单一，易于测试和维护
- **扩展性**: 新增模块按层级插入，不影响现有结构

---

## ConfirmationDialogViewModel (src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/ConfirmationDialogViewModel.cs:1-80)

### 1) 元信息

- **类型**: class, public
- **命名空间**: LYBT.Desktop.Shell.ViewModels
- **基类**: DialogViewModelBase
- **实现接口**: (none)
- **修饰符**: public
- **归属层角色**: Dialog Management

### 2) 属性清单

| 属性名        | 类型              | 可空  | 说明        |
| ---------- | --------------- | --- | --------- |
| Message    | string          | 否   | 对话框消息内容   |
| YesCommand | DelegateCommand | 否   | 确认命令（兼容性） |
| NoCommand  | DelegateCommand | 否   | 取消命令（兼容性） |

### 3) 方法清单

| 可见性                | 返回类型       | 方法名(参数列表)                                       | 源码行号  |
| ------------------ | ---------- | ----------------------------------------------- | ----- |
| public             | void       | SetContent(string message, string title = "确认") | 25-35 |
| protected override | Task<bool> | ExecuteConfirmAsync()                           | 37-47 |
| protected override | Task       | ExecuteCancelAsync()                            | 49-59 |

#### SetContent(string message, string title = "确认")

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/ConfirmationDialogViewModel.cs:25-35`
- **参数设置**: 
  - message: 对话框显示消息
  - title: 对话框标题（默认"确认"）
- **属性更新**: 同步更新Message和Title属性

#### ExecuteConfirmAsync()

- **源码位置**: `src/Client/Desktop/LYBT.Desktop.Shell/ViewModels/ConfirmationDialogViewModel.cs:37-47`
- **返回类型**: `Task<bool>`
- **返回值**: true表示用户确认
- **备注**: 重写基类方法，实现确认逻辑

### 4) UltraThink对话框架构

- **统一基类**: 继承DialogViewModelBase，统一对话框行为
- **兼容性设计**: YesCommand/NoCommand映射到基类命令
- **简化使用**: 通过SetContent方法快速配置对话框内容

### 5) MVVM对话框模式

- **ViewModel驱动**: 对话框逻辑完全在ViewModel中
- **数据绑定**: Message属性绑定到UI显示
- **命令模式**: 确认/取消操作通过命令实现

---

## 主题和设计系统

### UnifiedDesignSystem.xaml (src/Client/Desktop/LYBT.Desktop.Shell/Themes/UnifiedDesignSystem.xaml)

#### 颜色系统定义

```xml
<!-- 基础色彩 -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#2196F3"/>      <!-- 主色调 -->
<SolidColorBrush x:Key="SecondaryBrush" Color="#FF9800"/>    <!-- 辅助色 -->
<SolidColorBrush x:Key="AccentBrush" Color="#4CAF50"/>       <!-- 强调色 -->
<SolidColorBrush x:Key="ErrorBrush" Color="#F44336"/>        <!-- 错误色 -->
<SolidColorBrush x:Key="WarningBrush" Color="#FF5722"/>      <!-- 警告色 -->

<!-- 背景和表面 -->
<SolidColorBrush x:Key="BackgroundBrush" Color="#F5F5F5"/>   <!-- 背景色 -->
<SolidColorBrush x:Key="SurfaceBrush" Color="#FAFAFA"/>      <!-- 表面色 -->
<SolidColorBrush x:Key="CardBrush" Color="#FFFFFF"/>         <!-- 卡片色 -->

<!-- 文本色彩 -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#212121"/>   <!-- 主文本 -->
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#757575"/> <!-- 次文本 -->
<SolidColorBrush x:Key="TextHintBrush" Color="#BDBDBD"/>      <!-- 提示文本 -->
```

#### 按钮样式体系

```xml
<!-- 基础按钮样式 -->
<Style x:Key="BaseButtonStyle" TargetType="Button">
    <Setter Property="Padding" Value="16,8"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="FontWeight" Value="Medium"/>
</Style>

<!-- 主要按钮 -->
<Style x:Key="PrimaryButton" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
</Style>

<!-- 次要按钮 -->
<Style x:Key="SecondaryButton" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource SecondaryBrush}"/>
    <Setter Property="Foreground" Value="White"/>
</Style>

<!-- 成功按钮 -->
<Style x:Key="SuccessButton" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource AccentBrush}"/>
    <Setter Property="Foreground" Value="White"/>
</Style>

<!-- 危险按钮 -->
<Style x:Key="DangerButton" TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
    <Setter Property="Background" Value="{StaticResource ErrorBrush}"/>
    <Setter Property="Foreground" Value="White"/>
</Style>
```

### 设计系统特点

- **Material Design风格**: 采用Google Material Design色彩规范
- **统一色彩语言**: 主色调、辅助色、强调色形成完整色彩体系
- **语义化命名**: 按钮样式以功能命名（Primary、Secondary、Success、Danger）
- **可扩展性**: 基础样式支持继承和重写

---

## 区域管理和导航

### 区域定义

- **LoginRegion**: 登录界面容器区域
- **ContentRegion**: 主应用内容区域
- **ToolbarRegion**: 工具栏区域（预留）
- **StatusRegion**: 状态栏区域（预留）

### 导航策略

1. **状态驱动导航**: 基于IsLoggedIn状态切换LoginRegion/ContentRegion
2. **角色驱动导航**: 根据用户角色导航到不同工作台界面
3. **模块化导航**: 各业务模块独立导航管理

### Prism导航参数

```csharp
// 导航参数传递示例
var parameters = new NavigationParameters
{
    { "PatientId", selectedPatient.Id },
    { "MedicalCaseId", medicalCase.Id }
};
regionManager.RequestNavigate("ContentRegion", "ConsultationView", parameters);
```

---

## 全局统计

### 项目统计

- **类数量**: 15个核心类
- **ViewModel数量**: 8个主要ViewModel
- **扩展方法**: 3个服务注册扩展
- **XAML资源**: 统一设计系统和主题

### 架构特点

- **Prism模块化**: 8个业务模块独立管理和按需加载
- **UltraThink设计**: 角色驱动UI、5层依赖注册、统一设计语言
- **现代化C#**: C# 12主构造函数、现代化语法特性
- **MVVM标准**: 完整的MVVM模式实现和数据绑定

### 性能优化

- **角色驱动加载**: 按用户角色动态加载功能模块
- **异步初始化**: 非关键服务异步预热，提升启动速度
- **内存管理**: 完整的资源清理和内存泄漏防护
- **并行加载**: 数据加载使用并行模式减少等待时间

### 用户体验

- **键盘快捷键**: Ctrl+N、Ctrl+Shift+C、F1等全局快捷键
- **实时时钟**: 状态栏实时显示系统时间
- **主题切换**: 支持明暗主题动态切换
- **角色适配**: 界面根据用户角色自动调整功能可见性

### 扩展性设计

- **模块化架构**: 新增业务模块只需实现IModule接口
- **区域管理**: 预留工具栏和状态栏区域支持功能扩展
- **服务注册**: 5层注册策略支持复杂依赖关系管理
- **主题系统**: 统一设计系统支持主题定制和品牌化