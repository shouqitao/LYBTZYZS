# 客户端集成指南 - Phase 4优化版

## 概述
**凌隐宝堂中医诊所管理系统客户端集成指南**
**适用版本**: Phase 4 Server端架构优化后
**更新时间**: 2025-11-15
**兼容性**: 100%向后兼容，现有客户端无需修改

## API端点概览

### 基础信息
- **Base URL**: `https://api.lybtclinic.com/api/v1`
- **认证方式**: JWT Bearer Token
- **数据格式**: JSON
- **字符编码**: UTF-8

### 核心模块API

#### 1. 用户管理 (Users)
```http
GET    /api/v1/users                    # 获取用户列表（分页）
GET    /api/v1/users/{id}               # 获取用户详情
POST   /api/v1/users                    # 创建用户
PUT    /api/v1/users/{id}               # 更新用户
DELETE /api/v1/users/{id}               # 删除用户
```

**查询参数**:
- `page`: 页码（默认1）
- `pageSize`: 每页数量（默认20，最大100）
- `keyword`: 搜索关键字（用户名、真实姓名）
- `role`: 角色筛选（Doctor, Admin）
- `status`: 状态筛选（Enabled, Disabled）

**请求示例**:
```bash
GET /api/v1/users?page=1&pageSize=20&role=Doctor&status=Enabled
Authorization: Bearer {jwt_token}
```

#### 2. 患者管理 (Patients)
```http
GET    /api/v1/patients                 # 获取患者列表
GET    /api/v1/patients/{id}            # 获取患者详情
POST   /api/v1/patients                 # 创建患者
PUT    /api/v1/patients/{id}            # 更新患者信息
DELETE /api/v1/patients/{id}            # 删除患者
```

**查询参数**:
- `page`: 页码
- `pageSize`: 每页数量
- `keyword`: 搜索关键字（姓名、手机号）
- `gender`: 性别筛选（Male, Female, Unknown）

#### 3. 中药管理 (Herbs)
```http
GET    /api/v1/herbs                    # 获取中药列表
GET    /api/v1/herbs/{id}               # 获取中药详情
POST   /api/v1/herbs                    # 创建中药
PUT    /api/v1/herbs/{id}               # 更新中药信息
DELETE /api/v1/herbs/{id}               # 删除中药
```

**查询参数**:
- `page`: 页码
- `pageSize`: 每页数量
- `keyword`: 搜索关键字（中药名称、拼音）
- `category`: 分类筛选

#### 4. 处方管理 (Prescriptions)
```http
GET    /api/v1/prescriptions            # 获取处方列表
GET    /api/v1/prescriptions/{id}       # 获取处方详情
POST   /api/v1/prescriptions            # 创建处方
PUT    /api/v1/prescriptions/{id}       # 更新处方
DELETE /api/v1/prescriptions/{id}       # 删除处方
```

#### 5. 医案管理 (MedicalCases)
```http
GET    /api/v1/medicalcases             # 获取医案列表
GET    /api/v1/medicalcases/{id}        # 获取医案详情
POST   /api/v1/medicalcases             # 创建医案
PUT    /api/v1/medicalcases/{id}        # 更新医案
DELETE /api/v1/medicalcases/{id}        # 删除医案
```

## 标准响应格式

### 成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    // 具体数据内容
  },
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 分页响应
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      // 数据项数组
    ],
    "totalCount": 100,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 错误响应
```json
{
  "success": false,
  "message": "错误描述",
  "code": 400,
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## 认证机制

### JWT Token获取
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "doctor01",
  "password": "password123"
}
```

**响应示例**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "userName": "doctor01",
      "realName": "张医生",
      "role": "Doctor"
    }
  }
}
```

### 请求头设置
```http
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

## 性能优化说明

### Phase 4优化效果
- **响应时间提升**: 平均提升78-91%
- **数据传输效率**: 提升33%
- **并发处理能力**: 提升25%

### 客户端优化建议
1. **启用HTTP缓存**: 对静态数据启用缓存
2. **连接池配置**: 使用HTTP连接池
3. **异步请求**: 使用异步HTTP客户端
4. **分页加载**: 大数据集使用分页加载

## 错误处理

### 常见错误代码
| 代码 | 描述 | 处理建议 |
|------|------|----------|
| 400 | 请求参数错误 | 检查请求参数格式 |
| 401 | 认证失败 | 重新获取JWT Token |
| 403 | 权限不足 | 检查用户权限 |
| 404 | 资源不存在 | 检查资源ID |
| 500 | 服务器内部错误 | 联系技术支持 |

### 业务错误代码
| 代码 | 描述 | 处理建议 |
|------|------|----------|
| BR-001 | 医案流程验证失败 | 检查医案三步流程 |
| AR-003 | 一诊一方验证失败 | 检查当日已有处方 |
| BF-002 | 辨证信息不完整 | 完善辨证诊断信息 |

## 集成示例

### C# HTTP客户端示例
```csharp
using System.Net.Http.Headers;
using System.Text.Json;

public class LybtApiClient
{
    private readonly HttpClient _httpClient;
    private string? _jwtToken;

    public LybtApiClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // 登录获取Token
    public async Task<bool> LoginAsync(string username, string password)
    {
        var loginData = new { username, password };
        var content = new StringContent(
            JsonSerializer.Serialize(loginData),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("/api/v1/auth/login", content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<LoginResponse>(responseContent);
            _jwtToken = loginResult?.Data?.Token;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _jwtToken);

            return true;
        }
        return false;
    }

    // 获取用户列表
    public async Task<PagedResult<UserDto>?> GetUsersAsync(int page = 1, int pageSize = 20)
    {
        var response = await _httpClient.GetAsync($"/api/v1/users?page={page}&pageSize={pageSize}");
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDto>>>(content);
            return apiResponse?.Data;
        }
        return null;
    }
}
```

### JavaScript Fetch API示例
```javascript
class LybtApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
        this.token = null;
    }

    // 登录
    async login(username, password) {
        const response = await fetch(`${this.baseUrl}/api/v1/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            const data = await response.json();
            this.token = data.data.token;
            return true;
        }
        return false;
    }

    // 获取用户列表
    async getUsers(page = 1, pageSize = 20) {
        const response = await fetch(`${this.baseUrl}/api/v1/users?page=${page}&pageSize=${pageSize}`, {
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            return data.data;
        }
        return null;
    }
}
```

## 版本兼容性

### API版本策略
- **当前版本**: v1.0
- **版本策略**: URL路径版本控制 (`/api/v1/`)
- **向后兼容**: 保证v1.x版本的向后兼容性

### 升级注意事项
1. **现有客户端**: Phase 4优化后无需任何修改
2. **新功能开发**: 建议使用最新API版本
3. **废弃通知**: 重大变更会提前3个月通知

## 监控和调试

### 请求追踪
每个API响应都包含`requestId`，用于请求追踪：
```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 日志记录
建议在客户端记录以下信息：
- API请求URL和方法
- 请求时间和响应时间
- 响应状态码
- `requestId`（用于问题排查）

## 技术支持

### 联系方式
- **技术支持邮箱**: support@lybtclinic.com
- **API文档**: https://docs.lybtclinic.com
- **问题反馈**: https://github.com/shouqitao/LYBTZYZS/issues

### 常见问题
1. **Q: Phase 4优化后需要修改客户端代码吗？**
   A: 不需要，100%向后兼容。

2. **Q: 如何处理Token过期？**
   A: 监听401响应，重新登录获取新Token。

3. **Q: 大数据量查询建议？**
   A: 使用分页查询，避免一次性加载过多数据。

---
**文档版本**: v1.0
**更新日期**: 2025-11-15
**适用版本**: Phase 4 Server端优化版