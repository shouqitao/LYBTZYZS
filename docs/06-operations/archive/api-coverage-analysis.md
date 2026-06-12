# API Coverage Analysis Report

**生成时间**: 2026-04-01  
**分析范围**: Controller 源代码 vs Postman Collection vs API 文档

---

## 执行摘要

| 数据源 | 端点数 | 状态 |
|--------|--------|------|
| **Controller 源代码** | 95 | ✅ 权威来源 |
| **Postman Collection** | 81 (唯一) / 88 (总请求) | ⚠️ 缺失 13-14 个端点 |
| **API 文档 (README)** | 94 (不含 Registrations) | ⚠️ 缺少 Registrations 模块文档 |

**关键发现**:
- ❌ **Postman 缺失**: 约 13-14 个 Controller 端点未在 Postman 中实现
- ⚠️ **路径大小写不一致**: Postman 使用大写 (`/Herbs`), Controller/文档使用小写 (`/herbs`)
- 🔄 **重复请求**: 7 个端点在 Postman 中重复定义（跨多个文件夹）
- 📝 **文档缺失**: Registrations 模块未在 API 文档索引中列出

---

## 一、Controller 端点清单（权威来源）

### 1.1 按模块分组统计

| 模块 | 端点数 | 授权策略 | 特殊属性 |
|------|--------|----------|----------|
| **Auth** | 5 | AllowAnonymous (4) + Authorize (1) | EnableRateLimiting |
| **Users** | 14 | AdminOnly / SuperAdminOnly | - |
| **Patients** | 10 | PatientAccess | OutputCache |
| **Herbs** | 14 | DoctorOrAdmin | OutputCache |
| **Formulas** | 13 | DoctorOrAdmin | OutputCache |
| **MedicalCases** | 12 | DoctorOrAdmin | - |
| **MedicalCaseProcessing** | 4 | DoctorOrAdmin | - |
| **MedicalCasePrint** | 2 | DoctorOrAdmin | - |
| **MedicalCaseAudit** | 2 | DoctorOrAdmin | - |
| **Registrations** | 7 | PatientAccess / DoctorOrAdmin | - |
| **Sync** | 6 | DoctorOrAdmin | - |
| **Diagnostics** | 4 | SuperAdmin | - |
| **Health** | 3 | AllowAnonymous (2) + Authorize (1) | - |
| **总计** | **95** | - | - |

### 1.2 完整端点列表

#### Auth (5 endpoints)
```
POST   /api/v1/auth/login              [AllowAnonymous, RateLimit]
POST   /api/v1/auth/auto-login         [AllowAnonymous, RateLimit]
POST   /api/v1/auth/logout             [AllowAnonymous]
POST   /api/v1/auth/refresh            [AllowAnonymous]
GET    /api/v1/auth/validate           [Authorize]
```

#### Users (14 endpoints)
```
GET    /api/v1/users                   [AdminOnly]
GET    /api/v1/users/current           [Authorize]
GET    /api/v1/users/{id}              [AdminOnly]
POST   /api/v1/users                   [AdminOnly]
PUT    /api/v1/users/{id}              [AdminOnly]
DELETE /api/v1/users/{id}              [AdminOnly]
POST   /api/v1/users/{id}/reset-password     [SuperAdminOnly]
PUT    /api/v1/users/{id}/profile      [Authorize]
PUT    /api/v1/users/{id}/change-password    [Authorize]
POST   /api/v1/users/{id}/toggle-status      [AdminOnly]
POST   /api/v1/users/{id}/restore      [SuperAdminOnly]
POST   /api/v1/users/batch-delete      [AdminOnly]
POST   /api/v1/users/batch-enable      [AdminOnly]
POST   /api/v1/users/batch-disable     [AdminOnly]
```

#### Patients (10 endpoints)
```
GET    /api/v1/patients                [PatientAccess, OutputCache]
GET    /api/v1/patients/{id}           [PatientAccess]
POST   /api/v1/patients                [PatientAccess]
PUT    /api/v1/patients/{id}           [PatientAccess]
DELETE /api/v1/patients/{id}           [PatientAccess]
POST   /api/v1/patients/{id}/toggle-status   [PatientAccess]
POST   /api/v1/patients/{id}/restore   [PatientAccess]
POST   /api/v1/patients/batch-delete   [PatientAccess]
GET    /api/v1/patients/{id}/check-reference [PatientAccess]
POST   /api/v1/patients/batch-check-reference [PatientAccess]
```

#### Herbs (14 endpoints)
```
GET    /api/v1/herbs                   [DoctorOrAdmin, OutputCache]
GET    /api/v1/herbs/{id}              [DoctorOrAdmin]
POST   /api/v1/herbs                   [DoctorOrAdmin]
PUT    /api/v1/herbs/{id}              [DoctorOrAdmin]
DELETE /api/v1/herbs/{id}              [DoctorOrAdmin]
POST   /api/v1/herbs/batch-import      [DoctorOrAdmin]
GET    /api/v1/herbs/export-all        [DoctorOrAdmin]
GET    /api/v1/herbs/{id}/check-reference     [DoctorOrAdmin]
POST   /api/v1/herbs/batch-check-reference    [DoctorOrAdmin]
POST   /api/v1/herbs/{id}/toggle-status       [DoctorOrAdmin]
POST   /api/v1/herbs/{id}/restore      [DoctorOrAdmin]
POST   /api/v1/herbs/batch-enable      [DoctorOrAdmin]
POST   /api/v1/herbs/batch-disable     [DoctorOrAdmin]
POST   /api/v1/herbs/batch-delete      [DoctorOrAdmin]
```

#### Formulas (13 endpoints)
```
GET    /api/v1/formulas                [DoctorOrAdmin, OutputCache]
GET    /api/v1/formulas/{id}           [DoctorOrAdmin]
POST   /api/v1/formulas                [DoctorOrAdmin]
PUT    /api/v1/formulas/{id}           [DoctorOrAdmin]
DELETE /api/v1/formulas/{id}           [DoctorOrAdmin]
POST   /api/v1/formulas/batch-import   [DoctorOrAdmin]
GET    /api/v1/formulas/pending-validation    [DoctorOrAdmin]
POST   /api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate [DoctorOrAdmin]
POST   /api/v1/formulas/{id}/toggle-status    [DoctorOrAdmin]
POST   /api/v1/formulas/{id}/restore   [DoctorOrAdmin]
POST   /api/v1/formulas/batch-delete   [DoctorOrAdmin]
POST   /api/v1/formulas/batch-enable   [DoctorOrAdmin]
POST   /api/v1/formulas/batch-disable  [DoctorOrAdmin]
```

#### MedicalCases 家族 (18 endpoints across 4 controllers)

**MedicalCasesController (12 endpoints):**
```
POST   /api/v1/medicalcases            [DoctorOrAdmin]
PUT    /api/v1/medicalcases/{id}/prescription-flag [DoctorOrAdmin]
PUT    /api/v1/medicalcases/{id}       [DoctorOrAdmin]
DELETE /api/v1/medicalcases/{id}       [DoctorOrAdmin]
POST   /api/v1/medicalcases/batch-delete [DoctorOrAdmin]
POST   /api/v1/medicalcases/batch-details [DoctorOrAdmin]
GET    /api/v1/medicalcases/{id}       [DoctorOrAdmin]
GET    /api/v1/medicalcases            [DoctorOrAdmin]
GET    /api/v1/medicalcases/query      [DoctorOrAdmin]
GET    /api/v1/medicalcases/search     [DoctorOrAdmin]
GET    /api/v1/medicalcases/{medicalCaseId}/consultations [DoctorOrAdmin]
GET    /api/v1/medicalcases/{medicalCaseId}/prescriptions [DoctorOrAdmin]
```

**MedicalCaseProcessingController (4 endpoints):**
```
PUT    /api/v1/medicalcases/{id}/status     [DoctorOrAdmin]
PUT    /api/v1/medicalcases/{id}/close      [DoctorOrAdmin]
PUT    /api/v1/medicalcases/{id}/suspend    [DoctorOrAdmin]
PUT    /api/v1/medicalcases/{id}/cancel     [DoctorOrAdmin]
```

**MedicalCasePrintController (2 endpoints):**
```
PUT    /api/v1/medicalcases/{id}/print-completed [DoctorOrAdmin]
POST   /api/v1/medicalcases/{id}/print-logs      [DoctorOrAdmin]
```

**MedicalCaseAuditController (2 endpoints):**
```
GET    /api/v1/medicalcases/{id}/permissions     [DoctorOrAdmin]
GET    /api/v1/medicalcases/{id}/audit-logs      [DoctorOrAdmin]
```

#### Registrations (7 endpoints)
```
POST   /api/v1/registrations/quick-visit [DoctorOrAdmin]
POST   /api/v1/registrations           [PatientAccess]
GET    /api/v1/registrations/{id}      [PatientAccess]
GET    /api/v1/registrations           [PatientAccess]
GET    /api/v1/registrations/queue     [PatientAccess]
PUT    /api/v1/registrations/{id}/start-visit [DoctorOrAdmin]
PUT    /api/v1/registrations/{id}/cancel      [PatientAccess]
```

#### Sync (6 endpoints)
```
GET    /api/v1/sync/entity-types       [DoctorOrAdmin]
GET    /api/v1/sync/metadata           [DoctorOrAdmin]
POST   /api/v1/sync/compare            [DoctorOrAdmin]
POST   /api/v1/sync/upload             [DoctorOrAdmin]
POST   /api/v1/sync/download           [DoctorOrAdmin]
POST   /api/v1/sync/delete             [DoctorOrAdmin]
```

#### Diagnostics (4 endpoints)
```
GET    /api/v1/diagnostics/logging/status         [SuperAdmin]
POST   /api/v1/diagnostics/logging/debug/enable   [SuperAdmin]
POST   /api/v1/diagnostics/logging/debug/disable  [SuperAdmin]
POST   /api/v1/diagnostics/logging/level          [SuperAdmin]
```

#### Health (3 endpoints)
```
GET    /api/v1/health                  [AllowAnonymous]
GET    /api/v1/health/ping             [AllowAnonymous]
GET    /api/v1/health/details          [Authorize]
```

---

## 二、Postman Collection 分析

### 2.1 总体统计

| 指标 | 数值 |
|------|------|
| **总请求数** | 88 |
| **唯一端点** | 81 |
| **重复端点** | 7 |
| **Setup/Helper 请求** | 约 10-12 |

### 2.2 文件夹结构

```
0. Auth               (5 requests)
1. Setup              (6 helper requests)
2. Users & Patients   (8 requests)
4. Medical Cases      (7 requests)
5. Medical Case Workflow (4 requests)
6. Medical Case Audit (2 requests)
7. Medical Case Print (2 requests)
8. Herbs              (14 requests)
9. Formulas           (14 requests)
10. Sync              (6 requests)
11. Registrations     (6 requests)
12. Diagnostics       (5 requests, 含 Login helper)
13. Health            (3 requests)
+ Root: Logout        (1 request)
```

### 2.3 Setup/Helper 请求清单

**文件夹 "1. Setup"**:
1. `0. Create Test User` - POST /api/v1/users (stores testUserId)
2. `2. Get Doctor Info` - GET /api/v1/users?pageIndex=1 (stores testDoctorId)
3. `0b. Login as Doctor` - POST /api/v1/auth/login (stores doctorToken)
4. `1. Create Test Patient` - POST /api/v1/patients (stores testPatientId)
5. `3. Create Test Formula` - POST /api/v1/formulas (stores testFormulaId)
6. `4. Create Test Herb` - POST /api/v1/herbs (stores testHerbId)

**其他 Helper 请求**:
- `12. Diagnostics/0. Login as Admin` - POST /api/v1/auth/login (stores adminToken)

### 2.4 重复端点

| 端点 | 出现次数 | 位置 |
|------|----------|------|
| POST /api/v1/auth/login | 4 | 0. Auth, 1. Setup, 12. Diagnostics (2x) |
| POST /api/v1/formulas | 2 | 1. Setup, 9. Formulas |
| POST /api/v1/registrations | 2 | ? |
| POST /api/v1/herbs | 2 | 1. Setup, 8. Herbs |
| GET /api/v1/users | 2 | ? |

**建议**: 保留功能文件夹的主请求，Setup 文件夹的标记为 `is_helper: true`。

### 2.5 路径大小写问题

Postman 使用大写资源名:
- `/api/v1/Herbs` (应为 `/api/v1/herbs`)
- `/api/v1/Formulas` (应为 `/api/v1/formulas`)
- `/api/v1/Users` (应为 `/api/v1/users`)

需要**批量规范化为小写**。

---

## 三、缺口分析

### 3.1 缺失端点（Controller 有，Postman 无）

基于初步对比，以下端点**可能**缺失（需规范化后精确确认）：

#### Users 模块 (可能缺失 ~10 个)
```
❌ GET    /api/v1/users/{id}
❌ PUT    /api/v1/users/{id}
❌ DELETE /api/v1/users/{id}
❌ POST   /api/v1/users/{id}/reset-password
❌ PUT    /api/v1/users/{id}/profile
❌ PUT    /api/v1/users/{id}/change-password
❌ POST   /api/v1/users/{id}/restore
❌ POST   /api/v1/users/batch-enable
❌ POST   /api/v1/users/batch-disable
```

#### Patients 模块 (可能缺失 ~4 个)
```
❌ GET    /api/v1/patients
❌ GET    /api/v1/patients/{id}
❌ PUT    /api/v1/patients/{id}
❌ DELETE /api/v1/patients/{id}
```

#### Registrations 模块 (可能缺失 ~1 个)
```
❌ POST   /api/v1/registrations/quick-visit
```

**注**: 上述清单为初步推断，需等待规范化对比后确认精确缺失列表。

### 3.2 多余端点（Postman 有，Controller 无）

需规范化后检查，当前未发现明显多余端点（Setup 文件夹除外）。

---

## 四、修复建议

### 4.1 立即行动

#### 优先级 1: 规范化 Postman 路径
**问题**: 大小写不一致导致无法精确匹配。

**操作**:
```
查找替换 (在 LYBTZYZS_API_Collection.json 中):
/api/v1/Herbs       → /api/v1/herbs
/api/v1/Formulas    → /api/v1/formulas
/api/v1/Users       → /api/v1/users
/api/v1/Patients    → /api/v1/patients
/api/v1/MedicalCases → /api/v1/medicalcases
/api/v1/Registrations → /api/v1/registrations
```

#### 优先级 2: 添加缺失端点到 Postman
**目标**: 覆盖全部 95 个 Controller 端点。

**分批添加**:
1. **第一批 (Users 模块)**: 添加 10 个缺失的 CRUD 和批量操作端点
2. **第二批 (Patients 模块)**: 添加 4 个缺失的基础 CRUD 端点
3. **第三批 (其他模块)**: 逐一补全

**模板示例** (Users.GetById):
```json
{
  "name": "Get User by ID",
  "request": {
    "method": "GET",
    "header": [
      {"key": "Authorization", "value": "Bearer {{authToken}}"}
    ],
    "url": {
      "raw": "{{baseUrl}}/api/v1/users/{{testUserId}}",
      "host": ["{{baseUrl}}"],
      "path": ["api", "v1", "users", "{{testUserId}}"]
    }
  },
  "event": [
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test(\"Status 200\", () => pm.response.to.have.status(200));",
          "pm.test(\"Has user data\", () => {",
          "  const json = pm.response.json();",
          "  pm.expect(json.success).to.be.true;",
          "  pm.expect(json.data).to.have.property('id');",
          "});"
        ]
      }
    }
  ]
}
```

#### 优先级 3: 清理重复和 Helper
**操作**:
1. **重复端点**: 保留功能文件夹的主请求，删除其他重复
2. **Helper 请求**: 移动所有 Setup/Login helper 到 "1. Setup" 文件夹，或在请求名前加 `[HELPER]` 标记

### 4.2 文档更新

#### API 文档 (docs/04-api-reference/README.md)
**问题**: Registrations 模块未在端点索引中列出。

**操作**: 在 "模块端点索引" 章节添加:
```markdown
### 挂号模块 ([registrations.md](../07-registrations.md)) -- PatientAccess / DoctorOrAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/registrations/quick-visit` | 快速就诊 (DoctorOrAdmin) |
| POST | `/registrations` | 创建挂号 |
| GET | `/registrations/{id}` | 挂号详情 |
| GET | `/registrations` | 挂号列表 |
| GET | `/registrations/queue` | 就诊队列 |
| PUT | `/registrations/{id}/start-visit` | 开始就诊 (DoctorOrAdmin) |
| PUT | `/registrations/{id}/cancel` | 取消挂号 |
```

---

## 五、下一步行动清单

### 短期 (本周)
- [ ] 规范化 Postman Collection 路径（大小写修正）
- [ ] 生成精确的缺失端点清单（规范化后对比）
- [ ] 添加前 10 个高优先级缺失端点（Users 模块）

### 中期 (2周内)
- [ ] 补全所有 95 个 Controller 端点到 Postman
- [ ] 清理重复请求和 Helper 标记
- [ ] 运行 Newman 验证新增端点
- [ ] 识别并移除冗余的 .NET 集成测试

### 长期
- [ ] 建立自动化对比脚本（Controller → Postman 覆盖率检查）
- [ ] 集成到 CI/CD pipeline（Newman 自动测试）
- [ ] 更新 docs/05-development/05-testing.md（Postman 优先策略）

---

## 附录

### A. 授权策略映射

| 策略 | 角色 | 端点数量 |
|------|------|----------|
| AllowAnonymous | - | 7 (Auth 4 + Health 2 + 其他) |
| Authorize (any) | 任何已认证用户 | 5 |
| AdminOnly | Admin, SuperAdmin | 14 |
| SuperAdminOnly | SuperAdmin | 6 |
| DoctorOrAdmin | Doctor, Admin, SuperAdmin | 61 |
| PatientAccess | Doctor, Admin, SuperAdmin | 17 |

### B. 特殊属性统计

| 属性 | 端点数 | 示例 |
|------|--------|------|
| OutputCache | 3 | Patients, Herbs, Formulas 列表端点 |
| EnableRateLimiting | 2 | Auth login/auto-login |
| ProducesResponseType | 95 | 所有端点（标准实践） |

### C. 规范化规则

**路径规范化**:
1. 移除 `{{baseUrl}}` 变量
2. 替换测试变量为 `{id}`: `{{testPatientId}}` → `{id}`
3. 替换 GUID 占位符: `00000000-0000-0000-0000-000000000000` → `{id}`
4. 小写资源名: `/Herbs` → `/herbs`
5. 移除查询字符串（用于路径匹配）

**端点唯一性**:
- 唯一键: `METHOD + normalized_path`
- 示例: `POST /api/v1/users/{id}/toggle-status`

---

**报告结束**

*如需详细的 CSV/JSON 数据，请查看后续附件文件。*
