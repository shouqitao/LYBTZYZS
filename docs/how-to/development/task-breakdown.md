---
name: lybtzyzs-task-breakdown
description: 为LYBTZYZS项目从设计文档自动生成结构化任务分解清单，支持Phase划分、依赖分析、工作量估算。输出标准化task文档供批量生成Issues。触发关键词：任务分解、生成任务清单、task breakdown、拆分任务、分解设计、task decomposition
---

# LYBTZYZS 任务分解生成器

## 核心能力

1. **智能任务拆分**：根据设计文档的Phase/模块/职责自动分解
2. **依赖关系分析**：识别任务间的依赖关系（如Repository → Service → Controller）
3. **工作量估算**：基于代码量和复杂度评估时间（标准任务2-4小时）
4. **Phase划分**：按实施顺序分组（Phase 1/2/3）
5. **验收标准生成**：每个任务自动生成具体的验收标准
6. **关键路径识别**：标注关键任务和依赖链
7. **输出标准task文档**：供lybtzyzs-issue-template批量生成Issues

## 何时使用

- 设计文档完成并通过架构验证后（lybtzyzs-design-arch-validator通过）
- 需要将Epic拆分成可执行的子任务
- 需要规划实施顺序和依赖关系
- 需要标准化task文档供批量生成Issues
- 重构计划需要分解成多个步骤

## 工作流程

1. 读取设计文档（docs/design/*.md）
2. 解析Phase划分和技术方案
3. 智能拆分任务（按模块、职责、实施顺序）
4. 分析任务间依赖关系
5. 估算每个任务的工作量
6. 生成标准化task文档（docs/tasks/*.md）
7. 输出task清单摘要

## 输入要求

**必需**：
- 设计文档路径（docs/design/*.md）
- 或Epic描述 + 简要技术方案

**可选**：
- 任务粒度控制（默认：2-4小时/任务）
- Phase数量建议（默认：自动识别）
- Epic编号（用于关联）

## 输出格式

### 1. Task文档（标准格式）

**文件路径**：`docs/tasks/{feature-name}-tasks.md`

**文档结构**：
```markdown
# {Feature Name} 任务分解文档

## 📋 元数据
- Epic: #XXXX
- 设计文档: docs/design/{feature-name}-design.md
- 需求文档: docs/requirements/{feature-name}-requirements.md
- 总工作量: X-Y小时
- 实施阶段: Phase 1-N

## 🎯 任务清单（Task Checklist）

### Phase 1: {阶段名称}（预计X-Y小时）

#### Task 1.1: {任务标题}
- **工作量**: X-Y小时
- **依赖**: 无 / Task X.Y
- **类型**: Repository / Service / Controller / ViewModel / View / 其他
- **文件范围**:
  - `src/Server/.../XXX.cs`
  - `src/Client/.../XXX.xaml`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 单元测试通过（如果有）
  - [ ] 具体功能验证
- **技术要点**:
  - 关键实现细节
  - 注意事项

#### Task 1.2: {任务标题}
- **工作量**: X-Y小时
- **依赖**: Task 1.1
- **类型**: Service
- **文件范围**:
  - `src/Server/.../Services/XXXService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 业务逻辑测试通过
  - [ ] 符合Service层设计标准
- **技术要点**:
  - 业务规则实现
  - 异常处理

### Phase 2: {阶段名称}（预计X-Y小时）

#### Task 2.1: {任务标题}
...

## 📊 任务统计
- 总任务数: N个
- 总工作量: X-Y小时
- Phase数量: N个
- 关键路径长度: N个任务

## 🔗 依赖关系图

### Phase 1依赖
```
Task 1.1 (无依赖) → Task 1.2 → Task 1.3
```

### Phase 2依赖
```
Task 2.1 (依赖Task 1.2) → Task 2.2
```

### 跨Phase依赖
```
Phase 1 → Phase 2 → Phase 3
Task 1.2 → Task 2.1
Task 2.3 → Task 3.1
```

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：
1. Task 1.1: 创建基础Repository
2. Task 1.2: 实现Service层
3. Task 2.1: 实现Controller
4. Task 3.1: 创建ViewModel
5. Task 3.2: 实现View

**并行任务**（可同时进行）：
- Task 1.3 和 Task 1.4（都依赖Task 1.2，但互不依赖）
- Task 2.2 和 Task 2.3（都依赖Task 2.1，但互不依赖）

## 📝 实施建议

### 优先级排序
1. 🔴 高优先级：关键路径任务（Task 1.1, 1.2, 2.1, 3.1, 3.2）
2. 🟡 中优先级：功能增强任务
3. 🟢 低优先级：优化和文档任务

### 并行策略
- Phase 1完成后，可以同时开始Phase 2和部分Phase 3任务
- Task 1.3和Task 1.4可以并行开发

### 风险提示
- Task 2.1依赖Task 1.2的接口稳定，建议完整测试Task 1.2后再开始
- Client端任务（Phase 3）需要Server端API可用（Phase 2完成）

## 🧪 测试策略

### 单元测试
- Task 1.1: Repository层单元测试（Mock DbContext）
- Task 1.2: Service层单元测试（Mock Repository）

### 集成测试
- Task 2.1: API端点集成测试（真实数据库）
- Task 3.1: ViewModel集成测试（Mock Service）

### E2E测试
- Phase 3完成后：完整用户流程测试
```

### 2. 控制台输出（摘要）

```markdown
✅ 任务分解完成！

📁 Task文档已生成：docs/tasks/medicalcase-enhancement-tasks.md

📊 任务统计：
- 总任务数：8个
- 总工作量：18-24小时
- Phase划分：3个阶段
- 关键路径：5个任务

🔗 Phase划分：
- Phase 1: 基础架构（4个任务，6-8小时）
- Phase 2: API端点（2个任务，4-6小时）
- Phase 3: Client集成（2个任务，8-10小时）

⚠️ 关键依赖：
- Task 1.1 → Task 1.2 → Task 2.1 → Task 3.1
- Task 2.1必须等待Task 1.2完成
- Phase 3必须等待Phase 2完成

💡 下一步操作：
1. 审查task文档：docs/tasks/medicalcase-enhancement-tasks.md
2. 调整任务粒度（如果需要）
3. 运行批量生成Issues：使用lybtzyzs-issue-template读取task文档
```

## 任务拆分策略

### 1. 基于Phase拆分（设计文档中的Phase）

如果设计文档明确定义了Phase，直接使用：
```
设计文档 Phase 1: 数据模型 → Task文档 Phase 1: 基础架构
设计文档 Phase 2: API实现 → Task文档 Phase 2: API端点
设计文档 Phase 3: UI集成 → Task文档 Phase 3: Client集成
```

### 2. 基于技术栈拆分（三层架构）

如果设计文档没有明确Phase，按技术栈拆分：
```
Phase 1: Server端基础层
- Repository接口和实现
- Entity和DTO定义
- 数据库迁移（如果需要）

Phase 2: Server端业务层
- Service接口和实现
- Controller实现
- API端点测试

Phase 3: Client端展示层
- ViewModel实现
- View (XAML) 实现
- UI集成测试
```

### 3. 基于职责拆分（SOLID原则）

每个任务对应单一职责：
```
Task: 创建XxxRepository
- 职责：数据访问层
- 文件：1-2个文件
- 工作量：1-2小时

Task: 实现XxxService
- 职责：业务逻辑层
- 文件：1-2个文件
- 工作量：2-3小时

Task: 实现XxxController
- 职责：API端点层
- 文件：1个文件
- 工作量：1-2小时
```

### 4. 依赖关系识别规则

**自动识别依赖**：
```
规则1: Repository → Service → Controller（三层依赖）
规则2: Server API → Client ViewModel（跨端依赖）
规则3: ViewModel → View（MVVM依赖）
规则4: 基础设施 → 业务功能（基础依赖）
```

**依赖标注格式**：
```
Task 1.1: 依赖: 无（可立即开始）
Task 1.2: 依赖: Task 1.1（必须等Task 1.1完成）
Task 2.1: 依赖: Task 1.2, Task 1.3（必须等两个任务都完成）
```

### 5. 工作量估算规则

**基于复杂度**：
```
简单任务（1-1.5小时）：
- 创建简单DTO
- 简单Repository实现（CRUD）
- 简单Controller（单个端点）

中等任务（2-3小时）：
- Service层实现（包含业务逻辑）
- 复杂Controller（多个端点）
- ViewModel实现（包含Command）

复杂任务（4-6小时）：
- 复杂Service（多业务规则）
- 聚合根Repository（复杂查询）
- 复杂View（自定义控件）
```

**基于文件数量**：
```
1-2个文件：1-2小时
3-4个文件：2-4小时
5+个文件：4-6小时（建议拆分成更小任务）
```

## 技术实现

### 使用的MCP工具链

1. **Read**：读取设计文档内容
2. **Grep**：搜索Phase标记、技术方案关键词
3. **mcp__serena__find_symbol**：分析相关代码结构（如果需要）
4. **mcp__sequential-thinking**：深度分析任务拆分策略
5. **Write**：生成task文档
6. **mcp__memory**（可选）：读取项目知识库（架构模式、代码规范）

### 实现逻辑

```
Step 1: 读取设计文档
  → Read(docs/design/*.md)
  → 提取Phase、技术方案、文件范围

Step 2: 解析设计文档结构
  → Grep搜索"Phase"、"## 实施步骤"等关键词
  → 识别技术栈（Server/Client）
  → 识别模块（MedicalCase/Prescription等）

Step 3: 智能任务拆分（核心算法）
  → 如果有明确Phase → 按Phase拆分
  → 如果没有Phase → 按三层架构拆分（Repository → Service → Controller → ViewModel → View）
  → 每个任务控制在2-4小时粒度

Step 4: 依赖关系分析
  → 应用依赖规则（Repository → Service → Controller）
  → 识别跨Phase依赖（Server → Client）
  → 标注并行任务（同一依赖的独立任务）

Step 5: 工作量估算
  → 基于复杂度规则（简单/中等/复杂）
  → 基于文件数量（1-2个文件 = 1-2小时）
  → 生成区间估算（X-Y小时）

Step 6: 生成task文档
  → Write(docs/tasks/{feature-name}-tasks.md)
  → 使用标准模板格式
  → 包含所有元数据、任务清单、依赖图、实施建议

Step 7: 输出摘要
  → 控制台显示任务统计
  → 提示下一步操作（批量生成Issues）
```

### 核心算法：任务拆分策略

```python
def decompose_tasks(design_doc):
    """
    从设计文档生成任务清单
    """
    # Step 1: 解析设计文档
    phases = extract_phases(design_doc)  # 提取Phase
    tech_stack = identify_tech_stack(design_doc)  # Server/Client/Shared
    modules = identify_modules(design_doc)  # MedicalCase/Prescription等

    # Step 2: 如果有明确Phase，直接使用
    if phases:
        tasks = []
        for phase in phases:
            phase_tasks = split_phase_into_tasks(phase, target_hours=2-4)
            tasks.extend(phase_tasks)

    # Step 3: 如果没有Phase，按三层架构拆分
    else:
        tasks = []
        # Phase 1: Repository层
        tasks.extend(create_repository_tasks(modules))

        # Phase 2: Service层 + Controller层
        tasks.extend(create_service_tasks(modules))
        tasks.extend(create_controller_tasks(modules))

        # Phase 3: ViewModel层 + View层（如果是Client端）
        if "Client" in tech_stack:
            tasks.extend(create_viewmodel_tasks(modules))
            tasks.extend(create_view_tasks(modules))

    # Step 4: 分析依赖关系
    for task in tasks:
        task.dependencies = analyze_dependencies(task, tasks)

    # Step 5: 估算工作量
    for task in tasks:
        task.effort = estimate_effort(task)

    return tasks

def split_phase_into_tasks(phase, target_hours):
    """
    将一个Phase拆分成多个任务（目标粒度2-4小时）
    """
    if phase.estimated_hours <= target_hours:
        # 如果Phase本身就是小任务，不再拆分
        return [create_task_from_phase(phase)]

    # 按职责拆分（Repository/Service/Controller）
    responsibilities = identify_responsibilities(phase)
    tasks = []
    for resp in responsibilities:
        task = create_task_from_responsibility(resp)
        tasks.append(task)

    return tasks

def analyze_dependencies(task, all_tasks):
    """
    分析任务的依赖关系
    """
    dependencies = []

    # 规则1: Service依赖Repository
    if task.type == "Service":
        repo_tasks = [t for t in all_tasks if t.type == "Repository" and t.module == task.module]
        dependencies.extend(repo_tasks)

    # 规则2: Controller依赖Service
    if task.type == "Controller":
        service_tasks = [t for t in all_tasks if t.type == "Service" and t.module == task.module]
        dependencies.extend(service_tasks)

    # 规则3: ViewModel依赖API (Controller)
    if task.type == "ViewModel":
        controller_tasks = [t for t in all_tasks if t.type == "Controller" and t.module == task.module]
        dependencies.extend(controller_tasks)

    # 规则4: View依赖ViewModel
    if task.type == "View":
        vm_tasks = [t for t in all_tasks if t.type == "ViewModel" and t.module == task.module]
        dependencies.extend(vm_tasks)

    return dependencies

def estimate_effort(task):
    """
    估算任务工作量
    """
    # 基于文件数量
    file_count = len(task.files)
    if file_count <= 2:
        base_hours = 1.5
    elif file_count <= 4:
        base_hours = 3
    else:
        base_hours = 5

    # 基于复杂度调整
    complexity_factor = {
        "Simple": 0.8,
        "Medium": 1.0,
        "Complex": 1.5
    }

    adjusted_hours = base_hours * complexity_factor[task.complexity]

    # 返回区间估算
    return (adjusted_hours * 0.8, adjusted_hours * 1.2)
```

## 示例

### 示例1：从设计文档生成任务清单

**输入（用户命令）**：
```
"根据设计文档生成任务清单"
或
"分解任务: docs/design/medicalcase-enhancement-design.md"
```

**Skill执行过程**：
```
Step 1: 读取设计文档
✓ 读取: docs/design/medicalcase-enhancement-design.md

Step 2: 解析文档结构
✓ 识别3个Phase
✓ 技术栈: Server + Client
✓ 模块: MedicalCase

Step 3: 任务拆分
✓ Phase 1拆分为4个任务（Repository + Service层）
✓ Phase 2拆分为2个任务（Controller层）
✓ Phase 3拆分为2个任务（ViewModel + View层）

Step 4: 依赖分析
✓ 识别关键路径: Task 1.1 → 1.2 → 2.1 → 3.1
✓ 识别并行任务: Task 1.3 || Task 1.4

Step 5: 工作量估算
✓ 总工作量: 18-24小时
✓ Phase 1: 6-8小时
✓ Phase 2: 4-6小时
✓ Phase 3: 8-10小时

Step 6: 生成task文档
✓ 已生成: docs/tasks/medicalcase-enhancement-tasks.md
```

**输出（控制台摘要）**：
```
✅ 任务分解完成！

📁 Task文档：docs/tasks/medicalcase-enhancement-tasks.md

📊 任务统计：
- 总任务数：8个
- 总工作量：18-24小时
- Phase划分：3个阶段

🔗 关键路径（5个任务）：
1. Task 1.1: 创建MedicalCaseRepository
2. Task 1.2: 实现MedicalCaseService
3. Task 2.1: 实现MedicalCaseController
4. Task 3.1: 创建MedicalCaseViewModel
5. Task 3.2: 实现MedicalCaseView

💡 下一步：
审查task文档后，使用lybtzyzs-issue-template批量生成Issues
```

**生成的Task文档示例**：
```markdown
# 医案增强功能 任务分解文档

## 📋 元数据
- Epic: #1494
- 设计文档: docs/design/medicalcase-enhancement-design.md
- 需求文档: docs/requirements/medicalcase-enhancement-requirements.md
- 总工作量: 18-24小时
- 实施阶段: Phase 1-3

## 🎯 任务清单

### Phase 1: Server端基础层（6-8小时）

#### Task 1.1: 创建MedicalCaseRepository扩展方法
- **工作量**: 1.5-2小时
- **依赖**: 无
- **类型**: Repository
- **文件范围**:
  - `src/Server/Modules/LYBT.Server.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 单元测试通过（Mock DbContext）
  - [ ] 新增查询方法可用
- **技术要点**:
  - 扩展GetOtherCasesAsync方法
  - 使用EF Core LINQ查询

#### Task 1.2: 实现MedicalCaseService业务逻辑
- **工作量**: 2.5-3小时
- **依赖**: Task 1.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Server.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 业务逻辑单元测试通过
  - [ ] 符合Service层设计标准
- **技术要点**:
  - 实现GetOtherCasesAsync业务逻辑
  - 验证业务规则（如患者存在性）

#### Task 1.3: 创建Consultation/Prescription DTO
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1
- **类型**: DTO
- **文件范围**:
  - `src/Server/Modules/LYBT.Server.MedicalCase/DTOs/ConsultationDto.cs`
  - `src/Server/Modules/LYBT.Server.MedicalCase/DTOs/PrescriptionDto.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] DTO字段完整
  - [ ] AutoMapper配置正确
- **技术要点**:
  - 包含关联数据字段
  - 避免循环引用

#### Task 1.4: 配置AutoMapper Profile
- **工作量**: 1-1.5小时
- **依赖**: Task 1.3
- **类型**: Configuration
- **文件范围**:
  - `src/Server/Modules/LYBT.Server.MedicalCase/MappingProfiles/MedicalCaseMappingProfile.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] AutoMapper配置验证通过
  - [ ] 单元测试覆盖映射规则
- **技术要点**:
  - 配置Entity → DTO映射
  - 处理嵌套对象映射

### Phase 2: Server端API层（4-6小时）

#### Task 2.1: 实现MedicalCaseController API端点
- **工作量**: 2-3小时
- **依赖**: Task 1.2, Task 1.4
- **类型**: Controller
- **文件范围**:
  - `src/Server/Modules/LYBT.Server.MedicalCase/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] Swagger文档生成
  - [ ] API集成测试通过
- **技术要点**:
  - GET /api/v1/medicalcases/other-cases
  - 异步处理，异常处理

#### Task 2.2: 编写API集成测试
- **工作量**: 2-3小时
- **依赖**: Task 2.1
- **类型**: Test
- **文件范围**:
  - `tests/IntegrationTests/Server/MedicalCase/MedicalCaseControllerTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 所有测试用例通过
  - [ ] 覆盖成功和失败场景
- **技术要点**:
  - 使用WebApplicationFactory
  - Mock认证

### Phase 3: Client端集成（8-10小时）

#### Task 3.1: 创建MedicalCaseViewModel扩展
- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] MVVM绑定正确
  - [ ] Command可触发
  - [ ] 单元测试通过
- **技术要点**:
  - 实现LoadOtherCasesCommand
  - 调用API Repository
  - 处理异常和加载状态

#### Task 3.2: 更新MedicalCaseView UI
- **工作量**: 5-6小时
- **依赖**: Task 3.1
- **类型**: View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseView.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseView.xaml.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] UI布局正确
  - [ ] 数据绑定生效
  - [ ] 用户交互流畅
- **技术要点**:
  - 添加"其他病案"Tab
  - DataGrid绑定OtherCases集合
  - 响应式布局

## 📊 任务统计
- 总任务数: 8个
- 总工作量: 18-24小时
- Phase数量: 3个
- 关键路径长度: 5个任务

## 🔗 依赖关系图

### Phase 1依赖
```
Task 1.1 (无依赖)
  ├─> Task 1.2
  └─> Task 1.3 → Task 1.4
```

### Phase 2依赖
```
Task 2.1 (依赖Task 1.2, Task 1.4)
  └─> Task 2.2
```

### Phase 3依赖
```
Task 3.1 (依赖Task 2.1)
  └─> Task 3.2
```

### 关键路径
```
Task 1.1 → Task 1.2 → Task 2.1 → Task 3.1 → Task 3.2
```

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：
1. Task 1.1: 创建MedicalCaseRepository扩展
2. Task 1.2: 实现MedicalCaseService业务逻辑
3. Task 2.1: 实现MedicalCaseController API端点
4. Task 3.1: 创建MedicalCaseViewModel扩展
5. Task 3.2: 更新MedicalCaseView UI

**并行任务**（可同时进行）：
- Task 1.3 和 Task 1.2（都依赖Task 1.1，但互不依赖）
- Task 1.4 可以在Task 1.3完成后立即开始

## 📝 实施建议

### 优先级排序
1. 🔴 高优先级：关键路径任务（Task 1.1, 1.2, 2.1, 3.1, 3.2）
2. 🟡 中优先级：DTO和映射配置（Task 1.3, 1.4）
3. 🟢 低优先级：集成测试（Task 2.2）

### 并行策略
- Task 1.2和Task 1.3可以由不同开发者并行完成
- Phase 2的Task 2.2可以在Task 2.1完成后异步进行

### 风险提示
- Task 3.1需要Task 2.1的API可用，建议完整测试API后再开始
- Task 3.2的UI实现可能需要多次迭代，预留缓冲时间

## 🧪 测试策略

### 单元测试
- Task 1.2: MedicalCaseService单元测试（Mock Repository）
- Task 3.1: MedicalCaseViewModel单元测试（Mock API Repository）

### 集成测试
- Task 2.2: API端点集成测试（真实数据库）

### E2E测试
- Phase 3完成后：完整用户流程测试（加载病案 → 查看其他病案 → 导航）
```

### 示例2：从Epic描述生成任务清单

**输入（用户命令）**：
```
"分解Epic #1494：医案流程增强，包括三步工作流和其他病案查看功能"
```

**Skill执行过程**：
```
Step 1: 解析Epic描述
✓ 识别关键功能：三步工作流、其他病案查看
✓ 无设计文档，使用默认三层架构拆分

Step 2: 生成简化任务清单
✓ Phase 1: Repository层（2个任务）
✓ Phase 2: Service + Controller层（3个任务）
✓ Phase 3: ViewModel + View层（3个任务）

Step 3: 提示用户完善设计文档
⚠️ 建议：先完成设计文档以获得更准确的任务拆分
```

**输出**：
```
⚠️ 任务分解建议

检测到：未提供设计文档，基于Epic描述生成初步任务清单。

📁 已生成简化版task文档：docs/tasks/epic-1494-tasks.md
📊 任务数：8个（粗粒度）

💡 建议流程：
1. 先完成设计文档：docs/design/epic-1494-design.md
2. 运行lybtzyzs-design-arch-validator验证架构
3. 重新运行任务分解以获得更准确的拆分

或者：
直接使用当前简化版task文档批量生成Issues
```

## 限制条件

1. **需要设计文档**：最佳实践是基于设计文档拆分，Epic描述只能生成粗粒度任务
2. **依赖设计文档质量**：设计文档越详细（包含Phase、技术方案、文件范围），任务拆分越准确
3. **任务粒度固定**：默认2-4小时/任务，可能需要人工调整
4. **依赖关系基于规则**：自动识别的依赖关系基于标准三层架构，特殊依赖需要人工标注
5. **工作量估算基于经验**：估算可能不准确，建议根据实际情况调整
6. **不执行代码分析**：不分析现有代码结构，仅基于设计文档拆分

## 最佳实践

1. **先完成设计文档**：确保设计文档包含Phase划分、技术方案、文件范围
2. **运行架构验证**：先运行lybtzyzs-design-arch-validator确保设计符合架构标准
3. **审查task文档**：生成后人工审查任务清单，调整不合理的拆分
4. **调整任务粒度**：如果任务过大（>4小时），手动拆分成更小任务
5. **明确依赖关系**：检查依赖关系是否准确，标注特殊依赖
6. **估算工作量**：根据团队实际情况调整工作量估算
7. **批量生成Issues**：审查通过后使用lybtzyzs-issue-template批量生成Issues

## 与其他Skill的协同

### Skill工作流

```
设计阶段：lybtzyzs-design-arch-validator
  ↓ 设计文档架构验证通过
任务分解：lybtzyzs-task-breakdown（本Skill）
  ↓ 生成task文档
Issue创建：lybtzyzs-issue-template（批量模式）
  ↓ 批量生成GitHub Issues
实施阶段：Issue-Driven开发
```

### Skill对比

| Skill | 阶段 | 输入 | 输出 |
|-------|------|------|------|
| **lybtzyzs-design-arch-validator** | 设计 | 设计文档 | 架构验证报告 |
| **lybtzyzs-task-breakdown** | 任务规划 | 设计文档 | Task文档 |
| **lybtzyzs-issue-template** | Issue创建 | Task文档 | GitHub Issues |

## 性能指标

- 设计文档解析：<3秒
- 任务拆分算法：<5秒
- 依赖关系分析：<2秒
- Task文档生成：<2秒
- **端到端完成**：<12秒

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-26 | 初始版本，支持基于Phase和三层架构的任务拆分 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-10-26
