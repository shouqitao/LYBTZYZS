---
type: concept
title: 异常类型体系
created: 2026-06-10
updated: 2026-06-10
tags: [architecture, backend, exception]
related: [error-handling, problem-details-rfc7807, medical-case-module, auth-module]
sources: ["docs/02-requirements/error-handling.md"]
---

# 异常类型体系

异常类型体系是凌隐宝堂系统统一错误处理的核心基础。通过建立分层的 `AppException` 继承树，系统能够自动将业务逻辑中的异常映射到正确的 HTTP 状态码和标准化的 `ProblemDetails` 响应中。

## 核心基类: AppException

所有自定义业务异常均继承自 `AppException`。它包含以下关键属性：
*   `ErrorCode`: 字符串类型的错误码 (如 "ERR-10101")。
*   `TypedErrorCode`: 枚举类型的错误分类。
*   `UserMessage`: 面向用户的友好消息模板。
*   `ShowDetailToUser`: 布尔值，控制是否向用户展示详细技术信息。

## 具体异常类型

| 异常类型 | HTTP 状态码 | 类别 | 说明 | 典型场景 |
| :--- | :--- | :--- | :--- | :--- |
| `BusinessException` | 400 | Business | 业务规则违反 | 医案状态转换非法、处方打印保护触发 |
| `NotFoundException` | 404 | Resource | 资源未找到 | 患者ID不存在、医案ID无效 |
| `ConflictException` | 409 | Concurrency | 并发冲突或重复 | 医案版本冲突、身份证号重复注册 |
| `ValidationException` | 400 | Validation | 输入验证失败 | 表单字段格式错误、必填项缺失 |
| `UnauthorizedException` | 401 | Authentication | 身份认证失败 | Token过期、密码错误、账户被锁定 |
| `ApiException` | Varies | External | 外部服务调用失败 | 同步模块调用远程API失败 |

## 静态工厂方法

为了简化异常创建并确保一致性，各异常类提供了静态工厂方法。例如：
*   `NotFoundException.User(guid)`: 快速创建用户未找到异常。
*   `ConflictException.MedicalCaseLocked(id)`: 快速创建医案锁定冲突异常。
*   `UnauthorizedException.InvalidPassword()`: 快速创建密码错误异常。

## 与服务端处理的集成

当 Service 层抛出这些异常时，`BusinessExceptionHandler` 会捕获它们，并根据异常类型提取 `ErrorCode` 和 `UserMessage`，构建符合 ProblemDetails RFC7807 标准的响应对象。未知异常则交由 `SystemExceptionHandler` 处理，记录 Error 级别日志并返回通用的 500 错误。

## 相关概念

*   [整体异常处理架构](error-handling.md)
*   ProblemDetails RFC7807 - 错误响应数据结构
*   [医案模块](modules/medical-case-module.md) - 医案模块特定异常 (如 `InvalidStatusTransition`)
*   [认证模块](modules/auth-module.md) - 认证模块特定异常 (如 `TokenExpired`)