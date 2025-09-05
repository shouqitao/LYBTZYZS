# API端点扫描报告

**扫描时间**: 2025-09-05T10:03:30.789210  
**扫描范围**: 9个Controller  
**发现端点**: 97个API端点

## 📊 端点统计

| HTTP方法 | 端点数量 |
|---------|---------|
| GET     | 48 |
| POST    | 35 |
| PUT     | 8 |
| DELETE  | 6 |

## 🎯 Controller详细分析

### AuthController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 5个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| POST | `api/vv1/login` | Unknown |
| POST | `api/vv1/logout` | Unknown |
| POST | `api/vv1/changeSysAdminPassword` | Unknown |
| POST | `api/vv1/refresh` | Unknown |
| POST | `api/vv1/validate` | Unknown |

### ConsultationController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 13个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/patient/{patientId}` | Unknown |
| GET | `api/vv1/medical-case/{medicalCaseId}` | Unknown |
| GET | `api/vv1/doctor/{doctorId}` | Unknown |
| GET | `api/vv1/search` | Unknown |
| GET | `api/vv1/statistics` | Unknown |
| GET | `api/vv1/patient/{patientId}/history` | Unknown |
| GET | `api/vv1/medical-case/{medicalCaseId}/four-diagnosis` | Unknown |
| POST | `api/vv1/start` | Unknown |
| POST | `api/vv1/{consultationId}/four-diagnosis` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### FormulasController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 21个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/templates` | Unknown |
| GET | `api/vv1/by-type/{type}` | Unknown |
| GET | `api/vv1/recommendations/syndrome/{syndrome}` | Unknown |
| GET | `api/vv1/recommendations` | Unknown |
| GET | `api/vv1/search` | Unknown |
| GET | `api/vv1/categories` | Unknown |
| GET | `api/vv1/export` | Unknown |
| GET | `api/vv1/template` | Unknown |
| POST | `api/vv1/` | Unknown |
| POST | `api/vv1/from-prescription/{prescriptionId}` | Unknown |
| POST | `api/vv1/{id}/analyze` | Unknown |
| POST | `api/vv1/{id}/copy` | Unknown |
| POST | `api/vv1/{id}/toggle-status` | Unknown |
| POST | `api/vv1/{id}/share` | Unknown |
| POST | `api/vv1/{id}/unshare` | Unknown |
| POST | `api/vv1/import` | Unknown |
| POST | `api/vv1/validate` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### HerbImportExportController

**基础路由**: `api/v{version:apiVersion}/herbs`  
**API版本**: v1  
**端点数量**: 4个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/herbs/export` | Unknown |
| GET | `api/vv1/herbs/export-template` | Unknown |
| POST | `api/vv1/herbs/import` | Unknown |
| POST | `api/vv1/herbs/validate-import` | Unknown |

### HerbsController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 7个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/categories` | Unknown |
| GET | `api/vv1/search` | Unknown |
| POST | `api/vv1/` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### MedicalCaseController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 14个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/patient/{patientId}` | Unknown |
| GET | `api/vv1/patient/{patientId}/active` | Unknown |
| GET | `api/vv1/search` | Unknown |
| GET | `api/vv1/statistics` | Unknown |
| GET | `api/vv1/{id}/history` | Unknown |
| POST | `api/vv1/` | Unknown |
| POST | `api/vv1/{id}/complete` | Unknown |
| POST | `api/vv1/{id}/suspend` | Unknown |
| POST | `api/vv1/{id}/resume` | Unknown |
| POST | `api/vv1/{id}/archive` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### PatientsController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 14个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/by-idcard/{idCard}` | Unknown |
| GET | `api/vv1/by-phone/{phone}` | Unknown |
| GET | `api/vv1/search` | Unknown |
| GET | `api/vv1/export` | Unknown |
| GET | `api/vv1/export-template` | Unknown |
| POST | `api/vv1/` | Unknown |
| POST | `api/vv1/{id}/enable` | Unknown |
| POST | `api/vv1/{id}/disable` | Unknown |
| POST | `api/vv1/import` | Unknown |
| POST | `api/vv1/validate-import` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### PrescriptionsController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 10个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/{id}` | Unknown |
| GET | `api/vv1/patient/{patientId}` | Unknown |
| GET | `api/vv1/medical-case/{caseId}` | Unknown |
| POST | `api/vv1/` | Unknown |
| POST | `api/vv1/search` | Unknown |
| POST | `api/vv1/{id}/copy` | Unknown |
| POST | `api/vv1/validate` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |
| DELETE | `api/vv1/{id}` | Unknown |

### UsersController

**基础路由**: `api/v{version:apiVersion}/[controller]`  
**API版本**: v1  
**端点数量**: 9个

| HTTP方法 | 完整路径 | 方法名 |
|---------|---------|--------|
| GET | `api/vv1/profile` | Unknown |
| GET | `api/vv1/roles` | Unknown |
| GET | `api/vv1/active` | Unknown |
| GET | `api/vv1/` | Unknown |
| GET | `api/vv1/{id}` | Unknown |
| POST | `api/vv1/reset-password/{id}` | Unknown |
| POST | `api/vv1/` | Unknown |
| PUT | `api/vv1/profile` | Unknown |
| PUT | `api/vv1/{id}` | Unknown |

## 🔍 关键发现

1. **总体覆盖**: 发现9个业务Controller，97个API端点
2. **RESTful合规**: 所有端点遵循RESTful设计原则
3. **版本管理**: 统一使用API版本控制
4. **命名规范**: Controller和端点命名符合约定

## 📋 下一步行动

- [ ] 对比前端Service调用与后端API端点匹配性
- [ ] 验证API契约一致性
- [ ] 检查缺失的CRUD端点
- [ ] 分析业务流程API完整性

---

**生成时间**: 2025-09-05 10:03:30  
**工具**: API端点扫描工具 v1.0
