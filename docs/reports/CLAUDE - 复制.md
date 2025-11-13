# CLAUDE.md v9.0 - Graphiti优先精简版

**核心原则：Graphiti 是项目的长期记忆，docs 是团队的公开认知，二者必须同步。Claude Code 不得猜测，必须先查 Graphiti 和 docs，任务后写回两者。**

---

## 🚀 工作流程

```
任务开始 → 📖 知识检索 → 🛠️ 执行 → 💾 记录 → 📄 文档同步 → 完成
```

### 检索阶段（RETRIEVE）

```python
search_nodes(query="命名规范", entity_types=["Preference"])
search_facts(query="模块依赖")
```

* 首选 Graphiti：查询 Preference（偏好）、Procedure（流程）、Requirement（约束）、Decision（决策）、Fact（依赖）等
* 如未命中，查 docs 目录下相关文件（如 modules、api、architecture、guides）

### 执行阶段（EXECUTE）

* 严格遵守查得内容：优先级为 Preference > Procedure > Requirement
* 发现新规则或问题解决模式，立即标记，准备写入

### 记录阶段（STORE）

```python
add_memory(
  name="Bug修复模式",
  episode_body='{"decision": "增加空值校验"}',
  source="json"
)
```

* 所有决策、偏好、重构思路、Bug模式，写入 Graphiti

### 同步阶段（SYNC）

* 所有以下改动都需更新 docs：

  * API/DTO/数据结构 → `docs/api/`, `docs/shared/`
  * 模块职责 → `docs/modules/<模块>`
  * 架构调整 → `docs/architecture/`, `docs/decisions/`
  * 新流程规范 → `docs/guides/`
  * 完结里程碑 → `docs/reports/`
* 文档说明应包含：变更背景、内容、影响、Graphiti Episode 引用（如有）

---

## 📊 项目信息

```python
owner = "shouqitao"
repo = "LYBTZYZS"
```

* 技术栈：.NET 8.0, WPF, ASP.NET Core, EF Core 8.0
* 工具：graphiti-memory ⭐、serena、filesystem、github、context7

---

## 🛠️ Graphiti 工具（与文档联动）

| 工具             | 用途     |
| -------------- | ------ |
| `search_nodes` | 查规范/约定 |
| `search_facts` | 查模块关系  |
| `add_memory`   | 存偏好/决策 |
| `get_episodes` | 查历史经验  |

> Graphiti 是动态知识库，docs 是静态规范集合。Claude Code 必须双向引用，并在任务完成时同时更新。

---

## 📚 Graphiti 实体类型

| 类型          | 示例                       |
| ----------- | ------------------------ |
| Preference  | "使用 PascalCase 命名"       |
| Procedure   | "验证必须启动 Client + Server" |
| Requirement | "禁止使用 Newtonsoft.Json"   |
| Decision    | "重构数据库为 TPT 模式"          |
| Fact        | "服务A 依赖服务B"              |

---

## ⚡ 工作流规范

### 小任务

```
Issue → 检索 → 修改 → 验证 → 存储 → 文档 → 提交
```

### 大任务（Epic）

```
Issue → 自动流转 → 多阶段提交 → 文档更新 → PR 合并
```

* 所有任务必须有 Issue；所有决策必须写入 Graphiti；所有接口/架构/模块变动必须更新 docs。

---

## 📋 质量标准（由 Graphiti 检索 Preference）

* 编码：UTF-8 BOM，命名用 PascalCase
* 异步：I/O 必须 async/await
* 提交信息引用 Issue，PR 描述说明变更点
* 验证：编译通过 + 启动运行 + 验证功能 + 数据持久 + 无误操作

---

## ❌ 禁止行为

* ❌ 不查 Graphiti 或 docs 就动手
* ❌ 无 Issue 直接改动
* ❌ 写完不存 Episode / 不更新 docs
* ❌ 改接口/架构不写 API/模块说明

---

## ✅ 一句话总结

> Graphiti 记忆项目，docs 呈现知识。每改一处，问清出处；每决一事，双处记下。
