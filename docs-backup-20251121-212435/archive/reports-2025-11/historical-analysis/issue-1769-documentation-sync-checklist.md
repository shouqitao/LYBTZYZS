# Issue #1769 文档同步检查清单

**生成时间**: 2025-11-02
**关联决策**: [ADR-008: Desktop端Consultation/Prescription不独立实现Repository](../explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)
**代码变更**: Commit 10a962ac

---

## 📊 变更摘要

### 代码变更
- **删除文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Interfaces/IConsultationRepository.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Interfaces/IConsultationApiClient.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Interfaces/IPrescriptionRepository.cs`

- **新增文件**:
  - `docs/explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md`

### 架构决策
- **决策**: Desktop端Consultation/Prescription不独立实现Repository
- **原则**: 子实体通过聚合根访问（DDD标准）
- **影响范围**: Desktop端Repository模式

---

## 📝 文档更新清单

### 🔴 高优先级（核心架构文档）

#### 1. Desktop端架构文档
- [ ] `docs/explanation/architecture/client/README.md` (lines 1388, 1515, 1527)
  - **问题**: 仍引用已删除的`IConsultationRepository`
  - **需要**: 移除对已删除接口的引用，强调聚合根模式

- [ ] `docs/explanation/architecture/client/consultation-design.md`
  - **需要**: 更新为聚合根访问模式，移除独立Repository设计

- [ ] `docs/explanation/architecture/client/prescriptions-design.md`
  - **需要**: 确认已正确标注Obsolete状态并引用ADR-008

- [ ] `docs/explanation/architecture/client/presentation-design.md`
  - **需要**: 验证引用准确性

#### 2. ADR交叉引用
- [x] `docs/explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md` - 已创建 ✅
- [ ] `docs/explanation/architecture/decisions/ADR-003-repository-simplification.md`
  - **需要**: 添加ADR-008的后续决策引用
- [ ] `docs/explanation/architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md`
  - **需要**: 添加ADR-008实践案例引用
- [ ] `docs/explanation/architecture/decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md`
  - **需要**: 添加ADR-008补充说明引用
- [ ] `docs/explanation/architecture/decisions/ADR-007-repository-service-simplification.md`
  - **需要**: 添加ADR-008 Desktop端实践引用

### 🟡 中优先级（设计与开发指南）

#### 3. Repository模式文档
- [ ] `docs/explanation/architecture/patterns/repository-pattern.md`
  - **需要**: 添加Desktop端不实现子实体Repository的说明

#### 4. 开发指南
- [ ] `docs/how-to-guides/client/consultation-development.md`
  - **需要**: 更新为聚合根访问模式
- [ ] `docs/how-to-guides/client/prescriptions-development.md`
  - **需要**: 确认已正确引导开发者使用IMedicalCaseRepository
- [ ] `docs/how-to-guides/client/medical-case-development.md`
  - **需要**: 验证聚合根使用示例

#### 5. Server端文档（仅确认说明清晰）
- [ ] `docs/explanation/architecture/server/consultation-design.md`
  - **需要**: 确认Server端Read-only Repository说明与Desktop端区分清晰
- [ ] `docs/explanation/architecture/server/prescriptions-design.md`
  - **需要**: 确认Server端Read-only Repository说明与Desktop端区分清晰
- [ ] `docs/explanation/architecture/server/medical-case-design.md`
  - **需要**: 验证聚合根模式说明

### 🟢 低优先级（辅助文档）

#### 6. 设计与需求文档
- [ ] `docs/explanation/design/medicalcase-consultation-prescription-architecture-refactoring-plan.md`
- [ ] `docs/explanation/design/medicalcase-consultation-prescription-enhancement-design.md`
- [ ] `docs/explanation/design/medicalcase-consultation-prescription-gap-analysis.md`
- [ ] `docs/explanation/design/server-refactor-design.md`
- [ ] `docs/explanation/requirements/medicalcase-consultation-prescription-refactoring-requirements.md`
- [ ] `docs/explanation/requirements/server-refactor-requirements.md`

#### 7. 演进与异常处理文档
- [ ] `docs/explanation/architecture/evolution.md`
- [ ] `docs/explanation/architecture/exceptions.md`

#### 8. 参考文档
- [ ] `docs/reference/modules/consultation/README.md`
- [ ] `docs/reference/modules/prescriptions/README.md`

#### 9. 共享组件文档
- [ ] `docs/how-to-guides/shared/components-usage.md`
- [ ] `docs/how-to-guides/shared/prescription-auto-numbering-implementation.md`
- [ ] `docs/how-to-guides/shared/README.md`

#### 10. 深度模式文档
- [ ] `docs/deep/advanced-patterns.md`

#### 11. 任务与设计文档
- [ ] `docs/design/phase2-repository-simplification-design.md`
- [ ] `docs/tasks/server-refactor-tasks.md`

---

## ✅ 验证检查

### 链接有效性
- [x] ADR-008内部引用链接有效 ✅
  - [x] ADR-005存在 ✅
  - [x] ADR-006存在 ✅
  - [x] ADR-007存在 ✅

### 代码编译
- [x] 编译通过（0 errors, 0 warnings）✅

### 架构一致性
- [x] 删除操作符合DDD聚合根模式 ✅
- [x] Server端Read-only Repository保留（符合CQRS-Lite）✅
- [x] Desktop端移除子实体Repository（符合聚合边界）✅

---

## 📌 更新原则

### 核心原则
1. **明确Desktop/Server Repository职责区分**
   - Desktop Repository = HTTP客户端（Refit API wrapper）
   - Server Repository = 数据库访问（EF Core）

2. **强调聚合根模式**
   - Desktop端: 子实体通过IMedicalCaseRepository访问
   - Server端: 子实体可有Read-only Repository（性能优化）

3. **YAGNI原则说明**
   - 不预留未来可能不需要的接口
   - 需要时渐进式演进（ADR-008已定义触发条件）

### 更新优先级
- **高优先级**: 开发者直接参考的架构文档和开发指南
- **中优先级**: 设计决策和模式说明文档
- **低优先级**: 历史设计文档和辅助参考文档

### 更新方式
- **移除引用**: 删除对已删除接口的代码示例
- **添加说明**: 补充ADR-008决策背景和原因
- **更新示例**: 使用聚合根模式的正确代码示例

---

## 🎯 预计工作量

| 优先级 | 文档数量 | 预计时间 |
|--------|---------|---------|
| 🔴 高优先级 | 9个 | 2-3小时 |
| 🟡 中优先级 | 7个 | 1-2小时 |
| 🟢 低优先级 | 17个 | 2-3小时 |
| **总计** | **33个** | **5-8小时** |

---

## 📎 相关资源

- **ADR-008**: [Desktop端Consultation/Prescription不独立实现Repository](../explanation/architecture/decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)
- **Issue #1769**: 全局接口设计与实现合理性审查
- **Issue #1768**: 全局[Obsolete]代码清理
- **Issue #1606**: MedicalCase聚合根重构
- **Commit**: 10a962ac2bd5fc47fd6a4a31d4e30c92f2a9e34b
