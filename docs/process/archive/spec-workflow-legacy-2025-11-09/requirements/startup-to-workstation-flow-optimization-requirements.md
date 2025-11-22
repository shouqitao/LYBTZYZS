# 启动到工作台流程端到端重构优化需求文档

**文档版本**：v1.0
**创建时间**：2025-11-04
**需求来源**：端到端流程分析与设计（docs/explanation/workflows/00-complete-startup-to-workstation-flow.md）
**对应Epic**：待创建
**优先级**：高（用户体验核心流程优化）

---

## 📋 需求概述

### 业务背景

当前系统从应用启动到用户进入工作台主页的流程存在多个体验痛点：
1. **启动流程冗余**：API健康检查在LoginViewModel执行，导致登录界面延迟显示
2. **Token验证缺失**：未实现Token自动验证，每次启动都需要手动登录
3. **连接模式固化**：无法手动切换远程API模式和本地数据库模式（v2.0需求）
4. **登录界面单调**：缺少品牌元素，登录框布局中规中矩
5. **医生主页功能不突出**：核心功能「开始接诊」按钮不明显，缺少快速导航

### 核心问题

| 问题类别 | 当前状态 | 用户影响 |
|---------|---------|---------|
| **启动性能** | API健康检查在登录页执行 | 登录界面延迟显示2-5秒 |
| **登录体验** | 无Token自动验证 | 每次启动都需手动输入密码 |
| **连接管理** | 无模式切换入口 | v2.0本地模式无法主动切换 |
| **UI美观度** | 登录界面无背景，居中布局 | 缺少品牌识别度 |
| **功能导航** | 医生主页6个按钮平铺 | 核心功能「开始接诊」不突出 |

### 业务目标

- **提升启动速度**：API健康检查前置，登录界面立即显示，减少等待感知
- **改善登录体验**：Token自动验证，优先无感登录，失败自动降级密码登录
- **支持双模式架构**：提供远程/本地模式切换入口，为v2.0本地数据库模式做准备
- **强化品牌形象**：全屏中医背景 + 右侧登录框，提升专业性和美观度
- **突出核心功能**：医生主页卡片式布局，500x220px主卡片强调「开始接诊」

---

## 🎯 功能需求

### FR-1: API健康检查前置

**需求描述**：
将API健康检查从LoginViewModel移至应用启动Phase 3，避免登录界面显示延迟。

**用户故事**：
作为用户，我希望应用启动后立即看到登录界面，而不是等待API检查完成，减少等待焦虑感。

**验收标准**：
- [ ] 创建 `IApplicationStateService` 接口（IsApiHealthy, ApiBaseUrl, ConnectionStatus属性）
- [ ] `MainWindowViewModel.InitializeAsync()` Phase 3执行API健康检查
- [ ] `LoginViewModel` 不再执行API健康检查，从 `IApplicationStateService` 读取状态
- [ ] 登录界面启动显示时间 < 500ms（从SplashScreen完成到LoginView显示）
- [ ] API不可用时，登录界面顶部显示「远程API连接失败」警告横幅
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：启动速度明显提升，API失败时警告正常显示

**优先级**：🔴 P0（最高）

**相关模块**：
- `LYBT.Desktop.Shell.Services.IApplicationStateService`（新建）
- `LYBT.Desktop.Shell.ViewModels.MainWindowViewModel`（修改）
- `LYBT.Desktop.Auth.ViewModels.LoginViewModel`（修改）

**技术约束**：
- API健康检查仍需执行，只是移至更早阶段
- 需确保Phase 3检查结果在LoginViewModel可访问

**依赖**：
- 无

---

### FR-2: Token验证策略实现（Plan C）

**需求描述**：
实现Token自动验证机制，优先验证已保存的AccessToken，验证失败或不存在时降级到密码登录。

**用户故事**：
作为医生用户，我希望应用启动后自动完成登录（如果Token有效），而不是每次都输入用户名密码，节省重复操作时间。

**验收标准**：

**Server端**：
- [ ] 创建 `ValidateTokenRequest` DTO（AccessToken: string）
- [ ] 创建 `ValidateTokenResponse` DTO（IsValid: bool, UserId: int?, Username: string?, Role: string?, ExpiresAt: DateTime?, ErrorMessage: string?）
- [ ] 实现 `POST /api/v1/auth/validate` 端点
- [ ] Token验证逻辑：检查签名、过期时间、用户存在性
- [ ] 单元测试覆盖：有效Token、过期Token、无效签名、用户已删除场景

**Client端**：
- [ ] `IAuthService` 新增 `ValidateTokenAsync(string accessToken)` 方法
- [ ] `LoginViewModel.OnNavigatedTo()` 实现Token验证流程：
  1. 检查 `_credentialService.HasSavedCredentials()`
  2. 调用 `_authService.ValidateTokenAsync(savedToken)`
  3. 验证成功 → 发布 `LoginSuccessEvent` → 自动导航
  4. 验证失败 → 清除无效Token → 显示登录表单
- [ ] 日志记录：Token验证成功/失败/跳过的原因
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：有效Token自动登录，无效Token降级密码登录

**优先级**：🔴 P0（最高）

**相关模块**：
- `LYBT.Server.Auth.Controllers.AuthController`（新增端点）
- `LYBT.Server.Auth.DTOs.ValidateTokenRequest/Response`（新建）
- `LYBT.Desktop.Auth.Services.IAuthService`（新增方法）
- `LYBT.Desktop.Auth.ViewModels.LoginViewModel`（修改）

**技术约束**：
- Token验证失败不能阻塞用户登录，必须自动降级
- 需处理Token过期、签名无效、用户已删除等所有异常场景

**依赖**：
- JWT配置（appsettings.json中的JWT:SecretKey）

---

### FR-3: 连接模式切换功能（v2.0）

**需求描述**：
在登录界面添加远程/本地模式切换RadioButton，为v2.0本地数据库模式提供UI入口。

**用户故事**：
作为用户，我希望在远程API不可用时能够主动切换到本地数据库模式，保证业务不中断。

**验收标准**：

**UI层**：
- [ ] `LoginView.xaml` 添加连接模式选择区域（登录按钮上方）：
  - 标题：「⚙️ 连接模式：」
  - RadioButton 1：「远程模式」（默认选中）
  - RadioButton 2：「本地模式」
  - 上下各一条Separator（视觉分隔）
- [ ] 登录框高度调整：420x580px → 420x620px（容纳新增控件）
- [ ] 状态栏显示当前模式：
  - 远程模式：🟢 当前模式：远程 | API: api.lybtzyzs.com | 已连接
  - 本地模式：🔵 当前模式：本地 | DB: C:\LYBT\lybt.db | 已连接
  - API失败：🔴 远程API连接失败 | 建议切换到本地模式

**ViewModel层**：
- [ ] 创建 `ConnectionMode` 枚举（Remote, Local）
- [ ] `LoginViewModel` 新增属性：
  - `ConnectionMode ConnectionMode`（默认Remote）
  - `bool IsRemoteModeSelected`（双向绑定RadioButton）
  - `bool IsLocalModeSelected`（双向绑定RadioButton）
  - `string ConnectionModeDisplay`（状态栏显示）
- [ ] 属性变更时触发：
  - 更新UI相关属性
  - 调用 `UpdateConnectionStatus()`
  - 调用 `SaveConnectionMode(value)`

**Service层**：
- [ ] 创建 `IConnectionSettingsService` 接口：
  - `ConnectionMode GetConnectionMode()`
  - `void SaveConnectionMode(ConnectionMode mode)`
  - `bool RememberLastChoice { get; set; }`
- [ ] 实现 `ConnectionSettingsService`：
  - 配置文件路径：`%LOCALAPPDATA%\LYBT\Desktop\connection-settings.json`
  - JSON结构：`{ "DefaultMode": "Remote", "RememberLastChoice": true }`
- [ ] `LoginViewModel.ExecuteLoginAsync()` 根据模式分流：
  - `Remote` → `LoginWithRemoteApiAsync()`
  - `Local` → `LoginWithLocalDatabaseAsync()`

**本地模式基础**（v2.0实现，此阶段仅预留接口）：
- [ ] 创建 `ILocalAuthService` 接口（本地用户认证）
- [ ] 创建 `ILocalDatabaseService` 接口（本地数据库初始化）
- [ ] `LoginWithLocalDatabaseAsync()` 临时实现：弹出「本地模式将在v2.0版本中提供」提示

**验收标准**：
- [ ] RadioButton切换流畅，状态栏实时更新
- [ ] 配置文件持久化，应用重启后记住上次选择
- [ ] 远程模式正常登录
- [ ] 本地模式点击登录弹出提示信息（v2.0开发中）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：UI正确显示，状态栏更新，配置保存成功

**优先级**：🟡 P1（中高）

**相关模块**：
- `LYBT.Desktop.Auth.Views.LoginView`（修改XAML）
- `LYBT.Desktop.Auth.ViewModels.LoginViewModel`（新增属性和逻辑）
- `LYBT.Desktop.Auth.Services.IConnectionSettingsService`（新建）
- `LYBT.Desktop.Auth.Services.ConnectionSettingsService`（新建）
- `LYBT.Desktop.Auth.Services.ILocalAuthService`（新建接口，v2.0实现）

**技术约束**：
- 本地模式真正实现需等待v2.0版本（SQLite数据库、数据同步机制）
- 此阶段仅搭建UI框架和配置系统

**依赖**：
- 无（独立功能）

---

### FR-4: 登录界面UI优化

**需求描述**：
全屏中医背景 + 右侧登录框布局，提升品牌专业性和美观度。

**用户故事**：
作为用户，我希望登录界面更具中医特色和专业感，而不是简单的白色背景和居中表单，提升软件品质感知。

**验收标准**：

**背景层**：
- [ ] 全屏中医特色背景图片（中药材、古典医书、阴阳五行元素）
- [ ] 半透明遮罩层（opacity: 0.3-0.5），确保登录框清晰可读
- [ ] 左侧品牌文字区域：
  - 系统名称：「LYBT 中医诊疗管理系统」（大字体，白色/金色）
  - Slogan：「传承经典 · 数字化诊疗」（副标题）
  - 版本号：v1.x.x.x（右下角）

**登录框**：
- [ ] 位置：右侧固定，距右边缘100px，垂直居中
- [ ] 尺寸：420x620px（高度增加以容纳连接模式切换）
- [ ] 样式：白色背景 + 圆角 + 阴影，半透明效果
- [ ] 内容布局：
  - 顶部：Logo + 系统名称
  - 用户名输入框
  - 密码输入框
  - 记住用户名/记住密码 CheckBox
  - **分隔线**
  - **连接模式RadioButton**（FR-3）
  - **分隔线**
  - 登录按钮
  - 底部：版本信息 + 帮助链接

**响应式适配**：
- [ ] 窗口宽度 < 1200px 时，登录框自动居中（优先保证可用性）
- [ ] 最小窗口尺寸：1024x768px

**验收标准**：
- [ ] 设计师确认背景图片和配色方案（或从免费素材库选择）
- [ ] 不同分辨率下（1920x1080, 1366x768, 1024x768）测试布局正常
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：界面美观，登录框清晰，品牌元素明显

**优先级**：🟢 P2（中）

**相关模块**：
- `LYBT.Desktop.Auth.Views.LoginView`（大幅修改XAML）
- 新增资源文件：`Assets/Images/login-background.jpg`

**技术约束**：
- 背景图片需考虑版权问题（使用免费素材或自行设计）
- 不能影响现有登录功能

**依赖**：
- FR-3（连接模式切换UI需集成在登录框中）

---

### FR-5: 医生工作台主页重构（卡片式布局）

**需求描述**：
重构医生主页为卡片式布局，突出核心功能「开始接诊」，提供快速导航辅助卡片。

**用户故事**：
作为医生用户，我希望进入工作台后立即看到醒目的「开始接诊」按钮，同时能快速访问患者管理、病历查询等常用功能，提升操作效率。

**验收标准**：

**布局结构**：
- [ ] 采用上下分层布局：
  - **上层**（500x220px 主卡片 + 240x220px 统计卡片）
  - **下层**（4×160x160px 辅助卡片）
- [ ] 卡片间距：20px
- [ ] 总布局宽度：~800px，垂直居中

**主卡片**（500x220px）：
- [ ] 背景：渐变色（蓝色系或中医特色色）
- [ ] 核心内容：
  - 大图标：医生+患者图标（80x80px）
  - 大字标题：「开始接诊」（36px加粗）
  - 副标题：「点击进入患者选择」（16px）
  - 点击命令：`StartConsultationCommand` → 导航到 `PatientSelectionView`
- [ ] Hover效果：轻微放大 + 阴影加深

**统计卡片**（240x220px）：
- [ ] 背景：浅色（白色/浅灰）
- [ ] 内容：
  - 标题：「今日统计」
  - 今日接诊数：`TodayConsultationCount`（大数字 + 单位）
  - 待完成病历：`PendingCaseCount`（大数字 + 单位）
  - 底部：「查看详情」链接（可选）
- [ ] 数据来源：`LoadTodayStatistics()`方法（Phase 1临时返回0，Phase 2实现真实统计）

**辅助卡片**（4×160x160px）：
- [ ] 卡片1：「患者管理」→ `NavigateToPatientManagementCommand`
- [ ] 卡片2：「病历查询」→ `NavigateToMedicalCaseQueryCommand`
- [ ] 卡片3：「药材库」→ `NavigateToHerbLibraryCommand`
- [ ] 卡片4：「验方库」→ `NavigateToFormulaLibraryCommand`
- [ ] 统一样式：图标 + 文字，Hover效果

**ViewModel实现**：
- [ ] `ClinicalHomeViewModel` 保留现有命令
- [ ] 新增统计属性：`TodayConsultationCount`, `PendingCaseCount`
- [ ] 新增导航命令：`NavigateToHerbLibraryCommand`, `NavigateToFormulaLibraryCommand`
- [ ] `OnNavigatedTo()` 刷新统计数据

**验收标准**：
- [ ] 主卡片醒目，点击正常导航到 `PatientSelectionView`
- [ ] 统计卡片显示今日数据（临时为0，真实统计Phase 2实现）
- [ ] 4个辅助卡片点击正常导航
- [ ] Hover效果流畅
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：布局美观，所有导航功能正常

**优先级**：🟢 P2（中）

**相关模块**：
- `LYBT.Desktop.Clinical.Views.ClinicalHomeView`（大幅修改XAML）
- `LYBT.Desktop.Clinical.ViewModels.ClinicalHomeViewModel`（新增属性和命令）

**技术约束**：
- 不能破坏现有导航流程（Issue #1567）
- 统计数据真实实现需等待Phase 2（Server端统计接口开发）

**依赖**：
- 无（独立功能）

---

### FR-6: 角色路由机制优化

**需求描述**：
将角色路由逻辑从LoginViewModel解耦，采用事件驱动机制，由MainWindowViewModel统一处理导航。

**用户故事**：
作为开发者，我希望登录成功后的导航逻辑不耦合在LoginViewModel中，而是通过事件发布由全局管理器统一处理，提升代码可维护性。

**验收标准**：
- [ ] 创建 `LoginSuccessEvent` 事件类（UserId, Username, Role, AccessToken, RefreshToken属性）
- [ ] `LoginViewModel` 登录成功后发布 `LoginSuccessEvent`，不再直接调用导航
- [ ] `MainWindowViewModel` 订阅 `LoginSuccessEvent`：
  - 根据 `Role` 字段分发：
    - `Role == "Admin"` → 导航到 `AdminHomeView`
    - `Role == "Doctor"` → 导航到 `ClinicalHomeView`
    - 其他角色 → 日志错误并显示提示
- [ ] 删除 `LoginViewModel` 中的角色判断和导航代码
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：Admin和Doctor用户登录后正确导航到各自主页

**优先级**：🟡 P1（中高）

**相关模块**：
- `LYBT.Desktop.Auth.Events.LoginSuccessEvent`（新建）
- `LYBT.Desktop.Auth.ViewModels.LoginViewModel`（修改）
- `LYBT.Desktop.Shell.ViewModels.MainWindowViewModel`（修改，新增事件订阅）

**技术约束**：
- 必须确保事件订阅在LoginViewModel初始化之前完成
- 需处理未知角色的异常情况

**依赖**：
- Prism EventAggregator机制

---

## 🔧 非功能需求

### NFR-1: 性能要求

| 指标 | 当前值 | 目标值 |
|-----|-------|-------|
| 应用启动到登录界面显示 | 2-5秒 | < 1秒 |
| Token验证耗时 | N/A | < 500ms |
| 登录成功到工作台显示 | ~1秒 | < 800ms |
| API健康检查超时 | 30秒 | 10秒 |

### NFR-2: 可用性要求

- **容错性**：Token验证失败必须自动降级到密码登录，不能阻塞用户
- **提示清晰**：API不可用时显示明确提示，并建议切换本地模式
- **状态透明**：用户始终能通过状态栏了解当前连接模式和状态

### NFR-3: 兼容性要求

- **分辨率支持**：1024x768 ~ 3840x2160（4K）
- **Windows版本**：Windows 10/11（.NET 8.0 Runtime要求）
- **向后兼容**：不破坏现有用户的已保存凭据和配置

### NFR-4: 安全性要求

- **Token存储**：使用DPAPI加密保存AccessToken和RefreshToken
- **密码存储**：使用DPAPI加密保存密码（如果勾选"记住密码"）
- **配置文件**：连接配置文件不包含敏感信息（仅存储模式选择和API URL）

### NFR-5: 可维护性要求

- **代码规范**：所有新增代码遵循项目编码规范（中文注释、PascalCase命名、依赖注入）
- **文档同步**：所有变更同步更新相关架构文档和开发指南
- **测试覆盖**：Server端Token验证接口单元测试覆盖率 ≥ 80%

---

## 🚀 实施计划

### Phase 1: 核心流程优化（2-3天）

**目标**：API健康检查前置 + Token验证策略实现 + 角色路由优化

**任务清单**：
1. **IApplicationStateService创建**（2小时）
   - 接口定义：IsApiHealthy, ApiBaseUrl, ConnectionStatus属性
   - 实现类：ApplicationStateService，注册为单例
2. **MainWindowViewModel改造**（3小时）
   - InitializeAsync() Phase 3执行API健康检查
   - 结果保存到IApplicationStateService
3. **LoginViewModel改造**（4小时）
   - 移除API健康检查代码
   - 从IApplicationStateService读取状态
   - API不可用时显示警告横幅
4. **Server端Token验证接口**（4小时）
   - ValidateTokenRequest/Response DTO
   - POST /api/v1/auth/validate 端点实现
   - 单元测试（4种场景）
5. **Client端Token验证逻辑**（5小时）
   - IAuthService.ValidateTokenAsync() 方法
   - LoginViewModel.OnNavigatedTo() 实现Plan C流程
   - 日志记录和异常处理
6. **角色路由解耦**（3小时）
   - LoginSuccessEvent事件类
   - LoginViewModel发布事件
   - MainWindowViewModel订阅并处理导航
7. **集成测试**（3小时）
   - 启动速度验证
   - Token自动登录验证
   - 角色路由验证

**里程碑**：Phase 1完成后，用户启动应用立即看到登录界面，有效Token自动登录成功

---

### Phase 2: 连接模式切换（1.5-2天）

**目标**：实现远程/本地模式切换UI和配置系统（本地模式真实功能v2.0实现）

**任务清单**：
1. **LoginView UI改造**（3小时）
   - 添加连接模式RadioButton区域
   - 登录框高度调整
   - 状态栏模式显示
2. **ConnectionMode基础架构**（4小时）
   - ConnectionMode枚举
   - LoginViewModel新增属性和逻辑
   - IConnectionSettingsService接口和实现
3. **配置持久化**（2小时）
   - JSON配置文件读写
   - 应用启动时加载上次选择
4. **本地模式接口预留**（2小时）
   - ILocalAuthService接口定义
   - LoginWithLocalDatabaseAsync()临时实现（弹出提示）
5. **集成测试**（2小时）
   - RadioButton切换验证
   - 配置持久化验证
   - 状态栏显示验证

**里程碑**：Phase 2完成后，用户可在登录界面切换远程/本地模式，配置持久化生效

---

### Phase 3: UI美化（1-2天）

**目标**：全屏中医背景 + 右侧登录框，提升品牌形象

**任务清单**：
1. **背景素材准备**（2小时）
   - 选择或设计中医特色背景图片
   - 确认版权和尺寸
2. **LoginView布局重构**（5小时）
   - 全屏背景 + 遮罩层
   - 左侧品牌文字区
   - 右侧登录框（420x620px）
3. **响应式适配**（3小时）
   - 不同分辨率测试
   - 窗口缩放处理
4. **集成测试**（2小时）
   - 视觉效果验证
   - 不同分辨率测试

**里程碑**：Phase 3完成后，登录界面具备专业品牌形象

---

### Phase 4: 医生主页重构（1.5-2天）

**目标**：卡片式布局，突出「开始接诊」核心功能

**任务清单**：
1. **ClinicalHomeView布局重构**（6小时）
   - 主卡片（500x220px）设计和实现
   - 统计卡片（240x220px）设计和实现
   - 4个辅助卡片（160x160px）设计和实现
   - Hover效果和动画
2. **ClinicalHomeViewModel改造**（3小时）
   - 新增统计属性
   - 新增辅助导航命令
   - OnNavigatedTo()刷新逻辑
3. **集成测试**（2小时）
   - 所有卡片导航功能验证
   - 统计数据显示验证（临时为0）
   - Hover效果验证

**里程碑**：Phase 4完成后，医生用户进入工作台看到醒目的卡片式主页

---

### Phase 5: 文档和验收（0.5天）

**任务清单**：
1. **文档更新**（2小时）
   - 更新架构文档（启动流程、登录流程）
   - 更新开发指南（新增服务和事件）
   - 更新快速参考（连接模式切换）
2. **最终验收**（2小时）
   - 完整流程端到端测试
   - 性能指标验证
   - 用户体验验证

**总预估工期**：6-9天（取决于并行开发和测试效率）

---

## 📊 验收标准

### 整体验收标准

#### 功能完整性
- [x] 所有6个功能需求（FR-1至FR-6）的验收标准全部通过
- [x] Admin和Doctor两种角色登录流程完整可用
- [x] 远程模式登录完整可用，本地模式UI完整（功能提示v2.0）

#### 性能指标
- [x] 应用启动到登录界面显示 < 1秒
- [x] Token验证耗时 < 500ms
- [x] 登录成功到工作台显示 < 800ms

#### 代码质量
- [x] 编译通过（0 errors, 0 warnings）
- [x] Server端Token验证接口单元测试覆盖率 ≥ 80%
- [x] 所有代码遵循项目编码规范

#### 用户体验
- [x] 启动速度明显提升（用户感知等待时间减少）
- [x] 有效Token自动登录成功（无需手动输入）
- [x] 登录界面美观专业（中医特色明显）
- [x] 医生主页「开始接诊」按钮醒目易找

#### 文档同步
- [x] 架构文档更新完成（启动流程、登录流程、工作台主页）
- [x] 开发指南更新完成（新增服务、事件、配置）
- [x] 快速参考更新完成（连接模式切换说明）

---

## ⚠️ 风险与依赖

### 高风险项

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **Token验证逻辑复杂** | 可能影响登录成功率 | 充分单元测试，保证降级机制可靠 |
| **UI改造影响现有功能** | 可能导致登录失败 | 增量改造，每个Phase独立验证 |
| **背景图片版权问题** | 可能需要替换素材 | 优先选择免费素材库，或使用纯色渐变 |

### 中风险项

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| **API健康检查前置可能遗漏场景** | 某些场景API状态未更新 | 充分测试API不可用场景 |
| **连接配置持久化失败** | 用户选择未保存 | 添加异常处理和日志记录 |
| **医生主页布局适配问题** | 某些分辨率下布局异常 | 多分辨率测试，添加响应式断点 |

### 依赖项

| 依赖 | 状态 | 备注 |
|-----|------|------|
| **JWT配置** | ✅ 已存在 | appsettings.json中JWT:SecretKey |
| **Prism EventAggregator** | ✅ 已集成 | 用于LoginSuccessEvent |
| **DPAPI加密** | ✅ Windows原生 | 用于凭据加密存储 |
| **中医背景素材** | ⚠️ 待准备 | Phase 3前需确认 |

---

## 📚 参考文档

### 设计文档
- [完整端到端流程设计](../workflows/00-complete-startup-to-workstation-flow.md)
- [启动与登录流程优化设计](../workflows/02-startup-login-optimized.md)
- [医生工作台主页设计](../workflows/03-clinical-home-dashboard.md)

### 架构文档
- [Client端MVVM架构](../architecture/client/README.md)
- [Server端三层架构](../architecture/server/README.md)

### 开发指南
- [Issue驱动工作流](../../../.claude/guides/issue-workflow.md)
- [编码规范](../../../.claude/reference/coding-standards.md)
- [测试指南](../../../.claude/guides/testing.md)

---

**文档状态**：✅ 已完成
**下一步**：创建GitHub Epic Issue，拆分子任务，开始Phase 1开发
