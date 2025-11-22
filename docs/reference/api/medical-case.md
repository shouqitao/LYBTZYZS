# 病历管理API参考文档

> **技术参考**: 面向开发者，提供准确的病历管理API接口文档
> **版本**: v1.0
> **基础URL**: `/api/v1/medical-cases`

## 📋 病历管理核心API

### 基础操作接口

#### 创建病历

```http
POST /api/v1/medical-cases
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "doctorId": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
  "consultationDate": "2024-01-01T14:30:00Z",
  "remark": "患者主诉头痛，需要进一步诊断"
}
```

**成功响应 (201)**:
```json
{
  "success": true,
  "data": {
    "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientName": "张三",
    "doctorId": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
    "doctorName": "李医生",
    "consultationDate": "2024-01-01T14:30:00Z",
    "status": 1,
    "statusText": "进行中",
    "needsPrescription": null,
    "remark": "患者主诉头痛，需要进一步诊断",
    "createdAt": "2024-01-01T14:30:00Z",
    "updatedAt": "2024-01-01T14:30:00Z",
    "consultation": {
      "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
      "chiefComplaint": "",
      "presentIllness": "",
      "inspection": "",
      "auscultation": "",
      "olfaction": "",
      "inquiry": "",
      "pulse": "",
      "tongue": "",
      "tcmDiagnosis": "",
      "syndromeDifferentiation": "",
      "treatmentPlan": "",
      "step1CompletedAt": null,
      "step2CompletedAt": null,
      "step3CompletedAt": null
    },
    "prescription": null
  }
}
```

**错误响应**:
```json
// 400 - 业务规则验证失败
{
  "success": false,
  "error": "PATIENT_HAS_ACTIVE_CASE",
  "message": "该患者已有进行中的病历，请先完成现有病历"
}

// 404 - 患者不存在
{
  "success": false,
  "error": "PATIENT_NOT_FOUND",
  "message": "指定的患者不存在"
}
```

#### 获取病历详情

```http
GET /api/v1/medical-cases/{id}
Authorization: Bearer {token}
```

**路径参数**:
- `id` (string, required): 病历唯一标识符

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "patientName": "张三",
    "doctorId": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
    "doctorName": "李医生",
    "consultationDate": "2024-01-01T14:30:00Z",
    "status": 1,
    "statusText": "进行中",
    "needsPrescription": true,
    "remark": "患者主诉头痛，需要进一步诊断",
    "createdAt": "2024-01-01T14:30:00Z",
    "updatedAt": "2024-01-01T15:45:00Z",
    "consultation": {
      "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
      "chiefComplaint": "头痛3天，加重1天",
      "presentIllness": "患者3天前无明显诱因出现头痛，呈搏动性，以双侧颞部为主，伴随恶心呕吐...",
      "inspection": "神志清楚，精神尚可，面色正常",
      "auscultation": "语声正常，无异常呼吸音",
      "olfaction": "无异常气味",
      "inquiry": "头痛呈搏动性，劳累后加重，休息后稍缓解，伴随畏光、声音过敏...",
      "pulse": "弦数",
      "tongue": "舌质淡红，苔薄白",
      "tcmDiagnosis": "头痛（肝阳上亢型）",
      "syndromeDifferentiation": "肝阳上亢，清窍失养",
      "treatmentPlan": "平肝潜阳，活血止痛",
      "step1CompletedAt": "2024-01-01T15:00:00Z",
      "step2CompletedAt": "2024-01-01T15:30:00Z",
      "step3CompletedAt": null
    },
    "prescription": {
      "id": "6gd85f64-5717-4562-b3fc-2c963f66afa6",
      "diagnosis": "头痛（肝阳上亢型）",
      "instructions": "每日1剂，水煎分两次温服",
      "status": 1,
      "statusText": "草稿",
      "isPrinted": false,
      "createdAt": "2024-01-01T15:35:00Z",
      "updatedAt": "2024-01-01T15:45:00Z",
      "details": [
        {
          "id": "7he85f64-5717-4562-b3fc-2c963f66afa6",
          "herbId": "8if85f64-5717-4562-b3fc-2c963f66afa6",
          "herbName": "天麻",
          "dosage": 15.0,
          "unit": "g",
          "quantity": 7,
          "instructions": "先煎"
        },
        {
          "id": "9jg85f64-5717-4562-b3fc-2c963f66afa6",
          "herbId": "0kh85f64-5717-4562-b3fc-2c963f66afa6",
          "herbName": "钩藤",
          "dosage": 12.0,
          "unit": "g",
          "quantity": 7,
          "instructions": "后下"
        }
      ]
    }
  }
}
```

### 辨证信息管理接口

#### 更新辨证信息 (Step 1)

```http
PUT /api/v1/medical-cases/{id}/consultation
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "chiefComplaint": "头痛3天，加重1天",
  "presentIllness": "患者3天前无明显诱因出现头痛，呈搏动性，以双侧颞部为主，伴随恶心呕吐。头痛于下午和夜间加重，休息后稍有缓解。无发热、无肢体麻木无力。",
  "inspection": "神志清楚，精神尚可，面色正常，形体适中。",
  "auscultation": "语声正常，呼吸平稳，无异常呼吸音。",
  "olfaction": "无异常气味。",
  "inquiry": "头痛呈搏动性，劳累后加重，休息后稍缓解，伴随畏光、声音过敏。食欲尚可，睡眠欠佳，二便正常。",
  "pulse": "弦数，尺脉沉细。",
  "tongue": "舌质淡红，苔薄白，边有齿痕。",
  "tcmDiagnosis": "头痛（肝阳上亢型）",
  "syndromeDifferentiation": "肝阳上亢，清窍失养。患者情志不畅，肝气郁结，郁久化火，上扰清窍而致头痛。",
  "treatmentPlan": "平肝潜阳，活血止痛。治以天麻钩藤饮加减。"
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "consultation": {
      "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
      "step1CompletedAt": "2024-01-01T15:00:00Z",
      "updatedAt": "2024-01-01T15:00:00Z"
      // ... 其他辨证信息字段
    }
  }
}
```

#### 设置处方需求 (Step 2)

```http
PUT /api/v1/medical-cases/{id}/prescription-need
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "needsPrescription": true,
  "reason": "患者需要中药调理治疗"
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "needsPrescription": true,
    "updatedAt": "2024-01-01T15:30:00Z",
    "consultation": {
      "step2CompletedAt": "2024-01-01T15:30:00Z",
      "prescriptionEnabled": true
    }
  }
}
```

### 处方管理接口

#### 创建处方 (Step 3a)

```http
POST /api/v1/medical-cases/{id}/prescriptions
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "diagnosis": "头痛（肝阳上亢型）",
  "instructions": "每日1剂，水煎分两次温服。忌食辛辣刺激性食物。",
  "details": [
    {
      "herbId": "8if85f64-5717-4562-b3fc-2c963f66afa6",
      "dosage": 15.0,
      "unit": "g",
      "quantity": 7,
      "instructions": "先煎30分钟"
    },
    {
      "herbId": "0kh85f64-5717-4562-b3fc-2c963f66afa6",
      "dosage": 12.0,
      "unit": "g",
      "quantity": 7,
      "instructions": "后下，煎煮5分钟即可"
    },
    {
      "herbId": "1li85f64-5717-4562-b3fc-2c963f66afa6",
      "dosage": 10.0,
      "unit": "g",
      "quantity": 7,
      "instructions": ""
    },
    {
      "herbId": "2mj85f64-5717-4562-b3fc-2c963f66afa6",
      "dosage": 9.0,
      "unit": "g",
      "quantity": 7,
      "instructions": ""
    }
  ]
}
```

**成功响应 (201)**:
```json
{
  "success": true,
  "data": {
    "id": "6gd85f64-5717-4562-b3fc-2c963f66afa6",
    "medicalCaseId": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": 1,
    "statusText": "草稿",
    "isPrinted": false,
    "createdAt": "2024-01-01T15:35:00Z",
    "details": [
      {
        "id": "7he85f64-5717-4562-b3fc-2c963f66afa6",
        "prescriptionId": "6gd85f64-5717-4562-b3fc-2c963f66afa6",
        "herbId": "8if85f64-5717-4562-b3fc-2c963f66afa6",
        "herbName": "天麻",
        "dosage": 15.0,
        "unit": "g",
        "quantity": 7,
        "instructions": "先煎30分钟"
      }
      // ... 其他药品明细
    ]
  }
}
```

#### 更新处方

```http
PUT /api/v1/medical-cases/{medicalCaseId}/prescriptions/{prescriptionId}
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "diagnosis": "头痛（肝阳上亢型）",
  "instructions": "每日1剂，水煎分两次温服。注意休息，避免劳累。",
  "status": 2,
  "details": [
    {
      "id": "7he85f64-5717-4562-b3fc-2c963f66afa6",
      "dosage": 18.0,
      "unit": "g",
      "quantity": 7,
      "instructions": "先煎30分钟"
    }
  ]
}
```

#### 打印处方

```http
POST /api/v1/medical-cases/{medicalCaseId}/prescriptions/{prescriptionId}/print
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "printOptions": {
    "includePrice": true,
    "includeInstructions": true,
    "copies": 2
  }
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "prescriptionId": "6gd85f64-5717-4562-b3fc-2c963f66afa6",
    "isPrinted": true,
    "printedAt": "2024-01-01T16:00:00Z",
    "printedBy": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
    "printUrl": "/api/v1/downloads/prescription-6gd85f64-5717-4562-b3fc-2c963f66afa6.pdf"
  }
}
```

### 病历状态管理接口

#### 完成病历

```http
POST /api/v1/medical-cases/{id}/complete
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "completionNote": "三步流程已完成，患者症状缓解，建议继续观察"
}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
    "status": 2,
    "statusText": "已完成",
    "completedAt": "2024-01-01T16:30:00Z",
    "consultation": {
      "step3CompletedAt": "2024-01-01T16:30:00Z"
    }
  }
}
```

#### 取消病历

```http
POST /api/v1/medical-cases/{id}/cancel
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "reason": "患者要求取消治疗",
  "notes": "患者个人原因，无法继续治疗"
}
```

### 查询和搜索接口

#### 病历列表查询

```http
GET /api/v1/medical-cases?patientId={patientId}&doctorId={doctorId}&status={status}&startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

**查询参数**:
- `patientId` (string, optional): 患者ID过滤
- `doctorId` (string, optional): 医生ID过滤
- `status` (int, optional): 状态过滤 (1=进行中, 2=已完成, 3=已取消)
- `startDate` (string, optional): 开始日期 (yyyy-MM-dd)
- `endDate` (string, optional): 结束日期 (yyyy-MM-dd)
- `keyword` (string, optional): 关键词搜索（患者姓名、诊断等）
- `pageIndex` (int, optional): 页码，默认1
- `pageSize` (int, optional): 页大小，默认20

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "5fc85f64-5717-4562-b3fc-2c963f66afa6",
        "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "patientName": "张三",
        "doctorId": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
        "doctorName": "李医生",
        "consultationDate": "2024-01-01T14:30:00Z",
        "status": 2,
        "statusText": "已完成",
        "needsPrescription": true,
        "chiefComplaint": "头痛3天，加重1天",
        "tcmDiagnosis": "头痛（肝阳上亢型）",
        "createdAt": "2024-01-01T14:30:00Z",
        "completedAt": "2024-01-01T16:30:00Z",
        "hasPrescription": true,
        "isPrinted": true
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

#### 患者病历查询

```http
GET /api/v1/patients/{patientId}/medical-cases?status={status}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

#### 医生工作量查询

```http
GET /api/v1/doctors/{doctorId}/medical-cases?startDate={startDate}&endDate={endDate}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

### 模板管理接口

#### 获取辨证模板列表

```http
GET /api/v1/medical-cases/consultation-templates?keyword={keyword}&pageIndex={pageIndex}&pageSize={pageSize}
Authorization: Bearer {token}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "8no85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "头痛-肝阳上亢型模板",
        "description": "适用于肝阳上亢型头痛患者的标准辨证模板",
        "chiefComplaint": "头痛",
        "tcmDiagnosis": "头痛（肝阳上亢型）",
        "syndromeDifferentiation": "肝阳上亢，清窍失养",
        "treatmentPlan": "平肝潜阳，活血止痛",
        "useCount": 25,
        "createdAt": "2024-01-01T10:00:00Z"
      }
    ]
  }
}
```

#### 应用辨证模板

```http
POST /api/v1/medical-cases/{id}/apply-consultation-template
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "templateId": "8no85f64-5717-4562-b3fc-2c963f66afa6",
  "overrideExisting": false
}
```

#### 保存辨证模板

```http
POST /api/v1/medical-cases/consultation-templates
Content-Type: application/json
Authorization: Bearer {token}
```

**请求体**:
```json
{
  "name": "失眠-心脾两虚型模板",
  "description": "适用于心脾两虚型失眠患者的标准辨证模板",
  "chiefComplaint": "失眠多梦",
  "presentIllness": "患者入睡困难，多梦易醒，伴有心悸、健忘、食欲不振等症状。",
  "inspection": "面色萎黄，精神疲惫",
  "auscultation": "语声低微",
  "olfaction": "无异常气味",
  "inquiry": "失眠多梦，心悸健忘，食欲不振，神疲乏力，舌淡脉细。",
  "pulse": "细弱",
  "tongue": "舌质淡，苔薄白",
  "tcmDiagnosis": "失眠（心脾两虚型）",
  "syndromeDifferentiation": "心脾两虚，心神失养",
  "treatmentPlan": "健脾养心，安神定志"
}
```

### 数据分析和统计接口

#### 病历统计查询

```http
GET /api/v1/medical-cases/analytics?startDate={startDate}&endDate={endDate}&groupBy={groupBy}
Authorization: Bearer {token}
```

**查询参数**:
- `startDate` (string, required): 开始日期
- `endDate` (string, required): 结束日期
- `groupBy` (string, optional): 分组方式 (day/week/month/doctor/diagnosis)

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "summary": {
      "totalCases": 156,
      "completedCases": 142,
      "cancelledCases": 14,
      "casesWithPrescription": 128,
      "averageDuration": 7.5,
      "completionRate": 0.91
    },
    "trends": [
      {
        "date": "2024-01-01",
        "totalCases": 12,
        "completedCases": 11,
        "topDiagnoses": [
          {
            "diagnosis": "头痛（肝阳上亢型）",
            "count": 3
          },
          {
            "diagnosis": "失眠（心脾两虚型）",
            "count": 2
          }
        ]
      }
    ],
    "diagnosisStatistics": [
      {
        "diagnosis": "头痛（肝阳上亢型）",
        "count": 28,
        "percentage": 0.179
      }
    ],
    "doctorStatistics": [
      {
        "doctorId": "4fb85f64-5717-4562-b3fc-2c963f66afa6",
        "doctorName": "李医生",
        "totalCases": 45,
        "completedCases": 42,
        "averageDuration": 6.8
      }
    ]
  }
}
```

### 权限验证接口

#### 检查编辑权限

```http
GET /api/v1/medical-cases/{id}/can-edit
Authorization: Bearer {token}
```

**成功响应 (200)**:
```json
{
  "success": true,
  "data": {
    "canEdit": true,
    "reason": null,
    "permissionLevel": "Creator",
    "editRestrictions": {
      "canEditConsultation": true,
      "canEditPrescription": true,
      "canChangeStatus": true,
      "canDelete": false
    }
  }
}
```

**不可编辑响应**:
```json
{
  "success": true,
  "data": {
    "canEdit": false,
    "reason": "病历创建于2024-01-01，仅当天可编辑",
    "permissionLevel": "None",
    "editRestrictions": {
      "canEditConsultation": false,
      "canEditPrescription": false,
      "canChangeStatus": false,
      "canDelete": false
    },
    "additionalInfo": {
      "createdDate": "2024-01-01",
      "currentDate": "2024-01-02",
      "editRule": "当天可改原则"
    }
  }
}
```

## 📊 数据模型定义

### MedicalCaseDto (病历数据传输对象)

```csharp
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; }
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; }
    public DateTime ConsultationDate { get; set; }

    [JsonPropertyName("status")]
    public MedicalCaseStatus Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; }

    public bool? NeedsPrescription { get; set; }
    public string Remark { get; set; }

    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // 关联数据
    [JsonPropertyName("consultation")]
    public ConsultationDto Consultation { get; set; }

    [JsonPropertyName("prescription")]
    public PrescriptionDto Prescription { get; set; }

    // 状态标志
    [JsonPropertyName("hasConsultation")]
    public bool HasConsultation => Consultation != null;

    [JsonPropertyName("hasPrescription")]
    public bool HasPrescription => Prescription != null;

    [JsonPropertyName("isPrinted")]
    public bool IsPrinted => Prescription?.IsPrinted ?? false;
}
```

### ConsultationDto (辨证信息数据传输对象)

```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "主诉不能为空")]
    [StringLength(200, ErrorMessage = "主诉长度不能超过200字符")]
    public string ChiefComplaint { get; set; }

    [StringLength(1000, ErrorMessage = "现病史长度不能超过1000字符")]
    public string PresentIllness { get; set; }

    public string Inspection { get; set; }
    public string Auscultation { get; set; }
    public string Olfaction { get; set; }
    public string Inquiry { get; set; }
    public string Pulse { get; set; }
    public string Tongue { get; set; }

    [Required(ErrorMessage = "中医诊断不能为空")]
    [StringLength(500, ErrorMessage = "中医诊断长度不能超过500字符")]
    public string TcmDiagnosis { get; set; }

    [StringLength(1000, ErrorMessage = "辨证分析长度不能超过1000字符")]
    public string SyndromeDifferentiation { get; set; }

    [StringLength(1000, ErrorMessage = "治疗方案长度不能超过1000字符")]
    public string TreatmentPlan { get; set; }

    // 三步流程时间戳
    public DateTime? Step1CompletedAt { get; set; }
    public DateTime? Step2CompletedAt { get; set; }
    public DateTime? Step3CompletedAt { get; set; }

    // 状态标志
    public bool PrescriptionEnabled { get; set; }

    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### PrescriptionDto (处方数据传输对象)

```csharp
public class PrescriptionDto
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid PatientId { get; set; }
    public Guid UserId { get; set; }

    [JsonPropertyName("status")]
    public PrescriptionStatus Status { get; set; }

    [JsonPropertyName("statusText")]
    public string StatusText { get; set; }

    public string Diagnosis { get; set; }
    public string Instructions { get; set; }

    public bool IsPrinted { get; set; }
    public DateTime? PrescribedAt { get; set; }
    public DateTime? PrintedAt { get; set; }

    // 处方明细
    public List<PrescriptionDetailDto> Details { get; set; }

    // 统计信息
    public int TotalHerbs => Details?.Count ?? 0;
    public decimal TotalDosage => Details?.Sum(d => d.Dosage) ?? 0;

    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 枚举定义

```csharp
public enum MedicalCaseStatus
{
    Active = 1,      // 进行中
    Completed = 2,   // 已完成
    Cancelled = 3    // 已取消
}

public enum PrescriptionStatus
{
    Draft = 1,       // 草稿
    Confirmed = 2,   // 已确认
    Printed = 3,     // 已打印
    Dispensed = 4,   // 已配药
    Completed = 5,   // 已完成
    Cancelled = 6    // 已取消
}
```

## 🔐 错误代码定义

### 业务错误 (4xx)

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `MEDICAL_CASE_NOT_FOUND` | 404 | 病历不存在 | 检查病历ID是否正确 |
| `PATIENT_HAS_ACTIVE_CASE` | 400 | 患者已有进行中病历 | 先完成现有病历 |
| `STEP1_NOT_COMPLETED` | 400 | Step 1未完成 | 先完成辨证信息填写 |
| `STEP2_NOT_COMPLETED` | 400 | Step 2未完成 | 先设置处方需求标记 |
| `PRESCRIPTION_NOT_NEEDED` | 400 | 未标记需要开处方 | 标记需要开处方后重试 |
| `PRESCRIPTION_ALREADY_EXISTS` | 400 | 处方已存在 | 使用更新接口而非创建 |
| `PRESCRIPTION_ALREADY_PRINTED` | 400 | 处方已打印 | 申请修改权限或创建新处方 |
| `EDIT_PERMISSION_DENIED` | 403 | 无编辑权限 | 检查权限规则和时间限制 |
| `INVALID_STATUS_TRANSITION` | 400 | 非法状态流转 | 检查状态流转规则 |
| `CONSULTATION_REQUIRED` | 400 | 辨证信息缺失 | 先完成Step 1 |

### 验证错误 (4xx)

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `VALIDATION_ERROR` | 400 | 数据验证失败 | 检查请求参数格式 |
| `CHIEF_COMPLAINT_REQUIRED` | 400 | 主诉不能为空 | 填写主诉信息 |
| `TCM_DIAGNOSIS_REQUIRED` | 400 | 中医诊断不能为空 | 填写中医诊断 |
| `DOSAGE_OUT_OF_RANGE` | 400 | 药品剂量超出安全范围 | 调整药品剂量 |
| `HERB_INCOMPATIBILITY` | 400 | 药品配伍禁忌 | 调整药品配伍 |
| `PRESCRIPTION_EMPTY` | 400 | 处方无药品明细 | 添加药品到处方 |

### 系统错误 (5xx)

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `DATABASE_ERROR` | 500 | 数据库操作错误 | 联系系统管理员 |
| `INTEGRITY_CHECK_FAILED` | 500 | 数据完整性检查失败 | 运行数据修复工具 |
| `PERMISSION_CHECK_FAILED` | 500 | 权限检查失败 | 联系系统管理员 |
| `TEMPLATE_APPLY_FAILED` | 500 | 模板应用失败 | 检查模板数据完整性 |

## ⚡ 性能参数

### 响应时间要求

| 接口类型 | 响应时间要求 | 优化策略 |
|---------|-------------|----------|
| 病历详情查询 | < 300ms | 缓存优化、索引优化 |
| 病历列表查询 | < 500ms | 分页优化、查询优化 |
| 辨证信息更新 | < 200ms | 事务优化、异步处理 |
| 处方创建 | < 400ms | 批量处理、缓存 |
| 模板应用 | < 100ms | 本地缓存、预编译 |
| 统计查询 | < 2秒 | 预计算、缓存策略 |

### 并发限制

| 接口类型 | 并发限制 | 策略 |
|---------|---------|------|
| 查询接口 | 200/秒/IP | 缓存、限流 |
| 更新接口 | 50/秒/用户 | 乐观锁、重试 |
| 创建接口 | 30/秒/用户 | 队列、限流 |
| 打印接口 | 10/分钟/用户 | 异步处理 |
| 统计接口 | 20/分钟 | 预计算、缓存 |

### 数据传输优化

- **默认页大小**: 20
- **最大页大小**: 100
- **字段选择**: 支持指定返回字段
- **数据压缩**: 大数据集启用gzip压缩
- **缓存策略**: 分层缓存（本地 + 分布式）

## 🔒 安全特性

### 权限控制

| 用户角色 | 查看权限 | 编辑权限 | 删除权限 | 打印权限 |
|---------|---------|---------|---------|----------|
| 管理员 | 完整信息 | 完整权限 | 完整权限 | 完整权限 |
| 医生 | 自己病历 | 当天病历 | 自己病历 | 自己病历 |
| 护士 | 基础信息 | 无权限 | 无权限 | 查看权限 |
| 其他 | 无权限 | 无权限 | 无权限 | 无权限 |

### 编辑权限规则

```csharp
public class EditPermissionRules
{
    // 规则1: 管理员可以编辑所有病历
    public bool CanEditAsAdmin => true;

    // 规则2: 创建者当天可编辑
    public bool CanEditAsCreator(MedicalCase medicalCase, Guid currentUserId)
    {
        return medicalCase.DoctorId == currentUserId &&
               medicalCase.CreatedAt.Date == DateTime.Today;
    }

    // 规则3: 仅Active状态可编辑
    public bool CanEditByStatus(MedicalCaseStatus status)
    {
        return status == MedicalCaseStatus.Active;
    }

    // 规则4: 已打印处方需要特殊权限
    public bool CanEditPrintedPrescription(bool isAdmin)
    {
        return isAdmin; // 只有管理员可以编辑已打印的处方
    }
}
```

### 数据脱敏

| 字段 | 脱敏规则 | 示例 |
|------|----------|------|
| 患者姓名 | 根据用户角色决定 | 张三 (医生) / *** (其他) |
| 医生信息 | 非本人不可见 | 李医生 (本人) / *** (其他) |
| 详细诊断 | 医护人员可见 | 完整诊断 (医护) / 简化 (其他) |

### 审计日志

```csharp
public class MedicalCaseAuditLog
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public AuditAction Action { get; set; }
    public string ActionDescription { get; set; }
    public object OldValues { get; set; }
    public object NewValues { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 📋 SDK使用示例

### C# SDK

```csharp
// 初始化客户端
var medicalCaseClient = new MedicalCaseClient(new HttpClient()
{
    BaseAddress = new Uri("https://api.example.com"),
    DefaultRequestHeaders = { {"Authorization", "Bearer " + token}}
});

// 创建病历
var createDto = new MedicalCaseCreateDto
{
    PatientId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    ConsultationDate = DateTime.Now,
    Remark = "新患者首次就诊"
};

var medicalCase = await medicalCaseClient.CreateAsync(createDto);
Console.WriteLine($"病历创建成功: {medicalCase.Data.Id}");

// 更新辨证信息
var consultationDto = new ConsultationInputDto
{
    ChiefComplaint = "头痛3天",
    TcmDiagnosis = "头痛（肝阳上亢型）",
    TreatmentPlan = "平肝潜阳，活血止痛"
};

var updatedCase = await medicalCaseClient.UpdateConsultationAsync(
    medicalCase.Data.Id,
    consultationDto);

// 创建处方
var prescriptionDto = new PrescriptionCreateDto
{
    Diagnosis = "头痛（肝阳上亢型）",
    Instructions = "每日1剂，水煎分两次温服",
    Details = new List<PrescriptionDetailCreateDto>
    {
        new() { HerbId = herbId, Dosage = 15m, Unit = "g", Quantity = 7 }
    }
};

var prescription = await medicalCaseClient.CreatePrescriptionAsync(
    medicalCase.Data.Id,
    prescriptionDto);
```

### JavaScript SDK

```javascript
// 初始化客户端
const medicalCaseClient = new MedicalCaseClient({
    baseURL: 'https://api.example.com',
    token: 'your-jwt-token'
});

// 创建病历
const createMedicalCase = async () => {
    try {
        const result = await medicalCaseClient.create({
            patientId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
            consultationDate: new Date().toISOString(),
            remark: '新患者首次就诊'
        });

        console.log('病历创建成功:', result.data.id);
        return result.data;
    } catch (error) {
        console.error('创建失败:', error.message);
    }
};

// 三步流程操作
const threeStepProcess = async (medicalCaseId) => {
    try {
        // Step 1: 更新辨证信息
        await medicalCaseClient.updateConsultation(medicalCaseId, {
            chiefComplaint: '头痛3天',
            tcmDiagnosis: '头痛（肝阳上亢型）',
            treatmentPlan: '平肝潜阳，活血止痛'
        });

        // Step 2: 设置处方需求
        await medicalCaseClient.setPrescriptionNeed(medicalCaseId, {
            needsPrescription: true
        });

        // Step 3: 创建处方
        const prescription = await medicalCaseClient.createPrescription(medicalCaseId, {
            diagnosis: '头痛（肝阳上亢型）',
            instructions: '每日1剂，水煎分两次温服',
            details: [
                {
                    herbId: 'herb-123',
                    dosage: 15,
                    unit: 'g',
                    quantity: 7
                }
            ]
        });

        // 完成病历
        await medicalCaseClient.complete(medicalCaseId, {
            completionNote: '三步流程已完成'
        });

        console.log('病历处理完成');
    } catch (error) {
        console.error('处理失败:', error.message);
    }
};
```

---

**文档类型**: API Reference
**版本**: v1.0
**更新时间**: 2025-11-22
**相关资源**: [病历管理教程](../../tutorials/modules/medicalcase/medical-case-management-tutorial.md) | [问题解决指南](../../how-to-guides/modules/medicalcase/medical-case-management-issues.md)