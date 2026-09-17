---
feature: docs-content-optimization
status: in-progress
updated: 2026-09-17
branch: master
commits: 408f3cf38..HEAD
---

# 全文档内容优化

## Report

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
- [ ] T1: 5 方向并行审查 (covers: S2)
- [ ] T2: 根据审查结果执行优化 (covers: S2; depends: T1)
- [ ] T3: 提交并验证 (depends: T2)
