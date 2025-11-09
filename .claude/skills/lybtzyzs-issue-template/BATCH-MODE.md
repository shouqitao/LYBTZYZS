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
- 设计文档: docs/explanation/{feature-name}-design.md
- 需求文档: docs/explanation/{feature-name}-requirements-discussion.md
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
