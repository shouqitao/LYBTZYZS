# Postman API 测试分析报告

> 生成时间: 2026-03-30 12:30  
> 分析者: 小墨 🖊️

## 测试结果总览

| 指标 | 值 |
|------|------|
| 请求数 | 91 (全部执行成功) |
| 断言总数 | 279 |
| 通过 | 219 (78.5%) |
| 失败 | 60 (21.5%) |
| 运行时间 | 29.5s |
| 平均响应 | 13ms |

## 失败根因分析

### 🔴 问题 A: ApiResponse 错误响应缺少 `data` 字段 (约 30 个断言)

**严重程度**: 高 (影响所有错误场景的测试)

**根本原因**: `ApiResponse<T>.CreateFail()` 不设置 `Data` 属性（为 null），C# 默认的 System.Text.Json 序列化会**忽略 null 值属性**。但 Postman 断言强制要求 `data` 存在：

```javascript
pm.expect(jsonData).to.have.property('data');
```

**实际返回**:
```json
{"success":false,"message":"未找到...","timestamp":1774844737,"requestId":"..."}
// ← 没有 data 字段
```

**影响范围**: 所有返回 4xx/5xx 的请求的第二个断言 ("ApiResponse structure is valid")

**修复建议**: 在 `ApiResponse<T>` 上添加全局 JSON 序列化选项，确保 null 属性也序列化：

```csharp
// 方案 1: 在 ApiResponse 的 Data 属性上标注
[JsonPropertyName("data")]
[JsonIgnore(Condition = JsonIgnoreCondition.Never)]  // 始终序列化
public T? Data { get; set; }
```

或在 `Program.cs` 的全局 JSON 选项中：
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never);
```

---

### 🔴 问题 B: Postman 测试数据链断裂 (约 20 个断言)

**严重程度**: 中 (测试流程设计问题)

**根本原因**: Setup 阶段的 "0b. Login as Doctor" 覆盖了 `authToken`，后续所有请求切换为 Doctor 身份。但 MedicalCase 创建依赖正确的 Patient + Doctor ID 传递。

**失败链条**:
1. Setup "2. Get Doctor Info" → `GET /api/v1/users?page=1&pageSize=1&Role=1` → **URL 参数名不匹配**
   - Postman 用 `page` 参数，但 API 用 `pageIndex` 和 `pageSize`
   - 所以 Users List 返回 200 但是默认分页，可能 Doctor 找不到
   
2. MedicalCase 创建可能因 testPatientId 无效而失败 → 后续所有 MedicalCase 操作返回 404

**具体 404 列表**:
- Get Medical Case by ID → 404
- Update Status → 404
- Close Case → 404
- Suspend Case → 404
- Cancel Case → 404
- Get Permissions → 404
- Get Audit Logs → 404
- Mark Print Completed → 404
- Create Print Log → 404
- Herbs Check Reference / Toggle Status → 404 (testHerbId 无效)
- Formulas Toggle Status → 404 (testFormulaId 无效)

**修复建议**: 修改 Postman "Get Doctor Info" 的 URL，使用正确的参数名 `pageIndex=1&pageSize=1&role=1`

---

### 🔴 问题 C: Diagnostics 端点 403 (8 个断言)

**严重程度**: 中 (Postman 测试设计问题)

**根本原因**: `[Authorize(Roles = "SuperAdmin")]` 但 Postman 用 Doctor token 访问。

**日志确认**:
```
标准化化了 7 个Claims: UserId=db630273-..., UserName=testuser13800061773, Role=Doctor
GET /api/v1/diagnostics/logging/status → 403
```

**修复建议**: Postman 的 Diagnostics 文件夹应添加独立 auth，使用 sysadmin 的 token，或在测试集合中增加一个 "Login as Admin for Diagnostics" 步骤。

---

### 🔴 问题 D: Sync Upload DTO 验证 (2 个断言)

**严重程度**: 低

**请求**: `POST /api/v1/sync/upload`
**Postman Body**: `{"EntityType":"Herb","Entities":[...],"OverwriteConflicts":false}`
**API 返回**: 400 "实体类型不能为空"

**根因**: API 的 SyncUploadInputDto 可能使用不同的属性名，或者 ModelState 验证对 EntityType 有特殊要求。

---

### 🔴 问题 E: Formula Validate Herb Item 422 (2 个断言)

**严重程度**: 低

**请求**: `POST /api/v1/Formulas/{id}/Herbs/{herbId}/validate`
**返回**: 422

**根因**: testFormulaId 或 testHerbId 无效（数据链断裂导致）

---

### 🔴 问题 F: Herbs/Formulas 多个端点 404/400/422 (约 10 个断言)

**严重程度**: 低 (同问题 B 数据链断裂)

Herbs 的 Create/Update 返回 400/422 说明 DTO 验证较严格，Postman 发送的数据可能不满足验证要求。

---

## Postman 测试 vs API 设计对比

### ✅ 测试用例与设计匹配 (符合设计)
| 模块 | 端点数 | 覆盖情况 |
|------|--------|----------|
| Auth (login/refresh/validate/auto-login) | 4 | ✅ 完全覆盖 |
| Users (CRUD + toggle + restore) | 8 | ✅ 完全覆盖 |
| Patients (CRUD + batch + import/export) | ~11 | ⚠️ 部分在 Users 文件夹下 |
| Medical Cases (CRUD + workflow + audit + print) | 12 | ✅ 完全覆盖 |
| Herbs (CRUD + batch + import + reference) | 17 | ✅ 完全覆盖 |
| Formulas (CRUD + batch + validate) | 15 | ✅ 完全覆盖 |
| Sync (full workflow) | 6 | ✅ 完全覆盖 |
| Registrations | 7 | ✅ 完全覆盖 |
| Diagnostics | 4 | ✅ 完全覆盖 |
| Health | 3 | ✅ 完全覆盖 |

### ❌ Postman 测试中存在的问题
1. **"Users" 文件夹混入了 Patients 端点** — 6 个 patients 请求放在 "2. Users" 下
2. **"Get Doctor Info" 用错误的查询参数** — `page=1` 应为 `pageIndex=1`
3. **缺少 Diagnostics 的独立 auth** — 需要 SuperAdmin token
4. **错误响应断言过于严格** — 要求所有响应包含 `data` 字段
5. **Sync Upload 请求体可能不匹配 DTO**

### ❌ API 中可能需要清理的内容
1. **UsersController 缺少用户列表端点** → 实际有 `GetList`（仅 AdminOnly）
2. **PatientsController 没有 `batch-enable/batch-disable`** → Herbs 和 Formulas 有，但 Patients 没有（可能设计如此）
3. **HerbsController 同时有 `import` 和 `batch-import`** → 功能可能重叠

---

## 建议的修复优先级

### P0 (阻塞测试通过)
1. **修改 ApiResponse 序列化** — 确保 null 属性也序列化 `data` 字段
2. **修改 Postman "Get Doctor Info"** — `page` → `pageIndex`
3. **Postman Diagnostics 添加独立 auth** — 使用 admin token

### P1 (提升测试质量)
4. **修复 Postman Setup 数据链** — 确保所有变量正确传递
5. **Sync Upload DTO 对齐** — 检查字段名匹配

### P2 (代码清理，需牧川确认)
6. **HerbsController: `import` vs `batch-import`** — 是否需要两个？
7. **Postman 文件夹重组** — 把 Patients 请求从 Users 移出
8. **错误响应统一** — 所有错误都返回完整的 ApiResponse 结构
