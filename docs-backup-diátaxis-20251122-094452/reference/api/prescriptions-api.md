# Prescriptions API 参考文档

**版本**: v1
**基础路径**: `/api/v1/prescriptions`
**认证方式**: Bearer Token (JWT)
**Epic来源**: #1600 - Server端重构（实现PrescriptionsController）

---

## 📋 目录

- [概述](#概述)
- [Read Layer - 读操作](#read-layer-读操作)
  - [GET /api/v1/prescriptions/{id}](#1-get-apiv1prescriptionsid---获取处方详情)
  - [GET /api/v1/prescriptions/medicalcase/{medicalCaseId}](#2-get-apiv1prescriptionsmedicalcasemedicalcaseid---获取病案处方列表)
  - [GET /api/v1/prescriptions/search](#3-get-apiv1prescriptionssearch---搜索处方req-2)
  - [GET /api/v1/prescriptions/patient/{patientId}/recent](#4-get-apiv1prescriptionspatientpatientidrecent---获取患者最近处方req-1)
- [通用响应格式](#通用响应格式)
- [业务规则说明](#业务规则说明)

---

## 概述

### 架构设计原则

Prescriptions API遵循三层对齐架构和AR-001业务规则：

- **Read Layer（读操作）**: 独立查询接口，支持按ID、病案、患者、病症关键字查询处方
- **Write Layer（写操作）**: 所有数据修改必须通过MedicalCase聚合根完成（见[MedicalCase API](./medicalcase-api.md)）

### 核心业务规则

- **AR-001**: 写操作必须通过MedicalCase聚合根（见[MedicalCase API](./medicalcase-api.md)）
- **AR-003**: 一诊一方约束（一个病案只能有一个有效处方）
- **REQ-1**: 支持按患者查询最近处方列表
- **REQ-2**: 支持按病症关键字搜索处方

### Write vs Read 分离

**重要说明**：本文档仅包含Read-only查询接口，所有Write操作（创建、更新、删除处方）请参见[MedicalCase API](./medicalcase-api.md)的Write Layer章节：

- **POST /api/v1/medicalcases/{id}/prescriptions** - 创建处方（通过聚合根）
- **PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}** - 更新处方（通过聚合根）
- **DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}** - 删除处方（通过聚合根）

---

## Read Layer - 读操作

### 1. GET /api/v1/prescriptions/{id} - 获取处方详情

**描述**: 根据处方ID获取处方详情，包含完整的药材明细列表（处方项Items）。

**业务规则**:
- 自动预加载处方项（Items）关联数据
- 返回包含所有药材信息的完整处方

**请求**:
```http
GET /api/v1/prescriptions/8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | Guid | ✅ | 处方ID |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
    "medicalCaseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "prescriptionNumber": "RX20251027001",
    "indication": "外感风寒，肺气不宣",
    "dosageCount": 7,
    "usage": "水煎服，每日一剂，分早晚两次温服",
    "discount": 1.0,
    "advice": "服药期间忌食生冷辛辣",
    "formulaSource": "伤寒论",
    "referencedFormulas": "桂枝汤",
    "totalAmount": 38.5,
    "items": [
      {
        "id": "9b0c1d2e-3f4a-5b6c-7d8e-9f0a1b2c3d4e",
        "prescriptionId": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
        "herbId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
        "herbName": "桂枝",
        "specification": "10g",
        "quantity": 7,
        "unitPrice": 2.5,
        "totalPrice": 17.5,
        "usage": "先煎"
      },
      {
        "id": "0c1d2e3f-4a5b-6c7d-8e9f-0a1b2c3d4e5f",
        "prescriptionId": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
        "herbId": "2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e",
        "herbName": "白芍",
        "specification": "10g",
        "quantity": 7,
        "unitPrice": 3.0,
        "totalPrice": 21.0,
        "usage": ""
      }
    ],
    "remark": "首次处方",
    "isDeleted": false,
    "createdAt": "2025-10-27T10:15:00Z",
    "updatedAt": "2025-10-27T10:15:00Z"
  }
}
```

❌ **失败 - 400 Bad Request** (参数验证失败)
```json
{
  "success": false,
  "message": "处方ID格式无效，请提供有效的GUID",
  "data": null
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "处方不存在",
  "data": null
}
```

**代码示例**:

```csharp
// C# Client
var response = await httpClient.GetAsync($"/api/v1/prescriptions/{prescriptionId}");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<PrescriptionDto>>();

if (result.Success)
{
    var prescription = result.Data;
    Console.WriteLine($"处方编号: {prescription.PrescriptionNumber}");
    Console.WriteLine($"药材数量: {prescription.Items.Count}");
}
```

---

### 2. GET /api/v1/prescriptions/medicalcase/{medicalCaseId} - 获取病案处方列表

**描述**: 根据病案ID获取该病案的所有处方记录，包含完整的处方项信息。

**业务规则**:
- 返回指定病案的所有处方（包括已删除的处方）
- 按创建时间倒序排列（最新的在前）
- 自动预加载处方项（Items）关联数据

**请求**:
```http
GET /api/v1/prescriptions/medicalcase/7c9e6679-7425-40de-944b-e07fc1f90ae7
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| medicalCaseId | Guid | ✅ | 病案ID |

**响应**:

✅ **成功 - 200 OK** (有处方)
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
      "medicalCaseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "prescriptionNumber": "RX20251027001",
      "indication": "外感风寒，肺气不宣",
      "dosageCount": 7,
      "totalAmount": 38.5,
      "items": [
        {
          "herbName": "桂枝",
          "specification": "10g",
          "quantity": 7
        }
      ],
      "createdAt": "2025-10-27T10:15:00Z"
    }
  ]
}
```

✅ **成功 - 200 OK** (无处方)
```json
{
  "success": true,
  "message": "查询成功",
  "data": []
}
```

❌ **失败 - 400 Bad Request** (参数验证失败)
```json
{
  "success": false,
  "message": "病案ID格式无效，请提供有效的GUID",
  "data": null
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "病案不存在",
  "data": null
}
```

**代码示例**:

```csharp
// C# Client
var response = await httpClient.GetAsync($"/api/v1/prescriptions/medicalcase/{medicalCaseId}");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionDto>>>();

if (result.Success && result.Data.Any())
{
    Console.WriteLine($"该病案有 {result.Data.Count} 个处方");
}
```

---

### 3. GET /api/v1/prescriptions/search - 搜索处方（REQ-2）

**描述**: 按患者姓名或病症关键字搜索处方，返回搜索结果列表（REQ-2：按病症查询处方）。

**业务规则**:
- 必须至少提供一个搜索条件（患者姓名或病症关键字）
- 搜索范围：
  - **患者姓名**：精确匹配或部分匹配
  - **病症关键字**：匹配处方的主治（Indication）、药材名称（HerbName）
- 返回结果按创建时间倒序排列
- 包含患者基本信息（ID、姓名）和处方摘要信息

**请求**:
```http
GET /api/v1/prescriptions/search?patientName=张三&symptomKeyword=风寒
Authorization: Bearer {token}
```

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| patientName | String | ❌ | 患者姓名（部分匹配） |
| symptomKeyword | String | ❌ | 病症关键字（匹配主治或药材名称） |

**注意**：至少提供一个参数，否则返回400错误。

**响应**:

✅ **成功 - 200 OK** (有结果)
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "prescriptionId": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
      "prescriptionNumber": "RX20251027001",
      "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "patientName": "张三",
      "indication": "外感风寒，肺气不宣",
      "dosageCount": 7,
      "totalAmount": 38.5,
      "createdAt": "2025-10-27T10:15:00Z"
    }
  ]
}
```

✅ **成功 - 200 OK** (无结果)
```json
{
  "success": true,
  "message": "查询成功",
  "data": []
}
```

❌ **失败 - 400 Bad Request** (参数验证失败)
```json
{
  "success": false,
  "message": "请至少提供一个搜索条件（患者姓名或病症关键字）",
  "data": null
}
```

**代码示例**:

```csharp
// C# Client - 按患者姓名搜索
var response = await httpClient.GetAsync("/api/v1/prescriptions/search?patientName=张三");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionSearchResultDto>>>();

// C# Client - 按病症关键字搜索（REQ-2）
var response = await httpClient.GetAsync("/api/v1/prescriptions/search?symptomKeyword=风寒");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionSearchResultDto>>>();

// C# Client - 组合搜索
var response = await httpClient.GetAsync("/api/v1/prescriptions/search?patientName=张三&symptomKeyword=风寒");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionSearchResultDto>>>();
```

**使用场景**:
- ✅ 查询某患者的所有处方："按患者姓名搜索"
- ✅ 查询某病症的所有处方："按病症关键字搜索"（REQ-2）
- ✅ 查询某患者的某病症处方："组合搜索"

---

### 4. GET /api/v1/prescriptions/patient/{patientId}/recent - 获取患者最近处方（REQ-1）

**描述**: 获取指定患者最近的N个处方记录（REQ-1：按患者查询处方），默认返回最近5个。

**业务规则**:
- 返回指定患者的最近处方（按创建时间倒序）
- 默认返回5个处方，最多支持20个
- 包含患者基本信息和处方摘要信息

**请求**:
```http
GET /api/v1/prescriptions/patient/3fa85f64-5717-4562-b3fc-2c963f66afa6/recent?count=10
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| patientId | Guid | ✅ | 患者ID |

**查询参数**:

| 参数 | 类型 | 必填 | 默认值 | 范围 | 说明 |
|------|------|------|--------|------|------|
| count | Integer | ❌ | 5 | 1-20 | 返回处方数量 |

**响应**:

✅ **成功 - 200 OK** (有处方)
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "prescriptionId": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
      "prescriptionNumber": "RX20251027001",
      "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "patientName": "张三",
      "indication": "外感风寒，肺气不宣",
      "dosageCount": 7,
      "totalAmount": 38.5,
      "createdAt": "2025-10-27T10:15:00Z"
    },
    {
      "prescriptionId": "7b8c9d0e-1f2a-3b4c-5d6e-7f8a9b0c1d2e",
      "prescriptionNumber": "RX20251020002",
      "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "patientName": "张三",
      "indication": "脾胃虚弱，食欲不振",
      "dosageCount": 14,
      "totalAmount": 105.0,
      "createdAt": "2025-10-20T14:30:00Z"
    }
  ]
}
```

✅ **成功 - 200 OK** (无处方)
```json
{
  "success": true,
  "message": "查询成功",
  "data": []
}
```

❌ **失败 - 400 Bad Request** (参数验证失败 - 患者ID)
```json
{
  "success": false,
  "message": "患者ID格式无效，请提供有效的GUID",
  "data": null
}
```

❌ **失败 - 400 Bad Request** (参数验证失败 - count范围)
```json
{
  "success": false,
  "message": "返回数量必须在1-20之间",
  "data": null
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "患者不存在",
  "data": null
}
```

**代码示例**:

```csharp
// C# Client - 获取最近5个处方（默认）
var response = await httpClient.GetAsync($"/api/v1/prescriptions/patient/{patientId}/recent");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionSearchResultDto>>>();

// C# Client - 获取最近10个处方
var response = await httpClient.GetAsync($"/api/v1/prescriptions/patient/{patientId}/recent?count=10");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PrescriptionSearchResultDto>>>();
```

**使用场景**:
- ✅ 患者历史处方查询（REQ-1）
- ✅ 复诊参考历史用药
- ✅ 处方历史记录追踪

---

## 通用响应格式

### ApiResponse<T> 结构

```typescript
{
  success: boolean;      // 操作是否成功
  message: string;       // 消息描述
  data: T | null;        // 响应数据（失败时为null）
}
```

### PrescriptionDto 结构

```typescript
{
  id: Guid;                        // 处方ID
  medicalCaseId: Guid;             // 病案ID
  patientId: Guid;                 // 患者ID
  prescriptionNumber: string;      // 处方编号
  indication: string;              // 主治
  dosageCount: number;             // 剂数
  usage: string;                   // 用法
  discount: number;                // 折扣（0.0-1.0）
  advice: string;                  // 医嘱
  formulaSource: string;           // 验方来源
  referencedFormulas: string;      // 引用验方
  totalAmount: number;             // 总金额
  items: PrescriptionItemDto[];    // 处方项列表
  remark: string;                  // 备注
  isDeleted: boolean;              // 是否已删除
  createdAt: DateTime;             // 创建时间
  updatedAt: DateTime;             // 更新时间
}
```

### PrescriptionItemDto 结构

```typescript
{
  id: Guid;                  // 处方项ID
  prescriptionId: Guid;      // 处方ID
  herbId: Guid;              // 药材ID
  herbName: string;          // 药材名称
  specification: string;     // 规格（如"10g"）
  quantity: number;          // 数量
  unitPrice: number;         // 单价
  totalPrice: number;        // 总价
  usage: string;             // 特殊用法（如"先煎"）
}
```

### PrescriptionSearchResultDto 结构

```typescript
{
  prescriptionId: Guid;        // 处方ID
  prescriptionNumber: string;  // 处方编号
  patientId: Guid;             // 患者ID
  patientName: string;         // 患者姓名
  indication: string;          // 主治
  dosageCount: number;         // 剂数
  totalAmount: number;         // 总金额
  createdAt: DateTime;         // 创建时间
}
```

### 标准HTTP状态码

| 状态码 | 说明 | 适用场景 |
|--------|------|----------|
| 200 OK | 成功 | 所有成功的GET请求 |
| 400 Bad Request | 请求参数错误 | 参数验证失败、格式错误、缺少必填参数 |
| 401 Unauthorized | 未授权 | Token无效或过期 |
| 404 Not Found | 资源不存在 | 处方、病案、患者不存在 |
| 500 Internal Server Error | 服务器错误 | 未预期的系统异常 |

---

## 业务规则说明

### AR-001: 聚合根约束

**定义**: 所有对Prescription的**写操作**必须通过MedicalCase聚合根完成。

**Read vs Write分离**:
- ✅ **Read操作**：本文档的4个查询接口可以直接调用PrescriptionsController
- ⚠️ **Write操作**：创建、更新、删除处方必须通过[MedicalCase API](./medicalcase-api.md)的聚合根端点

**Write操作端点（聚合根）**:
- POST /api/v1/medicalcases/{id}/prescriptions - 创建处方
- PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 更新处方
- DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 删除处方

**违规示例**:
```csharp
// ❌ 错误：直接操作Prescription实体
await prescriptionRepository.AddAsync(prescription);

// ✅ 正确：通过MedicalCase聚合根
await medicalCaseService.CreatePrescriptionAsync(medicalCaseId, request);
```

### AR-003: 一诊一方约束

**定义**: 一个病案只能有一个有效处方。

**影响查询接口**:
- GET /api/v1/prescriptions/medicalcase/{medicalCaseId} - 通常只返回1个处方（除非有历史删除的处方）

**相关端点**:
- 创建处方时，如果病案已有处方，会返回422错误

### REQ-1: 按患者查询处方

**定义**: 支持查询指定患者的历史处方记录。

**实现端点**:
- GET /api/v1/prescriptions/patient/{patientId}/recent - 获取患者最近N个处方

**使用场景**:
- 患者复诊时查看历史用药
- 处方历史记录追踪
- 生成患者用药报告

### REQ-2: 按病症查询处方

**定义**: 支持按病症关键字搜索处方（用于经验方查询、病症统计等）。

**实现端点**:
- GET /api/v1/prescriptions/search - 按病症关键字搜索（搜索主治或药材名称）

**使用场景**:
- 查询某病症的常用处方
- 经验方统计分析
- 药材用量分析

---

## 参考资料

### 源码文件
- **Controller**: `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs`
- **Service**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- **Repository**: `src/Server/Modules/LYBT.Module.Prescriptions/Repositories/PrescriptionRepository.cs`
- **DTOs**: `src/Shared/LYBT.Shared.Models/Dtos/Prescriptions/`

### 测试文件
- **单元测试**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/`
- **集成测试**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/PrescriptionsControllerIntegrationTests.cs`

### 相关文档
- **MedicalCase API**: `docs/reference/api/medicalcase-api.md` (Write操作端点)
- **Server端重构设计**: `docs/explanation/design/server-refactor-design.md`
- **架构指南**: `docs/explanation/architecture/server/README.md`
- **业务规则**: `docs/explanation/business-rules.md` (AR-001, AR-003)

---

**文档版本**: v1.0
**最后更新**: 2025-10-27
**维护者**: Claude Code + lybtzyzs-doc-sync
**关联Issue**: #1674 (Epic #1600 - Task 2.1: 实现PrescriptionsController)
**Epic**: #1600 (Server端重构 - Phase 2完成)

---

**文档状态**: ✅ 已验证（基于编译通过和代码审查）
**API稳定性**: ✅ 稳定（0 errors, 0 warnings）
**覆盖需求**: ✅ REQ-1（按患者查询）+ REQ-2（按病症查询）
