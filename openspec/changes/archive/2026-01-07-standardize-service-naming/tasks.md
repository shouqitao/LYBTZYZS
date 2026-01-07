# Tasks: standardize-service-naming

**Change ID**: standardize-service-naming
**Total Tasks**: 4
**Completed**: 0/4

---

## Task List

### Task 1: Create Naming Convention Document
- **ID**: SSN-001
- **Status**: pending
- **Priority**: P0
- **Effort**: 1h

**Description**: 创建Desktop层命名规范文档

**Target File**: `docs/architecture/desktop-naming-conventions.md`

**Content Outline**:
1. Service Classes (Service/Handler/Manager/Coordinator)
2. Repository Pattern
3. ViewModel Naming
4. Interface Naming (I{Name})
5. File Naming (一文件一类)

**Acceptance Criteria**:
- [ ] 文档内容完整
- [ ] 包含示例代码
- [ ] 规范清晰可执行

---

### Task 2: Review Existing Code
- **ID**: SSN-002
- **Status**: pending
- **Priority**: P1
- **Effort**: 1h
- **Depends On**: SSN-001

**Description**: 审查现有代码命名情况

**Focus Areas**:
- Foundation层服务
- Infrastructure层服务
- 业务模块服务

**Deliverable**:
- 命名合规清单
- 命名违例清单（如有）
- 改进建议

---

### Task 3: Update Project Documentation
- **ID**: SSN-003
- **Status**: pending
- **Priority**: P1
- **Effort**: 30min
- **Depends On**: SSN-001

**Description**: 更新项目文档引用命名规范

**Files to Update**:
- `CLAUDE.md` - 添加命名规范引用
- `docs/README.md` - 添加文档索引（如存在）

**Acceptance Criteria**:
- [ ] CLAUDE.md包含规范链接
- [ ] 新开发者可以找到规范

---

### Task 4: Create PR Review Checklist
- **ID**: SSN-004
- **Status**: pending
- **Priority**: P2
- **Effort**: 30min
- **Depends On**: SSN-001

**Description**: 创建PR Review检查清单

**Target File**: `.github/PULL_REQUEST_TEMPLATE.md` 或 `docs/pr-checklist.md`

**Checklist Items**:
- [ ] 服务类命名符合规范
- [ ] 接口定义在正确位置
- [ ] Converter在Converters目录
- [ ] 一文件一类

---

## Progress Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Document | SSN-001 | pending |
| Review | SSN-002 | pending |
| Update | SSN-003 | pending |
| Checklist | SSN-004 | pending |
