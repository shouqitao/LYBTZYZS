# LYBTZYZS WebAPI Newman 测试报告

## 测试执行摘要

| 指标 | 数值 |
|------|------|
| 总请求数 | 102 |
| 总测试数 | 311 |
| 通过测试 | 187 |
| 失败测试 | 124 |
| 通过率 | 60.1% |
| 执行时间 | ~10s |
| 平均响应时间 | ~15ms |

---

## 测试结果分类

### 1. 认证模块 (Auth) - 基本通过

| 端点 | 状态 | 说明 |
|------|------|------|
| Login | 200 OK | 登录成功，Token 已生成 |
| Logout | 200 OK | 登出成功 |
| Auto-Login | 401 | 使用了无效的 autoLoginToken |
| Refresh Token | 400 | 无有效的 refreshToken |
| Validate Token | 401 | 未提供 Bearer Token |

**分析**: 认证模块基本工作正常。登录成功后，后续请求可以正确获取和使用 Token。

### 2. 健康检查 (Health) - 部分通过

| 端点 | 状态 | 说明 |
|------|------|------|
| Health Check | 200 OK | 基础健康检查通过 |
| Ping | 200 OK | Ping 测试通过 |
| Health Details | 401 | 需要认证 |

**问题**: Health Check 和 Ping 返回的是简单对象，不是 `ApiResponse<T>` 格式，导致断言失败。

### 3. 主要失败类别

#### A. 请求体验证失败 (400 Bad Request)

以下端点返回 400，提示 "模型验证失败"：

- **Sync / Compare** - 请求体格式不正确
- **Sync / Download** - 请求体格式不正确  
- **Sync / Delete** - 请求体格式不正确
- **Registrations / Create Registration** - 缺少必填字段
- **Diagnostics / Set Log Level** - 缺少日志级别参数

**根因**: Postman Collection 中的请求体示例使用了占位符数据，不符合实际 API 的验证要求。

#### B. 资源不存在 (404 Not Found)

以下端点返回 404：

- **Registrations / Start Visit** - `{{testRegistrationId}}` 为空
- **Registrations / Cancel Registration** - `{{testRegistrationId}}` 为空

**根因**: 依赖的测试资源 ID 变量未被正确设置（需要先创建资源才能获取 ID）。

#### C. 响应格式不匹配

以下端点返回的数据格式与 `ApiResponse<T>` 断言不匹配：

- **Diagnostics / Get Logging Status** - 返回简单对象而非 ApiResponse
- **Diagnostics / Enable Debug Mode** - 返回简单对象而非 ApiResponse
- **Diagnostics / Disable Debug Mode** - 返回简单对象而非 ApiResponse
- **Health / Health Check** - 返回简单对象而非 ApiResponse
- **Health / Ping** - 返回简单对象而非 ApiResponse
- **Health / Health Details** - 返回简单对象而非 ApiResponse

**根因**: 这些端点设计时返回的是直接对象，而非统一的 `ApiResponse<T>` 包装格式。

#### D. 数据依赖问题

大量测试失败是因为：

1. **变量未设置**: `{{testUserId}}`, `{{testPatientId}}`, `{{testMedicalCaseId}}` 等为空
2. **资源不存在**: 尝试获取/更新/删除不存在的资源
3. **前置条件不满足**: 例如尝试完成挂号但挂号不存在

**根因**: Postman Collection 没有正确设置前置条件，导致依赖链断裂。

---

## 关键发现

### 1. 响应格式不一致

**Health 和 Diagnostics 端点**返回的是直接对象：
```json
// Health Check 实际返回
{ "status": "Healthy", "timestamp": "..." }

// 但测试期望
{ "success": true, "message": "...", "data": { "status": "Healthy" } }
```

### 2. Postman Collection 数据问题

**登录端点请求体**在修复前使用了错误的密码：
```json
// 修复前 (错误)
{ "userName": "sysadmin", "password": "string" }

// 修复后 (正确)
{ "userName": "sysadmin", "password": "DevPass123" }
```

### 3. Token 存储逻辑有缺陷

Postman Collection 的测试脚本尝试存储 Token：
```javascript
pm.collectionVariables.set('authToken', j.data.accessToken || j.data.token);
```

但实际上返回的字段是 `token` 而非 `accessToken`。

---

## 修复计划

### 高优先级修复 (P0)

#### 1. 统一响应格式或更新测试断言

**文件**: 相关 Controller 或 Postman Collection

**选项 A - 修改 API 返回统一格式** (推荐):
```csharp
// HealthController.cs
[HttpGet]
[AllowAnonymous]
public IActionResult Get()
{
    var result = new HealthCheckResponse { Status = "Healthy", Timestamp = DateTime.UtcNow };
    return Ok(ApiResponse<HealthCheckResponse>.CreateSuccess(result, "Health check passed"));
}
```

**选项 B - 修改 Postman 断言**:
```javascript
// 对于 Health 端点，移除 ApiResponse 结构断言
pm.test('Status code is 200', function () {
    pm.response.to.have.status(200);
});
// 移除: pm.expect(jsonData).to.have.property('success');
```

#### 2. 修复 Postman Collection 请求体

**文件**: `docs/06-operations/LYBTZYZS_API_Collection.json`

修复以下端点的请求体：

| 端点 | 当前问题 | 修复方案 |
|------|----------|----------|
| Sync/Compare | 空 entityType | 提供有效值如 "Herb" |
| Sync/Download | 空 entityIds | 提供有效 GUID 数组 |
| Registrations/Create | 缺少必填字段 | 添加 PatientId, DoctorId 等 |
| Diagnostics/SetLogLevel | 空 level | 提供有效值如 "Debug" |

### 中优先级修复 (P1)

#### 3. 实现测试数据链

**方案**: 在 Postman Collection 中添加 Pre-request Script，按顺序创建依赖资源：

```javascript
// 在 Create Patient 成功后
pm.collectionVariables.set("testPatientId", pm.response.json().data.id);

// 在 Create Registration 前检查依赖
if (!pm.collectionVariables.get("testPatientId")) {
    pm.test.skip("Skipping - no test patient available");
}
```

#### 4. 添加环境初始化端点

创建一个专门的测试初始化请求，按顺序：
1. 创建测试患者
2. 创建测试药材
3. 创建测试验方
4. 创建测试挂号

### 低优先级修复 (P2)

#### 5. 完善测试覆盖率

添加以下场景的测试：
- 边界条件测试 (空列表、超大分页)
- 错误场景测试 (无效 GUID、越界值)
- 并发测试 (同时修改同一资源)

---

## 修复工作量评估

| 任务 | 工作量 | 优先级 |
|------|--------|--------|
| 修复 Health/Diagnostics 响应格式 | 2-4 小时 | P0 |
| 更新 Postman Collection 请求体 | 4-6 小时 | P0 |
| 实现测试数据链 | 4-8 小时 | P1 |
| 添加环境初始化 | 2-4 小时 | P1 |
| 完善测试覆盖 | 8-16 小时 | P2 |
| **总计** | **20-38 小时** | - |

---

## 附录: 生成的报告文件

| 文件 | 路径 |
|------|------|
| JSON Report | `docs/06-operations/newman-report.json` |
| HTML Report | `docs/06-operations/newman-report.html` |
| 本报告 | `docs/06-operations/NEWMAN_TEST_REPORT.md` |

---

## 测试执行命令

```bash
# 运行测试并生成报告
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --reporters cli,json,htmlextra \
  --reporter-json-export docs/06-operations/newman-report.json \
  --reporter-htmlextra-export docs/06-operations/newman-report.html \
  --insecure \
  --timeout-request 10000
```

---

*报告生成时间: 2026-03-28*  
*测试环境: Development*  
*API Base URL: https://localhost:5001*
