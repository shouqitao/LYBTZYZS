# LocalWebAPI API 端点

## 概述

LocalWebAPI 提供 10 个控制器，共 112 个端点。所有端点位于 `http://127.0.0.1:{dynamicPort}/api/`。

端口号由 OS 动态分配，通过 `LocalWebApiHost.Port` 获取。

| 控制器 | 路由前缀 | 认证 | 端点数 |
|--------|----------|------|--------|
| Auth | /api/auth | 部分匿名 | 5 |
| Health | /api/health | 匿名 | 3 |
| Users | /api/users | [Authorize] | 14 |
| Patients | /api/patients | [Authorize] | 14 |
| Herbs | /api/herbs | [Authorize] | 17 |
| Formulas | /api/formulas | [Authorize] | 17 |
| Registrations | /api/registrations | [Authorize] | 9 |
| MedicalCases | /api/medicalcases | [Authorize] | 22 |
| Diagnostics | /api/diagnostics | 匿名 | 7 |
| Configuration | /api/configuration | 无 | 4 |

## 认证说明

- 大部分端点需要 JWT Bearer Token（`Authorization: Bearer {token}`）。
- `Health`、`Diagnostics` 控制器标记 `[AllowAnonymous]`，无需认证。
- `Auth` 控制器中 `login`、`auto-login` 为匿名端点；`refresh`、`validate`、`logout` 需要认证。
- `Configuration` 控制器未标记认证属性（公开访问）。

## 认证端点（Auth）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| POST | /api/auth/login | Anonymous | 登录 |
| POST | /api/auth/logout | Required | 登出 |
| POST | /api/auth/refresh | Required | 刷新 Token |
| GET | /api/auth/validate | Required | 验证 Token 有效性 |
| POST | /api/auth/auto-login | Anonymous | 自动登录 |

### POST /api/auth/login

**请求体:** `LoginRequest { UserName, Password }`

**成功响应 (200):** `{ Token, UserId, Username, Role }`

**失败响应 (401):** 用户名或密码错误

### POST /api/auth/auto-login

**请求体:** `AutoLoginRequest { UserName }`

**成功响应 (200):** `{ Token, UserId, Username, Role }`

**失败响应 (401):** 用户不存在

## 健康检查端点（Health）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/health | Anonymous | 基础健康检查 |
| GET | /api/health/ping | Anonymous | 简单存活检查 |
| GET | /api/health/details | Anonymous | 详细健康信息（DB 连接、版本、统计） |

### GET /api/health

**响应:**
```json
{ "status": "Healthy", "timestamp": "...", "database": "Connected" }
```

### GET /api/health/ping

**响应:**
```json
{ "status": "ok", "timestamp": "..." }
```

### GET /api/health/details

**响应:**
```json
{
  "status": "Healthy",
  "timestamp": "...",
  "version": "1.0.0-local",
  "database": { "connected": true, "provider": "...", "responseMs": 12 },
  "statistics": { "totalUsers": 3 }
}
```

## 用户端点（Users）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/users | Required | 用户列表（精简：Id, Username, Role） |
| GET | /api/users/{id} | Required | 用户详情（含 RealName, Status） |
| POST | /api/users | Required (Admin) | 创建用户 |
| PUT | /api/users/{id} | Required (Admin) | 更新用户 |
| DELETE | /api/users/{id} | Required | 软删除用户 |
| PUT | /api/users/{id}/change-password | Required | 修改密码 |
| POST | /api/users/{id}/toggle-status | Required | 切换启用/禁用状态 |
| POST | /api/users/{id}/restore | Required | 恢复已删除用户 |
| POST | /api/users/batch-delete | Required | 批量软删除 |
| POST | /api/users/batch-enable | Required | 批量启用 |
| POST | /api/users/batch-disable | Required | 批量禁用 |
| GET | /api/users/current | Required | 获取当前登录用户详情 |
| POST | /api/users/{id}/reset-password | Required (Admin) | 重置密码（返回临时密码） |
| PUT | /api/users/{id}/profile | Required | 修改用户资料（RealName, Phone, Email） |

## 患者端点（Patients）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/patients?keyword=&page=&pageSize= | Required | 患者列表（分页） |
| GET | /api/patients/{id} | Required | 患者详情 |
| POST | /api/patients | Required | 创建患者 |
| PUT | /api/patients/{id} | Required | 更新患者 |
| DELETE | /api/patients/{id} | Required | 软删除患者 |
| GET | /api/patients/by-id-number/{idNumber} | Required | 按身份证号查询（含已删除） |
| POST | /api/patients/batch-delete | Required | 批量软删除（检查医案引用） |
| POST | /api/patients/{id}/restore | Required | 恢复已删除患者 |
| POST | /api/patients/{id}/toggle-status | Required | 切换启用/禁用状态 |
| GET | /api/patients/export | Required | 导出全部患者 |
| GET | /api/patients/import-template | Required | 获取导入模板 |
| POST | /api/patients/import | Required | 批量导入患者 |
| GET | /api/patients/{id}/check-reference | Required | 检查医案引用 |
| POST | /api/patients/batch-check-reference | Required | 批量检查医案引用 |

## 药材端点（Herbs）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/herbs?keyword=&category= | Required | 药材列表（支持分类筛选） |
| GET | /api/herbs/{id} | Required | 药材详情 |
| POST | /api/herbs | Required | 创建药材 |
| PUT | /api/herbs/{id} | Required | 更新药材 |
| DELETE | /api/herbs/{id} | Required | 软删除药材 |
| POST | /api/herbs/batch-delete | Required | 批量软删除（检查处方引用） |
| POST | /api/herbs/batch-enable | Required | 批量启用 |
| POST | /api/herbs/batch-disable | Required | 批量禁用 |
| POST | /api/herbs/{id}/toggle-status | Required | 切换启用/禁用状态 |
| POST | /api/herbs/{id}/restore | Required | 恢复已删除药材 |
| GET | /api/herbs/export | Required | 导出全部药材 |
| GET | /api/herbs/import-template | Required | 获取导入模板 |
| POST | /api/herbs/batch-import | Required | 批量导入药材（支持更新） |
| GET | /api/herbs/export-all?category= | Required | 导出全部药材（含分类筛选） |
| GET | /api/herbs/categories | Required | 获取所有分类列表 |
| GET | /api/herbs/{id}/check-reference | Required | 检查处方引用 |
| POST | /api/herbs/batch-check-reference | Required | 批量检查处方引用 |

## 验方端点（Formulas）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/formulas?keyword=&category= | Required | 验方列表 |
| GET | /api/formulas/{id} | Required | 验方详情 |
| POST | /api/formulas | Required | 创建验方 |
| PUT | /api/formulas/{id} | Required | 更新验方 |
| DELETE | /api/formulas/{id} | Required | 软删除验方 |
| POST | /api/formulas/batch-delete | Required | 批量软删除 |
| POST | /api/formulas/batch-enable | Required | 批量启用 |
| POST | /api/formulas/batch-disable | Required | 批量禁用 |
| POST | /api/formulas/{id}/toggle-status | Required | 切换状态 |
| POST | /api/formulas/{id}/restore | Required | 恢复已删除 |
| POST | /api/formulas/{id}/clone | Required | 克隆验方（含药材项） |
| GET | /api/formulas/export | Required | 导出（含药材项） |
| GET | /api/formulas/import-template | Required | 导入模板 |
| POST | /api/formulas/batch-import | Required | 批量导入 |
| GET | /api/formulas/pending-validation | Required | 获取待验证验方列表 |
| POST | /api/formulas/{formulaId}/herbs/{herbItemId}/validate | Required | 验证验方药材项 |
| GET | /api/formulas/categories | Required | 分类列表 |

## 挂号端点（Registrations）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/registrations?date= | Required | 挂号列表（按日期筛选） |
| GET | /api/registrations/{id} | Required | 挂号详情 |
| POST | /api/registrations | Required | 创建挂号 |
| PUT | /api/registrations/{id} | Required | 更新挂号 |
| DELETE | /api/registrations/{id} | Required | 软删除挂号 |
| GET | /api/registrations/queue?doctorId= | Required | 获取候诊队列 |
| PUT | /api/registrations/{id}/start-visit | Required | 接诊（状态→InProgress） |
| PUT | /api/registrations/{id}/cancel | Required | 取消挂号 |
| POST | /api/registrations/quick-visit | Required | 快速就诊（创建挂号+医案） |

## 医案端点（MedicalCases）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/medicalcases?status=&patientId=&page=&pageSize=&includeAllDoctors=&keyword= | Required | 医案列表（分页） |
| GET | /api/medicalcases/{id} | Required | 医案详情（含 Consultation + Prescription） |
| POST | /api/medicalcases | Required | 创建医案 |
| DELETE | /api/medicalcases/{id} | Required | 软删除医案 |
| GET | /api/medicalcases/search?patientName=&diagnosisKeyword=&startDate=&endDate=&page=&pageSize= | Required | 高级搜索 |
| GET | /api/medicalcases/query | Required | 结构化查询（ByPatient/Pending/Unfinished/Recent/All） |
| POST | /api/medicalcases/batch-details | Required | 批量获取详情（≤50） |
| GET | /api/medicalcases/{id}/permissions | Required | 获取编辑/删除权限 |
| GET | /api/medicalcases/by-status/{status} | Required | 按状态查询 |
| PUT | /api/medicalcases/{id}/close | Required | 完结医案 |
| PUT | /api/medicalcases/{id}/suspend | Required | 挂起医案 |
| PUT | /api/medicalcases/{id}/cancel | Required | 取消医案（恢复挂号为 Waiting） |
| PUT | /api/medicalcases/{id}/prescription-flag | Required | 设置是否需要处方标记 |
| PUT | /api/medicalcases/{id}/status | Required | 更新医案状态 |
| PUT | /api/medicalcases/{id}/print-completed | Required | 记录打印完成 |
| PUT | /api/medicalcases/{id} | Required | 聚合保存（医案+诊断+处方） |
| POST | /api/medicalcases/batch-delete | Required | 批量软删除 |
| GET | /api/medicalcases/pending?patientId= | Required | 获取待处理医案 |
| GET | /api/medicalcases/{id}/audit-logs?page=&pageSize= | Required | 审计日志（本地模式返回空） |
| POST | /api/medicalcases/{id}/print-logs | Required | 记录打印日志 |
| GET | /api/medicalcases/{id}/consultations | Required | 获取诊断列表 |
| GET | /api/medicalcases/{id}/prescriptions | Required | 获取处方列表 |

## 诊断端点（Diagnostics）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/diagnostics/db-info | Anonymous | 数据库连接信息 |
| GET | /api/diagnostics/version | Anonymous | 版本信息（程序集、框架、OS） |
| GET | /api/diagnostics/logs/recent?count=50 | Anonymous | 获取最近日志（≤500条） |
| GET | /api/diagnostics/logging/status | Anonymous | 日志级别状态 |
| POST | /api/diagnostics/logging/debug/enable | Anonymous | 启用调试模式（临时提升日志级别） |
| POST | /api/diagnostics/logging/debug/disable | Anonymous | 禁用调试模式 |
| POST | /api/diagnostics/logging/level | Anonymous | 设置日志级别 |

## 配置端点（Configuration）

| 方法 | 路径 | 认证 | 说明 |
|------|------|------|------|
| GET | /api/configuration | Anonymous | 获取全部配置项 |
| GET | /api/configuration/{key} | Anonymous | 按 Key 获取配置 |
| PUT | /api/configuration/{key} | Anonymous | 设置配置项 |
| POST | /api/configuration/validate | Anonymous | 验证配置完整性 |

## 响应格式

LocalWebAPI 控制器直接返回实体/DTO，**不使用** Server WebAPI 的 `ApiResponse<T>` 包装。

**成功:**
```json
{ "Id": "...", "Name": "...", ... }
```

**分页列表:**
```json
{ "Items": [...], "TotalCount": 100, "CurrentPage": 1, "PageSize": 20 }
```

**错误:**
- 404: 资源不存在
- 400: 参数验证失败
- 401: 未认证
- 403: 权限不足（如非 Admin 访问 Admin 操作）
- 409: 冲突（如用户名重复、存在引用无法删除）
- 500: 服务器错误

## JSON 序列化

- 属性名: PascalCase (`PropertyNamingPolicy = null`)
- 大小写不敏感: `PropertyNameCaseInsensitive = true`
- 枚举: 字符串格式

## 与 Server WebAPI 的区别

| 特性 | Server WebAPI | LocalWebAPI |
|------|---------------|-------------|
| 响应包装 | ApiResponse<T> | 直接返回实体 |
| API 版本 | `/api/v{version}/[controller]` | `/api/[controller]` |
| 认证策略 | Policy-based (AdminOnly, DoctorOrAdmin) | 简单 [Authorize] + 运行时角色检查 |
| 分页参数 | page, pageSize, keyword | 各端点参数不同 |
| 批量操作 | 支持 | 支持（batch-delete/enable/disable/import） |
| 导入/导出 | Excel | JSON |
| 数据访问层 | Controller → Service → Repository | Controller → DbContext 直连 |
| 审计日志 | 持久化存储 | 返回空列表 |

## 变更日志

| 日期 | 变更 | 说明 |
|------|------|------|
| 2026-06-12 | 全面更新 | 从实际代码重新生成全部端点文档；端点数从 ~41 更正为 112；新增 Auth (5), Health (3), Diagnostics (7), Configuration (4) 控制器端点；Users/Patients/Herbs/Formulas/Registrations/MedicalCases 控制器补充批量操作、导入导出、状态管理等遗漏端点 |
| 2026-04-26 | 初始版本 | 首次创建 LocalWebAPI 端点文档 |
