# CLAUDE.md v9.0 - Graphiti优先精简版

**核心原则：Graphiti作为项目"第一大脑"，协调整个开发过程，做到不懂就问，不随便猜测。**

---

## 🚀 Graphiti 优先工作流（强制）

```
任务开始 → 📖 RETRIEVE → 🛠️ EXECUTE → 💾 STORE → 任务完成
           检索知识    遵循规则    存储决策
```

### 核心规则
- ✅ 任务前：search_nodes + search_facts 检索项目知识
- ✅ 执行中：严格遵循检索到的 Preference > Procedure > Requirement
- ✅ 任务后：add_memory 存储决策、新规则、Bug模式

---

## 📊 项目信息

### GitHub参数
```python
owner = "shouqitao"
repo = "LYBTZYZS"
```

### 技术栈
- .NET 8.0, WPF, ASP.NET Core, EF Core 8.0, Prism 8.x
- SQL Server 2022

### MCP工具优先级
1. **graphiti-memory** (⭐第一大脑)
2. serena (代码分析)
3. filesystem (文件操作)
4. github (Issue/PR)
5. context7 (官方文档)

---

## 🔍 Graphiti 三阶段工作流

### 1. RETRIEVE (任务前检索)
```python
# 检索编码规范
search_nodes(query="编码规范", entity_types=["Preference"], max_nodes=10)

# 检索流程规范
search_nodes(query="验证流程", entity_types=["Procedure"], max_nodes=5)

# 检索约束条件
search_nodes(query="MVP约束", entity_types=["Requirement"], max_nodes=5)

# 检索模块依赖
search_facts(query="模块依赖", max_facts=20)
```

### 2. EXECUTE (执行阶段)
**执行原则**：
- 严格遵循检索结果
- 优先级：Preference > Procedure > Requirement
- 发现新规则时实时存储

### 3. STORE (任务后存储)
**强制存储时机**：
- ✅ 用户表达偏好、需求、流程
- ✅ 任务完成决策
- ✅ Bug修复模式
- ✅ 架构调整记录

**存储示例**：
```python
# JSON结构化存储
add_memory(
    name="编码规范决策",
    episode_body='{"decision": "I/O操作使用async/await", "rationale": "性能优化", "applies_to": ["Server", "Client"]}',
    source="json",
    group_id="lybtzyzs_project"
)
```

---

## 📚 实体类型 (5种)

| 类型 | 用途 | 检索关键词 |
|-----|-----|-----------|
| **Preference** | 开发偏好 | "编码规范", "命名规范", "技术栈" |
| **Procedure** | 流程规范 | "工作流", "验证流程", "PR流程" |
| **Requirement** | 项目约束 | "MVP约束", "技术黑名单", "架构触发指标" |
| **Fact** | 事实关系 | "模块依赖", "架构层次", "文档位置" |
| **Decision** | 决策记录 | "Issue决策", "重构历史", "Bug模式" |

---

## ⚡ 双轨工作流

**核心规则**：所有改动必须有GitHub Issue

### 小需求 (90%) - 直接修改
```
Issue创建 → Graphiti检索 → 代码修改 → 验证 → 提交
```

### 大需求 (10%) - 自动化流程
```
Issue创建 → lybtzyzs-workflow-orchestrator skill → 14状态自动化
```

---

## 🛠️ Graphiti 工具

### 核心工具
- `add_memory`：存储知识
- `search_nodes`：搜索实体节点
- `search_facts`：搜索事实关系
- `get_episodes`：获取历史
- `delete_episode`：删除episode

### 强制更新要求
- 必须成功更新
- 异常时立即重试
- 等待"queued for processing"确认
- 失败时记录本地文件

---

## 📋 质量标准 (从Graphiti检索)

### 编码规范
- 语言：中文
- 编码：UTF-8 with BOM
- 命名：PascalCase/_camelCase
- 异步：I/O必须async/await

### 验证流程
1. 编译：0 errors, 0 warnings
2. 启动：Client + Server
3. 测试：真实操作场景
4. 验证：数据库状态
5. 确认：用户视角功能完整

---

## ⚠️ 禁止行为

### 工作流
- ❌ 未检索就开始任务
- ❌ 无Issue就修改代码
- ❌ 只编译通过就关闭Issue

### 知识管理
- ❌ 任务后不存储决策
- ❌ 发现新规则不记录
- ❌ Bug修复后不沉淀模式

---

**最后更新**：2025-11-13 (v9.0 200行内精简版)
**核心特色**：Graphiti协调整个开发过程，知识图谱驱动，不懂就问