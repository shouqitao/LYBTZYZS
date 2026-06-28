# API 参考

> 凌隐宝堂中医诊所管理系统 RESTful API 文档

## API 基本信息

| 属性 | 值 |
|------|-----|
| **Base URL** | `https://{host}/api/v1` |
| **协议** | HTTPS |
| **API 版本** | v1 (URL Path Versioning) |
| **认证方式** | Bearer Token (JWT) |
| **内容类型** | `application/json` (默认) |
| **字符编码** | UTF-8 |

## 认证方式

所有需要认证的端点必须在请求头中携带 JWT Token:

```
Authorization: Bearer {access_token}
```

### TOKEN 获取脚本

所有需要认证的端点 curl 示例均假设 `$TOKEN` 环境变量已设置。获取方式：

```bash
# 标准账号 (admin)
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

# sysadmin 账号
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"sysadmin","password":"SysAdmin@2026!"}' | jq -r '.data.token')
```

> Token 有效期 60 分钟（`AddMinutes(60)` 硬编码）。完整认证流程见 [认证 API](01-auth.md)。

## 通用响应格式

### `ApiResponse<T>` 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `success` | bool | 操作是否成功 |
| `message` | string | 操作结果消息 |
| `data` | T? | 响应数据（泛型，失败时为 null） |
| `errors` | object? | 错误详情（见下方失败模板） |
| `timestamp` | long | Unix 时间戳（秒） |
| `requestId` | string | 请求追踪 ID |

> ⚠️ **无 `code` 字段**（基线§6）。响应以 `success` 布尔判断成败。

### 成功响应

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "requestId": "0HN8V..."
}
```

### 失败响应信封模板

所有失败响应统一使用 `ApiResponse` 信封（**少数端点除外**，如 `/auth/login` 验证失败返回裸 JSON）：

```json
{
  "success": false,
  "message": "用户可读的错误描述",
  "data": null,
  "errors": {
    "code": "ERR-XXXXX",
    "details": ["可选的详细错误信息"]
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V..."
}
```

> `errors` 可为 null、字符串数组、或 `{ "code": "ERR-XXXXX" }` 对象。`code` 形式为 `ERR-{5位数字}`，分区见各模块文档末尾错误码表。

### 分页响应

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [ ... ],
    "totalCount": 100,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "requestId": "0HN8V..."
}
```

## 分页参数规范

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| `page` | int | 1 | >= 1 | 页码 |
| `pageSize` | int | 20 | 1-100 | 每页记录数 |
| `keyword` | string? | null | - | 搜索关键词 |

## 通用 HTTP 状态码

> **各端点文档只列出该端点特有的状态码**（如 422/409/204），通用码 401/403/404/400 统一参考本表。

| 状态码 | 含义 | 场景 |
|--------|------|------|
| 200 | 成功 | 查询、更新、业务操作成功 |
| 201 | 已创建 | 资源创建成功 |
| 204 | 无内容 | 取消成功（MedicalCase.Cancel）|
| 400 | 请求错误 | 参数验证失败 |
| 401 | 未授权 | Token 无效/过期/被撤销 |
| 403 | 禁止访问 | 权限不足（非管理员操作他人资源）|
| 404 | 未找到 | 资源不存在 |
| 405 | 方法不允许 | 不支持的 HTTP 方法 |
| 422 | 不可处理 | 业务规则验证失败（状态流转错误等）|
| 429 | 请求过多 | 触发限流（Login 端点）|
| 500 | 服务器错误 | 内部异常 |
| 503 | 服务不可用 | 服务端不可用 |

## 模块端点索引

> **端点总数说明**：v1.0 已实现约 **76** 个公开端点（Auth 5 + Users 11 + Patients 7 + Herbs 8 + Formulas 10 + MedicalCases 15 + Registrations 7 + Reports 3 + Diagnostics 4 + Configuration 3 + Health 3；Sync 模块整块未实现）。另有 **D1-D10 待补回项**（见各分文档「🚧 v1.0 待实现」标注）属 v1.0 范围但代码尚未落地，文档保留设计。基线 `docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md`。

### 认证模块 ([01-auth.md](01-auth.md))

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| POST | `/auth/login` | 匿名 | 用户登录 |
| POST | `/auth/auto-login` | 匿名 | AutoLoginToken 自动登录 |
| POST | `/auth/logout` | 匿名 | 用户登出 |
| POST | `/auth/refresh` | 匿名 | 刷新 Token |
| GET | `/auth/validate` | 已认证 | 验证 Token |

### 用户模块 ([02-users.md](02-users.md)) -- AdminOrSuperAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/users` | 用户列表 (分页) |
| GET | `/users/current` | 当前登录用户信息 |
| GET | `/users/{id}` | 用户详情 |
| POST | `/users` | 创建用户 |
| PUT | `/users/{id}` | 更新用户 |
| DELETE | `/users/{id}` | 删除用户 (软删除) |
| POST | `/users/{id}/reset-password` | 重置密码 |
| PUT | `/users/{id}/profile` | 修改个人资料 |
| PUT | `/users/{id}/change-password` | 修改密码 |
| POST | `/users/{id}/toggle-status` | 启用/禁用切换 |
| POST | `/users/{id}/restore` | 恢复已删除用户 |
| POST | `/users/batch-delete` | 批量删除 |
| POST | `/users/batch-enable` | 批量启用 |
| POST | `/users/batch-disable` | 批量禁用 |

### 患者模块 ([03-patients.md](03-patients.md)) -- DoctorOrReceptionist ⚠️ 代码当前为 DoctorOrAdmin，待对齐（D7）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/patients` | 患者列表 (分页) |
| GET | `/patients/{id}` | 患者详情 |
| POST | `/patients` | 新增患者 |
| PUT | `/patients/{id}` | 更新患者 |
| DELETE | `/patients/{id}` | 删除患者 (软删除) |
| GET | `/patients/import-template` | 下载导入模板 |
| GET | `/patients/export` | 导出 Excel |
| POST | `/patients/{id}/restore` | 恢复已删除患者 |
| POST | `/patients/{id}/toggle-status` | 启用/禁用切换 (AdminOrSuperAdmin, US-PAT-013) |
| POST | `/patients/batch-delete` | 批量删除 |
| GET | `/patients/{id}/check-reference` | 引用检查 |
| POST | `/patients/batch-check-reference` | 批量引用检查 |

### 药材模块 ([04-herbs.md](04-herbs.md)) -- DoctorOrReceptionist ⚠️ 代码当前为 DoctorOrAdmin，待对齐（D7）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/herbs` | 药材列表 (分页, OutputCache) |
| GET | `/herbs/{id}` | 药材详情 |
| POST | `/herbs` | 创建药材 |
| PUT | `/herbs/{id}` | 更新药材 |
| DELETE | `/herbs/{id}` | 删除药材 (软删除) |
| POST | `/herbs/{id}/toggle-status` | 启用/禁用切换 |
| POST | `/herbs/batch-import` | JSON 批量导入 |
| POST | `/herbs/batch-delete` | 批量删除 |

### 验方模块 ([05-formulas.md](05-formulas.md)) -- DoctorOrReceptionist ⚠️ 代码当前为 DoctorOrAdmin，待对齐（D7）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/formulas` | 验方列表 (分页, 角色过滤) |
| GET | `/formulas/{id}` | 验方详情 |
| POST | `/formulas` | 新增验方 |
| PUT | `/formulas/{id}` | 更新验方 |
| DELETE | `/formulas/{id}` | 删除验方 (软删除) |
| POST | `/formulas/batch-import` | JSON 批量导入 |
| GET | `/formulas/pending-validation` | 待校验验方列表 |
| POST | `/formulas/{formulaId}/herbs/{herbItemId}/validate` | 验证药材绑定 |
| POST | `/formulas/{id}/toggle-status` | 启用/禁用切换 |
| POST | `/formulas/batch-delete` | 批量删除 |

### 医案模块 ([06-medical-cases.md](06-medical-cases.md)) -- DoctorOrReceptionist（创建端点为 Doctor-only 目标）⚠️ 代码当前为 DoctorOrAdmin，待对齐（D7）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/medicalcases` | 创建医案 (Doctor Only) |
| PUT | `/medicalcases/{id}` | 聚合保存 (Consultation + Prescription) |
| PUT | `/medicalcases/{id}/prescription-flag` | 设置处方标记 |
| PUT | `/medicalcases/{id}/status` | 更新状态 |
| PUT | `/medicalcases/{id}/close` | 关闭医案 |
| PUT | `/medicalcases/{id}/suspend` | 挂起医案 |
| PUT | `/medicalcases/{id}/cancel` | 取消医案 |
| DELETE | `/medicalcases/{id}` | 删除医案 (软删除) |
| POST | `/medicalcases/batch-delete` | 批量删除 |
| POST | `/medicalcases/batch-details` | 批量获取详情 |
| GET | `/medicalcases` | 医案列表 (分页) |
| GET | `/medicalcases/{id}` | 医案详情 |
| GET | `/medicalcases/query` | 统一查询端点 |
| GET | `/medicalcases/search` | 跨医案搜索 |
| GET | `/medicalcases/{id}/permissions` | 获取权限 |
| GET | `/medicalcases/{id}/audit-logs` | 审计日志 |
| GET | `/medicalcases/{id}/consultations` | 诊断记录列表 |
| GET | `/medicalcases/{id}/prescriptions` | 处方记录列表 |
| PUT | `/medicalcases/{id}/print-completed` | 记录打印完成 (详见 [08-printing.md](08-printing.md)) |
| POST | `/medicalcases/{id}/print-logs` | 添加打印日志 (详见 [08-printing.md](08-printing.md)) |

### 挂号管理模块 ([07-registrations.md](07-registrations.md)) -- DoctorOrReceptionist ⚠️ 代码当前为 DoctorOrAdmin，待对齐（D7）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/registrations` | 创建挂号 (前台模式) |
| POST | `/registrations/quick-visit` | 医生快速看诊 (DoctorOrReceptionist) |
| GET | `/registrations/{id}` | 挂号详情 |
| GET | `/registrations` | 挂号列表 (分页) |
| GET | `/registrations/queue` | 等待队列 |
| PUT | `/registrations/{id}/start-visit` | 接诊 (DoctorOrReceptionist) |
| PUT | `/registrations/{id}/cancel` | 取消挂号 |

### 打印模块 ([08-printing.md](08-printing.md))

> 打印相关端点（`PUT /medicalcases/{id}/print-completed`、`POST /medicalcases/{id}/print-logs`）挂在 `MedicalCasesController` 下，详见上方「医案模块」末两行。完整打印模板、预览、PDF 导出设计见 [08-printing.md](08-printing.md)。

### 数据同步模块 ([09-sync.md](09-sync.md)) -- 🔴 v2.0 规划，v1.0 不实现

> 基线§2 N1 决策：v1.0 远程与本地数据孤立，不互通。下列 7 端点均无对应 Controller，保留作 v2.0 设计参考。

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/sync/entity-types` | 获取支持的实体类型 |
| GET | `/sync/metadata` | 获取同步元数据 |
| POST | `/sync/compare` | 比对差异 |
| POST | `/sync/upload` | 上传本地数据 |
| POST | `/sync/download` | 下载服务端数据 |
| POST | `/sync/delete` | 同步删除 |

### 健康检查 ([11-health.md](11-health.md))

> 端点说明（基线§6）：`/health`、`/health/ping`、`/health/details` 来自 `HealthController`（Server + LocalWebAPI 各一份）。另有 `/health/database` 由中间件层 `MapHealthChecks` 映射，**仅 Server WebAPI 存在**，LocalWebAPI 无此端点。

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/health` | 匿名 | 基础健康检查 |
| GET | `/health/ping` | 匿名 | Ping |
| GET | `/health/details` | 已认证 | 详细健康检查 (含数据库) |
| GET | `/health/database` | 匿名 | 中间件层健康检查（仅 Server，LocalWebAPI 无） |

### 报表模块 ([13-reports.md](13-reports.md)) -- DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/reports/daily/income` | 当日收入汇总 |
| GET | `/reports/daily/consultations` | 当日问诊统计 |
| GET | `/reports/daily/herbs` | 当日药材使用排行 |

### 诊断工具 ([12-diagnostics.md](12-diagnostics.md)) -- AdminOrSuperAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/diagnostics/logging/status` | 日志级别状态 |
| POST | `/diagnostics/logging/debug/enable` | 启用调试模式 |
| POST | `/diagnostics/logging/debug/disable` | 禁用调试模式 |
| POST | `/diagnostics/logging/level` | 设置日志级别 |

### 系统配置 ([10-configuration.md](10-configuration.md)) -- AdminOrSuperAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/configuration` | 获取系统配置 |
| GET | `/configuration/{key}` | 获取单个配置项 |
| POST | `/configuration/validate` | 验证生产环境配置 |

## 认证错误码

> ⚠️ **实现状态说明**：`AuthController` 代码实际仅返回少量错误（主要是 `AuthInvalidCredentials` 凭据错误、Token 校验失败）。下表多数条目为**设计扩展，未在代码中实现返回路径**，作为安全防御性设计与未来扩展保留。详见 [01-auth.md](01-auth.md)。

| 错误码 | 说明 | HTTP 状态码 | 实现状态 |
|--------|------|-------------|----------|
| InvalidCredentials | 用户名或密码错误 | 401 | ✅ 已实现 |
| TokenInvalid | Token 无效 | 401 | ✅ 已实现 |
| TokenExpired | Token 已过期 | 401 | 🚧 设计扩展 |
| UserDisabled | 用户已禁用 | 401 | 🚧 设计扩展 |
| UserNotFound | 用户不存在 | 401 | 🚧 设计扩展 |
| RefreshTokenInvalid | RefreshToken 无效 | 401 | 🚧 设计扩展 |
| InternalError | 内部错误 | 500 | ✅ 通用兜底 |

> 其余此前列出的码（PasswordExpired / TokenRevoked / RefreshTokenExpired / SessionNotFound / SessionExpired / ConcurrentSessionLimit / ServiceUnavailable）均为设计扩展，代码无对应返回路径。

## 授权策略

> 策略常量定义见 `PolicyConstants.cs`（共 4 项：`AdminOnly`、`DoctorOrAdmin`、`AdminOrSuperAdmin`、`DoctorOrReceptionist`；**无 `DoctorOnly`**）。基线§3：文档保留目标策略，代码不符处加「⚠️ 代码当前为 X，待对齐（D7）」。

| 策略 | 角色 | 适用模块 | 代码现状 |
|------|------|----------|----------|
| AdminOrSuperAdmin | Admin, SuperAdmin | 用户管理、系统配置、诊断工具 | ✅ 一致 |
| DoctorOrReceptionist | Doctor, Admin, SuperAdmin, Receptionist | 患者、药材、验方、挂号管理 | ⚠️ 代码为 DoctorOrAdmin，待对齐（D7） |
| DoctorOrReceptionist（医案创建为 Doctor-only 目标） | Doctor, Admin, SuperAdmin, Receptionist | 医案 | ⚠️ 代码为 DoctorOrAdmin，待对齐（D7） |

> Sync 模块属 v2.0，不计入 v1.0 策略适用范围。

## 废弃端点

> ⚠️ 此前版本曾列出 5 个 `[Obsolete]` 端点（with-details/pending/by-patient/recent/unfinished）。经代码核对，这些端点**从未存在**于任何 Controller，相关查询能力已由 `GET /medicalcases/query`（统一查询端点，`queryType` 参数）覆盖。原表已删除（基线§7 原则 2）。

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，覆盖全部 10 个 Controller |
| 2026-02-18 | v1.1 | PRD同步: 患者模块新增 PUT /patients/{id}/status (FR-PAT-013); 端点总数 92->93 |
| 2026-02-18 | v1.2 | 认证错误码章节补充注释: 标注 4 个设计扩展码 (PasswordExpired/SessionNotFound/SessionExpired/ConcurrentSessionLimit) |
| 2026-02-22 | v1.3 | MC-D20 同步: 医案端点 `/draft` 重命名为 `/suspend` (Draft→Suspended 状态重命名) |
| 2026-05-04 | v1.4 | 新增挂号管理模块 (registrations.md, 7 端点); 新增系统配置端点 (3 端点); 医案模块补充打印端点 (2 端点); 患者模块修正 check-reference (GET) 和 toggle-status (POST) 动词 |
| 2026-06-12 | v1.5 | 端点总数更新为 ~106 (14 controllers); 移除 POST /patients/import (客户端功能); US-PAT-013 改为 toggle-status; 打印端点交叉引用 printing.md; 204 状态码修正为 Cancel |
| 2026-06-25 | v2.0 | 修正药材/验方/患者模块策略为 DoctorOrAdmin; 移除不存在的药材端点 (export, export-all, import-template, check-reference, batch-check-reference, batch-enable, batch-disable, restore); 移除不存在的验方端点 (export, import-template, restore, batch-enable, batch-disable); 所有模块补充完整 JSON 示例和 curl 命令 |
| 2026-06-25 | v2.1 | 新增报表模块 (13-reports.md, 3 端点); 端点总数更新为 ~109 |
| 2026-06-28 | v2.2 | 文档对齐基线：端点总数改为「v1.0 已实现约76 + D1-D10 待补回」；删除虚构废弃端点表（5 端点从未存在）；Auth 错误码表精简并标注实现状态；各模块策略标注对齐基线§3（D7）；health 端点澄清（/health/database 仅 Server）；Sync 模块标 v2.0 |
| 2026-06-28 | v2.3 | 文档结构优化批次1（S2）集中化：新增 TOKEN 获取脚本（标准+sysadmin）、ApiResponse&lt;T&gt; 字段说明表、失败响应信封模板（errors.code 形态）、通用 HTTP 状态码表强化（各端点只保留特有码）。各端点文档去 ApiResponse 外壳只留 data、错误 JSON 合并到错误码表、curl 删除 TOKEN 脚本、通用码引用 README |
