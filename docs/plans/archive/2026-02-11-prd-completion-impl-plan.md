# PRD文档全面补全 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 补全PRD文档体系，消除代码实现与需求文档之间的4类12项差距，产出5个新PRD + 修改12个现有文档。

**Architecture:** 纯文档编写任务。每个新PRD遵循统一模板 (概述/角色/功能清单/数据模型/决策/变更)。信息源来自代码实现的逆向工程，通过 Grep/Read 提取端点定义、异常类型、配置参数。

**Tech Stack:** Markdown文档，遵循现有PRD模板格式，FR-xxx-NNN 统一编号体系。

---

## Task 1: 创建 health-diagnostics.md

**Files:**
- Create: `docs/02-requirements/health-diagnostics.md`
- Reference: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`
- Reference: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`
- Reference: `docs/04-api-reference/health.md`
- Reference: `docs/04-api-reference/diagnostics.md`

**Step 1: 读取 HealthController 和 DiagnosticsController 代码**

读取两个 Controller 文件，提取:
- 所有端点路由 + HTTP方法
- 授权注解 (`[Authorize]`, `[AllowAnonymous]`)
- 请求/响应DTO类型
- 业务逻辑 (数据库检查、迁移状态、日志级别管理)

**Step 2: 读取已有 API 参考文档**

读取 `docs/04-api-reference/health.md` 和 `docs/04-api-reference/diagnostics.md`，与代码交叉验证。

**Step 3: 编写 health-diagnostics.md**

遵循以下模板结构:

```markdown
# 系统健康与诊断 需求规格

## 概述
系统健康检查和运行时诊断模块...

## 用户角色
| 角色 | 权限 |
| SuperAdmin | 全部 (含诊断) |
| Admin | 健康检查 (详细) |
| Doctor | 健康检查 (基础) |
| Receptionist | 健康检查 (基础) |

## 功能清单
### FR-SYS-001 ~ FR-SYS-007 (每个FR含: 描述/业务规则/远程模式/本地模式/验收标准)

## 数据模型 (HealthCheckResult, LoggingStatus 等)

## 配置参数

## 错误码

## 决策记录

## 变更记录
```

**关键业务规则 (从代码提取):**
- FR-SYS-001: 匿名访问，返回 status + version + environment
- FR-SYS-003: 需认证，检查数据库连接 + 迁移状态 + 待执行迁移数
- FR-SYS-005: durationMinutes 参数范围 1-120，默认 30 分钟
- FR-SYS-007: 仅 SuperAdmin，支持设置 Verbose/Debug/Information/Warning/Error/Fatal

**Step 4: 验证文档完整性**

检查:
- 每个 FR 都有双模式对比 (健康检查: 远程=API调用, 本地=不适用/仅客户端自检)
- 错误码与 Controller 中的返回值一致
- 验收标准格式: `- [ ] [场景] -> [预期结果]`

---

## Task 2: 创建 error-handling.md

**Files:**
- Create: `docs/02-requirements/error-handling.md`
- Reference: `src/Shared/LYBT.Shared.ExceptionHandling/` (所有文件)
- Reference: Server端异常处理中间件
- Reference: `src/Client/Desktop/Shell/` 中的异常处理

**Step 1: 读取 ExceptionHandling 模块代码**

搜索并读取:
- 异常基类 (BusinessException, SystemException 或类似)
- ExceptionSeverity 枚举
- ProblemDetails 相关类
- 服务端 ExceptionHandler/Middleware
- 客户端 DesktopExceptionHandler

**Step 2: 收集所有 ErrorCode 定义**

使用 Grep 搜索:
```
Grep: "ErrorCode" 或 "errorCode" 在 src/ 目录
Grep: "BusinessException" 在 src/ 目录
```

**Step 3: 编写 error-handling.md**

```markdown
# 异常处理策略 需求规格

## 概述
系统采用分层异常处理架构...

## 功能清单
### FR-ERR-001: 服务端全局异常处理
- BusinessException: 业务逻辑错误 (400/404/409)
- SystemException: 系统内部错误 (500)
- 中间件自动捕获并转换为 ProblemDetails

### FR-ERR-002: ProblemDetails 标准化
- RFC 7807 格式
- 包含: type, title, status, detail, instance, errorCode
- 开发环境包含 stackTrace

### FR-ERR-003: 客户端异常处理
- 未处理异常弹窗 (用户友好消息)
- API 调用异常自动解析 ProblemDetails
- 网络错误特殊处理

### FR-ERR-004: 异常严重度分级
- Low/Medium/High/Critical 枚举

### FR-ERR-005: 全局错误码注册表
- 按模块分组的错误码表 (从 Phase 2 收集的错误码汇总)

## 数据模型 (ProblemDetails 结构, ExceptionSeverity 枚举)

## 决策记录
## 变更记录
```

---

## Task 3: 创建 logging.md

**Files:**
- Create: `docs/02-requirements/logging.md`
- Reference: `src/Shared/LYBT.Shared.Logging/` (所有文件)
- Reference: `src/Server/Core/LYBT.Entities/` 中的 SecurityAuditLog 和 SystemLog 实体

**Step 1: 读取 Logging 模块代码**

搜索并读取:
- LoggingLevelManager (运行时级别管理)
- SensitiveDataAttribute / 脱敏相关
- CorrelationId 相关 (中间件或 enricher)
- Serilog 配置

**Step 2: 读取审计日志实体**

读取:
- SecurityAuditLog.cs (字段定义)
- SystemLog.cs (字段定义)

**Step 3: 编写 logging.md**

```markdown
# 日志与审计体系 需求规格

## 概述
系统采用 Serilog 结构化日志框架...

## 功能清单
### FR-LOG-001: 结构化日志
- Serilog 集成
- CorrelationId 自动注入 (请求级唯一标识)
- 日志输出: Console + File (按天滚动)
- 日志字段: Timestamp, Level, Message, CorrelationId, MachineName, ThreadId

### FR-LOG-002: 安全审计日志
- SecurityAuditLog 实体 (数据模型)
- 事件类型: Login/Logout/RefreshToken/PasswordChange/UserDisabled 等
- 包含: UserId, EventType, Success, IpAddress, ErrorMessage

### FR-LOG-003: 敏感数据脱敏
- SensitiveDataAttribute 标记字段
- 自动遮蔽: 密码、Token、身份证号等
- 脱敏规则: 保留前2后2，中间用 ***

### FR-LOG-004: 运行时日志级别管理
- LoggingLevelManager: 动态调整日志级别
- 支持临时提升 (自动恢复)
- 与 DiagnosticsController 联动

## 数据模型
### SecurityAuditLog (完整字段表)
### SystemLog (完整字段表)

## 决策记录
## 变更记录
```

---

## Task 4: 创建 desktop-shell.md

**Files:**
- Create: `docs/02-requirements/desktop-shell.md`
- Reference: `src/Client/Desktop/Shell/` (App.xaml.cs, Bootstrapper, Services/)
- Reference: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/` (导航、对话框)
- Reference: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/` (基类)

**Step 1: 读取 Shell 启动代码**

搜索并读取:
- ApplicationBootstrapper / App.xaml.cs
- StartupPipeline 或启动步骤类
- SessionLifecycleManager
- LoginCoordinator

**Step 2: 读取 Infrastructure 代码**

搜索并读取:
- NavigationCoordinator / IRegionManager 用法
- MenuManager
- IDialogService / 对话框实现
- AccountSettingsViewModel

**Step 3: 编写 desktop-shell.md**

```markdown
# Desktop Shell 基础设施 需求规格

## 概述
Desktop Shell 是 WPF 客户端的宿主框架，基于 Prism 模块化架构...

## 用户角色
| 角色 | 权限 |
| 所有角色 | 使用 Shell 基础设施 (导航/对话框/菜单) |
| Admin | 管理工作区入口 |
| Doctor | 临床工作区入口 |

## 功能清单
### FR-SHELL-001: 应用启动流水线
- 5步启动: DI容器初始化 → 健康检查 → 会话恢复 → 模块加载 → 导航就绪
- 启动失败处理: 显示错误对话框，允许重试或退出

### FR-SHELL-002: 会话生命周期管理
- SessionLifecycleManager: 登录→工作台→登出
- 登录成功后切换到主窗口
- 登出后返回登录窗口
- 会话超时自动登出

### FR-SHELL-003: 页面导航系统
- Prism Region 导航
- NavigationCoordinator: 封装导航逻辑
- 返回栈管理
- 导航参数传递 (NavigationParameters)

### FR-SHELL-004: 菜单系统
- MenuManager: 根据角色动态生成菜单
- 菜单项: 图标 + 标题 + 导航目标
- 角色过滤: Doctor 看临床菜单, Admin 看管理菜单

### FR-SHELL-005: 通用对话框体系
- ConfirmationDialog: 确认操作 (是/否)
- InputDialog: 文本输入 (如修改原因)
- MessageDialog: 消息提示 (信息/警告/错误)

### FR-SHELL-006: 启动诊断与监控
- StartupDiagnostics: 环境检查
- StartupPerformanceMonitor: 各步骤耗时
- 诊断结果日志记录

### FR-SHELL-007: 账户设置
- AccountSettingsControl: 个人信息查看
- 修改密码入口
- 个人资料编辑

## 数据模型 (MenuItemModel, NavigationContext 等)
## 决策记录
## 变更记录
```

**注意:** 本地模式和远程模式的描述:
- 大部分 Shell 功能不区分模式 (纯客户端)
- FR-SHELL-002 中的会话管理: 远程=JWT Token管理, 本地=简化登录状态

---

## Task 5: 创建 configuration.md

**Files:**
- Create: `docs/02-requirements/configuration.md`
- Reference: `src/Shared/LYBT.Shared.Configuration/` (所有 Options 类)
- Reference: `src/Server/Services/LYBT.WebAPI/appsettings.json`
- Reference: `docs/06-operations/configuration.md`
- Reference: 各模块 PRD 中散落的配置参数

**Step 1: 收集所有 Options 类**

在 `src/Shared/LYBT.Shared.Configuration/` 中读取所有 `*Options.cs` 文件:
- JwtOptions
- SessionOptions
- SecurityOptions
- PasswordPolicyOptions
- RateLimitingOptions
- DatabaseOptions
- SwaggerOptions
- MemoryCacheOptions
- SystemAdminOptions
- UserManagementOptions

**Step 2: 读取 appsettings.json**

读取完整配置文件，提取所有配置节。

**Step 3: 收集客户端配置**

搜索客户端配置:
- 连接模式 (Local/Remote)
- API 基地址
- 超时设置
- 打印机配置
- 读卡器配置 (CardReaderOptions)

**Step 4: 编写 configuration.md**

```markdown
# 配置参数 需求规格

## 概述
系统配置采用 ASP.NET Core Options 模式...

## 功能清单
### FR-CFG-001: 服务端配置参数
- 完整参数表 (节名/参数名/类型/默认值/约束/说明)
- 按功能分组: JWT / Session / Security / RateLimiting / Database / Swagger

### FR-CFG-002: 客户端配置参数
- 连接模式配置
- API 连接参数
- 打印机默认设置
- 读卡器配置
- 超时配置

### FR-CFG-003: 环境配置管理
- appsettings.json (基础)
- appsettings.Development.json (开发覆盖)
- appsettings.Production.json (生产覆盖)
- 环境变量覆盖规则

## 配置参数总表 (核心交付)

### 服务端配置
| 配置节 | 参数 | 类型 | 默认值 | 约束 | 说明 |
|--------|------|------|--------|------|------|
| Jwt | AccessTokenExpirationMinutes | int | 30 | >0 | ... |
| ... | ... | ... | ... | ... | ... |

### 客户端配置
| 配置项 | 类型 | 默认值 | 说明 |
| ... | ... | ... | ... |

## 决策记录
## 变更记录
```

---

## Task 6: 补充 users.md 错误码

**Files:**
- Modify: `docs/02-requirements/users.md`
- Reference: `src/Server/Modules/LYBT.Module.Users/` (Service 层异常)

**Step 1: 搜索 Users 模块错误码**

```
Grep: "throw\|Exception\|Error\|错误" in src/Server/Modules/LYBT.Module.Users/
Grep: "StatusCode\|BadRequest\|NotFound\|Conflict\|Forbid" in Users Controller
```

**Step 2: 在 users.md 末尾的"决策记录"前插入错误码章节**

```markdown
## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 用户不存在 | 404 | User not found | GetById/Update/Delete 时 ID 无效 |
| 用户名重复 | 409 | Username already exists | 创建时用户名冲突 |
| 保留用户名 | 400 | Username is reserved | 使用 admin/root 等保留名 |
| 不能删除自己 | 400 | Cannot delete yourself | 当前用户尝试删除自己 |
| 最后管理员保护 | 400 | Cannot delete last admin | 删除后无 Admin 用户 |
| 权限不足 | 403 | Insufficient permissions | Admin 修改 SuperAdmin |
| 密码不符合策略 | 400 | Password does not meet policy | 密码复杂度不足 |
| 旧密码错误 | 400 | Current password is incorrect | 修改密码时旧密码错误 |
```

---

## Task 7: 补充 patients.md 错误码

**Files:**
- Modify: `docs/02-requirements/patients.md`
- Reference: `src/Server/Modules/LYBT.Module.Patients/` (Service 层异常)

**Step 1: 搜索 Patients 模块错误码**

```
Grep: "throw\|Exception\|Error" in src/Server/Modules/LYBT.Module.Patients/
```

**Step 2: 插入错误码章节**

```markdown
## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 患者不存在 | 404 | Patient not found | GetById/Update/Delete 时 ID 无效 |
| 手机号重复 | 409 | Phone number already exists | 创建/更新时手机号冲突 |
| 身份证号重复 | 409 | ID number already exists | 创建/更新时身份证号冲突 |
| 存在关联医案 | 400 | Patient has references | 删除时有关联 MedicalCase |
| 导入格式错误 | 400 | Invalid import format | Excel 导入时格式不匹配 |
```

---

## Task 8: 补充 herbs.md 错误码

**Files:**
- Modify: `docs/02-requirements/herbs.md`
- Reference: `src/Server/Modules/LYBT.Module.Herbs/` (Service 层异常)

**Step 1: 搜索 Herbs 模块错误码**

**Step 2: 插入错误码章节**

```markdown
## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 药材不存在 | 404 | Herb not found | GetById/Update/Delete 时 ID 无效 |
| 药材名重复 | 409 | Herb name already exists | 创建/更新时名称冲突 |
| 存在处方引用 | 400 | Herb is referenced by prescriptions | 删除/禁用时被处方引用 |
| 导入格式错误 | 400 | Invalid import format | Excel/JSON 导入时格式不匹配 |
| 价格无效 | 400 | Invalid price | 价格<=0 或超出范围 |
```

---

## Task 9: 补充 formulas.md 错误码

**Files:**
- Modify: `docs/02-requirements/formulas.md`
- Reference: `src/Server/Modules/LYBT.Module.Formula/` (Service 层异常)

**Step 1: 搜索 Formula 模块错误码**

**Step 2: 插入错误码章节**

```markdown
## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 验方不存在 | 404 | Formula not found | GetById/Update/Delete 时 ID 无效 |
| 无编辑权限 | 403 | No permission to edit | Doctor 编辑他人验方 |
| 无删除权限 | 403 | No permission to delete | Doctor 删除他人验方 |
| 验方名重复 | 409 | Formula name already exists | 创建时名称冲突 |
| 药材未绑定 | 400 | Herb not bound | 延迟绑定的药材未关联药材库 |
```

---

## Task 10: 补充 medical-cases.md 错误码

**Files:**
- Modify: `docs/02-requirements/medical-cases.md`
- Reference: `src/Server/Modules/LYBT.Module.MedicalCase/` (Service 层异常)

**Step 1: 搜索 MedicalCase 模块错误码**

**Step 2: 插入错误码章节**

```markdown
## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 医案不存在 | 404 | Medical case not found | GetById/Update 时 ID 无效 |
| 无编辑权限 | 403 | Permission denied | Doctor 编辑他人医案 |
| 需要修改原因 | 400 | Edit reason required | 编辑已锁定医案未提供理由 |
| 无效状态转换 | 400 | Invalid status transition | 如 Cancelled→Active |
| 患者不存在 | 404 | Patient not found | 创建医案时 PatientId 无效 |
| 处方无药材 | 400 | Prescription must have items | 保存处方时药材列表为空 |
| 辨证必填 | 400 | TcmDiagnosis is required | 完成医案时未填辨证 |
```

---

## Task 11: 补充 sync.md / printing.md / card-reader.md 错误码

**Files:**
- Modify: `docs/02-requirements/sync.md`
- Modify: `docs/02-requirements/printing.md`
- Modify: `docs/02-requirements/card-reader.md`
- Reference: 对应模块代码

**Step 1: 搜索各模块错误码**

**Step 2: 分别插入错误码章节**

sync.md:
```markdown
## 错误码
| 场景 | HTTP | 触发条件 |
| 实体类型不支持 | 400 | 请求同步不支持的实体类型 |
| 同步冲突 | 409 | 双方数据不一致 |
| 引用检查失败 | 400 | 删除时有引用 |
```

printing.md:
```markdown
## 错误码
| 场景 | 触发条件 |
| 打印机未找到 | 指定打印机不存在 |
| 打印失败 | 打印机错误/纸张不匹配 |
| 处方数据不完整 | 缺少必要打印字段 |
```

card-reader.md:
```markdown
## 错误码
| 场景 | 触发条件 |
| 设备未连接 | IsConnected=false 时读卡 |
| 读卡失败 | 硬件通信错误 |
| 无卡片 | DetectCard 返回 false |
```

---

## Task 12: 细化全部9个PRD的验收标准

**Files:**
- Modify: `docs/02-requirements/auth.md`
- Modify: `docs/02-requirements/users.md`
- Modify: `docs/02-requirements/patients.md`
- Modify: `docs/02-requirements/herbs.md`
- Modify: `docs/02-requirements/formulas.md`
- Modify: `docs/02-requirements/medical-cases.md`
- Modify: `docs/02-requirements/sync.md`
- Modify: `docs/02-requirements/printing.md`
- Modify: `docs/02-requirements/card-reader.md`
- Reference: `tests/` 目录下对应测试文件

**Step 1: 搜索测试文件映射**

对每个模块，搜索对应的测试文件:
```
tests/LYBT.Tests.Unit/ (服务端单元测试)
tests/LYBT.Tests.Desktop.Unit/ (客户端单元测试)
tests/UnitTests/Server/Modules/ (模块单元测试)
```

**Step 2: 逐文档更新验收标准**

将:
```markdown
- [ ] 正确凭据返回 Token 和用户信息
```

改为:
```markdown
- [ ] 正确凭据 -> 返回 200 + AccessToken + RefreshToken + UserDetailDto
```

保持简洁但增加预期结果的具体性。每个验收标准确保包含:
1. 触发场景 (输入)
2. 预期结果 (输出/状态码/行为)

---

## Task 13: vision.md 补充版本路线图

**Files:**
- Modify: `docs/01-product/vision.md`
- Reference: 全部 PRD 中的"决策记录"章节 (v2.0 相关条目)

**Step 1: 收集所有 v2.0 规划条目**

从各模块决策记录中提取:
- auth.md: (无 v2.0 条目)
- sync.md: MedicalCase 同步 (v2.0), 自动同步提示 + NetworkStatusService (v2.0)
- printing.md: PDF 导出 (v2.0), 诊所信息配置化 (v2.0)
- medical-cases.md: (无显式 v2.0)

**Step 2: 在 vision.md "系统边界" 后插入版本路线图**

```markdown
## 版本路线图

### v1.0 -- 核心诊疗流程 (当前)

**范围**: 120 个功能需求 (FR)，覆盖 14 个模块

**核心功能**:
- 完整的中医诊疗流程 (患者→医案→诊断→处方→打印)
- 四层角色权限体系
- 本地/远程双模式运行
- 基础数据同步 (药材/患者/验方)
- 身份证读卡器集成

### v2.0 -- 扩展与集成 (规划中)

| 功能 | 来源 | 说明 |
|------|------|------|
| MedicalCase 数据同步 | FR-SYNC 决策#3 | 聚合根多表级联同步 |
| PDF 处方导出 | FR-PRINT 决策#1 | PdfSharp 或 XPS→PDF 转换 |
| 自动同步提示 | FR-SYNC 决策#4 | NetworkStatusService + 状态栏指示器 |
| 诊所信息配置化 | FR-PRINT 决策#2 | 从 appsettings.json 或数据库读取 |
| 用户数据同步 | FR-SYNC 决策#2 | User 实体加入同步范围 |
```

---

## Task 14: user-roles.md 修正 Receptionist

**Files:**
- Modify: `docs/01-product/user-roles.md`

**Step 1: 修改角色定义表**

将:
```
| 前台接待 | Receptionist | 0 | 患者登记、预约管理 |
```

改为:
```
| 前台接待 | Receptionist | 0 | v1.0 仅查看权限。不在 DoctorOrAdmin/AdminOnly 策略中，无任何写操作权限 |
```

**Step 2: 检查权限矩阵一致性**

确认 Receptionist 行在所有模块都标记为 "禁止" 或 "无权限"。

---

## Task 15: 更新 02-requirements/README.md

**Files:**
- Modify: `docs/02-requirements/README.md`

**Step 1: 新增 5 个模块到索引表**

在现有 9 个模块后追加:

```markdown
| 系统健康与诊断 | [health-diagnostics.md](health-diagnostics.md) | FR-SYS-001 ~ 007 | 7 |
| 异常处理策略 | [error-handling.md](error-handling.md) | FR-ERR-001 ~ 005 | 5 |
| 日志与审计 | [logging.md](logging.md) | FR-LOG-001 ~ 004 | 4 |
| Desktop Shell | [desktop-shell.md](desktop-shell.md) | FR-SHELL-001 ~ 007 | 7 |
| 配置参数 | [configuration.md](configuration.md) | FR-CFG-001 ~ 003 | 3 |
```

**Step 2: 更新总计**

从 `> **总计: 94 个功能需求**` 改为 `> **总计: 120 个功能需求**`

**Step 3: 更新 FR 编号规则表**

在模块缩写中追加:
```
| 模块缩写 | SYS / ERR / LOG / SHELL / CFG (新增) |
```

---

## Task 16: 全文档交叉验证

**Step 1: 检查 FR 编号无冲突**

使用 Grep 搜索所有 `docs/02-requirements/` 中的 FR- 编号:
```
Grep: "FR-[A-Z]+-\d{3}" in docs/02-requirements/
```

验证:
- 无重复编号
- 各模块编号连续无跳号

**Step 2: 检查内部链接**

验证 README.md 中所有 `[xxx.md](xxx.md)` 链接指向存在的文件。

**Step 3: 验证角色权限一致性**

对比 `01-product/user-roles.md` 中的权限矩阵与各模块 PRD 中的"用户角色"章节，确保一致。

---

## Task 17: 最终审查与提交准备

**Step 1: 读取所有新建和修改的文件列表**

```bash
git status
git diff --stat
```

**Step 2: 验证文档格式统一**

检查所有 17 个文件:
- 标题层级一致
- 表格格式正确
- 变更记录日期为 2026-02-11

**Step 3: 报告完成状态**

汇总:
- 新增文件数
- 修改文件数
- 新增 FR 总数
- 报告任何发现的问题

---

## 依赖关系

```
Task 1~5 (新建5个PRD) -- 可并行
  ↓
Task 6~11 (错误码补全) -- 可并行，Task 2 的 FR-ERR-005 需要这些错误码
  ↓
Task 12 (验收标准细化) -- 依赖 Task 1~11 完成
  ↓
Task 13~14 (产品层修正) -- 可并行
  ↓
Task 15 (README更新) -- 依赖 Task 1~5 确认文件名和FR范围
  ↓
Task 16 (交叉验证) -- 依赖全部完成
  ↓
Task 17 (最终审查) -- 最后执行
```

## 执行建议

- **Phase 1 (Task 1~5)**: 5个新PRD可并行编写
- **Phase 2 (Task 6~11)**: 6个错误码补全可并行
- **Phase 3 (Task 12~15)**: 验收标准+产品层修正
- **Phase 4 (Task 16~17)**: 验证与审查

---

创建时间: 2026-02-11
设计文档: docs/plans/2026-02-11-prd-completion-design.md
