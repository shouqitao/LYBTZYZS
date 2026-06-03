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

Token 获取方式见 [认证 API](auth.md)。

## 通用响应格式

### 成功响应

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "requestId": "0HN8V..."
}
```

### 失败响应

```json
{
  "success": false,
  "message": "错误描述",
  "errors": null,
  "requestId": "0HN8V..."
}
```

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

| 状态码 | 含义 | 场景 |
|--------|------|------|
| 200 | 成功 | 查询、更新、业务操作成功 |
| 201 | 已创建 | 资源创建成功 (Users.Create) |
| 204 | 无内容 | 删除成功 (MedicalCase.Delete) |
| 400 | 请求错误 | 参数验证失败 |
| 401 | 未授权 | Token 无效/过期/被撤销 |
| 403 | 禁止访问 | 权限不足 (非管理员操作他人资源) |
| 404 | 未找到 | 资源不存在 |
| 405 | 方法不允许 | 不支持的 HTTP 方法 |
| 422 | 不可处理 | 业务规则验证失败 (状态流转错误等) |
| 429 | 请求过多 | 触发限流 (Login 端点) |
| 500 | 服务器错误 | 内部异常 |
| 503 | 服务不可用 | 服务端不可用 |

## 模块端点索引

### 认证模块 ([auth.md](auth.md))

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| POST | `/auth/login` | 匿名 | 用户登录 |
| POST | `/auth/auto-login` | 匿名 | AutoLoginToken 自动登录 |
| POST | `/auth/logout` | 匿名 | 用户登出 |
| POST | `/auth/refresh` | 匿名 | 刷新 Token |
| GET | `/auth/validate` | 已认证 | 验证 Token |

### 用户模块 ([users.md](users.md)) -- AdminOnly

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

### 患者模块 ([patients.md](patients.md)) -- DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/patients` | 患者列表 (分页) |
| GET | `/patients/{id}` | 患者详情 |
| POST | `/patients` | 新增患者 |
| PUT | `/patients/{id}` | 更新患者 |
| DELETE | `/patients/{id}` | 删除患者 (软删除) |
| POST | `/patients/import` | Excel 导入 |
| GET | `/patients/import-template` | 下载导入模板 |
| GET | `/patients/export` | 导出 Excel |
| POST | `/patients/{id}/restore` | 恢复已删除患者 |
| POST | `/patients/{id}/toggle-status` | 启用/禁用切换 (AdminOnly, FR-PAT-013) |
| POST | `/patients/batch-delete` | 批量删除 |

### 药材模块 ([herbs.md](herbs.md)) -- DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/herbs` | 药材列表 (分页) |
| GET | `/herbs/{id}` | 药材详情 |
| POST | `/herbs` | 创建药材 |
| PUT | `/herbs/{id}` | 更新药材 |
| DELETE | `/herbs/{id}` | 删除药材 (软删除) |
| POST | `/herbs/import` | Excel 导入 |
| GET | `/herbs/import-template` | 下载导入模板 |
| GET | `/herbs/export` | 导出 Excel |
| POST | `/herbs/batch-import` | JSON 批量导入 |
| GET | `/herbs/export-all` | 导出全部 (JSON) |
| GET | `/herbs/{id}/check-reference` | 引用检查 |
| POST | `/herbs/batch-check-reference` | 批量引用检查 |
| POST | `/herbs/{id}/toggle-status` | 启用/禁用切换 |
| POST | `/herbs/{id}/restore` | 恢复已删除药材 |
| POST | `/herbs/batch-enable` | 批量启用 |
| POST | `/herbs/batch-disable` | 批量禁用 |
| POST | `/herbs/batch-delete` | 批量删除 |

### 验方模块 ([formulas.md](formulas.md)) -- DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/formulas` | 验方列表 (分页, 角色过滤) |
| GET | `/formulas/{id}` | 验方详情 |
| POST | `/formulas` | 新增验方 |
| PUT | `/formulas/{id}` | 更新验方 |
| DELETE | `/formulas/{id}` | 删除验方 (软删除) |
| POST | `/formulas/batch-import` | JSON 批量导入 |
| GET | `/formulas/export` | 导出 Excel |
| GET | `/formulas/import-template` | 下载导入模板 |
| GET | `/formulas/pending-validation` | 待校验验方列表 |
| POST | `/formulas/{formulaId}/herbs/{herbItemId}/validate` | 验证药材绑定 |
| POST | `/formulas/{id}/toggle-status` | 启用/禁用切换 |
| POST | `/formulas/{id}/restore` | 恢复已删除验方 |
| POST | `/formulas/batch-delete` | 批量删除 |
| POST | `/formulas/batch-enable` | 批量启用 |
| POST | `/formulas/batch-disable` | 批量禁用 |

### 医案模块 ([medical-cases.md](medical-cases.md)) -- DoctorOrAdmin

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
| PUT | `/medicalcases/{id}/print-completed` | 记录打印完成 |
| POST | `/medicalcases/{id}/print-logs` | 添加打印日志 |

### 挂号管理模块 ([registrations.md](registrations.md)) -- PatientAccess

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/registrations` | 创建挂号 (前台模式) |
| POST | `/registrations/quick-visit` | 医生快速看诊 (DoctorOrAdmin) |
| GET | `/registrations/{id}` | 挂号详情 |
| GET | `/registrations` | 挂号列表 (分页) |
| GET | `/registrations/queue` | 等待队列 |
| PUT | `/registrations/{id}/start-visit` | 接诊 (DoctorOrAdmin) |
| PUT | `/registrations/{id}/cancel` | 取消挂号 |

### 数据同步模块 ([sync.md](sync.md)) -- DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/sync/entity-types` | 获取支持的实体类型 |
| GET | `/sync/metadata` | 获取同步元数据 |
| POST | `/sync/compare` | 比对差异 |
| POST | `/sync/upload` | 上传本地数据 |
| POST | `/sync/download` | 下载服务端数据 |
| POST | `/sync/delete` | 同步删除 |

### 健康检查 ([health.md](health.md))

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/health` | 匿名 | 基础健康检查 |
| GET | `/health/ping` | 匿名 | Ping |
| GET | `/health/details` | 已认证 | 详细健康检查 (含数据库) |

### 诊断工具 ([diagnostics.md](diagnostics.md)) -- SuperAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/diagnostics/logging/status` | 日志级别状态 |
| POST | `/diagnostics/logging/debug/enable` | 启用调试模式 |
| POST | `/diagnostics/logging/debug/disable` | 禁用调试模式 |
| POST | `/diagnostics/logging/level` | 设置日志级别 |

### 系统配置 -- AdminOnly

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/configuration` | 获取系统配置 |
| GET | `/configuration/{key}` | 获取单个配置项 |
| POST | `/configuration/validate` | 验证生产环境配置 |

## 认证错误码

| 错误码 | 说明 | HTTP 状态码 |
|--------|------|-------------|
| InvalidCredentials | 用户名或密码错误 | 401 |
| UserNotFound | 用户不存在 | 401 |
| UserDisabled | 用户已禁用 | 401 |
| PasswordExpired | 密码已过期 | 401 |
| TokenExpired | Token 已过期 | 401 |
| TokenInvalid | Token 无效 | 401 |
| TokenRevoked | Token 已撤销 | 401 |
| RefreshTokenExpired | RefreshToken 已过期 | 401 |
| RefreshTokenInvalid | RefreshToken 无效 | 401 |
| SessionNotFound | 会话不存在 | 401 |
| SessionExpired | 会话已过期 | 401 |
| ConcurrentSessionLimit | 并发会话数超限 | 401 |
| InternalError | 内部错误 | 500 |
| ServiceUnavailable | 服务不可用 | 503 |

> 其中 PasswordExpired、SessionNotFound、SessionExpired、ConcurrentSessionLimit 为设计扩展 (不在 [auth.md](../02-requirements/auth.md) PRD 中定义)，作为安全防御性措施保留。

## 授权策略

| 策略 | 角色 | 适用模块 |
|------|------|----------|
| AdminOnly | Admin, SuperAdmin | 用户管理 |
| DoctorOrAdmin | Doctor, Admin, SuperAdmin | 患者、药材、验方、医案、同步 |
| SuperAdmin | SuperAdmin | 诊断工具 |

## 废弃端点

以下端点已标记 `[Obsolete]`，将在 v2.0 移除:

| 端点 | 替代方案 |
|------|----------|
| `GET /medicalcases/{id}/with-details` | `GET /medicalcases/{id}` |
| `GET /medicalcases/pending` | `GET /medicalcases/query?queryType=Pending` |
| `GET /medicalcases/by-patient/{patientId}` | `GET /medicalcases/query?queryType=ByPatient` |
| `GET /medicalcases/patient/{patientId}/recent` | `GET /medicalcases/query?queryType=Recent` |
| `GET /medicalcases/patient/{patientId}/unfinished` | `GET /medicalcases/query?queryType=Unfinished` |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，覆盖全部 10 个 Controller |
| 2026-02-18 | v1.1 | PRD同步: 患者模块新增 PUT /patients/{id}/status (FR-PAT-013); 端点总数 92->93 |
| 2026-02-18 | v1.2 | 认证错误码章节补充注释: 标注 4 个设计扩展码 (PasswordExpired/SessionNotFound/SessionExpired/ConcurrentSessionLimit) |
| 2026-02-22 | v1.3 | MC-D20 同步: 医案端点 `/draft` 重命名为 `/suspend` (Draft→Suspended 状态重命名) |
| 2026-05-04 | v1.4 | 新增挂号管理模块 (registrations.md, 7 端点); 新增系统配置端点 (3 端点); 医案模块补充打印端点 (2 端点); 患者模块修正 check-reference (GET) 和 toggle-status (POST) 动词 |
