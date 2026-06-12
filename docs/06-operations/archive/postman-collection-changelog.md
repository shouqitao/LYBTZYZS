# Postman Collection 更新日志

**版本**: v2.1.0 → v2.2.0  
**更新日期**: 2026-04-01  
**文件**: `docs/06-operations/LYBTZYZS_API_Collection.json`

---

## 更新摘要

| 指标 | 更新前 | 更新后 | 变化 |
|------|--------|--------|------|
| **总请求数** | 88 | 100 | +12 |
| **唯一端点覆盖** | 81/95 | 95/95 | +14 endpoints |
| **覆盖率** | 84.2% | **100%** | +15.8% |
| **重复端点** | 7 | 0 | -7 |
| **Setup 辅助请求标记** | 0/6 | 5/5 | ✅ 全部标记 |
| **路径规范化** | 大小写混合 | **全小写** | ✅ 修复 |

---

## 主要变更

### 1. 路径规范化 (Phase 1)

**问题**: Postman 路径使用大写资源名 (`/api/v1/Herbs`),与 Controller 小写路径 (`/api/v1/herbs`) 不一致,导致大小写敏感服务器路由失败。

**修复**:
- 所有路径统一为小写: `herbs`, `formulas`, `users`, `patients`, `medicalcases`, `registrations`
- 修复范围: 路径数组 (`"path": ["api", "v1", "herbs"]`) + 原始 URL (`{{baseUrl}}/api/v1/herbs`)
- 影响请求数: ~88 个请求全部规范化

**验证**:
```powershell
# 验证无大写资源名残留
$json = Get-Content "LYBTZYZS_API_Collection.json" -Raw
$json -match '\/api\/v1\/[A-Z]' # 应返回 False
```

---

### 2. 新增缺失端点 (Phase 2)

#### 2.1 Users 模块 (+10 endpoints)

| 端点 | 方法 | 授权 | 说明 |
|------|------|------|------|
| `/api/v1/users/{id}` | GET | AdminOnly | 获取用户详情 |
| `/api/v1/users/{id}` | PUT | AdminOnly | 更新用户信息 |
| `/api/v1/users/{id}` | DELETE | AdminOnly | 软删除用户 (返回 200) |
| `/api/v1/users/{id}/change-password` | PUT | Authorize | 修改密码 (OldPassword + NewPassword) |
| `/api/v1/users/{id}/reset-password` | POST | SuperAdminOnly | 重置密码 (返回临时密码) |
| `/api/v1/users/{id}/profile` | PUT | Authorize | 修改个人资料 (RealName + PhoneNumber) |
| `/api/v1/users/{id}/restore` | POST | AdminOnly | 恢复软删除用户 |
| `/api/v1/users/batch-delete` | POST | AdminOnly | 批量删除用户 |
| `/api/v1/users/batch-enable` | POST | AdminOnly | 批量启用用户 |
| `/api/v1/users/batch-disable` | POST | AdminOnly | 批量禁用用户 |

**测试脚本模板**:
```javascript
pm.test('Status code is 200', function () {
    pm.response.to.have.status(200);
});
pm.test('ApiResponse structure is valid', function () {
    const j = pm.response.json();
    pm.expect(j).to.have.property('success');
    pm.expect(j).to.have.property('message');
});
```

#### 2.2 Patients 模块 (+4 endpoints)

| 端点 | 方法 | 授权 | 说明 |
|------|------|------|------|
| `/api/v1/patients` | GET | PatientAccess | 分页查询患者列表 |
| `/api/v1/patients/{id}` | GET | PatientAccess | 获取患者详情 |
| `/api/v1/patients/{id}` | PUT | PatientAccess | 更新患者信息 |
| `/api/v1/patients/{id}` | DELETE | PatientAccess | 软删除患者 |

---

### 3. 清理重复 & 标记辅助请求 (Phase 3)

#### 3.1 移除重复端点

| 端点 | 原出现位置 | 保留位置 | 操作 |
|------|-----------|---------|------|
| `POST /api/v1/auth/login` | 3 处 (Auth, Setup, Diagnostics) | **0. Auth** | 从 Setup & Diagnostics 移除 |
| `POST /api/v1/herbs` | 2 处 (Setup, Herbs) | **8. Herbs** | Setup 版本标记为 `[HELPER]` |
| `POST /api/v1/formulas` | 2 处 (Setup, Formulas) | **9. Formulas** | Setup 版本标记为 `[HELPER]` |
| `POST /api/v1/users` | 2 处 (Setup, Users) | **2. Users & Patients** | Setup 版本标记为 `[HELPER]` |

**净减少**: 2 个纯重复请求 (login 从 Setup & Diagnostics 移除)

#### 3.2 Setup 辅助请求标记

所有 "1. Setup" 文件夹中的请求现已标记为 `[HELPER]`,明确其作为测试夹具的角色:

1. `[HELPER] Create Test User`
2. `[HELPER] Login as Doctor`
3. `[HELPER] Create Test Patient`
4. `[HELPER] Create Test Formula`
5. `[HELPER] Create Test Herb`

#### 3.3 消除歧义命名

| 原名称 | 新名称 | 原因 |
|--------|--------|------|
| `Toggle Status` (2 个同名) | `Toggle User Status` | 明确针对用户状态切换 |
| `Toggle Status` (2 个同名) | `Toggle Patient Status` | 明确针对患者状态切换 |

#### 3.4 补充缺失的规范端点

发现问题: `POST /api/v1/users` (Create User) 只存在于 Setup 辅助请求中,缺少规范的功能测试端点。

**新增**: "Create User" 请求到 "2. Users & Patients" 文件夹
- 方法: `POST /api/v1/users [AdminOnly]`
- 响应: 201 Created + Location header (CreatedAtAction 模式)
- 测试脚本: 验证 201 状态码, ApiResponse 结构, Location header

---

## 当前状态

### 文件夹结构

```
LYBTZYZS_API_Collection.json
├── 0. Auth (4 requests)
│   ├── Login ✅
│   ├── Auto Login
│   ├── Refresh Token
│   └── Validate Token
├── 1. Setup (5 requests) [ALL MARKED AS HELPERS]
│   ├── [HELPER] Create Test User
│   ├── [HELPER] Login as Doctor
│   ├── [HELPER] Create Test Patient
│   ├── [HELPER] Create Test Formula
│   └── [HELPER] Create Test Herb
├── 2. Users & Patients (23 requests) ✅ 100% coverage
│   ├── List Users
│   ├── Get Current User
│   ├── Create User ✅ NEW
│   ├── Get User ✅ NEW
│   ├── Update User ✅ NEW
│   ├── Delete User ✅ NEW
│   ├── Change Password ✅ NEW
│   ├── Reset Password ✅ NEW
│   ├── Change Profile ✅ NEW
│   ├── Restore User ✅ NEW
│   ├── Batch Delete Users ✅ NEW
│   ├── Batch Enable Users ✅ NEW
│   ├── Batch Disable Users ✅ NEW
│   ├── Toggle User Status (renamed)
│   ├── Toggle Patient Status (renamed)
│   ├── Restore Patient
│   ├── Batch Delete
│   ├── Check Reference
│   ├── Batch Check Reference
│   ├── Get Patients ✅ NEW
│   ├── Get Patient ✅ NEW
│   ├── Update Patient ✅ NEW
│   └── Delete Patient ✅ NEW
├── 4-13. Other Modules (68 requests) ✅ 100% coverage
│   └── (Herbs, Formulas, MedicalCases, Registrations, Sync, Diagnostics, Health)
└── Logout (standalone request)

Total: 100 requests
```

### 覆盖率明细

| 模块 | Controller 端点数 | Postman 覆盖数 | 覆盖率 |
|------|------------------|---------------|--------|
| Auth | 4 | 4 | 100% ✅ |
| Users | 14 | 14 | **100%** ✅ (从 28.6% 提升) |
| Patients | 10 | 10 | **100%** ✅ (从 60% 提升) |
| Herbs | 14 | 14 | 100% ✅ |
| Formulas | 13 | 13 | 100% ✅ |
| MedicalCases | 26 | 26 | 100% ✅ |
| Registrations | 7 | 7 | 100% ✅ |
| Sync | 2 | 2 | 100% ✅ |
| Diagnostics | 2 | 2 | 100% ✅ |
| Health | 2 | 2 | 100% ✅ |
| Print | 1 | 1 | 100% ✅ |
| **总计** | **95** | **95** | **100%** ✅ |

---

## 测试验证

### 本地验证步骤

#### 前提条件
1. 安装 Newman: `npm install -g newman`
2. 启动 WebAPI: `dotnet run --project src/Server/Services/LYBT.WebAPI`
3. 确保数据库可用 (SQL Server 或 SQLite)

#### 执行测试

```powershell
# 1. 完整测试套件 (包含 Setup)
newman run "docs/06-operations/LYBTZYZS_API_Collection.json" `
    --environment "docs/06-operations/LYBTZYZS_Environment.json" `
    --reporters cli,json `
    --reporter-json-export "test-results/postman-report.json"

# 2. 跳过 Setup 辅助请求 (仅功能测试)
newman run "docs/06-operations/LYBTZYZS_API_Collection.json" `
    --environment "docs/06-operations/LYBTZYZS_Environment.json" `
    --folder "0. Auth" `
    --folder "2. Users & Patients" `
    --folder "8. Herbs" `
    --folder "9. Formulas" `
    # ... (其他功能文件夹)

# 3. 仅测试新增端点
newman run "docs/06-operations/LYBTZYZS_API_Collection.json" `
    --environment "docs/06-operations/LYBTZYZS_Environment.json" `
    --folder "2. Users & Patients" `
    --filter-request "Get User|Update User|Delete User|Change Password|Reset Password|Change Profile|Restore User|Batch*"
```

#### 预期结果

- **总请求数**: 100
- **测试通过率**: ≥ 95% (部分端点依赖数据状态可能失败)
- **关键验证点**:
  - ✅ 所有路径均为小写 (无 404 路由错误)
  - ✅ 无重复端点执行
  - ✅ Setup helpers 按依赖顺序执行
  - ✅ 环境变量正确传递 (authToken, testUserId 等)

### CI/CD 集成

```yaml
# .github/workflows/api-tests.yml (示例)
name: API Integration Tests
on: [push, pull_request]

jobs:
  postman-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Start WebAPI
        run: dotnet run --project src/Server/Services/LYBT.WebAPI &
      - name: Install Newman
        run: npm install -g newman
      - name: Run Postman Tests
        run: |
          newman run docs/06-operations/LYBTZYZS_API_Collection.json \
            --environment docs/06-operations/LYBTZYZS_Environment.json \
            --reporters cli,junit \
            --reporter-junit-export test-results/postman-junit.xml
      - name: Publish Test Results
        uses: EnricoMi/publish-unit-test-result-action@v2
        if: always()
        with:
          files: test-results/postman-junit.xml
```

---

## 后续工作 (Phase 5)

### 移除冗余 .NET HTTP 测试

**目标**: 删除 `tests/LYBT.Tests.Server/` 中与 Postman 重复的 HTTP 测试,保留不可替代的集成测试。

**约束**: **仅在 Postman 测试验证通过后执行** (Hard Constraint)

#### 待移除测试文件候选

| 文件路径 | 说明 | 保留原因 (如有) |
|---------|------|---------------|
| `Api/Controllers/UsersControllerTests.cs` | Users API HTTP 测试 | 可能包含权限边界测试,需逐个评估 |
| `Api/Controllers/PatientsControllerTests.cs` | Patients API HTTP 测试 | 同上 |
| `Api/Controllers/HerbsControllerTests.cs` | Herbs API HTTP 测试 | Postman 已覆盖全部端点 |
| `Api/Controllers/FormulasControllerTests.cs` | Formulas API HTTP 测试 | Postman 已覆盖全部端点 |

**评估标准**:
- ✅ **可移除**: 纯端点可达性测试 (HTTP 200/201 验证)
- ✅ **可移除**: 简单请求/响应格式验证 (Postman 已覆盖)
- ❌ **保留**: 权限策略边界测试 (Postman 难以模拟所有用户上下文)
- ❌ **保留**: 数据库事务/并发测试 (需要 Respawn + DbContext 控制)
- ❌ **保留**: 复杂业务逻辑单元测试 (不属于 HTTP 测试范畴)

#### 保留的 .NET 测试类型

1. **Service 层单元测试** (`Services/`)
   - 业务逻辑验证 (与 HTTP 无关)
   - Mock Repository 的隔离测试
   
2. **Repository 层集成测试** (`Infrastructure/`)
   - EF Core 查询优化验证
   - 数据库约束测试 (唯一索引, 外键等)
   
3. **架构测试** (`tests/LYBT.Tests.Architecture/`)
   - 依赖规则验证 (NetArchTest)
   - 命名约定检查

---

## 附录

### A. 测试脚本标准模板

```javascript
// 1. 基础响应验证 (所有端点)
pm.test('Status code is 200', function () {
    pm.response.to.have.status(200);
});

pm.test('ApiResponse structure is valid', function () {
    const j = pm.response.json();
    pm.expect(j).to.have.property('success');
    pm.expect(j).to.have.property('message');
    pm.expect(j).to.have.property('timestamp');
});

// 2. 数据结构验证 (GET 端点)
pm.test('Response data is valid', function () {
    const j = pm.response.json();
    if (j.success && j.data) {
        pm.expect(j.data).to.have.property('id');
        pm.expect(j.data).to.have.property('userName'); // 根据 DTO 调整
    }
});

// 3. 分页响应验证 (List 端点)
pm.test('Pagination structure is valid', function () {
    const j = pm.response.json();
    if (j.success && j.data) {
        pm.expect(j.data).to.have.property('items');
        pm.expect(j.data).to.have.property('totalCount');
        pm.expect(j.data).to.have.property('currentPage');
        pm.expect(j.data).to.have.property('pageSize');
    }
});

// 4. 环境变量存储 (Create 端点)
pm.test('Store created ID', function () {
    const j = pm.response.json();
    if (j.success && j.data && j.data.id) {
        pm.environment.set('createdUserId', j.data.id);
    }
});

// 5. CreatedAtAction 验证 (POST 端点)
pm.test('Location header is present', function () {
    pm.response.to.have.header('Location');
    const location = pm.response.headers.get('Location');
    pm.expect(location).to.include('/api/v1/users/');
});
```

### B. 环境变量清单

| 变量名 | 类型 | 说明 | 设置时机 |
|--------|------|------|---------|
| `baseUrl` | 常量 | API 基础 URL | 环境配置 |
| `authToken` | 动态 | JWT 访问令牌 | Login 请求后 |
| `doctorToken` | 动态 | 医生角色令牌 | Setup: Login as Doctor |
| `adminToken` | 动态 | 管理员令牌 | 可选 (如需测试权限) |
| `testUserId` | 动态 | 测试用户 ID | Setup: Create Test User |
| `testPatientId` | 动态 | 测试患者 ID | Setup: Create Test Patient |
| `testHerbId` | 动态 | 测试草药 ID | Setup: Create Test Herb |
| `testFormulaId` | 动态 | 测试方剂 ID | Setup: Create Test Formula |
| `testRegistrationId` | 动态 | 测试挂号 ID | Setup: Create Test Registration |
| `uniquePhone` | 函数 | 唯一手机号 | `{{$timestamp}}` 动态生成 |
| `uniqueIdNumber` | 函数 | 唯一身份证号 | `{{$randomInt}}` 动态生成 |
| `uniqueHerbName` | 函数 | 唯一草药名 | `"草药_{{$timestamp}}"` |
| `uniqueFormulaName` | 函数 | 唯一方剂名 | `"方剂_{{$timestamp}}"` |

### C. 相关文档

- **覆盖分析报告**: `docs/06-operations/api-coverage-analysis.md`
- **缺口详细数据**: `docs/06-operations/api-gap-analysis.csv`
- **缺口修复计划**: `docs/06-operations/api-gap-summary.md`
- **Postman Collection**: `docs/06-operations/LYBTZYZS_API_Collection.json`
- **Postman Environment**: `docs/06-operations/LYBTZYZS_Environment.json` (待创建)

---

**版本历史**:
- **v2.2.0** (2026-04-01): 路径规范化 + 14 个缺失端点 + 重复清理 + Setup 标记 → **100% 覆盖率**
- **v2.1.0** (2026-03-xx): 初始版本 (84.2% 覆盖率, 88 请求)
