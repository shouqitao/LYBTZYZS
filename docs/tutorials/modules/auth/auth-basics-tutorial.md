# Auth模块基础教程 (Auth Module Tutorial)

> **学习导向**: 手把手掌握LYBTZYZS认证授权系统的使用和开发
> **适合人群**: 新手开发者、系统管理员、中医诊所信息化人员
> **学习时间**: 45分钟
> **难度级别**: 初级

## 🎯 学习目标

完成本教程后，您将能够：
- 理解LYBTZYZS认证授权系统的架构和核心概念
- 掌握用户登录、登出的完整流程
- 学会JWT Token的使用和管理
- 了解基于角色的权限控制(RBAC)
- 能够在开发环境中配置和使用认证功能

## 📋 前置条件

### 技术要求
- 基础的.NET开发知识
- 了解HTTP协议和REST API
- 具备基本的数据库概念
- 熟悉JSON数据格式

### 环境准备
- Visual Studio 2022或Visual Studio Code
- .NET 8.0 SDK
- SQL Server (本地或远程)
- Postman或类似API测试工具

### 项目准备
- 已克隆LYBTZYZS项目到本地
- 数据库已创建并初始化
- 开发环境已配置完成

## 🔍 核心概念理解

### 认证vs授权
- **认证 (Authentication)**: 验证用户身份，确认"你是谁"
- **授权 (Authorization)**: 确认用户权限，决定"你能做什么"

### JWT Token机制
- **JWT (JSON Web Token)**: 无状态的令牌认证机制
- **Access Token**: 短期有效(15分钟)，用于API访问
- **Refresh Token**: 长期有效(7天)，用于获取新的Access Token

### 角色权限体系
- **SuperAdmin**: 系统超级管理员
- **Admin**: 诊所管理员
- **Doctor**: 医生
- **Nurse**: 护士

## 📝 模块一：用户登录流程

### 1.1 理解登录架构

LYBTZYZS采用三层认证架构：
```
前端(WPF) → API控制器(AuthController) → 认证服务(AuthService) → 数据库
```

### 1.2 实现用户登录

#### 步骤1: 准备登录请求
```json
{
  "userName": "admin",
  "password": "password123",
  "rememberMe": true
}
```

#### 步骤2: 调用登录API
```bash
POST /api/v1/auth/login
Content-Type: application/json

{
  "userName": "admin",
  "password": "password123",
  "rememberMe": true
}
```

#### 步骤3: 接收登录响应
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcy1pcy1hLXJlZnJlc2gtdG9rZW4=",
    "expiresIn": 900,
    "user": {
      "id": "00000000-0000-0000-0000-000000000001",
      "userName": "admin",
      "displayName": "系统管理员",
      "role": "SuperAdmin",
      "isActive": true
    }
  }
}
```

### 1.3 登录流程详解

1. **参数验证**: 系统首先验证用户名和密码是否为空
2. **凭据验证**: AuthService.VerifyCredentialsAsync()验证用户凭据
3. **Token生成**: JwtService生成JWT访问令牌
4. **刷新令牌**: 生成刷新令牌并存储到数据库
5. **审计日志**: 记录登录事件到安全审计日志
6. **返回结果**: 返回令牌对和用户信息

## 📝 模块二：Token管理和使用

### 2.1 Access Token使用

在API请求中添加Authorization头：
```bash
GET /api/v1/users/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 2.2 Token刷新机制

当Access Token过期时，使用Refresh Token获取新Token：

```bash
POST /api/v1/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "dGhpcy1pcy1hLXJlZnJlc2gtdG9rZW4="
}
```

### 2.3 Token撤销

用户登出时，Token被加入黑名单：
```bash
POST /api/v1/auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "refreshToken": "dGhpcy1pcy1hLXJlZnJlc2gtdG9rZW4="
}
```

## 📝 模块三：权限控制和角色管理

### 3.1 API端点权限控制

使用Authorize特性保护API：
```csharp
[ApiController]
[Authorize]  // 默认需要认证
[Route("api/v1/[controller]")]
public class UserController : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfile>> GetProfile()
    {
        // 只有认证用户才能访问
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin,SuperAdmin")]  // 仅管理员访问
    public async Task<ActionResult<AdminData>> GetAdminData()
    {
        // 只有管理员和超级管理员才能访问
    }
}
```

### 3.2 前端权限检查

在WPF前端中检查用户权限：
```csharp
public class MainViewModel
{
    private readonly IAuthService _authService;

    public bool CanManageUsers => CurrentUser?.Role == "Admin"
                                || CurrentUser?.Role == "SuperAdmin";

    public bool CanViewReports => CurrentUser?.Role != null;

    private async Task LoadCurrentUser()
    {
        var userInfo = await _authService.GetSessionInfoAsync();
        CurrentUser = userInfo?.User;
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanViewReports));
    }
}
```

## 📝 模块四：安全特性配置

### 4.1 密码策略

系统强制执行以下密码策略：
- 最小长度：8个字符
- 包含大写字母
- 包含小写字母
- 包含数字
- 包含特殊字符

### 4.2 登录限流保护

为防止暴力破解，登录端点启用限流：
- 同一IP地址：每分钟最多5次登录尝试
- 同一用户名：每10分钟最多3次失败尝试
- 超过限制后临时锁定账户30分钟

### 4.3 安全审计日志

系统记录以下安全事件：
- 登录成功/失败
- 登出操作
- 密码修改
- 权限变更
- 异常访问尝试

## 🔧 实践练习

### 练习1: 实现登录功能
**目标**: 创建一个简单的登录界面并调用认证API

**要求**:
1. 创建WPF登录窗口
2. 实现用户名密码输入验证
3. 调用/auth/login API
4. 处理登录响应并保存Token
5. 显示用户信息和权限

### 练习2: Token自动刷新
**目标**: 实现Token过期时自动刷新机制

**要求**:
1. 监控Token过期时间
2. 在Token即将过期时自动调用刷新API
3. 更新本地存储的Token
4. 处理刷新失败的情况

### 练习3: 权限控制UI
**目标**: 根据用户角色显示/隐藏界面功能

**要求**:
1. 获取当前用户的角色信息
2. 根据角色控制菜单项显示
3. 实现按钮的权限控制
4. 处理权限不足的情况

## 🚨 常见问题和解决方案

### Q1: 登录时提示"用户名或密码错误"
**解决方案**:
1. 检查用户是否已在系统中创建
2. 确认密码是否正确
3. 检查用户账户是否被禁用
4. 查看数据库连接是否正常

### Q2: Token过期后无法访问API
**解决方案**:
1. 实现Token自动刷新机制
2. 在API调用前检查Token有效性
3. 处理401未授权响应
4. 引导用户重新登录

### Q3: 权限控制不生效
**解决方案**:
1. 确认API端点已添加Authorize特性
2. 检查用户角色是否正确设置
3. 验证前端权限检查逻辑
4. 查看Token中的角色声明

## ✅ 学习成果验证

完成以下任务以验证学习成果：

### 验证任务1: 基础认证流程
- [ ] 成功调用登录API并获取Token
- [ ] 使用Token访问受保护的API端点
- [ ] 实现用户登出功能
- [ ] 验证Token被正确撤销

### 验证任务2: 权限控制
- [ ] 创建不同角色的测试用户
- [ ] 验证角色权限控制生效
- [ ] 实现前端权限检查
- [ ] 测试权限不足的情况

### 验证任务3: 安全功能
- [ ] 测试密码策略验证
- [ ] 验证登录限流保护
- [ ] 查看安全审计日志
- [ ] 测试Token自动刷新

## 📚 后续学习路径

完成本教程后，建议继续学习：

1. **[用户管理模块教程](../users/users-management-tutorial.md)** - 了解用户创建和管理
2. **[安全最佳实践指南](../../how-to-guides/security-best-practices.md)** - 深入学习系统安全
3. **[API权限控制详解](../../explanation/architecture/rbac-system.md)** - 理解RBAC权限系统
4. **[JWT技术深度解析](../../explanation/technology/jwt-implementation.md)** - 了解JWT实现细节

## 🔗 相关资源

### 技术文档
- [Auth API参考文档](../../reference/api/auth.md)
- [认证配置指南](../../reference/configuration/authentication.md)
- [安全审计日志说明](../../reference/business-rules/security-audit.md)

### 开发资源
- [认证服务源码](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Server/Modules/LYBT.Module.Auth)
- [API控制器源码](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Server/Services/LYBT.WebAPI/Controllers)
- [前端认证组件](https://github.com/shouqitao/LYBTZYZS/tree/main/src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/Auth)

### 外部资源
- [JWT官方文档](https://jwt.io/)
- [ASP.NET Core认证文档](https://docs.microsoft.com/aspnet/core/security/authentication/)
- [OWASP认证备忘单](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

---

**文档类型**: Tutorial
**学习时间**: 45分钟
**难度级别**: 初级
**维护团队**: 架构组 + 开发团队
**更新时间**: 2025-11-22