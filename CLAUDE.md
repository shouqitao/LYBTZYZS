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
**技术栈**: .NET 8 + WPF + Prism + EF Core + SQL Server
**阶段**: 架构功能完善期

---

## 开发准则

1. **Architecture First** - 架构完善优先，符合既定架构模式
2. **Root Cause Analysis** - 定位根因，禁止表面修补
3. **Test Coverage** - 新功能必须编写测试
4. **Documentation** - 架构决策和API变更必须更新文档

---

## 修改前必查 (铁律)

**出方案或修改代码前，必须完成:**

1. **查记忆**: `mcp__serena__list_memories()` → `read_memory("记忆名")`
2. **查文档**: context7 / microsoft_docs_mcp 查官方文档
3. **查案例**: tavily-search / brave-search 查业界实现
4. **问用户**: 方案确认后再执行，不确定必问

**禁止**: 未经调研直接编码 | 猜测方案 | 跳过用户确认

---

## 详细规则 (按需读取)

| 文件 | 内容 | 何时读取 |
|------|------|----------|
| `.claude/rules/tools.md` | Serena/Claude工具体系、选择优先级 | 需要使用工具时 |
| `.claude/rules/development-flow.md` | 统一开发流程、OpenSpec Skills | 开始新功能/重构时 |
| `.claude/rules/code-standards.md` | 代码变更标准、死代码识别 | 修改代码前 |

---

## 核心约束

- **TodoWrite必用** - 复杂任务必须创建任务列表
- **Serena记忆优先** - 重要决策存入记忆系统
- **兼容代码临时** - 必须添加注释标记，有明确移除计划

---

## 架构索引

```
mcp__serena__list_memories()  # 查看架构记忆文件
```

**主要架构**: WPF + Prism + MVVM | ASP.NET Core 三层 | EF Core + SQL Server | DDD聚合根

---

最后更新: 2026-01-17
文档版本: v4.0-simplified
