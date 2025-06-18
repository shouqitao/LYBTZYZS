# WPF 应用程序开发指南 (基于 Prism MVVM 模式)

## 1. 项目概述
本项目是一个基于 WPF 的桌面应用程序，采用 Prism 框架实现 MVVM（Model-View-ViewModel）模式，旨在提供诊所管理系统的客户端界面。后端 API 通过 Refit 进行通信。

核心设计理念：

- **单一窗口应用**：应用程序启动后只显示一个主窗口 (ShellView)，所有功能模块的内容都动态加载到此窗口内部。
- **模块化**：使用 Prism Modules 实现功能模块的解耦和独立开发。
- **职责分离 (MVVM)**：严格遵循 MVVM 模式，将 UI (View)、UI 逻辑 (ViewModel) 和业务数据 (Model) 分离。
- **可扩展性**：通过 Prism 的区域（Regions）和事件聚合器（EventAggregator），方便地添加新功能和组件。

## 2. 项目结构
`LYBT.UI.WPF` 项目是 WPF 客户端的主项目。其关键文件夹和文件如下：

```
LYBT.UI.WPF/
├── App.xaml                      # 应用程序的入口点和全局资源定义
├── App.xaml.cs                   # 应用程序启动逻辑，Prism 配置，依赖注入注册
├── Views/                        # 包含所有 WPF 界面文件 (.xaml 和 .xaml.cs)
│   ├── AdminView.xaml/cs         # 管理员功能视图
│   ├── DiagnosingDoctorView.xaml/cs # 看诊医生视图
│   ├── LoginView.xaml/cs         # 登录界面视图 (作为 ShellView 的初始内容)
│   ├── PharmacyStaffView.xaml/cs # 药房人员视图
│   ├── RegistrationStaffView.xaml/cs # 挂号人员视图
│   ├── ShellView.xaml/cs         # 主应用程序窗口 (宿主所有其他视图)
│   └── TreatmentDoctorView.xaml/cs # 诊疗室医生视图
├── ViewModels/                   # 包含所有视图模型 (.cs 文件)
│   ├── LoginViewModel.cs         # 登录界面的数据和逻辑
│   ├── NavigationItem.cs         # 导航菜单项的模型
│   └── ShellViewModel.cs         # 主窗口的整体逻辑，导航控制
├── Services/                     # 客户端与后端 API 交互的服务层
│   ├── AuthHttpMessageHandler.cs # 用于在 HTTP 请求中附加认证 Token
│   ├── IAuthApi.cs               # Refit 定义的认证 API 接口
│   └── TokenService.cs           # 管理用户 Token 和信息的服务
├── Converters/                   # UI 绑定中使用的辅助转换器
│   └── NullToCollapsedConverter.cs # 将 null 值转换为 Visibility.Collapsed
└── AssemblyInfo.cs               # 程序集信息
```

其他依赖项目还包括 `LYBT.Common`、`LYBT.Module.*` 等模块，提供枚举、模型和业务逻辑。

## 3. 开发流程与模式
### 3.1 应用程序启动
- `App.xaml` 定义全局资源与主题。
- `App.xaml.cs` 作为入口点，重写 `CreateShell()` 创建并返回 `ShellView` 实例，在 `RegisterTypes()` 中注册视图和服务。

### 3.2 登录流程 (嵌入 ShellView)
1. **ShellView 显示**
   - 应用启动后立即显示 `ShellView`，其 `MainRegion` 用来宿主所有视图。
2. **初始导航到 LoginView**
   - `ShellViewModel` 检查 `TokenService` 是否已有登录信息，如无则导航到 `LoginView`。
3. **用户登录操作**
   - 用户在 `LoginView` 输入凭据，`LoginViewModel` 调用 `IAuthApi` 进行认证。
4. **登录成功后的界面切换**
   - `LoginSuccessEvent` 发布后，`ShellViewModel` 更新 `CurrentUser`，构建导航菜单并切换到相应视图。

### 3.3 导航与内容切换
- 左侧导航 `ListBox` 绑定 `NavigationItems`，点击项后通过 `MapsCommand` 调用 `RequestNavigate`。
- 各功能视图通过 `ViewModelLocator.AutoWireViewModel` 自动绑定对应 ViewModel。

### 3.4 登出流程
- 用户点击“退出”按钮触发 `LogoutCommand`，清除 `TokenService` 中的数据并重新导航回 `LoginView`。

## 4. 关键组件与最佳实践
- **MVVM**：View 层保持纯粹，业务逻辑放在 ViewModel。
- **Prism 框架**：利用 DI、Regions、EventAggregator 等实现松耦合。
- **服务层**：`TokenService` 管理登录状态，`AuthHttpMessageHandler` 自动附加 Bearer Token。
- **DTO**：存放于各 `LYBT.Module.*.Dtos` 目录，仅包含数据属性。
- **错误处理**：捕获 `ApiException` 与 `HttpRequestException`，并在导航回调中处理失败场景。

## 5. 后续开发与扩展
- **新模块**：创建新的 View 与 ViewModel，并在 `RegisterTypes`/`BuildNavigation` 中注册。
- **持久化登录信息**：在 `TokenService` 中加入本地存储机制，支持“记住我”。
- **权限管理**：根据用户角色动态生成菜单，同时确保后端也进行授权检查。
- **加载指示器**：对于耗时任务，在 UI 上通过 `IsLoading` 属性绑定显示等待动画。

