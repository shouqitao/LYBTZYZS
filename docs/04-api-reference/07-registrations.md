# 挂号管理 API

> Controller: `RegistrationsController` | 路由前缀: `/api/v1/registrations` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`

## 概述

挂号 (Registration) 模块管理患者挂号流程，支持前台挂号和医生快速看诊两种模式。
US-REG-001~006: 创建、查询、接诊、取消等操作。

---

## POST /registrations

创建挂号记录 (前台模式)。

- **权限**: DoctorOrReceptionist (前台/医生/管理员)
- US-REG-001: Source=Receptionist, Status=Waiting

**请求体** (`RegistrationInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `patientId` | Guid | 是 | 患者 ID |
| `patientName` | string | 是 | 患者姓名 |
| `doctorId` | Guid | 是 | 医生 ID |
| `doctorName` | string | 是 | 医生姓名 |
| `source` | string | 是 | 来源: `Receptionist` 或 `Doctor` |
| `remark` | string? | 否 | 备注 |

```json
{
  "patientId": "12345678-abcd-1234-abcd-123456789012",
  "patientName": "李四",
  "doctorId": "87654321-dcba-4321-dcba-210987654321",
  "doctorName": "张医生",
  "source": "Receptionist",
  "remark": "初诊患者，腰痛"
}
```

**成功响应** (201 Created): `ApiResponse<RegistrationDetailDto>`

```json
{
  "success": true,
  "message": "挂号创建成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "patientId": "12345678-abcd-1234-abcd-123456789012",
    "patientName": "李四",
    "doctorId": "87654321-dcba-4321-dcba-210987654321",
    "doctorName": "张医生",
    "medicalCaseId": null,
    "source": "Receptionist",
    "queueNumber": 3,
    "registrationFee": 20.00,
    "status": "Waiting",
    "remark": "初诊患者，腰痛",
    "createdAt": "2026-06-25T08:30:00Z",
    "updatedAt": null,
    "createdBy": "f1234567-abcd-1234-abcd-123456789012"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/registrations" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": "12345678-abcd-1234-abcd-123456789012",
    "patientName": "李四",
    "doctorId": "87654321-dcba-4321-dcba-210987654321",
    "doctorName": "张医生",
    "source": "Receptionist",
    "remark": "初诊患者，腰痛"
  }'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 409 | ERR-40102 | 患者已有进行中挂号 |

---

## POST /registrations/quick-visit

医生快速看诊（后台静默创建 Registration + MedicalCase）。

- **权限**: DoctorOrReceptionist
- US-REG-002: Source=Doctor, Status=InProgress, 医生无感知
- 使用数据库事务，确保 Registration 和 MedicalCase 同时创建或回滚

**请求体** (`QuickVisitInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `patientId` | Guid | 是 | 患者 ID |
| `patientName` | string | 是 | 患者姓名 |
| `remark` | string? | 否 | 备注 |

```json
{
  "patientId": "12345678-abcd-1234-abcd-123456789012",
  "patientName": "李四",
  "remark": "复诊"
}
```

**成功响应** (201 Created): `ApiResponse<QuickVisitResultDto>`

```json
{
  "success": true,
  "message": "快速看诊创建成功",
  "data": {
    "registrationId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "medicalCaseId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "patientId": "12345678-abcd-1234-abcd-123456789012",
    "patientName": "李四",
    "doctorId": "87654321-dcba-4321-dcba-210987654321",
    "doctorName": "张医生",
    "createdAt": "2026-06-25T09:15:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/registrations/quick-visit" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": "12345678-abcd-1234-abcd-123456789012",
    "patientName": "李四",
    "remark": "复诊"
  }'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 422 | — | 医案创建失败，事务已回滚 |

---

## GET /registrations/{id}

获取挂号详情。

- **权限**: DoctorOrReceptionist

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<RegistrationDetailDto>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "patientId": "12345678-abcd-1234-abcd-123456789012",
    "patientName": "李四",
    "doctorId": "87654321-dcba-4321-dcba-210987654321",
    "doctorName": "张医生",
    "medicalCaseId": null,
    "source": "Receptionist",
    "queueNumber": 3,
    "registrationFee": 20.00,
    "status": "Waiting",
    "remark": "初诊患者，腰痛",
    "createdAt": "2026-06-25T08:30:00Z",
    "updatedAt": null,
    "createdBy": "f1234567-abcd-1234-abcd-123456789012"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X GET "http://localhost:5000/api/v1/registrations/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 404 | ERR-40101 | 挂号不存在 |

---

## GET /registrations

分页查询挂号记录。

- **权限**: DoctorOrReceptionist
- US-REG-007: 支持按日期范围、患者、医生过滤

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页记录数 |
| `keyword` | string? | null | 搜索关键词（匹配患者名、医生名） |
| `startDate` | DateTime? | null | 开始日期 |
| `endDate` | DateTime? | null | 结束日期 |
| `patientId` | Guid? | null | 患者 ID |
| `doctorId` | Guid? | null | 医生 ID |

**成功响应** (200): `ApiResponse<PagedResult<RegistrationListDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "patientId": "12345678-abcd-1234-abcd-123456789012",
        "patientName": "李四",
        "doctorId": "87654321-dcba-4321-dcba-210987654321",
        "doctorName": "张医生",
        "medicalCaseId": null,
        "queueNumber": 3,
        "registrationFee": 20.00,
        "source": "Receptionist",
        "status": "Waiting",
        "createdAt": "2026-06-25T08:30:00Z",
        "hasMedicalCase": false
      },
      {
        "id": "d4e5f6a7-b8c9-0123-def0-123456789012",
        "patientId": "23456789-abcd-1234-abcd-123456789012",
        "patientName": "王五",
        "doctorId": "87654321-dcba-4321-dcba-210987654321",
        "doctorName": "张医生",
        "medicalCaseId": "e5f6a7b8-c9d0-1234-ef01-234567890123",
        "queueNumber": 2,
        "registrationFee": 20.00,
        "source": "Doctor",
        "status": "InProgress",
        "createdAt": "2026-06-25T08:00:00Z",
        "hasMedicalCase": true
      }
    ],
    "totalCount": 120,
    "page": 1,
    "pageSize": 20,
    "totalPages": 6
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
# 获取挂号列表（默认分页）
curl -X GET "http://localhost:5000/api/v1/registrations?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"

# 按日期范围筛选
curl -X GET "http://localhost:5000/api/v1/registrations?startDate=2026-06-01&endDate=2026-06-30" \
  -H "Authorization: Bearer $TOKEN"

# 按患者筛选
curl -X GET "http://localhost:5000/api/v1/registrations?patientId=12345678-abcd-1234-abcd-123456789012" \
  -H "Authorization: Bearer $TOKEN"

# 按医生筛选
curl -X GET "http://localhost:5000/api/v1/registrations?doctorId=87654321-dcba-4321-dcba-210987654321" \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /registrations/queue

获取等待队列。

- **权限**: DoctorOrReceptionist
- US-REG-003: Waiting 状态，按挂号时间升序

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `doctorId` | Guid? | 医生 ID（可选，过滤特定医生的队列） |

**成功响应** (200): `ApiResponse<List<RegistrationListDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": [
    {
      "id": "d4e5f6a7-b8c9-0123-def0-123456789012",
      "patientId": "23456789-abcd-1234-abcd-123456789012",
      "patientName": "王五",
      "doctorId": "87654321-dcba-4321-dcba-210987654321",
      "doctorName": "张医生",
      "medicalCaseId": null,
      "queueNumber": 2,
      "registrationFee": 20.00,
      "source": "Receptionist",
      "status": "Waiting",
      "createdAt": "2026-06-25T08:00:00Z",
      "hasMedicalCase": false
    },
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "patientId": "12345678-abcd-1234-abcd-123456789012",
      "patientName": "李四",
      "doctorId": "87654321-dcba-4321-dcba-210987654321",
      "doctorName": "张医生",
      "medicalCaseId": null,
      "queueNumber": 3,
      "registrationFee": 20.00,
      "source": "Receptionist",
      "status": "Waiting",
      "createdAt": "2026-06-25T08:30:00Z",
      "hasMedicalCase": false
    }
  ],
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
# 获取所有等待队列
curl -X GET "http://localhost:5000/api/v1/registrations/queue" \
  -H "Authorization: Bearer $TOKEN"

# 获取特定医生的等待队列
curl -X GET "http://localhost:5000/api/v1/registrations/queue?doctorId=87654321-dcba-4321-dcba-210987654321" \
  -H "Authorization: Bearer $TOKEN"
```

---

## PUT /registrations/{id}/start-visit

接诊：从队列选中患者，Registration 状态变更为 InProgress。

- **权限**: DoctorOrReceptionist
- US-REG-003 验收标准第4条

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<Guid>`（返回关联的 MedicalCaseId）

```json
{
  "success": true,
  "message": "接诊成功",
  "data": "c3d4e5f6-a7b8-9012-cdef-123456789012",
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X PUT "http://localhost:5000/api/v1/registrations/a1b2c3d4-e5f6-7890-abcd-ef1234567890/start-visit" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 404 | ERR-40101 | 挂号不存在 |
| 422 | ERR-40103 | 非 Waiting 状态，不可接诊 |

---

## PUT /registrations/{id}/cancel

取消挂号。

- **权限**: DoctorOrReceptionist
- US-REG-004: 仅 Waiting 状态可取消

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse`

```json
{
  "success": true,
  "message": "挂号已取消",
  "data": null,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X PUT "http://localhost:5000/api/v1/registrations/a1b2c3d4-e5f6-7890-abcd-ef1234567890/cancel" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 404 | ERR-40101 | 挂号不存在 |
| 422 | ERR-40104 | 非 Waiting 状态，不可取消 |

---

## 错误码汇总

| 错误码 | HTTP | 触发条件 |
|--------|------|----------|
| ERR-40101 | 404 | 挂号不存在 |
| ERR-40102 | 409 | 患者已有进行中挂号 |
| ERR-40103 | 400/422 | 挂号状态不允许此操作 |
| ERR-40104 | 422 | 仅 Waiting 状态可取消 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-05-04 | v1.0 | 初始版本，覆盖 RegistrationsController 全部 7 个端点 |
| 2026-06-12 | v1.1 | 修正 GET /registrations 和 GET /registrations/{id} 响应类型为 ApiResponse<> |
| 2026-06-12 | v1.2 | 新增错误码章节 (ERR-40101~40104) |
| 2026-06-25 | v2.0 | 全面重写：为全部 7 个端点补充完整请求/响应 JSON 示例、curl 命令、参数表；使用真实 GUID 和中文姓名 |
