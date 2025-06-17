# LYBT.Module.Auth

用户登陆验证模块，负责校验用户名与密码是否匹配，并记录最后登录时间。
从 1.1 版本起，登录成功或失败都会自动写入 `LYBT.Module.Logs` 提供的日志表。

## 主要服务及接口
- `IAuthService` / `AuthService`
- `IAuthRepository` / `AuthRepository`

## 用法
在应用启动时调用 `AuthModule.Register(services)` 完成依赖注入，随后通过 `IAuthService.LoginAsync` 验证用户登录信息。
