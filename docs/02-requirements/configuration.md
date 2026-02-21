# 配置参数 需求规格

## 概述

系统配置采用 ASP.NET Core Options 模式，通过强类型 Options 类绑定 appsettings.json 配置节。配置分为服务端 (Server) 和客户端 (Client) 两类，共 17 个 Options 类。支持环境分层覆盖 (Development/Production) 和 DataAnnotation 验证。

---

## 用户角色

| 角色 | 在本模块中的交互 |
|------|-----------------|
| 运维人员 | 修改 appsettings.json 配置 |
| SuperAdmin | 通过 DiagnosticsController 管理日志级别 |

> 配置管理主要面向运维人员，业务用户不直接接触。

---

## 功能清单

### FR-CFG-001: 服务端配置参数

- **描述**: 服务端通过 12 个 Options 类管理 JWT、会话、安全、数据库等核心配置
- **业务规则**:
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

> **[已修订 2026-02-21]** Swagger/Json 注册方式要求简化，PRD 不再规定具体注册方式，由实现自行决定
> 原因: 注册方式属实现细节，不影响功能  |  参考: CFG-06

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

- **远程模式**: 服务端读取 appsettings.json
- **本地模式**: 不适用 (服务端配置)

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

- **验收标准**:
  - [ ] 所有必填参数未配置 -> 启动时抛出验证异常
  - [ ] 参数超出 Range 范围 -> 启动时抛出验证异常
  - [ ] 环境变量可覆盖 appsettings.json 中的值
  - [ ] Logging 配置修改 -> 无需重启即生效
  - [ ] JWT/Database 配置修改 -> 需重启才生效

### FR-CFG-002: 客户端配置参数

- **描述**: Desktop 客户端通过 5 个 Options 类管理 API 连接、会话、功能开关等配置

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

> **[延期 2026-02-21]** FeatureToggle 热更新支持延期实现，当前需重启 Desktop 生效
> 原因: MVP 阶段重启够用，热更新复杂度高  |  计划: 运维完善 Sprint  |  参考: CFG-07

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

- **远程模式**: 客户端 appsettings.json 配置 API 连接参数
- **本地模式**: ApiClient 配置无效，使用本地数据源

#### 客户端配置变更行为

Desktop 客户端所有配置均**需重启**才能生效 (Prism 容器启动时一次性绑定)。

#### FeatureToggle UI 行为规则

> **已确定**: FeatureToggle=false 时，对应功能的按钮/菜单项**完全隐藏** (Visibility=Collapsed)，用户不可见、不可交互。

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

- **验收标准**:
  - [ ] ApiClient.BaseUrl 配置错误 -> 启动时显示连接失败
  - [ ] FeatureToggle=false -> 对应功能按钮/菜单项完全隐藏 (Collapsed)
  - [ ] FeatureToggle=true -> 对应功能正常显示和交互
  - [ ] ClinicSettings.Name 在处方打印模板中显示
  - [ ] 所有客户端配置变更 -> 需重启 Desktop 才生效

### FR-CFG-003: 环境配置管理

- **描述**: 支持分环境配置文件和环境变量覆盖
- **业务规则**:
  1. 配置加载优先级: appsettings.json -> appsettings.{Environment}.json -> 环境变量
  2. 开发环境 (Development): 宽松配置 (AutoMigrate=true, IgnoreSslErrors=true)
  3. 生产环境 (Production): 严格配置 (EnableInProduction=false for Swagger, AllowInProduction=false for DefaultPasswords)
  4. 环境变量格式: `Section__Key` (双下划线分隔)
  5. 敏感配置推荐使用环境变量而非配置文件
- **远程模式**: 服务端分环境配置
- **本地模式**: 客户端分环境配置
- **验收标准**:
  - [ ] Development 环境 -> Swagger 可访问
  - [ ] Production 环境 -> Swagger 不可访问 (EnableInProduction=false)
  - [ ] 环境变量 Jwt__SecretKey -> 覆盖配置文件中的值

### FR-CFG-004: 生产环境启动配置验证

- **描述**: Production 环境启动时，通过 ProductionConfigurationValidator 强制验证关键配置项，缺失则阻止启动
- **业务规则**:
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

> **[已修订 2026-02-21]** 配置错误输出格式对齐代码实现，实际输出格式以代码为准
> 原因: 输出格式细节差异不影响功能，PRD 对齐代码  |  参考: CFG-08

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

- **远程模式**: 服务端启动时验证
- **本地模式**: 不适用
- **验收标准**:
  - [ ] Production + 连接字符串缺失 -> 启动失败，退出码 1
  - [ ] Production + JWT 密钥过短 -> 启动失败
  - [ ] Development + 连接字符串缺失 -> 正常启动 (不触发验证)
  - [ ] 错误输出包含具体的环境变量设置命令

---

## 决策记录

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | Options 类放置位置 | 按 Server/Client/Common 三层分类 | 2026-02-11 |
| 2 | 默认密码安全策略 | 生产环境禁止使用默认密码，仅空库时有效 | 2026-02-11 |
| 3 | 连接字符串 fallback 链 | DatabaseOptions.ConnectionString -> ConnectionStrings:DefaultConnection -> 环境变量 | 2026-02-11 |
| 4 | FeatureToggle=false UI 行为 | 隐藏 (Collapsed)，用户不可见、不可交互。API 端点不受影响 | 2026-02-17 |
| 5 | 配置变更管理 | 安全配置 (JWT/DB/Security) 需重启；运维配置 (Logging) 支持热更新；Desktop 端全部需重启 | 2026-02-17 |
| 6 | 生产环境启动验证 | Critical 配置缺失阻止启动 (Exit 1)；Important 配置输出警告；仅 Production 环境触发 | 2026-02-17 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | R8 深化: 新增 FR-CFG-004 (生产环境启动验证)、配置变更行为表、FeatureToggle UI 行为规则和 v1.0 默认状态表、CardReader 开关、3 条新决策 |
| 2026-02-17 | v2.1 | PRD审查修复: A3-InactivityTimeout 5->15min/Warning 0->2min, A5-DefaultRole Staff->Doctor |
| 2026-02-21 | v2.2 | PRD vs Code 偏差分析修订: 2 项修订, 1 项延期标注 |
