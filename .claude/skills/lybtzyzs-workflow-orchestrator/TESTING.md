# Workflow Orchestrator 测试文档

## 测试目的

验证完整自动化流程（从用户需求到PR合并）的正确性和Skills协同能力。

---

## 测试场景：病案草稿功能

### 输入
```
用户: 开始新需求：实现病案草稿功能，允许医生保存未完成的病案信息
```

---

## 预期执行流程

### 阶段1: 需求讨论（RequirementsDiscussion）

**自动执行**：
1. ✅ 创建Spec名称：`medicalcase-draft`
2. ✅ 调用 `lybtzyzs-requirements-generator`
   - 调用 `sequential-thinking` 深度分析需求（5轮推理）
   - 调用 `lybtzyzs-doc-sync` 检索现有文档
   - 调用 `context7` 查询技术方案（EF Core本地存储）
   - 调用 `lybtzyzs-mvp-compliance` 检查技术栈
3. ✅ 生成需求文档：`docs/explanation/architecture/server/medicalcase-draft-discussion.md`

**预期输出**：
```markdown
## 需求讨论文档已生成

**文件**: docs/explanation/architecture/server/medicalcase-draft-discussion.md

**包含内容**:
- 需求概述（业务目标：支持草稿保存）
- 功能性需求（6个User Story）
  - FR-001: 创建草稿
  - FR-002: 编辑草稿
  - FR-003: 查看草稿列表
  - FR-004: 删除草稿
  - FR-005: 草稿转正式病案
  - FR-006: 草稿自动保存
- 非功能性需求（性能、安全、可用性）
- 业务规则（4条）
  - BR-001: 草稿仅创建者可见
  - BR-002: 草稿可无限次修改
  - BR-003: 草稿转正式需验证
  - BR-004: 草稿可物理删除
- 数据模型草案（MedicalCaseDraft Entity）
- 架构约束（Server端三层架构 + Client端MVVM）
- 开放问题（3个）

**下一步**: 请审查需求文档，确认后进入设计阶段
```

**状态转换**: RequirementsDiscussion → RequirementsApproval

**状态文件**:
```json
{
  "workflowId": "wf-medicalcase-draft-20251107",
  "currentState": "RequirementsApproval",
  "artifacts": {
    "requirementsDoc": "docs/.../medicalcase-draft-discussion.md"
  }
}
```

---

### 阶段2: 需求确认（RequirementsApproval）🔴

**AskUserQuestion交互**：
```
Claude: 需求讨论文档已生成，请确认:

1. 需求理解是否准确？
   - 背景: 支持病案草稿功能
   - 目标: 医生可保存未完成的病案，稍后继续编辑

2. 业务规则是否完整？
   - 规则1: 草稿仅创建者可见
   - 规则2: 草稿可无限次修改
   - 规则3: 草稿可转为正式病案
   - 规则4: 草稿可物理删除

3. 是否有遗漏的需求？

请选择:
A. 确认需求，进入设计阶段
B. 需要修改（请说明）
C. 查看完整需求文档
```

**用户选择**: A

**状态转换**: RequirementsApproval → DesignGeneration

---

### 阶段3: 设计生成（DesignGeneration）

**自动执行**：
1. ✅ 调用 `lybtzyzs-design-generator` 生成设计文档
2. ✅ 调用 `lybtzyzs-design-arch-validator` 验证架构
3. ✅ 调用 `lybtzyzs-mvp-compliance` 检查MVP约束
4. ✅ 生成设计文档：`docs/explanation/medicalcase-draft-design.md`

**预期输出**：
```markdown
## 设计文档已生成

**文件**: docs/explanation/medicalcase-draft-design.md

**包含内容**:
- 架构设计
  - Server端: MedicalCaseDraft Entity + Repository + Service + Controller
  - Client端: DraftViewModel + DraftModel + DraftView
  - 存储: SQL Server（新增MedicalCaseDrafts表）
- API设计（5个端点）
  - GET /api/v1/medicalcase-drafts
  - POST /api/v1/medicalcase-drafts
  - PUT /api/v1/medicalcase-drafts/{id}
  - DELETE /api/v1/medicalcase-drafts/{id}
  - POST /api/v1/medicalcase-drafts/{id}/publish
- 数据模型（完整Entity定义）
- Phase拆分（3个Phase，预计14-16小时）
  - Phase 1: Server端Entity+Repository（6h）
  - Phase 2: Server端Service+Controller（4h）
  - Phase 3: Client端ViewModel+View（6h）

**架构验证**: ✅ 通过（符合三层架构）
**MVP合规**: ✅ 通过（未使用禁用技术）

**下一步**: 请审查设计文档，确认后进入任务分解
```

**状态转换**: DesignGeneration → DesignApproval

---

### 阶段4: 设计确认（DesignApproval）🔴

**AskUserQuestion交互**：
```
Claude: 设计文档已生成，请确认:

## 架构设计
- Server端: Entity + Repository + Service + Controller
- Client端: ViewModel + View
- 存储: SQL Server（新增MedicalCaseDrafts表）

## 关键技术决策
1. 数据库: SQL Server（新增表）
2. 自动保存: Client端30秒定时器
3. 权限控制: Repository层按UserId过滤

## Phase拆分
- Phase 1: Server端Entity+Repository（6h）
- Phase 2: Server端Service+Controller（4h）
- Phase 3: Client端ViewModel+View（6h）
- 总计: 16小时（包含20% buffer）

## 风险提示
✅ 技术栈全部符合MVP约束
⚠️ 新增数据库表（需迁移脚本）

请选择:
A. 确认设计，进入任务分解
B. 需要调整（请说明）
C. 查看完整设计文档
```

**用户选择**: A

**状态转换**: DesignApproval → TaskBreakdown

---

### 阶段5: 任务分解（TaskBreakdown）

**自动执行**：
1. ✅ 调用 `lybtzyzs-task-breakdown` 生成任务清单
2. ✅ 生成task文档：`docs/tasks/medicalcase-draft-tasks.md`

**预期输出**：
```markdown
## 任务分解完成

**文件**: docs/tasks/medicalcase-draft-tasks.md

**拆分结果**:
- 总任务数: 8个
- 总工作量: 16小时
- Phase数: 3个
- 依赖关系:
  - Task 2 → Task 3
  - Task 4 → Task 5
  - Task 6 → Task 7

**任务清单**:
1. [Phase 1] 创建MedicalCaseDraft Entity（2h）
2. [Phase 1] 创建数据库迁移脚本（1h）
3. [Phase 1] 实现MedicalCaseDraftRepository（3h，依赖Task 2）
4. [Phase 2] 实现MedicalCaseDraftService（2h）
5. [Phase 2] 实现API Controller（2h，依赖Task 4）
6. [Phase 3] 实现DraftViewModel（3h）
7. [Phase 3] 实现DraftView XAML（2h，依赖Task 6）
8. [Phase 3] 单元测试和集成测试（1h）

**下一步**: 批量创建GitHub Issues
```

**状态转换**: TaskBreakdown → TaskApproval（auto模式自动跳过）

---

### 阶段6: Task确认（TaskApproval）🟡 自动通过

**配置**: `"taskApproval": "auto"`

**自动行为**: 跳过确认，直接进入IssueCreation

**状态转换**: TaskApproval → IssueCreation

---

### 阶段7: Issue创建（IssueCreation）

**自动执行**：
1. ✅ 调用 `lybtzyzs-issue-template` 批量创建Issues
2. ✅ 自动关联Epic #1500
3. ✅ 标注依赖关系
4. ✅ 调用 `lybtzyzs-task-tracker` 初始化追踪

**预期输出**：
```markdown
## Issues创建完成

**Epic**: #1500（病案草稿功能）

**创建的Issues**:
- #1501: [Phase 1] 创建MedicalCaseDraft Entity
- #1502: [Phase 1] 创建数据库迁移脚本
- #1503: [Phase 1] 实现MedicalCaseDraftRepository（依赖#1502）
- #1504: [Phase 2] 实现MedicalCaseDraftService
- #1505: [Phase 2] 实现API Controller（依赖#1504）
- #1506: [Phase 3] 实现DraftViewModel
- #1507: [Phase 3] 实现DraftView XAML（依赖#1506）
- #1508: [Phase 3] 单元测试和集成测试

**依赖关系已标注**: ✅
**任务追踪已初始化**: ✅

**下一步**: 开始执行Issue #1501
```

**状态转换**: IssueCreation → CodeImplementation

---

### 阶段8: 代码实现（CodeImplementation）循环

**执行Issue #1501**:
```markdown
## 执行Issue #1501

1. ✅ 调用 `lybtzyzs-context-builder` 聚合上下文
   - 需求文档: medicalcase-draft-discussion.md
   - 设计文档: medicalcase-draft-design.md
   - 相关代码: MedicalCase.cs, BaseEntity.cs
   - Constitution: MVP约束

2. ✅ 调用 `lybtzyzs-task-executor` 生成代码
   - 创建文件: src/Server/Domain/Entities/MedicalCaseDraft.cs（80行）
   - 内容: Entity定义、导航属性

3. ✅ 自动验证
   - 编译: ✅ 通过
   - 测试: ✅ 通过（0个新测试，无需测试Entity）
   - MVP合规: ✅ 通过
   - 架构合规: ✅ 通过

4. ✅ 提交代码
   - Commit: feat(medicalcase): Issue #1501 创建MedicalCaseDraft Entity

5. ✅ 调用 `lybtzyzs-task-tracker` 更新状态
   - Issue #1501: Open → Completed
   - Epic #1500进度: 1/8（12.5%）

**下一步**: 执行Issue #1502
```

**重复8次直到所有Issue完成**

**状态转换**: CodeImplementation → PRCreation

---

### 阶段9: PR创建（PRCreation）

**自动执行**：
1. ✅ 调用 `lybtzyzs-pr-generator` 生成PR
2. ✅ 关联所有Issues（#1501-#1508）
3. ✅ 调用 `lybtzyzs-quality-reporter` 生成质量报告
4. ✅ 创建PR到GitHub

**预期输出**：
```markdown
## PR已创建

**PR**: #150（病案草稿功能 - Epic #1500）

**包含Commits**: 8个
**关联Issues**: #1501-#1508（全部已完成）

**质量报告**:
- ✅ 编译通过（0 errors, 0 warnings）
- ✅ 测试通过（24/24 - 8个新测试 + 16个回归测试）
- ✅ MVP合规（无违规项）
- ✅ 架构合规（依赖方向正确）
- ✅ 代码覆盖率: 92%

**技术债务**:
- TD-001: Repository层缺少分页支持（低优先级）
- TD-002: 自动保存定时器未实现取消令牌（中优先级）

**质量评分**: 88/100

**下一步**: 质量门禁检查
```

**状态转换**: PRCreation → QualityGate

---

### 阶段10: 质量门禁（QualityGate）🔴

**自动执行**：
1. ✅ 检查自动合并条件
2. ✅ 生成质量报告摘要

**AskUserQuestion交互**：
```
Claude: PR已创建，质量检查结果:

## 自动合并条件检查
✅ 测试通过: 24/24
✅ MVP合规: 通过
✅ 架构合规: 通过
✅ 质量评分: 88（≥85）
⚠️ 技术债务: 2个（≤3）✅
✅ 关键债务: 0个

**结论**: 满足自动合并条件 ✅

## 技术债务详情
1. TD-001（低优先级）
   - 问题: Repository层缺少分页支持
   - 影响: 未来数据量大时性能问题
   - 预计修复: 30分钟

2. TD-002（中优先级）
   - 问题: 自动保存定时器未实现取消令牌
   - 影响: 内存泄漏风险
   - 预计修复: 15分钟

## 决策选项
A. 批准合并（技术债务已记录，稍后处理）
B. 查看PR详情
C. 立即修复技术债务（预计45分钟）
D. 拒绝合并（说明原因）
```

**用户选择**: A

**状态转换**: QualityGate → Merge

---

### 阶段11: 合并代码（Merge）

**自动执行**：
1. ✅ 调用GitHub API合并PR #150
2. ✅ 关闭所有Issues（#1501-#1508）
3. ✅ 调用 `lybtzyzs-task-tracker` 更新Epic进度
4. ✅ 清理临时分支

**预期输出**：
```markdown
## PR已合并

**PR**: #150 → master
**关闭Issues**: #1501-#1508（8个）
**Epic进度**: #1500（100% - 所有子Issues完成）

**下一步**: 生成Epic反思报告
```

**状态转换**: Merge → Reflection

---

### 阶段12: 反思总结（Reflection）

**自动执行**：
1. ✅ 调用 `lybtzyzs-task-reflector` 生成反思报告
2. ✅ 分析执行情况
3. ✅ 识别技术债务
4. ✅ 提取学习要点
5. ✅ 生成候选ADR

**预期输出**：
```markdown
## Epic反思报告已生成

**Epic**: #1500（病案草稿功能）

**执行总结**:
- 完成Issues: 8个
- 总耗时: 15.5小时（预估16h，偏差-3%）
- 质量评分: 88分
- 技术债务: 2个（已记录到backlog）

**做得好的地方**:
1. Phase拆分合理（依赖清晰，无阻塞）
2. Entity设计符合三层架构
3. 测试覆盖率高（92%）

**可改进的地方**:
1. Repository缺少分页支持（需后续补充）
2. 自动保存定时器资源管理（内存泄漏风险）
3. 工作量估算略高（-3%偏差可接受）

**学习要点**:
1. Entity设计最佳实践（已归档memory）
2. 草稿状态管理模式（已归档memory）

**候选ADR**:
1. ADR-012: 草稿数据管理方案

**下一步**: 审查反思报告（可选）
```

**状态转换**: Reflection → ReflectionReview（auto模式自动跳过）

---

### 阶段13: 反思审查（ReflectionReview）🟡 自动通过

**配置**: `"reflectionReviewApproval": "auto"`

**自动行为**: 跳过审查，直接归档

**状态转换**: ReflectionReview → Archive

---

### 阶段14: 归档知识（Archive）

**自动执行**：
1. ✅ 归档反思报告到 `docs/retrospectives/`
2. ✅ 归档学习要点到memory
3. ✅ 归档候选ADR到 `docs/adr/candidates/`
4. ✅ 清理workflow状态文件

**预期输出**：
```markdown
## Epic已完成并归档

**Epic**: #1500（病案草稿功能）

**归档内容**:
- 反思报告: docs/retrospectives/epic-1500-reflection.md
- Memory更新: 2个（Entity设计、草稿状态管理）
- 候选ADR: docs/adr/candidates/ADR-012-draft-management.md

**完整流程耗时**: 约2小时（从需求到归档）
**自动化率**: 85%（仅3次人工确认）

**工作流程结束** ✅
```

**状态转换**: Archive → 完成

**清理状态文件**: 删除 `workflow-state.json`

---

## 测试总结

### Skills调用统计

| Skill | 调用次数 | 阶段 |
|-------|---------|------|
| requirements-generator | 1 | 需求讨论 |
| design-generator | 1 | 设计生成 |
| design-arch-validator | 1 | 设计生成 |
| mvp-compliance | 3 | 需求、设计、代码验证 |
| task-breakdown | 1 | 任务分解 |
| issue-template | 1 | Issue创建 |
| task-tracker | 10 | 初始化+8次更新+进度查询 |
| context-builder | 8 | 每个Issue执行前 |
| task-executor | 8 | 8个Issues |
| pr-generator | 1 | PR创建 |
| quality-reporter | 1 | 质量门禁 |
| task-reflector | 1 | 反思总结 |
| **总计** | **38次** | - |

### 人工确认统计

| 确认点 | 类型 | 配置 | 是否触发 |
|-------|------|------|---------|
| RequirementsApproval | 🔴 强制 | required | ✅ 是 |
| DesignApproval | 🔴 强制 | required | ✅ 是 |
| TaskApproval | 🟡 可选 | auto | ❌ 否（自动通过）|
| QualityGate | 🔴 强制 | required | ✅ 是 |
| ReflectionReview | 🟡 可选 | auto | ❌ 否（自动通过）|
| **总计** | - | - | **3次确认** |

### 自动化率计算

```
总步骤数: 14个阶段
人工确认: 3次
自动化步骤: 11个

自动化率 = 11 / 14 = 78.6%

实际自动化率（包含Skills内部自动化）:
- 需求分析内部5轮推理（自动）
- 8个Issue自动执行（自动）
- 质量验证自动化（自动）

综合自动化率 ≈ 85%
```

---

## 预期问题与解决

### 问题1: requirements-generator生成的文档不符合预期

**症状**: 业务规则遗漏或理解偏差

**解决**:
1. RequirementsApproval阶段选择"B. 需要修改"
2. Orchestrator回退到RequirementsDiscussion
3. requirements-generator根据反馈重新生成

### 问题2: 某个Issue执行失败

**症状**: 编译错误或测试失败

**解决**:
1. task-executor自动重试（最多2次）
2. 简单错误（缺using）自动修复
3. 复杂错误提示用户，等待确认

### 问题3: 质量门禁不通过

**症状**: 质量评分<85或关键债务>0

**解决**:
1. QualityGate阶段选择"C. 立即修复技术债务"
2. Orchestrator回退到CodeImplementation
3. 修复后重新创建PR

---

## 实际运行指令

```
用户: 开始新需求：实现病案草稿功能，允许医生保存未完成的病案信息

→ Orchestrator自动启动
→ 依次执行14个阶段
→ 仅需3次人工确认（需求、设计、质量）
→ 约2小时完成整个Epic
```

---

**测试日期**: 2025-11-07
**测试状态**: ✅ 流程设计完整，待实际执行验证
