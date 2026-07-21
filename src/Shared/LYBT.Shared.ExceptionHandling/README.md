# LYBT.Shared.ExceptionHandling

> 共享异常类型库 | 无平台依赖

## 项目定位

- **层级**: Shared
- **职责**: 提供统一的异常类型层次（AppException 及子类）
- **状态**: Active

## 目录结构

```
LYBT.Shared.ExceptionHandling/
└── Exceptions/
    ├── Base/AppException.cs              # 基础异常 (TypedErrorCode)
    ├── Business/                         # 业务异常 (4类)
    │   ├── BusinessException.cs
    │   ├── ConflictException.cs
    │   ├── NotFoundException.cs
    │   └── ValidationException.cs
    ├── External/ApiException.cs          # 外部 API 异常
    ├── Factory/ExceptionFactory.cs       # 异常工厂
    └── Security/UnauthorizedException.cs # 认证异常
```

## 核心类型

| 名称 | 说明 |
|------|------|
| AppException | 基础异常，携带 TypedErrorCode/UserMessage |
| BusinessException | 业务规则违反 (HTTP 400) |
| NotFoundException | 资源未找到 (HTTP 404) |
| ConflictException | 资源冲突 (HTTP 409) |
| ValidationException | 数据验证失败 (HTTP 400) |
| UnauthorizedException | 未授权 (HTTP 401) |
| ApiException | 外部 API 调用异常 |
| ExceptionFactory | 按领域分组的便捷创建方法 |

## 平台特定代码位置

| 组件 | 位置 |
|------|------|
| Server 异常处理器 | `LYBT.Infrastructure.ExceptionHandling` |
| Desktop 异常处理器 | `LYBT.Desktop.Infrastructure.ExceptionHandling` |
| 客户端错误消息映射 | `LYBT.Desktop.Foundation.ExceptionHandling` |

## 依赖关系

### 依赖
- LYBT.Shared.Models (ErrorCode 枚举, DTO 类型)

### 被依赖
- LYBT.Infrastructure (Server)
- LYBT.Desktop.Foundation (Desktop)
- LYBT.Desktop.Infrastructure (Desktop)
