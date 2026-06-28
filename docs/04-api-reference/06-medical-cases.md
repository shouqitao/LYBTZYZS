# 医案 API

> Controllers: `MedicalCasesController`, `MedicalCaseProcessingController` | 路由前缀: `/api/v1/medicalcases` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`
>
> ⚠️ **权限待对齐（D7，基线§3）**：文档目标策略为 `DoctorOrReceptionist`，医案**创建端点**目标为 Doctor-only（注：`PolicyConstants` 中**无 `DoctorOnly` 常量**，仅有 `AdminOnly/DoctorOrAdmin/AdminOrSuperAdmin/DoctorOrReceptionist` 四项）。代码当前统一为 `DoctorOrAdmin`，待对齐。

## 概述

医案 (MedicalCase) 是系统核心聚合根，包含 Consultation (诊断) 和 Prescription (处方) 子实体。
采用 CQRS 原则: Command/Query/State 服务分离。所有写操作通过聚合根统一保存。
资源级授权通过 `MedicalCaseAuthorizationHandler` 实现。

**所有响应使用统一信封**: `ApiResponse<T>` (`{ "success": true, "message": "...", "data": T, "errors": null, "timestamp": ..., "requestId": "..." }`，**无 `code` 字段**，基线§6)

---

## 写操作 (Command)

### POST /medicalcases

创建新医案。

- **权限**: `[Authorize(Policy = "DoctorOrAdmin")]`
- 支持创建时同时包含 Consultation 和 Prescription 数据
- `Id=null` 触发创建逻辑

**请求体** (`MedicalCaseInputDto`):

```json
{
  "id": null,
  "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "registrationId": "b3a4c5d6-e7f8-9012-abcd-ef3456789012",
  "editReason": null,
  "needsPrescription": true,
  "consultation": {
    "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
    "tongueDiagnosis": "舌淡红，苔白腻",
    "pulseDiagnosis": "脉滑",
    "tcmDiagnosis": "痰湿蕴肺证"
  },
  "prescription": {
    "id": null,
    "medicalCaseId": "00000000-0000-0000-0000-000000000000",
    "needsPrescription": true,
    "dosageCount": 7,
    "usage": "每日一剂，水煎服",
    "advice": "忌食生冷，注意保暖",
    "referencedFormulas": "二陈汤",
    "discount": 1.0,
    "totalPrice": 0,
    "remark": null,
    "items": [
      {
        "id": null,
        "herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890",
        "herbName": "半夏",
        "unit": "g",
        "dosage": 10,
        "unitPrice": 0.50,
        "subtotal": 5.00,
        "usage": "先煎",
        "decocteMethod": "Default",
        "remark": null
      },
      {
        "id": null,
        "herbId": "d2e3f4a5-b6c7-8901-abcd-ef2345678901",
        "herbName": "陈皮",
        "unit": "g",
        "dosage": 6,
        "unitPrice": 0.30,
        "subtotal": 1.80,
        "usage": null,
        "decocteMethod": "Default",
        "remark": null
      },
      {
        "id": null,
        "herbId": "e3f4a5b6-c7d8-9012-abcd-ef3456789012",
        "herbName": "茯苓",
        "unit": "g",
        "dosage": 15,
        "unitPrice": 0.40,
        "subtotal": 6.00,
        "usage": null,
        "decocteMethod": "Default",
        "remark": null
      },
      {
        "id": null,
        "herbId": "f4a5b6c7-d8e9-0123-abcd-ef4567890123",
        "herbName": "甘草",
        "unit": "g",
        "dosage": 3,
        "unitPrice": 0.20,
        "subtotal": 0.60,
        "usage": null,
        "decocteMethod": "Default",
        "remark": null
      }
    ]
  }
}
```

**字段说明**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | Guid? | 是 | 创建时必须为 `null` |
| `patientId` | Guid | 是 | 患者 ID |
| `userId` | Guid | 是 | 医生 ID |
| `registrationId` | Guid? | 否 | 关联挂号 ID |
| `editReason` | string? | 否 | 编辑原因 (非当天本人操作时必填) |
| `needsPrescription` | bool? | 否 | 是否开处方标志 |
| `consultation` | object? | 否 | 诊断信息 (嵌套) |
| `prescription` | object? | 否 | 处方信息 (嵌套) |
| `prescription.items` | array | 是 | 处方项列表 (至少一项) |

**成功响应** (201 Created) `ApiResponse<MedicalCaseDetailDto>` — data 部分 (下方为完整 DTO 结构，其他端点引用此处):

```json
{
  "id": "12345678-abcd-ef01-2345-678901234567",
  "createdAt": "2026-06-25T10:30:00Z",
  "updatedAt": null,
  "createdBy": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "caseNumber": "MC-20260625-001",
  "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "patientName": "张三",
  "patientGender": "Male",
  "patientAge": 45,
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "doctorName": "李医生",
  "consultationId": "22345678-abcd-ef01-2345-678901234567",
  "prescriptionId": "32345678-abcd-ef01-2345-678901234567",
  "completedAt": null,
  "caseStatus": "Active",
  "diagnosis": "痰湿蕴肺证",
  "hasConsultation": true,
  "hasPrescription": true,
  "isLocked": false,
  "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
  "consultation": {
    "id": "22345678-abcd-ef01-2345-678901234567",
    "createdAt": "2026-06-25T10:30:00Z",
    "updatedAt": null,
    "createdBy": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "patientName": "张三",
    "doctorName": "李医生",
    "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
    "tongueDiagnosis": "舌淡红，苔白腻",
    "pulseDiagnosis": "脉滑",
    "tcmDiagnosis": "痰湿蕴肺证"
  },
  "prescription": {
    "id": "32345678-abcd-ef01-2345-678901234567",
    "prescriptionNumber": "RX-20260625-001",
    "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
    "dosageCount": 7,
    "usage": "每日一剂，水煎服",
    "advice": "忌食生冷，注意保暖",
    "referencedFormulas": "二陈汤",
    "remark": null,
    "singleDosePrice": 13.40,
    "totalPrice": 93.80,
    "totalWeight": 34.0,
    "discount": 1.0,
    "status": "Active",
    "createdAt": "2026-06-25T10:30:00Z",
    "updatedAt": null,
    "duplicateWarning": null,
    "missingDrugWarning": null,
    "items": [
      {
        "id": "42345678-abcd-ef01-2345-678901234567",
        "herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890",
        "herbName": "半夏",
        "unit": "g",
        "unitPrice": 0.50,
        "dosage": 10,
        "totalPrice": 35.00,
        "totalWeight": 70.0,
        "subtotal": 5.00,
        "usage": "先煎",
        "decocteMethod": "Default",
        "role": "None",
        "remark": null,
        "notes": null
      },
      {
        "id": "52345678-abcd-ef01-2345-678901234567",
        "herbId": "d2e3f4a5-b6c7-8901-abcd-ef2345678901",
        "herbName": "陈皮",
        "unit": "g",
        "unitPrice": 0.30,
        "dosage": 6,
        "totalPrice": 12.60,
        "totalWeight": 42.0,
        "subtotal": 1.80,
        "usage": null,
        "decocteMethod": "Default",
        "role": "None",
        "remark": null,
        "notes": null
      },
      {
        "id": "62345678-abcd-ef01-2345-678901234567",
        "herbId": "e3f4a5b6-c7d8-9012-abcd-ef3456789012",
        "herbName": "茯苓",
        "unit": "g",
        "unitPrice": 0.40,
        "dosage": 15,
        "totalPrice": 42.00,
        "totalWeight": 105.0,
        "subtotal": 6.00,
        "usage": null,
        "decocteMethod": "Default",
        "role": "None",
        "remark": null,
        "notes": null
      },
      {
        "id": "72345678-abcd-ef01-2345-678901234567",
        "herbId": "f4a5b6c7-d8e9-0123-abcd-ef4567890123",
        "herbName": "甘草",
        "unit": "g",
        "unitPrice": 0.20,
        "dosage": 3,
        "totalPrice": 4.20,
        "totalWeight": 21.0,
        "subtotal": 0.60,
        "usage": null,
        "decocteMethod": "Default",
        "role": "None",
        "remark": null,
        "notes": null
      }
    ]
  }
}
```

**curl 命令**:

```bash
curl -X POST "https://api.example.com/api/v1/medicalcases" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "id": null,
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "consultation": {
      "presentIllness": "患者近一周反复咳嗽，痰多色白",
      "tongueDiagnosis": "舌淡红，苔白腻",
      "pulseDiagnosis": "脉滑",
      "tcmDiagnosis": "痰湿蕴肺证"
    },
    "prescription": {
      "dosageCount": 7,
      "items": [
        {"herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890", "dosage": 10, "unit": "g", "unitPrice": 0.50, "subtotal": 5.00}
      ]
    }
  }'
```

**错误响应** (通用 401/403 见 [README](README.md#通用-http-状态码)):

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 404 | ERR-30101 | 患者不存在 |
| 422 | ERR-30103 | 该患者已有进行中的医案 (BR-001) |
| 422 | ERR-30104 | 该患者已有挂起的医案 (BR-001) |
| 422 | ERR-30105 | 该患者已被禁用 |

---

### PUT /medicalcases/{id}

聚合保存 -- 在单个事务中同时保存 Consultation 和 Prescription。

**路径参数**: `id` (Guid)

**请求体**: `MedicalCaseInputDto` (同创建，但 `id` 必须与路由一致)

额外字段:

| 字段 | 类型 | 说明 |
|------|------|------|
| `editReason` | string? | 修改原因。以下场景必填: 编辑已完成医案、隔天编辑、非本人编辑、**打印后编辑** (MC-D15) |

**授权**: 资源级授权 (Edit 操作)

**打印保护规则** (MC-D15):
- 当 `MedicalCase.IsPrinted=true` 时，修改 Consultation 或 Prescription 内容需提供 `editReason`，否则返回 ERR-30403
- 修改成功后: `MedicalCase.IsPrinted=false`、`MedicalCase.PrintVersion++` (标记需重新打印)
- 打印后删除处方始终禁止 (ERR-30404)

**请求示例** (更新处方):

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "12345678-abcd-ef01-2345-678901234567",
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "editReason": "打印后修正剂量",
    "consultation": {
      "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。二诊：咳嗽减轻。",
      "tongueDiagnosis": "舌淡红，苔薄白",
      "pulseDiagnosis": "脉滑",
      "tcmDiagnosis": "痰湿蕴肺证"
    },
    "prescription": {
      "id": "32345678-abcd-ef01-2345-678901234567",
      "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
      "dosageCount": 5,
      "items": [
        {"herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890", "dosage": 12, "unit": "g", "unitPrice": 0.50, "subtotal": 6.00},
        {"herbId": "d2e3f4a5-b6c7-8901-abcd-ef2345678901", "dosage": 6, "unit": "g", "unitPrice": 0.30, "subtotal": 1.80}
      ]
    }
  }'
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- 结构同 POST 创建响应

**错误响应** (通用 401/403/404 见 [README](README.md#通用-http-状态码)):

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 400 | ERR-30601 | 路由 ID 与请求体 `id` 不一致 |
| 422 | ERR-30403 | 医案已打印，修改需要提供修改原因 (MC-D15) |
| 422 | ERR-30404 | 医案已打印，不允许删除处方 (MC-D15) |

---

### PUT /medicalcases/{id}/prescription-flag

标记是否需要开处方 (三步流程 Step 2)。

**路径参数**: `id` (Guid)

**请求体** (`SetPrescriptionFlagRequest`):

```json
{
  "needsPrescription": true
}
```

**curl 命令**:

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/prescription-flag" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"needsPrescription": true}'
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- 返回更新后的医案详情

**错误响应**: 通用 401/403/404 见 [README](README.md#通用-http-状态码)，本端点无专属错误码。

---

### PUT /medicalcases/{id}/status

更新医案状态。支持 Suspended/Active 状态流转。

> **注意**: 远程模式下 `Completed` 状态会自动委托给 `CompleteAsync` (等同于调用 `/{id}/close`)。本地模式拒绝直接设置 `Completed`，必须使用 `/{id}/close` 端点。`Cancelled` 状态已移除，取消操作请使用 `PUT /{id}/cancel` (软删除)。

**路径参数**: `id` (Guid)

**请求体** (`MedicalCaseStatusInputDto`):

```json
{
  "status": "Suspended",
  "statusChangeReason": "患者需要时间考虑"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `status` | MedicalCaseStatus | 是 | `Suspended` (0) 或 `Active` (1) |
| `statusChangeReason` | string? | 否 | 状态变更原因，最多 500 字符 |

**curl 命令**:

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/status" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"status": "Suspended", "statusChangeReason": "患者需要时间考虑"}'
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- `caseStatus` 变为目标状态

**错误响应** (通用 401/403/404 见 [README](README.md#通用-http-状态码)):

| HTTP | 说明 |
|------|------|
| 422 | 非法状态转换 (如 Suspended → Completed) |
| 422 | 尝试设置 Completed 状态 (请使用 /close) |

---

### PUT /medicalcases/{id}/close

完成医案 (统一入口)。底层调用 `CompleteAsync`，支持 `skipWorkflowValidation` 参数跳过三步流程校验。通过聚合根域方法 `MedicalCase.Complete()` 设置状态和 CompletedAt。

**路径参数**: `id` (Guid)

**curl 命令**:

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/close" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<MedicalCaseDetailDto>` — data 部分:

```json
{
  "id": "12345678-abcd-ef01-2345-678901234567",
  "caseStatus": "Completed",
  "completedAt": "2026-06-25T15:00:00Z",
  "...": "其他字段同 MedicalCaseDetailDto (见 POST 创建响应)"
}
```

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 422 | ERR-30301 | 不允许的状态转换 |
| 422 | ERR-30302 | 请先标记是否需要开处方 (BR-003) |
| 422 | ERR-30303 | 处方不存在，无法完成医案 (BR-003) |

---

### PUT /medicalcases/{id}/suspend

挂起医案。保存当前数据，设置状态为 Suspended，不触发完成验证。

**路径参数**: `id` (Guid)

**请求体** (可选): `ConsultationInputDto` -- 可附带诊断信息一起保存

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/suspend" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "presentIllness": "患者复诊，咳嗽好转",
    "tcmDiagnosis": "痰湿蕴肺证"
  }'
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- `caseStatus` 变为 `Suspended`

**错误响应** (通用 401/403/404 见 [README](README.md#通用-http-状态码)):

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 422 | ERR-30502 | 保存失败 (重试后仍失败) |

---

### PUT /medicalcases/{id}/cancel

取消医案 (统一为软删除 + 审计日志)。需要审计理由 (非当天本人操作时)。

**路径参数**: `id` (Guid)

**请求体** (可选) (`CancelMedicalCaseRequestDto`):

```json
{
  "reason": "患者要求取消挂号"
}
```

**curl 命令**:

```bash
curl -X PUT "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/cancel" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"reason": "患者要求取消挂号"}'
```

**成功响应** (204): 无内容 (取消操作统一为软删除，不再返回 DTO)

**错误响应**: 通用 401/403/404 见 [README](README.md#通用-http-状态码)，本端点无专属错误码。

---

### DELETE /medicalcases/{id}

删除医案 (软删除)。

**路径参数**: `id` (Guid)

**curl 命令**:

```bash
curl -X DELETE "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<bool>` — data: `true`

**错误响应**: 通用 401/403/404 见 [README](README.md#通用-http-状态码)，本端点无专属错误码。

---

### POST /medicalcases/batch-delete

批量删除医案。

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": [
    "12345678-abcd-ef01-2345-678901234567",
    "22345678-abcd-ef01-2345-678901234567",
    "32345678-abcd-ef01-2345-678901234567"
  ]
}
```

**curl 命令**:

```bash
curl -X POST "https://api.example.com/api/v1/medicalcases/batch-delete" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"ids": ["12345678-abcd-ef01-2345-678901234567", "22345678-abcd-ef01-2345-678901234567"]}'
```

**成功响应** (200) `ApiResponse<BatchOperationResultDto>` — data 部分 (全部成功):

```json
{
  "isSuccess": true,
  "message": "批量删除完成",
  "errorCode": null,
  "operationTime": "2026-06-25T10:30:00Z",
  "totalCount": 2,
  "successCount": 2,
  "failureCount": 0,
  "skippedCount": 0,
  "successfulIds": [
    "12345678-abcd-ef01-2345-678901234567",
    "22345678-abcd-ef01-2345-678901234567"
  ],
  "failedIds": [],
  "errors": [],
  "failedItems": [],
  "successRate": 100.0
}
```

**部分失败响应** (data 部分):

```json
{
  "isSuccess": true,
  "message": "批量删除完成，部分失败",
  "totalCount": 3,
  "successCount": 2,
  "failureCount": 1,
  "skippedCount": 0,
  "successfulIds": [
    "12345678-abcd-ef01-2345-678901234567",
    "22345678-abcd-ef01-2345-678901234567"
  ],
  "failedIds": ["32345678-abcd-ef01-2345-678901234567"],
  "errors": [],
  "failedItems": [
    {
      "id": "32345678-abcd-ef01-2345-678901234567",
      "name": null,
      "reason": "无权限删除此医案"
    }
  ],
  "successRate": 66.67
}
```

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 400 | ERR-30604 | 请至少选择一个医案 (ids 为空) |

---

## 读操作 (Query)

### GET /medicalcases/{id}

获取医案详情 (含 Consultation + Prescription 完整数据)。

**路径参数**: `id` (Guid)

**curl 命令**:

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<MedicalCaseDetailDto>` — 结构同 [POST 创建响应](#post-medicalcases) 的 MedicalCaseDetailDto

**错误响应** (通用 401/403 见 [README](README.md#通用-http-状态码)):

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 404 | ERR-30607 | 医案不存在 |

---

### GET /medicalcases

医案列表 (分页)。Doctor 角色自动过滤为仅看到自己的医案。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `status` | MedicalCaseStatus? | null | 状态筛选: `Suspended` (0) / `Active` (1) / `Completed` (2) |
| `patientId` | Guid? | null | 按患者筛选 |
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `includeAllDoctors` | bool | false | 跨医生查询 |
| `keyword` | string? | null | 搜索关键词 |

**curl 命令**:

```bash
# 查询 Active 状态的医案
curl -X GET "https://api.example.com/api/v1/medicalcases?status=Active&page=1&pageSize=10" \
  -H "Authorization: Bearer <token>"

# 按患者查询
curl -X GET "https://api.example.com/api/v1/medicalcases?patientId=a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<PagedResult<MedicalCaseListDto>>` — data 部分:

```json
{
  "items": [
    {
      "id": "12345678-abcd-ef01-2345-678901234567",
      "caseNumber": "MC-20260625-001",
      "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "patientName": "张三",
      "patientGender": "Male",
      "patientAge": 45,
      "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "doctorName": "李医生",
      "completedAt": null,
      "caseStatus": "Active",
      "diagnosis": "痰湿蕴肺证",
      "hasConsultation": true,
      "hasPrescription": true,
      "createdAt": "2026-06-25T10:30:00Z"
    },
    {
      "id": "22345678-abcd-ef01-2345-678901234567",
      "caseNumber": "MC-20260625-002",
      "patientId": "b2c3d4e5-f6a7-8901-abcd-ef2345678901",
      "patientName": "李四",
      "patientGender": "Female",
      "patientAge": 32,
      "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "doctorName": "李医生",
      "completedAt": null,
      "caseStatus": "Suspended",
      "diagnosis": "肝郁气滞证",
      "hasConsultation": true,
      "hasPrescription": false,
      "createdAt": "2026-06-25T09:15:00Z"
    }
  ],
  "totalCount": 25,
  "pageIndex": 1,
  "pageSize": 10
}
```

---

### GET /medicalcases/query

统一查询端点。整合多种查询类型。

**查询参数** (`MedicalCaseQueryDto`):

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `queryType` | MedicalCaseQueryType | All | 查询类型: All (0) / ByPatient (1) / Pending (2) / Unfinished (3) / Recent (4) |
| `pageIndex` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `patientId` | Guid? | null | 患者 ID (ByPatient/Recent 必填) |
| `doctorId` | Guid? | null | 医生 ID (不传使用当前用户) |
| `keyword` | string? | null | 关键字搜索 |
| `includeAllDoctors` | bool | false | Admin 自动设置为 true |
| `limit` | int? | null | 限制返回数量 (Recent 查询) |

**示例 1: 查询所有医案 (All)**

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=All&pageIndex=1&pageSize=20" \
  -H "Authorization: Bearer <token>"
```

**示例 2: 按患者查询 (ByPatient)**

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=ByPatient&patientId=a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer <token>"
```

**示例 3: 查询待看诊 (Pending)**

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=Pending" \
  -H "Authorization: Bearer <token>"
```

**示例 4: 查询未完成 (Unfinished)**

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=Unfinished&patientId=a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer <token>"
```

**示例 5: 最近记录 (Recent，用于处方参考)**

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=Recent&patientId=a1b2c3d4-e5f6-7890-abcd-ef1234567890&limit=5" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200): `ApiResponse<PagedResult<MedicalCaseListDto>>` -- 结构同 GET /medicalcases

---

### GET /medicalcases/search

跨医案搜索。支持按患者名称、诊断关键词等条件查询。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `patientName` | string? | 患者名称 (模糊匹配) |
| `diagnosisKeyword` | string? | 诊断关键词 |
| `startDate` | DateTime? | 开始日期 |
| `endDate` | DateTime? | 结束日期 |
| `page` | int | 页码 (默认 1) |
| `pageSize` | int | 每页大小 (默认 20) |

**curl 命令**:

```bash
# 按患者名称搜索
curl -X GET "https://api.example.com/api/v1/medicalcases/search?patientName=张三" \
  -H "Authorization: Bearer <token>"

# 按诊断关键词 + 日期范围搜索
curl -X GET "https://api.example.com/api/v1/medicalcases/search?diagnosisKeyword=咳嗽&startDate=2026-06-01&endDate=2026-06-25" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<PagedResult<MedicalCaseDetailDto>>` — data 部分 (items 中每个元素结构同 [POST 创建响应](#post-medicalcases) 的 MedicalCaseDetailDto):

```json
{
  "items": [
    {
      "id": "12345678-abcd-ef01-2345-678901234567",
      "caseNumber": "MC-20260625-001",
      "patientName": "张三",
      "doctorName": "李医生",
      "caseStatus": "Completed",
      "diagnosis": "痰湿蕴肺证",
      "presentIllness": "患者近一周反复咳嗽，痰多色白",
      "consultation": { "..." },
      "prescription": { "..." },
      "createdAt": "2026-06-25T10:30:00Z"
    }
  ],
  "totalCount": 3,
  "pageIndex": 1,
  "pageSize": 20
}
```

---

### GET /medicalcases/{medicalCaseId}/consultations

获取医案的诊断记录列表。

**路径参数**: `medicalCaseId` (Guid)

**curl 命令**:

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/consultations" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<List<ConsultationDetailDto>>` — data 部分:

```json
[
  {
    "id": "22345678-abcd-ef01-2345-678901234567",
    "createdAt": "2026-06-25T10:30:00Z",
    "updatedAt": null,
    "createdBy": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "patientName": "张三",
    "doctorName": "李医生",
    "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
    "tongueDiagnosis": "舌淡红，苔白腻",
    "pulseDiagnosis": "脉滑",
    "tcmDiagnosis": "痰湿蕴肺证"
  }
]
```

---

### GET /medicalcases/{medicalCaseId}/prescriptions

获取医案的处方记录列表。

**路径参数**: `medicalCaseId` (Guid)

**curl 命令**:

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases/12345678-abcd-ef01-2345-678901234567/prescriptions" \
  -H "Authorization: Bearer <token>"
```

**成功响应** (200) `ApiResponse<List<PrescriptionDetailDto>>` — data 部分:

```json
[
  {
    "id": "32345678-abcd-ef01-2345-678901234567",
    "prescriptionNumber": "RX-20260625-001",
    "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
    "dosageCount": 7,
    "usage": "每日一剂，水煎服",
    "advice": "忌食生冷，注意保暖",
    "referencedFormulas": "二陈汤",
    "remark": null,
    "singleDosePrice": 13.40,
    "totalPrice": 93.80,
    "totalWeight": 34.0,
    "discount": 1.0,
    "status": "Active",
    "createdAt": "2026-06-25T10:30:00Z",
    "updatedAt": null,
    "duplicateWarning": null,
    "missingDrugWarning": null,
    "items": [
      {
        "id": "42345678-abcd-ef01-2345-678901234567",
        "herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890",
        "herbName": "半夏",
        "unit": "g",
        "unitPrice": 0.50,
        "dosage": 10,
        "totalPrice": 35.00,
        "totalWeight": 70.0,
        "subtotal": 5.00,
        "usage": "先煎",
        "decocteMethod": "Default",
        "role": "None",
        "remark": null,
        "notes": null
      }
    ]
  }
]
```

---

## 复制历史处方 (FR-MC-018)

复制历史处方无专用端点，通过现有 API 组合实现 (客户端驱动模式):

```
步骤 1: GET /medicalcases?patientId={patientId}&status=Completed
         → 获取同一患者的历史已完成医案列表

步骤 2: GET /medicalcases/{historicalCaseId}
         → 获取历史医案完整详情 (含 Prescription.Items)

步骤 3: 客户端组装
         → 用户预览并选择处方药材
         → 禁用药材跳过并提示 (MC-D09)
         → UnitPrice 从药材库实时获取 (MC-D13)
         → ReferencedFormulas 记录来源

步骤 4: PUT /medicalcases/{currentId}
         → 聚合保存，Prescription.Items 包含复制的药材
```

**业务规则** (来自 PRD FR-MC-018):
- 价格策略: 从药材库实时获取，历史价格仅作预览参考 (MC-D13)
- 禁用药材: 跳过并提示用户 (MC-D09)
- 数据独立: 复制后的处方与原医案无关联，修改不影响原数据
- 总价公式: SingleDosePrice = SUM(Items.Amount); TotalPrice = SingleDosePrice x DosageCount x Discount (MC-D14)

---

## 已移除端点

以下端点已从代码中移除 (v2.3 深度重构):

| 已移除端点 | 替代方案 | 移除原因 |
|----------|----------|----------|
| `GET /{id}/with-details` | `GET /{id}` | 已统一返回完整详情，75 行内联映射冗余 |
| `GET /by-patient/{patientId}` | `GET /query?queryType=ByPatient&patientId=...` | 统一查询端点已覆盖 |
| `GET /patient/{patientId}/recent` | `GET /query?queryType=Recent&patientId=...` | 统一查询端点已覆盖 |
| `GET /patient/{patientId}/unfinished` | `GET /query?queryType=Unfinished&patientId=...` | 统一查询端点已覆盖 |

---

## 错误码

> 完整错误码定义见 [medical-cases.md PRD](../02-requirements/07-medical-cases.md)。错误码分区: 3xxxx。

### 创建医案 (301xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30101 | PatientNotFound | 404 | 患者不存在 | POST / |
| ERR-30102 | DoctorNotFound | 404 | 医生不存在 | POST / |
| ERR-30103 | ActiveCaseExists | 422 | 该患者已有进行中的医案 | POST / (BR-001) |
| ERR-30104 | SuspendedCaseExists | 422 | 该患者已有挂起的医案 | POST / (BR-001) |
| ERR-30105 | PatientDisabled | 422 | 该患者已被禁用 | POST / |

### 权限 (302xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30201 | CannotEditCase | 403 | 无权限编辑此医案 | PUT /{id} |
| ERR-30202 | CannotDeleteCase | 403 | 无权限删除此医案 | DELETE /{id} |
| ERR-30203 | CannotCancelCase | 403 | 无权限取消此医案 | PUT /{id}/cancel |
| ERR-30204 | CannotDeletePrescription | 403 | 无权限删除处方 | PUT /{id} (聚合保存触发) |
| ERR-30205 | CannotSuspend | 403 | 无权限挂起此医案 | PUT /{id}/suspend |

### 状态转换 (303xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30301 | InvalidStatusTransition | 422 | 不允许的状态转换 | PUT /{id}/close, PUT /{id}/cancel |
| ERR-30302 | PrescriptionFlagRequired | 422 | 请先标记是否需要开处方 | PUT /{id}/close (BR-003) |
| ERR-30303 | PrescriptionRequired | 422 | 处方不存在，无法完成医案 | PUT /{id}/close (BR-003) |

### 处方 (304xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30401 | PrescriptionFlagNotSet | 422 | 未标记需要开处方 | PUT /{id} (聚合保存触发) |
| ERR-30402 | PrescriptionAlreadyExists | 422 | 医案已存在处方 | PUT /{id} (聚合保存触发) |
| ERR-30403 | PrintedRequiresReason | 422 | 医案已打印，修改需要提供修改原因 | PUT /{id} (MC-D15) |
| ERR-30404 | PrintedCannotDelete | 422 | 医案已打印，不允许删除处方 | PUT /{id} (MC-D15) |

### 并发和系统 (305xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30501 | PrescriptionCreateRetryFailed | 500 | 创建处方失败 | PUT /{id} (聚合保存触发) |
| ERR-30502 | SaveRetryFailed | 500 | 保存失败 | PUT /{id}, PUT /{id}/suspend |

### 参数验证 (306xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30601 | RequestIdMismatch | 400 | 路由 ID 与请求体不一致 | PUT /{id} |
| ERR-30602 | InvalidPagination | 400 | 分页参数无效 | GET / |
| ERR-30603 | BatchQueryExceeded | 400 | 单次最多查询 50 个医案 | POST /batch-details |
| ERR-30604 | BatchOperationEmpty | 400 | 请至少选择一个医案 | POST /batch-delete |
| ERR-30607 | CaseNotFound | 404 | 医案不存在 | GET /{id}, PUT /{id}, DELETE /{id} |

---

## 🚧 待实现端点（v1.0 范围，D1/D2/D9 补回项）

> 以下端点属 v1.0 设计范围（基线§1），代码尚未实现，文档保留设计。详见 `docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md`。

| 端点 | 说明 | 补回决策 |
|------|------|----------|
| `POST /medicalcases/batch-details` | 批量获取医案详情（单次最多 50 个，对应 ERR-30603） | 🚧 v1.0 待实现（**D9** 历史聚合查询补回） |
| `GET /medicalcases/{id}/permissions` | 获取当前用户对该医案的操作权限 | 🚧 v1.0 待实现（**D9**） |
| `GET /medicalcases/{id}/audit-logs` | 医案审计日志查询（需 AuditLog 实体 + Service） | 🚧 v1.0 待实现（**D1** 医案审计日志补回，对应 MC-017） |
| `PUT /medicalcases/{id}/print-completed` | 记录打印完成（IsPrinted/PrintVersion/PrintCount/LastPrintedAt 回写） | 🚧 v1.0 待实现（**D2** 打印保护/回写补回，PRINT-004） |
| `POST /medicalcases/{id}/print-logs` | 添加打印日志（需 PrintLog 实体） | 🚧 v1.0 待实现（**D2**） |

> **幻影实体说明**：`AuditLog`、`MedicalCasePrintLog` 实体当前**代码中不存在**（`AppDbContext` 无对应 DbSet），属 D1/D2 补回时新建。详见基线§7 原则 2。

---

## 打印操作

打印端点已独立到 [printing.md](08-printing.md)，包含:

- `PUT /medicalcases/{id}/print-completed` — 记录打印完成 🚧（D2 待实现）
- `POST /medicalcases/{id}/print-logs` — 添加打印日志 🚧（D2 待实现）

---

## 枚举值速查

### MedicalCaseStatus

| 值 | 整数 | 说明 |
|----|------|------|
| Suspended | 0 | 已挂起 (MC-D20: 原 Draft) |
| Active | 1 | 进行中 |
| Completed | 2 | 已完成 |

### MedicalCaseQueryType

| 值 | 整数 | 说明 |
|----|------|------|
| All | 0 | 全部分页列表 |
| ByPatient | 1 | 按患者查询 |
| Pending | 2 | 待看诊 |
| Unfinished | 3 | 未完成 |
| Recent | 4 | 最近记录 (处方参考) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，18+ 端点 (含 5 个废弃) |
| 2026-02-18 | v1.1 | PRD同步: PUT /medicalcases/{id} 补充打印保护规则 (MC-D15, editReason/ERR-30403/ERR-30404); 新增"复制历史处方"组合API实现路径文档 (FR-MC-018) |
| 2026-02-18 | v1.2 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-30101~30607)，含创建/权限/状态/处方/并发/参数六类 |
| 2026-02-21 | v1.3 | 深度重构同步: PUT /status 移除 Cancelled/Completed 支持 (Completed 需用 /close); PUT /cancel 响应改为 204; 4 个废弃端点已从代码移除; caseStatus 枚举移除 Cancelled; /close 补充统一入口说明; 术语"病案"统一为"医案" |
| 2026-02-22 | v1.4 | MC-D20 同步: Draft→Suspended 状态重命名; `/draft`→`/suspend` 端点重命名; DraftCaseExists→SuspendedCaseExists; CannotSaveDraft→CannotSuspend |
| 2026-06-12 | v1.5 | 打印端点移至 printing.md，改为交叉引用 |
| 2026-06-12 | v1.6 | MedicalCaseInputDto: 新增 userId/registrationId/editReason/needsPrescription 字段 |
| 2026-06-25 | v2.0 | 补充所有端点完整请求/响应 JSON 示例和 curl 命令; 更新 DTO 字段与代码对齐 (移除 remark 字段, 修正 PrescriptionInputDto 结构); 新增枚举值速查表; 移除不存在的 MedicalCasePrintController/MedicalCaseAuditController 引用 |
| 2026-06-28 | v2.1 | 文档对齐基线：响应信封 code→success（基线§6）；权限策略加 D7 待对齐标注（目标 DoctorOrReceptionist/创建 Doctor-only，代码 DoctorOrAdmin）；新增「待实现端点」章节标注 batch-details/permissions/audit-logs/print-completed/print-logs（D1 审计/D2 打印回写/D9 历史） |
| 2026-06-28 | v2.2 | 文档结构优化批次1：3 处 MedicalCaseDetailDto 重复合一；JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；通用状态码引用 README |
