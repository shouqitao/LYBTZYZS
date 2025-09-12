# P3 冒烟验证计划 - Record-Only模式基线验证

**生成时间**: 2025-09-12  
**目标**: 验证删减后系统在Record-Only（CRUD + 历史查询）基线下可完整运行  
**范围**: 4个核心业务模块的基本CRUD操作和历史查询功能  

## 验证原则

- ✅ 仅验证基础CRUD操作和历史查询
- ✅ 不涉及智能推荐、配伍检查、规则引擎等超范围功能  
- ✅ 验证前后端API映射的完整闭环
- ✅ 确保所有端点返回预期的2xx/404状态码

## 用例矩阵

### A) Patients 模块 - 患者档案管理

| 操作 | API端点 | 请求方法 | 预期状态码 | 说明 |
|------|---------|----------|------------|------|
| 创建患者 | `/api/v1/patients` | POST | 200/201 | 创建基本患者档案 |
| 获取患者 | `/api/v1/patients/{id}` | GET | 200/404 | 根据ID获取患者详情 |
| 患者列表 | `/api/v1/patients` | GET | 200 | 分页获取患者列表 |
| 更新患者 | `/api/v1/patients/{id}` | PUT | 200/404 | 更新患者基本信息 |
| 删除患者 | `/api/v1/patients/{id}` | DELETE | 200/404 | 软删除患者档案 |

**示例请求载荷** (创建患者):
```json
{
  "name": "测试患者01",
  "gender": "Male", 
  "birthDate": "1990-01-01T00:00:00Z",
  "phone": "13800138001",
  "address": "测试地址123号"
}
```

**示例响应字段**:
```json
{
  "success": true,
  "data": {
    "id": "uuid-string",
    "name": "测试患者01",
    "gender": "Male",
    "age": 35,
    "createTime": "2025-09-12T14:30:00Z"
  },
  "message": "患者创建成功"
}
```

### B) Prescriptions 模块 - 处方管理

| 操作 | API端点 | 请求方法 | 预期状态码 | 说明 |
|------|---------|----------|------------|------|
| 创建处方 | `/api/v1/prescriptions` | POST | 200/201 | 创建包含药材的处方 |
| 获取处方 | `/api/v1/prescriptions/{id}` | GET | 200/404 | 获取处方详情和药材列表 |
| 处方列表 | `/api/v1/prescriptions` | GET | 200 | 分页获取处方列表 |
| 删除处方 | `/api/v1/prescriptions/{id}` | DELETE | 200/404 | 删除处方及其药材项 |

**示例请求载荷** (创建处方):
```json
{
  "patientId": "patient-uuid",
  "consultationId": "consultation-uuid",
  "prescriptionName": "测试处方01",
  "usage": "每日3次，饭后服用",
  "dosage": "每次10克",
  "items": [
    {
      "herbId": "herb-uuid-1",
      "quantity": 15.0,
      "unit": "克",
      "usage": "先煎"
    },
    {
      "herbId": "herb-uuid-2", 
      "quantity": 10.0,
      "unit": "克",
      "usage": "后下"
    }
  ]
}
```

**示例响应字段**:
```json
{
  "success": true,
  "data": {
    "id": "prescription-uuid",
    "prescriptionName": "测试处方01",
    "totalPrice": 25.50,
    "itemCount": 2,
    "items": [...]
  },
  "message": "处方创建成功"
}
```

### C) Consultation 模块 - 看诊诊断

| 操作 | API端点 | 请求方法 | 预期状态码 | 说明 |
|------|---------|----------|------------|------|
| 创建诊断 | `/api/v1/consultations` | POST | 200/201 | 创建包含四诊的诊断记录 |
| 获取历史 | `/api/v1/consultations/patient/{patientId}` | GET | 200 | 获取患者诊疗历史 |

**示例请求载荷** (创建诊断):
```json
{
  "patientId": "patient-uuid",
  "medicalCaseId": "case-uuid",
  "chiefComplaint": "头痛3天",
  "presentIllness": "患者3天前开始出现头痛，持续性胀痛",
  "inspection": "面色稍黄，精神可",
  "auscultation": "语音清晰",
  "inquiry": "睡眠一般，大小便正常",
  "palpation": "脉象弦细，舌质淡红苔薄白",
  "diagnosis": "头痛（肝阳上亢）",
  "treatment": "平肝潜阳，镇静止痛"
}
```

### D) Herbs 模块 - 中药材管理

| 操作 | API端点 | 请求方法 | 预期状态码 | 说明 |
|------|---------|----------|------------|------|
| 创建药材 | `/api/v1/herbs` | POST | 200/201 | 添加新的中药材 |
| 获取药材 | `/api/v1/herbs/{id}` | GET | 200/404 | 获取药材详情 |
| 药材列表 | `/api/v1/herbs` | GET | 200 | 分页获取药材列表 |
| 更新药材 | `/api/v1/herbs/{id}` | PUT | 200/404 | 更新药材信息 |
| 删除药材 | `/api/v1/herbs/{id}` | DELETE | 200/404 | 删除药材 |

**示例请求载荷** (创建药材):
```json
{
  "name": "当归",
  "category": "补血药",
  "properties": "甘、辛，温",
  "meridians": "心、肝、脾经",
  "effects": "补血调经，活血止痛",
  "dosage": "5-15克",
  "price": 0.80
}
```

### E) Formula 模块 - 验方管理

| 操作 | API端点 | 请求方法 | 预期状态码 | 说明 |
|------|---------|----------|------------|------|
| 创建验方 | `/api/v1/formulas` | POST | 200/201 | 添加新验方模板 |
| 获取验方 | `/api/v1/formulas/{id}` | GET | 200/404 | 获取验方详情 |
| 验方列表 | `/api/v1/formulas` | GET | 200 | 分页获取验方列表 |
| 更新验方 | `/api/v1/formulas/{id}` | PUT | 200/404 | 更新验方信息 |
| 删除验方 | `/api/v1/formulas/{id}` | DELETE | 200/404 | 删除验方 |

**示例请求载荷** (创建验方):
```json
{
  "name": "四君子汤",
  "category": "补气方",
  "effect": "益气健脾",
  "usage": "水煎服",
  "isShared": true,
  "herbs": [
    {
      "herbName": "人参",
      "dosage": 9.0,
      "unit": "克"
    },
    {
      "herbName": "白术",
      "dosage": 9.0,
      "unit": "克"
    }
  ]
}
```

## 健康检查端点

| 端点 | 说明 |
|------|------|
| `/api/v1/health` | 系统基础健康检查 |
| `/api/v1/health/ready` | 应用就绪状态检查 |

## 验证执行顺序

1. **Herbs**: 先创建药材数据（后续处方需要）
2. **Formula**: 创建验方模板（可选）
3. **Patients**: 创建患者档案（诊疗流程需要）
4. **Consultation**: 创建诊断记录（处方需要）
5. **Prescriptions**: 创建处方（使用前面创建的数据）
6. **清理**: 按相反顺序删除测试数据

## 失败标准

以下情况视为验证失败：
- API返回5xx服务器错误
- 必要的CRUD端点返回404未实现
- 返回数据结构与预期DTO不匹配
- 创建->查询->更新->删除闭环断裂
- 出现智能推荐、配伍检查等超范围功能的API响应

## 成功标准

以下情况视为验证通过：
- 所有CRUD操作返回预期状态码
- 数据创建后可正确查询和更新
- 历史查询功能正常
- 分页和列表功能正常
- 无超范围功能残留的API响应

---

**计划版本**: 1.0  
**适用范围**: Record-Only基线验证  
**更新日期**: 2025-09-12