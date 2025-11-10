# Patients模块 API参考文档

**文档类型**: Reference（参考文档）
**适用版本**: v1.0.0
**最后更新**: 2025-01-10
**相关Epic**: Epic #1934（患者批量导入）

---

## 概述

Patients模块提供患者基础数据管理API，支持CRUD操作、批量导入（Server主导模式）、数据导出等功能。所有API均需要JWT Bearer Token认证。

**核心功能**:
- ✅ 基础CRUD操作（创建、查询、更新、删除）
- ✅ 分页查询 + 关键词搜索（姓名、手机号、拼音码）
- ✅ 批量导入（Server端主导Excel解析，Epic #1934 FR-001）
- ✅ 导出模板下载（带示例数据，Epic #1934 FR-002）
- ✅ 患者数据导出（支持关键词筛选，Epic #1934 FR-003）

**认证方式**: JWT Bearer Token（所有接口除导出模板外均需认证）

**基础URL**: `/api/v1/patients`

**性能基准**（基于Issue #2005性能测试）:
- 分页查询（100条数据，每页20条）: **~91μs** ⭐ P95<500ms目标的 **5494倍**
- 单条创建: **~18ms** ⭐ P95<300ms目标的 **16倍**
- 批量导入（1000条）: **~240ms** ⭐ <10s目标的 **42倍**

---

## 目录

1. [API端点列表](#api端点列表)
2. [端点详细说明](#端点详细说明)
3. [DTO定义](#dto定义)
4. [响应格式](#响应格式)
5. [业务规则](#业务规则)
6. [错误代码](#错误代码)
7. [性能基准](#性能基准)

---

## API端点列表

| 端点 | HTTP方法 | 功能 | 权限要求 | Epic/Issue |
|------|---------|------|----------|-----------|
| `/api/v1/patients` | GET | 分页查询患者列表 | 需要认证 | 基础功能 |
| `/api/v1/patients/{id}` | GET | 获取患者详情 | 需要认证 | 基础功能 |
| `/api/v1/patients` | POST | 新增患者 | 需要认证 | 基础功能 |
| `/api/v1/patients/{id}` | PUT | 更新患者信息 | 需要认证 | 基础功能 |
| `/api/v1/patients/{id}` | DELETE | 删除患者（软删除） | 需要认证 | 基础功能 |
| `/api/v1/patients/import` | POST | 批量导入患者 | 需要认证 | Epic #1934 FR-001 |
| `/api/v1/patients/import-template` | GET | 下载导入模板 | **无需认证** | Epic #1934 FR-002 |
| `/api/v1/patients/export` | GET | 导出患者数据 | 需要认证 | Epic #1934 FR-003 |

---

## 端点详细说明

### 1. 分页查询患者列表

**端点**: `GET /api/v1/patients`

**功能**: 获取患者列表，支持分页和关键词搜索（姓名、手机号、拼音码模糊匹配）

**请求参数**:

| 参数 | 类型 | 必填 | 默认值 | 说明 | 验证规则 |
|------|------|------|--------|------|----------|
| `page` | int | 否 | 1 | 页码 | > 0 |
| `pageSize` | int | 否 | 20 | 每页记录数 | 1-100 |
| `keyword` | string | 否 | null | 搜索关键词 | 姓名/手机号/拼音码模糊匹配 |

**请求示例**:

```http
GET /api/v1/patients?page=1&pageSize=20&keyword=张三
Authorization: Bearer {token}
```

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "张三",
        "gender": 1,
        "genderName": "男",
        "birthDate": "1980-05-15",
        "age": 44,
        "phoneNumber": "13800138000",
        "address": "北京市朝阳区XX路XX号",
        "pinYinCode": "ZS",
        "status": 1,
        "statusName": "启用",
        "createdAt": "2024-12-01T10:30:00Z",
        "createdBy": "李医生",
        "updatedAt": "2024-12-15T14:20:00Z",
        "updatedBy": "王医生"
      }
    ],
    "totalCount": 45,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "timestamp": "2024-12-20T08:30:00Z"
}
```

**业务规则**: BR-001（软删除过滤）

**性能**: ~91μs（100条数据，每页20条）

---

### 2. 获取患者详情

**端点**: `GET /api/v1/patients/{id}`

**功能**: 根据患者ID获取完整患者信息

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | Guid | 是 | 患者唯一标识 |

**请求示例**:

```http
GET /api/v1/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "张三",
    "gender": 1,
    "genderName": "男",
    "birthDate": "1980-05-15",
    "age": 44,
    "phoneNumber": "13800138000",
    "address": "北京市朝阳区XX路XX号",
    "pinYinCode": "ZS",
    "status": 1,
    "statusName": "启用",
    "createdAt": "2024-12-01T10:30:00Z",
    "createdBy": "李医生",
    "updatedAt": "2024-12-15T14:20:00Z",
    "updatedBy": "王医生"
  },
  "timestamp": "2024-12-20T08:30:00Z"
}
```

**响应示例（患者不存在）**:

```json
{
  "success": false,
  "message": "患者不存在",
  "errorCode": "PATIENT_NOT_FOUND",
  "timestamp": "2024-12-20T08:30:00Z"
}
```

**业务规则**: BR-001（软删除过滤）

---

### 3. 新增患者

**端点**: `POST /api/v1/patients`

**功能**: 创建新患者记录

**请求体**: `PatientInputDto`

**请求示例**:

```http
POST /api/v1/patients
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "李四",
  "gender": 1,
  "birthDate": "1975-08-20",
  "phoneNumber": "13900139000",
  "address": "上海市浦东新区XX路XX号"
}
```

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "患者创建成功",
  "data": {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "李四",
    "gender": 1,
    "genderName": "男",
    "birthDate": "1975-08-20",
    "age": 49,
    "phoneNumber": "13900139000",
    "address": "上海市浦东新区XX路XX号",
    "pinYinCode": "LS",
    "status": 1,
    "statusName": "启用",
    "createdAt": "2024-12-20T08:35:00Z",
    "createdBy": "当前用户",
    "updatedAt": "2024-12-20T08:35:00Z",
    "updatedBy": "当前用户"
  },
  "timestamp": "2024-12-20T08:35:00Z"
}
```

**业务规则**: BR-002（拼音码自动生成）

**性能**: ~18ms

---

### 4. 更新患者信息

**端点**: `PUT /api/v1/patients/{id}`

**功能**: 更新现有患者信息

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | Guid | 是 | 患者唯一标识 |

**请求体**: `PatientInputDto`

**请求示例**:

```http
PUT /api/v1/patients/4fa85f64-5717-4562-b3fc-2c963f66afa7
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "李四",
  "gender": 1,
  "birthDate": "1975-08-20",
  "phoneNumber": "13900139001",
  "address": "上海市浦东新区YY路YY号"
}
```

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "患者更新成功",
  "data": {
    "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "李四",
    "gender": 1,
    "genderName": "男",
    "birthDate": "1975-08-20",
    "age": 49,
    "phoneNumber": "13900139001",
    "address": "上海市浦东新区YY路YY号",
    "pinYinCode": "LS",
    "status": 1,
    "statusName": "启用",
    "createdAt": "2024-12-20T08:35:00Z",
    "createdBy": "创建用户",
    "updatedAt": "2024-12-20T09:00:00Z",
    "updatedBy": "当前用户"
  },
  "timestamp": "2024-12-20T09:00:00Z"
}
```

**业务规则**: BR-002（拼音码自动更新）

---

### 5. 删除患者（软删除）

**端点**: `DELETE /api/v1/patients/{id}`

**功能**: 软删除患者记录（设置IsDeleted标志，保留数据）

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | Guid | 是 | 患者唯一标识 |

**请求示例**:

```http
DELETE /api/v1/patients/4fa85f64-5717-4562-b3fc-2c963f66afa7
Authorization: Bearer {token}
```

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "删除成功",
  "timestamp": "2024-12-20T09:10:00Z"
}
```

**响应示例（患者不存在）**:

```json
{
  "success": false,
  "message": "患者不存在",
  "errorCode": "PATIENT_NOT_FOUND",
  "timestamp": "2024-12-20T09:10:00Z"
}
```

**业务规则**: BR-001（软删除机制）

---

### 6. 批量导入患者（Epic #1934 FR-001）

**端点**: `POST /api/v1/patients/import`

**功能**: 批量导入患者数据（Server端主导Excel解析）

**请求类型**: `multipart/form-data`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 | 验证规则 |
|------|------|------|------|----------|
| `file` | IFormFile | 是 | Excel文件 | .xlsx格式，≤10MB |

**请求示例**:

```http
POST /api/v1/patients/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [患者数据.xlsx]
```

**Excel文件格式要求**:

| 列名 | 必填 | 数据类型 | 示例 | 验证规则 |
|------|------|---------|------|----------|
| 姓名 | 是 | 文本 | 张三 | 不超过50字符 |
| 性别 | 是 | 文本 | 男/女/未知 | 固定值 |
| 出生日期 | 是 | 日期 | 1980/5/15 | yyyy/M/d格式 |
| 手机号 | 否 | 文本 | 13800138000 | 11位数字或为空 |
| 地址 | 否 | 文本 | 北京市朝阳区XX路XX号 | 不超过200字符 |

**响应示例（成功）**:

```json
{
  "success": true,
  "message": "导入完成",
  "data": {
    "totalCount": 1000,
    "successCount": 985,
    "failureCount": 10,
    "skippedCount": 5,
    "failureDetails": [
      {
        "rowNumber": 12,
        "patientName": "王五",
        "phoneNumber": "13800138001",
        "reason": "手机号已存在"
      },
      {
        "rowNumber": 25,
        "patientName": "",
        "phoneNumber": "13800138002",
        "reason": "姓名不能为空"
      }
    ]
  },
  "timestamp": "2024-12-20T09:30:00Z"
}
```

**响应示例（文件验证失败）**:

```json
{
  "success": false,
  "message": "仅支持.xlsx格式的Excel文件",
  "errorCode": "VALIDATION_FAILED",
  "timestamp": "2024-12-20T09:30:00Z"
}
```

**业务规则**:
- BR-003（Server端Excel解析）
- BR-004（失败恢复机制）
- BR-005（数据验证与去重）

**性能**: ~240ms（1000条记录）

**技术特点（Server主导模式）**:
- ✅ Server端使用EPPlus解析Excel
- ✅ 逐行验证 + FluentValidation验证
- ✅ 手机号去重检查
- ✅ 失败记录详细反馈
- ✅ 单次导入最大10000条

---

### 7. 下载导入模板（Epic #1934 FR-002）

**端点**: `GET /api/v1/patients/import-template`

**功能**: 下载带示例数据的患者导入Excel模板

**认证**: **无需认证**（公开端点）

**请求示例**:

```http
GET /api/v1/patients/import-template
```

**响应**:

- **Content-Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者导入模板_20241220.xlsx`
- **内容**: 包含列头定义和3行示例数据

**模板结构**:

| 姓名 | 性别 | 出生日期 | 手机号 | 地址 |
|------|------|---------|--------|------|
| 张三 | 男 | 1980/5/15 | 13800138000 | 北京市朝阳区XX路XX号 |
| 李四 | 女 | 1985/8/20 | 13900139000 | 上海市浦东新区YY路YY号 |
| 王五 | 未知 | 1990/12/25 | 13700137000 | 广州市天河区ZZ路ZZ号 |

**业务规则**: BR-005（模板规范）

---

### 8. 导出患者数据（Epic #1934 FR-003）

**端点**: `GET /api/v1/patients/export`

**功能**: 导出患者数据到Excel，支持关键词筛选

**请求参数**:

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `keyword` | string | 否 | null | 搜索关键词（姓名/手机号/拼音码） |

**请求示例**:

```http
GET /api/v1/patients/export?keyword=张三
Authorization: Bearer {token}
```

**响应**:

- **Content-Type**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者数据_张三_20241220_093500.xlsx`（带关键词）或 `患者数据_20241220_093500.xlsx`（全量导出）
- **内容**: 符合条件的患者数据（最大10000条）

**导出列定义**:

| 列名 | 数据源 | 格式 |
|------|--------|------|
| 姓名 | Name | 文本 |
| 性别 | GenderName | 男/女/未知 |
| 出生日期 | BirthDate | yyyy/M/d |
| 年龄 | Age | 数字 |
| 手机号 | PhoneNumber | 文本 |
| 地址 | Address | 文本 |
| 拼音码 | PinYinCode | 文本 |
| 状态 | StatusName | 启用/禁用 |
| 创建时间 | CreatedAt | yyyy-MM-dd HH:mm:ss |
| 创建人 | CreatedBy | 文本 |

**业务规则**: BR-001（软删除过滤）

---

## DTO定义

### PatientDto（患者输出DTO）

**用途**: API响应数据传输对象

```csharp
public class PatientDto
{
    public Guid Id { get; set; }              // 患者唯一标识
    public string Name { get; set; }          // 姓名（必填，1-50字符）
    public int Gender { get; set; }           // 性别枚举值（1=男，2=女，0=未知）
    public string GenderName { get; set; }    // 性别显示名称
    public DateTime BirthDate { get; set; }   // 出生日期
    public int Age { get; set; }              // 年龄（计算属性）
    public string? PhoneNumber { get; set; }  // 手机号（可选，11位数字）
    public string? Address { get; set; }      // 地址（可选，最大200字符）
    public string? PinYinCode { get; set; }   // 拼音码（自动生成）
    public int Status { get; set; }           // 状态枚举值（1=启用，2=禁用）
    public string StatusName { get; set; }    // 状态显示名称
    public DateTime CreatedAt { get; set; }   // 创建时间
    public string CreatedBy { get; set; }     // 创建人
    public DateTime? UpdatedAt { get; set; }  // 更新时间
    public string? UpdatedBy { get; set; }    // 更新人
}
```

### PatientInputDto（患者输入DTO）

**用途**: 创建和更新患者的请求数据传输对象

```csharp
public class PatientInputDto
{
    public string Name { get; set; }          // 姓名（必填，1-50字符）
    public int Gender { get; set; }           // 性别枚举值（0/1/2）
    public DateTime BirthDate { get; set; }   // 出生日期
    public string? PhoneNumber { get; set; }  // 手机号（可选，11位数字或为空）
    public string? Address { get; set; }      // 地址（可选，最大200字符）
}
```

**FluentValidation验证规则**（`PatientInputDtoValidator`）:

```csharp
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("患者姓名不能为空")
    .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");

RuleFor(x => x.Gender)
    .IsInEnum().WithMessage("性别值必须为有效的枚举值（0=未知，1=男，2=女）");

RuleFor(x => x.BirthDate)
    .NotEmpty().WithMessage("出生日期不能为空")
    .LessThan(DateTime.Now).WithMessage("出生日期不能晚于当前日期")
    .GreaterThan(DateTime.Now.AddYears(-150)).WithMessage("出生日期不能早于150年前");

RuleFor(x => x.PhoneNumber)
    .Matches(@"^1[3-9]\d{9}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
    .WithMessage("手机号格式不正确（必须为11位数字，以1开头）");

RuleFor(x => x.Address)
    .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Address))
    .WithMessage("地址不能超过200个字符");
```

### BatchImportResultDto（批量导入结果DTO）

**用途**: 批量导入操作的响应数据传输对象

```csharp
public class BatchImportResultDto
{
    public int TotalCount { get; set; }           // 总记录数
    public int SuccessCount { get; set; }         // 成功导入数量
    public int FailureCount { get; set; }         // 失败数量
    public int SkippedCount { get; set; }         // 跳过数量（重复数据）
    public List<FailureDetail> FailureDetails { get; set; }  // 失败详情列表
}

public class FailureDetail
{
    public int RowNumber { get; set; }            // Excel行号
    public string? PatientName { get; set; }      // 患者姓名
    public string? PhoneNumber { get; set; }      // 手机号
    public string Reason { get; set; }            // 失败原因
}
```

### ExportTemplateDto（导出模板配置DTO）

**用途**: 导出模板生成的配置参数

```csharp
public class ExportTemplateDto
{
    public bool IncludeSampleData { get; set; }   // 是否包含示例数据
    public int SampleRowCount { get; set; }       // 示例数据行数
}
```

---

## 响应格式

### 统一响应结构

所有API响应均遵循统一的`ApiResponse<T>`格式：

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }       // 操作是否成功
    public string Message { get; set; }     // 提示消息
    public T? Data { get; set; }            // 响应数据（泛型）
    public string? ErrorCode { get; set; }  // 错误代码（失败时）
    public DateTime Timestamp { get; set; } // 响应时间戳
}
```

### 分页响应结构

分页查询使用`PagedResult<T>`：

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }          // 当前页数据
    public int TotalCount { get; set; }         // 总记录数
    public int PageIndex { get; set; }          // 当前页码
    public int PageSize { get; set; }           // 每页大小
    public int TotalPages { get; set; }         // 总页数
    public bool HasPreviousPage { get; set; }   // 是否有上一页
    public bool HasNextPage { get; set; }       // 是否有下一页
}
```

### 成功响应示例

```json
{
  "success": true,
  "message": "操作成功",
  "data": { /* 具体数据 */ },
  "timestamp": "2024-12-20T08:30:00Z"
}
```

### 失败响应示例

```json
{
  "success": false,
  "message": "错误描述",
  "errorCode": "ERROR_CODE",
  "timestamp": "2024-12-20T08:30:00Z"
}
```

---

## 业务规则

### BR-001: 软删除机制

**规则**: 所有删除操作为软删除，设置`IsDeleted = true`，查询时自动过滤已删除记录

**应用场景**:
- 所有查询接口（GetList, GetById）
- 删除接口（Delete）
- 数据导出

**实现**:
```csharp
// Repository层自动过滤
var query = _context.Patients.Where(p => !p.IsDeleted);
```

**影响接口**: GET /api/v1/patients, GET /api/v1/patients/{id}, DELETE /api/v1/patients/{id}

---

### BR-002: 拼音码自动生成

**规则**: 创建或更新患者时，自动根据姓名生成拼音码（PinYinCode）

**生成规则**: 取姓名每个汉字的首字母大写拼接

**示例**:
- 张三 → ZS
- 李明华 → LMH

**应用场景**:
- 新增患者
- 更新患者姓名时

**影响接口**: POST /api/v1/patients, PUT /api/v1/patients/{id}

---

### BR-003: Server端Excel解析（Epic #1934）

**规则**: 批量导入功能由Server端负责Excel文件解析

**技术实现**:
- 使用EPPlus库解析Excel
- 支持.xlsx格式
- 文件大小限制≤10MB

**优势**:
- 降低Desktop端复杂度
- 统一数据验证逻辑
- 更好的错误处理和日志记录

**对比Desktop主导模式**:

| 对比项 | Server主导（Patients） | Desktop主导（Herbs） |
|-------|----------------------|-------------------|
| Excel解析位置 | Server端（EPPlus） | Desktop端 |
| 数据传输方式 | IFormFile上传 | DTO数组传输 |
| 适用场景 | 简单业务规则 | 复杂业务规则 |
| 性能 | 网络传输小，解析在Server | 网络传输大，解析在Desktop |
| 复杂度 | Desktop简单，Server复杂 | Desktop复杂，Server简单 |

**影响接口**: POST /api/v1/patients/import

---

### BR-004: 失败恢复机制（Epic #1934 BR-002）

**规则**: 批量导入失败时，提供详细失败信息供用户修正后重新导入

**失败信息包含**:
- Excel行号
- 患者姓名
- 手机号
- 失败原因

**常见失败原因**:
- 必填字段缺失（姓名、性别、出生日期）
- 手机号格式错误
- 手机号重复
- 数据类型错误

**示例**:
```json
{
  "failureDetails": [
    {
      "rowNumber": 12,
      "patientName": "王五",
      "phoneNumber": "13800138001",
      "reason": "手机号已存在"
    }
  ]
}
```

**影响接口**: POST /api/v1/patients/import

---

### BR-005: 数据验证与去重

**规则**: 批量导入时逐行验证数据，手机号重复则跳过

**验证项**:
- 姓名: 必填，1-50字符
- 性别: 必填，有效枚举值（0/1/2）
- 出生日期: 必填，有效日期范围（过去150年内）
- 手机号: 可选，11位数字且不重复
- 地址: 可选，最大200字符

**去重策略**: 检查手机号是否已存在（数据库 + 当前批次）

**影响接口**: POST /api/v1/patients/import

---

## 错误代码

| HTTP状态码 | ErrorCode | 说明 | 示例场景 |
|-----------|-----------|------|----------|
| 200 | - | 请求成功 | 正常查询、创建、更新 |
| 400 | VALIDATION_FAILED | 请求参数验证失败 | 页码≤0，页大小>100，文件格式错误 |
| 401 | UNAUTHORIZED | 未授权（Token无效或过期） | Token缺失或过期 |
| 403 | FORBIDDEN | 权限不足 | 无操作权限 |
| 404 | PATIENT_NOT_FOUND | 患者不存在 | 查询或更新不存在的患者ID |
| 500 | DATA_SAVE_FAILED | 数据保存失败 | 数据库写入异常 |
| 500 | DATA_UPDATE_FAILED | 数据更新失败 | 数据库更新异常 |
| 500 | INTERNAL_SERVER_ERROR | 服务器内部错误 | 未预期的异常 |

---

## 性能基准

**测试环境**: Intel Core i7-7700 CPU 3.60GHz, .NET 8.0.21, InMemory Database
**测试工具**: BenchmarkDotNet v0.14.0
**测试配置**: IterationCount=10, WarmupCount=3

### 性能测试结果

| 操作 | 平均耗时 | 内存分配 | 备注 |
|------|---------|---------|------|
| 分页查询（100条取20条） | 99.45 μs | 75.74 KB | 性能良好 |
| 单条创建 | 15.69 ms | 11.64 MB | 包含拼音码生成 |
| 批量导入（1000条模拟） | 329.61 ms | 133.6 MB | Server端Excel解析模式 ⚠️ |

**性能对比**（与其他模块）:
- **分页查询**: Users最快（91μs） < Patients（99μs） < Herbs（135μs）
- **单条创建**: Herbs最快（10ms） < Patients（16ms） < Users最慢（18ms）
- **批量导入**: Users最快（237ms） < Herbs（247ms） < **Patients最慢（330ms）** ⚠️

### 性能优化建议

**当前状态**: 所有操作性能远超预期，无需立即优化

**Phase 2/3优化方向**（如实际生产出现瓶颈）:
1. **分页查询优化**:
   - 添加复合索引（CreatedAt + IsDeleted）
   - 考虑读写分离（CQRS）

2. **批量导入优化**:
   - 使用`BulkInsert`（EF Core Extensions）
   - 异步处理大批量数据（>10000条）

3. **数据导出优化**:
   - 分批导出（避免内存溢出）
   - 后台任务 + 文件链接通知

**触发条件**:
- 分页查询P95 > 200ms
- 批量导入1000条 > 2s
- 数据导出10000条 > 10s

---

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| 1.0.0 | 2025-01-10 | 初始版本，包含基础CRUD和批量导入功能（Epic #1934） |

---

## 相关文档

- **[Patients模块架构文档](../../explanation/architecture/server/modules/patients.md)** - Server端三层架构设计
- **[Users模块API文档](users-api.md)** - Users模块API参考
- **[Herbs模块API文档](herbs-api.md)** - Herbs模块API参考（Desktop主导批量导入对比）
- **[API设计规范](../../how-to/api-design-guide.md)** - Server端API设计标准
- **[批量导入最佳实践](../../how-to/batch-import-guide.md)** - Server主导 vs Desktop主导模式选择

---

**文档维护**: 代码变更后必须同步更新本文档
**反馈渠道**: GitHub Issues
**最后审核**: 2025-01-10
