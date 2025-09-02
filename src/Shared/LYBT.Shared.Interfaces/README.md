# LYBT.Shared.Interfaces

凌隐宝堂中医诊所系统 - 共享接口定义项目

## 项目概述

这个项目定义了系统中所有核心接口，包括服务层接口、API客户端接口和缓存服务接口。它作为前后端之间的契约，确保接口的一致性和类型安全。

## 目录结构

```
LYBT.Shared.Interfaces/
├── Api/                    # API客户端接口（Refit生成）
│   ├── IAuthApi.cs        # 身份认证API接口
│   ├── IConsultationApi.cs # 诊断管理API接口
│   ├── IFormulaApi.cs     # 验方管理API接口
│   ├── IHerbApi.cs        # 中药材API接口
│   ├── IMedicalCaseApi.cs # 医疗案例API接口
│   ├── IPatientApi.cs     # 患者管理API接口
│   ├── IPrescriptionApi.cs # 处方管理API接口
│   └── IUserApi.cs        # 用户管理API接口
├── Services/              # 业务服务接口
│   ├── IAuthService.cs    # 认证服务接口
│   ├── IConsultationService.cs # 诊断服务接口
│   ├── IFormulaService.cs # 验方服务接口
│   ├── IHerbService.cs    # 中药材服务接口
│   ├── IMedicalCaseService.cs # 医疗案例服务接口
│   ├── IPatientService.cs # 患者服务接口
│   ├── IPrescriptionService.cs # 处方服务接口
│   └── IUserService.cs    # 用户服务接口
└── Caching/               # 缓存服务接口
    └── ISimplifiedCacheService.cs # 简化缓存服务接口
```

## 核心功能

### 1. API客户端接口 (Api/)

使用 **Refit 8.0.0** 技术栈生成类型安全的HTTP客户端接口：

#### 认证API (IAuthApi)
- **用户登录**: `/api/v1/auth/login` - JWT令牌认证
- **用户登出**: `/api/v1/auth/logout` - 会话清理
- **当前用户**: `/api/v1/auth/current-user` - 获取用户信息
- **刷新令牌**: `/api/v1/auth/refresh-token` - JWT刷新
- **修改密码**: `/api/v1/auth/change-password` - 密码更新
- **健康检查**: `/api/v1/health/alive` - 服务状态检查

#### 业务模块API
- **用户管理** (IUserApi): CRUD操作、角色分配、状态管理
- **患者档案** (IPatientApi): 患者信息、就诊历史、联系方式
- **医疗案例** (IMedicalCaseApi): 诊疗流程、状态跟踪
- **看诊诊断** (IConsultationApi): 四诊记录、辨证论治
- **处方管理** (IPrescriptionApi): 药材组合、价格计算
- **中药材库** (IHerbApi): 药材信息、单价管理
- **验方模板** (IFormulaApi): 经典验方、个人验方

### 2. 业务服务接口 (Services/)

定义前端业务逻辑层的服务契约：

#### 核心特性
- **统一返回格式**: `ServiceResult<T>` 包装所有操作结果
- **异步优先**: 所有方法支持 `async/await` 模式
- **类型安全**: 强类型参数和返回值
- **错误处理**: 统一的异常处理和错误响应

#### 主要服务接口
- **认证服务**: 登录验证、会话管理、权限检查
- **用户服务**: 账户管理、角色权限、密码策略
- **患者服务**: 档案管理、历史查询、联系维护
- **诊断服务**: 四诊录入、症状分析、治疗方案
- **处方服务**: 配方组合、配伍检查、打印输出
- **验方服务**: 模板管理、个人验方、智能推荐
- **药材服务**: 信息维护、价格管理、用法指导

### 3. 缓存服务接口 (Caching/)

#### ISimplifiedCacheService
提供简化的内存缓存操作接口：

**同步操作**:
- `Get<T>(string key)`: 获取缓存项
- `Set<T>(string key, T value, TimeSpan? expiration)`: 设置缓存
- `Remove(string key)`: 移除缓存
- `Clear()`: 清空所有缓存

**异步操作**:
- `GetAsync<T>(string key)`: 异步获取缓存
- `SetAsync<T>(string key, T value, TimeSpan? expiration)`: 异步设置
- `RemoveAsync(string key)`: 异步移除
- `GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration)`: 获取或设置（核心方法）

## 技术栈

### 依赖项
- **.NET 8.0**: 现代.NET平台
- **Refit 8.0.0**: 类型安全REST客户端生成
- **LYBT.Shared.Models**: 共享数据模型

### 设计原则
- **契约优先**: 接口定义先于实现
- **类型安全**: 强类型接口，编译时检查
- **版本兼容**: 接口变更向后兼容
- **文档化**: 完整的XML注释文档

## 集成使用

### WPF客户端集成
```csharp
// Refit客户端注册
services.AddRefitClient<IAuthApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.lybt.com"));

// 业务服务注册
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IUserService, UserService>();
```

### API调用示例
```csharp
// 使用强类型API接口
var loginResponse = await _authApi.LoginAsync(new LoginRequest 
{ 
    Username = "doctor", 
    Password = "password" 
});

// 使用业务服务接口
var result = await _userService.GetUserByIdAsync(userId);
if (result.Success)
{
    var user = result.Data;
    // 处理用户数据
}
```

### 缓存服务使用
```csharp
// 获取或设置缓存
var users = await _cacheService.GetOrSetAsync(
    "users_list", 
    async () => await _userService.GetAllUsersAsync(),
    TimeSpan.FromMinutes(10)
);
```

## API响应格式

所有API接口遵循统一的响应格式 `ApiResponse<T>`:

```json
{
    "success": true,
    "message": "操作成功",
    "data": { /* 业务数据 */ },
    "timestamp": "2025-01-01T10:30:00Z",
    "requestId": "req-123456"
}
```

## 错误处理

### ServiceResult<T> 模式
```csharp
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public string ErrorCode { get; set; }
}
```

### 统一异常处理
- **ValidationException**: 参数验证错误
- **BusinessException**: 业务逻辑错误  
- **UnauthorizedException**: 认证授权错误
- **NotFoundException**: 资源不存在错误

## 开发指南

### 添加新接口
1. **API接口**: 在 `Api/` 目录添加，使用Refit特性
2. **服务接口**: 在 `Services/` 目录添加，返回 `ServiceResult<T>`
3. **XML注释**: 为所有公共成员添加完整注释
4. **单元测试**: 在对应测试项目中添加接口测试

### 版本管理
- **接口变更**: 遵循语义化版本控制
- **向后兼容**: 新增接口成员，避免破坏性变更
- **弃用处理**: 使用 `[Obsolete]` 特性标记过时接口

## 质量保证

### 代码规范
- **命名约定**: 接口以 `I` 开头，使用PascalCase
- **方法命名**: 动词开头，明确表达操作意图
- **参数验证**: 在接口文档中明确参数约束
- **异常文档**: 记录可能抛出的异常类型

### 测试覆盖
- **Mock测试**: 使用接口进行单元测试
- **集成测试**: 验证API接口与后端的集成
- **契约测试**: 确保接口定义与实现一致

## 相关文档

- [LYBT.Shared.Models](../LYBT.Shared.Models/README.md) - 共享数据模型
- [API标准规范](../../docs/api/api-standards.md) - API设计标准
- [前后端契约规范](../../docs/前后端契约规范.md) - 接口契约定义

---

**项目状态**: ✅ 生产就绪 | **最后更新**: 2025-01-01