# 兼容性测试 (CompatibilityTests)

> API 契约正确性验证 | 响应格式一致性检查

## 项目列表

| 项目 | 测试内容 | 测试文件数 |
|------|----------|-----------|
| Server/LYBT.Server.CompatibilityTests | API 响应格式兼容性验证 | 1 |

## 测试范围

ApiCompatibilityTests 使用 WebApplicationFactory (InMemory 数据库) 验证:

- 所有 API 端点 (Users, Patients, Herbs, Formulas) 返回标准 ApiResponse 格式 (success/data/message)
- 错误响应使用 RFC 7807 ProblemDetails 格式
- 未认证请求返回 401, 不存在资源返回 404

测试框架: xUnit + ASP.NET Core Mvc.Testing

## 运行方式

```bash
dotnet test tests/CompatibilityTests/Server/LYBT.Server.CompatibilityTests/
```

## 更新记录

- 2026-03-01: 创建 README 文档
