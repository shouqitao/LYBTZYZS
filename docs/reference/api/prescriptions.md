# 处方管理 API 参考文档
**完整的处方管理 REST API 规范，包含创建、查询、修改、打印、审核等 20 个核心端点**

## 📋 API 目录

1. [处方基础操作](#1-处方基础操作)
   - 创建处方
   - 查询处方详情  
   - 更新处方信息
   - 删除处方
   - 获取处方列表

2. [处方高级功能](#2-处方高级功能)
   - 处方审核
   - 处方打印
   - 处方修改
   - 处方历史查询

3. [验方集成功能](#3-验方集成功能)
   - 从验方创建处方
   - 验方搜索
   - 多验方合并
   - 验方导入检查

4. [价格管理功能](#4-价格管理功能)
   - 价格计算
   - 折扣管理
   - 价格更新
   - 价格验证

5. [统计分析功能](#5-统计分析功能)
   - 处方统计
   - 趋势分析
   - 热门药材分析
   - 医生处方统计

---

## 1. 处方基础操作

### 1.1 创建处方

创建新的处方记录，支持直接创建或从验方导入。

**端点**: `POST /api/prescriptions`

**认证**: 需要 JWT Bearer Token

**请求头**:
```http
Content-Type: application/json
Authorization: Bearer {jwt_token}
```

**请求体**:
```json
{
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "indication": "失眠多梦，心脾两虚",
  "dosageCount": 7,
  "advice": "睡前1小时服用，避免咖啡因",
  "discount": 0.9,
  "remark": "患者为老客户，给予9折优惠",
  "items": [
    {
      "herbId": "550e8400-e29b-41d4-a716-446655440001",
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "usage": "单煎",
      "remark": "高丽参"
    },
    {
      "herbId": "550e8400-e29b-41d4-a716-446655440002", 
      "herbName": "酸枣仁",
      "quantity": 15,
      "unit": "g",
      "usage": "炒制",
      "remark": "安神助眠"
    }
  ]
}
```

**响应 (201 Created)**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "prescriptionNumber": "RX-20251122-0001",
  "patientId": "patient-uuid-here",
  "patientName": "张三",
  "doctorId": "doctor-uuid-here", 
  "doctorName": "李医生",
  "indication": "失眠多梦，心脾两虚",
  "dosageCount": 7,
  "advice": "睡前1小时服用，避免咖啡因",
  "discount": 0.9,
  "status": "Draft",
  "totalPrice": 156.80,
  "items": [
    {
      "id": "item-uuid-1",
      "herbId": "550e8400-e29b-41d4-a716-446655440001",
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "unitPrice": 12.00,
      "amount": 108.00,
      "usage": "单煎",
      "remark": "高丽参"
    },
    {
      "id": "item-uuid-2", 
      "herbId": "550e8400-e29b-41d4-a716-446655440002",
      "herbName": "酸枣仁",
      "quantity": 15,
      "unit": "g", 
      "unitPrice": 2.40,
      "amount": 36.00,
      "usage": "炒制",
      "remark": "安神助眠"
    }
  ],
  "createdAt": "2025-11-22T10:30:00Z",
  "createdBy": "user-uuid-here"
}
```

**错误响应**:
```json
{
  "error": {
    "code": "PRESCRIPTION_CREATION_FAILED",
    "message": "处方创建失败",
    "details": [
      "该医疗案例未确认需要处方",
      "药材 人参 价格信息异常"
    ]
  }
}
```

**状态码**:
- `201 Created` - 处方创建成功
- `400 Bad Request` - 请求参数错误
- `403 Forbidden` - 权限不足
- `404 Not Found` - 医疗案例不存在
- `409 Conflict` - 医疗案例已存在处方

---

### 1.2 查询处方详情

获取指定处方的详细信息。

**端点**: `GET /api/prescriptions/{id}`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**响应 (200 OK)**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "prescriptionNumber": "RX-20251122-0001",
  "patientInfo": {
    "id": "patient-uuid-here",
    "name": "张三",
    "gender": "Male",
    "age": 45,
    "phoneNumber": "138****5678"
  },
  "doctorInfo": {
    "id": "doctor-uuid-here",
    "name": "李医生",
    "department": "中医科",
    "license": "医师资格证123456"
  },
  "consultationInfo": {
    "chiefComplaint": "失眠多梦3月余",
    "tcmDiagnosis": "心脾两虚，肝郁化火",
    "treatmentPrinciple": "健脾养心，疏肝解郁"
  },
  "indication": "失眠多梦，心脾两虚",
  "dosageCount": 7,
  "advice": "睡前1小时服用，避免咖啡因",
  "discount": 0.9,
  "formulaSource": "归脾汤",
  "referencedFormulas": "归脾汤",
  "status": "Draft",
  "printVersion": 1,
  "isPrinted": false,
  "printCount": 0,
  "totalPrice": 156.80,
  "subtotalPrice": 174.20,
  "discountAmount": 17.40,
  "items": [
    {
      "id": "item-uuid-1",
      "herbId": "550e8400-e29b-41d4-a716-446655440001",
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "unitPrice": 12.00,
      "amount": 108.00,
      "totalAmount": 756.00,
      "usage": "单煎",
      "remark": "高丽参"
    }
  ],
  "createdAt": "2025-11-22T10:30:00Z",
  "createdBy": "user-uuid-here",
  "updatedAt": "2025-11-22T10:30:00Z"
}
```

**错误响应**:
```json
{
  "error": {
    "code": "PRESCRIPTION_NOT_FOUND",
    "message": "未找到指定的处方"
  }
}
```

**状态码**:
- `200 OK` - 查询成功
- `404 Not Found` - 处方不存在
- `403 Forbidden` - 权限不足

---

### 1.3 更新处方信息

更新处方的基础信息（非药材修改）。

**端点**: `PUT /api/prescriptions/{id}`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "indication": "失眠多梦，心脾两虚（复诊）",
  "dosageCount": 10,
  "advice": "睡前1小时服用，避免咖啡因，保持规律作息",
  "discount": 0.85,
  "remark": "复诊患者，症状有所改善"
}
```

**响应 (200 OK)**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "prescriptionNumber": "RX-20251122-0001",
  "indication": "失眠多梦，心脾两虚（复诊）",
  "dosageCount": 10,
  "advice": "睡前1小时服用，避免咖啡因，保持规律作息",
  "discount": 0.85,
  "remark": "复诊患者，症状有所改善",
  "totalPrice": 171.80,
  "updatedAt": "2025-11-22T14:30:00Z",
  "updatedBy": "user-uuid-here"
}
```

**错误响应**:
```json
{
  "error": {
    "code": "PRESCRIPTION_UPDATE_FAILED",
    "message": "处方更新失败",
    "details": [
      "已打印的处方不能修改",
      "只能修改当天创建的处方"
    ]
  }
}
```

**状态码**:
- `200 OK` - 更新成功
- `400 Bad Request` - 请求参数错误
- `403 Forbidden` - 权限不足或状态不允许修改
- `404 Not Found` - 处方不存在

---

### 1.4 删除处方

删除指定处方（仅允许删除草稿状态的处方）。

**端点**: `DELETE /api/prescriptions/{id}`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**查询参数**:
- `reason` (string, required): 删除原因

**响应 (204 No Content)**:
```http
HTTP/1.1 204 No Content
```

**错误响应**:
```json
{
  "error": {
    "code": "PRESCRIPTION_DELETE_FAILED",
    "message": "处方删除失败",
    "details": [
      "已打印的处方不能删除",
      "只能删除草稿状态的处方"
    ]
  }
}
```

**状态码**:
- `204 No Content` - 删除成功
- `400 Bad Request` - 缺少删除原因
- `403 Forbidden` - 权限不足或状态不允许删除
- `404 Not Found` - 处方不存在

---

### 1.5 获取处方列表

分页查询处方列表，支持多种筛选条件。

**端点**: `GET /api/prescriptions`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `pageIndex` (integer, optional): 页码，默认 1
- `pageSize` (integer, optional): 每页数量，默认 20
- `patientId` (string, optional): 患者ID筛选
- `doctorId` (string, optional): 医生ID筛选
- `status` (string, optional): 处方状态筛选 (Draft|Active|Printed|Completed|Cancelled)
- `startDate` (string, optional): 开始日期 (YYYY-MM-DD)
- `endDate` (string, optional): 结束日期 (YYYY-MM-DD)
- `searchTerm` (string, optional): 搜索关键词（处方编号或患者姓名）

**响应 (200 OK)**:
```json
{
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "prescriptionNumber": "RX-20251122-0001",
      "patientName": "张三",
      "doctorName": "李医生",
      "status": "Draft",
      "totalPrice": 156.80,
      "dosageCount": 7,
      "herbCount": 8,
      "createdAt": "2025-11-22T10:30:00Z"
    },
    {
      "id": "223e4567-e89b-12d3-a456-426614174001", 
      "prescriptionNumber": "RX-20251122-0002",
      "patientName": "李四",
      "doctorName": "王医生",
      "status": "Printed",
      "totalPrice": 234.50,
      "dosageCount": 7,
      "herbCount": 12,
      "createdAt": "2025-11-22T11:15:00Z"
    }
  ],
  "totalCount": 156,
  "pageIndex": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

**状态码**:
- `200 OK` - 查询成功
- `400 Bad Request` - 查询参数错误

---

## 2. 处方高级功能

### 2.1 处方审核

对处方进行全面审核，包括配伍禁忌、剂量安全等。

**端点**: `POST /api/prescriptions/{id}/audit`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "auditLevel": "Comprehensive",
  "checkCompatibility": true,
  "checkDosage": true,
  "checkContraindications": true
}
```

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "prescriptionNumber": "RX-20251122-0001",
  "overallResult": "Warning",
  "auditTime": "2025-11-22T15:00:00Z",
  "auditItems": [
    {
      "category": "基础检查",
      "result": "Pass",
      "level": "Required",
      "issues": [],
      "details": null
    },
    {
      "category": "安全检查", 
      "result": "Warning",
      "level": "Warning",
      "issues": [
        "配伍慎用：人参 与 五灵脂 - 建议谨慎使用，注意观察患者反应"
      ],
      "details": null
    },
    {
      "category": "优化建议",
      "result": "Info", 
      "level": "Info",
      "issues": [
        "处方价格较高（¥156.80），可考虑优化药材配比降低成本"
      ],
      "details": null
    }
  ],
  "recommendations": [
    "建议确认人参与五灵脂的配伍使用",
    "可考虑减少部分贵重药材用量"
  ]
}
```

**状态码**:
- `200 OK` - 审核完成
- `404 Not Found` - 处方不存在
- `400 Bad Request` - 审核参数错误

---

### 2.2 处方打印

打印处方，生成正式的处方单据。

**端点**: `POST /api/prescriptions/{id}/print`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "printerName": "HP_LaserJet_1010",
  "reason": "患者取药",
  "format": "Standard",
  "includeWatermark": true
}
```

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "prescriptionNumber": "RX-20251122-0001",
  "printVersion": 2,
  "printCount": 2,
  "printTime": "2025-11-22T16:30:00Z",
  "printData": {
    "basicInfo": {
      "prescriptionNumber": "RX-20251122-0001",
      "printDate": "2025年11月22日",
      "printVersion": 2
    },
    "patientInfo": {
      "name": "张*",
      "age": 45,
      "gender": "男"
    },
    "doctorInfo": {
      "name": "李医生",
      "department": "中医科",
      "license": "医师资格证123****"
    },
    "prescriptionContent": [
      {
        "sequence": 1,
        "herbName": "人参",
        "quantity": 9,
        "unit": "g",
        "usage": "单煎"
      }
    ],
    "securityFeatures": {
      "watermark": "LYBT-SECURE-20251122-A1B2C3D4",
      "verificationCode": "VER-123456789",
      "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
    }
  },
  "warnings": [
    "该处方已打印 2 次，这是第 2 次打印"
  ],
  "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
}
```

**错误响应**:
```json
{
  "error": {
    "code": "PRINT_PERMISSION_DENIED", 
    "message": "打印权限验证失败",
    "details": [
      "打印次数已达上限（3次），需要管理员权限"
    ]
  }
}
```

**状态码**:
- `200 OK` - 打印成功
- `403 Forbidden` - 打印权限不足
- `404 Not Found` - 处方不存在

---

### 2.3 处方修改

修改处方内容和药材信息。

**端点**: `POST /api/prescriptions/{id}/modify`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "modificationType": "AddItem",
  "reason": "患者复诊，增加安神药物",
  "newItems": [
    {
      "herbId": "550e8400-e29b-41d4-a716-446655440003",
      "herbName": "柏子仁",
      "quantity": 12,
      "unit": "g",
      "usage": "炒制",
      "remark": "加强安神效果"
    }
  ],
  "modifiedItems": [
    {
      "itemId": "item-uuid-2",
      "newQuantity": 18,
      "newUsage": "炒制，加强安神",
      "newRemark": "增加剂量，加强安神效果"
    }
  ]
}
```

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "prescriptionNumber": "RX-20251122-0001", 
  "modificationType": "AddItem",
  "modifiedAt": "2025-11-22T17:00:00Z",
  "modifiedFields": [
    "添加药材: 柏子仁 12g",
    "药材 酸枣仁 剂量: 15g -> 18g",
    "药材 酸枣仁 用法: 炒制 -> 炒制，加强安神",
    "总价: ¥156.80 -> ¥179.20"
  ],
  "warnings": [
    "该处方已打印，修改将产生新的打印版本"
  ],
  "newVersion": 2,
  "newTotalPrice": 179.20
}
```

**状态码**:
- `200 OK` - 修改成功
- `403 Forbidden` - 修改权限不足
- `404 Not Found` - 处方不存在
- `400 Bad Request` - 修改参数错误

---

### 2.4 处方历史查询

查询处方的修改历史和打印记录。

**端点**: `GET /api/prescriptions/{id}/history`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**查询参数**:
- `type` (string, optional): 历史类型 (modification|print|all)，默认 all
- `startDate` (string, optional): 开始日期
- `endDate` (string, optional): 结束日期

**响应 (200 OK)**:
```json
{
  "prescriptionInfo": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "prescriptionNumber": "RX-20251122-0001",
    "patientName": "张三",
    "totalPrintCount": 2,
    "lastPrintTime": "2025-11-22T16:30:00Z",
    "currentVersion": 2,
    "status": "Printed"
  },
  "modificationHistory": [
    {
      "modificationId": "mod-uuid-1",
      "modificationType": "AddItem",
      "modifiedBy": "李医生",
      "modifiedAt": "2025-11-22T17:00:00Z",
      "modificationReason": "患者复诊，增加安神药物",
      "modifiedFields": "添加药材: 柏子仁 12g; 酸枣仁 剂量: 15g -> 18g",
      "versionChange": 1 -> 2
    }
  ],
  "printHistory": [
    {
      "printId": "print-uuid-1",
      "printVersion": 1,
      "printTime": "2025-11-22T14:00:00Z",
      "printedBy": "李医生", 
      "printerName": "HP_LaserJet_1010",
      "printReason": "首次打印",
      "iPAddress": "192.168.1.100"
    },
    {
      "printId": "print-uuid-2",
      "printVersion": 2,
      "printTime": "2025-11-22T16:30:00Z",
      "printedBy": "李医生",
      "printerName": "HP_LaserJet_1010", 
      "printReason": "修改后重印",
      "iPAddress": "192.168.1.100"
    }
  ],
  "statistics": {
    "totalModifications": 1,
    "totalPrints": 2,
    "firstPrintTime": "2025-11-22T14:00:00Z",
    "lastPrintTime": "2025-11-22T16:30:00Z",
    "uniquePrinters": 1
  }
}
```

**状态码**:
- `200 OK` - 查询成功
- `404 Not Found` - 处方不存在

---

## 3. 验方集成功能

### 3.1 从验方创建处方

基于已有验方快速创建处方。

**端点**: `POST /api/prescriptions/from-formula`

**认证**: 需要 JWT Bearer Token

**请求体**:
```json
{
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "formulaId": "formula-uuid-gui-pi-tang",
  "dosageCount": 7,
  "discount": 0.9,
  "advice": "归脾汤加减，健脾养心",
  "modifications": [
    {
      "modificationType": "AddHerb",
      "herbId": "herb-uuid-suan-suan-zi-ren",
      "quantity": 15,
      "usage": "炒制",
      "reason": "安神助眠"
    },
    {
      "modificationType": "ModifyQuantity", 
      "herbId": "herb-uuid-huang-qi",
      "newQuantity": 20,
      "reason": "加强补气效果"
    }
  ],
  "autoReplaceMissingHerbs": false
}
```

**响应 (201 Created)**:
```json
{
  "id": "223e4567-e89b-12d3-a456-426614174002",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "prescriptionNumber": "RX-20251122-0003",
  "formulaSource": "归脾汤",
  "referencedFormulas": "归脾汤",
  "indication": "心脾两虚，失眠多梦",
  "dosageCount": 7,
  "advice": "归脾汤加减，健脾养心",
  "discount": 0.9,
  "status": "Draft",
  "totalPrice": 186.50,
  "items": [
    {
      "id": "item-uuid-1",
      "herbId": "herb-uuid-ren-shen",
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "unitPrice": 12.00,
      "amount": 108.00,
      "usage": "单煎",
      "remark": "来自验方: 归脾汤"
    },
    {
      "id": "item-uuid-2",
      "herbId": "herb-uuid-huang-qi", 
      "herbName": "黄芪",
      "quantity": 20,
      "unit": "g",
      "unitPrice": 8.00,
      "amount": 160.00,
      "usage": "蜜炙",
      "remark": "剂量调整: 18g -> 20g - 来自验方: 归脾汤"
    },
    {
      "id": "item-uuid-3",
      "herbId": "herb-uuid-suan-suan-zi-ren",
      "herbName": "酸枣仁", 
      "quantity": 15,
      "unit": "g",
      "unitPrice": 2.40,
      "amount": 36.00,
      "usage": "炒制",
      "remark": "个性化添加"
    }
  ],
  "createdAt": "2025-11-22T18:00:00Z",
  "createdBy": "user-uuid-here"
}
```

**状态码**:
- `201 Created` - 创建成功
- `400 Bad Request` - 请求参数错误
- `404 Not Found` - 医疗案例或验方不存在

---

### 3.2 验方搜索

搜索可用的验方库。

**端点**: `GET /api/formulas/search`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `keyword` (string, optional): 搜索关键词
- `category` (string, optional): 验方分类
- `indication` (string, optional): 主治筛选
- `pageSize` (integer, optional): 返回数量，默认 50

**响应 (200 OK)**:
```json
{
  "formulas": [
    {
      "id": "formula-uuid-gui-pi-tang",
      "name": "归脾汤",
      "pinyin": "gui pi tang",
      "category": "补益剂",
      "source": "《济生方》",
      "indication": "心脾气血两虚证。心悸怔忡，失眠健忘，食少体倦，面色萎黄",
      "herbCount": 8,
      "commonUsage": "临床常用于神经衰弱、失眠、贫血等心脾两虚证",
      "createdAt": "2025-01-01T00:00:00Z"
    },
    {
      "id": "formula-uuid-tian-wang-bu-xin-dan",
      "name": "天王补心丹", 
      "pinyin": "tian wang bu xin dan",
      "category": "安神剂",
      "source": "《摄生秘剖》",
      "indication": "阴亏血少。心悸怔忡，失眠多梦，神疲健忘",
      "herbCount": 16,
      "commonUsage": "常用于失眠、神经衰弱、冠心病等心阴不足证",
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 23,
  "hasMore": true
}
```

**状态码**:
- `200 OK` - 搜索成功
- `400 Bad Request` - 搜索参数错误

---

### 3.3 多验方合并

将多个验方合并为一个处方。

**端点**: `POST /api/prescriptions/from-multiple-formulas`

**认证**: 需要 JWT Bearer Token

**请求体**:
```json
{
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "formulaIds": [
    "formula-uuid-gui-pi-tang",
    "formula-uuid-tian-wang-bu-xin-dan"
  ],
  "dosageCount": 7,
  "discount": 0.9,
  "advice": "归脾汤合天王补心丹，健脾养心，滋阴安神"
}
```

**响应 (201 Created)**:
```json
{
  "id": "323e4567-e89b-12d3-a456-426614174003",
  "medicalCaseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "prescriptionNumber": "RX-20251122-0004",
  "formulaSource": "归脾汤, 天王补心丹",
  "referencedFormulas": "归脾汤, 天王补心丹",
  "indication": "心脾两虚，阴亏血少",
  "dosageCount": 7,
  "advice": "归脾汤合天王补心丹，健脾养心，滋阴安神",
  "discount": 0.9,
  "status": "Draft",
  "totalPrice": 245.60,
  "items": [
    {
      "id": "item-uuid-1",
      "herbId": "herb-uuid-ren-shen",
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "unitPrice": 12.00,
      "amount": 108.00,
      "usage": "单煎",
      "remark": "来自验方: 归脾汤"
    },
    {
      "id": "item-uuid-2", 
      "herbId": "herb-uuid-suan-suan-zi-ren",
      "herbName": "酸枣仁",
      "quantity": 30,
      "unit": "g",
      "unitPrice": 2.40,
      "amount": 72.00,
      "usage": "炒制",
      "remark": "合并: 归脾汤 + 天王补心丹"
    }
  ],
  "mergeDetails": [
    {
      "herbName": "酸枣仁",
      "originalSources": ["归脾汤 (15g)", "天王补心丹 (15g)"],
      "finalQuantity": 30,
      "mergeAction": "剂量累加"
    }
  ],
  "createdAt": "2025-11-22T19:00:00Z",
  "createdBy": "user-uuid-here"
}
```

**状态码**:
- `201 Created` - 创建成功
- `400 Bad Request` - 请求参数错误
- `409 Conflict` - 验方冲突或重复

---

### 3.4 验方导入检查

在导入验方前检查药材可用性和价格信息。

**端点**: `POST /api/formulas/{id}/import-check`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 验方ID

**响应 (200 OK)**:
```json
{
  "formulaId": "formula-uuid-gui-pi-tang",
  "formulaName": "归脾汤",
  "canImport": true,
  "issues": [],
  "warnings": [
    "单位不一致: 白术(g->钱)"
  ],
  "missingHerbs": [],
  "inactiveHerbs": [],
  "priceIssueHerbs": [],
  "unitIssueHerbs": [
    {
      "herbName": "白术",
      "currentUnit": "钱", 
      "targetUnit": "g",
      "conversionRate": 5
    }
  ],
  "estimatedPrice": {
    "perDosePrice": 186.50,
    "totalPrice": 1305.50,
    "dosageCount": 7,
    "priceRange": {
      "min": 165.00,
      "max": 208.00
    }
  }
}
```

**状态码**:
- `200 OK` - 检查完成
- `404 Not Found` - 验方不存在

---

## 4. 价格管理功能

### 4.1 价格计算

计算处方的详细价格信息。

**端点**: `POST /api/prescriptions/{id}/calculate-price`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "dosageCount": 7,
  "discount": 0.9,
  "includeDetailed": true
}
```

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "dosageCount": 7,
  "discount": 0.9,
  "items": [
    {
      "herbName": "人参",
      "quantity": 9,
      "unit": "g",
      "unitPrice": 12.00,
      "perDoseAmount": 108.00,
      "totalAmount": 756.00,
      "discountedAmount": 680.40
    },
    {
      "herbName": "酸枣仁",
      "quantity": 15,
      "unit": "g", 
      "unitPrice": 2.40,
      "perDoseAmount": 36.00,
      "totalAmount": 252.00,
      "discountedAmount": 226.80
    }
  ],
  "summary": {
    "perDoseSubtotal": 144.00,
    "totalSubtotal": 1008.00,
    "totalDiscount": 100.80,
    "finalPrice": 907.20
  },
  "statistics": {
    "totalHerbs": 8,
    "averageHerbPrice": 18.00,
    "mostExpensiveHerb": "人参",
    "leastExpensiveHerb": "茯苓",
    "priceRange": {
      "min": 6.00,
      "max": 108.00,
      "average": 18.00
    }
  }
}
```

**状态码**:
- `200 OK` - 计算完成
- `404 Not Found` - 处方不存在

---

### 4.2 折扣管理

获取和设置处方折扣。

**端点**: `GET /api/prescriptions/{id}/discount`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "currentDiscount": 1.0,
  "suggestedDiscount": 0.9,
  "availableRules": [
    {
      "name": "无折扣",
      "discountRate": 1.0,
      "condition": "默认",
      "description": "标准价格，无折扣",
      "isApplicable": true
    },
    {
      "name": "老患者优惠",
      "discountRate": 0.9,
      "condition": "就诊次数超过10次",
      "description": "老患者享受9折优惠",
      "isApplicable": true,
      "reason": "该患者已就诊15次"
    },
    {
      "name": "批量处方优惠",
      "discountRate": 0.8,
      "condition": "单帖价格超过200元",
      "description": "高价处方享受8折优惠",
      "isApplicable": false,
      "reason": "单帖价格为144.00元，未达到200元标准"
    }
  ],
  "discountCalculation": {
    "originalPrice": 1008.00,
    "discountAmount": 100.80,
    "finalPrice": 907.20,
    "savingPercentage": 10.0
  }
}
```

**状态码**:
- `200 OK` - 查询成功
- `404 Not Found` - 处方不存在

---

### 4.3 价格更新

更新处方中的药材价格。

**端点**: `POST /api/prescriptions/{id}/update-prices`

**认证**: 需要 JWT Bearer Token，需要管理员权限

**路径参数**:
- `id` (string, required): 处方ID

**请求体**:
```json
{
  "updateReason": "药材价格调整",
  "forceUpdate": false
}
```

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "prescriptionNumber": "RX-20251122-0001",
  "originalPrice": 1008.00,
  "newPrice": 1125.60,
  "priceDifference": 117.60,
  "updatedItems": [
    {
      "herbName": "人参",
      "originalPrice": 12.00,
      "newPrice": 15.00,
      "priceChange": 3.00,
      "changePercentage": 25.0
    },
    {
      "herbName": "当归",
      "originalPrice": 8.00,
      "newPrice": 9.50,
      "priceChange": 1.50,
      "changePercentage": 18.75
    }
  ],
  "updatedAt": "2025-11-22T20:00:00Z",
  "updatedBy": "admin-uuid-here"
}
```

**状态码**:
- `200 OK` - 更新成功
- `403 Forbidden` - 权限不足
- `404 Not Found` - 处方不存在

---

### 4.4 价格验证

验证处方价格的合理性。

**端点**: `POST /api/prescriptions/{id}/validate-price`

**认证**: 需要 JWT Bearer Token

**路径参数**:
- `id` (string, required): 处方ID

**响应 (200 OK)**:
```json
{
  "prescriptionId": "123e4567-e89b-12d3-a456-426614174000",
  "isValid": true,
  "calculatedPrice": 907.20,
  "warnings": [
    "处方价格较高（¥907.20），可考虑优化药材配比降低成本",
    "单味药 人参 价格占比过高: ¥680.40 (75.0%)"
  ],
  "errors": [],
  "priceAnalysis": {
    "priceRange": {
      "min": 600.00,
      "max": 1200.00,
      "average": 900.00,
      "currentPosition": "higher"
    },
    "priceFactors": [
      {
        "factor": "高价药材",
        "impact": "high",
        "herbs": ["人参", "黄芪"]
      },
      {
        "factor": "药材数量",
        "impact": "medium", 
        "count": 8,
        "averageCount": 6
      }
    ]
  },
  "optimizationSuggestions": [
    "可考虑减少人参用量或替换为性价比更高的党参",
    "检查部分药材是否为必需，可适当精简处方"
  ]
}
```

**状态码**:
- `200 OK` - 验证完成
- `404 Not Found` - 处方不存在

---

## 5. 统计分析功能

### 5.1 处方统计

获取处方相关统计数据。

**端点**: `GET /api/prescriptions/statistics`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `startDate` (string, optional): 开始日期 (YYYY-MM-DD)
- `endDate` (string, optional): 结束日期 (YYYY-MM-DD)
- `doctorId` (string, optional): 医生ID筛选
- `groupBy` (string, optional): 分组方式 (day|week|month)，默认 day

**响应 (200 OK)**:
```json
{
  "analysisPeriod": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-22"
  },
  "totalCount": 156,
  "totalAmount": 45680.50,
  "averageAmount": 292.82,
  "statusStatistics": [
    {
      "status": "Draft",
      "count": 23,
      "amount": 6780.50
    },
    {
      "status": "Printed", 
      "count": 98,
      "amount": 28900.00
    },
    {
      "status": "Completed",
      "count": 35,
      "amount": 10000.00
    }
  ],
  "dailyStatistics": [
    {
      "date": "2025-11-22",
      "count": 8,
      "amount": 2340.50
    },
    {
      "date": "2025-11-21",
      "count": 12,
      "amount": 3560.80
    }
  ],
  "popularHerbs": [
    {
      "herbName": "人参",
      "usageCount": 89,
      "totalQuantity": 801,
      "totalAmount": 10813.50,
      "averageQuantity": 9.0
    },
    {
      "herbName": "黄芪",
      "usageCount": 76,
      "totalQuantity": 1520,
      "totalAmount": 12160.00,
      "averageQuantity": 20.0
    }
  ],
  "popularFormulas": [
    {
      "formulaName": "归脾汤",
      "usageCount": 23,
      "percentage": 14.7
    },
    {
      "formulaName": "天王补心丹",
      "usageCount": 18,
      "percentage": 11.5
    }
  ]
}
```

**状态码**:
- `200 OK` - 统计成功
- `400 Bad Request` - 参数错误

---

### 5.2 趋势分析

分析处方趋势数据。

**端点**: `GET /api/prescriptions/trends`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `startDate` (string, required): 开始日期
- `endDate` (string, required): 结束日期
- `groupBy` (string, optional): 分组方式 (hour|day|week|month)

**响应 (200 OK)**:
```json
{
  "analysisPeriod": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-22"
  },
  "trendData": [
    {
      "period": "2025-11-01",
      "prescriptionCount": 15,
      "totalAmount": 4350.50,
      "averageAmount": 290.03,
      "uniquePatients": 12,
      "uniqueDoctors": 3
    },
    {
      "period": "2025-11-02",
      "prescriptionCount": 18,
      "totalAmount": 5280.80,
      "averageAmount": 293.38,
      "uniquePatients": 14,
      "uniqueDoctors": 3
    }
  ],
  "trendAnalysis": {
    "countTrend": {
      "direction": "increasing",
      "changeRate": 15.2,
      "correlation": 0.78
    },
    "amountTrend": {
      "direction": "increasing", 
      "changeRate": 18.6,
      "correlation": 0.82
    }
  },
  "insights": [
    "处方数量呈上升趋势，月度增长15.2%",
    "平均处方金额稳定在290元左右",
    "周三和周五为处方开具高峰期",
    "心脾两虚类处方占比较高"
  ]
}
```

**状态码**:
- `200 OK` - 分析完成
- `400 Bad Request` - 参数错误

---

### 5.3 热门药材分析

分析常用药材统计数据。

**端点**: `GET /api/prescriptions/popular-herbs`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `startDate` (string, optional): 开始日期
- `endDate` (string, optional): 结束日期
- `limit` (integer, optional): 返回数量，默认 20

**响应 (200 OK)**:
```json
{
  "analysisPeriod": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-22"
  },
  "popularHerbs": [
    {
      "herbName": "人参",
      "pinyin": "ren shen",
      "category": "补气药",
      "usageCount": 89,
      "totalQuantity": 801,
      "totalAmount": 10813.50,
      "averageQuantity": 9.0,
      "averageDosageRange": {
        "min": 6,
        "max": 15,
        "recommended": 9
      },
      "usageTrend": {
        "direction": "stable",
        "changeRate": 2.1
      },
      "commonPairings": [
        {
          "herbName": "黄芪",
          "coUsageCount": 67,
          "coUsagePercentage": 75.3
        },
        {
          "herbName": "白术",
          "coUsageCount": 45,
          "coUsagePercentage": 50.6
        }
      ]
    },
    {
      "herbName": "酸枣仁",
      "pinyin": "suan zao ren",
      "category": "安神药",
      "usageCount": 76,
      "totalQuantity": 1140,
      "totalAmount": 2736.00,
      "averageQuantity": 15.0,
      "averageDosageRange": {
        "min": 9,
        "max": 30,
        "recommended": 15
      },
      "usageTrend": {
        "direction": "increasing",
        "changeRate": 12.8
      },
      "commonPairings": [
        {
          "herbName": "柏子仁",
          "coUsageCount": 34,
          "coUsagePercentage": 44.7
        }
      ]
    }
  ],
  "categoryStatistics": [
    {
      "category": "补气药",
      "herbCount": 5,
      "totalUsage": 234,
      "percentage": 35.2
    },
    {
      "category": "安神药",
      "herbCount": 3,
      "totalUsage": 156,
      "percentage": 23.5
    }
  ]
}
```

**状态码**:
- `200 OK` - 分析完成
- `400 Bad Request` - 参数错误

---

### 5.4 医生处方统计

统计医生的处方数据。

**端点**: `GET /api/prescriptions/doctor-statistics`

**认证**: 需要 JWT Bearer Token

**查询参数**:
- `doctorId` (string, optional): 特定医生ID，不填则统计所有医生
- `startDate` (string, optional): 开始日期
- `endDate` (string, optional): 结束日期
- `includeRanking` (boolean, optional): 是否包含排名，默认 true

**响应 (200 OK)**:
```json
{
  "analysisPeriod": {
    "startDate": "2025-11-01",
    "endDate": "2025-11-22"
  },
  "summary": {
    "totalDoctors": 8,
    "totalPrescriptions": 156,
    "totalAmount": 45680.50,
    "averagePerDoctor": {
      "prescriptionCount": 19.5,
      "amount": 5700.06
    }
  },
  "doctorStatistics": [
    {
      "doctorId": "doctor-uuid-1",
      "doctorName": "李医生",
      "department": "中医科",
      "prescriptionCount": 28,
      "totalAmount": 8234.50,
      "averageAmount": 294.09,
      "uniquePatients": 23,
      "popularHerbs": [
        {
          "herbName": "人参",
          "usageCount": 22,
          "percentage": 78.6
        }
      ],
      "specializations": [
        {
          "category": "失眠调理",
          "prescriptionCount": 15,
          "percentage": 53.6
        }
      ],
      "ranking": {
        "byCount": 1,
        "byAmount": 1,
        "byPatients": 2
      }
    },
    {
      "doctorId": "doctor-uuid-2",
      "doctorName": "王医生",
      "department": "中医科",
      "prescriptionCount": 22,
      "totalAmount": 6156.80,
      "averageAmount": 279.85,
      "uniquePatients": 18,
      "ranking": {
        "byCount": 2,
        "byAmount": 2,
        "byPatients": 3
      }
    }
  ]
}
```

**状态码**:
- `200 OK` - 统计完成
- `400 Bad Request` - 参数错误
- `404 Not Found` - 医生不存在

---

## 🔒 错误代码参考

### 通用错误代码

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `UNAUTHORIZED` | 401 | 未提供有效的认证令牌 | 提供有效的JWT Token |
| `FORBIDDEN` | 403 | 权限不足 | 检查用户权限设置 |
| `PRESCRIPTION_NOT_FOUND` | 404 | 处方不存在 | 检查处方ID是否正确 |
| `VALIDATION_ERROR` | 400 | 请求参数验证失败 | 检查请求参数格式和值 |
| `INTERNAL_SERVER_ERROR` | 500 | 服务器内部错误 | 联系系统管理员 |

### 业务错误代码

| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|---------|-----------|------|----------|
| `PRESCRIPTION_CREATION_FAILED` | 400 | 处方创建失败 | 检查医疗案例状态和药材信息 |
| `PRESCRIPTION_UPDATE_FAILED` | 400 | 处方更新失败 | 检查处方状态和修改权限 |
| `PRESCRIPTION_DELETE_FAILED` | 403 | 处方删除失败 | 检查处方状态，只能删除草稿状态处方 |
| `PRINT_PERMISSION_DENIED` | 403 | 打印权限不足 | 检查打印权限设置和打印次数限制 |
| `MODIFICATION_PERMISSION_DENIED` | 403 | 修改权限不足 | 检查修改权限和时间限制 |
| `FORMULA_IMPORT_FAILED` | 400 | 验方导入失败 | 检查验方状态和药材可用性 |
| `PRICE_CALCULATION_ERROR` | 400 | 价格计算错误 | 检查药材价格信息和折扣设置 |

### 警告代码

| 警告代码 | 描述 | 建议操作 |
|---------|------|----------|
| `PRESCRIPTION_AUDIT_WARNING` | 处方审核有警告 | 仔细查看审核结果，确认是否需要调整 |
| `PRICE_ABOVE_AVERAGE` | 处方价格高于平均水平 | 考虑优化药材配比降低成本 |
| `DOSAGE_SAFETY_WARNING` | 剂量安全警告 | 确认剂量设置是否合理 |
| `PRINT_COUNT_WARNING` | 打印次数警告 | 确认是否需要重复打印 |

---

## 📋 使用示例

### JavaScript/TypeScript 示例

```typescript
// 创建处方
async function createPrescription(medicalCaseId: string, prescriptionData: any) {
  const response = await fetch('/api/prescriptions', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${jwtToken}`
    },
    body: JSON.stringify({
      medicalCaseId,
      ...prescriptionData
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error.message);
  }
  
  return await response.json();
}

// 打印处方
async function printPrescription(prescriptionId: string, printerName: string) {
  const response = await fetch(`/api/prescriptions/${prescriptionId}/print`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${jwtToken}`
    },
    body: JSON.stringify({
      printerName,
      reason: '患者取药'
    })
  });
  
  return await response.json();
}
```

### C# 示例

```csharp
// 创建处方
public async Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionRequest request)
{
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await _httpClient.PostAsync("/api/prescriptions", content);
    response.EnsureSuccessStatusCode();
    
    var responseJson = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<PrescriptionDto>(responseJson);
}

// 处方审核
public async Task<PrescriptionAuditResult> AuditPrescriptionAsync(Guid prescriptionId)
{
    var response = await _httpClient.PostAsync($"/api/prescriptions/{prescriptionId}/audit", null);
    response.EnsureSuccessStatusCode();
    
    var responseJson = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<PrescriptionAuditResult>(responseJson);
}
```

---

## 📝 API 使用说明

### 认证要求

所有API端点都需要有效的JWT Bearer Token进行身份验证。Token应包含在请求头的`Authorization`字段中：

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 分页参数

列表查询API支持分页，常用参数：
- `pageIndex`: 页码，从1开始
- `pageSize`: 每页数量，建议不超过100

### 时间格式

所有时间相关参数使用ISO 8601格式：
- 日期：`YYYY-MM-DD` (如：2025-11-22)
- 日期时间：`YYYY-MM-DDTHH:mm:ssZ` (如：2025-11-22T10:30:00Z)

### 错误处理

API使用标准HTTP状态码表示请求结果：
- `2xx`: 成功
- `4xx`: 客户端错误
- `5xx`: 服务器错误

错误响应格式：
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "错误描述",
    "details": ["详细错误信息"]
  }
}
```

### 速率限制

为保护系统性能，API实施速率限制：
- 每分钟最多100个请求
- 每小时最多1000个请求
- 每天最多10000个请求

超出限制时返回`429 Too Many Requests`状态码。

---

## 🔄 API 版本管理

当前API版本：v1.0

版本控制通过URL路径实现：
- v1: `/api/v1/prescriptions`
- v2: `/api/v2/prescriptions` (未来版本)

向后兼容性保证：
- 新增字段不会破坏现有客户端
- 废弃字段会提前通知
- 重大变更会发布新版本