# Sysadmin 运维控制台设计

## [S1] 问题

sysadmin（系统运维）目前共用 Admin 的业务管理界面，无法体现运维职能。需要专属运维控制台。

## [S2] 用户画像

sysadmin 是独立用户（非角色），负责：
- 系统正常运行监控
- admin 账号生命周期管理
- 故障排查（日志/性能/会话）
- 系统配置查看

## [S3] 控制台架构

### 首页：运维仪表盘

| 卡片 | 数据源 | 展示 |
|------|--------|------|
| API 状态 | `/api/v1/health` | 在线/离线 + 响应时间 |
| 数据库连接 | `/api/v1/diagnostics/db-status` | 连接正常/异常 + 连接池使用率 |
| 今日登录数 | `/api/v1/diagnostics/login-stats` | 数字 + 趋势 |
| 活跃用户 | `/api/v1/diagnostics/active-sessions` | 在线用户列表 |
| 系统信息 | 静态 | 版本号、环境、启动时间 |

### 用户管理（仅 admin）

sysadmin 可执行：
- 创建 admin 账号
- 重置 admin 密码
- 锁定/解锁 admin 账号
- 查看 admin 登录历史

sysadmin 不可管理 doctor/receptionist（由 admin 管理）。

### 日志级别控制

| 功能 | API |
|------|-----|
| 查看当前级别 | GET `/api/v1/diagnostics/logging/status` |
| 开启 Debug 模式 | POST `/api/v1/diagnostics/logging/debug/enable` |
| 关闭 Debug 模式 | POST `/api/v1/diagnostics/logging/debug/disable` |
| 手动设置级别 | POST `/api/v1/diagnostics/logging/level` |

### 操作审计日志

记录 sysadmin 的所有操作：
- 用户管理（创建/重置/锁定）
- 日志级别变更
- 配置查看
- 登录/登出

## [S4] UI 设计

- **主题**：深色运维风格（#1a1a2e 背景、#16213e 卡片、#0f3460 强调色）
- **布局**：顶部状态栏 + 左侧导航 + 右侧内容区
- **首页**：2×3 状态卡片网格
- **数据更新**：30 秒轮询

## [S5] v1 范围（最小可用）

1. 运维仪表盘（4 个状态卡片）
2. admin 用户管理（复用现有 UserMasterDetail，筛选 admin 角色）
3. 日志级别控制（调用已有 DiagnosticsController 端点）

## [S6] 后续迭代

- 日志查看器（实时流式日志）
- 性能监控（CPU/内存/响应时间图表）
- 会话管理（在线用户列表 + 强制下线）
- 诊所信息配置
- 数据库备份配置
