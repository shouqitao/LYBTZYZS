# LYBT医疗系统 - Postman API测试指南

## 🚀 快速开始

### 环境配置

- **Base URL**: `http://localhost:5297`
- **API Version**: `v1`
- **Content-Type**: `application/json`

### 认证方式

系统使用JWT Bearer Token认证。需要先登录获取token，然后在后续请求中添加Authorization头。

## 📋 测试流程

### 1. 健康检查测试

**基础健康检查**

```
GET {{baseUrl}}/api/health
```

**数据库健康检查**

```
GET {{baseUrl}}/api/health/database
```

**详细系统状态**

```
GET {{baseUrl}}/api/health/detailed
```

### 2. 认证系统测试

#### 2.1 密码哈希测试

```
GET {{baseUrl}}/api/v1/Auth/hashPassword?password=Admin@123456
```

#### 2.2 管理员登录

```
POST {{baseUrl}}/api/v1/Auth/login
Content-Type: application/json

{
    "username": "sysadmin",
    "password": "Admin@123456",
    "rememberMe": true,
    "loginType": "Password"
}
```

**成功响应示例:**

```json
{
    "success": true,
    "message": "操作成功",
    "data": {
        "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "user": {
            "id": "ccbb14f9-06c6-491e-ba64-d359f5ad72d2",
            "userName": "sysadmin",
            "realName": "系统管理员",
            "role": "Admin",
            "isActive": true,
            "createdTime": "2025-07-30T19:06:22.4503794+08:00",
            "lastLoginTime": "2025-07-30T19:06:22.5281081+08:00"
        }
    },
    "statusCode": 200,
    "timestamp": "2025-07-30T11:06:22.5312801Z"
}
```

#### 2.3 Token验证测试

登录成功后，将token保存为环境变量，用于后续API调用：

```
Authorization: Bearer {{token}}
```

### 3. 用户管理测试

#### 3.1 获取用户列表

```
GET {{baseUrl}}/api/v1/Users
Authorization: Bearer {{token}}
```

#### 3.2 创建新用户

```
POST {{baseUrl}}/api/v1/Users
Authorization: Bearer {{token}}
Content-Type: application/json

{
    "userName": "testuser",
    "realName": "测试用户",
    "email": "test@example.com",
    "phoneNumber": "13800138000",
    "password": "Test@123456",
    "role": "Doctor",
    "isActive": true
}
```

#### 3.3 获取单个用户

```
GET {{baseUrl}}/api/v1/Users/{{userId}}
Authorization: Bearer {{token}}
```

#### 3.4 更新用户信息

```
PUT {{baseUrl}}/api/v1/Users/{{userId}}
Authorization: Bearer {{token}}
Content-Type: application/json

{
    "realName": "更新后的姓名",
    "email": "updated@example.com",
    "phoneNumber": "13900139000",
    "role": "Doctor",
    "isActive": true
}
```

#### 3.5 删除用户

```
DELETE {{baseUrl}}/api/v1/Users/{{userId}}
Authorization: Bearer {{token}}
```

### 4. 患者管理测试

#### 4.1 获取患者列表

```
GET {{baseUrl}}/api/v1/Patients?page=1&pageSize=10
Authorization: Bearer {{token}}
```

#### 4.2 创建患者记录

```
POST {{baseUrl}}/api/v1/Patients
Authorization: Bearer {{token}}
Content-Type: application/json

{
    "name": "张三",
    "idCard": "123456789012345678",
    "phoneNumber": "13800138000",
    "gender": "Male",
    "birthDate": "1990-01-01",
    "address": "北京市朝阳区"
}
```

#### 4.3 搜索患者

```
GET {{baseUrl}}/api/v1/Patients/search?keyword=张三
Authorization: Bearer {{token}}
```

### 5. 诊疗记录测试

#### 5.1 创建诊疗记录

```
POST {{baseUrl}}/api/v1/ConsultationRecords
Authorization: Bearer {{token}}
Content-Type: application/json

{
    "patientId": "{{patientId}}",
    "doctorId": "{{doctorId}}",
    "symptoms": "头痛，发热",
    "diagnosis": "感冒",
    "treatment": "休息，多喝水",
    "consultationDate": "2025-07-30T10:00:00"
}
```

#### 5.2 获取患者诊疗记录

```
GET {{baseUrl}}/api/v1/ConsultationRecords/patient/{{patientId}}
Authorization: Bearer {{token}}
```

## 🔧 Postman环境变量设置

### 全局变量

1. **baseUrl**: `http://localhost:5297`
2. **token**: 登录后获取的JWT token

### 动态变量设置

在登录请求的Tests脚本中添加：

```javascript
// 保存token
if (responseBody) {
    var jsonData = JSON.parse(responseBody);
    if (jsonData.success && jsonData.data.token) {
        pm.environment.set("token", jsonData.data.token);
        pm.environment.set("userId", jsonData.data.user.id);
        console.log("Token saved:", jsonData.data.token);
    }
}
```

在创建用户/患者的Tests脚本中添加：

```javascript
// 保存创建的ID
if (responseBody) {
    var jsonData = JSON.parse(responseBody);
    if (jsonData.success && jsonData.data.id) {
        pm.environment.set("createdUserId", jsonData.data.id);
        // 或者
        pm.environment.set("createdPatientId", jsonData.data.id);
    }
}
```

## ✅ 测试检查清单

### 基础功能测试

- [ ] 健康检查端点正常
- [ ] 数据库连接状态正常
- [ ] Swagger UI可访问

### 认证功能测试

- [ ] 密码哈希生成正常
- [ ] 超级管理员登录成功
- [ ] JWT token格式正确
- [ ] Token有效期验证

### 用户管理测试

- [ ] 用户列表获取正常
- [ ] 创建用户成功
- [ ] 用户信息更新正常
- [ ] 用户删除功能正常
- [ ] 权限验证正确

### 患者管理测试

- [ ] 患者列表分页正常
- [ ] 患者创建功能正常
- [ ] 患者搜索功能正常
- [ ] 患者信息更新正常

### 诊疗记录测试

- [ ] 创建诊疗记录正常
- [ ] 获取患者记录正常
- [ ] 记录查询功能正常

## 🚨 常见问题排查

### 1. 认证失败

- 检查用户名密码是否正确
- 确认token是否已过期
- 验证Authorization头格式

### 2. 数据库连接问题

- 确认SQL Server服务运行
- 检查连接字符串配置
- 验证数据库是否存在

### 3. API响应慢

- 检查数据库性能
- 查看应用程序日志
- 监控服务器资源使用

## 📊 测试报告格式

测试完成后，记录以下信息：

- 测试时间
- 测试环境
- 成功/失败的API数量
- 具体错误信息
- 性能表现
- 建议改进点

---

**测试账户信息:**

- 用户名: `sysadmin`
- 密码: `Admin@123456`
- 角色: 超级管理员

**最后更新:** 2025-07-30