# LYBTZYZS 应用启动与登录流程（优化版）

**文档版本**: 2.2
**创建日期**: 2025-11-04
**最后更新**: 2025-11-04 - 新增连接模式切换（远程/本地）
**基于**: 实际代码分析 + UX优化方案
**用途**: 启动到工作台的完整流程设计

---

## 设计要点

### ✅ 已确认的设计决策
1. **取消自动登录** - 即使有有效Token，也必须显示登录界面
2. **记住凭据** - 支持"记住用户名"和"记住密码"（DPAPI加密），但仍需手动点击登录
3. **Token验证策略（方案C）** - 用户点击登录时，优先验证本地Token，失败后回退到密码登录
4. **全屏登录界面** - 中医主题背景图 + 右侧登录框
5. **API健康检查前置** - 在SplashScreen Phase 3执行，不在LoginViewModel
6. **错误降级处理** - API不可用时仍显示登录界面，提示警告，为v2.x本地模式铺路
7. **启动进度可视化** - SplashScreen显示进度条
8. **连接模式切换（v2.0新增）** - 登录框内RadioButton切换远程/本地模式，位置在登录按钮上方

### 🎯 优化目标
- ✅ 用户体验流畅（明确的进度反馈）
- ✅ 错误处理友好（API不可用不阻塞登录界面显示）
- ✅ 架构前瞻性（为v2.x本地模式预留扩展点）

---

## 完整流程图

```mermaid
flowchart TD
    Start([用户启动应用]) --> OnStartup[App.OnStartup<br/>应用启动入口]

    OnStartup --> ShowSplash[显示SplashScreen<br/>启动画面]
    ShowSplash --> UpdateStatus1[更新状态:<br/>正在初始化应用程序...]

    UpdateStatus1 --> CallBase[调用 base.OnStartup<br/>触发Prism生命周期]

    CallBase --> CreateShell[CreateShell<br/>创建MainWindow<br/>但不显示]
    CreateShell --> InitShell[InitializeShell<br/>初始化后立即Hide]
    InitShell --> OnInit[OnInitialized<br/>开始异步初始化]

    OnInit --> StartMonitor[启动性能监控<br/>StartupPerformanceMonitor]
    StartMonitor --> Phase1Start[Phase 1: 错误处理初始化]

    Phase1Start --> UpdateSplash1[SplashScreen更新<br/>进度: 10%<br/>正在初始化错误处理...]
    UpdateSplash1 --> InitError[初始化ErrorHandlingService]
    InitError --> Phase2Start[Phase 2: 模块协调器初始化]

    Phase2Start --> UpdateSplash2[SplashScreen更新<br/>进度: 40%<br/>正在初始化模块协调器...]
    UpdateSplash2 --> InitModules[初始化ModuleCoordinator]
    InitModules --> Phase3Start[Phase 3: 核心服务初始化]

    Phase3Start --> UpdateSplash3[SplashScreen更新<br/>进度: 70%<br/>正在初始化核心服务...]
    UpdateSplash3 --> LoadConfig[加载配置文件<br/>appsettings.json]
    LoadConfig --> ConfigStatus{配置检查}

    ConfigStatus -->|配置错误| ConfigErrorDialog[显示错误对话框<br/>配置文件缺失或错误]
    ConfigStatus -->|配置正常| CheckAPI[⭐检查API连接<br/>健康检查前置]

    ConfigErrorDialog --> ExitApp([退出应用])

    CheckAPI --> UpdateSplash3b[SplashScreen更新<br/>进度: 75%<br/>正在检查API连接...]
    UpdateSplash3b --> APITest[API: GET /api/v1/health<br/>健康检查端点]

    APITest --> APIStatus{API响应}
    APIStatus -->|连接成功| APIHealthy[标记: API可用]
    APIStatus -->|连接失败| APIUnhealthy[标记: API不可用]

    APIHealthy --> InitOtherServices[初始化其他核心服务<br/>进度: 80%]
    APIUnhealthy --> InitOtherServices

    InitOtherServices --> Phase4Start[Phase 4: 应用预热]

    Phase4Start --> UpdateSplash4[SplashScreen更新<br/>进度: 90%<br/>正在预热应用...]
    UpdateSplash4 --> Warmup[应用预热<br/>加载常用模块]
    Warmup --> Complete[初始化完成<br/>进度: 100%]

    Complete --> CloseSplash[关闭SplashScreen]
    CloseSplash --> ShowMainWindow[显示MainWindow<br/>全屏中医背景]

    ShowMainWindow --> ShowLoginUI[⭐显示登录界面<br/>右侧登录框]
    ShowLoginUI --> LoginRegion[NavigationManager.ShowLoginDialog<br/>导航到LoginRegion]

    LoginRegion --> LoginViewModel[LoginViewModel构造]
    LoginViewModel --> LoadCredentials[加载保存的凭据<br/>记住用户名+记住密码]

    LoadCredentials --> CheckSaved{有保存的凭据?}
    CheckSaved -->|有| AutoFill[自动填充:<br/>Username + Password]
    CheckSaved -->|无| EmptyForm[空白表单]

    AutoFill --> DisplayLogin[显示登录界面]
    EmptyForm --> DisplayLogin

    DisplayLogin --> ShowAPIStatus[显示API状态<br/>底部状态栏]
    ShowAPIStatus --> APIStatusDisplay{API状态}

    APIStatusDisplay -->|API可用| EnableRemote[✅ 远程登录可用<br/>显示绿色状态]
    APIStatusDisplay -->|API不可用| DisableRemote[⚠️ 显示警告<br/>API连接失败<br/>远程登录不可用]

    EnableRemote --> WaitUser[等待用户操作]
    DisableRemote --> ShowLocalHint[提示:<br/>本地模式开发中<br/>v2.0将支持]
    ShowLocalHint --> WaitUser

    WaitUser --> UserAction{用户操作}
    UserAction -->|输入/修改凭据| UpdateForm[更新表单]
    UserAction -->|点击登录按钮| ValidateForm{表单验证}

    UpdateForm --> WaitUser

    ValidateForm -->|验证失败| ShowValidationError[显示验证错误<br/>用户名或密码为空]
    ValidateForm -->|验证成功| CheckAPIBeforeLogin{检查API状态}

    ShowValidationError --> WaitUser

    CheckAPIBeforeLogin -->|API不可用| ShowAPIError[显示错误:<br/>API服务器不可用<br/>无法登录]
    CheckAPIBeforeLogin -->|API可用| CheckLocalToken{检查本地Token}

    ShowAPIError --> WaitUser

    CheckLocalToken -->|无Token| CallPasswordLogin[调用密码登录API]
    CheckLocalToken -->|有Token| ValidateToken[⭐ 验证Token<br/>API: POST /api/v1/auth/validate]

    ValidateToken --> ValidateResponse{Token验证响应}
    ValidateResponse -->|200 OK Token有效| ReceiveUserInfo[获取用户信息<br/>直接登录成功]
    ValidateResponse -->|401/403 Token无效| CallPasswordLogin
    ValidateResponse -->|网络错误| ShowNetworkError[显示错误:<br/>网络连接失败]

    CallPasswordLogin --> PasswordLoginAPI[API: POST /api/v1/auth/login<br/>Body: {username, password}]
    PasswordLoginAPI --> PasswordLoginResponse{密码登录响应}

    PasswordLoginResponse -->|401 Unauthorized| ShowLoginError[显示错误:<br/>账号或密码错误]
    PasswordLoginResponse -->|网络错误| ShowNetworkError
    PasswordLoginResponse -->|200 OK| ReceiveResponse[接收LoginResponse]

    ShowLoginError --> ClearPassword[清空密码字段]
    ShowNetworkError --> ClearPassword
    ClearPassword --> WaitUser

    ReceiveUserInfo --> ParseUserInfo[解析用户数据<br/>User含Role]
    ReceiveResponse --> ParseResponse[解析响应数据<br/>Token + RefreshToken<br/>User含Role]

    ParseUserInfo --> PublishEvent[发布LoginSuccessEvent]
    ParseResponse --> SaveToken[保存Token到本地<br/>ITokenStorageService]
    SaveToken --> CheckRemember{勾选了记住密码?}

    CheckRemember -->|是| SaveCredentials[保存凭据<br/>DPAPI加密<br/>Username + Password]
    CheckRemember -->|否| CheckRememberUser{勾选了记住用户名?}

    SaveCredentials --> PublishEvent[发布LoginSuccessEvent]

    CheckRememberUser -->|是| SaveUsername[仅保存Username]
    CheckRememberUser -->|否| ClearSaved[清除已保存凭据]

    SaveUsername --> PublishEvent
    ClearSaved --> PublishEvent

    PublishEvent --> EventReceived[MainWindowViewModel<br/>监听到LoginSuccessEvent]

    EventReceived --> SetLoggedIn[设置状态:<br/>IsLoggedIn = true<br/>CurrentUser = user]

    SetLoggedIn --> LoadWorkstation[EnsureWorkstationModulesLoaded<br/>根据角色加载模块]

    LoadWorkstation --> RoleCheck{用户角色}
    RoleCheck -->|Admin| LoadAdminModules[加载管理员模块<br/>AdminModule]
    RoleCheck -->|Doctor| LoadClinicalModules[加载医生模块<br/>ClinicalModule]

    LoadAdminModules --> LoadMainContent[LoadMainContent<br/>加载主界面内容]
    LoadClinicalModules --> LoadMainContent

    LoadMainContent --> UpdateTitle[更新窗口标题<br/>显示用户名和角色]
    UpdateTitle --> ClearLoginRegion[清除LoginRegion<br/>隐藏登录界面]

    ClearLoginRegion --> NavigateRole[RoleNavigationService<br/>NavigateToRoleHome]

    NavigateRole --> RoleRouting{角色路由}
    RoleRouting -->|Admin| ShowAdmin[显示AdminWorkstation<br/>管理员工作台]
    RoleRouting -->|Doctor| ShowClinical[显示ClinicalWorkstation<br/>临床工作台/医生主页]

    ShowAdmin --> WorkstationReady([工作台就绪])
    ShowClinical --> WorkstationReady

    style Start fill:#e1f5e1
    style ExitApp fill:#ffe1e1
    style CheckAPI fill:#fff4e1
    style APITest fill:#fff4e1
    style ValidateToken fill:#fff4e1
    style PasswordLoginAPI fill:#fff4e1
    style DisableRemote fill:#ffcccc
    style ShowLocalHint fill:#ffffcc
    style ShowLoginUI fill:#e5f5ff
    style DisplayLogin fill:#e5f5ff
    style WorkstationReady fill:#e1f5e1
```

---

## 关键流程说明

### 1. 应用启动（4个Phase）

```
Phase 1 (10%):  错误处理初始化
Phase 2 (40%):  模块协调器初始化
Phase 3 (70%):  核心服务初始化 + ⭐API健康检查（新增）
Phase 4 (90%):  应用预热
完成 (100%):    显示MainWindow
```

**优化点**：API健康检查从LoginViewModel前置到Phase 3，提前发现连接问题。

### 2. 登录界面（全屏中医背景 + 模式切换）

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│                                       ┌──────────────┐  │
│   中医主题背景图                        │  登录框      │  │
│   (竹子/山水/古典)                      │              │  │
│   全屏显示                              │  用户名      │  │
│                                       │  密码        │  │
│                                       │  □记住用户名  │  │
│                                       │  □记住密码    │  │
│                                       │              │  │
│                                       │  ⚙️ 连接模式：│  │
│                                       │  ◉ 远程模式  │  │
│                                       │  ○ 本地模式  │  │
│                                       │              │  │
│                                       │  [登录按钮]  │  │
│                                       └──────────────┘  │
│  状态栏: 🟢 远程API已连接 | 2025-11-04 14:30         │
└─────────────────────────────────────────────────────────┘
```

**v1.x当前版本**：
```
┌─────────────────────────────────────────────────────────┐
│                                       ┌──────────────┐  │
│   中医主题背景图                        │  登录框      │  │
│                                       │  用户名      │  │
│                                       │  密码        │  │
│                                       │  □记住用户名  │  │
│                                       │  □记住密码    │  │
│                                       │  [登录按钮]  │  │
│                                       └──────────────┘  │
│  状态栏: 🟢 远程API已连接                            │
└─────────────────────────────────────────────────────────┘
```

**v2.0版本（新增模式切换）**：
```
┌─────────────────────────────────────────────────────────┐
│                                       ┌──────────────┐  │
│   中医主题背景图                        │  登录框      │  │
│                                       │  用户名      │  │
│                                       │  密码        │  │
│                                       │  □记住用户名  │  │
│                                       │  □记住密码    │  │
│                                       │  ─────────── │  │
│                                       │  ⚙️ 连接模式：│  │
│                                       │  ◉ 远程模式  │  │
│                                       │  ○ 本地模式  │  │
│                                       │  ─────────── │  │
│                                       │  [登录按钮]  │  │
│                                       └──────────────┘  │
│  状态栏: 🟢 当前模式：远程 | API已连接               │
└─────────────────────────────────────────────────────────┘
```

**设计要点**：
- 全屏中医主题背景（MainWindow背景）
- 右侧登录框（420x620，半透明或纯白）⚠️ 高度从580px增加到620px
- **连接模式RadioButton**（v2.0新增）：
  - 位置：登录按钮上方，记住密码下方
  - 分隔线：上下各一条分隔线，视觉独立
  - 默认：远程模式（根据配置或上次选择）
  - 交互：点击切换，实时更新状态栏
- 底部状态栏显示当前模式和连接状态

### 3. Token验证策略（方案C）⭐

**设计决策**：即使软件退出后Token仍有效（服务器控制ExpiresAt），用户点击登录时优先使用Token验证。

**完整流程**：
```
用户点击"登录"
  ↓
检查本地Token（%LOCALAPPDATA%\LYBT\Desktop\auth.json）
  ↓
【有Token】→ 调用 POST /api/v1/auth/validate
  ├─ Token有效(200 OK) → 获取用户信息 → 直接登录成功 ✅
  ├─ Token无效(401/403) → 回退到密码登录 🔄
  └─ 网络错误 → 显示错误提示 ❌

【无Token】→ 直接调用 POST /api/v1/auth/login（密码登录）
```

**优势**：
- ✅ 安全性：每次登录都验证Token有效性（防止Token被撤销）
- ✅ 用户体验：Token有效时无需输入密码
- ✅ 优雅降级：Token失效时自动回退密码登录

**Token存储位置**：`C:\Users\<username>\AppData\Local\LYBT\Desktop\auth.json`

**Token内容**：
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "RefreshToken": "abcd1234...",
  "User": {
    "UserName": "admin",
    "Role": "Admin",
    "RealName": "管理员"
  },
  "ExpiresAt": "2025-11-04T15:30:00Z"
}
```

### 4. 记住凭据但不自动登录

**流程**：
```
启动 → 加载凭据 → 自动填充表单 → 等待用户点击登录
```

**实现**：
- ✅ 记住用户名：保存到本地（明文或Base64）
- ✅ 记住密码：使用DPAPI加密保存
- ❌ 不自动调用LoginAPI，必须用户点击"登录"按钮

### 5. API不可用时的降级处理

**当前（v1.x）**：
```
API不可用 → 显示登录界面 + 警告提示
→ 登录按钮禁用
→ 提示："本地模式开发中，敬请期待v2.0"
```

**未来（v2.x）**：
```
API不可用 → 显示登录界面 + 警告提示
→ 提供"切换到本地模式"按钮
→ 本地模式：使用本地数据库，无需API
```

### 6. 角色路由

**基于事件驱动**：
```
LoginViewModel.ExecuteLoginAsync()
  ↓ 登录成功
发布 LoginSuccessEvent(UserDto)
  ↓
MainWindowViewModel.OnLoginSuccess(UserDto)
  ↓ 提取User.Role
LoadMainContent() → RoleNavigationService
  ↓
NavigateToRoleHome(role)
  ├─ Admin → AdminWorkstation
  └─ Doctor → ClinicalWorkstation
```

---

## 技术实现要点

### 代码层面的关键修改

#### 1. API健康检查前置（Phase 3）
```csharp
// App.xaml.cs - InitializePhase3_CoreServicesAsync()
private async Task InitializePhase3_CoreServicesAsync()
{
    _splashScreen?.UpdateStatus("正在初始化核心服务...");
    _splashScreen?.UpdateProgress(70);

    await _bootstrapper.InitializeCoreServicesAsync();

    // ⭐ 新增：前置API健康检查
    _splashScreen?.UpdateStatus("正在检查API连接...");
    _splashScreen?.UpdateProgress(75);

    var apiHealthCheck = Container.Resolve<IApiHealthCheckService>();
    var apiStatus = await apiHealthCheck.CheckHealthAsync();

    // 保存API状态到全局服务
    var appState = Container.Resolve<IApplicationStateService>();
    appState.ApiStatus = apiStatus;

    _splashScreen?.UpdateProgress(80);
}
```

#### 2. MainWindow全屏登录背景
```xaml
<!-- MainWindow.xaml - 未登录状态 -->
<Grid Grid.Row="0"
      Visibility="{Binding IsNotLoggedIn, Converter={...}}">

    <!-- ⭐ 新增：全屏中医背景图 -->
    <Grid.Background>
        <ImageBrush ImageSource="/Assets/Images/login-background.jpg"
                    Stretch="UniformToFill" />
    </Grid.Background>

    <!-- ⭐ 修改：登录框移到右侧 -->
    <Border Background="White"
            CornerRadius="8"
            Width="420" Height="580"
            HorizontalAlignment="Right"
            VerticalAlignment="Center"
            Margin="0,0,100,0">

        <ContentControl prism:RegionManager.RegionName="LoginRegion" />
    </Border>
</Grid>
```

#### 3. 取消自动登录逻辑
```csharp
// MainWindowViewModel.cs - CheckLoginStatusAsync()
private async Task CheckLoginStatusAsync()
{
    // ❌ 删除：自动Token验证逻辑
    // var token = await _tokenStorage.GetTokenAsync();
    // if (!string.IsNullOrEmpty(token)) { ... }

    // ✅ 新增：始终显示登录界面
    _navigationManager.ShowLoginDialog();
}
```

#### 4. LoginViewModel不再检查API
```csharp
// LoginViewModel.cs - 构造函数
public LoginViewModel(...)
{
    // ❌ 删除：构造时的API健康检查
    // _ = Task.Run(async () => await CheckApiHealthAsync());

    // ✅ 新增：从全局状态读取API状态
    var appState = Container.Resolve<IApplicationStateService>();
    ApiStatus = appState.ApiStatus;

    // 加载保存的凭据（自动填充，但不登录）
    _ = Task.Run(async () => await LoadSavedCredentialsAsync());
}
```

#### 5. Token验证策略（方案C）⭐
```csharp
// LoginViewModel.cs - ExecuteLoginAsync()
private async Task ExecuteLoginAsync()
{
    // Step 1: 检查本地是否有Token
    var localToken = await _tokenStorage.GetTokenAsync();

    if (!string.IsNullOrEmpty(localToken))
    {
        // Step 2: 有Token，先验证Token
        var validateResult = await _authService.ValidateTokenAsync(localToken);

        if (validateResult.IsSuccess)
        {
            // Token有效，直接登录成功
            var user = validateResult.Data.User;
            EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);
            return;
        }

        // Token无效，继续执行密码登录（回退策略）
    }

    // Step 3: 无Token或Token无效，使用密码登录
    var loginRequest = new LoginRequest
    {
        UserName = Username,
        Password = Password
    };

    var response = await _authService.LoginAsync(loginRequest);

    if (response.IsSuccess)
    {
        // 保存Token
        await _tokenStorage.SaveAuthenticationAsync(response.Data, RememberMe);

        // 保存凭据
        if (RememberPassword)
        {
            await _credentialStorage.SaveCredentialsAsync(Username, Password, true);
        }

        // 发布事件
        EventAggregator.GetEvent<LoginSuccessEvent>().Publish(response.Data.User);
    }
}
```

```csharp
// IAuthService.cs - 新增接口
public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);

    // ⭐ 新增：Token验证接口
    Task<ApiResponse<ValidateTokenResponse>> ValidateTokenAsync(string token);
}

// ValidateTokenResponse.cs - 新增DTO
public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public UserDto User { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

---

## 连接模式切换设计（v2.0新增）

### UI位置与交互

**LoginView.xaml结构**：
```xaml
<StackPanel>
    <!-- 用户名 -->
    <TextBox Text="{Binding Username}" />

    <!-- 密码 -->
    <PasswordBox ... />

    <!-- 记住凭据 -->
    <CheckBox Content="记住用户名" IsChecked="{Binding RememberUsername}" />
    <CheckBox Content="记住密码" IsChecked="{Binding RememberPassword}" />

    <!-- ⭐ 分隔线 -->
    <Separator Margin="0,15,0,10" />

    <!-- ⭐ 连接模式选择 -->
    <TextBlock Text="⚙️ 连接模式：" FontSize="12" Foreground="#666" />
    <RadioButton Content="远程模式"
                 IsChecked="{Binding IsRemoteModeSelected}"
                 Margin="0,5,0,5" />
    <RadioButton Content="本地模式"
                 IsChecked="{Binding IsLocalModeSelected}"
                 Margin="0,0,0,5" />

    <!-- ⭐ 分隔线 -->
    <Separator Margin="0,10,0,15" />

    <!-- 登录按钮 -->
    <Button Content="登录" Command="{Binding LoginCommand}" />
</StackPanel>
```

### ViewModel实现

```csharp
// LoginViewModel.cs
public enum ConnectionMode
{
    Remote,  // 远程API模式
    Local    // 本地数据库模式
}

private ConnectionMode _connectionMode = ConnectionMode.Remote;
public ConnectionMode ConnectionMode
{
    get => _connectionMode;
    set
    {
        if (SetProperty(ref _connectionMode, value))
        {
            // 通知UI更新
            RaisePropertyChanged(nameof(IsRemoteModeSelected));
            RaisePropertyChanged(nameof(IsLocalModeSelected));
            RaisePropertyChanged(nameof(ConnectionModeDisplay));

            // 更新状态栏
            UpdateConnectionStatus();

            // 保存选择
            SaveConnectionMode(value);
        }
    }
}

public bool IsRemoteModeSelected
{
    get => ConnectionMode == ConnectionMode.Remote;
    set { if (value) ConnectionMode = ConnectionMode.Remote; }
}

public bool IsLocalModeSelected
{
    get => ConnectionMode == ConnectionMode.Local;
    set { if (value) ConnectionMode = ConnectionMode.Local; }
}

public string ConnectionModeDisplay =>
    ConnectionMode == ConnectionMode.Remote ? "远程模式" : "本地模式";

private async Task ExecuteLoginAsync()
{
    if (ConnectionMode == ConnectionMode.Remote)
    {
        // 远程API登录（Token验证 + 密码登录）
        await LoginWithRemoteApiAsync();
    }
    else
    {
        // 本地数据库登录
        await LoginWithLocalDatabaseAsync();
    }
}
```

### 配置持久化

**appsettings.json** 或 **%LOCALAPPDATA%\LYBT\Desktop\connection-settings.json**：
```json
{
  "ConnectionSettings": {
    "DefaultMode": "Remote",
    "RememberLastChoice": true,
    "RemoteApi": {
      "BaseUrl": "https://api.lybtzyzs.com",
      "Timeout": 30
    },
    "LocalDatabase": {
      "Path": "C:\\LYBT\\Database\\lybt.db",
      "Provider": "SQLite"
    }
  }
}
```

### 状态栏显示

**远程模式**：
```
🟢 当前模式：远程 | API: api.lybtzyzs.com | 已连接
```

**本地模式**：
```
🔵 当前模式：本地 | DB: C:\LYBT\lybt.db | 已连接
```

**API不可用（智能提示）**：
```
🔴 远程API连接失败 | 建议切换到本地模式
```

---

## 待实现清单

### Phase 1: 核心流程优化
- [ ] API健康检查前置到App.InitializePhase3
- [ ] 创建IApplicationStateService保存全局状态
- [ ] MainWindowViewModel.CheckLoginStatusAsync改为始终显示登录
- [ ] LoginViewModel移除构造时的API检查

### Phase 2: Token验证策略（方案C）⭐
**Server端（API）**：
- [ ] 创建ValidateTokenResponse DTO（IsValid, User, ExpiresAt）
- [ ] 实现POST /api/v1/auth/validate端点
  - 验证Token签名和过期时间
  - 返回用户信息（含Role）
  - 处理无效Token（401/403）

**Client端（Desktop）**：
- [ ] IAuthService新增ValidateTokenAsync方法
- [ ] AuthService实现ValidateTokenAsync（调用API）
- [ ] LoginViewModel.ExecuteLoginAsync重构：
  1. 检查本地Token
  2. Token存在 → 先验证
  3. 验证成功 → 直接登录
  4. 验证失败或无Token → 密码登录

### Phase 2.5: 连接模式切换（v2.0）⭐
**UI层**：
- [ ] LoginView.xaml添加连接模式RadioButton
  - 位置：登录按钮上方
  - 分隔线：上下各一条
  - 登录框高度：580px → 620px
- [ ] 状态栏显示当前模式和连接状态

**ViewModel层**：
- [ ] LoginViewModel新增ConnectionMode枚举和属性
- [ ] 新增IsRemoteModeSelected和IsLocalModeSelected属性
- [ ] 新增ConnectionModeDisplay属性（状态栏）
- [ ] 保存和加载用户选择（配置文件）

**Service层**：
- [ ] 创建IConnectionSettingsService接口
- [ ] 实现连接配置的保存和加载
- [ ] ExecuteLoginAsync根据模式分流：
  - Remote → LoginWithRemoteApiAsync
  - Local → LoginWithLocalDatabaseAsync

**本地模式基础**：
- [ ] 创建ILocalAuthService接口
- [ ] 实现LocalAuthService（SQLite用户认证）
- [ ] 创建ILocalDatabaseService接口
- [ ] 实现本地数据库初始化和连接

### Phase 3: UI改造
- [ ] MainWindow添加全屏中医背景图
- [ ] 登录框移到右侧（Margin="0,0,100,0"）
- [ ] SplashScreen添加进度条显示
- [ ] 登录框样式优化（半透明/纯白可选）

### Phase 4: 智能切换提示（v2.1）
- [ ] API不可用时自动提示切换本地模式
- [ ] 系统设置添加"连接设置"页面
- [ ] 本地数据库初始化向导
- [ ] 远程↔本地数据同步（后期）

---

## 用户体验改进总结

### 优化前的问题
1. ❌ API检查太晚，用户看到登录界面才发现API不可用
2. ❌ 自动登录缺少反馈，用户不知道发生了什么
3. ❌ 登录界面单调（420x580白框居中）
4. ❌ 初始化失败直接退出，无补救机会

### 优化后的体验
1. ✅ API检查前置，SplashScreen阶段就知道连接状态
2. ✅ 取消自动登录，明确的用户操作流程
3. ✅ 全屏中医背景+右侧登录框，视觉美观
4. ✅ API不可用时仍显示登录界面，提示本地模式（v2.x）
5. ✅ 进度条可视化，用户了解启动进度

---

**文档状态**: 设计完成（含Token验证策略），待实施
**下一步**:
1. 实施Token验证策略（Phase 2）
2. UI改造（Phase 3）
3. 讨论角色工作台主页设计
