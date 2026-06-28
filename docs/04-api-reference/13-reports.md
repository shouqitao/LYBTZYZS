# 报表 API

> Controller: `ReportsController` | 路由前缀: `/api/v1/reports` | 默认权限: `DoctorOrAdmin`

## 概述

提供经营数据统计报表（收入 / 就诊 / 药材），支持时间范围查询（日 / 月 / 自定义区间，默认当日）。三个端点各接受可选 `startDate` / `endDate` 查询参数（ISO 日期），缺省时默认当日，向后兼容历史调用。详见需求 [US-REPORT-001~003](../02-requirements/10-reports.md)。

---

## 端点总览

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/reports/daily/income` | Doctor, Admin, SuperAdmin | 收入汇总（支持时间范围，默认当日） |
| GET | `/reports/daily/consultations` | Doctor, Admin, SuperAdmin | 问诊统计（支持时间范围，默认当日） |
| GET | `/reports/daily/herbs` | Doctor, Admin, SuperAdmin | 药材使用排行（支持时间范围，默认当日） |

> **公共查询参数**：三个端点均接受可选 `startDate` / `endDate`（ISO 日期，如 `2026-06-01`），缺省默认当日，向后兼容。区间为闭区间，`startDate > endDate` 返回 400。

---

## GET /reports/daily/income

查询收入汇总（支持时间范围，默认当日），包括挂号费、药费和总收入。

- **权限**: Doctor / Admin / SuperAdmin
- **查询参数**:

| 参数 | 类型 | 必填 | 默认 | 说明 |
|------|------|:---:|------|------|
| `startDate` | string (ISO date) | 否 | 当日 | 起始日期（含），如 `2026-06-01` |
| `endDate` | string (ISO date) | 否 | 当日 | 结束日期（含），如 `2026-06-30` |

> 仅传其一时，另一参数默认等于所传值（单日查询）；均不传则查询当日。

**成功响应** (200) `ApiResponse<DailyIncomeDto>` — `data`:

```json
{
  "totalIncome": 3580.00,
  "registrationFeeTotal": 1200.00,
  "medicineFeeTotal": 2380.00
}
```

**响应字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalIncome` | decimal | 总收入 (挂号费 + 药费) |
| `registrationFeeTotal` | decimal | 挂号费合计 |
| `medicineFeeTotal` | decimal | 药费合计 |

> 字段语义与历史一致，区别仅在统计区间由查询参数决定（默认当日）。

**curl 示例：**

```bash
# 当日（默认）
curl -X GET http://localhost:5000/api/v1/reports/daily/income \
  -H "Authorization: Bearer $TOKEN"

# 月度对账（2026 年 6 月）
curl -X GET "http://localhost:5000/api/v1/reports/daily/income?startDate=2026-06-01&endDate=2026-06-30" \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /reports/daily/consultations

查询问诊统计（支持时间范围，默认当日），包括总问诊数和按医生分组的统计。

- **权限**: Doctor / Admin / SuperAdmin
- **查询参数**: 同 [`/reports/daily/income`](#get-reportsdailyincome)（`startDate` / `endDate`，可选，默认当日）

**成功响应** (200) `ApiResponse<DailyConsultationDto>` — `data`:

```json
{
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
}
```

**响应字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalCount` | int | 问诊总数 |
| `byDoctor` | `DoctorCountDto[]` | 按医生分组的问诊统计 |
| `byDoctor[].doctorName` | string | 医生姓名 |
| `byDoctor[].count` | int | 该医生问诊数 |

**curl 示例：**

```bash
# 月度统计（2026 年 6 月）
curl -X GET "http://localhost:5000/api/v1/reports/daily/consultations?startDate=2026-06-01&endDate=2026-06-30" \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /reports/daily/herbs

查询药材使用排行（支持时间范围，默认当日），按使用次数降序排列。

- **权限**: Doctor / Admin / SuperAdmin
- **查询参数**: 同 [`/reports/daily/income`](#get-reportsdailyincome)（`startDate` / `endDate`，可选，默认当日）

**成功响应** (200) `ApiResponse<DailyHerbUsageDto>` — `data`:

```json
{
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
# 月度排行（2026 年 6 月）
curl -X GET "http://localhost:5000/api/v1/reports/daily/herbs?startDate=2026-06-01&endDate=2026-06-30" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 错误码

| HTTP 状态码 | 说明 | 场景 |
|------------|------|------|
| 400 | 参数错误 | `startDate > endDate` 或日期格式非法 |
| 500 | 服务器错误 | 内部异常 |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v1.1 | 3 端点加时间范围查询参数（`startDate`/`endDate`，默认当日）；概述更新；补 US-REPORT-001~003；错误码加 400 |
| 2026-06-25 | v1.0 | 初始版本，包含 3 个当日统计端点 |
| 2026-06-28 | v1.0 | 概述补报表清单待专项讨论说明（A7：日营业额/就诊量/热门药材/医生工作量等 US 待补） | spec S7 弱反映项补全 |
| 2026-06-28 | v1.2 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；通用状态码引用 README |
