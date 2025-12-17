<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# LYBTZYZS项目配置

**项目**: 凌隐宝堂中医诊所管理系统
**版本**: v1.0.0-rc (Release Candidate)
**阶段**: Pre-Release Stabilization Phase
**技术栈**: .NET 8 + WPF + Prism + EF Core + SQL Server
**仓库**: https://github.com/shouqitao/LYBTZYZS

---

## 🎯 当前阶段: Pre-Release Stabilization

**阶段目标**: 代码冻结前的质量收敛，确保系统达到生产就绪(Production-Ready)状态

### 四大核心方向

| 方向 | 英文术语 | 具体要求 |
|------|----------|----------|
| **修复** | Defect Resolution | 消除所有已知缺陷，包括运行时异常、绑定错误、边界条件处理 |
| **完善** | Feature Completion | 补全缺失的业务逻辑、错误处理、用户反馈机制 |
| **统一** | Standardization | 统一代码风格、命名规范、架构模式、API契约 |
| **优化** | Performance Tuning | 消除性能瓶颈、减少资源占用、提升响应速度 |

### 开发准则

1. **Zero Tolerance Policy**: 禁止引入新功能(Feature Freeze)，所有变更必须服务于质量提升
2. **Root Cause Analysis**: 修复问题必须定位根因，禁止表面修补(Workaround)
3. **Regression Prevention**: 每次修改必须评估回归风险，优先保证稳定性
4. **Technical Debt Resolution**: 识别并消除技术债务，但不做过度重构

### 代码变更标准

```
[ALLOW]  Bug修复 | 绑定错误修正 | 异常处理完善 | 类型安全修复
[ALLOW]  代码规范统一 | 命名一致性调整 | 架构模式对齐
[ALLOW]  性能热点优化 | 资源泄漏修复 | 响应延迟优化
[ALLOW]  局部架构优化 | 模式一致性改进 | 接口契约规范化
[REVIEW] P0级架构缺陷修复 - 需用户审批后方可执行
[DENY]   新功能开发 | 大规模架构重构 | 技术栈变更 | 实验性代码
```

**架构变更分级**:
- **局部优化**: 单模块内的模式调整、代码组织优化 → 直接执行
- **跨模块优化**: 影响2-3个模块的接口调整 → 说明影响范围后执行
- **P0架构缺陷**: 影响系统稳定性/可维护性的根本性问题 → **必须用户审批**
- **大规模重构**: 涉及核心架构变更、技术栈调整 → **禁止**

---

## 🔍 修改前必查(铁律)

**出方案或修改代码前，必须完成以下步骤:**

1. **查记忆**: `search_memory_facts("相关关键词")` 查已有解决方案
2. **查文档**: 用context7/microsoft_docs_mcp查官方文档和最佳实践
3. **查案例**: 用tavily-search/brave-search查业界优秀实现
4. **问用户**: 方案确认后再执行，不确定必问

**禁止**: 未经调研直接编码 | 猜测方案 | 跳过用户确认 | 兼容模式(发现问题一律优化为最优模式)

---

## 🎯 UltraThink四阶段(查Graphiti)

```
search_memory_facts("LYBTZYZS-UltraThink详细执行指南")
```

**THINK(深度思考)** → **PLAN(任务规划)** → **EXECUTE(渐进执行)** → **REFLECT(总结归档)**

---

## 🧠 核心约束(查Graphiti)

```
search_memory_facts("LYBTZYZS-核心约束")
search_memory_facts("LYBTZYZS-术语规范")
search_memory_facts("LYBTZYZS-Issue自动关闭标准")
```

**关键规则**: TodoWrite必用 | Graphiti第一大脑 | Issue自动关闭(满足4标准) | Consultation仅指诊断部分

---

## 📦 架构索引(查Graphiti)

```
search_memory_facts("LYBTZYZS-前端MVVM架构")
search_memory_facts("LYBTZYZS-后端三层架构")
search_memory_facts("LYBTZYZS-数据库Schema")
search_memory_facts("LYBTZYZS-DDD聚合根设计")
```

---

最后更新: 2025-12-16 19:37
文档版本: v3.3-rc-stabilization
