# 挂号管理 API

> Controller: `RegistrationsController` | 路由前缀: `/api/v1/registrations` | 默认权限: `[Authorize(Policy = "PatientAccess")]`

## 概述

挂号 (Registration) 模块管理患者挂号流程，支持前台挂号和医生快速看诊两种模式。
US-REG-001~006: 创建、查询、接诊、取消等操作。

---

## POST /registrations

创建挂号记录 (前台模式)。

- **权限**: PatientAccess (前台/医生/管理员)
- US-REG-001: Source=Receptionist, Status=Waiting

**请求体** (`RegistrationInputDto`):

```json
{
  "patientId": "guid",
  "patientName": "string",
  "doctorId": "guid",
  "doctorName": "string",
  "source": "Receptionist|Doctor",
  "remark": "string?"
}
```

**成功响应** (201):

```json
{
  "success": true,
  "message": "挂号创建成功",
  "data": {
    "id": "guid",
    "patientId": "guid",
    "patientName": "string",
    "doctorId": "guid",
    "doctorName": "string",
    "status": "Waiting",
    "source": "Receptionist",
    "createdAt": "2026-02-10T08:00:00Z"
  }
}
```

---

## POST /registrations/quick-visit

医生快速看诊 (后台静默创建 Registration + MedicalCase)。

- **权限**: DoctorOrAdmin
- US-REG-002: Source=Doctor, Status=InProgress, 医生无感知
- 使用数据库事务，确保 Registration 和 MedicalCase 同时创建或回滚

**请求体** (`QuickVisitInputDto`):

```json
{
  "patientId": "guid",
  "patientName": "string",
  "remark": "string?"
}
```

**成功响应** (201):

```json
{
  "success": true,
  "message": "快速看诊创建成功",
  "data": {
    "registrationId": "guid",
    "medicalCaseId": "guid",
    "patientId": "guid",
    "patientName": "string",
    "doctorId": "guid",
    "doctorName": "string",
    "createdAt": "2026-02-10T08:00:00Z"
  }
}
```

**错误场景**:

| 场景 | HTTP | 说明 |
|------|------|------|
| 医案创建失败 | 422 | 挂号记录已创建但医案创建失败，事务回滚 |

---

## GET /registrations/{id}

获取挂号详情。

- **权限**: PatientAccess

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| id | Guid | 挂号ID |

**成功响应** (200): `ApiResponse<RegistrationDetailDto>`

---

## GET /registrations

分页查询挂号记录。

- **权限**: PatientAccess
- US-REG-007: 支持按日期范围、患者、医生过滤

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| page | int | 1 | 页码 |
| pageSize | int | 20 | 每页记录数 |
| keyword | string? | null | 搜索关键词 |
| startDate | DateTime? | null | 开始日期 |
| endDate | DateTime? | null | 结束日期 |
| patientId | Guid? | null | 患者ID |
| doctorId | Guid? | null | 医生ID |

**成功响应** (200): `ApiResponse<PagedResult<RegistrationListDto>>`

---

## GET /registrations/queue

获取等待队列。

- **权限**: PatientAccess
- US-REG-003: Waiting 状态，按挂号时间升序

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| doctorId | Guid? | 医生ID (可选，过滤特定医生的队列) |

**成功响应** (200):

```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "patientName": "string",
      "doctorName": "string",
      "status": "Waiting",
      "createdAt": "2026-02-10T08:00:00Z"
    }
  ]
}
```

---

## PUT /registrations/{id}/start-visit

接诊: 从队列选中患者，Registration 状态变更为 InProgress。

- **权限**: DoctorOrAdmin
- US-REG-003 验收标准第4条

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| id | Guid | 挂号ID |

**成功响应** (200): `ApiResponse<Guid>` (返回关联的 MedicalCaseId)

**错误场景**:

| 场景 | HTTP | 说明 |
|------|------|------|
| 非 Waiting 状态 | 422 | 只有 Waiting 状态可接诊 |

---

## PUT /registrations/{id}/cancel

取消挂号。

- **权限**: PatientAccess
- US-REG-004: 仅 Waiting 状态可取消

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| id | Guid | 挂号ID |

**成功响应** (200):

```json
{
  "success": true,
  "message": "挂号已取消"
}
```

**错误场景**:

| 场景 | HTTP | 说明 |
|------|------|------|
| 非 Waiting 状态 | 422 | 只有 Waiting 状态可取消 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-05-04 | v1.0 | 初始版本，覆盖 RegistrationsController 全部 7 个端点 |
| 2026-06-12 | v1.1 | 修正 GET /registrations 和 GET /registrations/{id} 响应类型为 ApiResponse<> |
