# LYBTZYZS API 测试文档

> 凌隐宝堂中医诊所管理系统 API 测试用例与验证指南

---

## 测试环境

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `baseUrl` | `https://localhost:5001` | API 基础地址 |
| `authToken` | (自动填充) | JWT Bearer Token |
| `refreshToken` | (自动填充) | 刷新令牌 |
| `currentUserId` | (自动填充) | 当前登录用户 ID |

**前置条件**: 服务器运行中，数据库已初始化，SSL 证书已配置。

**执行顺序**: Health → Auth → Users → Patients → Herbs → Formulas → Registrations → Medical Cases → Sync → Diagnostics

---

## 认证测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-AUTH-001-01 | `POST /auth/login` | 正确凭据 | 200, Token+User+RefreshToken |
| TC-AUTH-001-02 | `POST /auth/login` | 错误密码 | 401 或 200 success=false |
| TC-AUTH-001-03 | `POST /auth/login` | 不存在用户 | 401 |
| TC-AUTH-001-04 | `POST /auth/login` | 空用户名 | 400 ValidationProblemDetails |
| TC-AUTH-002-01 | `POST /auth/auto-login` | 有效令牌 | 200, 新 Token |
| TC-AUTH-003-01 | `POST /auth/refresh` | 有效刷新令牌 | 200, 新 Token 对 |
| TC-AUTH-003-02 | `POST /auth/refresh` | 无效令牌 | 401 |
| TC-AUTH-004-01 | `GET /auth/validate` | 有效 Bearer Token | 200, isValid=true |
| TC-AUTH-004-04 | `GET /auth/validate` | 缺少 Authorization | 401 |
| TC-AUTH-005-01 | `POST /auth/logout` | 正常登出 | 200, success=true |
| TC-AUTH-005-02 | `POST /auth/logout` | 登出后用旧令牌 | 401 |

---

## 用户管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-USER-001-01 | `GET /users` | 默认列表 | 200, PagedResult |
| TC-USER-001-02 | `GET /users?keyword=sysadmin` | 关键字搜索 | 200, 包含匹配 |
| TC-USER-001-03 | `GET /users?role=Doctor` | 角色过滤 | 200, 所有 items.role=Doctor |
| TC-USER-001-04 | `GET /users?page=9999` | 超出范围 | 200, items=[] |
| TC-USER-002-01 | `GET /users/current` | 已认证 | 200, id==currentUserId |
| TC-USER-003-01 | `POST /users` | 正常创建 | 201, UserDetailDto |
| TC-USER-003-02 | `POST /users` | 重复用户名 | 409 Conflict |
| TC-USER-003-04 | `POST /users` | 密码太短 | 400 |
| TC-USER-003-05 | `POST /users` | 非 Admin 创建 | 403 |
| TC-USER-004-01 | `POST /users/{id}/reset-password` | Admin 重置 | 200, 临时密码 |
| TC-USER-004-03 | `POST /users/{id}/reset-password` | 不存在用户 | 404 |
| TC-USER-005-01 | `POST /users/batch-delete` | 批量删除 | 200 |
| TC-USER-005-02 | `POST /users/batch-delete` | 空列表 | 400 |

---

## 患者管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-PAT-001-01 | `POST /patients` | 创建患者 | 201, PatientDetailDto |
| TC-PAT-001-02 | `GET /patients/{id}` | 获取详情 | 200, 含 Age 计算字段 |
| TC-PAT-001-03 | `PUT /patients/{id}` | 更新患者 | 200 |
| TC-PAT-001-04 | `DELETE /patients/{id}` | 软删除 | 200 |
| TC-PAT-001-05 | `GET /patients/{id}` | 获取已删除 | 404 |
| TC-PAT-001-06 | `POST /patients/{id}/restore` | 恢复 | 200 |
| TC-PAT-002-01 | `POST /patients/batch-import` | JSON 批量导入 | 200, BatchOperationResult |
| TC-PAT-002-02 | `POST /patients/batch-import` | 空列表 | 400 |
| TC-PAT-002-03 | `GET /patients/import-template` | 下载模板 | 200, Excel |
| TC-PAT-002-04 | `GET /patients/export` | 导出患者 | 200, Excel |

---

## 医案管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-MC-001-01 | `POST /medicalcases` | 创建医案 | 201, MedicalCaseDetailDto |
| TC-MC-001-02 | `GET /medicalcases/{id}` | 获取详情 | 200, 含 Consultation+Prescription |
| TC-MC-001-03 | `PUT /medicalcases/{id}` | 保存草稿 | 200 |
| TC-MC-001-04 | `DELETE /medicalcases/{id}` | 删除 | 204 |
| TC-MC-001-05 | `GET /medicalcases` | 列表 | 200, PagedResult |
| TC-MC-002-01 | `PUT /medicalcases/{id}/status` | Completed | 200 |
| TC-MC-002-02 | `PUT /medicalcases/{id}/close` | 关闭 | 200 |
| TC-MC-002-03 | `PUT /medicalcases/{id}/suspend` | 挂起 | 200 |
| TC-MC-002-04 | `PUT /medicalcases/{id}/cancel` | 取消 | 204 |
| TC-MC-002-05 | `PUT /medicalcases/{id}/status` | 已完成再改 | 400 |
| TC-MC-003-01 | `GET /medicalcases/{id}/permissions` | 权限 | 200 |
| TC-MC-003-02 | `GET /medicalcases/{id}/audit-logs` | 审计日志 | 200, PagedResult |
| TC-MC-004-01 | `PUT /medicalcases/{id}/print-completed` | 记录打印 | 200 |
| TC-MC-004-02 | `POST /medicalcases/{id}/print-logs` | 打印日志 | 200 |

---

## 药材管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-HERB-001-01 | `POST /herbs` | 创建药材 | 201, HerbDetailDto |
| TC-HERB-001-02 | `GET /herbs/{id}` | 获取详情 | 200 |
| TC-HERB-001-03 | `PUT /herbs/{id}` | 更新 | 200 |
| TC-HERB-001-04 | `DELETE /herbs/{id}` | 软删除 | 200 |
| TC-HERB-001-05 | `GET /herbs` | 列表(分类过滤) | 200, PagedResult |
| TC-HERB-002-01 | `POST /herbs/batch-import` | 批量导入 | 200 |
| TC-HERB-002-02 | `POST /herbs/batch-import` | 超10000条 | 400 |
| TC-HERB-002-03 | `GET /herbs/export` | 导出 Excel | 200 |
| TC-HERB-002-04 | `GET /herbs/check-reference` | 引用检查 | 200 |
| TC-HERB-003-03 | `GET /herbs/import-template` | 下载模板 | 200 |
| TC-HERB-003-04 | `GET /herbs/export-all` | 导出 JSON | 200 |

---

## 验方管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-FORM-001-01 | `POST /formulas` | 创建验方 | 201, FormulaDetailDto |
| TC-FORM-001-02 | `GET /formulas/{id}` | 获取详情(含药材) | 200 |
| TC-FORM-001-03 | `PUT /formulas/{id}` | 更新 | 200 |
| TC-FORM-001-04 | `DELETE /formulas/{id}` | 删除 | 200 |
| TC-FORM-002-01 | `POST /formulas/batch-import` | 批量导入 | 200 |
| TC-FORM-002-02 | `GET /formulas/export` | 导出 Excel | 200 |
| TC-FORM-002-03 | `GET /formulas/pending-validation` | 待验证列表 | 200 |
| TC-FORM-002-04 | `POST /formulas/{id}/herbs/{id}/validate` | 验证药材 | 200 |

---

## 同步模块测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-SYNC-001-01 | `GET /sync/entity-types` | 实体类型列表 | 200 |
| TC-SYNC-001-02 | `GET /sync/metadata` | 元数据 | 200 |
| TC-SYNC-001-03 | `POST /sync/compare` | 比较差异 | 200, SyncDiffDto |
| TC-SYNC-001-04 | `POST /sync/upload` | 上传数据 | 200 |
| TC-SYNC-001-05 | `POST /sync/download` | 下载数据 | 200 |
| TC-SYNC-001-06 | `POST /sync/delete` | 删除同步数据 | 200 |

---

## 挂号管理测试

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-REG-001-01 | `POST /registrations` | 创建挂号 | 201 |
| TC-REG-001-02 | `GET /registrations/{id}` | 获取详情 | 200 |
| TC-REG-001-03 | `GET /registrations` | 列表 | 200, PagedResult |
| TC-REG-001-04 | `GET /registrations/queue` | 排队列表 | 200 |
| TC-REG-001-05 | `PUT /registrations/{id}/start-visit` | 开始就诊 | 200 |
| TC-REG-001-06 | `PUT /registrations/{id}/cancel` | 取消 | 200 |

---

## 诊断与健康检查

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-DIAG-001-01 | `GET /diagnostics/logging/status` | 日志状态 | 200 |
| TC-DIAG-001-02 | `POST /diagnostics/logging/debug/enable` | 启用调试 | 200 |
| TC-DIAG-001-05 | 以上端点 | 非 SuperAdmin | 403 |
| TC-HEALTH-001-01 | `GET /health` | 基础检查 | 200 |
| TC-HEALTH-001-02 | `GET /health/ping` | Ping | 200, "pong" |
| TC-HEALTH-001-03 | `GET /health/details` | 详细(需认证) | 200 |
| TC-HEALTH-001-04 | `GET /health/details` | 未认证 | 401 |

---

## 错误场景测试

| 测试编号 | 场景 | 端点 | 预期 |
|----------|------|------|------|
| TC-ERR-001-01 | 无 Token 受保护端点 | `GET /users` | 401 |
| TC-ERR-001-02 | 过期 Token | `GET /users` | 401 |
| TC-ERR-001-03 | 伪造 Token | `GET /users` | 401 |
| TC-ERR-002-01 | Doctor 访问 Admin 端点 | `POST /users` | 403 |
| TC-ERR-002-02 | Receptionist 访问 Doctor 端点 | `POST /medicalcases` | 403 |
| TC-ERR-003-01 | 缺少必填字段 | `POST /patients` (无 Name) | 400 |
| TC-ERR-003-02 | 字段长度超限 | `POST /users` (>32字) | 400 |
| TC-ERR-003-03 | 无效 GUID | `GET /users/invalid` | 400 |
| TC-ERR-003-04 | 无效枚举 | `PUT /medicalcases/{id}/status` (999) | 400 |
| TC-ERR-004-01 | 不存在用户 | `GET /users/00000000-...` | 404 |
| TC-ERR-004-02 | 不存在患者 | `GET /patients/00000000-...` | 404 |
| TC-ERR-005-01 | 批量删除空列表 | `POST /users/batch-delete` [] | 400 |
| TC-ERR-005-02 | 批量导入超限 | `POST /herbs/batch-import` (>10000) | 400 |
| TC-ERR-005-04 | 页码为 0 | `GET /users?page=0` | 400 或修正为 1 |

---

## 覆盖率统计

| 模块 | TC 组数 | 用例数 | 覆盖场景 |
|------|---------|--------|----------|
| Auth | 5 | 11 | 登录/登出/刷新/验证/自动登录 |
| Users | 5 | 13 | CRUD/批量/重置密码/权限 |
| Patients | 2 | 10 | CRUD/导入导出/软删除恢复 |
| Medical Cases | 4 | 14 | CRUD/工作流/审计/打印 |
| Herbs | 3 | 11 | CRUD/批量/导入导出 |
| Formulas | 2 | 8 | CRUD/批量/验证 |
| Sync | 1 | 6 | 同步全流程 |
| Registrations | 1 | 6 | 挂号全流程 |
| Diagnostics+Health | 2 | 7 | 日志管理/健康检查 |
| **错误场景** | 5 | 14 | 认证/权限/验证/资源/边界 |
| **合计** | **30** | **100** | - |
