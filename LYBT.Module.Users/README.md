# LYBT.Module.Users

## 主要功能



## 结构说明

- Dtos/             用户相关 DTO
- Interfaces/       服务与仓储接口
- Repositories/     数据访问实现
- Services/         业务逻辑实现
- Mapping/          AutoMapper 配置
- UsersModule.cs    模块注册入口

## 依赖

- Microsoft.EntityFrameworkCore
- AutoMapper
- LYBT.Common 提供的枚举和辅助类

本模块的 DTO 均使用数据注解进行参数校验，服务接口统一采用异步方式。

自 `v2` 起，用户支持多角色分配，`UserCreateDto` 和 `UserEditDto` 均采用
`Roles` 列表并要求至少包含一个角色。

新增用户时系统会自动生成初始密码，其规则可在 `appsettings.json` 的
`UserDefaults` 节点中配置（如 `InitialPasswordLength`）。管理员需告知
用户此密码以便首次登录后修改。重置密码接口仍需明确提供新密码。

