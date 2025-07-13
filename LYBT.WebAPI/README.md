# LYBT.WebAPI

LYBT.WebAPI 是一个 ASP.NET Core API，集成系统的业务模块并通过 REST 控制器对外提供接口。

## 项目概述
该应用注册位于 `LYBT.Module.*` 下的模块控制器，提供统一的 API 接口。API 使用 Entity Framework Core 进行数据访问，通过依赖注入管理服务，并使用 JWT 进行身份验证。

## 入门指南
1. 还原 NuGet 包：
   ```bash
   dotnet restore
   ```
2. 将 `appsettings.example.json` 复制为 `appsettings.json` 并更新数据库连接字符串等值。
3. 运行 API：
   ```bash
   dotnet run --project LYBT.WebAPI
   ```

## 默认密码与认证
若创建用户时未指定密码，将使用 `appsettings.json` 中 `UserDefaults:DefaultUserPassword` 的值。启用 JWT 身份验证；通过 `/api/Auth/login` 获取令牌，并在后续请求的 `Authorization` 头中加入 `Bearer <token>`。
重置密码操作需要在请求体中提供新密码。

## 控制器
`LYBT.WebAPI/Controllers` 下的控制器覆盖 Users、Patients、Registration、Billing、Prescriptions 等模块。更多各模块功能请参阅仓库 [README](../README.md)。
