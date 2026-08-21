# ADR-0024: 远程/离线双 JWT 密钥隔离及聚合边界标注

> 版本: v1.0 | 日期: 2026-08-21
> Status: ACCEPTED
> Related: ADR-0010 (统一服务层), ADR-0008 (Token 安全), ADR-0023 (白名单), P0-4

## Context

`architecture-deep-review-2026-08-21.md` P0-4 指出：`LocalWebAPI/Auth/LocalJwtConfig` 签发 1 年 JWT 无撤销，`SecurityAuditLog` 本地不落库，离线越权无追溯；且 `LYBT.Shared.Configuration/Options/Common/JwtOptions`（远程 30-480 分钟）与 `Options/Server/LocalJwtOptions`（本地 1 年）若配置同密钥，则离线 Token 可远程复用，安全边界失效。聚合 `MedicalCase` 可被 `CatalogCrossModuleService` 直查 `Prescriptions` 绕过。

## Decision

1. **双密钥隔离**：`Jwt:SecretKey`（远程，8h/30m）与 `LocalJwt:SecretKey`（本地 1 年）必须不同值，生产启动强校验（`LocalJwtOptionsValidator` + `JwtOptionsValidator` 双校验，`grep -c Jwt__SecretKey` 发布清单校验）。
2. **本地审计补偿**：`LocalWebAPI` 本地 `SecurityAuditLogs` 落库 SQLite（与远程同表结构），联网时由 `IAuditSyncService` 同步到远程（v2.0 完整 Sync 前先本地落库可追溯）。
3. **聚合边界标注**：`MedicalCaseRepository` 及 `MedicalCaseCommandService` 增注释“聚合根唯一入口”，跨聚合查询必须经 `IMedicalCaseCrossModuleService` 接口，禁止 `AppDbContext.Prescriptions` 直查（`CatalogCrossModuleService` 已改接口）。

## Rationale

- 同密钥复用使离线长期 Token 成为远程永久后门，隔离是最小安全代价。
- 本地审计先落库后同步，满足医疗合规可追溯，代价仅一 SQLite 表。
- 聚合标注以注释+ArchTests 为准，编译期约束优于约定，符合 ADR-0001。

## Consequences

- `LocalJwtOptionsValidator` 新增生产双密钥不同值校验（失败 `ValidateOptionsResult.Fail` → 启动 `Fatal`）。
- `01-deployment.md` 发布清单增“双密钥不同值” `grep` 校验步骤。
- `09-security-architecture.md` §双密钥隔离段为 SSOT。
- `MedicalCaseRepository.cs` 增聚合边界注释，`CatalogCrossModuleService` 直查清零（`grep` 0 命中）。

## 关联 US

- US-AUTH-012/013 本地认证 1 年、限流复用 Auth 模块 Service
- 医案聚合 US-MC-001/002 状态流转经聚合根唯一入口
