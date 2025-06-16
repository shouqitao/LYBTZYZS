# LYBT.Module.Registration

## 功能简介

- 提供挂号管理的领域模型、DTO、服务接口与实现、仓储接口与实现
- 支持挂号的增删改查等基本业务
- 代码全部带中文注释，分层清晰、易维护

## 主要文件结构

- Enums/              挂号类型与状态枚举
- Models/             实体模型与 DTO
- Interfaces/         服务与仓储接口
- Services/           业务逻辑服务
- Repositories/       数据持久化实现
- Extensions/         实体与 DTO 映射扩展

## 接口说明

- `IRegistrationService`：挂号业务逻辑接口
- `IRegistrationRepository`：数据库仓储接口，基于 `AppDbContext`
