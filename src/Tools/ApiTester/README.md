# ApiTester

> WebAPI 端点手动测试工具，用于验证 API 功能是否正常工作

## 用途

通过 HttpClient 直接调用 Server 端 API 端点进行功能验证。当前实现的测试场景:

- 使用 sysadmin 管理员账号登录获取 JWT Token
- 调用用户密码重置端点 (`/api/v1/users/{id}/reset-password`)
- 验证 API 响应状态和返回内容

适用于开发调试阶段，快速验证 API 端点的连通性和业务逻辑正确性。

## 使用方式

```
dotnet run --project src/Tools/ApiTester/
```

前置条件:
- Server 端 (LYBT.WebAPI) 已在 `https://localhost:5001` 启动
- sysadmin 管理员账号可正常登录

## 目录结构

```
ApiTester/
└── Program.cs    # API 登录 + 密码重置测试逻辑
```

## 技术信息

- **目标框架**: net8.0 (控制台应用)
- **项目依赖**: 无 (独立工具，仅使用 System.Net.Http / System.Text.Json)
- **关联工具**: LoginTester (密码重置后登录验证)

## 注意事项

- 已禁用 HTTPS 证书验证 (开发环境专用)
- 测试账号和密码硬编码在源码中，仅限开发环境使用
- 自定义 JsonSerializer 静态类遮蔽系统类型，需使用全限定名反序列化

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建文档 |

## 开发笔记

# ApiTester 代码知识

通过 WebAPI 登录并执行密码重置操作的控制台测试工具。

## 代码文件结构

```
├── Program.cs        # API登录 + 密码重置测试逻辑
```

### Program.cs
**Program** | 连接 WebAPI (https://localhost:5001)，使用 sysadmin 登录获取 JWT Token，然后调用密码重置接口重置指定用户密码

| 方法 | 说明 |
|------|------|
| Main(args) | 创建 HttpClient (跳过SSL验证)，POST 登录获取 Token，依次对 shouqitao 和 jjr 调用 `/api/v1/users/{id}/reset-password` |

**JsonSerializer** (静态类) | 封装 System.Text.Json.JsonSerializer，提供 camelCase 序列化配置

| 方法 | 说明 |
|------|------|
| Serialize\<T\>(obj) | 使用 camelCase + 大小写不敏感配置序列化对象 |

## 硬编码值

| 值 | 位置 | 说明 |
|----|------|------|
| `https://localhost:5001` | BaseAddress | WebAPI 地址 |
| `sysadmin` / `LybtAdmin2025@SecurePass#` | loginData | 管理员登录凭据 |
| `4b27657a-a128-4c5c-a7b0-ceb477f99bfe` | shouqitaoId | shouqitao 用户 GUID |
| `dd384a4f-05ad-498e-b13e-ea27a7ad57c1` | jjrId | jjr 用户 GUID |

## 死代码与废弃标记

(无)

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| SSL 证书验证被跳过 | ServerCertificateCustomValidationCallback 返回 true | 仅限开发环境使用，生产环境不适用 |
| 密码明文硬编码 | 一次性测试工具 | 不应提交到生产分支 |
| 自定义 JsonSerializer 遮蔽系统类型 | 命名空间内定义同名静态类 | Main 方法中使用全限定名 System.Text.Json.JsonSerializer 反序列化 |
