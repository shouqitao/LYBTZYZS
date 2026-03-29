# LYBTZYZS API 测试文档

> 凌隐宝堂中医诊所管理系统 API 测试用例与验证指南

## 目录

1. [测试环境准备](#测试环境准备)
2. [认证测试](#认证测试)
3. [用户管理测试](#用户管理测试)
4. [患者管理测试](#患者管理测试)
5. [医案管理测试](#医案管理测试)
6. [中药管理测试](#中药管理测试)
7. [验方管理测试](#验方管理测试)
8. [同步模块测试](#同步模块测试)
9. [挂号管理测试](#挂号管理测试)
10. [系统诊断测试](#系统诊断测试)
11. [健康检查测试](#健康检查测试)
12. [通用断言规则](#通用断言规则)
13. [错误场景测试](#错误场景测试)

---

## 测试环境准备

### 环境变量

Postman Collection 使用以下环境变量：

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `baseUrl` | `https://localhost:7001` | API 基础地址 |
| `authToken` | (自动填充) | JWT Bearer Token |
| `refreshToken` | (自动填充) | 刷新令牌 |
| `currentUserId` | (自动填充) | 当前登录用户 ID |
| `currentUsername` | `sysadmin` | 管理员用户名 |
| `testPatientId` | (手动设置) | 测试患者 ID |
| `testHerbId` | (手动设置) | 测试药材 ID |
| `testFormulaId` | (手动设置) | 测试验方 ID |
| `testMedicalCaseId` | (手动设置) | 测试医案 ID |

### 前置条件

1. 服务器已启动并运行在 `https://localhost:7001`
2. 数据库已初始化，存在默认管理员账号 `sysadmin`
3. SSL 证书已配置（开发环境可用自签名证书）

### 测试执行顺序

推荐按以下顺序执行测试：

```
1. Health → 2. Auth → 3. Users → 4. Patients → 5. Herbs
→ 6. Formulas → 7. Registrations → 8. Medical Cases
→ 9. Medical Case Workflow → 10. Medical Case Audit
→ 11. Medical Case Print → 12. Sync → 13. Diagnostics
```

---

## 认证测试

### TC-AUTH-001: 用户登录

**端点**: `POST /api/v1/auth/login`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-AUTH-001-01 | 正确凭据登录 | `{"UserName":"sysadmin","Password":"<正确密码>"}` | 200, `success=true`, 返回 Token+User+RefreshToken |
| TC-AUTH-001-02 | 错误密码登录 | `{"UserName":"sysadmin","Password":"wrong"}` | 401 或 200 `success=false` |
| TC-AUTH-001-03 | 不存在的用户 | `{"UserName":"nonexistent","Password":"123456"}` | 401 或 200 `success=false` |
| TC-AUTH-001-04 | 空用户名 | `{"UserName":"","Password":"123456"}` | 400 ValidationProblemDetails |
| TC-AUTH-001-05 | 空密码 | `{"UserName":"sysadmin","Password":""}` | 400 ValidationProblemDetails |

**断言**:
```javascript
// 成功登录
pm.test("Status is 200", () => pm.response.to.have.status(200));
pm.test("Login success", () => {
    const json = pm.response.json();
    pm.expect(json.success).to.be.true;
    pm.expect(json.data.token).to.be.a("string");
    pm.expect(json.data.refreshToken).to.be.a("string");
    // 存储 token 到环境变量
    pm.environment.set("authToken", json.data.token);
    pm.environment.set("refreshToken", json.data.refreshToken);
    pm.environment.set("currentUserId", json.data.user.id);
});
```

### TC-AUTH-002: 自动登录

**端点**: `POST /api/v1/auth/auto-login`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-AUTH-002-01 | 有效自动登录令牌 | `{"UserName":"sysadmin","AutoLoginToken":"<token>"}` | 200, 返回新 Token |
| TC-AUTH-002-02 | 无效自动登录令牌 | `{"UserName":"sysadmin","AutoLoginToken":"invalid"}` | 401 |

### TC-AUTH-003: 刷新令牌

**端点**: `POST /api/v1/auth/refresh`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-AUTH-003-01 | 有效刷新令牌 | `{"RefreshToken":"{{refreshToken}}"}` | 200, 返回新 Token 对 |
| TC-AUTH-003-02 | 无效刷新令牌 | `{"RefreshToken":"invalid"}` | 401 |
| TC-AUTH-003-03 | 空刷新令牌 | `{"RefreshToken":""}` | 400 |

### TC-AUTH-004: 验证令牌

**端点**: `GET /api/v1/auth/validate`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-AUTH-004-01 | 有效 Bearer Token | Header: `Authorization: Bearer {{authToken}}` | 200, `isValid=true` |
| TC-AUTH-004-02 | 过期 Token | Header: `Authorization: Bearer <expired>` | 401 |
| TC-AUTH-004-03 | 格式错误的 Token | Header: `Authorization: Bearer malformed` | 401 |
| TC-AUTH-004-04 | 缺少 Authorization 头 | 无 Header | 401 |

### TC-AUTH-005: 登出

**端点**: `POST /api/v1/auth/logout`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-AUTH-005-01 | 正常登出 | `{"UserName":"sysadmin","RefreshToken":"{{refreshToken}}"}` | 200, `success=true` |
| TC-AUTH-005-02 | 登出后使用旧令牌 | 登出后 GET /validate | 401 |

---

## 用户管理测试

### TC-USER-001: 用户列表

**端点**: `GET /api/v1/users`

| 测试编号 | 测试用例 | 参数 | 预期结果 |
|----------|----------|------|----------|
| TC-USER-001-01 | 获取默认用户列表 | `page=1&pageSize=20` | 200, PagedResult, items 数组 |
| TC-USER-001-02 | 关键字搜索 | `page=1&pageSize=20&keyword=sysadmin` | 200, 包含 sysadmin |
| TC-USER-001-03 | 角色过滤 | `page=1&pageSize=20&role=Doctor` | 200, 所有 items.role=Doctor |
| TC-USER-001-04 | 分页边界 | `page=1&pageSize=1` | 200, items.length<=1 |
| TC-USER-001-05 | 超出范围页码 | `page=9999&pageSize=20` | 200, items=[] |

**断言**:
```javascript
pm.test("Paged result structure", () => {
    const json = pm.response.json();
    pm.expect(json.data).to.have.property("items");
    pm.expect(json.data).to.have.property("totalCount");
    pm.expect(json.data).to.have.property("currentPage");
    pm.expect(json.data).to.have.property("totalPages");
});
```

### TC-USER-002: 获取当前用户

**端点**: `GET /api/v1/users/current`

| 测试编号 | 测试用例 | 预期结果 |
|----------|----------|----------|
| TC-USER-002-01 | 已认证用户获取自身信息 | 200, UserDetailDto, id==currentUserId |
| TC-USER-002-02 | 未认证请求 | 401 |

### TC-USER-003: 创建用户

**端点**: `POST /api/v1/users`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-USER-003-01 | 正常创建 | 完整 UserInputDto | 201, 返回 UserDetailDto |
| TC-USER-003-02 | 重复用户名 | 同一用户名 | 409 Conflict |
| TC-USER-003-03 | 缺少必填字段 UserName | `{"Password":"test123"}` | 400 ValidationProblemDetails |
| TC-USER-003-04 | 密码太短 | `{"Password":"123"}` | 400 |
| TC-USER-003-05 | 非 Admin 创建用户 | Doctor 角色调用 | 403 |

### TC-USER-004: 重置密码

**端点**: `POST /api/v1/users/{id}/reset-password`

| 测试编号 | 测试用例 | 预期结果 |
|----------|----------|----------|
| TC-USER-004-01 | Admin 重置他人密码 | 200, 返回临时密码 |
| TC-USER-004-02 | 非 Admin 重置 | 403 |
| TC-USER-004-03 | 重置不存在的用户 | 404 |

### TC-USER-005: 批量操作

**端点**: `POST /api/v1/users/batch-delete`, `batch-enable`, `batch-disable`

| 测试编号 | 测试用例 | 输入 | 预期结果 |
|----------|----------|------|----------|
| TC-USER-005-01 | 批量删除 | `{"Ids":["<guid>","<guid>"]}` | 200 |
| TC-USER-005-02 | 空列表 | `{"Ids":[]}` | 400 |
| TC-USER-005-03 | 包含无效 ID | `{"Ids":["00000000-0000-0000-0000-000000000000"]}` | 200, 部分失败 |

---

## 患者管理测试

### TC-PAT-001: 患者 CRUD

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-PAT-001-01 | `POST /api/v1/patients` | 创建患者 | 201, PatientDetailDto |
| TC-PAT-001-02 | `GET /api/v1/patients/{id}` | 获取患者详情 | 200, PatientDetailDto, 含计算字段 Age |
| TC-PAT-001-03 | `PUT /api/v1/patients/{id}` | 更新患者 | 200, 字段已更新 |
| TC-PAT-001-04 | `DELETE /api/v1/patients/{id}` | 软删除患者 | 200 |
| TC-PAT-001-05 | `GET /api/v1/patients/{id}` | 获取已删除患者 | 404 |
| TC-PAT-001-06 | `POST /api/v1/patients/{id}/restore` | 恢复患者 | 200 |
| TC-PAT-001-07 | `GET /api/v1/patients/{id}` | 恢复后获取 | 200 |

### TC-PAT-002: 患者导入导出

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-PAT-002-01 | `POST /api/v1/patients/import` | 上传 Excel 文件 | 200, ImportResultDto |
| TC-PAT-002-02 | `POST /api/v1/patients/import` | 超过 10MB 文件 | 400, 文件过大 |
| TC-PAT-002-03 | `GET /api/v1/patients/import-template` | 下载模板 | 200, Excel 文件 |
| TC-PAT-002-04 | `GET /api/v1/patients/export` | 导出患者 | 200, Excel 文件 |

---

## 医案管理测试

### TC-MC-001: 医案 CRUD

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-MC-001-01 | `POST /api/v1/medicalcases` | 创建医案（含诊断+处方） | 201, MedicalCaseDetailDto |
| TC-MC-001-02 | `GET /api/v1/medicalcases/{id}` | 获取医案详情 | 200, 含 Consultation + Prescription |
| TC-MC-001-03 | `PUT /api/v1/medicalcases/{id}` | 保存医案草稿 | 200 |
| TC-MC-001-04 | `DELETE /api/v1/medicalcases/{id}` | 删除医案 | 204 No Content |
| TC-MC-001-05 | `GET /api/v1/medicalcases` | 列表（含状态过滤） | 200, PagedResult |

**创建医案请求体示例**:
```json
{
  "PatientId": "{{testPatientId}}",
  "UserId": "{{currentUserId}}",
  "Consultation": {
    "PresentIllness": "患者自述头痛三天，伴失眠",
    "TongueDiagnosis": "舌质淡红，苔薄白",
    "PulseDiagnosis": "脉弦细",
    "TcmDiagnosis": "肝郁气滞"
  },
  "Prescription": {
    "DosageCount": 7,
    "Usage": "水煎服，日一剂，分两次温服",
    "Items": [
      {
        "HerbId": "{{testHerbId}}",
        "HerbName": "柴胡",
        "Unit": "克",
        "Dosage": 10,
        "UnitPrice": 0.5,
        "Subtotal": 35
      }
    ],
    "TotalPrice": 35
  },
  "NeedsPrescription": true
}
```

### TC-MC-002: 医案工作流

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-MC-002-01 | `PUT /api/v1/medicalcases/{id}/status` | 设置状态为 Completed | 200, caseStatus=Completed |
| TC-MC-002-02 | `PUT /api/v1/medicalcases/{id}/close` | 关闭医案 | 200 |
| TC-MC-002-03 | `PUT /api/v1/medicalcases/{id}/suspend` | 挂起医案 | 200 |
| TC-MC-002-04 | `PUT /api/v1/medicalcases/{id}/cancel` | 取消医案 | 204 |
| TC-MC-002-05 | `PUT /api/v1/medicalcases/{id}/status` | 已完成医案再次修改 | 400, 不允许 |

### TC-MC-003: 医案审计

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-MC-003-01 | `GET /api/v1/medicalcases/{id}/permissions` | 获取权限 | 200, MedicalCasePermissionDto |
| TC-MC-003-02 | `GET /api/v1/medicalcases/{id}/audit-logs` | 获取审计日志 | 200, PagedResult<MedicalCaseAuditLogDto> |

### TC-MC-004: 医案打印

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-MC-004-01 | `PUT /api/v1/medicalcases/{id}/print-completed` | 记录打印完成 | 200 |
| TC-MC-004-02 | `POST /api/v1/medicalcases/{id}/print-logs` | 添加打印日志 | 200 |

---

## 中药管理测试

### TC-HERB-001: 药材 CRUD

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-HERB-001-01 | `POST /api/v1/herbs` | 创建药材 | 201, HerbDetailDto |
| TC-HERB-001-02 | `GET /api/v1/herbs/{id}` | 获取药材详情 | 200 |
| TC-HERB-001-03 | `PUT /api/v1/herbs/{id}` | 更新药材信息 | 200 |
| TC-HERB-001-04 | `DELETE /api/v1/herbs/{id}` | 软删除药材 | 200 |
| TC-HERB-001-05 | `GET /api/v1/herbs` | 药材列表（含分类过滤） | 200, PagedResult |

**创建药材请求体示例**:
```json
{
  "Name": "柴胡",
  "PinYinCode": "CH",
  "Category": "解表药",
  "Origin": "河北",
  "Spec": "饮片",
  "Unit": "克",
  "Price": 0.50,
  "CostPrice": 0.35,
  "Effect": "疏散退热，疏肝解郁，升举阳气",
  "Usage": "煎服，3-10g"
}
```

### TC-HERB-002: 药材批量操作

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-HERB-002-01 | `POST /api/v1/herbs/batch-import` | 批量导入 10 条 | 200, 成功 10 条 |
| TC-HERB-002-02 | `POST /api/v1/herbs/batch-import` | 超过 10000 条 | 400, 超出限制 |
| TC-HERB-002-03 | `GET /api/v1/herbs/export` | 导出 Excel | 200, .xlsx 文件 |
| TC-HERB-002-04 | `GET /api/v1/herbs/check-reference` | 检查引用 | 200, HerbReferenceCheckDto |

### TC-HERB-003: 药材导入导出

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-HERB-003-01 | `POST /api/v1/herbs/import` | 上传 .xlsx 文件 | 200, ImportResultDto |
| TC-HERB-003-02 | `POST /api/v1/herbs/import` | 非 .xlsx 文件 | 400 |
| TC-HERB-003-03 | `GET /api/v1/herbs/import-template` | 下载模板 | 200, .xlsx |
| TC-HERB-003-04 | `GET /api/v1/herbs/export-all` | 导出所有 JSON | 200, JSON 数组 |

---

## 验方管理测试

### TC-FORM-001: 验方 CRUD

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-FORM-001-01 | `POST /api/v1/formulas` | 创建验方（含药材列表） | 201, FormulaDetailDto |
| TC-FORM-001-02 | `GET /api/v1/formulas/{id}` | 获取验方详情（含药材明细） | 200, Herbs 数组 |
| TC-FORM-001-03 | `PUT /api/v1/formulas/{id}` | 更新验方 | 200 |
| TC-FORM-001-04 | `DELETE /api/v1/formulas/{id}` | 删除验方 | 200 |

**创建验方请求体示例**:
```json
{
  "Name": "逍遥散",
  "Effect": "疏肝解郁，健脾养血",
  "Usage": "水煎服",
  "Property": "调和肝脾",
  "Category": "经方",
  "IsShared": true,
  "Indications": "肝郁血虚脾弱证",
  "Herbs": [
    {
      "HerbId": "{{testHerbId}}",
      "HerbName": "柴胡",
      "Dosage": 10,
      "Unit": "克",
      "Usage": "君药",
      "SortOrder": 1
    }
  ]
}
```

### TC-FORM-002: 验方批量操作

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-FORM-002-01 | `POST /api/v1/formulas/batch-import` | 批量导入验方 | 200 |
| TC-FORM-002-02 | `GET /api/v1/formulas/export` | 导出验方 Excel | 200, .xlsx |
| TC-FORM-002-03 | `GET /api/v1/formulas/pending-validation` | 获取待验证验方 | 200, 数组 |
| TC-FORM-002-04 | `POST /api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate` | 验证药材 | 200 |

---

## 同步模块测试

### TC-SYNC-001: 同步操作

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-SYNC-001-01 | `GET /api/v1/sync/entity-types` | 获取实体类型列表 | 200, 字符串数组 |
| TC-SYNC-001-02 | `GET /api/v1/sync/metadata?entityType=Patient` | 获取元数据 | 200, SyncMetadataDto 数组 |
| TC-SYNC-001-03 | `POST /api/v1/sync/compare` | 比较本地与远程 | 200, SyncDiffDto |
| TC-SYNC-001-04 | `POST /api/v1/sync/upload` | 上传数据 | 200, SyncUploadResultDto |
| TC-SYNC-001-05 | `POST /api/v1/sync/download` | 下载数据 | 200, SyncDownloadResultDto |
| TC-SYNC-001-06 | `POST /api/v1/sync/delete` | 删除同步数据 | 200, SyncDeleteResultDto |

---

## 挂号管理测试

### TC-REG-001: 挂号操作

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-REG-001-01 | `POST /api/v1/registrations` | 创建挂号 | 201, RegistrationDetailDto |
| TC-REG-001-02 | `GET /api/v1/registrations/{id}` | 获取挂号详情 | 200 |
| TC-REG-001-03 | `GET /api/v1/registrations` | 挂号列表（含日期过滤） | 200, PagedResult |
| TC-REG-001-04 | `GET /api/v1/registrations/queue` | 获取排队列表 | 200, 数组 |
| TC-REG-001-05 | `PUT /api/v1/registrations/{id}/start-visit` | 开始就诊 | 200 |
| TC-REG-001-06 | `PUT /api/v1/registrations/{id}/cancel` | 取消挂号 | 200 |

---

## 系统诊断测试

### TC-DIAG-001: 日志管理（需 SuperAdmin）

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-DIAG-001-01 | `GET /api/v1/diagnostics/logging/status` | 获取日志状态 | 200 |
| TC-DIAG-001-02 | `POST /api/v1/diagnostics/logging/debug/enable` | 启用调试模式 | 200 |
| TC-DIAG-001-03 | `POST /api/v1/diagnostics/logging/debug/disable` | 禁用调试模式 | 200 |
| TC-DIAG-001-04 | `POST /api/v1/diagnostics/logging/level` | 设置日志级别 | 200 |
| TC-DIAG-001-05 | 以上端点 | 非 SuperAdmin 调用 | 403 |

---

## 健康检查测试

### TC-HEALTH-001: 健康检查（无需认证）

| 测试编号 | 端点 | 测试用例 | 预期结果 |
|----------|------|----------|----------|
| TC-HEALTH-001-01 | `GET /api/v1/health` | 基础健康检查 | 200, HealthCheckResponse |
| TC-HEALTH-001-02 | `GET /api/v1/health/ping` | Ping 测试 | 200, "pong" |
| TC-HEALTH-001-03 | `GET /api/v1/health/details` | 详细健康信息（需认证） | 200, 含数据库状态等 |
| TC-HEALTH-001-04 | `GET /api/v1/health/details` | 未认证请求 | 401 |

---

## 通用断言规则

### ApiResponse 结构验证

每个 API 响应应满足以下结构（除 DELETE 返回 204 的端点外）：

```javascript
pm.test("ApiResponse structure", () => {
    const json = pm.response.json();
    pm.expect(json).to.have.property("success");
    pm.expect(json).to.have.property("message");
    pm.expect(json).to.have.property("data");
    pm.expect(json).to.have.property("timestamp");
    pm.expect(json.timestamp).to.be.a("number");
});
```

### PagedResult 分页验证

```javascript
pm.test("PagedResult structure", () => {
    const data = pm.response.json().data;
    pm.expect(data).to.have.property("items");
    pm.expect(data).to.have.property("totalCount");
    pm.expect(data).to.have.property("currentPage");
    pm.expect(data).to.have.property("pageSize");
    pm.expect(data).to.have.property("totalPages");
    pm.expect(data.items).to.be.an("array");
    pm.expect(data.totalCount).to.be.at.least(0);
    pm.expect(data.currentPage).to.be.at.least(1);
});
```

### 认证失败验证

```javascript
pm.test("Unauthorized returns 401", () => {
    pm.expect(pm.response.code).to.be.oneOf([401, 403]);
});
```

### 业务失败验证

```javascript
pm.test("Business failure", () => {
    const json = pm.response.json();
    pm.expect(json.success).to.be.false;
    pm.expect(json.message).to.be.a("string").and.not.empty;
});
```

### 响应时间验证

```javascript
pm.test("Response time < 5000ms", () => {
    pm.expect(pm.response.responseTime).to.be.below(5000);
});
```

---

## 错误场景测试

### TC-ERR-001: 认证错误

| 测试编号 | 场景 | 方法 | 预期 |
|----------|------|------|------|
| TC-ERR-001-01 | 无 Token 访问受保护端点 | GET /api/v1/users | 401 |
| TC-ERR-001-02 | 过期 Token | GET /api/v1/users | 401 |
| TC-ERR-001-03 | 伪造 Token | GET /api/v1/users | 401 |

### TC-ERR-002: 权限错误

| 测试编号 | 场景 | 方法 | 预期 |
|----------|------|------|------|
| TC-ERR-002-01 | Doctor 访问 AdminOnly 端点 | POST /api/v1/users | 403 |
| TC-ERR-002-02 | Receptionist 访问 DoctorOrAdmin 端点 | POST /api/v1/medicalcases | 403 |
| TC-ERR-002-03 | Doctor 访问 SuperAdminOnly 端点 | POST /api/v1/diagnostics/logging/debug/enable | 403 |

### TC-ERR-003: 数据验证错误

| 测试编号 | 场景 | 方法 | 预期 |
|----------|------|------|------|
| TC-ERR-003-01 | 缺少必填字段 | POST /api/v1/patients (无 Name) | 400 ValidationProblemDetails |
| TC-ERR-003-02 | 字段长度超限 | POST /api/v1/users (UserName > 32) | 400 |
| TC-ERR-003-03 | 无效的 GUID 格式 | GET /api/v1/users/invalid-guid | 400 |
| TC-ERR-003-04 | 无效的枚举值 | PUT /api/v1/medicalcases/{id}/status (status=999) | 400 |

### TC-ERR-004: 资源不存在

| 测试编号 | 场景 | 方法 | 预期 |
|----------|------|------|------|
| TC-ERR-004-01 | 获取不存在的用户 | GET /api/v1/users/00000000-0000-0000-0000-000000000000 | 404 |
| TC-ERR-004-02 | 获取不存在的患者 | GET /api/v1/patients/00000000-0000-0000-0000-000000000000 | 404 |
| TC-ERR-004-03 | 获取不存在的医案 | GET /api/v1/medicalcases/00000000-0000-0000-0000-000000000000 | 404 |

### TC-ERR-005: 并发与边界

| 测试编号 | 场景 | 方法 | 预期 |
|----------|------|------|------|
| TC-ERR-005-01 | 批量删除空列表 | POST /api/v1/users/batch-delete {"Ids":[]} | 400 |
| TC-ERR-005-02 | 批量导入超限 | POST /api/v1/herbs/batch-import (>10000 条) | 400 |
| TC-ERR-005-03 | 文件上传超限 | POST /api/v1/patients/import (>10MB) | 400 |
| TC-ERR-005-04 | 页码为 0 | GET /api/v1/users?page=0&pageSize=20 | 400 或自动修正为 1 |
| TC-ERR-005-05 | pageSize 为 0 | GET /api/v1/users?page=1&pageSize=0 | 400 或自动修正 |

---

## 测试覆盖率统计

| 模块 | 端点数 | 测试用例数 | 覆盖场景 |
|------|--------|------------|----------|
| Auth | 5 | 17 | 登录/登出/刷新/验证/自动登录 |
| Users | 14 | 15 | CRUD/批量/重置密码/状态切换 |
| Patients | 12 | 11 | CRUD/导入导出/引用检查 |
| Medical Cases | 12 | 8 | CRUD/列表/搜索/批量查询 |
| Medical Case Workflow | 4 | 5 | 状态流转/关闭/挂起/取消 |
| Medical Case Audit | 2 | 2 | 权限/审计日志 |
| Medical Case Print | 2 | 2 | 打印记录/打印日志 |
| Herbs | 17 | 11 | CRUD/导入导出/引用检查/批量操作 |
| Formulas | 15 | 8 | CRUD/导入导出/验证/批量操作 |
| Sync | 6 | 6 | 同步全流程 |
| Registrations | 6 | 6 | 挂号全流程 |
| Diagnostics | 4 | 5 | 日志管理+权限测试 |
| Health | 3 | 4 | 健康检查 |
| **通用/错误** | - | 20 | 认证/权限/验证/资源/边界 |
| **合计** | **102** | **120** | - |

---

## 附录: Postman 测试脚本模板

### 预请求脚本 - 设置动态时间戳

```javascript
// 设置当前时间戳
pm.environment.set("currentTimestamp", new Date().toISOString());
// 生成随机字符串用于测试
pm.environment.set("randomString", Math.random().toString(36).substring(7));
// 生成随机 GUID
function generateGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}
pm.environment.set("testGuidId", generateGuid());
```

### 测试脚本 - 自动保存 Token

```javascript
// 仅在登录端点使用
if (pm.request.url.toString().includes("/auth/login") && pm.response.code === 200) {
    const json = pm.response.json();
    if (json.success && json.data) {
        pm.environment.set("authToken", json.data.token);
        pm.environment.set("refreshToken", json.data.refreshToken);
        if (json.data.user) {
            pm.environment.set("currentUserId", json.data.user.id);
            pm.environment.set("currentUsername", json.data.user.userName);
        }
        console.log("Token saved to environment");
    }
}
```

### 测试脚本 - 保存创建资源的 ID

```javascript
// 在 POST 创建资源后自动保存 ID
if (pm.request.method === "POST" && pm.response.code === 201) {
    const json = pm.response.json();
    if (json.success && json.data && json.data.id) {
        // 根据请求 URL 决定存储到哪个变量
        const url = pm.request.url.toString();
        if (url.includes("/patients")) {
            pm.environment.set("testPatientId", json.data.id);
        } else if (url.includes("/herbs")) {
            pm.environment.set("testHerbId", json.data.id);
        } else if (url.includes("/formulas")) {
            pm.environment.set("testFormulaId", json.data.id);
        } else if (url.includes("/medicalcases")) {
            pm.environment.set("testMedicalCaseId", json.data.id);
        }
        console.log("Resource ID saved:", json.data.id);
    }
}
```
