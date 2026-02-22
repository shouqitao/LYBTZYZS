# ADR-0008: Token 安全防御性设计

## 状态

已接受 (2026-02-21)

## 背景

当前系统部署规模为单诊所 3-5 人。Token 安全机制 (FamilyId, Token 轮换, 重放攻击检测) 对此规模可能显得"过度设计"。

## 决策

**保留现有 Token 安全机制**，定位为"防御性设计"。

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

### 4. 审计合规

医疗系统对安全审计有更高要求。Token 重放检测提供了"异常登录行为"的检测能力。

## 后果

- Token 刷新流程比简单方案多 1 次 DB 写入 (标记 IsUsed + 创建新 Token)
- 数据库 RefreshTokens 表需定期清理过期记录 (已有 `CleanupExpiredTokensAsync` 实现)
- 新开发人员需理解 FamilyId 概念 (本 ADR 作为文档入口)

## 关联

- `2026-02-21-system-architecture-diagrams.md` Section 4.3: Token 生命周期状态图
- `AuthService.cs`: FamilyId 和 Token 轮换实现
- `ITokenRevocationService.cs`: Token 撤销接口
