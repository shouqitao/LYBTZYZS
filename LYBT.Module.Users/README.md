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
