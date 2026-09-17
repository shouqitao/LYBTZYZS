---
feature: docs-content-optimization
status: delivered
updated: 2026-09-17
branch: master
commits: 408f3cf38..HEAD
---

# 全文档内容优化

## Report

**What was built** — 全文档内容优化，按 5 个方向（SSOT 一致性/结构直观性/交叉引用完整性/内容时效性/可读性）审查并优化。5 个审查代理并行扫描，3 个优化代理实施修复 + 1 个可读性代理实施 P0 修复。

**优化内容**：
- **SSOT 权威层**：01-system-overview/03-server/04-permissions/04-data-model/06-error-handling/09-security 按代码实际状态更新（Shared 5 项目、Server 6 模块、8 策略、PBKDF2、删除虚构错误码）
- **API 文档**：策略/端点/控制器名/权限按代码对齐；删除不存在的引用
- **AGENTS.md**：控制器数/模块名/端口/路径全部更新
- **交叉引用**：9 处 wiki 死链修复、ADR-0014 损坏链接修复、13a/13b 瘦身
- **结构**：5 份长文档补 TOC、版本头同步、关键关联补全
- **可读性**：ADR 索引完整化、根 README 重写、术语统一、TL;DR 补充

**Verification** — 纯文档变更，无需 dotnet build。grep 验证：wiki 死链清零、虚构错误码清零、过时模块名清零。

**Journey log** —
- 权威层自身陈旧是最大风险（01-system-overview 被标 SSOT 但内容落后代码 6 个月）
- 策略名实不符（DoctorOrReceptionist 实含四角色）导致文档反复抄错
- 视图层（12/13a/13b）禁止复制字段/权限表，改为引用权威+代码
- CatalogController 已删除但文档仍引用——结构变更必须同步文档

## [S1] Problem

项目有 247 个文档，存在重复定义、格式不一、死链、过时内容、冗余等问题。需要按 5 个方向审查并优化。

## [S2] Design

### 优化方向
1. **SSOT 一致性** — 消除重复定义，每个信息点一个权威来源
2. **结构直观性** — 统一文档模板（标题层级/元信息/目录）
3. **交叉引用完整性** — 修复死链，补充缺失关联
4. **内容时效性** — 更新过时描述，标记待验证项
5. **可读性** — 精简冗余，突出关键信息

### 参考案例
- Stripe 文档 SSOT 模式
- Microsoft .NET 文档结构
- Diátaxis 框架
- ADR 状态标记
- GitHub README 最佳实践

## [S3] Out of Scope

- 不删除文档（只优化内容）
- 不改变文档目录结构（已整理完成）
- compose/archive/ 不优化（历史归档）

## Tasks
- [x] T1: 5 方向并行审查 (covers: S2)
- [x] T2: 根据审查结果执行优化 (covers: S2; depends: T1)
- [x] T3: 提交并验证 (depends: T2)
