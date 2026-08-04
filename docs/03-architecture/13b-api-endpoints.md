# LYBTZYZS API 端点

> 由 [13-project-master-plan.md §三](13-project-master-plan.md) 拆出（2026-08-04 规则体系优化 E-03），内容原样迁移，不改变定义。端点按模块组织，权限均为操作级（详细矩阵见 [12-permissions-matrix.md](12-permissions-matrix.md)）。

## 3.1 认证授权 (Auth) — `api/v1/auth`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| POST | /login | 用户名密码登录 | 匿名 |
| POST | /logout | 登出 | 匿名 |
| POST | /refresh | Token 刷新 | 匿名 |
| POST | /auto-login | 自动登录 | 匿名 |
| GET | /validate | Token 验证 | 需认证 |

## 3.2 用户管理 (Users) — `api/v1/users`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 用户列表（分页） | Admin+ |
| GET | /{id} | 用户详情 | Admin+ |
| POST | / | 创建用户 | Admin+ |
| PUT | /{id} | 更新用户 | Admin+ |
| DELETE | /{id} | 删除用户（软删除） | Admin+ |
| POST | /{id}/toggle-status | 启用/禁用 | Admin+ |
| POST | /{id}/restore | 恢复已删除 | Admin+ |
| POST | /batch-delete | 批量删除 | Admin+ |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |
| POST | /{id}/reset-password | 重置密码 | Admin+ |
| GET | /current | 当前用户信息 | 需认证 |
| PUT | /{id}/profile | 修改个人资料 | 需认证 |
| PUT | /{id}/change-password | 修改密码 | 需认证 |

## 3.3 患者管理 (Patients) — `api/v1/patients`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 患者列表（分页） | Doctor/Receptionist |
| GET | /{id} | 患者详情 | Doctor/Receptionist |
| POST | / | 创建患者 | Doctor/Receptionist |
| PUT | /{id} | 更新患者 | Doctor/Receptionist |
| DELETE | /{id} | 删除患者（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| GET | /{id}/check-reference | 检查引用关系 | Doctor/Receptionist |
| POST | /batch-check-reference | 批量检查引用 | Doctor/Receptionist |
| GET | /by-id-number/{idNumber} | 身份证号查询 | Doctor/Receptionist |

## 3.4 药材管理 (Herbs) — `api/v1/herbs`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 药材列表（分页） | Doctor/Receptionist |
| GET | /{id} | 药材详情 | Doctor/Receptionist |
| POST | / | 创建药材 | Doctor/Receptionist |
| PUT | /{id} | 更新药材 | Doctor/Receptionist |
| DELETE | /{id} | 删除药材（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| POST | /batch-import | 批量导入（JSON） | Doctor/Receptionist |
| GET | /{id}/check-reference | 检查引用关系 | Doctor/Receptionist |
| POST | /batch-check-reference | 批量检查引用 | Doctor/Receptionist |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |

## 3.5 验方管理 (Formulas) — `api/v1/formulas`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 验方列表（分页） | Doctor/Receptionist |
| GET | /{id} | 验方详情 | Doctor/Receptionist |
| POST | / | 创建验方 | Doctor/Receptionist |
| PUT | /{id} | 更新验方 | Doctor/Receptionist |
| DELETE | /{id} | 删除验方（软删除） | Doctor/Receptionist |
| POST | /{id}/toggle-status | 启用/禁用 | Doctor/Receptionist |
| POST | /{id}/restore | 恢复已删除 | Doctor/Receptionist |
| POST | /batch-delete | 批量删除 | Doctor/Receptionist |
| POST | /batch-import | 批量导入（JSON） | Doctor/Receptionist |
| GET | /pending-validation | 待校验验方列表 | Doctor/Receptionist |
| POST | /{formulaId}/herbs/{herbItemId}/validate | 校验药材匹配 | Doctor/Receptionist |
| POST | /batch-enable | 批量启用 | Admin+ |
| POST | /batch-disable | 批量禁用 | Admin+ |

## 3.6 医案管理 (MedicalCases) — `api/v1/medicalcases`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 医案列表（分页） | Doctor/Receptionist |
| GET | /{id} | 医案详情 | Doctor/Receptionist |
| POST | / | 创建医案 | Doctor |
| PUT | /{id} | 更新医案 | Doctor |
| DELETE | /{id} | 删除医案 | Doctor/Admin |
| POST | /batch-delete | 批量删除 | Doctor/Admin |
| PUT | /{id}/close | 完成医案 | Doctor |
| PUT | /{id}/suspend | 挂起医案 | Doctor |
| PUT | /{id}/cancel | 取消医案 | Doctor |
| PUT | /{id}/status | 更新状态 | Doctor |
| PUT | /{id}/prescription-flag | 标记处方需求 | Doctor |
| PUT | /{id}/print-completed | 记录打印完成 | Doctor |
| GET | /{id}/consultations | 辨证记录列表 | Doctor/Receptionist |
| GET | /{id}/prescriptions | 处方列表 | Doctor/Receptionist |
| GET | /patient/{id}/consultations | 患者辨证历史 | Doctor/Receptionist |
| GET | /patient/{id}/prescriptions | 患者处方历史 | Doctor/Receptionist |
| POST | /batch-details | 批量查询详情（≤50） | Doctor/Receptionist |
| GET | /search | 跨医案搜索 | Doctor/Receptionist |
| GET | /query | 统一查询端点 | Doctor/Receptionist |
| GET | /{id}/permissions | 操作权限查询 | Doctor/Receptionist |
| GET | /{id}/audit-logs | 审计日志 | Doctor/Receptionist |

## 3.7 挂号管理 (Registrations) — `api/v1/registrations`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 挂号列表（分页+筛选） | Doctor/Receptionist |
| GET | /{id} | 挂号详情 | Doctor/Receptionist |
| POST | / | 创建挂号 | Doctor/Receptionist |
| PUT | /{id}/start-visit | 接诊 | Doctor/Receptionist |
| PUT | /{id}/cancel | 取消挂号 | Doctor/Receptionist |
| GET | /queue | 等待队列 | Doctor/Receptionist |
| POST | /quick-visit | 医生快速看诊 | Doctor |

## 3.8 统计报表 (Reports) — `api/v1/reports`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | /daily/income | 日收入统计 | Doctor/Admin |
| GET | /daily/consultations | 日问诊统计 | Doctor/Admin |
| GET | /daily/herbs | 日药材使用统计 | Doctor/Admin |

## 3.9 系统配置 (Configuration) — `api/v1/configuration`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | / | 获取全部配置 | Admin+ |
| GET | /{key} | 获取单个配置 | Admin+ |
| POST | /validate | 生产环境验证 | Admin+ |
| ~~PUT~~ | ~~/~~ | ~~修改配置~~ | **❌ 缺失** |

## 3.10 诊断调试 (Diagnostics) — `api/v1/diagnostics`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| GET | /logging/status | 日志级别状态 | Admin+ |
| POST | /logging/debug/enable | 启用调试模式 | Admin+ |
| POST | /logging/debug/disable | 禁用调试模式 | Admin+ |
| POST | /logging/level | 设置日志级别 | Admin+ |

## 3.11 部署管理 (Deploy) — `api/v1/deploy`

| 方法 | 端点 | 功能 | 权限 |
|------|------|------|------|
| POST | /upload | 上传更新包 | Admin+ |
| POST | /restart | 重启服务 | Admin+ |
| ~~GET~~ | ~~/version~~ | ~~版本检查~~ | **❌ 缺失** |
