# 错误日志定位工作流（US-ERR-009）
> 版本: v1.0 | 日期: 2026-08-20

> 2026-08-13 架构审计产出——日志/错误统一管理（Shared.Logging + Shared.ExceptionHandling）的**使用者指南**：
> 服务器报错时按此工作流定位，无需读代码即可缩小根因范围。

## 日志文件位置

| 环境 | 路径 | 说明 |
|------|------|------|
| Server（生产/测试） | 工作目录 `logs/lybt-web-api-YYYYMMDD.log` | 主应用日志（按天滚动，保留 30 天，单文件 10MB） |
| Server 启动阶段 | 工作目录 `logs/bootstrap-YYYYMMDD.log` | 两阶段日志（Bootstrap Logger——启动早期异常也记录） |
| Server 归档（F-08） | 工作目录 `logs/archive/{前缀}-YYYY-MM.zip` | 已结束月份且超 7 天的日志按月打包（归档后源文件删除；`Logging:Archive` 控制） |
| Desktop | 应用数据目录 `logs/` | 桌面端日志（同名文件，`shared: true` 多进程共享） |

> **单实例日志文件（US-LOG-009）**：所有 File sink 启用 `shared: true`——多进程写同一文件（不产生 `_001` 后缀）。
> 同日多次重启/多进程并存时日志仍在同一文件——按 `[Timestamp]` + `pid` 区分进程。

## 定位工作流（四步）

### ① 启动首行看版本（代码新旧——部署验证第一步）

```bash
head -1 logs/bootstrap-YYYYMMDD.log
# 预期: [启动] LYBT.WebAPI v0.0.1+21cc2ecbe... (commit 21cc2ecbe...) env=Production pid=12345
```

- 版本含 `+commit SHA`（SDK 注入）——**对照服务器日志 vs 本地 publish 版本**
- 部署后先看这行：**commit 不是预期 SHA = 部署了旧代码**（排查「代码改了但行为没变」的第一步）
- `pid` 区分多进程（start.sh 重启后 pid 变化属正常）

### ② CorrelationId 关联（跨模块追踪一次请求）

```bash
grep "correlationId\|CorrelationId" logs/lybt-web-api-20260813.log | tail -5
# 找到出错请求的 CorrelationId（响应头 x-ms-correlation-id 同一值）
grep "a1b2c3d4-..." logs/lybt-web-api-20260813.log
# 同一请求在 Controller → Handler → Repository → DbContext 的全部日志行
```

- 每次请求一个 CorrelationId（`UseLybtCorrelationId` 中间件生成/透传）
- 客户端/前端报错时的 `requestId`/`correlationId` → 服务器日志同一值过滤

### ③ 异常多行看 `--->` 内层（根因——最里层才是真因）

```bash
grep -A 30 "数据已被其他用户修改\|DbUpdateConcurrency" logs/lybt-web-api-20260813.log
# 异常链渲染（@x）含 InnerException——从最内层 `--->` 后的类型开始读
```

- Serilog 异常链 `{Exception}` 完整渲染：`Exception A` → `---> Exception B` → `---> Exception C`（最内层 = 根因）
- 例：`InvalidOperationException: 数据已被其他用户修改` 的 `---> DbUpdateConcurrencyException: expected 1 row, affected 0`——根因在 `--->` 之后
- 栈帧 `at ...` 行给出精确代码位置（文件+行号）

### ④ 当前日志文件（确认在写哪个文件）

```bash
ls -lt logs/ | head -3
# 最新文件 = 当前活跃日志（lybt-web-api-YYYYMMDD.log）
tail -f logs/lybt-web-api-$(date +%Y%m%d).log
# 实时跟踪
```

- 时间不对（无今日文件）= 进程未写日志（启动失败/权限/路径问题）→ 查 `bootstrap-*.log`
- 同天 `_001` 后缀文件 = 旧配置（无 `shared: true`）残留——新版本不再产生（残留文件仍会被归档，见上表）
- **查历史月份日志**：源文件已被归档删除 → 从 `logs/archive/{前缀}-YYYY-MM.zip` 解包（`unzip -p logs/archive/lybt-web-api-202607.zip lybt-web-api-20260715.log | grep ...`）；当前月与 7 天内的文件仍在 `logs/` 顶层

## 生产屏蔽不破坏日志诊断

| 屏蔽项 | 日志影响 | 诊断仍可用 |
|--------|---------|-----------|
| `EnableSensitiveDataLogging(false)` | SQL 参数值不记录 | SQL 语句文本/异常照常（`expected 1 row, affected 0` 等） |
| 脱敏策略（SensitiveDataDestructuringPolicy） | 密码/密钥/token 值替换为 `***` | 消息结构/异常链完整 |
| 生产 `EnableDetailedErrors(false)` | 无 EF 详细错误 | 应用异常链（`--->` 内层）完整 |
| 生产环境异常响应「unexpected error」 | 响应体不泄露堆栈 | **日志完整记录堆栈**——诊断看日志不看响应 |

> 原则：**屏蔽的是「值」和「响应体」，不是「结构」和「日志」**——异常链、CorrelationId、调用栈始终完整。
