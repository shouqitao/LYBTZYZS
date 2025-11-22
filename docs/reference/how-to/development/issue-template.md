---
name: lybtzyzs-issue-template
description: 为LYBTZYZS项目生成标准化GitHub Issue模板,支持单Issue模式(Feature/Bug/Refactor等)和批量模式(从task文档批量创建)。触发关键词:创建Issue、批量创建Issues、根据task文档生成Issues
---

# LYBTZYZS Issue模板生成器

> **📦 版本**:v1.2(新增批量模式)
> **📅 最后更新**:2025-10-26

---

## 核心能力

### 单Issue模式(v1.0/1.1)
1. 根据Issue类型自动生成标准化模板(Feature/Bug Fix/Refactor/Documentation/Test/Chore)
2. 自动关联Epic和Milestone
3. 生成详细的验收标准清单
4. 智能提取技术约束(从Constitution)
5. 推荐相关文档和参考资料
6. 估算工作量(基于复杂度)
7. **直接在GitHub上创建Issue并返回URL**(核心功能)

### 批量模式(v1.2新增)⭐
8. **从task文档批量生成GitHub Issues**
9. **自动识别并标注任务间依赖关系**
10. **Epic自动关联**
11. **批量创建(一次性创建N个Issues)**

---

## 何时使用

### 单Issue模式
- 创建新的Feature、Bug Fix或Refactor任务时
- 需要规范化Issue格式确保信息完整时
- 希望自动关联Epic和生成验收清单时
- 新成员创建Issue需要指导时
- 需要快速标准化任务描述时

### 批量模式(v1.2新增)⭐
- 设计文档完成并生成task文档后(使用lybtzyzs-task-breakdown)
- 需要将Epic拆分成多个子Issues
- 需要为多个任务快速创建Issues
- 需要保持任务间依赖关系清晰

---

## 工作流程

### 单Issue模式(原有流程)

1. 询问Issue类型(Feature/Bug Fix/Refactor/Documentation/Test/Chore)
2. 收集必要信息(标题、简要描述、优先级)
3. 可选:询问关联的Epic(如Epic #1494 - 医案流程模块)
4. 读取Constitution提取相关技术约束
5. 生成标准化Issue内容
6. **在GitHub上创建Issue并返回Issue URL**

### 批量模式(v1.2新增)⭐

**完整流程**:
```
设计文档完成
  ↓
运行 lybtzyzs-design-arch-validator(验证架构)
  ↓
运行 lybtzyzs-task-breakdown(生成task文档)
  ↓
运行 lybtzyzs-issue-template(批量模式)⭐
  ↓
GitHub Issues批量创建完成
```

**批量模式执行步骤**:
```
Step 1: 读取task文档
  → Read(docs/tasks/*.md)
  → 解析元数据(Epic、设计文档、需求文档)

Step 2: 解析任务清单
  → 提取所有Task(#### Task X.Y: ...)
  → 提取元数据(工作量、依赖、类型、文件、验收标准)

Step 3: 批量创建Issues
  → 遍历每个Task
  → 生成Issue描述(基于task元数据)
  → 调用mcp__github__create_issue
  → 保存Issue编号(用于依赖关系映射)

Step 4: 标注依赖关系
  → 将Task依赖(Task 1.1)映射到Issue依赖(Issue #1601)
  → 在Issue描述中标注"依赖Issue #XXX"

Step 5: 输出结果
  → 显示所有创建的Issues(URL、编号、依赖关系)
  → 显示统计信息(数量、工作量、Epic)
```

---

## 输入要求

### 单Issue模式

**必需**:
- Issue类型(从6种类型中选择)
- 简要描述(1-2句话说明要做什么)

**可选**:
- 关联的Epic编号
- 优先级(高/中/低,默认:中)
- 预期完成时间

### 批量模式(v1.2新增)

**必需**:
- Task文档路径(docs/tasks/*.md)
- Task文档必须符合lybtzyzs-task-breakdown的标准格式

**可选**:
- Phase过滤(只为特定Phase创建Issues)

---

## 输出格式

### 单Issue模式输出

**最终输出**:
- ✅ **GitHub Issue URL**(主要输出):如 `https://github.com/shouqitao/LYBTZYZS/issues/1568`
- ✅ **Issue编号**:如 `#1568`
- ✅ **Issue状态**:Open

### 批量模式输出

**控制台输出示例**:
```
✅ 批量Issue创建完成!

📁 Task文档:docs/tasks/medicalcase-enhancement-tasks.md
📊 创建统计:
- 成功创建:8个Issues
- 关联Epic:#1494
- 总工作量:18-24小时

📋 创建的Issues:

Phase 1: Server端基础层
  ✅ Issue #1601: Task 1.1 - 创建MedicalCaseRepository扩展
     https://github.com/shouqitao/LYBTZYZS/issues/1601
     工作量: 1.5-2小时 | 依赖: 无

  ✅ Issue #1602: Task 1.2 - 实现MedicalCaseService业务逻辑
     https://github.com/shouqitao/LYBTZYZS/issues/1602
     工作量: 2.5-3小时 | 依赖: Issue #1601

...

🔗 依赖关系已标注在Issue描述中
📈 Epic #1494进度:8个子Issues已创建

💡 下一步:
在GitHub Projects中查看Issues,按依赖关系排序后开始实施
```

---

## Issue内容格式

### Feature Issue模板

```markdown
## 📝 任务描述
[清晰描述要做什么]

## 🎯 目标
- 业务目标:[从用户角度描述价值]
- 技术目标:[技术实现目标]

## ✅ 验收标准

### 功能验收
- [ ] 功能验收1
- [ ] 功能验收2

### 质量验收
- [ ] 编译通过:0 errors, 0 warnings
- [ ] 单元测试通过(如果存在)
- [ ] 代码符合MVVM规范(如果是Client端)
- [ ] 代码符合项目代码规范(中文注释、命名规范)

## 📐 设计方案
[架构设计、技术选型、实现思路]

## 📚 参考资料
- 相关文档:[链接]
- 相关Issue:[Issue编号]
- Constitution约束:[相关技术约束]

## 📊 实施范围
[预期修改的文件列表]

## ⏱️ 预计工作量
X-Y小时

## ⚠️ 技术约束
[从Constitution自动提取的相关约束]

## 🏷️ 标签
`enhancement` `client/server/shared` `模块名` `优先级`

---

**相关Epic**:Epic #XXXX
**优先级**:中
```

### Bug Fix Issue模板

```markdown
## 🐛 问题描述
[Bug现象的清晰描述]

## 🔄 复现步骤
1. 步骤1
2. 步骤2
3. 观察到的错误

## 💡 预期行为
[应该如何工作]

## 📷 截图/日志
[如果适用]

## 🔧 根因分析
[问题原因,如果已知]

## ✅ 验收标准
- [ ] Bug已修复,无法复现
- [ ] 回归测试通过
- [ ] 无新增副作用
- [ ] 相关文档已更新(如果需要)

## 📚 参考资料
- 相关Issue:[Issue编号]
- 错误日志:[路径]

## 🏷️ 标签
`bug` `client/server/shared` `模块名` `优先级`

---

**优先级**:高
```

### Refactor Issue模板

```markdown
## 🔄 重构目标
[要重构什么,为什么重构]

## 💡 重构动机
- 当前问题:[代码存在的问题]
- 期望改进:[重构后的改进点]

## 📐 重构方案
[技术方案、架构调整]

## ✅ 验收标准
- [ ] 功能行为保持不变
- [ ] 代码质量提升(可维护性/可读性/性能)
- [ ] 测试全部通过
- [ ] 编译通过:0 errors, 0 warnings

## 📊 影响范围
[预期修改的文件/模块]

## ⏱️ 预计工作量
X-Y小时

## ⚠️ 风险评估
- 风险1:[描述及缓解措施]
- 风险2:[描述及缓解措施]

## 🏷️ 标签
`refactor` `client/server/shared` `模块名`

---

**相关Epic**:Epic #XXXX
```

### 批量模式Issue模板

```markdown
## 📝 任务描述
{任务标题}

**技术要点**:
- {技术要点1}
- {技术要点2}

## 🎯 目标
实现{任务标题},作为Epic #{Epic编号}的一部分。

## ✅ 验收标准
- [ ] {验收标准1}
- [ ] {验收标准2}
- [ ] {验收标准3}

## 📊 实施范围
**文件范围**:
- `{文件路径1}`
- `{文件路径2}`

**工作量估算**:{X-Y小时}

## 🔗 依赖关系
⚠️ **依赖任务**:
- Issue #{依赖Issue编号1}: Task X.Y - {依赖任务标题}
- Issue #{依赖Issue编号2}: Task X.Z - {依赖任务标题}

或

✅ **无依赖**:可立即开始

## 📚 参考资料
- Task文档:docs/tasks/{feature-name}-tasks.md
- 设计文档:{设计文档路径}
- 需求文档:{需求文档路径}

## 🏷️ 标签
`task` `phase-{N}` `{type}` `{module}`

---

**相关Epic**:Epic #{Epic编号}
**Phase**:Phase {N}
**类型**:{Repository/Service/Controller/ViewModel/View}
```

---

## 示例

### 示例1:创建Feature Issue(单Issue模式)

**用户输入**:
```
"创建一个Feature Issue,实现医案列表的搜索功能"
```

**交互过程**:
```
Claude:检测到要创建Feature Issue。

关联的Epic(可选):
1. Epic #1494 - 医案流程模块
2. Epic #1343 - MVP核心功能
3. 无关联Epic

请选择或直接回车跳过:
```

**用户选择**:`1`

**最终输出**:
```
✅ Issue已成功创建!

Issue #1569: feat(medicalcase): 实现医案列表搜索功能
链接: https://github.com/shouqitao/LYBTZYZS/issues/1569
```

### 示例2:创建Bug Fix Issue(单Issue模式)

**用户输入**:
```
"创建Bug Issue,医案列表加载时出现400错误"
```

**最终输出**:
```
✅ Issue已成功创建!

Issue #1570: bug(medicalcase): 医案列表加载时出现400错误
链接: https://github.com/shouqitao/LYBTZYZS/issues/1570
```

### 示例3:批量模式 - 从task文档创建Issues(v1.2新增)

**用户命令**:
```
"根据task文档批量生成Issues"
或
"批量创建Issues: docs/tasks/medicalcase-enhancement-tasks.md"
```

**执行过程**:
```
Step 1: 读取task文档
✓ docs/tasks/medicalcase-enhancement-tasks.md
✓ Epic: #1494
✓ 识别8个任务

Step 2: 解析任务
✓ Task 1.1: 工作量1.5-2h, 依赖无
✓ Task 1.2: 工作量2.5-3h, 依赖Task 1.1
✓ Task 1.3: 工作量1-1.5h, 依赖Task 1.1
✓ Task 1.4: 工作量1-1.5h, 依赖Task 1.3
✓ Task 2.1: 工作量2-3h, 依赖Task 1.2, Task 1.4
✓ Task 2.2: 工作量2-3h, 依赖Task 2.1
✓ Task 3.1: 工作量3-4h, 依赖Task 2.1
✓ Task 3.2: 工作量5-6h, 依赖Task 3.1

Step 3: 批量创建(8个API调用)
✓ Issue #1601 (Task 1.1)
✓ Issue #1602 (Task 1.2, 依赖#1601)
✓ Issue #1603 (Task 1.3, 依赖#1601)
✓ Issue #1604 (Task 1.4, 依赖#1603)
✓ Issue #1605 (Task 2.1, 依赖#1602, #1604)
✓ Issue #1606 (Task 2.2, 依赖#1605)
✓ Issue #1607 (Task 3.1, 依赖#1605)
✓ Issue #1608 (Task 3.2, 依赖#1607)

Step 4: 标注依赖
✓ 所有依赖关系已标注

Step 5: 输出
✓ 详细清单已显示
```

**最终输出**:
```
✅ 批量Issue创建完成!

📁 Task文档:docs/tasks/medicalcase-enhancement-tasks.md
📊 创建统计:
- 成功创建:8个Issues
- 关联Epic:#1494
- 总工作量:18-24小时

📋 创建的Issues:

Phase 1: Server端基础层
  ✅ Issue #1601: Task 1.1 - 创建MedicalCaseRepository扩展
     https://github.com/shouqitao/LYBTZYZS/issues/1601
     工作量: 1.5-2小时 | 依赖: 无

  ✅ Issue #1602: Task 1.2 - 实现MedicalCaseService业务逻辑
     https://github.com/shouqitao/LYBTZYZS/issues/1602
     工作量: 2.5-3小时 | 依赖: Issue #1601

  ✅ Issue #1603: Task 1.3 - 创建Consultation/Prescription DTO
     https://github.com/shouqitao/LYBTZYZS/issues/1603
     工作量: 1-1.5小时 | 依赖: Issue #1601

  ✅ Issue #1604: Task 1.4 - 配置AutoMapper Profile
     https://github.com/shouqitao/LYBTZYZS/issues/1604
     工作量: 1-1.5小时 | 依赖: Issue #1603

Phase 2: Server端API层
  ✅ Issue #1605: Task 2.1 - 实现MedicalCaseController API端点
     https://github.com/shouqitao/LYBTZYZS/issues/1605
     工作量: 2-3小时 | 依赖: Issue #1602, Issue #1604

  ✅ Issue #1606: Task 2.2 - 编写API集成测试
     https://github.com/shouqitao/LYBTZYZS/issues/1606
     工作量: 2-3小时 | 依赖: Issue #1605

Phase 3: Client端集成
  ✅ Issue #1607: Task 3.1 - 创建MedicalCaseViewModel扩展
     https://github.com/shouqitao/LYBTZYZS/issues/1607
     工作量: 3-4小时 | 依赖: Issue #1605

  ✅ Issue #1608: Task 3.2 - 更新MedicalCaseView UI
     https://github.com/shouqitao/LYBTZYZS/issues/1608
     工作量: 5-6小时 | 依赖: Issue #1607

🔗 依赖关系已标注在Issue描述中
📈 Epic #1494进度:8个子Issues已创建

💡 下一步:
在GitHub Projects中查看Issues,按依赖关系排序后开始实施
```

---

## 技术实现

### 单Issue模式技术实现

**使用的MCP工具链**:
1. **Bash (git)**:获取GitHub仓库信息(owner/repo)
2. **Read**:读取`.spec-workflow/steering/constitution.md`提取技术约束
3. **mcp__memory**(可选):读取项目知识库(Epic信息、相关Issue)
4. **mcp__github__create_issue**:在GitHub上创建Issue(核心工具)
5. **mcp__serena**(可选):根据描述关键词定位相关代码,推荐可能需要修改的文件

**实现逻辑**:
```
1. 用户输入解析 → 提取Issue类型、描述、关键词
2. 获取仓库信息 → git remote get-url origin → 解析owner/repo
3. 交互式询问(可选) → Epic关联、优先级确认
4. Constitution检查 → 提取相关技术约束(基于关键词匹配)
5. 模块识别 → 根据关键词识别模块(医案/处方/患者等)
6. 模板组装 → 根据Issue类型选择模板
7. 智能填充 → 自动填充可推断的字段
8. GitHub创建 → 调用mcp__github__create_issue创建Issue
9. 输出结果 → 返回Issue URL和编号
```

### 批量模式技术实现(v1.2新增)

**使用的MCP工具链**:
```
1. Read: 读取task文档
   → mcp__filesystem__read_text_file(docs/tasks/*.md)

2. Grep: 提取任务清单(搜索"#### Task")
   → Grep(pattern="#### Task \d+\.\d+:", path=task_doc)

3. mcp__github__create_issue: 批量创建Issues
   → 循环调用,每个Task创建一个Issue

4. mcp__github__update_issue: 更新Issue依赖关系(可选)
   → 在Issue描述中添加依赖关系说明
```

**核心算法伪代码**:
```python
def batch_create_issues_from_task_doc(task_doc_path):
    """
    从task文档批量生成Issues
    """
    # Step 1: 读取task文档
    task_doc = Read(task_doc_path)

    # Step 2: 提取元数据
    epic_number = extract_epic_number(task_doc)
    design_doc = extract_design_doc(task_doc)
    requirements_doc = extract_requirements_doc(task_doc)

    # Step 3: 解析任务清单
    tasks = parse_task_checklist(task_doc)

    # Step 4: 批量创建Issues
    issue_mapping = {}
    for task in tasks:
        issue_body = generate_issue_body(
            task=task,
            epic_number=epic_number,
            design_doc=design_doc,
            requirements_doc=requirements_doc,
            issue_mapping=issue_mapping
        )

        issue = mcp__github__create_issue(
            owner="shouqitao",
            repo="LYBTZYZS",
            title=f"Task {task['id']}: {task['title']}",
            body=issue_body,
            labels=["task", f"phase-{task['phase']}", task['type'].lower()]
        )

        issue_mapping[task['id']] = issue['number']

    return issue_mapping
```

---

## 依赖关系处理(批量模式)

### 依赖关系识别逻辑

**从task文档解析**:
```
Task 1.2依赖Task 1.1
  ↓
创建Issue时映射
  ↓
Issue #1602依赖Issue #1601
  ↓
在Issue #1602描述中标注"依赖Issue #1601"
```

### 依赖关系标注方式

**在Issue描述中标注(自动执行)**:
```markdown
## 🔗 依赖关系
⚠️ **依赖任务**:
- Issue #1601: Task 1.1 - 创建MedicalCaseRepository扩展

**实施建议**:
等待Issue #1601完成并关闭后再开始本任务。
```

### 复杂依赖处理

**多个依赖**:
```
Task 2.1依赖Task 1.2和Task 1.4
  ↓
Issue #1605描述中标注:
⚠️ **依赖任务**:
- Issue #1602: Task 1.2 - 实现MedicalCaseService业务逻辑
- Issue #1604: Task 1.4 - 配置AutoMapper Profile

**实施建议**:
等待Issue #1602和Issue #1604都完成后再开始本任务。
```

---

## 限制条件

### 单Issue模式限制

- 仅支持GitHub Issue格式(目前不支持其他平台如GitLab/Jira)
- 需要GitHub认证(通过mcp__github工具)
- 需要git仓库配置了GitHub远程地址
- Constitution文件必须存在(.spec-workflow/steering/constitution.md)
- 关键词匹配依赖项目特定术语(医案、处方、患者等)
- 工作量估算基于经验规则,可能不准确
- Epic关联需要手动确认(无法自动识别)

### 批量模式限制(v1.2新增)

1. **需要task文档**:必须提供符合标准格式的task文档(由lybtzyzs-task-breakdown生成)
2. **GitHub API限制**:批量创建可能受GitHub API速率限制(建议<50个Issues/批次)
3. **依赖关系更新**:依赖关系仅标注在描述中,不使用GitHub原生依赖功能(需GitHub Projects)
4. **Epic关联**:必须在task文档中明确Epic编号
5. **Issue顺序**:按task文档顺序创建,不保证Issue编号连续
6. **无法回滚**:Issues创建后无法批量删除,需要手动关闭

---

## 最佳实践

### 单Issue模式最佳实践

1. **清晰描述**:提供清晰的任务描述,包含关键词(如"医案"、"处方")
2. **选择正确类型**:Feature(新功能)vs Enhancement(改进现有功能)
3. **关联Epic**:如果任务属于已有Epic,务必关联
4. **验收标准具体化**:生成后根据实际情况细化验收标准
5. **工作量验证**:生成的工作量估算仅供参考,需根据实际情况调整
6. **技术约束检查**:认真阅读生成的技术约束,避免违规实施

### 批量模式最佳实践(v1.2新增)

**使用前**:
1. **审查task文档**:确保task文档格式正确、内容准确
2. **验证Epic**:确认Epic编号正确
3. **检查任务粒度**:确保每个任务2-4小时(避免过大或过小)

**使用中**:
4. **分批创建**:如果任务数>20个,建议分批创建(按Phase)
5. **实时检查**:创建过程中检查输出,确保无错误

**使用后**:
6. **验证依赖**:检查依赖关系标注是否清晰
7. **使用GitHub Projects**:在Projects中可视化依赖关系
8. **更新Epic**:确认Epic下的所有子Issues已正确关联

---

## 性能指标

| 模式 | 操作 | 时间 |
|-----|------|------|
| **单Issue模式** | 基础模板生成 | <3秒 |
| | 包含Constitution检查 | <5秒 |
| | GitHub Issue创建 | <2秒 |
| | 端到端完成 | <7秒 |
| | 包含代码分析 | <10秒 |
| **批量模式** | Task文档解析 | <3秒 |
| | 单个Issue创建 | <2秒 |
| | 批量创建(8个) | <16秒 |
| | 依赖关系标注 | <5秒 |
| | 端到端完成(8个) | <24秒 |

---

## 与其他Skill的协同(v1.2新增)

### Skill工作流

```
设计阶段:lybtzyzs-design-arch-validator
  ↓ 架构验证通过
任务分解:lybtzyzs-task-breakdown
  ↓ 生成task文档
Issue创建:lybtzyzs-issue-template(批量模式)⭐
  ↓ 批量生成Issues
实施阶段:Issue-Driven开发
```

### Skill对比

| Skill | 输入 | 输出 | 用途 |
|-------|------|------|------|
| **lybtzyzs-design-arch-validator** | 设计文档 | 架构验证报告 | 架构合规性检查 |
| **lybtzyzs-task-breakdown** | 设计文档 | Task文档 | 任务拆分 |
| **lybtzyzs-issue-template(单模式)** | 简要描述 | 单个Issue | 单个任务创建 |
| **lybtzyzs-issue-template(批量模式)** | Task文档 | 批量Issues | 批量任务创建 |

---

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-22 | 初始版本,支持6种Issue类型 |
| v1.1 | 2025-10-22 | 更新为直接在GitHub上创建Issue,返回Issue URL |
| v1.2 | 2025-10-26 | 新增批量模式,支持从task文档批量生成Issues,自动标注依赖关系 |

---

## 相关文档

- **批量模式详细文档**:`.claude/skills/lybtzyzs-issue-template/BATCH-MODE.md`(保留作为详细参考)
- **Task分解文档**:`.claude/skills/lybtzyzs-task-breakdown/SKILL.md`
- **Skills总览**:`.claude/skills/README.md`

---

**维护者**:Claude Code
**反馈渠道**:GitHub Issues
**最后更新**:2025-10-26


---

# 批量模式

# LYBTZYZS Issue Template - 批量模式扩展文档（v1.2）

> **🔗 关联Skill**：lybtzyzs-issue-template
> **📦 新增版本**：v1.2 (2025-10-26)
> **🎯 核心功能**：从task文档批量生成GitHub Issues

---

## 📋 功能概述

**批量模式**允许从task文档（由lybtzyzs-task-breakdown生成）一次性创建多个相关的GitHub Issues，自动保持任务间的依赖关系。

### 与单Issue模式的对比

| 特性 | 单Issue模式（v1.0/1.1） | 批量模式（v1.2） |
|-----|----------------------|----------------|
| 输入 | 简要描述 | Task文档路径 |
| 创建数量 | 1个Issue | N个Issues（批量） |
| 依赖关系 | 手动标注 | 自动识别并标注 |
| Epic关联 | 交互式选择 | 自动从task文档提取 |
| 适用场景 | 单个Bug/Feature | Epic拆分、批量任务创建 |

---

## 🔄 工作流程

### 完整流程

```
设计文档完成
  ↓
运行 lybtzyzs-design-arch-validator（验证架构）
  ↓
运行 lybtzyzs-task-breakdown（生成task文档）
  ↓
运行 lybtzyzs-issue-template（批量模式）⭐
  ↓
GitHub Issues批量创建完成
```

### 批量模式执行步骤

```
Step 1: 读取task文档
  → Read(docs/tasks/*.md)
  → 解析元数据（Epic、设计文档、需求文档）

Step 2: 解析任务清单
  → 提取所有Task（#### Task X.Y: ...）
  → 提取元数据（工作量、依赖、类型、文件、验收标准）

Step 3: 批量创建Issues
  → 遍历每个Task
  → 生成Issue描述（基于task元数据）
  → 调用mcp__github__create_issue
  → 保存Issue编号（用于依赖关系映射）

Step 4: 标注依赖关系
  → 将Task依赖（Task 1.1）映射到Issue依赖（Issue #1601）
  → 在Issue描述中标注"依赖Issue #XXX"

Step 5: 输出结果
  → 显示所有创建的Issues（URL、编号、依赖关系）
  → 显示统计信息（数量、工作量、Epic）
```

---

## 📥 输入格式要求

### Task文档标准格式

**必须符合lybtzyzs-task-breakdown的输出格式**：

```markdown
# {Feature Name} 任务分解文档

## 📋 元数据
- Epic: #XXXX
- 设计文档: docs/design/{feature-name}-design.md
- 需求文档: docs/requirements/{feature-name}-requirements.md
- 总工作量: X-Y小时

## 🎯 任务清单

### Phase 1: {阶段名称}

#### Task 1.1: {任务标题}
- **工作量**: X-Y小时
- **依赖**: 无 / Task X.Y
- **类型**: Repository / Service / Controller / ViewModel / View
- **文件范围**:
  - `src/.../XXX.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 具体功能验证
- **技术要点**:
  - 关键实现细节
  - 注意事项

#### Task 1.2: {任务标题}
- **工作量**: X-Y小时
- **依赖**: Task 1.1
...
```

### 关键字段说明

| 字段 | 必需 | 用途 |
|-----|------|------|
| Epic | ✅ 是 | 关联Epic编号 |
| Task标题 | ✅ 是 | Issue标题 |
| 工作量 | ✅ 是 | 显示在Issue中 |
| 依赖 | ✅ 是 | 生成依赖关系标注 |
| 类型 | ⚠️ 推荐 | 用于标签分类 |
| 文件范围 | ⚠️ 推荐 | 实施指导 |
| 验收标准 | ✅ 是 | Issue验收清单 |
| 技术要点 | ⚠️ 推荐 | 实施指导 |

---

## 📤 输出格式

### 控制台输出示例

```
✅ 批量Issue创建完成！

📁 Task文档：docs/tasks/medicalcase-enhancement-tasks.md
📊 创建统计：
- 成功创建：8个Issues
- 关联Epic：#1494
- 总工作量：18-24小时

📋 创建的Issues：

Phase 1: Server端基础层
  ✅ Issue #1601: Task 1.1 - 创建MedicalCaseRepository扩展
     https://github.com/shouqitao/LYBTZYZS/issues/1601
     工作量: 1.5-2小时 | 依赖: 无

  ✅ Issue #1602: Task 1.2 - 实现MedicalCaseService业务逻辑
     https://github.com/shouqitao/LYBTZYZS/issues/1602
     工作量: 2.5-3小时 | 依赖: Issue #1601

  ✅ Issue #1603: Task 1.3 - 创建Consultation/Prescription DTO
     https://github.com/shouqitao/LYBTZYZS/issues/1603
     工作量: 1-1.5小时 | 依赖: Issue #1601

  ✅ Issue #1604: Task 1.4 - 配置AutoMapper Profile
     https://github.com/shouqitao/LYBTZYZS/issues/1604
     工作量: 1-1.5小时 | 依赖: Issue #1603

Phase 2: Server端API层
  ✅ Issue #1605: Task 2.1 - 实现MedicalCaseController API端点
     https://github.com/shouqitao/LYBTZYZS/issues/1605
     工作量: 2-3小时 | 依赖: Issue #1602, Issue #1604

  ✅ Issue #1606: Task 2.2 - 编写API集成测试
     https://github.com/shouqitao/LYBTZYZS/issues/1606
     工作量: 2-3小时 | 依赖: Issue #1605

Phase 3: Client端集成
  ✅ Issue #1607: Task 3.1 - 创建MedicalCaseViewModel扩展
     https://github.com/shouqitao/LYBTZYZS/issues/1607
     工作量: 3-4小时 | 依赖: Issue #1605

  ✅ Issue #1608: Task 3.2 - 更新MedicalCaseView UI
     https://github.com/shouqitao/LYBTZYZS/issues/1608
     工作量: 5-6小时 | 依赖: Issue #1607

🔗 依赖关系已标注在Issue描述中
📈 Epic #1494进度：8个子Issues已创建

💡 下一步：
在GitHub Projects中查看Issues，按依赖关系排序后开始实施
```

### 单个Issue描述格式

```markdown
## 📝 任务描述
{任务标题}

**技术要点**：
- {技术要点1}
- {技术要点2}

## 🎯 目标
实现{任务标题}，作为Epic #{Epic编号}的一部分。

## ✅ 验收标准
- [ ] {验收标准1}
- [ ] {验收标准2}
- [ ] {验收标准3}

## 📊 实施范围
**文件范围**：
- `{文件路径1}`
- `{文件路径2}`

**工作量估算**：{X-Y小时}

## 🔗 依赖关系
⚠️ **依赖任务**：
- Issue #{依赖Issue编号1}: Task X.Y - {依赖任务标题}
- Issue #{依赖Issue编号2}: Task X.Z - {依赖任务标题}

或

✅ **无依赖**：可立即开始

## 📚 参考资料
- Task文档：docs/tasks/{feature-name}-tasks.md
- 设计文档：{设计文档路径}
- 需求文档：{需求文档路径}

## 🏷️ 标签
`task` `phase-{N}` `{type}` `{module}`

---

**相关Epic**：Epic #{Epic编号}
**Phase**：Phase {N}
**类型**：{Repository/Service/Controller/ViewModel/View}
```

---

## 🎬 使用示例

### 示例1：完整Epic的批量Issue创建

**用户命令**：
```
"根据task文档批量生成Issues"
或
"批量创建Issues: docs/tasks/medicalcase-enhancement-tasks.md"
```

**执行过程**：
```
Step 1: 读取task文档
✓ docs/tasks/medicalcase-enhancement-tasks.md
✓ Epic: #1494
✓ 识别8个任务

Step 2: 解析任务
✓ Task 1.1: 工作量1.5-2h, 依赖无
✓ Task 1.2: 工作量2.5-3h, 依赖Task 1.1
✓ Task 1.3: 工作量1-1.5h, 依赖Task 1.1
✓ Task 1.4: 工作量1-1.5h, 依赖Task 1.3
✓ Task 2.1: 工作量2-3h, 依赖Task 1.2, Task 1.4
✓ Task 2.2: 工作量2-3h, 依赖Task 2.1
✓ Task 3.1: 工作量3-4h, 依赖Task 2.1
✓ Task 3.2: 工作量5-6h, 依赖Task 3.1

Step 3: 批量创建（8个API调用）
✓ Issue #1601 (Task 1.1)
✓ Issue #1602 (Task 1.2, 依赖#1601)
✓ Issue #1603 (Task 1.3, 依赖#1601)
✓ Issue #1604 (Task 1.4, 依赖#1603)
✓ Issue #1605 (Task 2.1, 依赖#1602, #1604)
✓ Issue #1606 (Task 2.2, 依赖#1605)
✓ Issue #1607 (Task 3.1, 依赖#1605)
✓ Issue #1608 (Task 3.2, 依赖#1607)

Step 4: 标注依赖
✓ 所有依赖关系已标注

Step 5: 输出
✓ 详细清单已显示
```

### 示例2：分Phase批量创建

**场景**：只想先创建Phase 1的Issues

**用户命令**：
```
"只为Phase 1创建Issues: docs/tasks/medicalcase-enhancement-tasks.md"
```

**执行过程**：
```
Step 1: 读取task文档
✓ 识别Phase 1的4个任务

Step 2: 批量创建Phase 1 Issues
✓ Issue #1601 (Task 1.1)
✓ Issue #1602 (Task 1.2, 依赖#1601)
✓ Issue #1603 (Task 1.3, 依赖#1601)
✓ Issue #1604 (Task 1.4, 依赖#1603)

Step 3: 输出
✓ Phase 1的4个Issues创建完成
```

**输出**：
```
✅ Phase 1 Issues创建完成！

📊 创建统计：
- 成功创建：4个Issues
- Phase：Phase 1 - Server端基础层
- 工作量：6-8小时

💡 提示：
Phase 2和Phase 3的Issues可以稍后创建
建议：Phase 1完成后再创建后续Phase的Issues
```

---

## 🔗 依赖关系处理

### 依赖关系识别逻辑

**从task文档解析**：
```
Task 1.2依赖Task 1.1
  ↓
创建Issue时映射
  ↓
Issue #1602依赖Issue #1601
  ↓
在Issue #1602描述中标注"依赖Issue #1601"
```

### 依赖关系标注方式

**方式1：在Issue描述中标注（推荐，自动执行）**
```markdown
## 🔗 依赖关系
⚠️ **依赖任务**：
- Issue #1601: Task 1.1 - 创建MedicalCaseRepository扩展

**实施建议**：
等待Issue #1601完成并关闭后再开始本任务。
```

**方式2：添加GitHub评论（可选，未实现）**
```
💡 任务依赖提示：
本任务依赖以下Issues完成：
- #1601 Task 1.1 - 创建MedicalCaseRepository扩展

请确保依赖任务完成后再开始实施。
```

**方式3：使用GitHub Projects（需要手动）**
```
在GitHub Projects Kanban中：
- 将Issues按依赖关系排列
- 使用"Blocked by"标签标注依赖
- 配置自动化规则（依赖Issue关闭后移动到Ready列）
```

### 复杂依赖处理

**多个依赖**：
```
Task 2.1依赖Task 1.2和Task 1.4
  ↓
Issue #1605描述中标注：
⚠️ **依赖任务**：
- Issue #1602: Task 1.2 - 实现MedicalCaseService业务逻辑
- Issue #1604: Task 1.4 - 配置AutoMapper Profile

**实施建议**：
等待Issue #1602和Issue #1604都完成后再开始本任务。
```

**跨Phase依赖**：
```
Task 3.1依赖Task 2.1（跨Phase）
  ↓
Issue #1607描述中标注：
⚠️ **依赖任务**：
- Issue #1605: Task 2.1 - 实现MedicalCaseController API端点（Phase 2）

**实施建议**：
需要等待Phase 2的Issue #1605完成，确保API可用后再开始ViewModel实现。
```

---

## 🛠️ 技术实现

### MCP工具链

```
1. Read: 读取task文档
   → mcp__filesystem__read_text_file(docs/tasks/*.md)

2. Grep: 提取任务清单（搜索"#### Task"）
   → Grep(pattern="#### Task \d+\.\d+:", path=task_doc)

3. mcp__github__create_issue: 批量创建Issues
   → 循环调用，每个Task创建一个Issue

4. mcp__github__update_issue: 更新Issue依赖关系（可选）
   → 在Issue描述中添加依赖关系说明
```

### 核心算法伪代码

```python
def batch_create_issues_from_task_doc(task_doc_path):
    """
    从task文档批量生成Issues
    """
    # Step 1: 读取task文档
    task_doc = Read(task_doc_path)

    # Step 2: 提取元数据
    epic_number = extract_epic_number(task_doc)
    design_doc = extract_design_doc(task_doc)
    requirements_doc = extract_requirements_doc(task_doc)

    # Step 3: 解析任务清单
    tasks = parse_task_checklist(task_doc)
    # tasks = [
    #   {
    #     "id": "Task 1.1",
    #     "title": "创建MedicalCaseRepository扩展",
    #     "phase": "Phase 1",
    #     "effort": "1.5-2小时",
    #     "dependencies": [],  # 无依赖
    #     "type": "Repository",
    #     "files": ["src/..."],
    #     "acceptance_criteria": ["编译通过", ...],
    #     "tech_points": ["扩展GetOtherCasesAsync方法", ...]
    #   },
    #   ...
    # ]

    # Step 4: 批量创建Issues
    issue_mapping = {}  # {task_id: issue_number}
    for task in tasks:
        # 生成Issue描述
        issue_body = generate_issue_body(
            task=task,
            epic_number=epic_number,
            design_doc=design_doc,
            requirements_doc=requirements_doc,
            issue_mapping=issue_mapping
        )

        # 调用GitHub API
        issue = mcp__github__create_issue(
            owner="shouqitao",
            repo="LYBTZYZS",
            title=f"Task {task['id']}: {task['title']}",
            body=issue_body,
            labels=["task", f"phase-{task['phase']}", task['type'].lower()]
        )

        # 保存Issue编号
        issue_mapping[task['id']] = issue['number']

    # Step 5: 输出统计
    return issue_mapping

def parse_task_checklist(task_doc):
    """
    解析task文档中的任务清单
    使用Grep工具搜索Task标题
    """
    # 搜索Task标题（#### Task X.Y: ...）
    task_lines = Grep(
        pattern=r"#### Task \d+\.\d+:",
        path=task_doc,
        output_mode="content",
        line_numbers=True
    )

    tasks = []
    for line_number, line_content in task_lines:
        # 提取Task ID和标题
        task_id = extract_task_id(line_content)  # "Task 1.1"
        task_title = extract_task_title(line_content)

        # 提取任务详细信息（工作量、依赖等）
        task_details = extract_task_details(task_doc, line_number)

        tasks.append({
            "id": task_id,
            "title": task_title,
            **task_details
        })

    return tasks

def extract_task_details(task_doc, start_line_number):
    """
    从Task标题行开始向后扫描，提取任务详细信息
    """
    details = {
        "effort": "",
        "dependencies": [],
        "type": "",
        "files": [],
        "acceptance_criteria": [],
        "tech_points": []
    }

    # 读取Task标题后续的N行（直到下一个Task或Section）
    lines = task_doc.split('\n')
    i = start_line_number
    while i < len(lines):
        line = lines[i].strip()

        # 遇到下一个Task或Section，停止
        if line.startswith("#### Task") or line.startswith("###"):
            break

        # 提取各字段
        if line.startswith("- **工作量**:"):
            details["effort"] = line.split(":", 1)[1].strip()
        elif line.startswith("- **依赖**:"):
            dep_str = line.split(":", 1)[1].strip()
            if dep_str != "无":
                details["dependencies"] = [d.strip() for d in dep_str.split(",")]
        elif line.startswith("- **类型**:"):
            details["type"] = line.split(":", 1)[1].strip()
        elif line.startswith("  - `"):
            details["files"].append(line.strip("  - `").strip("`"))
        elif line.startswith("  - [ ]"):
            details["acceptance_criteria"].append(line.strip("  - [ ]").strip())
        elif line.startswith("  - ") and not line.startswith("  - [ ]"):
            details["tech_points"].append(line.strip("  - ").strip())

        i += 1

    return details

def generate_issue_body(task, epic_number, design_doc, requirements_doc, issue_mapping):
    """
    从task元数据生成Issue描述
    """
    # 解析依赖Issue编号
    dependency_text = ""
    if task["dependencies"]:
        dependency_text = "⚠️ **依赖任务**：\n"
        for dep_id in task["dependencies"]:
            dep_issue_number = issue_mapping.get(dep_id, "待创建")
            dependency_text += f"- Issue #{dep_issue_number}: {dep_id}\n"
    else:
        dependency_text = "✅ **无依赖**：可立即开始"

    # 组装Issue描述
    return f"""## 📝 任务描述
{task['title']}

**技术要点**：
{chr(10).join([f"- {point}" for point in task['tech_points']])}

## 🎯 目标
实现{task['title']}，作为Epic #{epic_number}的一部分。

## ✅ 验收标准
{chr(10).join([f"- [ ] {criteria}" for criteria in task['acceptance_criteria']])}

## 📊 实施范围
**文件范围**：
{chr(10).join([f"- `{file}`" for file in task['files']])}

**工作量估算**：{task['effort']}

## 🔗 依赖关系
{dependency_text}

## 📚 参考资料
- Task文档：docs/tasks/（从文档路径提取）
- 设计文档：{design_doc}
- 需求文档：{requirements_doc}

## 🏷️ 标签
`task` `{task['phase'].lower().replace(' ', '-')}` `{task['type'].lower()}`

---

**相关Epic**：Epic #{epic_number}
**Phase**：{task['phase']}
**类型**：{task['type']}
"""
```

---

## ⚠️ 限制条件

1. **需要task文档**：必须提供符合标准格式的task文档（由lybtzyzs-task-breakdown生成）
2. **GitHub API限制**：批量创建可能受GitHub API速率限制（建议<50个Issues/批次）
3. **依赖关系更新**：依赖关系仅标注在描述中，不使用GitHub原生依赖功能（需GitHub Projects）
4. **Epic关联**：必须在task文档中明确Epic编号
5. **Issue顺序**：按task文档顺序创建，不保证Issue编号连续
6. **无法回滚**：Issues创建后无法批量删除，需要手动关闭

---

## 📊 性能指标

- Task文档解析：<3秒
- 单个Issue创建：<2秒（GitHub API调用）
- 批量创建（8个Issues）：<16秒
- 依赖关系标注：<5秒
- **端到端完成**：<24秒（8个Issues）

---

## ✅ 最佳实践

### 使用前

1. **审查task文档**：确保task文档格式正确、内容准确
2. **验证Epic**：确认Epic编号正确
3. **检查任务粒度**：确保每个任务2-4小时（避免过大或过小）

### 使用中

4. **分批创建**：如果任务数>20个，建议分批创建（按Phase）
5. **实时检查**：创建过程中检查输出，确保无错误

### 使用后

6. **验证依赖**：检查依赖关系标注是否清晰
7. **使用GitHub Projects**：在Projects中可视化依赖关系
8. **更新Epic**：确认Epic下的所有子Issues已正确关联

---

## 🔗 与其他Skill的协同

### Skill工作流

```
设计阶段：lybtzyzs-design-arch-validator
  ↓ 架构验证通过
任务分解：lybtzyzs-task-breakdown
  ↓ 生成task文档
Issue创建：lybtzyzs-issue-template（批量模式）⭐
  ↓ 批量生成Issues
实施阶段：Issue-Driven开发
```

### Skill对比

| Skill | 输入 | 输出 | 用途 |
|-------|------|------|------|
| **lybtzyzs-design-arch-validator** | 设计文档 | 架构验证报告 | 架构合规性检查 |
| **lybtzyzs-task-breakdown** | 设计文档 | Task文档 | 任务拆分 |
| **lybtzyzs-issue-template（单模式）** | 简要描述 | 单个Issue | 单个任务创建 |
| **lybtzyzs-issue-template（批量模式）** | Task文档 | 批量Issues | 批量任务创建 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-10-26


---

# 集成指南

# Issue Template Skill 批量模式整合指南

> **📅 创建时间**：2025-10-26
> **🎯 目的**：指导将BATCH-MODE.md的内容整合到SKILL.md主文件中

---

## 📋 整合说明

当前状态：
- ✅ **SKILL.md**：包含单Issue模式的完整文档（v1.0/1.1）
- ✅ **BATCH-MODE.md**：包含批量模式的完整文档（v1.2新增）
- ⚠️ **需要整合**：将批量模式内容合并到SKILL.md主文件

---

## 🔧 整合方案

### 方案A：保持分离（推荐）✅

**优点**：
- 文档结构清晰（单模式 vs 批量模式）
- 便于维护和更新
- 避免单个文件过长（SKILL.md已有384行）

**当前结构**：
```
.claude/skills/lybtzyzs-issue-template/
├── SKILL.md (384行) - 单Issue模式完整文档
├── BATCH-MODE.md (新建) - 批量模式完整文档
└── INTEGRATION-GUIDE.md (本文件) - 整合指南
```

**使用方式**：
- 单Issue模式 → 查阅SKILL.md
- 批量模式 → 查阅BATCH-MODE.md
- 快速对比 → 查阅BATCH-MODE.md第13-21行的对比表

### 方案B：完全合并（可选）

如果需要合并成单文件，按以下章节顺序整合：

#### 1. 修改文件头（SKILL.md Line 1-7）

**原内容**：
```markdown
---
name: lybtzyzs-issue-template
description: 为LYBTZYZS项目生成标准化GitHub Issue模板，支持Feature、Bug Fix、Refactor、Documentation、Test、Chore等类型。自动关联Epic、生成验收清单、提取技术约束。触发关键词：创建Issue、新建任务、Issue模板、generate issue、标准化Issue、create issue、issue template
---

# LYBTZYZS Issue模板生成器
```

**修改为**：
```markdown
---
name: lybtzyzs-issue-template
description: 为LYBTZYZS项目生成标准化GitHub Issue模板，支持单Issue模式（Feature/Bug/Refactor等）和批量模式（从task文档批量创建）。触发关键词：创建Issue、批量创建Issues、根据task文档生成Issues
---

# LYBTZYZS Issue模板生成器

> **📦 版本**：v1.2（新增批量模式）
> **📅 最后更新**：2025-10-26
```

#### 2. 修改"核心能力"章节（SKILL.md Line 8-16）

**在Line 16后添加**：
```markdown

### 批量模式（v1.2新增）⭐
8. **从task文档批量生成GitHub Issues**
9. **自动识别并标注任务间依赖关系**
10. **Epic自动关联**
11. **批量创建（一次性创建N个Issues）**
```

#### 3. 修改"何时使用"章节（SKILL.md Line 18-24）

**替换为**：
```markdown
## 何时使用

### 单Issue模式
- 创建新的Feature、Bug Fix或Refactor任务时
- 需要规范化Issue格式确保信息完整时
- 希望自动关联Epic和生成验收清单时
- 新成员创建Issue需要指导时
- 需要快速标准化任务描述时

### 批量模式（v1.2新增）⭐
- 设计文档完成并生成task文档后（使用lybtzyzs-task-breakdown）
- 需要将Epic拆分成多个子Issues
- 需要为多个任务快速创建Issues
- 需要保持任务间依赖关系清晰
```

#### 4. 修改"工作流程"章节（SKILL.md Line 26-33）

**替换为**：
```markdown
## 工作流程

### 单Issue模式（原有流程）

1. 询问Issue类型（Feature/Bug Fix/Refactor/Documentation/Test/Chore）
2. 收集必要信息（标题、简要描述、优先级）
3. 可选：询问关联的Epic（如Epic #1494 - 医案流程模块）
4. 读取Constitution提取相关技术约束
5. 生成标准化Issue内容
6. **在GitHub上创建Issue并返回Issue URL**

### 批量模式（v1.2新增）⭐

[从BATCH-MODE.md复制Line 41-65的内容]
```

#### 5. 修改"输入要求"章节（SKILL.md Line 35-44）

**替换为**：
```markdown
## 输入要求

### 单Issue模式

**必需**：
- Issue类型（从6种类型中选择）
- 简要描述（1-2句话说明要做什么）

**可选**：
- 关联的Epic编号
- 优先级（高/中/低，默认：中）
- 预期完成时间

### 批量模式（v1.2新增）

**必需**：
- Task文档路径（docs/tasks/*.md）
- Task文档必须符合lybtzyzs-task-breakdown的标准格式

**可选**：
- Phase过滤（只为特定Phase创建Issues）
```

#### 6. 在"示例"章节后添加批量模式示例

**在SKILL.md Line 321后添加**：
```markdown

### 示例3：批量模式 - 从task文档创建Issues（v1.2新增）

[从BATCH-MODE.md复制Line 277-370的完整示例]
```

#### 7. 在"技术实现"章节后添加批量模式实现

**在SKILL.md Line 343后添加**：
```markdown

### 批量模式技术实现（v1.2新增）

[从BATCH-MODE.md复制Line 465-875的技术实现部分]
```

#### 8. 更新"版本历史"章节（SKILL.md Line 372-377）

**修改为**：
```markdown
## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-22 | 初始版本，支持6种Issue类型 |
| v1.1 | 2025-10-22 | 更新为直接在GitHub上创建Issue，返回Issue URL |
| v1.2 | 2025-10-26 | 新增批量模式，支持从task文档批量生成Issues，自动标注依赖关系 |
```

#### 9. 更新最后更新时间（SKILL.md Line 383）

**修改为**：
```markdown
**最后更新**：2025-10-26
```

---

## ✅ 推荐做法（当前状态）

**保持文件分离**（方案A），理由：

1. **SKILL.md已经很长**：384行，再添加批量模式会超过600行
2. **职责分离**：单模式和批量模式是两种不同的使用场景
3. **便于维护**：独立文件更容易更新和查找
4. **Claude自动加载**：Claude会根据description自动加载对应Skill

**文档交叉引用**：

在SKILL.md末尾添加：
```markdown
---

## 🔗 相关文档

**批量模式详细文档**：`.claude/skills/lybtzyzs-issue-template/BATCH-MODE.md`

批量模式支持从task文档（由lybtzyzs-task-breakdown生成）一次性创建多个Issues，自动标注依赖关系和关联Epic。适用于Epic拆分和批量任务创建场景。
```

在BATCH-MODE.md开头已经有交叉引用：
```markdown
> **🔗 关联Skill**：lybtzyzs-issue-template
```

---

## 📊 文件结构对比

### 当前结构（推荐）✅
```
lybtzyzs-issue-template/
├── SKILL.md (384行)
│   ├── 核心能力（单模式）
│   ├── 使用场景（单模式）
│   ├── 工作流程（单模式）
│   ├── 输入输出（单模式）
│   ├── Issue模板（6种类型）
│   ├── 示例（2个单模式示例）
│   └── 技术实现（单模式）
│
├── BATCH-MODE.md (新建, ~900行)
│   ├── 批量模式概述
│   ├── 工作流程（批量）
│   ├── 输入格式（task文档）
│   ├── 输出格式（批量Issues）
│   ├── 示例（2个批量示例）
│   ├── 依赖关系处理
│   ├── 技术实现（批量）
│   └── 最佳实践
│
└── INTEGRATION-GUIDE.md (本文件)
    └── 整合指南和建议
```

### 合并后结构（可选）
```
lybtzyzs-issue-template/
└── SKILL.md (预计1200+行)
    ├── 核心能力（单模式 + 批量模式）
    ├── 使用场景（单模式 + 批量模式）
    ├── 工作流程（单模式 + 批量模式）
    ├── 输入输出（单模式 + 批量模式）
    ├── Issue模板（6种类型）
    ├── 示例（2个单模式 + 2个批量模式）
    ├── 技术实现（单模式 + 批量模式）
    └── 版本历史
```

---

## 💡 实施建议

**短期（当前）**：
- ✅ 保持文件分离（SKILL.md + BATCH-MODE.md）
- ✅ 在两个文件中添加交叉引用
- ✅ 更新CLAUDE.md和Skills/README.md指向两个文档

**中期（1个月后）**：
- 根据实际使用情况决定是否合并
- 如果批量模式使用频率很高，考虑合并成单文件
- 如果批量模式独立性强，继续保持分离

**长期（3个月后）**：
- 考虑创建统一的Skills文档生成器
- 自动合并多个文档片段生成完整Skill文档
- 支持模块化Skill定义

---

## ✅ 当前状态

**已完成**：
- ✅ BATCH-MODE.md已创建（完整批量模式文档）
- ✅ INTEGRATION-GUIDE.md已创建（本文件）
- ✅ Skills/README.md已更新（包含批量模式说明）
- ✅ CLAUDE.md已更新（Skills章节包含批量模式）

**推荐下一步**：
- 保持当前文件结构（分离模式）
- 用户可以根据需要查阅对应文档
- Claude会根据触发关键词自动加载对应模式

---

**维护者**：Claude Code
**创建日期**：2025-10-26
