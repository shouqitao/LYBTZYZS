# 报表 API

> Controller: `ReportsController` | 路由前缀: `/api/v1/reports` | 默认权限: `DoctorOrAdmin`

## 概述

提供当日经营数据统计报表，包括收入汇总、问诊统计和药材使用排行。所有端点返回当日实时聚合数据，无请求参数。

---

## 端点总览

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/reports/daily/income` | Doctor, Admin, SuperAdmin | 当日收入汇总 |
| GET | `/reports/daily/consultations` | Doctor, Admin, SuperAdmin | 当日问诊统计 |
| GET | `/reports/daily/herbs` | Doctor, Admin, SuperAdmin | 当日药材使用排行 |

---

## GET /reports/daily/income

查询当日收入汇总，包括挂号费、药费和总收入。

- **权限**: Doctor / Admin / SuperAdmin
- **请求参数**: 无

**成功响应** (200) `ApiResponse<DailyIncomeDto>`:

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "totalIncome": 3580.00,
    "registrationFeeTotal": 1200.00,
    "medicineFeeTotal": 2380.00
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9S..."
}
```

**响应字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalIncome` | decimal | 当日总收入 (挂号费 + 药费) |
| `registrationFeeTotal` | decimal | 当日挂号费合计 |
| `medicineFeeTotal` | decimal | 当日药费合计 |

**curl 示例：**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

curl -X GET http://localhost:5000/api/v1/reports/daily/income \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /reports/daily/consultations

查询当日问诊统计，包括总问诊数和按医生分组的统计。

- **权限**: Doctor / Admin / SuperAdmin
- **请求参数**: 无

**成功响应** (200) `ApiResponse<DailyConsultationDto>`:

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "totalCount": 15,
    "byDoctor": [
      {
        "doctorName": "张仲景",
        "count": 8
      },
      {
        "doctorName": "李时珍",
        "count": 7
      }
    ]
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9T..."
}
```

**响应字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalCount` | int | 当日问诊总数 |
| `byDoctor` | `DoctorCountDto[]` | 按医生分组的问诊统计 |
| `byDoctor[].doctorName` | string | 医生姓名 |
| `byDoctor[].count` | int | 该医生当日问诊数 |

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/reports/daily/consultations \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /reports/daily/herbs

查询当日药材使用排行，按使用次数降序排列。

- **权限**: Doctor / Admin / SuperAdmin
- **请求参数**: 无

**成功响应** (200) `ApiResponse<DailyHerbUsageDto>`:

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "herbName": "黄芪",
        "usageCount": 12,
        "totalDosage": 360.00
      },
      {
        "herbName": "当归",
        "usageCount": 9,
        "totalDosage": 180.00
      },
      {
        "herbName": "白术",
        "usageCount": 7,
        "totalDosage": 210.00
      },
      {
        "herbName": "茯苓",
        "usageCount": 6,
        "totalDosage": 180.00
      },
      {
        "herbName": "甘草",
        "usageCount": 5,
        "totalDosage": 75.00
      }
    ]
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9U..."
}
```

**响应字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `items` | `HerbUsageItemDto[]` | 药材使用排行列表 |
| `items[].herbName` | string | 药材名称 |
| `items[].usageCount` | int | 使用次数 (出现在处方中的次数) |
| `items[].totalDosage` | decimal | 总用量 (克) |

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/reports/daily/herbs \
  -H "Authorization: Bearer $TOKEN"
```

---

## 错误码

| HTTP 状态码 | 说明 | 场景 |
|------------|------|------|
| 200 | 查询成功 | 正常返回当日统计数据 |
| 401 | 未认证 | Token 无效/过期/被撤销 |
| 403 | 禁止访问 | 非 Doctor/Admin/SuperAdmin 角色 |
| 500 | 服务器错误 | 内部异常 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-25 | v1.0 | 初始版本，包含 3 个当日统计端点 |
