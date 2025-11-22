# 中医诊断 API 参考文档

## 概述

本文档提供LYBTZYZS系统中医诊断模块的完整API参考，包括诊断数据的创建、查询、更新、舌诊图像处理、脉诊数据分析、辨证分析等功能接口。

## 目录

- [核心API端点](#核心api端点)
- [数据模型](#数据模型)
- [舌诊图像API](#舌诊图像api)
- [脉诊分析API](#脉诊分析api)
- [辨证分析API](#辨证分析api)
- [诊断报告API](#诊断报告api)
- [权限和认证](#权限和认证)
- [错误代码](#错误代码)
- [速率限制](#速率限制)
- [SDK和示例](#sdk和示例)

---

## 核心API端点

### 1. 创建诊断记录

**端点**: `POST /api/consultation/diagnostic`

**描述**: 创建新的中医诊断记录

**请求头**:
```http
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:
```json
{
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "chiefComplaint": "头痛伴恶心呕吐3天",
  "presentIllness": "患者3天前无明显诱因出现头痛...",
  "inspection": "神志清楚，精神尚可，面色微黄",
  "auscultationOlfaction": "语音清晰，呼吸平稳，无异常气味",
  "inquiry": "头痛位于颞部，呈搏动性疼痛...",
  "palpation": "脉象弦滑，舌质淡红，苔薄白",
  "tcmDiagnosis": "肝阳上亢证",
  "treatmentPrinciple": "平肝潜阳，活血止痛"
}
```

**成功响应** (201 Created):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "doctorId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
  "chiefComplaint": "头痛伴恶心呕吐3天",
  "presentIllness": "患者3天前无明显诱因出现头痛...",
  "inspection": "神志清楚，精神尚可，面色微黄",
  "auscultationOlfaction": "语音清晰，呼吸平稳，无异常气味",
  "inquiry": "头痛位于颞部，呈搏动性疼痛...",
  "palpation": "脉象弦滑，舌质淡红，苔薄白",
  "tcmDiagnosis": "肝阳上亢证",
  "treatmentPrinciple": "平肝潜阳，活血止痛",
  "status": "Active",
  "createdAt": "2025-01-22T10:30:00Z",
  "lastModified": "2025-01-22T10:30:00Z"
}
```

**错误响应**:
- 400 Bad Request: 请求参数验证失败
- 401 Unauthorized: 未授权访问
- 403 Forbidden: 权限不足
- 404 Not Found: 患者或病历不存在

### 2. 查询诊断记录

**端点**: `GET /api/consultation/diagnostic/{id}`

**描述**: 根据ID获取诊断记录详情

**请求头**:
```http
Authorization: Bearer {token}
```

**路径参数**:
- `id` (Guid): 诊断记录ID

**成功响应** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "doctorId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
  "patient": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "张三",
    "age": 45,
    "gender": "Male"
  },
  "doctor": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
    "name": "李医生",
    "department": "中医内科"
  },
  "chiefComplaint": "头痛伴恶心呕吐3天",
  "presentIllness": "患者3天前无明显诱因出现头痛...",
  "inspection": "神志清楚，精神尚可，面色微黄",
  "auscultationOlfaction": "语音清晰，呼吸平稳，无异常气味",
  "inquiry": "头痛位于颞部，呈搏动性疼痛...",
  "palpation": "脉象弦滑，舌质淡红，苔薄白",
  "tcmDiagnosis": "肝阳上亢证",
  "treatmentPrinciple": "平肝潜阳，活血止痛",
  "status": "Active",
  "createdAt": "2025-01-22T10:30:00Z",
  "lastModified": "2025-01-22T10:30:00Z"
}
```

### 3. 更新诊断记录

**端点**: `PUT /api/consultation/diagnostic/{id}`

**描述**: 更新现有的诊断记录

**权限要求**: 仅限原诊断医师或上级医师在24小时内修改

**请求体**:
```json
{
  "chiefComplaint": "头痛伴恶心呕吐3天（更新）",
  "tcmDiagnosis": "肝阳上亢，痰浊阻络证",
  "treatmentPrinciple": "平肝潜阳，化痰通络"
}
```

### 4. 患者诊断历史查询

**端点**: `GET /api/consultation/diagnostic/patient/{patientId}`

**描述**: 获取患者的所有诊断历史记录

**查询参数**:
- `page` (int, default: 1): 页码
- `pageSize` (int, default: 20): 每页记录数
- `startDate` (string, format: yyyy-MM-dd): 开始日期
- `endDate` (string, format: yyyy-MM-dd): 结束日期
- `doctorId` (string, optional): 筛选特定医师的诊断

**成功响应**:
```json
{
  "totalCount": 15,
  "page": 1,
  "pageSize": 20,
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "doctorId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
      "chiefComplaint": "头痛伴恶心呕吐3天",
      "tcmDiagnosis": "肝阳上亢证",
      "createdAt": "2025-01-22T10:30:00Z"
    }
  ]
}
```

### 5. 诊断记录搜索

**端点**: `GET /api/consultation/diagnostic/search`

**描述**: 根据条件搜索诊断记录

**查询参数**:
- `keyword` (string): 搜索关键词（主诉、诊断等）
- `tcmDiagnosis` (string): 中医诊断
- `chiefComplaint` (string): 主诉
- `doctorId` (string): 医师ID
- `startDate` (string): 开始日期
- `endDate` (string): 结束日期
- `page` (int): 页码
- `pageSize` (int): 每页记录数

### 6. 删除诊断记录

**端点**: `DELETE /api/consultation/diagnostic/{id}`

**描述**: 删除诊断记录（仅限系统管理员）

**权限要求**: 系统管理员权限

---

## 舌诊图像API

### 7. 上传舌诊图像

**端点**: `POST /api/consultation/tongue-image`

**描述**: 上传和分析舌诊图像

**请求**: multipart/form-data
- `file`: 图像文件（支持JPG, PNG格式）
- `diagnosticId`: 诊断记录ID
- `imageType`: 图像类型（正脸、舌面、舌下等）

**成功响应**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afaa",
  "diagnosticId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "imageUrl": "/uploads/tongue-images/2025/01/22/3fa85f64-5717-4562-b3fc-2c963f66afaa.jpg",
  "imageType": "TongueSurface",
  "analysisResult": {
    "tongueColor": "淡红",
    "tongueShape": "正常",
    "coatingColor": "薄白",
    "coatingThickness": "薄",
    "sublingualVeins": "正常",
    "confidence": 0.85
  },
  "uploadedAt": "2025-01-22T10:35:00Z"
}
```

### 8. 舌诊图像分析

**端点**: `POST /api/consultation/tongue-image/{id}/analyze`

**描述**: 重新分析舌诊图像

**成功响应**:
```json
{
  "tongueBody": {
    "color": "淡红",
    "shape": "正常",
    "size": "适中",
    "mobility": "正常",
    "colorValues": {
      "hue": 15.2,
      "saturation": 0.65,
      "brightness": 0.72
    }
  },
  "tongueCoating": {
    "color": "薄白",
    "thickness": "薄",
    "distribution": "均匀",
    "dryness": "湿润"
  },
  "sublingualVeins": {
    "color": "淡青",
    "thickness": "正常",
    "curvature": "正常"
  },
  "overallAssessment": {
    "primaryPattern": "正常舌象",
    "secondaryPatterns": [],
    "confidence": 0.89,
    "qualityScore": 0.92
  }
}
```

### 9. 舌诊图像质量检查

**端点**: `POST /api/consultation/tongue-image/quality-check`

**描述**: 检查舌诊图像质量

**请求体**:
```json
{
  "imageUrl": "/uploads/tongue-images/2025/01/22/example.jpg"
}
```

**成功响应**:
```json
{
  "qualityScore": 0.78,
  "isAcceptable": true,
  "analysis": {
    "resolution": {
      "value": 1920,
      "status": "Good",
      "message": "分辨率满足要求"
    },
    "brightness": {
      "value": 0.68,
      "status": "Good",
      "message": "亮度适中"
    },
    "contrast": {
      "value": 0.45,
      "status": "Good",
      "message": "对比度良好"
    },
    "sharpness": {
      "value": 0.72,
      "status": "Good",
      "message": "图像清晰"
    },
    "colorAccuracy": {
      "value": 0.85,
      "status": "Good",
      "message": "色彩还原准确"
    }
  },
  "recommendations": [
    "图像质量良好，可以用于舌诊分析"
  ]
}
```

---

## 脉诊分析API

### 10. 脉诊数据采集

**端点**: `POST /api/consultation/pulse-data/collect`

**描述**: 采集和分析脉诊数据

**请求体**:
```json
{
  "diagnosticId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "deviceId": "pulse-device-001",
  "collectionDuration": 30,
  "positions": ["LeftCun", "LeftGuan", "LeftChi", "RightCun", "RightGuan", "RightChi"]
}
```

**成功响应**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afab",
  "diagnosticId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "pulseCharacteristics": {
    "rate": 72,
    "rhythm": "正常",
    "position": "中",
    "strength": "有力",
    "shape": "弦滑",
    "tension": 0.65,
    "length": "适中",
    "width": "正常"
  },
  "waveformData": {
    "samplingRate": 200,
    "duration": 30,
    "dataPoints": [0.1, 0.15, 0.2, ...],
    "averageAmplitude": 0.68,
    "peakFrequency": 1.2
  },
  "analysis": {
    "primaryPattern": "弦滑脉",
    "secondaryPatterns": ["正常脉"],
    "clinicalSignificance": "肝气郁结，痰湿内阻",
    "confidence": 0.82
  },
  "collectedAt": "2025-01-22T10:40:00Z"
}
```

### 11. 脉诊波形分析

**端点**: `POST /api/consultation/pulse-data/waveform-analysis`

**描述**: 分析脉诊波形数据

**请求体**:
```json
{
  "waveformData": [0.1, 0.15, 0.2, ...],
  "samplingRate": 200,
  "position": "LeftGuan"
}
```

**成功响应**:
```json
{
  "features": {
    "peakDetection": {
      "peaks": [
        {"index": 120, "amplitude": 0.85},
        {"index": 280, "amplitude": 0.78}
      ],
      "peakIntervals": [160]
    },
    "frequencyAnalysis": {
      "dominantFrequency": 1.2,
      "harmonics": [2.4, 3.6],
      "powerSpectrum": [0.1, 0.15, 0.2, ...]
    },
    "morphology": {
      "upstrokeTime": 0.08,
      "downstrokeTime": 0.12,
      "pulseWidth": 0.25,
      "dicroticNotch": true
    }
  },
  "classification": {
    "pulseShape": "弦滑",
    "confidence": 0.79,
    "probabilities": {
      "弦脉": 0.65,
      "滑脉": 0.58,
      "正常脉": 0.32,
      "细脉": 0.15
    }
  }
}
```

### 12. 脉诊历史对比

**端点**: `GET /api/consultation/pulse-data/patient/{patientId}/comparison`

**描述**: 对比患者不同时期的脉诊数据

**查询参数**:
- `baselineDate` (string): 基准日期
- `comparisonDate` (string): 对比日期
- `positions` (string): 脉位（多个用逗号分隔）

**成功响应**:
```json
{
  "baselineDate": "2025-01-15",
  "comparisonDate": "2025-01-22",
  "positionComparison": [
    {
      "position": "LeftGuan",
      "baseline": {
        "rate": 68,
        "strength": "有力",
        "shape": "弦脉"
      },
      "current": {
        "rate": 72,
        "strength": "有力",
        "shape": "弦滑"
      },
      "changes": {
        "rateChange": "+4",
        "strengthChange": "无变化",
        "shapeChange": "增加滑象"
      },
      "clinicalInterpretation": "脉象由弦脉转为弦滑脉，提示痰湿内阻加重"
    }
  ],
  "overallAssessment": {
    "improvement": false,
    "deterioration": true,
    "clinicalSignificance": "需要加强化痰祛湿治疗"
  }
}
```

---

## 辨证分析API

### 13. 八纲辨证分析

**端点**: `POST /api/consultation/syndrome-analysis/eight-principles`

**描述**: 进行八纲辨证分析

**请求体**:
```json
{
  "diagnosticId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "symptoms": ["头痛", "恶心", "呕吐", "口苦"],
  "tongueData": {
    "color": "淡红",
    "coating": "薄白",
    "shape": "正常"
  },
  "pulseData": {
    "rate": 72,
    "strength": "有力",
    "shape": "弦滑"
  }
}
```

**成功响应**:
```json
{
  "syndromeAnalysis": {
    "exteriorInterior": {
      "result": "里证",
      "confidence": 0.85,
      "evidence": {
        "symptoms": ["病程较长", "症状在内脏"],
        "tongue": "舌色淡红",
        "pulse": "脉象弦滑"
      }
    },
    "coldHeat": {
      "result": "热证",
      "confidence": 0.72,
      "evidence": {
        "symptoms": ["口苦", "头痛呈搏动性"],
        "tongue": "舌色正常",
        "pulse": "脉数有力"
      }
    },
    "deficiencyExcess": {
      "result": "实证",
      "confidence": 0.88,
      "evidence": {
        "symptoms": ["头痛剧烈", "呕吐"],
        "tongue": "舌苔薄白",
        "pulse": "脉象有力"
      }
    },
    "yinYang": {
      "result": "阳证",
      "confidence": 0.80,
      "evidence": "综合里热实证判断"
    }
  },
  "overallDiagnosis": "里实热证",
  "clinicalRecommendations": [
    "清热泻火",
    "平肝潜阳",
    "活血止痛"
  ]
}
```

### 14. 脏腑辨证分析

**端点**: `POST /api/consultation/syndrome-analysis/organ`

**描述**: 进行脏腑辨证分析

**成功响应**:
```json
{
  "organSyndromes": [
    {
      "organ": "肝",
      "syndromeType": "肝阳上亢证",
      "confidence": 0.86,
      "keySymptoms": ["头痛", "眩晕", "口苦"],
      "tongueSigns": ["舌质红", "苔薄黄"],
      "pulseSigns": ["脉弦数"],
      "pathogenesis": "情志不遂，肝气郁结，郁久化火，肝阳上亢"
    },
    {
      "organ": "胃",
      "syndromeType": "胃气上逆证",
      "confidence": 0.78,
      "keySymptoms": ["恶心", "呕吐"],
      "tongueSigns": ["舌苔薄白"],
      "pulseSigns": ["脉滑"],
      "pathogenesis": "肝气犯胃，胃失和降"
    }
  ],
  "primarySyndrome": {
    "organ": "肝",
    "syndromeType": "肝阳上亢证",
    "confidence": 0.86
  },
  "secondarySyndromes": [
    {
      "organ": "胃",
      "syndromeType": "胃气上逆证",
      "confidence": 0.78
    }
  ],
  "treatmentPrinciple": "平肝潜阳，和胃降逆"
}
```

### 15. 辨证历史趋势分析

**端点**: `GET /api/consultation/syndrome-analysis/patient/{patientId}/trends`

**描述**: 分析患者辨证历史趋势

**查询参数**:
- `months` (int, default: 12): 分析月数
- `syndromeType` (string): 辨证类型（eight-principles, organ）

**成功响应**:
```json
{
  "analysisPeriod": {
    "startDate": "2024-01-22",
    "endDate": "2025-01-22",
    "months": 12
  },
  "trendAnalysis": [
    {
      "period": "2024-01",
      "dominantSyndrome": "肝气郁结证",
      "severity": 0.6,
      "treatmentResponse": "良好"
    },
    {
      "period": "2024-04",
      "dominantSyndrome": "肝郁化火证",
      "severity": 0.7,
      "treatmentResponse": "一般"
    },
    {
      "period": "2025-01",
      "dominantSyndrome": "肝阳上亢证",
      "severity": 0.8,
      "treatmentResponse": "需要调整"
    }
  ],
  "patterns": [
    {
      "pattern": "证候由气转火，由火转阳",
      "frequency": 3,
      "clinicalSignificance": "病情进展，需要加强治疗"
    }
  ],
  "recommendations": [
    "密切监测病情变化",
    "考虑调整治疗方案",
    "加强生活调理指导"
  ]
}
```

---

## 诊断报告API

### 16. 生成诊断报告

**端点**: `POST /api/consultation/report/generate`

**描述**: 生成标准化的中医诊断报告

**请求体**:
```json
{
  "diagnosticId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "reportType": "Standard",
  "includeImages": true,
  "includePulseData": true,
  "format": "PDF"
}
```

**成功响应**:
```json
{
  "reportId": "3fa85f64-5717-4562-b3fc-2c963f66afac",
  "downloadUrl": "/api/consultation/report/3fa85f64-5717-4562-b3fc-2c963f66afac/download",
  "format": "PDF",
  "size": 2048576,
  "generatedAt": "2025-01-22T11:00:00Z",
  "expiresAt": "2025-01-29T11:00:00Z"
}
```

### 17. 下载诊断报告

**端点**: `GET /api/consultation/report/{reportId}/download`

**描述**: 下载已生成的诊断报告

**成功响应**: 文件流（PDF/Word格式）

### 18. 报告模板管理

**端点**: `GET /api/consultation/report/templates`

**描述**: 获取可用的诊断报告模板

**成功响应**:
```json
{
  "templates": [
    {
      "id": "standard-template",
      "name": "标准中医诊断报告",
      "description": "包含完整的四诊信息和辨证分析",
      "format": "PDF",
      "sections": [
        "PatientInfo",
        "ChiefComplaint",
        "FourDiagnosticMethods",
        "TonguePulseDiagnosis",
        "TCMDiagnosis",
        "TreatmentPrinciple"
      ]
    },
    {
      "id": "simple-template",
      "name": "简化诊断报告",
      "description": "简化的诊断报告格式",
      "format": "PDF",
      "sections": [
        "PatientInfo",
        "TCMDiagnosis",
        "TreatmentPrinciple"
      ]
    }
  ]
}
```

---

## 数据模型

### DiagnosticData（诊断数据）

```csharp
public class DiagnosticData
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid DoctorId { get; set; }

    // 主诉和现病史
    public string ChiefComplaint { get; set; }
    public string PresentIllness { get; set; }

    // 四诊信息
    public string Inspection { get; set; }        // 望诊
    public string AuscultationOlfaction { get; set; } // 闻诊
    public string Inquiry { get; set; }           // 问诊
    public string Palpation { get; set; }         // 切诊

    // 辨证分析
    public string TCMDiagnosis { get; set; }      // 中医诊断
    public string TreatmentPrinciple { get; set; } // 治则

    // 状态和时间
    public DiagnosticStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}
```

### TongueAnalysisResult（舌诊分析结果）

```csharp
public class TongueAnalysisResult
{
    public TongueBodyAnalysis TongueBody { get; set; }
    public TongueCoatingAnalysis TongueCoating { get; set; }
    public SublingualVeinAnalysis SublingualVeins { get; set; }
    public TongueShapeAnalysis TongueShape { get; set; }
    public TCMDiagnosisSuggestion TCMDiagnosis { get; set; }
    public float Confidence { get; set; }
}

public class TongueBodyAnalysis
{
    public TongueColor Color { get; set; }
    public TongueTexture Texture { get; set; }
    public TongueSize Size { get; set; }
    public TongueMobility Mobility { get; set; }
    public ColorAnalysis ColorValues { get; set; }
}
```

### PulseCharacteristics（脉诊特征）

```csharp
public class PulseCharacteristics
{
    // 脉位
    public PulsePosition Position { get; set; }    // 浮、中、沉
    public float PositionValue { get; set; }       // 0-1量化值

    // 脉率
    public int Rate { get; set; }                  // 次/分钟
    public PulseRhythm Rhythm { get; set; }        // 结、代、促、缓、数

    // 脉力
    public PulseStrength Strength { get; set; }    // 无力、有力、实脉
    public float StrengthValue { get; set; }       // 0-1量化值

    // 脉形
    public PulseShape Shape { get; set; }          // 弦、滑、涩、紧、濡
    public float Tension { get; set; }             // 紧张度 0-1
}
```

### SyndromeAnalysis（辨证分析）

```csharp
public class SyndromeAnalysis
{
    public EightPrincipleSyndrome EightPrinciples { get; set; }
    public List<OrganSyndrome> OrganSyndromes { get; set; }
    public string OverallDiagnosis { get; set; }
    public List<string> ClinicalRecommendations { get; set; }
    public float Confidence { get; set; }
}

public class EightPrincipleSyndrome
{
    public ExteriorInteriorSyndrome ExteriorInterior { get; set; }
    public ColdHeatSyndrome ColdHeat { get; set; }
    public DeficiencyExcessSyndrome DeficiencyExcess { get; set; }
    public YinYangSyndrome YinYang { get; set; }
}
```

---

## 权限和认证

### 权限级别

| 权限 | 描述 | 可访问操作 |
|------|------|------------|
| `diagnostic.view` | 查看诊断记录 | GET, SEARCH |
| `diagnostic.create` | 创建诊断记录 | POST |
| `diagnostic.edit` | 编辑诊断记录 | PUT（24小时内限制） |
| `diagnostic.edit.historical` | 编辑历史记录 | PUT（无时间限制） |
| `diagnostic.delete` | 删除诊断记录 | DELETE |
| `tongue.analyze` | 舌诊分析 | 舌诊相关API |
| `pulse.analyze` | 脉诊分析 | 脉诊相关API |
| `syndrome.analyze` | 辨证分析 | 辨证分析API |
| `report.generate` | 生成报告 | 报告生成API |
| `diagnostic.view.all` | 查看所有诊断 | 跨科室查看 |

### 认证方式

```http
Authorization: Bearer {jwt_token}
```

JWT Token包含以下信息：
- 用户ID
- 用户名
- 角色（Doctor, Admin, SystemAdmin）
- 权限列表
- 科室ID

### 当日编辑规则

- 医师只能编辑当日创建的诊断记录
- 上级医师可以编辑下级医师的诊断记录
- 超过24小时的修改需要`diagnostic.edit.historical`权限

---

## 错误代码

### 通用错误码

| 代码 | HTTP状态 | 描述 | 解决方案 |
|------|----------|------|----------|
| CONS001 | 400 | 请求参数验证失败 | 检查请求参数格式和必填字段 |
| CONS002 | 401 | 未授权访问 | 提供有效的JWT token |
| CONS003 | 403 | 权限不足 | 联系管理员分配相应权限 |
| CONS004 | 404 | 资源不存在 | 检查资源ID是否正确 |
| CONS005 | 409 | 资源冲突 | 检查资源状态和并发操作 |
| CONS006 | 429 | 请求过于频繁 | 降低请求频率 |
| CONS007 | 500 | 服务器内部错误 | 联系技术支持 |

### 诊断相关错误码

| 代码 | 描述 | 解决方案 |
|------|------|----------|
| CONS101 | 诊断数据不完整 | 补充缺失的必填信息 |
| CONS102 | 患者不存在 | 检查患者ID是否正确 |
| CONS103 | 病历不存在 | 检查病历ID是否正确 |
| CONS104 | 医师权限不足 | 确认医师具有诊断权限 |
| CONS105 | 超出编辑时间限制 | 联系上级医师或管理员 |
| CONS106 | 诊断记录已锁定 | 等待解锁或联系管理员 |

### 舌诊相关错误码

| 代码 | 描述 | 解决方案 |
|------|------|----------|
| CONS201 | 图像格式不支持 | 使用JPG或PNG格式 |
| CONS202 | 图像大小超限 | 图像大小不超过10MB |
| CONS203 | 图像质量不达标 | 重新采集高质量图像 |
| CONS204 | 舌诊分析失败 | 检查图像质量和网络连接 |
| CONS205 | 舌诊特征提取失败 | 尝试重新上传图像 |

### 脉诊相关错误码

| 代码 | 描述 | 解决方案 |
|------|------|----------|
| CONS301 | 脉诊设备连接失败 | 检查设备连接和状态 |
| CONS302 | 脉诊数据采集失败 | 重新进行数据采集 |
| CONS303 | 脉诊波形分析失败 | 检查数据质量和时长 |
| CONS304 | 脉诊特征识别失败 | 重新采集脉诊数据 |
| CONS305 | 脉诊设备校准失败 | 联系技术支持 |

### 辨证分析错误码

| 代码 | 描述 | 解决方案 |
|------|------|----------|
| CONS401 | 辨证数据不完整 | 补充四诊信息 |
| CONS402 | 辨证规则匹配失败 | 检查输入数据的准确性 |
| CONS403 | 辨证分析超时 | 重试分析或减少数据量 |
| CONS404 | 辨证结果置信度过低 | 检查数据质量和完整性 |

---

## 速率限制

### API调用限制

| 用户类型 | 每分钟限制 | 每小时限制 | 每日限制 |
|----------|------------|------------|----------|
| 普通医师 | 100次 | 2000次 | 10000次 |
| 主任医师 | 150次 | 3000次 | 15000次 |
| 系统管理员 | 200次 | 5000次 | 20000次 |

### 特殊操作限制

| 操作 | 限制 |
|------|------|
| 舌诊图像上传 | 10次/分钟 |
| 脉诊数据采集 | 5次/分钟 |
| 辨证分析 | 20次/分钟 |
| 报告生成 | 15次/分钟 |

超出限制时返回HTTP 429状态码，包含以下信息：
```json
{
  "error": "RATE_LIMIT_EXCEEDED",
  "message": "API调用频率超限",
  "retryAfter": 60,
  "limit": 100,
  "remaining": 0
}
```

---

## SDK和示例

### .NET SDK

```csharp
// 安装NuGet包
// Install-Package LYBT.Consultation.SDK

// 初始化客户端
var client = new ConsultationApiClient(new ConsultationApiClientOptions
{
    BaseUrl = "https://api.lybt.com",
    ApiKey = "your-api-key"
});

// 创建诊断记录
var diagnosticData = new CreateDiagnosticDataRequest
{
    PatientId = Guid.Parse("patient-id"),
    MedicalCaseId = Guid.Parse("medicalcase-id"),
    ChiefComplaint = "头痛伴恶心呕吐3天",
    PresentIllness = "患者3天前无明显诱因出现头痛...",
    TCMDiagnosis = "肝阳上亢证",
    TreatmentPrinciple = "平肝潜阳，活血止痛"
};

var result = await client.CreateDiagnosticDataAsync(diagnosticData);

// 上传舌诊图像
using var imageStream = File.OpenRead("tongue-image.jpg");
var tongueImage = await client.UploadTongueImageAsync(new UploadTongueImageRequest
{
    DiagnosticId = result.Id,
    ImageStream = imageStream,
    ImageType = TongueImageType.TongueSurface
});

// 进行辨证分析
var syndromeAnalysis = await client.AnalyzeEightPrinciplesAsync(new AnalyzeEightPrinciplesRequest
{
    DiagnosticId = result.Id,
    Symptoms = new[] { "头痛", "恶心", "呕吐" },
    IncludeTongueData = true,
    IncludePulseData = true
});
```

### JavaScript SDK

```javascript
// 安装npm包
// npm install @lybt/consultation-sdk

import { ConsultationApi } from '@lybt/consultation-sdk';

// 初始化客户端
const client = new ConsultationApi({
    baseUrl: 'https://api.lybt.com',
    apiKey: 'your-api-key'
});

// 创建诊断记录
const diagnosticData = {
    patientId: 'patient-id',
    medicalCaseId: 'medicalcase-id',
    chiefComplaint: '头痛伴恶心呕吐3天',
    presentIllness: '患者3天前无明显诱因出现头痛...',
    tcmDiagnosis: '肝阳上亢证',
    treatmentPrinciple: '平肝潜阳，活血止痛'
};

const result = await client.createDiagnosticData(diagnosticData);

// 上传舌诊图像
const imageFile = document.getElementById('tongueImage').files[0];
const tongueImage = await client.uploadTongueImage({
    diagnosticId: result.id,
    file: imageFile,
    imageType: 'TongueSurface'
});

// 进行辨证分析
const syndromeAnalysis = await client.analyzeEightPrinciples({
    diagnosticId: result.id,
    symptoms: ['头痛', '恶心', '呕吐'],
    includeTongueData: true,
    includePulseData: true
});
```

### Python SDK

```python
# 安装pip包
# pip install lybt-consultation-sdk

from lybt_consultation import ConsultationApi, CreateDiagnosticDataRequest

# 初始化客户端
client = ConsultationApi(
    base_url='https://api.lybt.com',
    api_key='your-api-key'
)

# 创建诊断记录
diagnostic_data = CreateDiagnosticDataRequest(
    patient_id='patient-id',
    medical_case_id='medicalcase-id',
    chief_complaint='头痛伴恶心呕吐3天',
    present_illness='患者3天前无明显诱因出现头痛...',
    tcm_diagnosis='肝阳上亢证',
    treatment_principle='平肝潜阳，活血止痛'
)

result = client.create_diagnostic_data(diagnostic_data)

# 上传舌诊图像
with open('tongue-image.jpg', 'rb') as f:
    tongue_image = client.upload_tongue_image(
        diagnostic_id=result.id,
        file=f,
        image_type='TongueSurface'
    )

# 进行辨证分析
syndrome_analysis = client.analyze_eight_principles(
    diagnostic_id=result.id,
    symptoms=['头痛', '恶心', '呕吐'],
    include_tongue_data=True,
    include_pulse_data=True
)
```

### cURL示例

```bash
# 创建诊断记录
curl -X POST "https://api.lybt.com/api/consultation/diagnostic" \
  -H "Authorization: Bearer your-jwt-token" \
  -H "Content-Type: application/json" \
  -d '{
    "patientId": "patient-id",
    "medicalCaseId": "medicalcase-id",
    "chiefComplaint": "头痛伴恶心呕吐3天",
    "presentIllness": "患者3天前无明显诱因出现头痛...",
    "tcmDiagnosis": "肝阳上亢证",
    "treatmentPrinciple": "平肝潜阳，活血止痛"
  }'

# 上传舌诊图像
curl -X POST "https://api.lybt.com/api/consultation/tongue-image" \
  -H "Authorization: Bearer your-jwt-token" \
  -F "file=@tongue-image.jpg" \
  -F "diagnosticId=diagnostic-id" \
  -F "imageType=TongueSurface"

# 进行辨证分析
curl -X POST "https://api.lybt.com/api/consultation/syndrome-analysis/eight-principles" \
  -H "Authorization: Bearer your-jwt-token" \
  -H "Content-Type: application/json" \
  -d '{
    "diagnosticId": "diagnostic-id",
    "symptoms": ["头痛", "恶心", "呕吐"],
    "tongueData": {
      "color": "淡红",
      "coating": "薄白"
    },
    "pulseData": {
      "rate": 72,
      "strength": "有力"
    }
  }'
```

---

## 版本更新记录

### v2.1.0 (2025-01-22)
- 新增舌诊图像质量检测API
- 优化脉诊波形分析算法
- 增强辨证分析准确性
- 支持诊断报告批量生成

### v2.0.0 (2024-12-15)
- 重构API架构，提升性能
- 新增脉诊设备集成支持
- 增强权限控制系统
- 支持离线数据同步

### v1.5.0 (2024-10-01)
- 新增舌诊图像自动分析
- 支持多种脉诊设备
- 优化辨证分析算法
- 增加诊断报告模板

### v1.0.0 (2024-08-01)
- 基础诊断数据管理
- 四诊信息录入
- 基本辨证分析
- 简单报告生成

如有其他问题或需要技术支持，请联系：
- 技术支持邮箱：support@lybt.com
- 开发者文档：https://docs.lybt.com
- API状态页面：https://status.lybt.com