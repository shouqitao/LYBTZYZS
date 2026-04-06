# LoginTester

> 登录流程批量验证工具，用于测试多个账号的登录功能

## 用途

批量测试指定账号列表的登录流程，验证:

- 用户名/密码组合能否成功通过 `/api/v1/auth/login` 认证
- 密码重置后新密码是否生效
- 登录 API 的响应格式和内容是否正确

适用于密码重置、账号批量操作后的回归验证。

## 使用方式

```
dotnet run --project src/Tools/LoginTester/
```

前置条件:
- Server 端 (LYBT.WebAPI) 已在 `https://localhost:5001` 启动
- 待测试账号已存在且密码已知

## 目录结构

```
LoginTester/
└── Program.cs    # 批量登录验证测试逻辑
```

## 技术信息

- **目标框架**: net8.0 (控制台应用)
- **项目依赖**: 无 (独立工具，仅使用 System.Net.Http / System.Text.Json)
- **关联工具**: ApiTester (密码重置操作)

## 注意事项

- 已禁用 HTTPS 证书验证 (开发环境专用)
- 测试账号列表硬编码在源码中，需根据实际情况修改
- 仅限开发/测试环境使用

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建文档 |

## 开发笔记

# LoginTester 代码知识

批量验证密码重置后的账号是否能正常登录的控制台测试工具。

## 代码文件结构

```
├── Program.cs        # 批量登录验证测试逻辑
```

### Program.cs
**Program** | 连接 WebAPI (https://localhost:5001)，遍历测试账号列表逐个执行登录，验证密码重置功能是否正常

| 方法 | 说明 |
|------|------|
| Main(args) | 创建 HttpClient (跳过SSL验证)，遍历 testAccounts 数组，对每个账号 POST `/api/v1/auth/login`，输出登录成功/失败结果 |

**JsonSerializer** (静态类) | 封装 System.Text.Json.JsonSerializer，提供 camelCase 序列化配置

| 方法 | 说明 |
|------|------|
| Serialize\<T\>(obj) | 使用 camelCase + 大小写不敏感配置序列化对象 |

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
| 密码明文硬编码 | 一次性测试工具，与 ApiTester 配合使用 | 不应提交到生产分支 |
| 自定义 JsonSerializer 遮蔽系统类型 | 命名空间内定义同名静态类 | 此工具仅用 Serialize 方法，不需要反序列化，未产生冲突 |
