# LYBTZYZS API 端点
> 版本: v1.2 | 日期: 2026-09-22

> 由 [13-project-master-plan.md §三](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03）。端点按模块组织。
>
> **权限权威**：权限矩阵 SSOT 见 [04-permissions.md](../01-product/04-permissions.md)（见 [ADR-0026](decisions/0026-authorization-matrix-ssot.md)）；架构层速查见 [12-permissions-matrix.md](12-permissions-matrix.md)。本文件不再维护权限列。
>
> **权威定义**：逐端点请求/响应/错误码等详细说明见 [04-api-reference/](../04-api-reference/README.md)。

## 3.1 认证授权 (Auth) — `api/v1/auth`

| 方法 | 端点 | 功能 |
|------|------|------|
| POST | /login | 用户名密码登录 |
| POST | /logout | 登出 |
| POST | /refresh | Token 刷新 |
| POST | /auto-login | 自动登录 |
| GET | /validate | Token 验证 |

## 3.2 用户管理 (Users) — `api/v1/users`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 用户列表（分页） |
| GET | /{id} | 用户详情 |
| POST | / | 创建用户 |
| PUT | /{id} | 更新用户 |
| DELETE | /{id} | 删除用户（软删除） |
| POST | /{id}/toggle-status | 启用/禁用 |
| POST | /{id}/restore | 恢复已删除 |
| POST | /batch-delete | 批量删除 |
| POST | /batch-enable | 批量启用 |
| POST | /batch-disable | 批量禁用 |
| POST | /{id}/reset-password | 重置密码 |
| GET | /current | 当前用户信息 |
| PUT | /{id}/profile | 修改个人资料 |
| PUT | /{id}/change-password | 修改密码 |

## 3.3 患者管理 (Patients) — `api/v1/patients`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 患者列表（分页） |
| GET | /{id} | 患者详情 |
| POST | / | 创建患者 |
| PUT | /{id} | 更新患者 |
| DELETE | /{id} | 删除患者（软删除） |
| POST | /{id}/toggle-status | 启用/禁用 |
| POST | /{id}/restore | 恢复已删除 |
| POST | /batch-delete | 批量删除 |
| POST | /batch-import | 批量导入（JSON） |
| GET | /{id}/check-reference | 检查引用关系 |
| POST | /batch-check-reference | 批量检查引用 |
| GET | /by-id-number/{idNumber} | 身份证号查询 |

## 3.4 药材管理 (Herbs) — `api/v1/herbs`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 药材列表（分页） |
| GET | /{id} | 药材详情 |
| POST | / | 创建药材 |
| PUT | /{id} | 更新药材 |
| DELETE | /{id} | 删除药材（软删除） |
| POST | /{id}/toggle-status | 启用/禁用 |
| POST | /{id}/restore | 恢复已删除 |
| POST | /batch-delete | 批量删除 |
| POST | /batch-import | 批量导入（JSON） |
| GET | /{id}/check-reference | 检查引用关系 |
| POST | /batch-check-reference | 批量检查引用 |
| POST | /batch-enable | 批量启用 |
| POST | /batch-disable | 批量禁用 |

## 3.5 验方管理 (Formulas) — `api/v1/formulas`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 验方列表（分页） |
| GET | /{id} | 验方详情 |
| POST | / | 创建验方 |
| PUT | /{id} | 更新验方 |
| DELETE | /{id} | 删除验方（软删除） |
| POST | /{id}/toggle-status | 启用/禁用 |
| POST | /{id}/restore | 恢复已删除 |
| POST | /batch-delete | 批量删除 |
| POST | /batch-import | 批量导入（JSON） |
| GET | /pending-validation | 待校验验方列表 |
| POST | /{formulaId}/herbs/{herbItemId}/validate | 校验药材匹配 |
| POST | /batch-enable | 批量启用 |
| POST | /batch-disable | 批量禁用 |

## 3.6 医案管理 (MedicalCases) — `api/v1/medicalcases`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 医案列表（分页） |
| GET | /{id} | 医案详情 |
| POST | / | 创建医案 |
| PUT | /{id} | 更新医案 |
| DELETE | /{id} | 删除医案 |
| POST | /batch-delete | 批量删除 |
| PUT | /{id}/close | 完成医案 |
| PUT | /{id}/suspend | 挂起医案 |
| PUT | /{id}/cancel | 取消医案 |
| PUT | /{id}/status | 更新状态 |
| PUT | /{id}/prescription-flag | 标记处方需求 |
| PUT | /{id}/print-completed | 记录打印完成 |
| GET | /{id}/consultations | 辨证记录列表 |
| GET | /{id}/prescriptions | 处方列表 |
| GET | /patient/{id}/consultations | 患者辨证历史 |
| GET | /patient/{id}/prescriptions | 患者处方历史 |
| POST | /batch-details | 批量查询详情（≤50） |
| GET | /search | 跨医案搜索 |
| GET | /query | 统一查询端点 |
| GET | /{id}/permissions | 操作权限查询 |
| GET | /{id}/audit-logs | 审计日志 |

## 3.7 挂号管理 (Registrations) — `api/v1/registrations`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 挂号列表（分页+筛选） |
| GET | /{id} | 挂号详情 |
| POST | / | 创建挂号 |
| PUT | /{id}/start-visit | 接诊 |
| PUT | /{id}/cancel | 取消挂号 |
| GET | /queue | 等待队列 |

> `POST /quick-visit` 已删除（QuickVisit 端点收敛，2026-08-14）；接诊统一走 `PUT /{id}/start-visit`。

## 3.8 统计报表 (Reports) — `api/v1/reports`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | /daily/income | 日收入统计 |
| GET | /daily/consultations | 日就诊统计（字段名保留 `consultations`） |
| GET | /daily/herbs | 日药材使用统计 |
| GET | /trend/income | 收入趋势（granularity=day/week/month，默认最近 30 天） |
| GET | /trend/consultations | 就诊趋势（granularity=day/week/month，默认最近 30 天） |
| GET | /doctor-performance | 医生绩效（就诊数/挂号费/药费/平均处方金额） |
| GET | /herbs/ranking | 热门药材排行（top 默认 10） |
| GET | /patient-flow | 患者流量（新/回头患者，granularity=day/week/month，默认最近 30 天） |

## 3.9 系统配置 (Configuration) — `api/v1/configuration`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 获取全部配置 |
| GET | /{key} | 获取单个配置 |
| PUT | /{key} | 修改单个配置（白名单校验 + 持久化 + 热更新） |
| PUT | / | 批量修改配置（白名单校验 + 持久化 + 热更新） |
| POST | /validate | 生产环境验证 |

## 3.10 诊断调试 (Diagnostics) — `api/v1/diagnostics`

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | /logging/status | 日志级别状态 |
| POST | /logging/debug/enable | 启用调试模式 |
| POST | /logging/debug/disable | 禁用调试模式 |
| POST | /logging/level | 设置日志级别 |

## 3.11 部署管理 (Deploy) — `api/v1/deploy`

| 方法 | 端点 | 功能 |
|------|------|------|
| POST | /upload | 上传更新包 |
| POST | /restart | 重启服务 |
| ~~GET~~ | ~~/version~~ | ~~版本检查~~（**❌ 缺失**） |

## 3.12 数据备份/恢复 (Backup) — `api/v1/backup`

> 双端同路由（远程 `LYBT.WebAPI` 与本地 `LYBT.LocalWebAPI` 均继承 `BaseBackupController`；B-06 / US-SHELL-013）。共享引擎 `IBackupService`（`SqlServerBackupService`）：远程对 SQL Server、本地对 LocalDB。权限：类级 `[Authorize]`（仅要求已认证）+ 管理操作逐方法 `SysAdminOnly`，`POST /auto` 为登录触发的自动备份（仅需认证）。详细请求/响应见 [04-api-reference/15-backup.md](../04-api-reference/15-backup.md)。

| 方法 | 端点 | 功能 |
|------|------|------|
| GET | / | 备份文件列表（按备份时间倒序） |
| GET | /status | 备份状态（上次备份/文件数/总大小/目录/保留天数/进行中作业与进度） |
| GET | /tables | 可选择性恢复的表清单（含记录数与是否支持记录级选择） |
| POST | / | 创建备份（全量/差异，可选压缩与文件级加密） |
| POST | /{id}/restore | 恢复指定备份（整库覆盖或按表/记录选择性回写；默认恢复前自动保护性备份） |
| DELETE | /{id} | 删除备份文件（被差异备份引用的全量拒绝删除） |
| POST | /cleanup | 清理超过保留期的备份（保护最新全量与差异链） |
| POST | /auto | 自动备份（登录后触发；距上次备份未满间隔时为空操作） |
