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

## 已实现功能
- **Auth**：登录、登出、修改管理员密码。
- **Users**：用户查询、添加/修改、启用/禁用、批量操作、重置密码、角色获取等。
- **Patients**：病人增删改查、批量处理、医生分配、导入导出、历史记录查询。
- **Billing**：费用结算列表、详情、新增/编辑/删除，标记支付、完成、退款和取消等。
- **Pharmacy**：待抓药列表、药房单增删改查、标记处方已抓药。
- **Queueing**：排队列表和详情、新增/编辑/删除、取消排队。
- **DiagnosisTreatment**：诊疗记录增删改查。
- **Doctors/Registration/Prescriptions/Herbs/FormulaTemplates/Records/Settings/Sync** 等模块亦提供各自的 CRUD 与状态处理接口。
