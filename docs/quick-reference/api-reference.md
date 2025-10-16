# API快速参考

**基于13个实际控制器的完整API参考**（8个业务控制器 + 5个系统控制器） - 解决日常80%的API调用需求

## 📋 控制器总览

### 业务控制器（8个）
位置：`src/Server/Services/LYBT.WebAPI/Controllers/`

1. **AuthController** - 认证授权
2. **UsersController** - 用户管理
3. **PatientsController** - 患者管理
4. **MedicalCaseController** - 医案管理
5. **ConsultationController** - 诊疗记录
6. **PrescriptionsController** - 处方管理
7. **HerbsController** - 药材管理
8. **FormulasController** - 验方管理

### 系统控制器（5个）
位置：`src/Server/Services/LYBT.WebAPI/Controllers/`

9. **HealthController** - 健康检查
10. **CacheHealthController** - 缓存健康检查
11. **PerformanceController** - 性能监控
12. **RootHealthController** - 根路径健康检查
13. **BaseApiController** - 基础控制器（抽象基类）

---

## 🔐 认证API (AuthController)

### 基础认证
```bash
# 用户登录
POST /api/v1/auth/login
Content-Type: application/json
# 参数说明:
#   - username: 用户名
#   - password: 密码
#   - rememberMe: 是否记住登录状态

{
  "username": "doctor001",
  "password": "password123",
  "rememberMe": true
}
# 响应:
#   - 成功: 返回JWT Token
#   - 失败: 返回错误信息

# 超级管理员登录（隐藏端点）
# ⚠️ 注意：此端点使用 [ApiExplorerSettings(IgnoreApi = true)] 从Swagger文档中隐藏
#    仅在特定场景下使用，请谨慎操作
POST /api/v1/auth/admin/login
Content-Type: application/json

{
  "password": "admin_password"
}

# 用户登出
POST /api/v1/auth/logout
Authorization: Bearer {token}

{
  "username": "doctor001"
}
```

### Token验证
```bash
# GET方式验证Token（从Header获取）
GET /api/v1/auth/validate
Authorization: Bearer {token}

# POST方式验证Token（直接传递）
POST /api/v1/auth/validate
Content-Type: application/json

"your_jwt_token_here"
```

### 管理员操作
```bash
# 修改系统管理员密码
POST /api/v1/auth/changeSysAdminPassword
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "newPassword": "new_admin_password_123"
}
```

## 👥 用户管理API (UsersController)

### 基础CRUD
```bash
# 获取用户列表
GET /api/v1/users?page=1&pageSize=20&keyword=张三
Authorization: Bearer {token}
# 参数说明:
#   - page: 页码 (从1开始)
#   - pageSize: 每页数量 (默认20, 最大100)
#   - keyword: 搜索关键字 (可选)

# 获取用户详情
GET /api/v1/users/{userId}
Authorization: Bearer {token}
# 参数说明:
#   - userId: 用户唯一标识符 (GUID格式)

# 创建用户
POST /api/v1/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com",
  "password": "password123",
  "role": "Doctor"
}

# 更新用户
PUT /api/v1/users/{userId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "updated@example.com",
  "role": "Admin"
}

# 删除用户
DELETE /api/v1/users/{userId}
Authorization: Bearer {token}
```

### 额外管理功能
```bash
# 获取当前用户信息
GET /api/v1/users/current
Authorization: Bearer {token}

# 批量删除用户
POST /api/v1/users/batch-delete
Authorization: Bearer {token}
Content-Type: application/json

{
  "userIds": ["guid1", "guid2", "guid3"]
}

# 重置用户密码
POST /api/v1/users/{userId}/reset-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "newPassword": "NewPassword123!",
  "forceChangeOnNextLogin": true
}

# 切换用户状态（启用/禁用）
POST /api/v1/users/{userId}/toggle-status
Authorization: Bearer {token}
Content-Type: application/json

{
  "reason": "账号维护需要"
}
```

## 🏥 患者管理API (PatientsController)

### 基础操作
```bash
# 获取患者列表
GET /api/v1/patients?page=1&pageSize=20&keyword=张三
Authorization: Bearer {token}
# 参数说明:
#   - page: 页码 (从1开始)
#   - pageSize: 每页数量 (默认20, 最大100)
#   - keyword: 搜索关键字 (可选)

# 获取患者详情
GET /api/v1/patients/{patientId}
Authorization: Bearer {token}
# 参数说明:
#   - patientId: 患者唯一标识符 (GUID格式)

# 新增患者
POST /api/v1/patients
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "张三",
  "gender": "Male",
  "age": 35,
  "phone": "13800138000",
  "address": "北京市朝阳区"
}

# 更新患者信息
PUT /api/v1/patients/{patientId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "张三",
  "phone": "13900139000"
}

# 删除患者（软删除）
DELETE /api/v1/patients/{patientId}
Authorization: Bearer {token}
```

### 批量导入
```bash
# 批量导入患者（Excel文件）
POST /api/v1/patients/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [选择.xlsx文件]

# 下载导入模板
GET /api/v1/patients/import-template
# 无需认证，直接下载模板文件
```

## 📋 医案管理API (MedicalCaseController)

### 基础操作
```bash
# 获取医案列表
GET /api/v1/medical-case?page=1&pageSize=20&status=登记
Authorization: Bearer {token}

# 获取医案详情
GET /api/v1/medical-case/{caseId}
Authorization: Bearer {token}

# 创建新医案
POST /api/v1/medical-case
Authorization: Bearer {token}
Content-Type: application/json

{
  "patientId": "patient-guid",
  "doctorId": "doctor-guid",
  "chiefComplaint": "头痛失眠",
  "status": "登记"
}

# 更新医案
PUT /api/v1/medical-case/{caseId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "chiefComplaint": "头痛失眠（已缓解）",
  "status": "诊疗中"
}
```

### 状态管理
```bash
# 获取医案状态枚举
GET /api/v1/medical-case/statuses
Authorization: Bearer {token}

# 更新医案状态
PATCH /api/v1/medical-case/{caseId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "已完成"
}
```

## 🩺 诊疗记录API (ConsultationController)

### 基础操作
```bash
# 获取诊疗记录
GET /api/v1/consultation?medicalCaseId={caseId}
Authorization: Bearer {token}

# 创建诊疗记录
POST /api/v1/consultation
Authorization: Bearer {token}
Content-Type: application/json

{
  "medicalCaseId": "case-guid",
  "inspection": "面色红润，舌苔薄白",
  "auscultation": "语声洪亮，呼吸平顺",
  "inquiry": "头痛失眠，食欲不振",
  "palpation": "脉象弦细，舌质淡红",
  "diagnosis": "肝郁脾虚",
  "treatmentPrinciple": "疏肝解郁，健脾养血"
}

# 更新诊疗记录
PUT /api/v1/consultation/{consultationId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "diagnosis": "肝郁脾虚（加重）",
  "treatmentPrinciple": "加强疏肝解郁"
}
```

## 💊 处方管理API (PrescriptionsController)

### 基础操作
```bash
# 获取处方列表
GET /api/v1/prescriptions?page=1&pageSize=20&medicalCaseId={caseId}
Authorization: Bearer {token}

# 获取处方详情
GET /api/v1/prescriptions/{prescriptionId}
Authorization: Bearer {token}

# 创建处方
POST /api/v1/prescriptions
Authorization: Bearer {token}
Content-Type: application/json

{
  "medicalCaseId": "case-guid",
  "prescriptionType": "汤剂",
  "totalPrice": 156.50,
  "instructions": "每日一剂，分两次服用"
}

# 更新处方
PUT /api/v1/prescriptions/{prescriptionId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "instructions": "每日一剂，分三次服用"
}
```

### 处方项目
```bash
# 添加处方项目
POST /api/v1/prescriptions/{prescriptionId}/items
Authorization: Bearer {token}
Content-Type: application/json

{
  "herbId": "herb-guid",
  "dosage": 15,
  "unit": "g",
  "price": 25.80
}

# 更新处方项目
PUT /api/v1/prescriptions/{prescriptionId}/items/{itemId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "dosage": 18,
  "unit": "g",
  "price": 30.96
}

# 删除处方项目
DELETE /api/v1/prescriptions/{prescriptionId}/items/{itemId}
Authorization: Bearer {token}
```

### 处方管理功能
```bash
# 生成处方编号
GET /api/v1/prescriptions/generate-no
Authorization: Bearer {token}

# 获取处方统计信息
GET /api/v1/prescriptions/statistics
Authorization: Bearer {token}

# 获取指定时间范围的处方统计
GET /api/v1/prescriptions/statistics/range?startDate=2025-10-01&endDate=2025-10-16
Authorization: Bearer {token}

# 复制备方
POST /api/v1/prescriptions/{prescriptionId}/copy
Authorization: Bearer {token}
Content-Type: application/json

{
  "medicalCaseId": "new-case-guid",
  "copyItems": true
}
```

## 🌿 药材管理API (HerbsController)

### 基础操作
```bash
# 获取药材列表
GET /api/v1/herbs?page=1&pageSize=20&keyword=当归
Authorization: Bearer {token}

# 获取药材详情
GET /api/v1/herbs/{herbId}
Authorization: Bearer {token}

# 创建药材
POST /api/v1/herbs
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "当归",
  "pinyin": "danggui",
  "category": "补血药",
  "properties": "甘、辛、温",
  "meridians": "肝、心、脾",
  "functions": "补血活血，调经止痛，润肠通便",
  "price": 120.50,
  "stock": 1000,
  "unit": "g"
}

# 更新药材
PUT /api/v1/herbs/{herbId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "price": 125.00,
  "stock": 950
}
```

### 搜索功能
```bash
# 按拼音码搜索
GET /api/v1/herbs/search?pinyin=dg
Authorization: Bearer {token}

# 按分类搜索
GET /api/v1/herbs/search?category=补血药
Authorization: Bearer {token}

# 按功效搜索
GET /api/v1/herbs/search?function=补血
Authorization: Bearer {token}
```

### 药材批量操作
```bash
# 批量删除药材
POST /api/v1/herbs/batch-delete
Authorization: Bearer {token}
Content-Type: application/json

{
  "herbIds": ["guid1", "guid2", "guid3"]
}

# 导入药材数据（Excel文件）
POST /api/v1/herbs/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [选择.xlsx文件]

# 导出药材数据
GET /api/v1/herbs/export?format=excel&category=补血药
Authorization: Bearer {token}

# 下载药材导入模板
GET /api/v1/herbs/import-template
# 无需认证，直接下载模板文件
```

## 📜 验方管理API (FormulasController)

### 基础操作
```bash
# 获取验方列表
GET /api/v1/formulas?page=1&pageSize=20&category=补血方
Authorization: Bearer {token}

# 获取验方详情
GET /api/v1/formulas/{formulaId}
Authorization: Bearer {token}

# 创建验方
POST /api/v1/formulas
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "四物汤",
  "category": "补血方",
  "source": "太平惠民和剂局方",
  "description": "补血调血的基础方剂",
  "indications": "营血虚滞，月经不调，痛经",
  "totalPrice": 48.00
}

# 更新验方
PUT /api/v1/formulas/{formulaId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "description": "补血调血的基础方剂，用于营血虚滞"
}
```

### 验方药材
```bash
# 获取验方药材列表
GET /api/v1/formulas/{formulaId}/herbs
Authorization: Bearer {token}

# 添加验方药材
POST /api/v1/formulas/{formulaId}/herbs
Authorization: Bearer {token}
Content-Type: application/json

{
  "herbId": "herb-guid",
  "dosage": 12,
  "unit": "g"
}

# 更新验方药材
PUT /api/v1/formulas/{formulaId}/herbs/{itemId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "dosage": 15,
  "unit": "g"
}

# 删除验方药材
DELETE /api/v1/formulas/{formulaId}/herbs/{itemId}
Authorization: Bearer {token}
```

### 验方批量操作
```bash
# 批量删除验方
POST /api/v1/formulas/batch-delete
Authorization: Bearer {token}
Content-Type: application/json

{
  "formulaIds": ["guid1", "guid2", "guid3"]
}

# 导入验方数据（Excel文件）
POST /api/v1/formulas/import
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [选择.xlsx文件]

# 导出验方数据
GET /api/v1/formulas/export?format=excel&category=补血方
Authorization: Bearer {token}

# 下载验方导入模板
GET /api/v1/formulas/import-template
# 无需认证，直接下载模板文件

# 复制验方
POST /api/v1/formulas/{formulaId}/copy
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "四物汤（复制）",
  "copyHerbs": true
}
```

### 智能推荐
```bash
# 基于症状推荐验方
POST /api/v1/formulas/recommend
Authorization: Bearer {token}
Content-Type: application/json

{
  "symptoms": ["头痛", "失眠", "食欲不振"],
  "patientGender": "Female",
  "patientAge": 35
}

# 基于诊断推荐验方
POST /api/v1/formulas/recommend-by-diagnosis
Authorization: Bearer {token}
Content-Type: application/json

{
  "diagnosis": "肝郁脾虚",
  "treatmentPrinciple": "疏肝解郁，健脾养血"
}
```

## 📊 通用响应格式

### 成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体数据内容
  },
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

### 分页响应
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      // 数据项列表
    ],
    "totalCount": 150,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

### 错误响应
```json
{
  "success": false,
  "message": "操作失败",
  "error": {
    "code": "VALIDATION_ERROR",
    "details": "参数验证失败：用户名不能为空"
  },
  "code": 400,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

## 🔒 认证机制

### Bearer Token认证
```bash
# 在请求头中添加JWT Token
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 双轨认证系统
- **普通用户**: 使用用户名和密码登录
- **超级管理员**: 使用配置的用户名和密码登录（隐藏端点）

### Token过期处理
- **有效期**: AccessToken 2小时
- **处理方式**: 返回401状态码，需要重新登录

## 🚨 常见错误码

| 错误码 | HTTP状态码 | 说明 |
|--------|------------|------|
| `VALIDATION_ERROR` | 400 | 参数验证失败 |
| `UNAUTHORIZED` | 401 | 未认证或Token无效 |
| `FORBIDDEN` | 403 | 权限不足 |
| `NOT_FOUND` | 404 | 资源不存在 |
| `CONFLICT` | 409 | 资源冲突 |
| `RATE_LIMIT_EXCEEDED` | 429 | 请求频率超限 |
| `INTERNAL_ERROR` | 500 | 服务器内部错误 |

## 📱 在线文档

- **Swagger UI**: http://localhost:5001/swagger
- **API版本**: v1
- **基础URL**: http://localhost:5001/api/v1

---

*此API参考文档基于实际代码生成，确保100%准确性。如有疑问，请查看在线Swagger文档。*