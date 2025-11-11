# Graphiti MCP Server 使用指南

**文档版本**: v1.0
**创建日期**: 2025-11-11
**适用范围**: LYBTZYZS 项目开发团队

---

## 📋 目录

1. [快速开始](#快速开始)
2. [三阶段工作流](#三阶段工作流)
3. [检索策略](#检索策略)
4. [存储策略](#存储策略)
5. [常见场景示例](#常见场景示例)
6. [FAQ](#faq)

---

## 快速开始

### 前置条件

1. **Neo4j 数据库运行中**
   ```bash
   docker run -d \
     --name neo4j \
     -p 7474:7474 -p 7687:7687 \
     -e NEO4J_AUTH=neo4j/demodemo \
     neo4j:latest
   ```

2. **环境变量配置**（.env 文件）
   ```bash
   OPENAI_API_KEY=sk-xxxxx
   MODEL_NAME=gpt-4.1-mini
   NEO4J_URI=bolt://localhost:7687
   NEO4J_USER=neo4j
   NEO4J_PASSWORD=demodemo
   SEMAPHORE_LIMIT=10
   ```

3. **运行初始化脚本**
   ```bash
   python scripts/init_graphiti_knowledge.py
   ```

4. **验证知识导入**
   ```bash
   python scripts/verify_graphiti_knowledge.py
   ```

---

## 三阶段工作流

### 阶段1：RETRIEVE（任务前检索）

**核心原则**: 任务前必须从 Graphiti 检索相关知识

**检索步骤**:
```markdown
1. 确定任务类型
2. search_nodes 检索实体（Preference/Procedure/Requirement）
3. search_facts 检索关系（模块依赖/架构层次）
4. 整理为"任务上下文"
```

**示例：新功能开发**
```python
# 1. 检索编码规范
preferences = search_nodes(
    query="编码规范 命名规范 异步规范",
    entity_types=["Preference"],
    max_nodes=10
)

# 2. 检索工作流
procedures = search_nodes(
    query="Issue工作流 验证流程",
    entity_types=["Procedure"],
    max_nodes=5
)

# 3. 检索MVP约束
requirements = search_nodes(
    query="MVP约束 技术黑名单",
    entity_types=["Requirement"],
    max_nodes=5
)

# 4. 检索模块依赖
facts = search_facts(
    query="模块依赖 架构层次",
    max_facts=20
)
```

---

### 阶段2：EXECUTE（遵循规则）

**核心原则**: 严格遵循检索到的规则

**优先级**:
```
Preference > Procedure > Requirement
```

**禁止行为**:
- ❌ 未从 Graphiti 检索就开始任务
- ❌ 忽略检索到的规则
- ❌ 只编译通过就关闭 Issue

---

### 阶段3：STORE（知识沉淀）

**核心原则**: 任务后必须存储新知识

**存储时机**:
- ✅ 用户表达偏好/需求/流程 → 立即存储
- ✅ 任务完成后 → 存储决策理由
- ✅ 遇到 Bug 后 → 存储修复模式
- ✅ 架构调整后 → 更新依赖关系

**示例：存储决策**
```python
add_memory(
    name="Issue决策",
    episode_body=json.dumps({
        "issue_number": 1234,
        "decision": "使用EF Core而非Dapper",
        "rationale": "项目规模小,EF Core开发效率高",
        "impact": "Repository层",
        "timestamp": datetime.now(timezone.utc).isoformat()
    }, ensure_ascii=False),
    source="json",
    source_description="Issue #1234 技术选型决策",
    group_id="lybtzyzs_project"
)
```

---

## 检索策略

### 按任务类型检索

| 任务类型 | 检索实体类型 | 关键词示例 |
|---------|------------|-----------|
| **新功能开发** | Preference, Procedure, Requirement | "编码规范", "工作流", "MVP约束" |
| **Bug修复** | Procedure, Decision | "验证流程", "历史Bug模式" |
| **架构调整** | Requirement, Fact | "架构触发指标", "模块依赖" |
| **代码审查** | Preference, Procedure | "命名规范", "代码审查流程" |
| **文档更新** | Procedure, Fact | "文档同步流程", "文档位置" |
| **性能优化** | Requirement, Decision | "性能指标", "历史优化决策" |

---

### 过滤器使用

```python
# 按实体类型过滤
search_nodes(
    query="编码规范",
    entity_types=["Preference"],  # 仅检索Preference
    max_nodes=10
)

# 按类别过滤（需在episode_body中指定category字段）
search_nodes(
    query="编码规范 category:coding_style",
    max_nodes=10
)

# 时间范围过滤
from datetime import datetime, timezone, timedelta
from graphiti_core.search.search_filters import SearchFilters

now = datetime.now(timezone.utc)
one_week_ago = now - timedelta(days=7)

results = search_facts(
    query="模块依赖",
    search_filter=SearchFilters(
        valid_after=one_week_ago,
        valid_before=now
    ),
    max_facts=20
)
```

---

## 存储策略

### 存储格式

**推荐：JSON格式（结构化）**
```python
add_memory(
    name="Preference: 异步规范",
    episode_body=json.dumps({
        "name": "异步规范",
        "category": "coding_style",
        "description": "I/O操作必须使用async/await",
        "priority": 10,
        "applies_to": ["Server", "Client"]
    }, ensure_ascii=False),
    source="json",
    source_description="项目偏好",
    group_id="lybtzyzs_project"
)
```

**备选：纯文本格式**
```python
add_memory(
    name="Bug修复模式",
    episode_body="NullReferenceException通常由未检查导航属性为null引起，解决方案：使用?.运算符或显式Include",
    source="text",
    source_description="Bug修复历史教训",
    group_id="lybtzyzs_project"
)
```

---

### 批量存储（高效）

```python
from graphiti_core.utils.bulk_utils import RawEpisode
from graphiti_core.nodes import EpisodeType

bulk_episodes = [
    RawEpisode(
        name="Preference: 编码格式",
        content=json.dumps({"name": "编码格式", ...}),
        source=EpisodeType.json,
        reference_time=datetime.now(timezone.utc)
    ),
    RawEpisode(
        name="Preference: 命名规范",
        content=json.dumps({"name": "命名规范", ...}),
        source=EpisodeType.json,
        reference_time=datetime.now(timezone.utc)
    )
]

result = add_episode_bulk(
    bulk_episodes=bulk_episodes,
    group_id="lybtzyzs_project"
)
```

---

## 常见场景示例

### 场景1：新功能开发

**步骤1：检索规则**
```python
# 检索编码规范
prefs = search_nodes(query="编码规范 命名规范", entity_types=["Preference"], max_nodes=10)

# 检索工作流
procs = search_nodes(query="Issue工作流", entity_types=["Procedure"], max_nodes=5)

# 检索MVP约束
reqs = search_nodes(query="MVP技术黑名单", entity_types=["Requirement"], max_nodes=5)
```

**步骤2：实施开发**
- 遵循检索到的规则
- 创建 GitHub Issue
- 编写代码

**步骤3：存储决策**
```python
add_memory(
    name="Issue #1234 决策",
    episode_body=json.dumps({
        "issue_number": 1234,
        "decision": "使用Prism区域导航实现多页面切换",
        "rationale": "符合MVVM架构，解耦性好",
        "impact": "Client端导航架构"
    }, ensure_ascii=False),
    source="json",
    group_id="lybtzyzs_project"
)
```

---

### 场景2：Bug修复

**步骤1：检索历史Bug模式**
```python
# 检索类似Bug
bugs = search_nodes(query="NullReferenceException Bug修复", max_nodes=10)

# 检索验证流程
proc = search_nodes(query="验证流程", entity_types=["Procedure"], max_nodes=3)
```

**步骤2：修复Bug**
- 遵循验证流程
- 运行时验证（强制）

**步骤3：存储Bug模式**
```python
add_memory(
    name="Bug修复模式: NullReferenceException",
    episode_body=json.dumps({
        "bug_type": "NullReferenceException",
        "root_cause": "导航属性未Include",
        "solution": "使用.Include(x => x.NavigationProperty)",
        "affected_modules": ["Patients", "MedicalCase"],
        "prevention": "代码审查时检查所有导航属性访问"
    }, ensure_ascii=False),
    source="json",
    group_id="lybtzyzs_project"
)
```

---

### 场景3：架构调整

**步骤1：检索架构规则**
```python
# 检索架构触发指标
reqs = search_nodes(query="架构触发指标", entity_types=["Requirement"], max_nodes=10)

# 检索当前架构层次
facts = search_facts(query="三层架构 模块依赖", max_facts=30)
```

**步骤2：执行调整**
- 评估触发条件
- 实施架构变更

**步骤3：更新依赖关系**
```python
# 更新模块依赖
add_memory(
    name="Fact: 新模块依赖",
    episode_body=json.dumps({
        "subject": "Consultation模块",
        "predicate": "依赖",
        "object": "Herbs模块",
        "category": "dependency",
        "source": "Issue #1234 架构调整"
    }, ensure_ascii=False),
    source="json",
    group_id="lybtzyzs_project"
)
```

---

## FAQ

### Q1: 检索不精准怎么办？

**A**: 优化检索关键词
```python
# ❌ 不精准：关键词太宽泛
search_nodes(query="规范", max_nodes=10)

# ✅ 精准：使用多个关键词
search_nodes(query="编码规范 命名规范 UTF-8", max_nodes=10)

# ✅ 精准：指定实体类型
search_nodes(query="规范", entity_types=["Preference"], max_nodes=10)
```

---

### Q2: 如何避免知识冗余？

**A**: 检索后再存储
```python
# 1. 先检索
existing = search_nodes(query="编码规范 UTF-8", max_nodes=5)

# 2. 检查是否已存在
if not existing:
    # 3. 不存在才存储
    add_memory(...)
```

---

### Q3: 如何删除错误的知识？

**A**: 使用 delete_episode
```python
# 1. 检索到错误的episode
episodes = get_episodes(group_ids=["lybtzyzs_project"], max_episodes=20)

# 2. 找到错误的episode_uuid
for ep in episodes:
    if "错误内容" in ep.content:
        # 3. 删除
        delete_episode(uuid=ep.uuid)
```

---

### Q4: 如何清空所有知识重新导入？

**A**: ⚠️ 谨慎使用 clear_graph
```python
# 清空指定group的所有数据
clear_graph(group_ids=["lybtzyzs_project"])

# 重新运行初始化脚本
python scripts/init_graphiti_knowledge.py
```

---

### Q5: 如何查看所有已存储的知识？

**A**: 使用 get_episodes
```python
episodes = get_episodes(
    group_ids=["lybtzyzs_project"],
    max_episodes=100
)

for ep in episodes:
    print(f"{ep.name}: {ep.content[:100]}...")
```

---

## 最佳实践总结

### ✅ 推荐做法

1. **任务前必检索**：养成"检索-执行-存储"习惯
2. **使用JSON格式**：结构化存储，便于过滤
3. **关键词精准**：多个关键词 + 实体类型过滤
4. **及时存储**：发现新规则立即记录
5. **定期验证**：运行验证脚本检查知识质量

---

### ❌ 禁止做法

1. **不检索就开始**：违反 Graphiti 优先原则
2. **忽略检索结果**：降低规则遵循率
3. **纯文本长篇**：难以检索和过滤
4. **重复存储**：造成知识冗余
5. **只编译不验证**：违反质量标准

---

## 联系方式

**技术支持**: Claude Code
**文档反馈**: 通过 GitHub Issue

---

**最后更新**: 2025-11-11
**版本**: v1.0
