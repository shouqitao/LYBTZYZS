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
