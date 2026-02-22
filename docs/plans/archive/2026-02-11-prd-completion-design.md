# PRD文档全面补全设计

## 概述

基于代码实现与PRD文档的系统性对比分析，识别出4大类12项差距，本方案完整覆盖所有差距。

---

## 差距分析

### A. 缺失的PRD模块 (代码已实现但无PRD)

| # | 功能模块 | 代码位置 | 差距说明 |
|---|---------|---------|---------|
| 1 | 健康检查 | HealthController (3端点) | 基础/Ping/详细健康检查无PRD |
| 2 | 系统诊断 | DiagnosticsController (4端点) | 运行时日志级别管理无PRD |

### B. 缺失的非功能需求文档

| # | NFR类别 | 代码位置 | 差距说明 |
|---|--------|---------|---------|
| 3 | 异常处理 | LYBT.Shared.ExceptionHandling | 服务端+客户端分级异常处理无PRD |
| 4 | 日志体系 | LYBT.Shared.Logging + SecurityAuditLog | CorrelationId/脱敏/动态级别无PRD |
| 5 | Desktop Shell | Shell/Services/ | 启动/导航/对话框/菜单无PRD |
| 6 | 配置参数 | 散落在各模块 appsettings | 无统一索引 |

### C. 现有PRD内容质量问题

| # | 问题 | 影响范围 |
|---|------|---------|
| 7 | 验收标准仅占位符 | 全部9个PRD |
| 8 | 错误码不完整 | auth.md外的8个PRD |
| 9 | 缺少版本路线图 | vision.md |
| 10 | Receptionist角色描述不准确 | user-roles.md |

### D. 产品层缺口

| # | 问题 | 说明 |
|---|------|------|
| 11 | 缺少v1.0/v2.0路线图 | 散落在各模块决策记录中的v2.0规划未汇总 |
| 12 | Receptionist描述为"患者登记、预约管理" | 代码中几乎无写权限，描述失真 |

---

## 设计决策

| # | 决策 | 结论 |
|---|------|------|
| 1 | NFR文档位置 | 全部放 `docs/02-requirements/`，与功能需求并列 |
| 2 | 编号体系 | 统一使用 FR-xxx-NNN，不区分 FR/NFR |
| 3 | 详细程度 | 全部采用完整PRD格式 (概述/角色/功能清单/数据模型/决策/变更) |
| 4 | 新模块前缀 | SYS(系统)/ERR(异常)/LOG(日志)/SHELL(Shell)/CFG(配置) |

---

## 新增文档清单

### 1. `health-diagnostics.md` -- 系统健康与诊断

| FR编号 | 功能 | 描述 |
|--------|------|------|
| FR-SYS-001 | 基础健康检查 | GET `/api/v1/health` 匿名访问，返回 Healthy/Unhealthy + 版本信息 |
| FR-SYS-002 | Ping端点 | GET `/api/v1/health/ping` 匿名，返回 pong + 时间戳 |
| FR-SYS-003 | 详细健康检查 | GET `/api/v1/health/details` 需认证，含数据库连接+迁移状态+模块健康 |
| FR-SYS-004 | 获取日志级别状态 | GET `/api/v1/diagnostics/logging/status` 仅SuperAdmin |
| FR-SYS-005 | 启用调试模式 | POST `/api/v1/diagnostics/logging/debug/enable` 临时提升1-120分钟 |
| FR-SYS-006 | 禁用调试模式 | POST `/api/v1/diagnostics/logging/debug/disable` 恢复默认 |
| FR-SYS-007 | 设置日志级别 | POST `/api/v1/diagnostics/logging/level` 手动设置特定级别 |

### 2. `error-handling.md` -- 异常处理策略

| FR编号 | 功能 | 描述 |
|--------|------|------|
| FR-ERR-001 | 服务端全局异常处理 | BusinessException + SystemException 分级处理 |
| FR-ERR-002 | ProblemDetails标准化 | RFC 7807 标准错误响应格式 |
| FR-ERR-003 | 客户端异常处理 | DesktopExceptionHandler 用户友好提示 |
| FR-ERR-004 | 异常严重度分级 | Low/Medium/High/Critical 四级 |
| FR-ERR-005 | 全局错误码注册表 | 统一索引所有模块的 ErrorCode |

### 3. `logging.md` -- 日志与审计体系

| FR编号 | 功能 | 描述 |
|--------|------|------|
| FR-LOG-001 | 结构化日志 | Serilog + CorrelationId 端到端追踪 |
| FR-LOG-002 | 安全审计日志 | SecurityAuditLog: 登录/登出/Token事件追踪 |
| FR-LOG-003 | 敏感数据脱敏 | SensitiveDataAttribute 自动遮蔽 |
| FR-LOG-004 | 运行时日志级别管理 | LoggingLevelManager 动态调整 |

### 4. `desktop-shell.md` -- Desktop Shell 基础设施

| FR编号 | 功能 | 描述 |
|--------|------|------|
| FR-SHELL-001 | 应用启动流水线 | StartupPipeline 5步启动 |
| FR-SHELL-002 | 会话生命周期管理 | 登录→工作台→登出完整生命周期 |
| FR-SHELL-003 | 页面导航系统 | Region导航、返回栈、参数传递 |
| FR-SHELL-004 | 菜单系统 | 角色感知菜单、动态显示/隐藏 |
| FR-SHELL-005 | 通用对话框体系 | Confirmation/Input/Message 三类 |
| FR-SHELL-006 | 启动诊断与监控 | StartupDiagnostics + 性能计时 |
| FR-SHELL-007 | 账户设置 | AccountSettingsControl: 修改密码/个人资料 |

### 5. `configuration.md` -- 配置参数总表

| FR编号 | 功能 | 描述 |
|--------|------|------|
| FR-CFG-001 | 服务端配置参数 | JWT/Session/Security/RateLimiting/Database 统一索引 |
| FR-CFG-002 | 客户端配置参数 | 连接模式/API地址/超时/打印机/读卡器配置 |
| FR-CFG-003 | 环境配置管理 | appsettings.json 分环境覆盖策略 |

---

## 现有文档修改计划

### 错误码补全 (8个文档)

为以下文档新增"错误码"章节:

| 文档 | 需补充的错误码 |
|------|--------------|
| users.md | UserNotFound/DuplicateUsername/CannotDeleteSelf/LastAdminProtection 等 |
| patients.md | PatientNotFound/DuplicateIdNumber/HasReferences 等 |
| herbs.md | HerbNotFound/DuplicateName/HasReferences 等 |
| formulas.md | FormulaNotFound/NoPermission/HasReferences 等 |
| medical-cases.md | CaseNotFound/PermissionDenied/EditReasonRequired/InvalidStatus 等 |
| sync.md | SyncFailed/ConflictDetected/ReferenceCheck 等 |
| printing.md | PrinterNotFound/PrintFailed 等 |
| card-reader.md | DeviceNotConnected/ReadFailed 等 |

### 验收标准细化 (9个文档)

将所有 `- [ ] xxx` 格式的验收标准从简单描述改为:
```
- [ ] [场景描述] -> [预期结果] (对应测试: [测试方法名或测试文件])
```

### 产品层修改

**vision.md -- 版本路线图**:
- v1.0 Scope: 明确列出包含的94个FR + 26个新FR
- v2.0 规划: 汇总各模块决策记录中的v2.0条目 (MedicalCase同步/PDF导出/自动同步提示/诊所配置化)

**user-roles.md -- Receptionist描述修正**:
- 从 "患者登记、预约管理" 修正为 "仅查看权限 (患者列表、药材列表)。v1.0不具备写操作权限"

### README.md 更新

新增5个模块索引:
```
| 系统健康与诊断 | health-diagnostics.md | FR-SYS-001 ~ 007 | 7 |
| 异常处理策略 | error-handling.md | FR-ERR-001 ~ 005 | 5 |
| 日志与审计 | logging.md | FR-LOG-001 ~ 004 | 4 |
| Desktop Shell | desktop-shell.md | FR-SHELL-001 ~ 007 | 7 |
| 配置参数 | configuration.md | FR-CFG-001 ~ 003 | 3 |
```

总计更新为: **14个模块 / 120个功能需求**

---

## 产出汇总

| 操作 | 数量 |
|------|------|
| 新增 PRD 文档 | 5个 |
| 新增 FR 编号 | 26个 |
| 修改现有 PRD | 9个 (错误码+验收标准) |
| 修改产品层文档 | 2个 (vision.md + user-roles.md) |
| 修改需求 README | 1个 |
| **总文件变更** | **17个** |

---

创建时间: 2026-02-11
状态: 已确认
