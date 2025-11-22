# 患者管理API参考文档

> **技术参考**: 面向开发者，提供准确的患者管理API接口文档
> **版本**: v1.0
> **基础URL**: `/api/v1/patients`

## 🏥 患者管理核心API

### 基础操作接口

#### 创建患者

```http
POST /api/v1/patients
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "name": "张三",
  "gender": 1,
  "birthDate": "1990-01-01",
  "idNumber": "110101199001011234",
  "phoneNumber": "13800138000",
  "address": "北京市朝阳区某某街道123号",
  "bloodType": "A",
  "email": "zhangsan@example.com",
  "emergencyContact": {
    "name": "李四",
    "relationship": "配偶",
    "phoneNumber": "13900139000"
  },
  "medicalHistory": {
    "hasAllergies": true,
    "allergies": ["青霉素", "海鲜"],
    "hasChronicDiseases": true,
    "chronicDiseases": ["高血压", "糖尿病"],
    "hasSurgicalHistory": false,
    "surgicalHistory": [],
    "familyHistory": {
      "hasHeredityDiseases": true,
      "heredityDiseases": ["心脏病", "高血压"]
    }
  }
}
```

**成功响应 (201)**:
```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "张三",
    "pinYinCode": "zs",
    "gender": 1,
    "genderText": "男",
    "birthDate": "1990-01-01T00:00:00Z",
    "age": 33,
    "idNumber": "110101********1234",
    "phoneNumber": "138****8000",
    "address": "北京市朝阳区********",
    "bloodType": "A",
    "bloodTypeText": "A型",
    "email": "zhangsan@example.com",
    "status": 1,
    "statusText": "正常",
    "registrationDate": "2024-01-01T10:00:00Z",
    "lastVisitDate": null,
    "emergencyContact": {
      "name": "李四",
      "relationship": "配偶",
      "phoneNumber": "139****9000"
    },
    "medicalHistory": {
      "hasAllergies": true,
      "allergies": ["青霉素", "海鲜"],
      "hasChronicDiseases": true,
      "chronicDiseases": ["高血压", "糖尿病"],
      "hasSurgicalHistory": false,
      "surgicalHistory": [],
      "familyHistory": {
        "hasHeredityDiseases": true,
        "heredityDiseases": ["心脏病", "高血压"]
      }
    },
    "createdAt": "2024-01-01T10:00:00Z",
    "updatedAt": "2024-01-01T10:00:00Z"
  }
}
```

**错误响应**:
```json
// 400 - 数据验证失败
{
  "success": false,
  "errors": [
    {
      "field": "idNumber",
      "message": "身份证号格式不正确"
    },
    {
      "field": "phoneNumber",
      "message": "手机号格式不正确"
    }
  ]
}

// 409 - 患者已存在
{
  "success": false,
  "error": "PATIENT_EXISTS",
  "message": "该身份证号的患者已存在"
}
```

#### 获取患者详情

```http
GET /api/v1/patients/{id}
Authorization: Bearer {token}
```

**路径参数**:
- `id` (string, required): 患者唯一标识符

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "张三",
    "pinYinCode": "zs",
    "pinYinFull": "zhangsan",
    "gender": 1,
    "genderText": "男",
    "birthDate": "1990-01-01T00:00:00Z",
    "age": 33,
    "idNumber": "110101********1234",
    "phoneNumber": "138****8000",
    "address": "北京市朝阳区********",
    "bloodType": 1,
    "bloodTypeText": "A型",
    "email": "zhangsan@example.com",
    "status": 1,
    "statusText": "正常",
    "registrationDate": "2024-01-01T10:00:00Z",
    "lastVisitDate": "2024-01-15T14:30:00Z",
    "totalVisits": 5,
    "emergencyContact": {
      "name": "李四",
      "relationship": "配偶",
      "phoneNumber": "139****9000"
    },
    "medicalHistory": {
      "hasAllergies": true,
      "allergies": ["青霉素", "海鲜"],
      "hasChronicDiseases": true,
      "chronicDiseases": ["高血压", "糖尿病"],
      "hasSurgicalHistory": false,
      "surgicalHistory": [],
      "familyHistory": {
        "hasHeredityDiseases": true,
        "heredityDiseases": ["心脏病", "高血压"]
      }
    },
    "createdAt": "2024-01-01T10:00:00Z",
    "updatedAt": "2024-01-15T14:30:00Z"
  }
}
```

**错误响应**:
```json
// 404 - 患者不存在
{
  "success": false,
  "error": "PATIENT_NOT_FOUND",
  "message": "指定的患者不存在"
}
```

#### 更新患者信息

```http
PUT /api/v1/patients/{id}
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "name": "张三丰",
  "phoneNumber": "13800138001",
  "address": "北京市朝阳区某某街道456号",
  "email": "zhangsanfeng@example.com",
  "emergencyContact": {
    "name": "李四",
    "relationship": "配偶",
    "phoneNumber": "13900139001"
  },
  "medicalHistory": {
    "hasAllergies": true,
    "allergies": ["青霉素", "海鲜", "花粉"],
    "hasChronicDiseases": true,
    "chronicDiseases": ["高血压", "糖尿病", "高血脂"],
    "hasSurgicalHistory": true,
    "surgicalHistory": [
      {
        "type": "阑尾炎手术",
        "date": "2015-06-15",
        "hospital": "北京协和医院",
        "description": "腹腔镜阑尾切除术"
      }
    ]
  }
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    // 返回更新后的完整患者信息
  }
}
```

#### 删除患者

```http
DELETE /api/v1/patients/{id}
Authorization: Bearer {token}
```

**成功响应 (204)**: 无内容

**错误响应**:
```json
// 400 - 患者有关联记录无法删除
{
  "success": false,
  "error": "PATIENT_HAS_RECORDS",
  "message": "该患者有关联的医疗记录，无法删除"
}
```

### 搜索和查询接口

#### 患者搜索

```http
GET /api/v1/patients/search?keyword={keyword}&gender={gender}&minAge={minAge}&maxAge={maxAge}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

**查询参数**:
- `keyword` (string, optional): 搜索关键词（支持姓名、拼音码、身份证号、手机号）
- `gender` (int, optional): 性别过滤 (0=未知, 1=男, 2=女)
- `minAge` (int, optional): 最小年龄
- `maxAge` (int, optional): 最大年龄
- `status` (int, optional): 状态过滤 (1=正常, 2=停诊, 3=已故)
- `startDate` (string, optional): 注册开始日期 (yyyy-MM-dd)
- `endDate` (string, optional): 注册结束日期 (yyyy-MM-dd)
- `pageIndex` (int, optional): 页码，默认1
- `pageSize` (int, optional): 页大小，默认20

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "张三",
        "pinYinCode": "zs",
        "gender": 1,
        "genderText": "男",
        "age": 33,
        "idNumber": "110101********1234",
        "phoneNumber": "138****8000",
        "status": 1,
        "statusText": "正常",
        "registrationDate": "2024-01-01T10:00:00Z",
        "lastVisitDate": "2024-01-15T14:30:00Z",
        "totalVisits": 5
      }
    ],
    "pagination": {
      "pageIndex": 1,
      "pageSize": 20,
      "totalCount": 1,
      "totalPages": 1,
      "hasNextPage": false,
      "hasPreviousPage": false
    }
  }
}
```

#### 高级搜索

```http
POST /api/v1/patients/advanced-search
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "filters": {
    "name": "张",
    "gender": [1],
    "ageRange": {
      "min": 20,
      "max": 50
    },
    "bloodType": [1, 2],
    "registrationDateRange": {
      "start": "2024-01-01",
      "end": "2024-12-31"
    },
    "hasAllergies": true,
    "hasChronicDiseases": true,
    "lastVisitDateRange": {
      "start": "2024-01-01"
    }
  },
  "sort": {
    "field": "lastVisitDate",
    "direction": "desc"
  },
  "pagination": {
    "pageIndex": 1,
    "pageSize": 50
  }
}
```

### 批量操作接口

#### 批量导入患者

```http
POST /api/v1/patients/batch-import
Content-Type: multipart/form-data
Authorization: Bearer {token}
```

**表单参数**:
- `file` (file, required): Excel文件 (.xlsx格式)
- `allowDuplicate` (boolean, optional): 是否允许重复导入，默认false
- `validateOnly` (boolean, optional): 仅验证不导入，默认false

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "importId": "batch-import-20240101-001",
    "status": "completed",
    "totalCount": 100,
    "successCount": 95,
    "failureCount": 5,
    "duplicateCount": 3,
    "results": [
      {
        "row": 2,
        "status": "success",
        "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "message": "导入成功"
      },
      {
        "row": 3,
        "status": "error",
        "field": "idNumber",
        "message": "身份证号格式错误"
      }
    ]
  }
}
```

#### 批量导出患者

```http
POST /api/v1/patients/batch-export
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "filters": {
    "registrationDateRange": {
      "start": "2024-01-01",
      "end": "2024-12-31"
    },
    "status": [1]
  },
  "includeSensitiveData": false,
  "fields": [
    "name",
    "gender",
    "birthDate",
    "age",
    "bloodType",
    "registrationDate",
    "lastVisitDate"
  ]
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "exportId": "batch-export-20240101-001",
    "status": "completed",
    "downloadUrl": "/api/v1/downloads/patients-export-20240101-001.xlsx",
    "fileName": "患者数据导出_20240101.xlsx",
    "fileSize": 2048576,
    "expiresAt": "2024-01-02T10:00:00Z"
  }
}
```

#### 批量更新状态

```http
PATCH /api/v1/patients/batch-update-status
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "patientIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "4fb85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "status": 2,
  "reason": "患者主动要求停诊"
}
```

### 患者关联接口

#### 获取患者医疗记录

```http
GET /api/v1/patients/{id}/medical-records?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "patientInfo": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "张三",
      "age": 33
    },
    "medicalRecords": [
      {
        "id": "record-001",
        "visitDate": "2024-01-15T14:30:00Z",
        "diagnosis": "上呼吸道感染",
        "treatment": "开具感冒药，建议休息",
        "doctorName": "李医生",
        "department": "内科"
      }
    ],
    "pagination": {
      "pageIndex": 1,
      "pageSize": 20,
      "totalCount": 5,
      "totalPages": 1
    }
  }
}
```

#### 获取患者处方记录

```http
GET /api/v1/patients/{id}/prescriptions?startDate={startDate}&endDate={endDate}&status={status}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

#### 获取患者诊断记录

```http
GET /api/v1/patients/{id}/consultations?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

## 📊 数据模型定义

### PatientDto (患者数据传输对象)

```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string PinYinCode { get; set; }
    public string PinYinFull { get; set; }

    [JsonPropertyName("gender")]
    public Gender Gender { get; set; }

    [JsonPropertyName("genderText")]
    public string GenderText { get; set; }

    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }

    [JsonPropertyName("idNumber")]
    public string IdNumber { get; set; } // 脱敏显示

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } // 脱敏显示

    public string Address { get; set; } // 脱敏显示
    public string Email { get; set; }

    [JsonPropertyName("bloodType")]
    public BloodType BloodType { get; set; }

    [JsonPropertyName("bloodTypeText")]
    public string BloodTypeText { get; set; }

    [JsonPropertyName("status")]
    public PatientStatus Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; }

    public DateTime RegistrationDate { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int TotalVisits { get; set; }

    [JsonPropertyName("emergencyContact")]
    public EmergencyContactDto EmergencyContact { get; set; }

    [JsonPropertyName("medicalHistory")]
    public MedicalHistoryDto MedicalHistory { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### PatientCreateDto (创建患者请求)

```csharp
public class PatientCreateDto
{
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "出生日期不能为空")]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "身份证号不能为空")]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$",
                     ErrorMessage = "身份证号格式不正确")]
    public string IdNumber { get; set; }

    [Phone(ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; }

    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string Address { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; }

    public BloodType? BloodType { get; set; }

    public EmergencyContactCreateDto EmergencyContact { get; set; }
    public MedicalHistoryCreateDto MedicalHistory { get; set; }
}
```

### 枚举定义

```csharp
public enum Gender
{
    Unknown = 0, // 未知
    Male = 1,    // 男
    Female = 2   // 女
}

public enum BloodType
{
    Unknown = 0, // 未知
    A = 1,       // A型
    B = 2,       // B型
    AB = 3,      // AB型
    O = 4        // O型
}

public enum PatientStatus
{
    Normal = 1,    // 正常
    Suspended = 2, // 停诊
    Deceased = 3   // 已故
}
```

## 🔐 错误代码定义

### 客户端错误 (4xx)

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `PATIENT_NOT_FOUND` | 404 | 患者不存在 | 检查患者ID是否正确 |
| `PATIENT_EXISTS` | 409 | 患者已存在 | 检查身份证号或手机号是否重复 |
| `INVALID_ID_NUMBER` | 400 | 身份证号格式错误 | 验证身份证号格式 |
| `INVALID_PHONE_NUMBER` | 400 | 手机号格式错误 | 验证手机号格式 |
| `PATIENT_HAS_RECORDS` | 400 | 患者有关联记录无法删除 | 先处理关联的医疗记录 |
| `VALIDATION_ERROR` | 400 | 数据验证失败 | 检查请求数据格式 |
| `UNAUTHORIZED_ACCESS` | 401 | 未授权访问 | 检查访问权限 |

### 服务器错误 (5xx)

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `DATABASE_ERROR` | 500 | 数据库操作错误 | 联系系统管理员 |
| `FILE_PROCESSING_ERROR` | 500 | 文件处理错误 | 检查文件格式和大小 |
| `SEARCH_INDEX_ERROR` | 500 | 搜索索引错误 | 联系系统管理员 |
| `DATA_ENCRYPTION_ERROR` | 500 | 数据加密错误 | 联系系统管理员 |

## ⚡ 性能参数

### 响应时间要求

| 接口类型 | 响应时间要求 | 优化策略 |
|---------|-------------|----------|
| 患者详情查询 | < 200ms | 数据库索引优化 |
| 患者搜索 | < 500ms | 拼音码索引、缓存 |
| 批量导入 | < 30秒/1000条 | 批量处理、事务优化 |
| 批量导出 | < 60秒/10000条 | 流式导出、分页处理 |
| 数据验证 | < 100ms | 本地验证、异步处理 |

### 并发限制

| 接口类型 | 并发限制 | 策略 |
|---------|---------|------|
| 搜索接口 | 100/秒/IP | 限流、缓存 |
| 导入接口 | 10/分钟/用户 | 队列处理 |
| 导出接口 | 5/分钟/用户 | 异步生成 |
| 修改接口 | 50/秒/用户 | 乐观锁、重试 |

### 分页参数

- **默认页大小**: 20
- **最大页大小**: 100
- **页码范围**: 1-1000

## 🔒 安全特性

### 数据脱敏规则

| 字段 | 脱敏规则 | 示例 |
|------|----------|------|
| 身份证号 | 保留前6后4位 | 110101********1234 |
| 手机号 | 保留前3后4位 | 138****8000 |
| 地址 | 保留前6后6位 | 北京市朝阳区******** |
| 姓名 | 根据角色决定是否显示 | ***(非医生角色) |

### 访问控制

| 用户角色 | 查看权限 | 修改权限 | 导出权限 |
|---------|---------|---------|---------|
| 管理员 | 完整信息 | 完整权限 | 完整数据 |
| 医生 | 脱敏信息 | 基础信息 | 脱敏数据 |
| 护士 | 脱敏信息 | 基础信息 | 脱敏数据 |
| 其他 | 高度脱敏 | 无权限 | 无权限 |

### API限流

| 限流类型 | 限制 | 时间窗口 |
|---------|------|----------|
| 搜索请求 | 100次/分钟 | 滑动窗口 |
| 修改请求 | 50次/分钟 | 滑动窗口 |
| 导入请求 | 5次/小时 | 固定窗口 |
| 导出请求 | 10次/小时 | 固定窗口 |

## 📋 SDK使用示例

### C# SDK

```csharp
// 初始化客户端
var patientClient = new PatientClient(new HttpClient()
{
    BaseAddress = new Uri("https://api.example.com"),
    DefaultRequestHeaders = { {"Authorization", "Bearer " + token}}
});

// 创建患者
var createDto = new PatientCreateDto
{
    Name = "张三",
    Gender = Gender.Male,
    BirthDate = new DateTime(1990, 1, 1),
    IdNumber = "110101199001011234",
    PhoneNumber = "13800138000"
};

var createResult = await patientClient.CreateAsync(createDto);
if (createResult.Success)
{
    Console.WriteLine($"患者创建成功，ID: {createResult.Data.Id}");
}

// 搜索患者
var searchDto = new PatientSearchDto
{
    Keyword = "张",
    PageIndex = 1,
    PageSize = 20
};

var searchResult = await patientClient.SearchAsync(searchDto);
foreach (var patient in searchResult.Data.Items)
{
    Console.WriteLine($"{patient.Name} - {patient.Age}岁");
}

// 批量导入
using var fileStream = File.OpenRead("patients.xlsx");
var importResult = await patientClient.BatchImportAsync(fileStream);
Console.WriteLine($"导入完成: 成功{importResult.Data.SuccessCount}条，失败{importResult.Data.FailureCount}条");
```

### JavaScript SDK

```javascript
// 初始化客户端
const patientClient = new PatientClient({
    baseURL: 'https://api.example.com',
    token: 'your-jwt-token'
});

// 创建患者
const createPatient = async () => {
    try {
        const result = await patientClient.create({
            name: '张三',
            gender: 1,
            birthDate: '1990-01-01',
            idNumber: '110101199001011234',
            phoneNumber: '13800138000'
        });

        console.log('患者创建成功:', result.data.id);
    } catch (error) {
        console.error('创建失败:', error.message);
    }
};

// 搜索患者
const searchPatients = async () => {
    try {
        const result = await patientClient.search({
            keyword: '张',
            pageIndex: 1,
            pageSize: 20
        });

        result.data.items.forEach(patient => {
            console.log(`${patient.name} - ${patient.age}岁`);
        });
    } catch (error) {
        console.error('搜索失败:', error.message);
    }
};
```

---

**文档类型**: API Reference
**版本**: v1.0
**更新时间**: 2025-11-22
**相关资源**: [患者管理教程](../tutorials/modules/patients/patient-management-tutorial.md) | [问题解决指南](../how-to-guides/modules/patients/patient-data-management-issues.md)