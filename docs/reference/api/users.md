# Users API参考文档 (Users API Reference)

> **信息导向**: 精确的用户管理API接口技术文档
> **适合人群**: 开发者、API集成人员、系统管理员
> **使用方式**: 精确查询、接口对接、技术实现

## 🔌 API概览

### 基本信息
- **API版本**: v1
- **基础路径**: `/api/v1/users`
- **认证方式**: JWT Bearer Token
- **内容类型**: `application/json`
- **字符编码**: UTF-8

### 权限要求
- **Admin权限**: 用户创建、更新、删除、状态管理
- **Doctor/Nurse权限**: 个人信息查看和修改
- **SuperAdmin权限**: 所有用户管理功能

## 👥 用户管理端点 (User Management Endpoints)

### 获取用户列表

#### 端点信息
```http
GET /api/v1/users
```

#### 查询参数
```
pageIndex=1&pageSize=20&sortBy=CreatedAt&sortOrder=desc&role=Doctor&status=Active&search=张三
```

**参数说明**
| 参数名 | 类型 | 必填 | 默认值 | 描述 |
|--------|------|------|--------|------|
| pageIndex | int | 否 | 1 | 页码（从1开始） |
| pageSize | int | 否 | 20 | 每页数量（1-100） |
| sortBy | string | 否 | CreatedAt | 排序字段 |
| sortOrder | string | 否 | desc | 排序方向（asc/desc） |
| role | string | 否 | - | 角色筛选 |
| status | string | 否 | - | 状态筛选 |
| search | string | 否 | - | 搜索关键词 |

#### 响应格式
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "00000000-0000-0000-0000-000000000001",
        "userName": "doctor_wang",
        "realName": "王医生",
        "pinYinCode": "wangys",
        "phoneNumber": "138****8000",
        "email": "doctor***@clinic.com",
        "role": "Doctor",
        "status": "Active",
        "department": "内科",
        "title": "主治医师",
        "lastLoginTime": "2025-01-01T09:30:00Z",
        "failedLoginCount": 0,
        "lockoutEnd": null,
        "createdAt": "2024-01-01T00:00:00Z",
        "createdBy": "admin",
        "updatedAt": "2025-01-01T08:00:00Z",
        "updatedBy": "admin",
        "remark": "内科主治医师，10年临床经验"
      }
    ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 156,
    "totalPages": 8
  }
}
```

### 获取用户详情

#### 端点信息
```http
GET /api/v1/users/{userId}
Authorization: Bearer <access_token>
```

#### 路径参数
| 参数名 | 类型 | 描述 |
|--------|------|------|
| userId | string | 用户ID（GUID格式） |

#### 响应格式
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "00000000-0000-0000-0000-000000000001",
    "userName": "doctor_wang",
    "realName": "王医生",
    "pinYinCode": "wangys",
    "phoneNumber": "13800138000",
    "email": "doctor.wang@clinic.com",
    "role": "Doctor",
    "status": "Active",
    "department": "内科",
    "title": "主治医师",
    "specialties": ["脾胃调理", "针灸治疗"],
    "qualification": "中医执业医师",
    "licenseNumber": "ZYY123456789",
    "workYears": 10,
    "lastLoginTime": "2025-01-01T09:30:00Z",
    "failedLoginCount": 0,
    "lockoutEnd": null,
    "createdAt": "2024-01-01T00:00:00Z",
    "createdBy": "admin",
    "updatedAt": "2025-01-01T08:00:00Z",
    "updatedBy": "admin",
    "remark": "内科主治医师，10年临床经验"
  }
}
```

### 创建新用户

#### 端点信息
```http
POST /api/v1/users
Authorization: Bearer <access_token>
```

#### 权限要求
- SuperAdmin: 可创建所有角色
- Admin: 可创建Doctor和Nurse角色

#### 请求体
```json
{
  "userName": "doctor_li",
  "password": "TempPassword123!",
  "realName": "李医生",
  "phoneNumber": "13800138001",
  "email": "doctor.li@clinic.com",
  "role": "Doctor",
  "department": "外科",
  "title": "住院医师",
  "specialties": ["外伤处理", "康复治疗"],
  "qualification": "中医执业医师",
  "licenseNumber": "ZYY987654321",
  "workYears": 3,
  "remark": "外科住院医师，擅长外伤处理"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 约束 | 描述 |
|--------|------|------|------|------|
| userName | string | 是 | 3-50字符，字母数字下划线 | 用户名，唯一 |
| password | string | 是 | 8-128字符，符合密码策略 | 初始密码 |
| realName | string | 是 | 2-50字符，中文字符 | 真实姓名 |
| phoneNumber | string | 是 | 11位手机号 | 手机号码 |
| email | string | 否 | 邮箱格式 | 电子邮箱 |
| role | string | 是 | Doctor/Nurse/Admin | 用户角色 |
| department | string | 否 | 50字符 | 所属科室 |
| title | string | 否 | 50字符 | 职称 |
| specialties | string[] | 否 | 数组 | 专业特长 |
| qualification | string | 否 | 100字符 | 资质证书 |
| licenseNumber | string | 否 | 50字符 | 执业证书号 |
| workYears | int | 否 | 0-50 | 工作年限 |
| remark | string | 否 | 500字符 | 备注信息 |

#### 响应格式
```json
{
  "success": true,
  "message": "用户创建成功",
  "data": {
    "id": "00000000-0000-0000-0000-000000000002",
    "userName": "doctor_li",
    "realName": "李医生",
    "pinYinCode": "liys",
    "role": "Doctor",
    "status": "Active",
    "createdAt": "2025-01-01T12:00:00Z",
    "createdBy": "admin"
  }
}
```

### 更新用户信息

#### 端点信息
```http
PUT /api/v1/users/{userId}
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "realName": "李主治医师",
  "phoneNumber": "13800138002",
  "email": "chief.doctor@clinic.com",
  "department": "中西医结合科",
  "title": "主治医师",
  "specialties": ["脾胃调理", "中西医结合治疗", "针灸治疗"],
  "qualification": "中医主治医师",
  "licenseNumber": "ZYY987654321",
  "workYears": 8,
  "remark": "中西医结合科主治医师，8年临床经验"
}
```

#### 响应格式
```json
{
  "success": true,
  "message": "用户信息更新成功",
  "data": {
    "id": "00000000-0000-0000-0000-000000000002",
    "realName": "李主治医师",
    "updatedAt": "2025-01-01T12:30:00Z",
    "updatedBy": "admin"
  }
}
```

### 删除用户

#### 端点信息
```http
DELETE /api/v1/users/{userId}
Authorization: Bearer <access_token>
```

#### 权限要求
- 仅限SuperAdmin

#### 请求体
```json
{
  "deleteMode": "Soft",
  "archiveData": true,
  "reason": "员工离职"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| deleteMode | string | 否 | 删除模式：Soft（软删除）/Hard（硬删除） |
| archiveData | boolean | 否 | 是否归档用户数据 |
| reason | string | 是 | 删除原因 |

#### 响应格式
```json
{
  "success": true,
  "message": "用户删除成功",
  "data": {
    "deletedUserId": "00000000-0000-0000-0000-000000000002",
    "deleteMode": "Soft",
    "archivedDataId": "archive_123456",
    "deletedAt": "2025-01-01T12:45:00Z",
    "deletedBy": "admin"
  }
}
```

## 🔍 搜索和筛选端点

### 用户搜索

#### 端点信息
```http
GET /api/v1/users/search
Authorization: Bearer <access_token>
```

#### 查询参数
```
q=王医生&searchType=mixed&role=Doctor&status=Active&department=内科&pageIndex=1&pageSize=20
```

**参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| q | string | 是 | 搜索关键词 |
| searchType | string | 否 | 搜索类型：name/pinyin/phone/mixed |
| role | string | 否 | 角色筛选 |
| status | string | 否 | 状态筛选 |
| department | string | 否 | 科室筛选 |
| pageIndex | int | 否 | 页码 |
| pageSize | int | 否 | 每页数量 |

#### 响应格式
```json
{
  "success": true,
  "message": "搜索完成",
  "data": {
    "items": [
      {
        "id": "00000000-0000-0000-0000-000000000001",
        "userName": "doctor_wang",
        "realName": "王医生",
        "pinYinCode": "wangys",
        "role": "Doctor",
        "status": "Active",
        "matchType": "NameExact"
      }
    ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 3,
    "searchQuery": "王医生",
    "searchType": "mixed"
  }
}
```

### 高级筛选

#### 端点信息
```http
GET /api/v1/users/filter
Authorization: Bearer <access_token>
```

#### 请求体（POST方式支持复杂筛选）
```json
{
  "filters": {
    "role": ["Doctor", "Nurse"],
    "status": ["Active"],
    "department": ["内科", "外科"],
    "workYears": {
      "min": 5,
      "max": 20
    },
    "createdDate": {
      "start": "2024-01-01",
      "end": "2024-12-31"
    },
    "lastLoginDate": {
      "start": "2024-06-01",
      "end": "2024-12-31"
    }
  },
  "pageIndex": 1,
  "pageSize": 20,
  "sortBy": "WorkYears",
  "sortOrder": "desc"
}
```

## 🔄 状态管理端点

### 启用用户

#### 端点信息
```http
POST /api/v1/users/{userId}/enable
Authorization: Bearer <access_token>
```

#### 权限要求
- SuperAdmin: 可启用所有用户
- Admin: 可启用Doctor和Nurse用户

#### 请求体
```json
{
  "reason": "假期结束，恢复正常工作"
}
```

### 禁用用户

#### 端点信息
```http
POST /api/v1/users/{userId}/disable
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "reason": "长期休假，临时禁用账户",
  "scheduledTime": "2025-01-02T00:00:00Z"
}
```

### 切换用户状态

#### 端点信息
```http
POST /api/v1/users/{userId}/toggle-status
Authorization: Bearer <access_token>
```

#### 响应格式
```json
{
  "success": true,
  "message": "用户状态切换成功",
  "data": {
    "userId": "00000000-0000-0000-0000-000000000001",
    "previousStatus": "Active",
    "newStatus": "Disabled",
    "changedAt": "2025-01-01T13:00:00Z",
    "changedBy": "admin"
  }
}
```

### 解锁用户账户

#### 端点信息
```http
POST /api/v1/users/{userId}/unlock
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "reason": "身份验证通过，解除登录锁定",
  "resetFailedCount": true
}
```

## 🔐 密码管理端点

### 重置用户密码

#### 端点信息
```http
POST /api/v1/users/{userId}/reset-password
Authorization: Bearer <access_token>
```

#### 权限要求
- SuperAdmin: 可重置所有用户密码
- Admin: 可重置Doctor和Nurse密码

#### 请求体
```json
{
  "newPassword": "NewSecurePassword456!",
  "forceChangeOnNextLogin": true,
  "reason": "密码定期更新",
  "notifyUser": true
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| newPassword | string | 是 | 新密码，需符合密码策略 |
| forceChangeOnNextLogin | boolean | 否 | 是否强制首次登录修改 |
| reason | string | 是 | 重置原因 |
| notifyUser | boolean | 否 | 是否通知用户 |

### 用户修改密码

#### 端点信息
```http
POST /api/v1/users/change-password
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword456!",
  "confirmPassword": "NewPassword456!"
}
```

### 更改个人信息

#### 端点信息
```http
PUT /api/v1/users/profile
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "phoneNumber": "13800138003",
  "email": "new.email@clinic.com",
  "department": "心内科",
  "specialties": ["心血管疾病", "高血压治疗"]
}
```

## 📊 批量操作端点

### 批量创建用户

#### 端点信息
```http
POST /api/v1/users/bulk-create
Authorization: Bearer <access_token>
Content-Type: multipart/form-data
```

#### 请求参数
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| file | file | 是 | Excel文件（.xlsx格式） |
| options | string | 否 | 操作选项JSON |

#### 选项配置
```json
{
  "dryRun": false,
  "skipDuplicates": true,
  "defaultPassword": "TempPass123!",
  "forcePasswordChange": true,
  "sendNotification": true,
  "batchSize": 50
}
```

### 批量更新用户

#### 端点信息
```http
PUT /api/v1/users/bulk-update
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "filter": {
    "userIds": ["uuid1", "uuid2"],
    "role": "Doctor"
  },
  "updates": {
    "department": "中西医结合科",
    "title": "主治医师"
  },
  "options": {
    "dryRun": false,
    "sendNotification": true
  }
}
```

### 批量删除用户

#### 端点信息
```http
DELETE /api/v1/users/bulk-delete
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "userIds": ["uuid1", "uuid2", "uuid3"],
  "deleteMode": "Soft",
  "archiveData": true,
  "reason": "员工离职"
}
```

## 📈 统计和分析端点

### 用户统计信息

#### 端点信息
```http
GET /api/v1/users/statistics
Authorization: Bearer <access_token>
```

#### 查询参数
```
groupBy=role&dateRange=last30days&includeInactive=true
```

#### 响应格式
```json
{
  "success": true,
  "message": "统计查询成功",
  "data": {
    "summary": {
      "totalUsers": 156,
      "activeUsers": 142,
      "inactiveUsers": 14,
      "newUsersThisMonth": 8,
      "totalLoginsThisMonth": 2845
    },
    "groupByRole": [
      {
        "role": "Doctor",
        "count": 45,
        "activeCount": 42,
        "percentage": 28.8
      },
      {
        "role": "Nurse",
        "count": 68,
        "activeCount": 65,
        "percentage": 43.6
      }
    ],
    "loginStatistics": [
      {
        "date": "2025-01-01",
        "loginCount": 156,
        "uniqueUsers": 89
      }
    ]
  }
}
```

### 用户活跃度分析

#### 端点信息
```http
GET /api/v1/users/activity
Authorization: Bearer <access_token>
```

#### 查询参数
```
period=last90days&role=Doctor&minLoginCount=10
```

#### 响应格式
```json
{
  "success": true,
  "message": "活跃度分析完成",
  "data": {
    "period": "last90days",
    "totalUsers": 45,
    "activeUsers": 38,
    "inactiveUsers": 7,
    "averageLoginFrequency": 12.5,
    "topActiveUsers": [
      {
        "userId": "uuid1",
        "userName": "doctor_wang",
        "realName": "王医生",
        "loginCount": 156,
        "lastLoginTime": "2025-01-01T09:30:00Z"
      }
    ]
  }
}
```

## 🔧 系统管理端点

### 下载用户模板

#### 端点信息
```http
GET /api/v1/users/template
Authorization: Bearer <access_token>
```

#### 查询参数
```
type=import&language=zh-CN
```

### 导出用户数据

#### 端点信息
```http
GET /api/v1/users/export
Authorization: Bearer <access_token>
```

#### 查询参数
```
format=excel&filter=role:Doctor&fields=userName,realName,phoneNumber,email,role,status
```

### 获取操作历史

#### 端点信息
```http
GET /api/v1/users/{userId}/history
Authorization: Bearer <access_token>
```

#### 查询参数
```
operationType=Create,Update&startDate=2024-01-01&endDate=2024-12-31&pageIndex=1&pageSize=20
```

## 🛡️ 安全端点

### 验证用户权限

#### 端点信息
```http
POST /api/v1/users/validate-permission
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "targetUserId": "00000000-0000-0000-0000-000000000001",
  "operation": "Update",
  "resource": "User"
}
```

### 检查用户状态

#### 端点信息
```http
GET /api/v1/users/{userId}/status
Authorization: Bearer <access_token>
```

#### 响应格式
```json
{
  "success": true,
  "message": "状态查询成功",
  "data": {
    "userId": "00000000-0000-0000-0000-000000000001",
    "userName": "doctor_wang",
    "status": "Active",
    "isLocked": false,
    "lockoutEnd": null,
    "failedLoginCount": 0,
    "lastLoginTime": "2025-01-01T09:30:00Z",
    "passwordExpired": false,
    "mustChangePassword": false
  }
}
```

## 🔄 API版本控制

### 版本策略
- **当前版本**: v1
- **版本格式**: URL路径版本控制 (`/api/v{version}`)
- **向后兼容**: 保持API向后兼容性
- **弃用通知**: 提前3个月通知API变更

### 版本变更记录
| 版本 | 发布日期 | 主要变更 | 兼容性 |
|------|----------|----------|--------|
| v1.0 | 2025-01-01 | 初始版本 | - |
| v1.1 | 计划中 | 添加批量操作API | 向后兼容 |
| v1.2 | 计划中 | 增强搜索功能 | 向后兼容 |

## 📊 错误处理

### 标准错误响应格式
```json
{
  "success": false,
  "message": "操作失败",
  "errors": [
    {
      "code": "USER_NOT_FOUND",
      "message": "指定的用户不存在",
      "field": "userId",
      "value": "invalid-uuid"
    }
  ],
  "timestamp": "2025-01-01T12:00:00Z",
  "requestId": "req_123456789"
}
```

### 常见错误代码
| 错误代码 | HTTP状态码 | 描述 | 解决方案 |
|----------|------------|------|----------|
| USER_NOT_FOUND | 404 | 用户不存在 | 检查用户ID是否正确 |
| DUPLICATE_USERNAME | 409 | 用户名已存在 | 使用不同的用户名 |
| INVALID_PASSWORD | 400 | 密码不符合策略 | 检查密码复杂度要求 |
| INSUFFICIENT_PERMISSIONS | 403 | 权限不足 | 确认用户角色和权限 |
| USER_LOCKED_OUT | 423 | 用户账户被锁定 | 联系管理员解锁 |
| INVALID_ROLE | 400 | 无效的角色 | 使用有效的角色名称 |

## 🔗 相关资源

### API文档
- [认证API参考](auth.md)
- [角色权限API](authorization.md)
- [批量操作API](bulk-operations.md)

### 技术规范
- [REST API设计规范](../technical-specs/rest-api.md)
- [数据模型说明](../technical-specs/entity-models.md)
- [错误处理指南](../technical-specs/error-handling.md)

### 外部资源
- [ASP.NET Core Web API文档](https://docs.microsoft.com/aspnet/core/web-api/)
- [JWT认证实现](../explanation/technology/jwt-implementation.md)
- [RBAC权限系统](../explanation/architecture/rbac-system.md)

---

**文档类型**: Reference API
**API版本**: v1.0
**更新时间**: 2025-11-22
**维护团队**: 架构组 + API团队
**测试覆盖**: 100%