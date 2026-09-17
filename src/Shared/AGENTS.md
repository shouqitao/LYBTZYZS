<!-- Parent: ../AGENTS.md -->
<!-- Updated: 2026-09-16 -->

# Shared

## Purpose
跨层共享库，Server 和 Desktop 唯一允许的跨层依赖。包含领域实体、DTOs、枚举、验证器、配置、异常处理、日志和工具类。

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| LYBT.Entities/ | 领域实体（DDD 聚合根、BaseEntity、软删除/审计接口） |
| LYBT.Shared.Models/ | DTOs、Contracts、Enums、Validators、Primitives、Utilities — 主跨层数据契约 |
| LYBT.Shared.Configuration/ | 共享配置模型（Options + Validators） |
| LYBT.Shared.ExceptionHandling/ | 共享异常类型（无平台依赖） |
| LYBT.Shared.Logging/ | 日志抽象层（Serilog + 脱敏 + CorrelationId） |

## For AI Agents

### Working In This Directory
- `LYBT.Shared.Models` 是最核心的项目 — 定义所有 DTO、枚举、验证器、工具类
- 领域实体放 `LYBT.Entities/`（按业务聚合分目录）
- 新增 DTO 放 `Shared.Models/Contracts/` 按领域分目录
- 新增验证器放 `Shared.Models/Validators/`（已合并自 LYBT.Shared.Validators）
- 新增枚举放 `Shared.Models/Enums/`
- 新增共享工具类放 `Shared.Models/Utilities/`
- 配置 Options 放 `Shared.Configuration/Options/`

### Common Patterns
- **DTO pattern**: 纯数据类，无行为
- **FluentValidation**: `AbstractValidator<T>` 子类
- **Result<T>**: 领域操作结果（非 API 响应，参见 OperationResultDto）
- **ErrorCode**: 统一错误码枚举

## Dependencies

### Internal
- Models 是叶子节点（无项目引用）
- Entities → Models（P2-5-7 刻意允许：仅复用枚举等纯值类型）
- Configuration 无项目引用
- ExceptionHandling → Models
- Logging → Models

### External
- FluentValidation, hyjiacan.pinyin4net, Microsoft.Extensions.Caching.Memory（Models）
- Microsoft.Extensions.Options（Configuration）
- Serilog（Logging）

> **BCrypt.Net-Next 已移除（A-27）**：密码哈希统一走 Identity PBKDF2（UserManager）；`PasswordHelper` 仅保留生成/策略，不提供哈希。

<!-- MANUAL: -->
