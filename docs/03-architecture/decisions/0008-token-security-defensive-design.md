# ADR-0008: Token 安全防御性设计

## 状态

已接受 (2026-02-21) · 🧲 **实现状态：v1.0 待补回（D3 B+ 方案，2026-06-28 复核）**

> **实现现状**：本 ADR 描述的 `RefreshToken` 实体、`TokenManagementService.RefreshTokenAsync`、`FamilyId` 字段在当前代码中**均不存在**（`SimplifyDataModel` 迁移曾移除，`AppDbContext` 无 `DbSet<RefreshToken>`）。
>
> **D3 B+ 决策（2026-06-28）**：保留本 ADR 作为**目标设计**，分两阶段补回：
> - **v1.0**：Token 族旋转 + 登录限流 + 登出撤销 + 安全审计日志
> - **v2.0**：FamilyId 重放检测
>
> 详见 [docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md §1 D3](../../compose/specs/2026-06-28-docs-reconciliation-baseline.md)。

## 背景

当前系统部署规模为单诊所 3-5 人。Token 安全机制 (FamilyId, Token 轮换, 重放攻击检测) 对此规模可能显得"过度设计"。

## 决策

**保留现有 Token 安全机制**（**目标态**，按 D3 B+ 分阶段补回），定位为"防御性设计"。

## 原因

### 1. 安全无"过度"

OWASP Session Management 最佳实践推荐 Token 轮换和重放检测。安全性不应以当前用户规模为折扣条件。

### 2. 面向扩展

系统设计为支持多诊所/云部署场景:
- `FamilyId` 机制在多设备登录场景下提供精确撤销能力
- Token 轮换在公网暴露场景 (云部署) 下是必要防护
- 若未来扩展到连锁诊所 (50+ 用户)，现有机制无需改动

### 3. 实现成本已沉没

Token 安全机制已实现并测试通过，维护成本极低 (仅 DB 字段 + 查询条件)。移除反而增加风险和工作量。

> 🧲 **2026-06-28 修正**：`SimplifyDataModel` 迁移曾删除 RefreshToken 表，"已沉没成本" 假设不再成立。D3 B+ 决策**重新采纳**本 ADR 的设计目标，将补回成本纳入 v1.0/v2.0 计划。

### 4. 审计合规

医疗系统对安全审计有更高要求。Token 重放检测提供了"异常登录行为"的检测能力。

## 实现状态（D3 B+ 方案）

| 功能 | 状态 | 版本 | 说明 |
|------|------|------|------|
| Token 族旋转 | 🧲 v1.0 补回 | v1.0 | 每次刷新生成新 Token，标记旧 Token IsUsed |
| 登录限流 | 🧲 v1.0 补回 | v1.0 | 5 次/60 秒，IP 固定窗口 |
| 登出撤销 | 🧲 v1.0 补回 | v1.0 | 登出时撤销该用户所有 RefreshToken |
| 安全审计日志 | 🧲 v1.0 补回 | v1.0 | 登录/登出/失败事件记录 |
| FamilyId 重放检测 | v2.0 | v2.0 | 基于 FamilyId 的异常登录检测 |
| AutoLoginToken 轮换 | v2.0 | v2.0 | 长期 Token 的定期轮换机制 |

## 依赖关系

- **依赖 ADR-0004**（用户上下文传递）：GetOperator() 提取 userId 用于 Token 操作审计
- **依赖 ADR-0005**（SuperAdmin）：SuperAdmin 和普通用户两种 Token 类型
- **被依赖于** D1（审计日志）和 D3（安全模型）的实现

## 关联

- `2026-02-21-system-architecture-diagrams.md` Section 4.3: Token 生命周期状态图
- `AuthService.cs`: FamilyId 和 Token 轮换实现（🧲 待补回）
- `ITokenRevocationService.cs`: Token 撤销接口（🧲 待补回）
- [D3 B+ 决策基线](../../compose/specs/2026-06-28-docs-reconciliation-baseline.md) §1

## 关联 US

- US-AUTH-002 / US-AUTH-003（账户锁定、登录限流）
- US-AUTH-004 / US-AUTH-006 / US-AUTH-008 / US-AUTH-010（令牌刷新 / 族旋转撤销 / 登出撤销 / AutoLoginToken 轮换）
- US-AUTH-007（安全审计日志：ISecurityAuditService）
- US-LOG-003 / US-LOG-004（敏感数据脱敏、审计日志保留期）
- US-PAT-013（患者敏感字段脱敏：[SensitiveData] 特性）
- US-SHELL-014 / US-SHELL-017（安全审计日志查看、生产环境安全门控）
