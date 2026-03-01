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
