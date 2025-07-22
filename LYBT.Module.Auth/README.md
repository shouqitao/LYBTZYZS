## AGENTS.md — 认证模块（LYBT.Module.Auth）

### 1. Agent 概述

认证模块负责全系统用户的身份验证，包括登录验证、令牌生成、登录日志写入等，是所有功能访问的安全入口。

### 2. 核心能力

- 用户名+密码登录认证
- 登录日志写入（与日志模块协作）
- 更新用户最后登录时间
- 支持注销登录
- 支持修改系统管理员密码

### 3. 输入输出规范

#### 输入

- 用户名、密码（通常为字符串）
- 登录请求参数对象

#### 输出

- 登录成功返回用户基本信息与认证 Token（如 `LoginResultDto`，含用户ID、姓名、Token 等）
- 登录失败返回错误信息或状态码

### 4. 协作与依赖模块

- **用户模块**：校验用户名与密码，读取用户信息
- **日志模块**：记录登录成功/失败等行为
- **通用模块**：响应结构、状态码等
- **基础设施模块**：持久化用户表数据（如登录时间）

### 5. 示例场景

#### 用户登录

```csharp
var result = await _authService.LoginAsync("doctor001", "123456");
if (result.Success) {
    // 获取 Token 和用户信息
}
```

#### 登录日志写入

```csharp
await _logService.WriteAsync(userId, "登录", "Auth", "Success", "登录成功");
```

### 6. 接口列表

- `Task<UserDto?> LoginAsync(LoginRequestDto dto)` - 用户登录并返回用户信息
- `Task<bool> LogoutAsync(LogoutRequestDto dto)` - 用户登出
- `Task<bool> ChangeSysAdminPasswordAsync(ChangeSysAdminPasswordDto dto)` - 修改系统管理员密码


### Web API 接口对照

| 设计接口 | 接口说明 | 状态 |
| --- | --- | --- |
| `POST /api/auth/login` | 用户登录 | 已实现 |
| `POST /api/auth/logout` | 用户登出 | 已实现 |
| `POST /api/auth/changeSysAdminPassword` | 修改 sysadmin 密码 | 已实现 |

接口数量：3
已实现 Web API 数量：3

## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
