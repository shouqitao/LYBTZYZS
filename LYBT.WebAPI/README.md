# LYBT.WebAPI

ASP.NET Core API 项目，负责整合所有业务模块并向客户端提供 REST 接口。

创建用户时，系统会根据 `appsettings.json` 中 `UserDefaults:DefaultUserPassword` 的设置自动生成初始密码。重置密码操作仍需在请求体中提供新密码。
