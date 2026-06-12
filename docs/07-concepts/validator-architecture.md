---
type: concept
title: 验证器架构与迁移策略
tags: [validation, architecture, fluentvalidation, code-quality]
related: [coding-standards, error-handling]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/03-architecture/server.md"]
---
# 验证器架构与迁移策略

为解决验证规则分散、重复和不一致的问题，系统采用了基于 FluentValidation 的分层验证器架构。

## 分层结构

| 层级 | 位置 | 职责 | 示例 |
|------|------|------|------|
| **共享验证规则** | `Shared.Validators/` | 跨模块通用规则，无业务上下文依赖 | `PhoneNumberValidator`, `IdNumberValidator`, `ChineseNameValidator` |
| **模块验证器** | `Module.{Entity}/Validators/` | 模块专属业务规则，组合引用共享规则 | `PatientInputDtoValidator`, `MedicalCaseCreateValidator` |

## 迁移原则

1.  **提取共享**：若某验证规则被 2 个以上模块使用，必须提取至 `Shared.Validators`。
2.  **组合引用**：模块内验证器通过 `Include()` 或 `SetValidator()` 引用共享规则，避免代码复制。
3.  **单一职责**：模块验证器仅关注该模块特有的业务逻辑（如药材是否存在、医案状态是否合法）。

## 集成方式

在 Service 层的 Create/Update 方法中，验证器在业务逻辑执行前被调用：

```csharp
var validationResult = await _validator.ValidateAsync(dto);
if (!validationResult.IsValid)
    return Result<T>.Failure(validationResult.Errors);
```

## 优势

*   **一致性**：确保手机号、身份证等格式在全系统校验逻辑一致。
*   **可维护性**：规则变更只需在共享层修改一处。
*   **测试友好**：共享验证规则可独立进行单元测试。