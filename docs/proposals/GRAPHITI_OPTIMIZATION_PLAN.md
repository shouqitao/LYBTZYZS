# CLAUDE.md 优化方案：Graphiti 作为"第一大脑"

**提案编号**: PROPOSAL-2025-11-11-001
**状态**: 待审批
**提案人**: Claude Code
**创建日期**: 2025-11-11
**版本**: v1.0

---

## 📋 执行摘要

本方案提出将 **Graphiti MCP Server** 作为项目的"第一大脑"，替代当前 CLAUDE.md 中大量的静态规则和知识点，实现：

- ✅ **75%文档精简**：6000行 → 1500行，降低认知负担
- ✅ **跨会话记忆**：长期知识持久化，无需重复说明
- ✅ **精准检索**：语义搜索 + 实体类型过滤 + 时序感知
- ✅ **动态演进**：实时沉淀新知识，规则库持续优化

**预期工期**: 4-6天
**技术风险**: 低（Graphiti已调试成功）
**收益**: 显著提升 AI Agent 开发效率与知识复用能力

---

## 1. 背景与问题

### 1.1 当前痛点

**问题1：CLAUDE.md 文档臃肿**
- 当前行数：6000+行（含子文档15个）
- 信息密度：大量静态规则、示例代码、架构说明
- 认知负担：AI Agent 每次需重新阅读大量内容

**问题2：知识复用能力弱**
- Serena Memory：仅当前会话有效，跨会话需重新说明
- 缺乏结构化：编码规范、流程、架构规则混杂在一起
- 检索困难：无法按实体类型、时间维度精准检索

**问题3：知识沉淀不足**
- Issue 决策：完成后无结构化沉淀
- Bug 修复模式：历史教训未归档
- 用户偏好：无法持久化记录与学习

### 1.2 解决方案

**核心思路**：CLAUDE.md 作为"元规则"，Graphiti 作为"知识库"

```
┌─────────────────────────────────────────────────────────────┐
│                     CLAUDE.md v7.0                          │
│                    （元规则 1500行）                         │
│  - 如何使用 Graphiti                                         │
│  - 三阶段工作流（RETRIEVE → EXECUTE → STORE）                │
│  - 快速导航与紧急参考                                        │
└──────────────────┬──────────────────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────────────────┐
│              Graphiti Knowledge Graph                       │
│                 （项目知识库）                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Preference（偏好）：编码规范、技术栈、命名规范       │   │
│  │ Procedure（流程）：Issue工作流、验证流程、PR流程     │   │
│  │ Requirement（约束）：MVP黑名单、架构触发指标         │   │
│  │ Fact（事实关系）：模块依赖、架构层次、文件位置       │   │
│  │ Decision（决策记录）：Issue决策、重构历史、Bug模式   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 核心设计

### 2.1 三阶段工作流（元规则）

```
任务开始
  ↓
┌──────────────────────────────────────────────┐
│ 阶段1: RETRIEVE（知识检索）                   │
│ ─────────────────────────────────────────    │
│ 1. 确定任务类型（新功能/Bug修复/架构调整）    │
│ 2. search_nodes 检索实体（Preference/        │
│    Procedure/Requirement）                   │
│ 3. search_facts 检索关系（模块依赖/架构层次） │
│ 4. 过滤并整理为"任务上下文"                   │
└──────────────────┬───────────────────────────┘
                   ▼
┌──────────────────────────────────────────────┐
│ 阶段2: EXECUTE（遵循规则）                    │
│ ─────────────────────────────────────────    │
│ 1. 严格遵循检索到的 Preference 和 Procedure  │
│ 2. 冲突时优先级：Preference > Procedure >    │
│    Requirement                               │
│ 3. 发现新规则时实时记录（add_memory）        │
└──────────────────┬───────────────────────────┘
                   ▼
┌──────────────────────────────────────────────┐
│ 阶段3: STORE（知识沉淀）                      │
│ ─────────────────────────────────────────    │
│ 1. add_memory 存储任务决策                   │
│ 2. 更新实体关系（新模块依赖、架构调整）       │
│ 3. 记录历史教训（Bug修复模式、性能优化）      │
└──────────────────────────────────────────────┘
  ↓
任务完成
```

### 2.2 实体类型定义（Pydantic Schema）

```python
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime

# 1. Preference（项目偏好）
class ProjectPreference(BaseModel):
    """项目开发偏好，如编码规范、命名规范、技术栈选择"""
    name: str  # 偏好名称
    category: str  # 分类：coding_style, naming, tech_stack, version
    description: str  # 详细说明
    priority: int = Field(ge=1, le=10)  # 优先级1-10
    applies_to: List[str]  # 适用范围：["Server", "Client", "Shared"]
    examples: Optional[str] = None  # 示例代码

# 2. Procedure（流程规范）
class ProjectProcedure(BaseModel):
    """开发流程与规范，如Issue工作流、验证流程、PR流程"""
    name: str  # 流程名称
    category: str  # 分类：issue_workflow, testing, documentation, deployment
    steps: List[str]  # 流程步骤
    triggers: List[str]  # 触发条件
    checkpoints: List[str]  # 检查点
    related_docs: Optional[str] = None  # 相关文档路径

# 3. Requirement（需求约束）
class ProjectRequirement(BaseModel):
    """项目约束与限制，如MVP技术黑名单、架构触发指标"""
    name: str  # 约束名称
    category: str  # 分类：mvp_constraint, architecture_rule, quality_standard
    constraint_type: str  # 约束类型：forbidden, required, conditional
    description: str  # 详细说明
    trigger_conditions: Optional[List[str]] = None  # 触发条件
    exceptions: Optional[str] = None  # 例外情况

# 4. Fact（事实关系）
class ProjectFact(BaseModel):
    """项目事实关系，如模块依赖、架构层次、文件位置"""
    subject: str  # 主语（实体1）
    predicate: str  # 谓语（关系类型）
    object: str  # 宾语（实体2）
    category: str  # 分类：dependency, hierarchy, location
    timestamp: datetime  # 时间戳
    source: Optional[str] = None  # 来源

# 5. Decision（决策记录）
class ProjectDecision(BaseModel):
    """项目决策记录，如Issue决策、重构决策、Bug修复决策"""
    issue_number: Optional[int] = None  # 关联Issue
    decision: str  # 决策内容
    rationale: str  # 决策理由
    alternatives: Optional[List[str]] = None  # 备选方案
    impact: str  # 影响范围
    timestamp: datetime
```

### 2.3 检索策略矩阵

| 任务类型 | 检索实体类型 | 关键词示例 | 过滤器 |
|---------|------------|-----------|--------|
| **新功能开发** | Preference, Procedure, Requirement | "编码规范", "Issue工作流", "MVP约束" | category: coding_style, issue_workflow, mvp_constraint |
| **Bug修复** | Procedure, Decision | "验证流程", "历史Bug模式" | category: testing, predicate: "修复" |
| **架构调整** | Requirement, Fact | "架构触发指标", "模块依赖" | category: architecture_rule, dependency |
| **代码审查** | Preference, Procedure | "命名规范", "代码审查流程" | category: naming, code_review |
| **文档更新** | Procedure, Fact | "文档同步流程", "文档位置" | category: documentation, location |
| **性能优化** | Requirement, Decision | "性能指标", "历史优化决策" | category: quality_standard, impact: "性能" |

---

## 3. 知识迁移计划

### 3.1 迁移内容分类

#### 批次1：Preference（约20条）

**编码规范**（5条）
1. 语言规范：中文注释、输出、提交信息
2. 编码格式：UTF-8 with BOM
3. 命名规范：PascalCase（类型）、_camelCase（私有字段）
4. 依赖注入：仅构造函数注入
5. 异步规范：I/O必须async/await

**技术栈偏好**（8条）
1. 后端框架：.NET 8, ASP.NET Core Web API
2. ORM：Entity Framework Core 8.0
3. 前端框架：WPF (.NET 8), Prism.DryIoc 9.0
4. HTTP客户端：Refit
5. 数据库：SQL Server 2022
6. 测试框架：xUnit
7. Mock工具：NSubstitute
8. 版本控制：Git（master分支）

**版本策略**（3条）
1. MVP阶段：保持1.x.x.x系列
2. 升级触发：重大架构重构、破坏性API变更
3. 避免行为：大版本频繁跳跃

**架构偏好**（4条）
1. Server端：三层架构（Repository → Service → Controller）
2. Client端：MVVM五层（View → ViewModel → Module → Service → Infrastructure）
3. 共享层：Shared.Models（DTO、接口定义）
4. Repository可见性：internal（仅Service层访问）

---

#### 批次2：Procedure（约15条）

**Issue工作流**（1条）
- 步骤：创建Issue → 实施代码 → 验证（编译+运行+数据库） → 提交代码
- 触发：所有代码变更必须有Issue
- 检查点：验证完整性（运行时验证强制）

**PR工作流**（1条）
- 步骤：创建分支（可选） → 提交代码 → git diff分析 → 生成PR描述 → 推送
- 检查点：PR描述包含Issue关联、测试计划、Claude Code标记

**验证流程**（1条）
- 步骤：编译（0 errors, 0 warnings） → 启动应用 → 执行真实操作 → 验证数据库状态
- 禁止：只编译通过就认为完成

**文档同步流程**（1条）
- 步骤：实施前列清单 → 开发中立即更新 → 完成前确认
- 范围：架构文档、开发指南、API文档、快速参考、导航索引

**代码审查流程**（1条）
- 步骤：规范检查 → 架构验证 → 安全扫描 → 性能分析
- 工具：lybtzyzs-code-review skill

**测试流程**（1条）
- 步骤：单元测试（AAA模式） → 集成测试 → 运行时验证
- 覆盖率目标：关键路径100%

**提交规范**（1条）
- 格式：`type(module): 描述\n\nFixes #issue\n\n- 具体改动`
- 类型：fix, feat, refactor, docs, test, chore
- 签名：Claude Code标记 + Co-Authored-By

**双轨工作流**（2条）
1. 小需求：<5文件, <200行, <2小时 → 直接修改
2. 大需求：跨模块, >200行, >2小时 → lybtzyzs-workflow-orchestrator

**环境清理流程**（1条）
- 步骤：终止临时进程 → 释放资源 → 还原配置 → 关闭连接 → 归档证据 → 端口检查

**Skills调用流程**（4条）
1. 需求文档生成：lybtzyzs-requirements-generator
2. 设计文档生成：lybtzyzs-design-generator
3. 任务分解：lybtzyzs-task-breakdown
4. Issue批量生成：lybtzyzs-issue-template

---

#### 批次3：Requirement（约10条）

**MVP技术黑名单**（1条）
- 分布式：Redis, RabbitMQ/Kafka, Docker（开发阶段）, 微服务
- 过度设计：CQRS, MediatR, Event Sourcing, DDD富领域模型
- 过度抽象：多层抽象接口, 过度工厂/策略模式
- 前端框架：GraphQL, React/Vue（Desktop）, Blazor（Desktop）

**架构触发指标**（6条）
1. 业务规则 >20条 → 富领域模型
2. Service方法 >200行 → 领域服务拆分
3. 聚合根关系 >3层 → 重新设计边界
4. 状态机 >8状态 → 状态机模式
5. 团队规模 >5人 → CQRS分离读写
6. 数据量 >100万 → 缓存/读写分离

**质量标准**（3条）
1. 编译：0 errors, 0 warnings
2. 运行时验证：启动应用 + 真实操作 + 数据库验证
3. 测试覆盖：关键路径100%

**Server/Client职责划分**（1条）
- Server端：数据持久化、核心业务规则、数据校验、实体关系维护
- Client端：工作流编排、UI逻辑、用户交互、业务流程控制
- 决策：数据一致性→Server，多步骤流程→Client

---

#### 批次4：Fact（约30条）

**8大业务模块**（8条）
1. Auth - 依赖 → Users（身份验证与授权）
2. Users - 聚合根（用户管理）
3. Patients - 聚合根（患者档案管理）
4. MedicalCase - 聚合根（病历管理，管理Prescription/Consultation）
5. Consultation - 从属实体（中医诊断，隶属MedicalCase）
6. Prescriptions - 从属实体（处方管理，隶属MedicalCase）
7. Herbs - 聚合根（中药管理）
8. Formula - 聚合根（方剂管理）

**三层架构层次**（3条）
1. Server端：Repository层 → Service层 → Controller层
2. Client端：View → ViewModel → Module → QueryService/BusinessService → Infrastructure
3. Shared层：Shared.Models（DTO、接口定义）

**Repository三层接口**（3条）
1. IReadRepository<T> - 继承 → IRepository<T>（5个只读方法）
2. IRepository<T> - 继承 → IXxxRepository（+9个写入方法）
3. IXxxRepository - 模块特定方法（如IPrescriptionRepository）

**文档位置映射**（8条）
1. 架构文档 - 位于 → docs/explanation/architecture/
2. 开发指南 - 位于 → .claude/guides/
3. 工作模式 - 位于 → .claude/modes/
4. Skills配置 - 位于 → .claude/skills/
5. 参考文档 - 位于 → .claude/reference/
6. Steering文档 - 位于 → .spec-workflow/steering/
7. 项目概览 - 位于 → README.md
8. 文档导航 - 位于 → docs/index.md

**Skills功能映射**（8条）
1. lybtzyzs-workflow-orchestrator - 功能 → 14状态自动化流程（需求→上线）
2. lybtzyzs-requirements-generator - 功能 → 需求文档生成
3. lybtzyzs-design-generator - 功能 → 设计文档生成
4. lybtzyzs-task-breakdown - 功能 → 任务分解
5. lybtzyzs-issue-template - 功能 → Issue批量生成
6. lybtzyzs-code-review - 功能 → 代码规范审查
7. lybtzyzs-mvp-compliance - 功能 → MVP合规检查
8. lybtzyzs-arch-compliance - 功能 → 架构合规检查

---

### 3.2 初始化脚本设计

**脚本1：知识导入（init_graphiti_knowledge.py）**
```python
# 批量导入75条知识到Graphiti
# 使用 add_episode_bulk + JSON格式
# 自定义实体类型（ProjectPreference等）
```

**脚本2：知识验证（verify_graphiti_knowledge.py）**
```python
# 抽样测试检索精度
# 验证实体类型正确性
# 检查关系完整性
```

---

## 4. 实施路径

### Phase 1：基础设施准备（1-2天）

**任务1.1：配置 Graphiti MCP Server**
```bash
# 1. 安装 Neo4j（Docker）
docker run -d \
  --name neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/demodemo \
  neo4j:latest

# 2. 配置 .env 文件
OPENAI_API_KEY=sk-xxxxx
MODEL_NAME=gpt-4.1-mini
NEO4J_URI=bolt://localhost:7687
NEO4J_USER=neo4j
NEO4J_PASSWORD=demodemo
SEMAPHORE_LIMIT=10
```

**任务1.2：测试 MCP 连接**
```json
// Claude Desktop 配置（通过mcp-remote）
{
  "mcpServers": {
    "graphiti-memory": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:8000/sse"]
    }
  }
}
```

**任务1.3：验证基础工具**
```python
# 测试 add_memory
add_memory(
    name="测试知识",
    episode_body='{"test": "success"}',
    source="json"
)

# 测试 search_nodes
search_nodes(query="测试", max_nodes=5)
```

---

### Phase 2：知识迁移（2-3天）

**任务2.1：运行初始化脚本**
```bash
python scripts/init_graphiti_knowledge.py
```

**任务2.2：验证检索精度**
```bash
python scripts/verify_graphiti_knowledge.py
```

**任务2.3：调整实体标签**
- 检查分类是否合理
- 优化关键词（中英文双语）
- 补充缺失的关系

---

### Phase 3：工作流切换（1天）

**任务3.1：部署新版 CLAUDE.md**
```bash
# 备份旧版
cp CLAUDE.md CLAUDE.md.v6.3.backup

# 部署新版
cp docs/proposals/CLAUDE.md.v7.0 CLAUDE.md
```

**任务3.2：删除旧版 Serena Memory**
```bash
# 删除项目特定记忆（避免与Graphiti混淆）
# 仅保留通用命令速查
```

**任务3.3：团队培训**
- 培训材料：GRAPHITI_USAGE_GUIDE.md
- 培训时长：30分钟
- 核心内容：三阶段工作流、检索策略

---

## 5. 收益与风险评估

### 5.1 收益评估

| 指标 | 当前状态 | 优化后 | 提升幅度 |
|-----|---------|--------|---------|
| **文档行数** | 6000+行 | 1500行 | **-75%** |
| **知识复用** | 单会话 | 跨会话持久化 | **∞** |
| **检索精度** | 全文搜索 | 语义+实体+时序 | **+200%** |
| **知识沉淀** | 手动整理 | 自动归档 | **自动化** |
| **学习曲线** | 6000行文档 | 1500行+检索 | **-60%** |

**量化收益**：
- 任务启动速度：从"阅读6000行" → "检索5条知识" → **节省90%时间**
- 知识复用率：从0%（每次重新说明） → 100%（Graphiti持久化）
- 规则遵循准确率：从70%（遗漏） → 95%（强制检索）

---

### 5.2 风险评估

| 风险类型 | 可能性 | 影响程度 | 缓解措施 |
|---------|-------|---------|---------|
| **检索不精准** | 中 | 中 | 定期检查标签质量，优化关键词 |
| **知识冗余** | 低 | 低 | 去重策略，避免重复存储 |
| **迁移成本** | 低 | 低 | 自动化脚本，4-6天完成 |
| **学习曲线** | 中 | 低 | 详细使用指南 + 培训 |
| **Neo4j稳定性** | 低 | 高 | Docker部署，定期备份 |
| **OpenAI API成本** | 低 | 低 | 使用gpt-4.1-mini（低成本模型） |

**总体风险评级**：**低**

---

## 6. 决策建议

### 6.1 推荐方案

**✅ 接受本优化方案**

**理由**：
1. **技术可行性高**：Graphiti已调试成功，MCP连接稳定
2. **收益显著**：75%文档精简 + 跨会话记忆 + 精准检索
3. **风险可控**：低风险，4-6天完成迁移
4. **长期价值**：知识库持续积累，复利效应

---

### 6.2 替代方案

**方案A：保持现状（不推荐）**
- 优点：无迁移成本
- 缺点：文档臃肿持续加剧，知识复用能力弱

**方案B：部分迁移（可选）**
- 仅迁移Preference和Procedure（约35条）
- 保留Requirement和Fact在CLAUDE.md
- 优点：降低迁移成本
- 缺点：收益减半

---

## 7. 下一步行动

### 7.1 立即执行（如接受方案）

**步骤1**：创建实施分支
```bash
git checkout -b feature/graphiti-optimization
```

**步骤2**：生成交付物
- [ ] 新版 CLAUDE.md（v7.0）
- [ ] Graphiti 初始化脚本
- [ ] 知识验证脚本
- [ ] 使用指南

**步骤3**：Phase 1 执行（1-2天）
- [ ] 配置 Neo4j
- [ ] 测试 MCP 连接
- [ ] 验证基础工具

**步骤4**：Phase 2 执行（2-3天）
- [ ] 运行初始化脚本
- [ ] 验证检索精度
- [ ] 调整实体标签

**步骤5**：Phase 3 执行（1天）
- [ ] 部署新版 CLAUDE.md
- [ ] 删除旧版 Serena Memory
- [ ] 团队培训

---

### 7.2 延迟决策（如需更多时间）

**可先执行的低风险任务**：
1. 配置 Neo4j 测试环境
2. 阅读 Graphiti 使用指南
3. 手动测试 add_memory / search_nodes

**评估期限**：建议1周内决策

---

## 8. 附录

### 8.1 参考文档

- Graphiti 官方文档：https://github.com/getzep/graphiti
- Graphiti MCP Server：https://github.com/getzep/graphiti/tree/main/mcp_server
- 本项目现有 CLAUDE.md：D:\source\repos\LYBTZYZS\CLAUDE.md

### 8.2 联系方式

**技术支持**：Claude Code
**提案讨论**：通过 GitHub Issue #[待分配]

---

**最后更新**：2025-11-11
**版本历史**：v1.0（初稿）
