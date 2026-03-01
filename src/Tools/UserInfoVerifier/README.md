# UserInfoVerifier

> 用户信息端到端验证工具，登录后获取并展示完整用户信息

## 用途

通过完整的登录 -> 获取用户信息流程，验证 WebAPI 返回的用户数据是否正确。主要功能:

- 使用指定账号登录，解析 JWT Token 和登录响应中的用户信息
- 调用 `/api/v1/users/{id}` 获取用户详情，与登录响应对比
- 输出用户的完整字段 (姓名、角色、状态、邮箱、电话、拼音码等)
- 支持多账号批量验证并汇总结果

适用于用户管理功能变更后的数据一致性验证。

## 使用方式

```
dotnet run --project src/Tools/UserInfoVerifier/
```

前置条件:
- Server 端 (LYBT.WebAPI) 已在 `https://localhost:5001` 启动
- 待验证账号已存在且密码已知

## 目录结构

```
UserInfoVerifier/
└── Program.cs    # 登录获取 Token + 用户信息查询 + 结果汇总
```

## 技术信息

- **目标框架**: net8.0 (控制台应用)
- **项目依赖**: 无 (独立工具，仅使用 System.Net.Http / System.Text.Json)
- **关联工具**: ApiTester (密码重置), LoginTester (登录验证)

## 注意事项

- 已禁用 HTTPS 证书验证 (开发环境专用)
- 测试账号列表硬编码在源码中，需根据实际情况修改
- HttpClient 认证头在循环中复用，异常退出可能携带上一用户 Token
- 仅限开发/测试环境使用

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建文档 |
