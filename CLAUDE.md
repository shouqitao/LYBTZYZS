# 🔄 凌隐宝堂中医诊所项目 (LYBTZYZS) 工作流程调度

> **项目全称**: 凌隐宝堂中医诊所管理系统
> **项目简称**: LYBTZYZS
> **说明**: 在描述性场合统一使用"凌隐宝堂中医诊所项目"，技术文档中使用LYBTZYZS简称

## 🎯 任务执行标准流程（UltraThink模式）

> **核心理念**: 深度思考 → 记忆检索 → 架构理解 → 任务规划 → 渐进执行 → 持续记录

### 🧠 阶段1: 深度思考与信息收集（THINK）

#### 1.1 激活UltraThink模式
```markdown
**思考维度**:
- 任务的本质是什么？解决什么核心问题？
- 涉及哪些模块、组件、架构层次？
- 可能的风险和挑战是什么？
- 历史上是否有类似任务？如何解决的？
```

#### 1.2 查询Graphiti记忆（必须执行）
```bash
# 1. 搜索相关历史任务
mcp__graphiti-memory__search_memory_facts
  query: "[模块名] [功能名] [技术关键词]"

# 2. 检索相关架构实体
mcp__graphiti-memory__search_nodes
  query: "[组件名] [类名] [服务名]"

# 3. 获取最近相关记录
mcp__graphiti-memory__get_episodes
  max_episodes: 10
```

**检索要点**:
- 该模块的历史Bug和解决方案
- 相关架构决策和约束
- 最佳实践和反模式
- 技术债务和待优化点

#### 1.3 理解项目架构
```bash
# 1. 查看项目架构文档
docs/explanation/architecture/{client|server|shared}/
  - DESKTOP_ARCHITECTURE_STANDARD.md  # 前端架构标准
  - SERVER_ARCHITECTURE_STANDARD.md   # 后端架构标准
  - Module-{模块名}.md                # 模块架构文档

# 2. 查看开发流程
docs/guides/requirement-driven-workflow.md

# 3. 查看技术约束
docs/reference/mvp-constraints.md
```

**架构理解检查清单**:
- [ ] 了解三层架构（Repository/Service/Controller或ViewModel）
- [ ] 了解模块依赖关系和通信方式
- [ ] 了解数据流和状态管理
- [ ] 了解测试策略和质量标准

#### 1.4 查看设计文档（如果存在）
```bash
# 查看需求和设计文档
docs/explanation/requirements/
docs/explanation/design/
docs/templates/requirement-confirmation-template.md
docs/templates/design-proposal-template.md
```

#### 1.5 验证流程一致性
**关键检查**:
- Graphiti记忆中的任务流程 vs docs/guides/requirement-driven-workflow.md
- 是否有流程不一致或遗漏？
- 是否有新的最佳实践需要更新到文档？

**不一致时的处理**:
1. 分析差异原因（技术演进、需求变化、经验积累）
2. 确定正确的流程（优先采用最新的最佳实践）
3. 更新相关文档（docs/guides/）
4. 记录到Graphiti记忆

### 📋 阶段2: 任务规划与清单生成（PLAN）

#### 2.1 确定需要调用的Skills
```markdown
**大需求 (Epic)**:
- [ ] lybtzyzs-requirements-generator（需求确认）
- [ ] lybtzyzs-design-generator（方案设计）
- [ ] lybtzyzs-task-breakdown（任务分解）
- [ ] lybtzyzs-task-executor（任务执行）
- [ ] lybtzyzs-pr-generator（PR生成）
- [ ] lybtzyzs-task-reflector（任务反思）

**小需求 (Issue)**:
- [ ] lybtzyzs-requirements-generator（可选，简化版）
- [ ] lybtzyzs-design-generator（可选，简化版）
- [ ] lybtzyzs-task-executor（任务执行）
- [ ] lybtzyzs-task-reflector（任务反思）

**GitHub操作**:
- [ ] mcp__github__issue_write（Issue管理）
- [ ] mcp__github__pull_request_write（PR管理）
```

#### 2.2 生成明确的任务清单
使用TodoWrite工具创建任务清单：

```markdown
**任务清单模板**:

[ ] 1. 深度思考与信息收集
  - 查询Graphiti记忆
  - 理解项目架构
  - 查看设计文档
  - 验证流程一致性

[ ] 2. 任务规划
  - 确定需要的Skills
  - 生成任务清单
  - 评估工作量和风险

[ ] 3. 需求确认（大需求必须，小需求可选）
  - 调用requirements-generator
  - 用户确认需求文档

[ ] 4. 方案设计（大需求必须，小需求可选）
  - 调用design-generator
  - 架构审查
  - 用户确认设计

[ ] 5. 任务执行
  - [ ] 5.1 子任务1描述
  - [ ] 5.2 子任务2描述
  - [ ] 5.3 子任务3描述
  - ...

[ ] 6. 验证测试
  - 单元测试
  - 集成测试
  - 功能验证

[ ] 7. 用户确认
  - 功能演示
  - 验收确认

[ ] 8. PR创建（大需求必须）
  - 生成PR描述
  - 创建PR

[ ] 9. 文档同步
  - 更新技术文档
  - 更新用户文档

[ ] 10. Graphiti更新
  - 记录决策和经验
  - 更新最佳实践

[ ] 11. 环境清理
  - 清理临时文件
  - 验证工作区状态

[ ] 12. Issue/Epic关闭
  - 验证所有子任务完成
  - 关闭Issue/Epic
```

### ⚡ 阶段3: 渐进执行与持续记录（EXECUTE）

#### 3.1 渐进式执行原则
- **单一职责**: 每次只专注一个子任务
- **小步快跑**: 子任务应≤2小时完成
- **持续验证**: 每个子任务完成后立即测试
- **及时记录**: 完成后立即保存记忆

#### 3.2 子任务执行模板
```markdown
**执行步骤**:
1. 标记任务为 in_progress
2. 执行具体操作（编码/配置/文档）
3. 编译验证
4. 功能测试
5. 标记任务为 completed
6. 保存Graphiti记忆（立即执行）
```

#### 3.3 每个子任务完成后的记忆保存
```bash
# 立即保存记忆
mcp__graphiti-memory__add_memory
  name: "{模块名}-{子任务名}-完成-{日期}"
  episode_body: """
  ## 子任务: {子任务名}

  **完成时间**: {timestamp}
  **父任务**: {父任务名}

  ### 完成内容
  - 具体做了什么
  - 修改了哪些文件

  ### 遇到的问题
  - 问题描述
  - 解决方案

  ### 技术要点
  - 关键技术点
  - 注意事项

  ### Git提交
  - Commit: {commit_hash}
  """
```

#### 3.4 持续检查与调整
**每完成3个子任务后检查**:
- [ ] 当前方向是否正确？
- [ ] 是否需要调整任务清单？
- [ ] 是否发现新的风险？
- [ ] 记忆中是否有新的经验可以应用？

### 📚 阶段4: 总结与归档（REFLECT）

#### 4.1 任务完成总结
```bash
# 调用task-reflector生成完整总结
lybtzyzs-task-reflector
```

#### 4.2 保存完整记忆
```bash
mcp__graphiti-memory__add_memory
  name: "{模块名}-{任务类型}-完成-{日期}"
  episode_body: """
  ## {任务完整标题}

  **时间**: {日期}
  **模块**: {模块名}
  **类型**: {Epic|Issue|Bug|Feature}
  **关联**: #{Issue编号}

  ### 任务概述
  - 背景和目标
  - 涉及范围

  ### 执行过程
  - 调用的Skills
  - 主要步骤
  - 遇到的问题和解决方案

  ### 技术决策
  - 关键决策点
  - 决策理由
  - 影响分析

  ### 经验教训
  - 成功经验
  - 失败教训
  - 最佳实践
  - 反模式警示

  ### Git记录
  - Commit历史
  - PR链接
  - 代码变更统计

  ### 验证结果
  - 测试覆盖率
  - 功能验证结果
  - 用户验收结果

  ### 后续建议
  - 待优化点
  - 技术债务
  - 改进方向
  """
```

#### 4.3 文档更新检查
- [ ] 架构文档是否需要更新？
- [ ] 开发流程是否需要优化？
- [ ] 最佳实践是否需要补充？
- [ ] 技术约束是否需要调整？

## 📋 简化流程视图

### 🔄 大需求 (Epic) 流程
🧠 UltraThink → 📖 查记忆/文档 → 📋 任务清单 → 📝 需求确认 → 🎯 方案设计 → 📝 Epic创建 → 🔍 Issue分解 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 🔀 PR创建 → 👀 PR审查 → 🔀 PR合并 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Epic关闭

### 🔄 小需求 (Issue) 流程
🧠 UltraThink → 📖 查记忆/文档 → 📋 任务清单 → 📝 需求确认 → 🎯 方案设计 → 📝 Issue创建 → ⚡ 渐进执行（每步保存记忆）→ ✅ 验证测试 → 👤 用户确认 → 📚 文档同步 → 🧠 完整记忆 → 🧹 环境清理 → ✅ Issue关闭

## 📖 详细流程指南
→ **查看**: `docs/guides/requirement-driven-workflow.md` (完整需求驱动流程)
→ **模板**: `docs/templates/` (需求确认和方案设计模板)
→ **技能**: 调用相应LYBTZYZS Skills自动化生成文档

## 🛠️ 核心Skills调用 (凌隐宝堂中医诊所项目专用)
- `lybtzyzs-requirements-generator` - 生成需求确认文档
- `lybtzyzs-design-generator` - 生成方案设计文档
- `lybtzyzs-task-executor` - 自动执行GitHub Issue
- `lybtzyzs-pr-generator` - 生成Pull Request描述
- `lybtzyzs-task-reflector` - 任务完成反思总结
- `lybtzyzs-context-builder` - 构建任务执行所需的完整上下文

## 🔧 GitHub操作规范
- **默认工具**: 使用GitHub MCP工具进行所有GitHub操作
- **Issue管理**: `mcp__github__issue_write` (创建/更新/关闭)
- **PR管理**: `mcp__github__pull_request_*` 系列工具
- **仓库操作**: `mcp__github_*` 系列工具
- **认证要求**: 确保GitHub token有足够权限

## 🚨 核心约束
- **需求驱动**: 所有工作从需求确认开始
- **文档生成**: 重要文档必须调用skill生成
- **Graphiti记忆系统**: 决策和经验存储到Graphiti第一大脑
- **用户确认**: 重要变更需要用户同意后再执行
- **环境清理**: 任务完成必须执行清理流程
- **Issue闭环**: 所有Issues必须手动关闭，确保流程完整
- **PR检查**: 大需求合并PR后，检查Issues是否自动关闭，未关闭则手动关闭

## 🧠 记忆管理详细规范（已整合到UltraThink流程）

> **说明**: 记忆管理已整合到上述"任务执行标准流程"的各个阶段，此处提供详细的操作指南和模板。

### 记忆管理三阶段

#### 📖 阶段1: 任务启动前 - 查阅记忆（RETRIEVE）
**对应UltraThink流程**: 阶段1.2 查询Graphiti记忆

**必须执行的操作**:
```bash
# 1. 搜索历史任务和解决方案
mcp__graphiti-memory__search_memory_facts
  query: "{模块名} {功能关键词} {技术概念}"
  max_facts: 20

# 2. 搜索相关架构实体和组件
mcp__graphiti-memory__search_nodes
  query: "{组件名} {类名} {服务名}"
  max_nodes: 15

# 3. 获取最近的任务记录
mcp__graphiti-memory__get_episodes
  max_episodes: 10
```

**检索目标**:
- [ ] 该模块的历史Bug和解决方案
- [ ] 相关的架构决策和技术约束
- [ ] 已知的最佳实践和反模式
- [ ] 技术债务和待优化点
- [ ] 类似任务的处理方式

**记忆应用**:
- 避免重复踩坑
- 复用成功经验
- 遵循已有决策
- 识别潜在风险

#### ⚡ 阶段2: 任务执行中 - 渐进记录（RECORD）
**对应UltraThink流程**: 阶段3.3 每个子任务完成后的记忆保存

**记录时机** - 每完成一个子任务立即记录：
- ✅ 完成一个功能模块
- ✅ 修复一个Bug
- ✅ 完成一个重要配置
- ✅ 做出一个技术决策
- ✅ 遇到并解决一个难题

**子任务记忆模板**:
```markdown
## 子任务: {具体子任务名称}

**完成时间**: {ISO时间戳}
**父任务**: {Epic/Issue名称}
**状态**: ✅ 已完成

### 完成内容
- 具体实现了什么功能
- 修改了哪些文件
- 新增/删除的代码行数

### 遇到的问题
**问题**: {具体问题描述}
**根因**: {根本原因分析}
**解决方案**: {具体解决方法}
**耗时**: {解决问题花费的时间}

### 技术要点
- 使用的技术/框架/库
- 关键代码片段
- 注意事项和约束
- 性能/安全考虑

### 决策记录
**决策**: {做出的技术决策}
**理由**: {选择该方案的原因}
**替代方案**: {考虑过但未采用的方案}
**影响**: {对后续开发的影响}

### Git提交
- Commit Hash: {commit_hash}
- Commit Message: {commit_message}
- Files Changed: {changed_files_count}

### 下一步
- 待完成的相关任务
- 需要关注的风险点
```

**快速记录模式** - 简单任务使用简化版：
```markdown
## {子任务名} - 完成

- **时间**: {timestamp}
- **内容**: {一句话描述}
- **文件**: {修改的主要文件}
- **要点**: {关键技术点}
- **Commit**: {hash}
```

#### 📚 阶段3: 任务结束后 - 完整归档（ARCHIVE）
**对应UltraThink流程**: 阶段4.2 保存完整记忆

**完整记忆模板**:
```markdown
## {任务完整标题}

**时间**: YYYY-MM-DD
**模块**: {所属模块}
**类型**: {Epic|Issue|Bug|Feature|Refactor}
**关联**: #{GitHub Issue编号}
**工作量**: {实际用时 vs 预估用时}

### 任务概述
**背景**: {任务的业务背景和技术背景}
**目标**: {要达成的具体目标}
**范围**: {涉及的模块、组件、文件}
**约束**: {技术约束、时间约束、资源约束}

### 执行过程
**调用Skills**:
- {skill1}: {用途}
- {skill2}: {用途}

**主要步骤**:
1. {步骤1描述} - {结果}
2. {步骤2描述} - {结果}
3. {步骤3描述} - {结果}

**遇到的问题**:
| 问题 | 根因 | 解决方案 | 耗时 |
|------|------|----------|------|
| {问题1} | {根因1} | {方案1} | {时间1} |
| {问题2} | {根因2} | {方案2} | {时间2} |

### 技术决策
**决策1**: {决策内容}
- **理由**: {为什么这样决策}
- **影响**: {对架构/性能/维护的影响}
- **权衡**: {考虑过的其他方案及优劣}

**决策2**: {决策内容}
- ...

### 架构变更
**变更内容**: {具体的架构调整}
**变更原因**: {为什么需要调整}
**影响范围**: {影响的模块和组件}
**兼容性**: {向后兼容性说明}

### 经验教训
**成功经验** ✅:
- {经验1}: {具体做法和效果}
- {经验2}: {具体做法和效果}

**失败教训** ❌:
- {教训1}: {错误做法和后果}
- {教训2}: {错误做法和后果}

**最佳实践** ⭐:
- {实践1}: {推荐做法和原因}
- {实践2}: {推荐做法和原因}

**反模式警示** ⚠️:
- {反模式1}: {应避免的做法和危害}
- {反模式2}: {应避免的做法和危害}

### Git记录
**Commit历史**:
- {commit1_hash}: {message1}
- {commit2_hash}: {message2}
- 总计: {commit_count}个提交

**代码变更统计**:
- 新增: {added_lines}行
- 删除: {deleted_lines}行
- 修改文件: {changed_files}个

**PR链接**: {pr_url}（如果有）

### 验证结果
**测试覆盖率**:
- 单元测试: {coverage}%
- 集成测试: {coverage}%
- 新增测试: {test_count}个

**功能验证**:
- [ ] 功能A - ✅ 通过
- [ ] 功能B - ✅ 通过
- [ ] 边界情况 - ✅ 通过

**性能指标**:
- 响应时间: {time}ms
- 内存占用: {memory}MB
- 数据库查询: {query_count}次

**用户验收**: {通过|部分通过|未通过}
- 验收意见: {用户反馈}

### 技术债务
**新增债务**:
- {债务1}: {描述和影响}
- {债务2}: {描述和影响}

**偿还债务**:
- {债务1}: {已解决}
- {债务2}: {已解决}

### 后续建议
**待优化点**:
- {优化点1}: {优化方向和预期收益}
- {优化点2}: {优化方向和预期收益}

**技术演进**:
- {演进方向1}: {理由和路线图}
- {演进方向2}: {理由和路线图}

**风险提示**:
- {风险1}: {描述和缓解措施}
- {风险2}: {描述和缓解措施}

### 相关资源
**文档链接**:
- 需求文档: {url}
- 设计文档: {url}
- API文档: {url}

**参考资料**:
- {资料1}: {url}
- {资料2}: {url}
```

### 记忆命名规范
```
格式: {模块名}-{任务类型}-{简要描述}-{日期}

示例:
- FormulaDetailView-Bug修复-XAML绑定错误-2025-01-18
- Auth模块-Feature-JWT刷新令牌-2025-01-15
- Patients模块-Refactor-Repository统一化-2025-01-10
- 全局-Architecture-三层架构规范更新-2025-01-08
```

### 记忆检索技巧
**按模块检索**:
```
query: "FormulaDetailView"
query: "Auth 模块"
query: "Patients Repository"
```

**按技术检索**:
```
query: "XAML 绑定"
query: "EF Core 查询优化"
query: "Prism 导航"
```

**按问题检索**:
```
query: "Bug NullReferenceException"
query: "性能 慢查询"
query: "内存泄漏"
```

**按时间检索**:
```
group_ids: ["main"]
max_episodes: 20  # 最近20条记录
```

### 记忆管理工具
```bash
# 添加记忆
mcp__graphiti-memory__add_memory

# 搜索事实关系
mcp__graphiti-memory__search_memory_facts

# 搜索实体节点
mcp__graphiti-memory__search_nodes

# 获取历史记录
mcp__graphiti-memory__get_episodes

# 删除错误记忆（慎用）
mcp__graphiti-memory__delete_episode

# 检查系统状态
mcp__graphiti-memory__get_status
```

## 📦 项目配置信息
- **项目全称**: 凌隐宝堂中医诊所管理系统
- **项目简称**: LYBTZYZS
- **GitHub账户**: shouqitao (TonyShou)
- **仓库路径**: https://github.com/shouqitao/LYBTZYZS
- **项目类型**: 企业级中医诊所管理系统
