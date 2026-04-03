# 配置参数 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所管理系统包含多个子系统 (认证、用户管理、医案、处方等)，每个子系统都有大量可调参数 (超时时间、安全策略、连接池大小、功能开关等)。缺乏统一的配置管理意味着: 参数散落在代码中难以调整、开发与生产环境行为不一致、敏感信息 (数据库密码、JWT 密钥) 暴露在代码仓库中、生产环境因配置缺失导致启动失败。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 运维人员 | 修改参数需改代码重编译 | 每次调整消耗 30+ 分钟，且存在引入 bug 的风险 |
| 运维人员 | 不清楚哪些配置在生产环境必须设置 | 部署后启动失败，排查耗时 |
| 运维人员 | 敏感配置 (密钥/密码) 写在配置文件中 | 代码仓库泄露风险 |
| 开发者 | 开发/生产环境配置混淆 | 开发环境误用生产数据库，或生产环境暴露 Swagger |

### 1.3 证据

- ASP.NET Core Options 模式是 .NET 生态标准配置管理方案，支持强类型绑定、验证和分环境覆盖
- 系统已有 17 个 Options 类 (12 服务端 + 5 客户端)，参数总计 80+ 项
- 生产部署经验: 首次部署因连接字符串缺失导致启动失败

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| 运维人员 | 修改 appsettings.json / 环境变量，管理全部服务端和客户端配置 |
| SuperAdmin | 通过 DiagnosticsController 管理运行时日志级别 |
| 开发者 | 维护 Options 类定义和 DataAnnotation 验证规则 |

> 配置管理主要面向运维人员和开发者，业务用户 (医生/前台) 不直接接触。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 运维自主性 | 运维人员通过配置文件/环境变量调整系统行为，无需开发介入 |
| 环境安全隔离 | 开发/生产环境配置严格分离，防止交叉污染 |
| 部署可靠性 | 生产环境启动前验证关键配置，缺失则阻止启动并给出明确修复指引 |
| 功能渐进发布 | FeatureToggle 控制功能模块的 UI 可见性，支持分阶段上线 |

### 3.2 Why Now

系统从开发阶段进入部署阶段，配置管理从"开发者自用"升级为"运维人员操作"。必须在首次生产部署前建立完整的配置验证和环境隔离机制，否则每次部署都是一次冒险。

---

## 4. Solution Overview

配置模块采用 ASP.NET Core Options 模式，实现强类型、可验证、分环境的配置管理体系:

**核心能力:**
- **强类型绑定**: 14 个 Options 类 (8 服务端 + 1 共享 + 4 客户端 + 1 WebAPI 专用) 通过 `IServiceCollection.Configure<T>()` 绑定 appsettings.json 配置节
- **DataAnnotation 验证**: 所有 Options 类支持 `[Required]`、`[Range]`、`[MinLength]` 等验证注解，启动时自动校验
- **分环境覆盖**: appsettings.json -> appsettings.{Environment}.json -> 环境变量，三级优先级
- **生产启动验证**: ProductionConfigurationValidator 在 Production 环境启动时强制检查关键配置
- **功能开关**: FeatureToggles 控制 Desktop UI 功能可见性，支持分阶段功能发布

**配置加载流程:**
```
应用启动 → 加载 appsettings.json
         → 加载 appsettings.{Environment}.json (覆盖)
         → 加载环境变量 (最高优先级)
         → DataAnnotation 验证
         → [Production] ProductionConfigurationValidator 验证关键项
         → 验证通过 → 正常启动
         → 验证失败 → 输出错误详情 + Exit(1)
```

---

## 5. Success Metrics

| 指标 | 当前 (硬编码) | v1.0 目标 | 衡量方式 |
|------|-------------|----------|---------|
| 配置覆盖率 | 部分参数硬编码 | 100% 可配置化 (0 硬编码参数) | 代码审查 |
| 生产启动成功率 | 配置缺失导致运行时异常 | 100% 配置问题在启动时捕获 | 部署日志 |
| 配置变更耗时 | 30+ 分钟 (改代码+编译+部署) | < 5 分钟 (改配置+重启) | 运维操作记录 |
| 敏感信息泄露 | 密钥写在配置文件中 | 0 敏感信息在代码仓库 | 安全扫描 |

---

## 6. Epic Hypothesis

We believe that 实现强类型 Options 绑定 + DataAnnotation 验证 + 分环境覆盖 + 生产启动验证的配置管理体系 for 运维人员和开发者 will achieve 部署零配置遗漏、环境安全隔离、运维自主调参的目标。We'll know we're right when 生产环境启动成功率 100%、配置变更无需开发介入、且零敏感信息泄露事件。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-CFG-001 | 服务端配置参数 | Must |
| US-CFG-002 | 客户端配置参数 | Must |
| US-CFG-003 | 环境配置管理 | Should |
| US-CFG-004 | 生产环境启动配置验证 | Should |

---

### US-CFG-001: 服务端配置参数

> As a 运维人员, I want to 通过 appsettings.json 管理服务端全部配置参数,
> so that 我可以在不修改代码的情况下调整系统行为。

**Acceptance Criteria:**
- [ ] 所有必填参数未配置 -> 启动时抛出验证异常
- [ ] 参数超出 Range 范围 -> 启动时抛出验证异常
- [ ] 环境变量可覆盖 appsettings.json 中的值
- [ ] Logging 配置修改 -> 无需重启即生效
- [ ] JWT/Database 配置修改 -> 需重启才生效

**Business Rules:**
1. 所有 Options 类支持 DataAnnotation 验证 ([Required], [Range], [MinLength] 等)
2. 配置节名称通过 ConfigurationSections 常量统一管理
3. Options 绑定通过 IServiceCollection.Configure<T>() 注册
4. 敏感配置 (SecretKey, Password) 支持环境变量覆盖

#### 配置参数总表

**Jwt (JWT 认证)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| SecretKey | string | (必填) | MinLength(32) | JWT 签名密钥 (Base64) |
| Issuer | string | "LYBT.WebAPI" | Required | 令牌发行者 |
| Audience | string | "LYBT.Client" | Required | 令牌受众 |
| AccessTokenExpirationMinutes | int | 30 | 5-1440 | AccessToken 过期时间 (分钟) |
| RefreshTokenExpirationDays | int | 7 | 1-30 | RefreshToken 过期时间 (天) |
| ClockSkewSeconds | int | 300 | 0-600 | 时钟偏差容忍度 (秒) |

**Session (会话管理)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| TimeoutMinutes | int | 120 | 5-1440 | 会话超时 (分钟) |
| AllowConcurrentSessions | bool | false | - | 是否允许并发会话 |
| SlidingExpiration | bool | true | - | 滑动过期 |

**Security (安全/速率限制)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| RateLimiting.Enabled | bool | true | - | 是否启用速率限制 |
| RateLimiting.GlobalLimit.PermitLimit | int | 200 | 1-10000 | 全局请求限制 |
| RateLimiting.GlobalLimit.WindowSeconds | int | 60 | 1-3600 | 时间窗口 (秒) |
| RateLimiting.LoginLimit.PermitLimit | int | 5 | 1-10000 | 登录请求限制 |
| RateLimiting.LoginLimit.InternalPermitLimit | int | 20 | - | 内网登录限制 |
| RateLimiting.ApiLimit.PermitLimit | int | 100 | 1-10000 | API 请求限制 |
| RateLimiting.ApiLimit.AdminPermitLimit | int | 200 | - | 管理员 API 限制 |
| RateLimiting.WhitelistedIPs | List | [127.0.0.1, ::1] | - | 白名单 IP |

**PasswordPolicy (密码策略)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| MinLength | int | 8 | 6-32 | 最小长度 |
| RequireDigit | bool | true | - | 需要数字 |
| RequireLowercase | bool | true | - | 需要小写 |
| RequireUppercase | bool | true | - | 需要大写 |
| RequireSpecialChar | bool | true | - | 需要特殊字符 |

**DefaultPasswords (默认密码)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| SysAdminPassword | string | (必填) | MinLength(8) | 系统管理员默认密码 |
| NewUserPassword | string | (必填) | MinLength(8) | 新用户默认密码 |
| ForceChangeOnFirstLogin | bool | true | - | 首次登录强制改密 |
| EnableInDevelopment | bool | true | - | 开发环境启用 |
| AllowInProduction | bool | false | - | 生产环境允许 (应为 false) |
| OnlyWhenDatabaseEmpty | bool | true | - | 仅空库时使用 |
| ExpiryDays | int | 30 | 1-365 | 默认密码过期天数 |

**Database (数据库)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| ConnectionString | string? | null | - | 连接字符串 (可选，有 fallback 链) |
| AutoMigrate | bool | false | - | 自动迁移 |
| EnsureCreatedInDevelopment | bool | true | - | 开发环境自动建库 |
| MigrationTimeoutSeconds | int | 300 | 30-7200 | 迁移超时 (秒) |
| ConnectionPool.MaxConnections | int | 20 | 1-100 | 最大连接数 |
| ConnectionPool.MinConnections | int | 2 | 0-50 | 最小连接数 |
| ConnectionPool.ConnectionTimeoutSeconds | int | 30 | 5-120 | 连接超时 |
| ConnectionPool.CommandTimeoutSeconds | int | 30 | 5-300 | 命令超时 |
| Monitoring.Enabled | bool | true | - | 启用监控 |
| Monitoring.LogAllQueries | bool | false | - | 记录所有查询 |
| Monitoring.SlowQueryThresholdMs | int | 1000 | 100-60000 | 慢查询阈值 (毫秒) |
| RetryPolicy.MaxRetryCount | int | 3 | 0-10 | 最大重试次数 |
| RetryPolicy.BaseDelayMs | int | 1000 | 100-10000 | 基础延迟 (毫秒) |
| RetryPolicy.MaxDelayMs | int | 10000 | 1000-60000 | 最大延迟 (毫秒) |

**MemoryCache (缓存)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| Enabled | bool | true | - | 是否启用缓存 |
| SizeLimit | long | 104857600 | 1MB-1GB | 缓存大小限制 (字节) |
| CompactionPercentage | double | 0.05 | 0.01-0.5 | 压缩百分比 |
| ExpirationScanFrequencySeconds | int | 60 | 10-300 | 过期扫描频率 (秒) |
| DefaultExpirationMinutes | int | 5 | 1-1440 | 默认过期时间 (分钟) |

**SystemAdmin (系统管理员)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| UserName | string | "sysadmin" | Required | 用户名 |
| Email | string | "admin@lybt.com" | Required, Email | 邮箱 |
| DisplayName | string | "系统管理员" | Required | 显示名称 |
| AutoCreateOnStartup | bool | true | - | 启动时自动创建 |
| SessionTimeoutMinutes | int | 240 | 30-480 | 会话超时 (分钟) |

**UserManagement (用户管理)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| DefaultRole | string | "Doctor" | Required | 默认角色 |
| AllowSelfRegistration | bool | false | - | 允许自注册 |
| RequireEmailConfirmation | bool | true | - | 需要邮箱确认 |
| EnableUserCache | bool | true | - | 启用用户缓存 |
| MaxBatchOperationSize | int | 100 | 1-1000 | 最大批量操作数 |

**Logging (日志清理)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| Cleanup.Enabled | bool | true | - | 启用日志清理 |
| Cleanup.RetentionDays | int | 90 | 1-365 | 保留天数 |
| Cleanup.CleanupIntervalHours | int | 24 | 1-168 | 清理间隔 (小时) |
| Cleanup.InitialDelayMinutes | int | 5 | 1-60 | 启动延迟 (分钟) |
| Cleanup.BatchSize | int | 1000 | 100-10000 | 批量清理大小 |

**Swagger (API 文档)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| Title | string | "凌隐宝堂中医诊所 API" | Required | 文档标题 |
| EnableXmlComments | bool | true | - | 启用 XML 注释 |
| RoutePrefix | string | "swagger" | - | 路由前缀 |
| EnableInProduction | bool | false | - | 生产环境启用 |

**Json (序列化)**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| UnsafeRelaxedEscaping | bool | false | 宽松 JSON 转义 |
| PropertyNamingPolicy | string | "CamelCase" | 属性命名策略 |
| IgnoreReadOnlyProperties | bool | false | 忽略只读属性 |
| AllowTrailingCommas | bool | false | 允许尾随逗号 |

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端读取 appsettings.json |
| 本地 | 不适用 (服务端配置) |

#### 配置变更行为

| 配置项 | 变更后行为 | 原因 |
|--------|-----------|------|
| Jwt | **需重启** (ValidateOnStart) | 安全敏感，运行时变更可能导致 Token 验证不一致 |
| Database | **需重启** (ValidateOnStart) | 连接池和连接字符串变更需重建 DbContext |
| Security | **需重启** (ValidateOnStart) | 速率限制策略影响中间件管道 |
| PasswordPolicy | **需重启** (ValidateOnStart) | 影响用户注册/改密等安全流程 |
| DefaultPasswords | **需重启** (ValidateOnStart) | 仅启动时使用 |
| SystemAdmin | **需重启** (ValidateOnStart) | 仅启动时创建 |
| Swagger | **需重启** | 影响中间件管道注册 |
| Json | **需重启** | 影响全局序列化行为 |
| Logging | **支持热更新** (无 ValidateOnStart) | 运维需要不停机调整日志级别 |
| MemoryCache | **需重启** | 缓存策略变更需重建缓存实例 |
| UserManagement | **需重启** | 影响 DI 注册行为 |

> **已确定**: 关键安全配置 (JWT/Database/Security) 使用 ValidateOnStart 确保启动时验证；运维配置 (Logging) 支持热更新，修改 appsettings.json 后自动生效无需重启。

### US-CFG-002: 客户端配置参数

> As a 运维人员, I want to 通过 Desktop 客户端的 appsettings.json 管理 API 连接、会话和功能开关,
> so that 我可以控制客户端的连接目标、超时行为和功能可见性。

**Acceptance Criteria:**
- [ ] ApiClient.BaseUrl 配置错误 -> 启动时显示连接失败
- [ ] FeatureToggle=false -> 对应功能按钮/菜单项完全隐藏 (Collapsed)
- [ ] FeatureToggle=true -> 对应功能正常显示和交互
- [ ] ClinicSettings.Name 在处方打印模板中显示
- [ ] 所有客户端配置变更 -> 需重启 Desktop 才生效

**Business Rules:**
1. Desktop 客户端通过 5 个 Options 类管理配置
2. 所有客户端配置需重启 Desktop 才能生效 (Prism 容器启动时一次性绑定)
3. FeatureToggle=false 时，对应功能的按钮/菜单项完全隐藏 (Visibility=Collapsed)，用户不可见、不可交互
4. FeatureToggle 仅控制 UI 层，API 端点不受影响

#### 配置参数总表

**ApiClient (API 连接)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| BaseUrl | string | "https://localhost:5001/" | Required, Url | API 基础地址 |
| TimeoutSeconds | int | 60 | 5-300 | 请求超时 (秒) |
| IgnoreSslErrors | bool | false | - | 忽略 SSL 错误 (仅开发) |

**ClientSession (客户端会话)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| InactivityTimeoutMinutes | int | 15 | 1-120 | 无活动超时 (分钟) |
| WarningBeforeTimeoutMinutes | int | 2 | 0-10 | 超时前警告 (分钟) |
| ActivityCheckIntervalSeconds | int | 30 | 10-120 | 活动检查间隔 (秒) |

**FeatureToggles (功能开关)**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| ConsultationCreate | bool | false | 创建诊断 (未上线) |
| ConsultationEdit | bool | false | 编辑诊断 (未上线) |
| ConsultationDelete | bool | false | 删除诊断 (未上线) |
| ConsultationViewDetail | bool | true | 查看诊断详情 |
| ConsultationSearch | bool | true | 搜索诊断 |
| PrescriptionCreate | bool | false | 创建处方 (未上线) |
| PrescriptionDelete | bool | false | 删除处方 (未上线) |
| PrescriptionClone | bool | true | 克隆处方 |
| PrescriptionExport | bool | true | 导出处方 |
| PrescriptionViewDetail | bool | true | 查看处方详情 |
| PrescriptionSearch | bool | true | 搜索处方 |
| CardReaderEnabled | bool | false | 身份证读卡器 (需硬件支持) |
| MedicalCaseCreate | bool | true | 创建医案 |
| MedicalCaseEdit | bool | true | 编辑医案 |
| MedicalCaseDelete | bool | true | 删除医案 |
| MedicalCaseViewDetail | bool | true | 查看医案详情 |
| MedicalCaseSearch | bool | true | 搜索医案 |

**ClinicSettings (诊所信息)**

| 参数 | 类型 | 默认值 | 约束 | 说明 |
|------|------|--------|------|------|
| Name | string | (必填) | Required | 诊所名称 |
| Address | string | "" | - | 地址 |
| Phone | string | "" | - | 电话 |
| Department | string | "中医科" | - | 科室 |

**Prescription (处方配置)**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| DuplicateHerbMergeStrategy | string | "Max" | 重复药材合并策略 (Max/Sum/First) |

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 客户端 appsettings.json 配置 API 连接参数 |
| 本地 | ApiClient 配置无效，使用本地数据源 |

#### FeatureToggle UI 行为规则

| 行为 | 规范 |
|------|------|
| 菜单项 | 隐藏，不占位 |
| 工具栏按钮 | 隐藏，不占位 |
| 快捷键 | 失效，不响应 |
| API 端点 | 仍然可用 (开关仅控制 UI 层) |

**ViewModel 实现模式**:
```csharp
// ViewModel 暴露只读属性
public bool CanCreateConsultation => _featureToggles.ConsultationCreate;

// XAML 绑定控制可见性
<Button Visibility="{Binding CanCreateConsultation,
        Converter={StaticResource BoolToVisibilityConverter}}" />
```

#### v1.0 功能开关默认状态

| 模块 | 开关 | 默认值 | 说明 |
|------|------|--------|------|
| Consultation | Create / Edit / Delete | **false** | 诊断独立 CRUD 未上线，通过医案内操作 |
| Consultation | ViewDetail / Search | **true** | 医案内查看/搜索诊断信息 |
| Prescription | Create / Delete | **false** | 处方独立 CRUD 未上线，通过医案内操作 |
| Prescription | Clone / Export / ViewDetail / Search | **true** | 医案内处方操作 |
| MedicalCase | Create / Edit / Delete / ViewDetail / Search | **true** | 核心功能全部启用 |
| CardReader | Enabled | **false** | 需硬件支持，默认关闭 |

### US-CFG-003: 环境配置管理

> As a 运维人员, I want to 通过分环境配置文件和环境变量管理不同部署环境的配置,
> so that 开发和生产环境行为严格隔离，敏感信息不暴露在代码仓库中。

**Acceptance Criteria:**
- [ ] Development 环境 -> Swagger 可访问
- [ ] Production 环境 -> Swagger 不可访问 (EnableInProduction=false)
- [ ] 环境变量 Jwt__SecretKey -> 覆盖配置文件中的值

**Business Rules:**
1. 配置加载优先级: appsettings.json -> appsettings.{Environment}.json -> 环境变量
2. 开发环境 (Development): 宽松配置 (AutoMigrate=true, IgnoreSslErrors=true)
3. 生产环境 (Production): 严格配置 (EnableInProduction=false for Swagger, AllowInProduction=false for DefaultPasswords)
4. 环境变量格式: `Section__Key` (双下划线分隔)
5. 敏感配置推荐使用环境变量而非配置文件

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端分环境配置 |
| 本地 | 客户端分环境配置 |

### US-CFG-004: 生产环境启动配置验证

> As a 运维人员, I want to 生产环境启动时自动验证关键配置项,
> so that 配置缺失或错误时应用立即报错并给出修复指引，而不是运行时出现莫名异常。

**Acceptance Criteria:**
- [ ] Production + 连接字符串缺失 -> 启动失败，退出码 1
- [ ] Production + JWT 密钥过短 -> 启动失败
- [ ] Development + 连接字符串缺失 -> 正常启动 (不触发验证)
- [ ] 错误输出包含具体的环境变量设置命令

**Business Rules:**
1. 仅在 `ASPNETCORE_ENVIRONMENT=Production` 时触发
2. 验证通过 -> 正常启动
3. 验证失败 -> 输出详细错误信息到控制台 + Fatal 日志，调用 Environment.Exit(1) 退出
4. 错误信息包含: 配置路径、对应环境变量名、问题描述、修复示例

#### 验证项清单

| 配置项 | 严重级别 | 验证规则 |
|--------|---------|---------|
| ConnectionStrings:DefaultConnection | **Critical** | 必须非空 |
| Lybt:Jwt:SecretKey | **Critical** | 必须非空，Base64 解码后 >= 32 字节 |
| Lybt:DefaultPasswords:SysAdminPassword | Important | 必须非空 |
| Lybt:DefaultPasswords:NewUserPassword | Important | 必须非空 |
| Lybt:Business:SystemAdmin:UserName | Important | 必须非空 |
| Lybt:Business:SystemAdmin:Email | Important | 必须非空，符合 Email 格式 |
| AllowedHosts | Optional | 建议设置 (不阻止启动) |

> **已确定**: Critical 级别配置缺失直接阻止启动；Important 级别缺失输出警告但允许启动；Optional 级别仅建议。

#### 错误输出格式

```
Production 配置验证失败
发现 N 个配置错误：

CRITICAL 错误（必须修复）:
[1] 数据库连接字符串
    配置路径: ConnectionStrings:DefaultConnection
    环境变量: ConnectionStrings__DefaultConnection
    问题: 配置值未设置
    修复方法: setx ConnectionStrings__DefaultConnection "<your-value>"
```

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端启动时验证 |
| 本地 | 不适用 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 配置管理 UI (Web 界面修改配置) | 诊所规模小，appsettings.json + 环境变量足够，v2.0+ 考虑 |
| FeatureToggle 热更新 | MVP 阶段重启够用，热更新复杂度高，延期到运维完善 Sprint |
| 配置加密存储 (Data Protection API) | 当前通过环境变量隔离敏感信息，v2.0+ 考虑 |
| 配置版本管理/回滚 | 运维通过 Git 管理配置文件变更，系统内不实现 |
| 集中配置中心 (Consul/etcd) | 单机部署场景不需要，v2.0+ 多节点部署时考虑 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 配置文件被误删 | 应用无法启动或使用默认值运行 | DataAnnotation 验证 + ProductionConfigurationValidator 在启动时捕获 |
| 环境变量命名错误 | 配置未被正确覆盖，使用默认值 | 错误输出包含正确的环境变量名和设置命令 |
| FeatureToggle 遗忘恢复 | 功能已开发完成但未开启开关 | 功能上线检查清单包含 FeatureToggle 确认项 |
| appsettings.json 提交敏感信息 | 密钥/密码泄露到代码仓库 | .gitignore 排除 appsettings.Production.json + 敏感项使用环境变量 |
| 配置变更后未重启 | 非热更新配置变更不生效 | 配置变更行为表明确标注哪些需重启 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-CFG-01 | FeatureToggle 热更新何时实现? | 延期。MVP 阶段需重启 Desktop 生效，计划在运维完善 Sprint 实现 |
| OQ-CFG-02 | 是否需要配置管理 UI (Web 界面)? | 延期。当前 appsettings.json 足够，v2.0+ 根据运维反馈决定 |
| OQ-CFG-03 | Swagger/Json 的 Options 注册方式是否需要 PRD 规定? | 已关闭。注册方式属实现细节，PRD 不规定 (CFG-06) |
| OQ-CFG-04 | 配置错误输出格式是否需要严格规定? | 已关闭。输出格式以代码实现为准，PRD 仅提供参考格式 (CFG-08) |

---

## Data Model

配置模块不引入独立数据库实体。所有配置通过 appsettings.json + 环境变量管理，运行时绑定到强类型 Options 类。

Options 类分布:
- **Server (8 个, Shared.Configuration)**: SessionOptions, SecurityOptions, DefaultPasswordOptions, DatabaseOptions, MemoryCacheOptions, SystemAdminOptions, LoggingOptions, SwaggerOptions
- **Server (1 个, WebAPI)**: JsonOptions (仅 WebAPI 使用，已从 Shared.Configuration 迁出)
- **Common (1 个)**: JwtOptions (Server/Client 共享)
- **Client (4 个)**: ApiClientOptions, ClientSessionOptions, FeatureToggleOptions, ClinicSettingsOptions

> **变更记录**: PrescriptionOptions + SyncOptions 已合并入 FeatureToggleOptions；PasswordPolicyOptions + UserManagementOptions 为死代码已删除；JsonOptions 迁移至 WebAPI 项目。

---

## Error Codes

配置模块不定义业务错误码。配置验证失败通过以下机制报告:

| 场景 | 报告方式 |
|------|---------|
| DataAnnotation 验证失败 | OptionsValidationException (启动时) |
| ProductionConfigurationValidator 失败 | Console 输出 + Fatal 日志 + Environment.Exit(1) |
| 客户端 API 连接失败 | UI 显示连接错误提示 |

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| CFG-D01 | Options 类放置位置 | US-CFG-001, US-CFG-002 | 已确定: 按 Server/Client/Common 三层分类 |
| CFG-D02 | 默认密码安全策略 | US-CFG-001 | 已确定: 生产环境禁止使用默认密码，仅空库时有效 |
| CFG-D03 | 连接字符串 fallback 链 | US-CFG-001 | 已确定: DatabaseOptions.ConnectionString -> ConnectionStrings:DefaultConnection -> 环境变量 |
| CFG-D04 | FeatureToggle=false UI 行为 | US-CFG-002 | 已确定: 隐藏 (Collapsed)，用户不可见、不可交互。API 端点不受影响 |
| CFG-D05 | 配置变更管理 | US-CFG-001, US-CFG-002 | 已确定: 安全配置 (JWT/DB/Security) 需重启；运维配置 (Logging) 支持热更新；Desktop 端全部需重启 |
| CFG-D06 | 生产环境启动验证 | US-CFG-004 | 已确定: Critical 配置缺失阻止启动 (Exit 1)；Important 配置输出警告；仅 Production 环境触发 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | Swagger/Json 注册方式简化，PRD 不再规定具体注册方式 | 注册方式属实现细节，不影响功能 | CFG-06 |
| 2026-02-21 | 配置错误输出格式对齐代码实现 | 输出格式细节差异不影响功能，PRD 对齐代码 | CFG-08 |
| 2026-02-21 | FeatureToggle 热更新支持延期实现 | MVP 阶段重启够用，热更新复杂度高 | CFG-07 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | R8 深化: 新增 FR-CFG-004 (生产环境启动验证)、配置变更行为表、FeatureToggle UI 行为规则和 v1.0 默认状态表、CardReader 开关、3 条新决策 |
| 2026-02-17 | v2.1 | PRD审查修复: A3-InactivityTimeout 5->15min/Warning 0->2min, A5-DefaultRole Staff->Doctor |
| 2026-02-21 | v2.2 | PRD vs Code 偏差分析修订: 2 项修订, 1 项延期标注 |
| 2026-03-06 | v3.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节，决策记录迁移为 CFG-D01~D06 编号体系 |
| 2026-04-03 | v3.1 | Data Model 对齐代码: Options 类数量从 17→14，反映 PrescriptionOptions/SyncOptions 合并、PasswordPolicyOptions/UserManagementOptions 删除、JsonOptions 迁移 |
