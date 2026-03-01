# LYBT.Shared.ExceptionHandling

> 统一异常体系 | Server/Desktop 双端处理 | ProblemDetails 标准化

## 项目定位

- **层级**: Shared
- **职责**: 提供统一的异常类型层次、Server 中间件处理、Desktop 异常处理器、错误消息映射
- **状态**: Active

## 目录结构

```
LYBT.Shared.ExceptionHandling/
├── Exceptions/
│   ├── Base/AppException.cs              # 基础异常 (TypedErrorCode)
│   ├── Business/                         # 业务异常 (4类)
│   │   ├── BusinessException.cs
│   │   ├── ConflictException.cs
│   │   ├── NotFoundException.cs
│   │   └── ValidationException.cs
│   ├── External/ApiException.cs          # 外部 API 异常
│   ├── Factory/ExceptionFactory.cs       # 异常工厂
│   └── Security/UnauthorizedException.cs # 认证异常
├── Extensions/                           # DI 注册扩展
├── Handlers/
│   ├── Desktop/                          # Desktop 异常处理器
│   │   ├── IDesktopExceptionHandler.cs
│   │   ├── DesktopExceptionHandler.cs
│   │   └── ExceptionSeverity.cs
│   └── Server/                           # Server 中间件
│       ├── BusinessExceptionHandler.cs
│       └── SystemExceptionHandler.cs
├── Mappers/                              # 错误消息映射 (5个)
└── ProblemDetails/                       # RFC 7807 标准化 (4个)
```

## 核心接口

| 名称 | 说明 |
|------|------|
| AppException | 基础异常，携带 TypedErrorCode/UserMessage |
| IDesktopExceptionHandler | Desktop 端异常处理抽象 |
| IErrorMessageMapper | 错误码到用户消息的映射 |
| ProblemDetailsFactory | RFC 7807 ProblemDetails 生成 |
| ExceptionFactory | 按 ErrorCode 创建类型化异常 |

## 设计依据

- 异常层次: AppException -> Business/Security/External 分类处理
- Server 使用 ASP.NET Core 中间件 + ProblemDetails (RFC 7807)
- Desktop 使用 IDesktopExceptionHandler + ExceptionSeverity 分级
- 统一两端异常体系，Server 用中间件 + ProblemDetails，Desktop 用 IDesktopExceptionHandler + 分级

## 依赖关系

### 依赖
- LYBT.Shared.Models (DTO 类型)
- LYBT.Shared.Primitives (ErrorCode 枚举)

### 被依赖
- LYBT.Infrastructure (Server 中间件注册)
- LYBT.WebAPI (异常处理管道)
- LYBT.Desktop.Foundation (Desktop 异常处理注册)
- LYBT.Desktop.Infrastructure (UI 层异常展示)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 创建 README |
| 2026-01 | ConfigurableErrorMessageMapper 添加 |
| 2025-12 | 从 Shared.Models 迁移异常类型 |
