# Tasks: document-project-architecture

## Overview

创建4个架构规范文档，明确项目架构分层和各Project的职责约束。

---

## Phase 1: 创建主架构规范

### Task 1.1: 创建project-architecture规范

**目标**: 创建项目架构总览规范

**文件**: `openspec/specs/project-architecture/spec.md`

**内容要求**:
- ARCH-001: 三层架构定义(Server/Shared/Client)
- ARCH-002: 项目命名规范
- ARCH-003: 依赖方向规范
- ARCH-004: 模块注册规范

**验收标准**:
- [ ] 规范文件创建
- [ ] 包含至少4个Requirements
- [ ] 每个Requirement包含Scenarios

---

## Phase 2: 创建分层规范

### Task 2.1: 创建server-layer-architecture规范

**目标**: Server层详细架构规范

**文件**: `openspec/specs/server-layer-architecture/spec.md`

**内容要求**:
- SRV-001: Core层职责(Entities, Infrastructure)
- SRV-002: Module层职责
- SRV-003: Services层职责(WebAPI)
- SRV-004: CQRS模式规范(MedicalCase)
- SRV-005: 传统三层模式规范

**验收标准**:
- [ ] 规范文件创建
- [ ] 包含至少5个Requirements
- [ ] 明确CQRS vs 传统三层的选择标准

---

### Task 2.2: 创建shared-layer-architecture规范

**目标**: Shared层详细架构规范

**文件**: `openspec/specs/shared-layer-architecture/spec.md`

**内容要求**:
- SHR-001: Models项目职责(DTO、契约)
- SHR-002: Utilities项目职责
- SHR-003: Validators项目职责
- SHR-004: Components项目职责
- SHR-005: DTO继承层次规范

**验收标准**:
- [ ] 规范文件创建
- [ ] 包含至少5个Requirements
- [ ] 明确各项目的边界

---

### Task 2.3: 创建client-layer-architecture规范

**目标**: Client(Desktop)层详细架构规范

**文件**: `openspec/specs/client-layer-architecture/spec.md`

**内容要求**:
- CLI-001: Core层职责(5个项目)
- CLI-002: Modules层职责(8个项目)
- CLI-003: Roles层职责(2个项目)
- CLI-004: Shell层职责
- CLI-005: ViewModel基类规范
- CLI-006: 模块注册规范

**验收标准**:
- [ ] 规范文件创建
- [ ] 包含至少6个Requirements
- [ ] 与现有viewmodel-conventions规范交叉引用

---

## Phase 3: 更新项目文档

### Task 3.1: 更新project.md

**目标**: 在project.md中添加架构规范引用

**文件**: `openspec/project.md`

**变更内容**:
- 添加References部分引用4个新规范
- 更新Architecture Patterns部分指向规范

**验收标准**:
- [ ] References部分包含4个规范链接
- [ ] 架构描述与规范一致

---

## Phase 4: 验证

### Task 4.1: 验证所有规范

**命令**: `npx openspec validate --strict`

**验收标准**:
- [ ] 所有规范文件语法正确
- [ ] 无缺失的必要字段
- [ ] 交叉引用有效

---

## Summary

| Phase | 任务数 | 产出文件 |
|-------|--------|----------|
| Phase 1 | 1 | project-architecture/spec.md |
| Phase 2 | 3 | server/shared/client-layer-architecture/spec.md |
| Phase 3 | 1 | project.md (修改) |
| Phase 4 | 1 | 验证通过 |

**总计**: 5个任务，4个新规范文件，1个修改文件
