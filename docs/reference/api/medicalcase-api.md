# MedicalCase API 参考文档

**版本**: v1
**基础路径**: `/api/v1/medicalcases`
**认证方式**: Bearer Token (JWT)
**Epic来源**: #1612 - MedicalCase模块重构（三层对齐架构）

---

## 📋 目录

- [概述](#概述)
- [Write Layer - 写操作](#write-layer-写操作)
  - [POST /api/v1/medicalcases](#1-post-apiv1medicalcases---创建新病案)
  - [PUT /api/v1/medicalcases/{id}/consultation](#2-put-apiv1medicalcasesidconsultation---更新辨证信息)
  - [PUT /api/v1/medicalcases/{id}/prescription-flag](#3-put-apiv1medicalcasesidprescription-flag---标记是否开处方)
  - [POST /api/v1/medicalcases/{id}/prescriptions](#4-post-apiv1medicalcasesidprescriptions---创建处方)
  - [PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}](#5-put-apiv1medicalcasesidprescriptionsprescriptionid---更新处方)
  - [DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}](#6-delete-apiv1medicalcasesidprescriptionsprescriptionid---删除处方)
  - [PUT /api/v1/medicalcases/{id}/status](#7-put-apiv1medicalcasesidstatus---更新病案状态)
  - [PUT /api/v1/medicalcases/{id}/complete](#8-put-apiv1medicalcasesidcomplete---完成病案)
  - [PUT /api/v1/medicalcases/{id}/close](#9-put-apiv1medicalcasesidclose---关闭病案epic-1676-phase-4新增) ⭐ **Phase 4新增**
- [Read Layer - 读操作](#read-layer-读操作)
  - [GET /api/v1/medicalcases/{id}](#10-get-apiv1medicalcasesid---获取病案详情)
  - [GET /api/v1/medicalcases](#11-get-apiv1medicalcases---查询病案列表)
  - [GET /api/v1/medicalcases/{medicalCaseId}/consultations](#12-get-apiv1medicalcasesmedicalcaseidconsultations---查询辨证记录列表)
  - [GET /api/v1/medicalcases/{medicalCaseId}/prescriptions](#13-get-apiv1medicalcasesmedicalcaseidprescriptions---查询处方列表)
  - [GET /api/v1/medicalcases/patients/{patientId}/unfinished](#14-get-apiv1medicalcasespatientspatientidunfinished---查询患者未完成病案epic-1676-phase-4新增) ⭐ **Phase 4新增**
- [Helper Layer - 辅助功能](#helper-layer-辅助功能)
  - [GET /api/v1/medicalcases/{id}/can-edit](#15-get-apiv1medicalcasesidcan-edit---验证病案是否可编辑)
  - [GET /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}/can-delete](#16-get-apiv1medicalcasesidprescriptionsprescriptionidcan-delete---验证处方是否可删除)
- [通用响应格式](#通用响应格式)
- [业务规则说明](#业务规则说明)

---

## 概述

### 架构设计原则

MedicalCase API遵循三层对齐架构和Write/Read/Helper Layer分离原则：

- **Write Layer（写操作）**: 所有数据修改通过MedicalCase聚合根完成，确保业务规则一致性
- **Read Layer（读操作）**: 独立查询接口，支持分页、过滤、预加载
- **Helper Layer（辅助功能）**: 验证类接口，用于UI状态控制

### 核心业务规则

- **AR-001**: 所有写操作必须通过MedicalCase聚合根
- **AR-003**: 一诊一方约束（一个病案只能有一个有效处方）
- **BF-002**: 三步流程验证（辨证 → 标记处方 → 开处方/完成）
- **BR-001**: 单个患者只能有一个Active状态病案

### 三步就诊流程

```
Step 1: 辨证（UpdateConsultation）
  ↓
Step 2: 标记是否开处方（SetPrescriptionFlag）
  ↓ 是
Step 3a: 开处方（CreatePrescription）
  ↓
完成病案（Complete）

或
  ↓ 否
Step 3b: 直接完成（Complete）
```

---

## Write Layer - 写操作

### 1. POST /api/v1/medicalcases - 创建新病案

**描述**: 为患者创建新的病案记录，病案初始状态为Active。

**业务规则**:
- **AR-001**: 通过聚合根创建
- **BR-001**: 单个患者只能有一个Active病案

**请求**:
```http
POST /api/v1/medicalcases
Content-Type: application/json
Authorization: Bearer {token}

{
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "visitDate": "2025-10-27T10:00:00Z"
}
```

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| patientId | Guid | ✅ | 患者ID，必须在系统中存在 |
| visitDate | DateTime | ✅ | 就诊日期时间 |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "病案创建成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientName": "张三",
    "doctorId": "8b7d9f2a-3b14-4c8e-a9d6-1f2e3d4c5b6a",
    "doctorName": "李医生",
    "visitDate": "2025-10-27T10:00:00Z",
    "status": "Active",
    "consultation": null,
    "prescription": null,
    "createdAt": "2025-10-27T10:00:00Z",
    "updatedAt": "2025-10-27T10:00:00Z"
  }
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

❌ **失败 - 422 Unprocessable Entity** (BR-001违规)
```json
{
  "success": false,
  "message": "该患者已有Active状态的病案，请先完成或取消现有病案",
  "data": null
}
```

**代码示例**:

```csharp
// C# Client
var request = new CreateMedicalCaseRequest
{
    PatientId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    VisitDate = DateTime.Now
};

var response = await httpClient.PostAsJsonAsync("/api/v1/medicalcases", request);
var result = await response.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseEntity>>();
```

---

### 2. PUT /api/v1/medicalcases/{id}/consultation - 更新辨证信息

**描述**: 更新病案的辨证信息（三步流程Step 1），包含完整的四诊字段。

**业务规则**:
- **AR-001**: 通过聚合根更新Consultation
- 病案必须处于Active状态

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/consultation
Content-Type: application/json
Authorization: Bearer {token}

{
  "chiefComplaint": "头痛三天，伴恶寒发热",
  "presentIllness": "患者三天前受凉后出现头痛，以前额为主，伴有恶寒发热，体温38.5°C",
  "inspection": "面色苍白，舌苔薄白",
  "auscultationOlfaction": "声音低沉，无异常气味",
  "inquiry": "怕冷明显，无汗，口不渴，食欲减退",
  "palpation": "脉浮紧，左关尺沉",
  "tcmDiagnosis": "风寒感冒",
  "treatmentPrinciple": "辛温解表，宣肺散寒",
  "medicalAdvice": "避风寒，注意保暖，多喝温水",
  "remark": "患者平素体弱，易感冒"
}
```

**请求参数**:

| 字段 | 类型 | 必填 | 长度 | 说明 |
|------|------|------|------|------|
| chiefComplaint | String | ✅ | ≤500 | 主诉（必填） |
| presentIllness | String | ❌ | ≤1000 | 现病史 |
| inspection | String | ❌ | ≤500 | 望诊（四诊之一） |
| auscultationOlfaction | String | ❌ | ≤500 | 闻诊（四诊之二） |
| inquiry | String | ❌ | ≤500 | 问诊（四诊之三） |
| palpation | String | ❌ | ≤500 | 切诊（四诊之四） |
| tcmDiagnosis | String | ❌ | ≤500 | 中医诊断 |
| treatmentPrinciple | String | ❌ | ≤500 | 治疗原则 |
| medicalAdvice | String | ❌ | ≤1000 | 医嘱 |
| remark | String | ❌ | ≤500 | 备注 |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "辨证信息更新成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "patientName": "张三",
    "status": "Active",
    "consultation": {
      "id": "2a3b4c5d-6e7f-8a9b-0c1d-2e3f4a5b6c7d",
      "chiefComplaint": "头痛三天，伴恶寒发热",
      "tcmDiagnosis": "风寒感冒",
      "step1CompletedAt": "2025-10-27T10:05:00Z"
    }
  }
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

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "病案状态不允许更新辨证信息（仅Active状态可更新）",
  "data": null
}
```

---

### 3. PUT /api/v1/medicalcases/{id}/prescription-flag - 标记是否开处方

**描述**: 标记病案是否需要开处方（三步流程Step 2），用于RadioBox选择"是"或"否"。

**业务规则**:
- **BF-002**: 动态流程控制
- **AR-003**: 已有处方时不能再标记为需要开处方
- 必须先完成辨证（Step 1）

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescription-flag
Content-Type: application/json
Authorization: Bearer {token}

{
  "needsPrescription": true
}
```

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| needsPrescription | Boolean | ✅ | true=需要开处方（Step 3a），false=不需要处方（Step 3b） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "处方标记更新成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "needsPrescription": true,
    "step2CompletedAt": "2025-10-27T10:10:00Z"
  }
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

❌ **失败 - 422 Unprocessable Entity** (BF-002违规)
```json
{
  "success": false,
  "message": "必须先完成辨证（Step 1）",
  "data": null
}
```

❌ **失败 - 422 Unprocessable Entity** (AR-003违规)
```json
{
  "success": false,
  "message": "已有处方，不能再标记为需要开处方",
  "data": null
}
```

---

### 4. POST /api/v1/medicalcases/{id}/prescriptions - 创建处方

**描述**: 为病案创建处方（三步流程Step 3a）。

**业务规则**:
- **AR-001**: 通过聚合根创建
- **AR-003**: 一诊一方约束（已有处方时禁止创建）
- **BF-002**: 必须先完成Step 1和Step 2（needsPrescription=true）

**请求**:
```http
POST /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescriptions
Content-Type: application/json
Authorization: Bearer {token}

{
  "prescriptionNumber": "RX20251027001",
  "indication": "外感风寒，肺气不宣",
  "dosageCount": 7,
  "usage": "水煎服，每日一剂，分早晚两次温服",
  "discount": 1.0,
  "advice": "服药期间忌食生冷辛辣",
  "formulaSource": "伤寒论",
  "referencedFormulas": "桂枝汤",
  "items": [
    {
      "herbId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "herbName": "桂枝",
      "specification": "10g",
      "quantity": 7,
      "unitPrice": 2.5,
      "totalPrice": 17.5,
      "usage": "先煎"
    },
    {
      "herbId": "2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e",
      "herbName": "白芍",
      "specification": "10g",
      "quantity": 7,
      "unitPrice": 3.0,
      "totalPrice": 21.0,
      "usage": ""
    }
  ],
  "remark": "首次处方"
}
```

**请求参数**:

| 字段 | 类型 | 必填 | 长度/范围 | 说明 |
|------|------|------|----------|------|
| prescriptionNumber | String | ❌ | ≤50 | 处方编号（可选，系统可自动生成） |
| indication | String | ❌ | ≤500 | 主治 |
| dosageCount | Integer | ✅ | 1-100 | 剂数（默认7） |
| usage | String | ❌ | ≤500 | 用法 |
| discount | Decimal | ✅ | 0.0-1.0 | 折扣（默认1.0） |
| advice | String | ❌ | ≤500 | 医嘱 |
| formulaSource | String | ❌ | ≤200 | 验方来源 |
| referencedFormulas | String | ❌ | ≤500 | 引用验方 |
| items | Array | ✅ | ≥1 | 处方药品列表（至少一味药） |
| remark | String | ❌ | ≤500 | 备注 |

**PrescriptionItemDto结构**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| herbId | Guid | ✅ | 药材ID |
| herbName | String | ✅ | 药材名称 |
| specification | String | ✅ | 规格（如"10g"） |
| quantity | Integer | ✅ | 数量 |
| unitPrice | Decimal | ✅ | 单价 |
| totalPrice | Decimal | ✅ | 总价 |
| usage | String | ❌ | 特殊用法（如"先煎"） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "处方创建成功",
  "data": {
    "id": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
    "medicalCaseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "prescriptionNumber": "RX20251027001",
    "indication": "外感风寒，肺气不宣",
    "dosageCount": 7,
    "totalAmount": 38.5,
    "createdAt": "2025-10-27T10:15:00Z"
  }
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

❌ **失败 - 422 Unprocessable Entity** (AR-003违规)
```json
{
  "success": false,
  "message": "该病案已有处方，不能再创建（一诊一方约束）",
  "data": null
}
```

❌ **失败 - 422 Unprocessable Entity** (BF-002违规)
```json
{
  "success": false,
  "message": "必须先完成辨证和标记处方需求",
  "data": null
}
```

---

### 5. PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 更新处方

**描述**: 更新已有处方的信息。

**业务规则**:
- **AR-001**: 通过聚合根更新
- 处方必须属于该病案
- 病案未完成时可更新

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescriptions/8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d
Content-Type: application/json
Authorization: Bearer {token}

{
  "dosageCount": 14,
  "usage": "水煎服，每日一剂，分早中晚三次温服",
  "advice": "服药期间忌食生冷辛辣，注意保暖",
  "items": [
    {
      "herbId": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "herbName": "桂枝",
      "specification": "10g",
      "quantity": 14,
      "unitPrice": 2.5,
      "totalPrice": 35.0,
      "usage": "先煎"
    }
  ]
}
```

**请求参数**: 同创建处方，所有字段均为可选（更新提供的字段）。

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "处方更新成功",
  "data": {
    "id": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
    "dosageCount": 14,
    "totalAmount": 70.0,
    "updatedAt": "2025-10-27T10:20:00Z"
  }
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "病案或处方不存在",
  "data": null
}
```

❌ **失败 - 403 Forbidden**
```json
{
  "success": false,
  "message": "处方不属于该病案",
  "data": null
}
```

---

### 6. DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 删除处方

**描述**: 删除病案的处方（软删除）。

**业务规则**:
- **AR-001**: 通过聚合根删除
- 处方必须属于该病案
- 病案未完成时可删除
- 处方未打印时可删除

**请求**:
```http
DELETE /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescriptions/8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 204 No Content**
```
（无响应体）
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "病案或处方不存在"
}
```

❌ **失败 - 403 Forbidden**
```json
{
  "success": false,
  "message": "处方不属于该病案"
}
```

❌ **失败 - 422 Unprocessable Entity**
```json
{
  "success": false,
  "message": "病案已完成，无法删除处方"
}
```

---

### 7. PUT /api/v1/medicalcases/{id}/status - 更新病案状态

**描述**: 更新病案状态，支持Draft/Active/Completed/Cancelled状态流转。

**业务规则**:
- 状态转换必须合法（如不能从Completed转回Active）
- 状态枚举值: `Draft`, `Active`, `Completed`, `Cancelled`

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/status
Content-Type: application/json
Authorization: Bearer {token}

{
  "status": "Cancelled"
}
```

**请求参数**:

| 字段 | 类型 | 必填 | 可选值 | 说明 |
|------|------|------|--------|------|
| status | Enum | ✅ | Draft, Active, Completed, Cancelled | 目标状态 |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "状态更新成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "status": "Cancelled",
    "updatedAt": "2025-10-27T10:25:00Z"
  }
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

❌ **失败 - 422 Unprocessable Entity**
```json
{
  "success": false,
  "message": "状态转换不合法：不能从Completed转换为Active",
  "data": null
}
```

---

### 8. PUT /api/v1/medicalcases/{id}/complete - 完成病案

**描述**: 完成病案（三步流程最后一步），将状态设置为Completed。

**业务规则**:
- **BF-002**: 三步流程验证
  - 必须完成辨证（Step 1）
  - 必须标记处方需求（Step 2）
  - 如果needsPrescription=true，必须已开处方（Step 3a）
  - 如果needsPrescription=false，可直接完成（Step 3b）

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/complete
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "病案已完成",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "status": "Completed",
    "completedAt": "2025-10-27T10:30:00Z"
  }
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

❌ **失败 - 422 Unprocessable Entity** (BF-002违规)
```json
{
  "success": false,
  "message": "辨证未完成（Step 1）",
  "data": null
}
```

```json
{
  "success": false,
  "message": "处方标记未完成（Step 2）",
  "data": null
}
```

```json
{
  "success": false,
  "message": "已标记需要开处方，但未创建处方（Step 3a）",
  "data": null
}
```

---

### 9. PUT /api/v1/medicalcases/{id}/close - 关闭病案（Epic #1676 Phase 4新增）

**描述**: 直接关闭病案，将状态设置为Completed。与端点8（完成病案）的区别：不验证三步流程，直接标记为已完成。

**业务规则**:
- ✅ **不验证三步流程**：无需完成辨证、标记处方、开处方，可直接关闭
- ✅ **用于特殊场景**：患者取消就诊、暂存病案需要关闭、Desktop端快速关闭操作
- ❌ **不推荐常规使用**：正常就诊流程应使用端点8（PUT /complete）

**请求**:
```http
PUT /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/close
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "病案已关闭",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "status": "Completed",
    "closedAt": "2025-10-28T14:00:00Z"
  }
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

❌ **失败 - 409 Conflict** (病案已完成)
```json
{
  "success": false,
  "message": "病案已处于Completed状态，无需重复关闭",
  "data": null
}
```

**Desktop端使用示例**:
```csharp
// PatientSelectionViewModel.cs - 快速关闭未完成病案
var success = await _medicalCaseRepository.CloseCaseAsync(unfinishedCase.Id);
if (success)
{
    MessageBox.Show("病案已关闭，可以创建新病案");
}
```

**与端点8的对比**:

| 特性 | 端点8: Complete | 端点9: Close (新增) |
|------|----------------|-------------------|
| **三步流程验证** | ✅ 强制验证 | ❌ 不验证 |
| **使用场景** | 正常就诊完成 | 快速关闭、取消就诊 |
| **状态变更** | Draft/Active → Completed | Draft/Active → Completed |
| **推荐程度** | ⭐⭐⭐ 推荐 | ⭐ 特殊场景 |

---

## Read Layer - 读操作

### 10. GET /api/v1/medicalcases/{id} - 获取病案详情

**描述**: 获取病案完整信息，自动预加载Consultation和Prescription关联数据。

**请求**:
```http
GET /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientName": "张三",
    "doctorId": "8b7d9f2a-3b14-4c8e-a9d6-1f2e3d4c5b6a",
    "doctorName": "李医生",
    "visitDate": "2025-10-27T10:00:00Z",
    "status": "Active",
    "needsPrescription": true,
    "consultation": {
      "id": "2a3b4c5d-6e7f-8a9b-0c1d-2e3f4a5b6c7d",
      "chiefComplaint": "头痛三天，伴恶寒发热",
      "tcmDiagnosis": "风寒感冒",
      "treatmentPrinciple": "辛温解表，宣肺散寒"
    },
    "prescription": {
      "id": "8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d",
      "prescriptionNumber": "RX20251027001",
      "indication": "外感风寒，肺气不宣",
      "dosageCount": 7,
      "totalAmount": 38.5
    },
    "createdAt": "2025-10-27T10:00:00Z",
    "updatedAt": "2025-10-27T10:15:00Z"
  }
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

---

### 11. GET /api/v1/medicalcases - 查询病案列表

**描述**: 分页查询病案列表，支持按状态、患者ID过滤。

**请求**:
```http
GET /api/v1/medicalcases?status=Active&patientId=3fa85f64-5717-4562-b3fc-2c963f66afa6&page=1&pageSize=20
Authorization: Bearer {token}
```

**查询参数**:

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| status | Enum | ❌ | - | 病案状态过滤（Draft/Active/Completed/Cancelled） |
| patientId | Guid | ❌ | - | 患者ID过滤 |
| page | Integer | ✅ | 1 | 页码（≥1） |
| pageSize | Integer | ✅ | 20 | 每页大小（1-100） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "patientName": "张三",
        "doctorName": "李医生",
        "visitDate": "2025-10-27T10:00:00Z",
        "status": "Active"
      }
    ],
    "totalCount": 1,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "页码和页大小参数无效（页码>0，页大小1-100）",
  "data": null
}
```

---

### 12. GET /api/v1/medicalcases/{medicalCaseId}/consultations - 查询辨证记录列表

**描述**: 获取指定病案的所有历史辨证记录。

**请求**:
```http
GET /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/consultations
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "2a3b4c5d-6e7f-8a9b-0c1d-2e3f4a5b6c7d",
      "medicalCaseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "chiefComplaint": "头痛三天，伴恶寒发热",
      "presentIllness": "患者三天前受凉后出现头痛",
      "tcmDiagnosis": "风寒感冒",
      "treatmentPrinciple": "辛温解表，宣肺散寒",
      "createdAt": "2025-10-27T10:05:00Z"
    }
  ]
}
```

---

### 13. GET /api/v1/medicalcases/{medicalCaseId}/prescriptions - 查询处方列表

**描述**: 获取指定病案的所有历史处方记录。

**请求**:
```http
GET /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescriptions
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
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
          "quantity": 7,
          "unitPrice": 2.5,
          "totalPrice": 17.5
        }
      ],
      "createdAt": "2025-10-27T10:15:00Z"
    }
  ]
}
```

---

### 14. GET /api/v1/medicalcases/patients/{patientId}/unfinished - 查询患者未完成病案（Epic #1676 Phase 4新增）

**描述**: 查询指定患者的未完成病案（Status != Completed）。用于Desktop端PatientSelectionViewModel检测患者是否有正在进行的病案。

**业务规则**:
- ✅ 只返回状态不为Completed的病案（Draft/Active）
- ✅ 一个患者理论上只有一个Active病案（BR-001），但可能有Draft状态的暂存病案
- ✅ 如无未完成病案，返回404

**请求**:
```http
GET /api/v1/medicalcases/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6/unfinished
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| patientId | Guid | ✅ | 患者ID |

**响应**:

✅ **成功 - 200 OK** (存在未完成病案)
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientName": "张三",
    "doctorId": "8b7d9f2a-3b14-4c8e-a9d6-1f2e3d4c5b6a",
    "doctorName": "李医生",
    "visitDate": "2025-10-27T10:00:00Z",
    "status": "Active",
    "needsPrescription": null,
    "createdAt": "2025-10-27T10:00:00Z"
  }
}
```

❌ **失败 - 404 Not Found** (无未完成病案)
```json
{
  "success": false,
  "message": "患者无未完成病案",
  "data": null
}
```

**Desktop端使用示例**:
```csharp
// PatientSelectionViewModel.cs
var unfinishedCase = await _medicalCaseRepository
    .GetUnfinishedCaseByPatientIdAsync(selectedPatient.Id);

if (unfinishedCase != null)
{
    // 提示用户继续现有病案或关闭后创建新病案
    ShowUnfinishedCaseDialog(unfinishedCase);
}
```

---

## Helper Layer - 辅助功能

### 15. GET /api/v1/medicalcases/{id}/can-edit - 验证病案是否可编辑

**描述**: 检查病案当前状态是否允许编辑，用于UI按钮状态控制。

**请求**:
```http
GET /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/can-edit
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK** (可编辑)
```json
{
  "success": true,
  "message": "验证成功",
  "data": {
    "canEdit": true,
    "reason": "病案处于Active状态，可以编辑"
  }
}
```

✅ **成功 - 200 OK** (不可编辑)
```json
{
  "success": true,
  "message": "验证成功",
  "data": {
    "canEdit": false,
    "reason": "病案已完成，无法编辑"
  }
}
```

---

### 16. GET /api/v1/medicalcases/{id}/prescriptions/{prescriptionId}/can-delete - 验证处方是否可删除

**描述**: 检查处方是否可删除（如是否已打印），用于UI删除按钮状态控制。

**请求**:
```http
GET /api/v1/medicalcases/7c9e6679-7425-40de-944b-e07fc1f90ae7/prescriptions/8a9b0c1d-2e3f-4a5b-6c7d-8e9f0a1b2c3d/can-delete
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK** (可删除)
```json
{
  "success": true,
  "message": "验证成功",
  "data": {
    "canDelete": true,
    "reason": "处方未打印，可以删除"
  }
}
```

✅ **成功 - 200 OK** (不可删除)
```json
{
  "success": true,
  "message": "验证成功",
  "data": {
    "canDelete": false,
    "reason": "处方已打印，无法删除"
  }
}
```

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

### 标准HTTP状态码

| 状态码 | 说明 | 适用场景 |
|--------|------|----------|
| 200 OK | 成功 | 所有成功的GET/PUT/POST请求 |
| 204 No Content | 成功（无响应体） | DELETE成功 |
| 400 Bad Request | 请求参数错误 | 参数验证失败、格式错误 |
| 401 Unauthorized | 未授权 | Token无效或过期 |
| 403 Forbidden | 权限不足 | 资源访问被拒绝（如处方不属于该病案） |
| 404 Not Found | 资源不存在 | 病案、患者、处方不存在 |
| 422 Unprocessable Entity | 业务规则验证失败 | AR-001/AR-003/BF-002/BR-001违规 |
| 500 Internal Server Error | 服务器错误 | 未预期的系统异常 |

---

## 业务规则说明

### AR-001: 聚合根约束

**定义**: 所有对Consultation和Prescription的操作必须通过MedicalCase聚合根完成。

**影响端点**:
- ✅ PUT /api/v1/medicalcases/{id}/consultation - 通过聚合根更新
- ✅ POST /api/v1/medicalcases/{id}/prescriptions - 通过聚合根创建
- ✅ PUT /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 通过聚合根更新
- ✅ DELETE /api/v1/medicalcases/{id}/prescriptions/{prescriptionId} - 通过聚合根删除

**违规示例**:
```csharp
// ❌ 错误：直接操作Consultation实体
await consultationRepository.UpdateAsync(consultation);

// ✅ 正确：通过MedicalCase聚合根
await medicalCaseService.UpdateConsultationAsync(medicalCaseId, request);
```

### AR-003: 一诊一方约束

**定义**: 一个病案只能有一个有效处方。

**影响端点**:
- POST /api/v1/medicalcases/{id}/prescriptions
- PUT /api/v1/medicalcases/{id}/prescription-flag

**违规场景**:
- 已有处方时，尝试再次创建处方 → 422
- 已有处方时，尝试标记needsPrescription=true → 422

**解决方法**: 先删除旧处方，再创建新处方。

### BF-002: 三步流程验证

**定义**: 病案必须按照"辨证 → 标记处方 → 开处方/完成"的三步流程操作。

**流程图**:
```
Step 1: UpdateConsultation（辨证）
  ↓ step1CompletedAt != null
Step 2: SetPrescriptionFlag（标记）
  ↓ step2CompletedAt != null
  ├─ needsPrescription = true
  │   ↓
  │ Step 3a: CreatePrescription（开处方）
  │   ↓
  └─ Complete（完成）
  │
  └─ needsPrescription = false
      ↓
    Step 3b: Complete（直接完成）
```

**影响端点**:
- PUT /api/v1/medicalcases/{id}/prescription-flag - 必须先完成Step 1
- POST /api/v1/medicalcases/{id}/prescriptions - 必须先完成Step 1+2
- PUT /api/v1/medicalcases/{id}/complete - 验证三步流程完整性

### BR-001: 单患者单Active病案

**定义**: 单个患者同一时间只能有一个Active状态的病案。

**影响端点**:
- POST /api/v1/medicalcases

**违规场景**: 患者已有Active病案时，尝试创建新病案 → 422

**解决方法**: 先完成或取消现有病案，再创建新病案。

---

## 参考资料

### 源码文件
- **Controller**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **Service**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **Repository**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **DTOs**: `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/`

### 测试文件
- **单元测试**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs` (32个测试，82.6%覆盖率)
- **集成测试**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs` (18个测试，100%通过率)

### 相关文档
- **E2E测试分析**: `docs/reports/e2e-test-coverage-analysis.md`
- **文档同步清单**: `docs/reports/epic-1612-doc-sync-checklist.md`
- **架构指南**: `docs/explanation/architecture/server/README.md`
- **业务规则**: `docs/explanation/business-rules.md` (AR-001, AR-003, BF-002, BR-001)

---

**文档版本**: v1.0
**最后更新**: 2025-10-27
**维护者**: Claude Code + lybtzyzs-doc-sync
**关联Issue**: #1670 (Epic #1612文档同步)
**Epic**: #1612 (MedicalCase模块重构 - Phase 2-3完成)

---

**文档状态**: ✅ 已验证（基于18个集成测试和32个单元测试）
**API稳定性**: ✅ 稳定（100%集成测试通过率）
**代码覆盖率**: ✅ 82.6%行覆盖率，57.14%分支覆盖率
