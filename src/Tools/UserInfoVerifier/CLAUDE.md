# UserInfoVerifier 代码知识

登录 WebAPI 获取用户详细信息并输出验证报告的控制台工具。

## 代码文件结构

```
├── Program.cs        # 登录获取Token + 用户信息查询 + 结果汇总
```

### Program.cs
**Program** | 遍历测试账号列表，登录获取 JWT Token，解析登录响应中的用户信息，再通过 `/api/v1/users/{id}` 获取完整用户详情，最终输出汇总报告

| 方法 | 说明 |
|------|------|
| Main(args) | 遍历测试账号，POST 登录获取 Token 和用户基本信息，GET 用户详情 API，输出汇总验证结果 |
| ParseUserInfo(userElement, username) | 从 JsonElement 解析用户信息到 UserInfo 对象，包含 Id/Username/RealName/Role/Status 等字段 |
| PrintUserInfo(user) | 格式化输出单个用户的完整信息 |

**UserInfo** (数据类) | 用户信息 DTO

| 属性 | 类型 | 说明 |
|------|------|------|
| Id | string | 用户 GUID |
| Username | string | 登录用户名 |
| RealName | string | 真实姓名 |
| Role | string | 用户角色 |
| Status | string | 账号状态 |
| PhoneNumber | string | 电话号码 |
| Email | string | 邮箱地址 |
| PinYinCode | string | 拼音码 |
| FailedLoginCount | int | 登录失败次数 |
| IsActive | bool | 是否激活 |
| IsEnabled | bool | 是否启用 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

**JsonSerializer** (静态类) | 封装 System.Text.Json.JsonSerializer，提供 camelCase 序列化配置

| 方法 | 说明 |
|------|------|
| Serialize\<T\>(obj) | 使用 camelCase + 大小写不敏感配置序列化对象 |

## API 调用流程

```
POST /api/v1/auth/login  -->  获取 Token + 用户基本信息
                               |
GET /api/v1/users/{id}   -->  获取用户完整详情 (使用 Bearer Token 认证)
                               |
输出汇总报告              -->  对比验证所有用户信息
```

## 硬编码值

| 值 | 位置 | 说明 |
|----|------|------|
| `https://localhost:5001` | BaseAddress | WebAPI 地址 |
| `shouqitao` / `Lybt2025@TempPass#` | testAccounts | 测试账号1 |
| `jjr` / `Lybt2025@TempPass#` | testAccounts | 测试账号2 |

## 死代码与废弃标记

(无)

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| SSL 证书验证被跳过 | ServerCertificateCustomValidationCallback 返回 true | 仅限开发环境使用 |
| HttpClient 认证头共享 | 循环中复用同一 HttpClient 实例，每次迭代末尾清除 Authorization | 如果中途异常退出，下一次迭代可能携带上一用户的 Token |
| 自定义 JsonSerializer 遮蔽系统类型 | 命名空间内定义同名静态类 | Main 方法中使用全限定名 System.Text.Json.JsonSerializer 反序列化 |
| UserInfo 属性可变 | 所有属性使用 get/set 且有默认值 | 作为一次性测试工具可接受，不影响功能 |
