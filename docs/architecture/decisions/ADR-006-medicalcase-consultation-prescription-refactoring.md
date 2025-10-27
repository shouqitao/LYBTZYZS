# ADR-006: MedicalCase/Consultation/Prescription架构重构

**日期**: 2025-10-27
**状态**: Accepted
**决策者**: 项目团队
**标签**: #架构 #重构 #聚合根 #DDD

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-006 |
| **创建日期** | 2025-10-27 |
| **最后更新** | 2025-10-27 |
| **状态** | Accepted |
| **决策者** | 项目团队 |
| **影响范围** | Server/Client/Shared |
| **相关Issue** | Epic #1612 |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

MedicalCase、Consultation、Prescription三个实体之间存在架构违规问题：

**ARCH-001违规**：9个API端点绕过MedicalCase聚合根，允许直接操作Consultation和Prescription：
- `POST /consultations/{id}/complete` - 绕过聚合根完成辨证
- `DELETE /consultations/{id}` - 直接删除诊疗记录
- `POST /prescriptions` - 绕过聚合根创建处方
- `PUT /prescriptions/{id}` - 绕过聚合根更新处方
- `DELETE /prescriptions/{id}` - 直接删除处方

### 当前状态

**设计问题**：
- MedicalCase作为聚合根，但Consultation/Prescription可独立操作
- 违反DDD聚合根边界原则
- 业务逻辑分散，难以维护

**UI问题**：
- 辨证和处方录入流程固定（必须先诊疗再开处方）
- 无法支持"不开处方"的场景
- 暂存和继续看诊功能缺失

### 问题影响

**业务逻辑风险**：
- 数据一致性问题：MedicalCase与Consultation/Prescription状态可能不一致
- 业务规则绕过：直接操作子实体可能绕过聚合根验证

**用户体验问题**：
- 流程不灵活，无法适应不同诊疗场景
- 无法中途暂存，必须一次性完成

**维护成本**：
- 架构违规导致代码复杂度高
- API端点冗余，文档维护困难

---

## ✅ 决策（Decision）

### 核心决策

**1. 强制聚合根模式**：所有Consultation/Prescription的Write操作必须通过MedicalCase聚合根

**重构前**：
```csharp
// ❌ 违规：绕过聚合根
POST /api/v1/consultations
PUT /api/v1/consultations/{id}
DELETE /api/v1/consultations/{id}

POST /api/v1/prescriptions
PUT /api/v1/prescriptions/{id}
DELETE /api/v1/prescriptions/{id}
```

**重构后**：
```csharp
// ✅ 通过聚合根
PUT /api/v1/medicalcases/{medicalCaseId}/consultation
DELETE /api/v1/medicalcases/{medicalCaseId}  // 级联删除

POST /api/v1/medicalcases/{medicalCaseId}/prescription
PUT /api/v1/medicalcases/{medicalCaseId}/prescription
DELETE /api/v1/medicalcases/{medicalCaseId}/prescription
```

**2. 动态流程设计**：引入RadioBox决策点，支持"是否开处方"选择

**UI实现**：
- 辨证步骤独立完成
- RadioBox选择"是/否"决定是否开处方
- 处方面板动态显示/隐藏
- 支持暂存和继续看诊

**3. 三步工作流辅助方法**：

```csharp
// 辅助方法（保留用于向导式UI，可选）
POST /api/v1/medicalcases/{medicalCaseId}/complete-step1  // 完成辨证
POST /api/v1/medicalcases/{medicalCaseId}/complete-step2  // 开处方（可选）
PUT  /api/v1/medicalcases/{medicalCaseId}/reset-consultation-steps  // 重置诊疗步骤
```

**4. 暂存和继续看诊**：

```csharp
// 暂存功能
PUT /api/v1/medicalcases/{medicalCaseId}/save-as-draft

// 继续看诊（通过LoadAsync恢复所有数据）
GET /api/v1/medicalcases/{id}/with-details
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **架构合规**：强制聚合根模式，消除ARCH-001违规
- ✅ **数据一致性**：所有Write操作通过聚合根，保证业务规则执行
- ✅ **流程灵活**：支持动态决策（是否开处方）
- ✅ **用户体验提升**：支持暂存和继续看诊
- ✅ **代码简化**：删除9个违规端点，API更清晰
- ✅ **维护性提升**：业务逻辑集中在Service层

### 缺点（Cons）

- ❌ **API路径变更**：需要更新所有Client端调用
- ❌ **学习成本**：开发者需要理解聚合根模式
- ❌ **UI复杂度**：RadioBox + 动态面板增加了UI逻辑

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Client端迁移遗漏 | 部分功能失效 | Grep搜索验证，编译通过 + 运行时测试 |
| Server端API未完成 | Client端无法调用 | Phase 2优先完成API实现 |
| RadioBox状态管理复杂 | UI Bug风险 | 防抖 + 错误回滚机制 |
| 暂存数据不完整 | 继续看诊失败 | LoadAsync恢复所有字段（辨证 + 处方 + RadioBox） |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: 保留独立Controller，仅添加验证

**描述**: 保留ConsultationController和PrescriptionController，在每个方法中添加聚合根验证

**优点**:
- ✅ API路径不变，无需Client端迁移
- ✅ 改动范围小

**缺点**:
- ❌ 架构违规仍然存在（API设计层面）
- ❌ 业务逻辑分散，维护困难
- ❌ 无法从根本上保证聚合根模式

**为什么未采纳**: 治标不治本，无法从架构层面保证聚合根模式

---

### 方案B: 完全删除Consultation和Prescription实体

**描述**: 将Consultation和Prescription的字段直接合并到MedicalCase实体

**优点**:
- ✅ 聚合根模式最简化
- ✅ 不存在跨实体一致性问题

**缺点**:
- ❌ MedicalCase实体过于庞大（>50个字段）
- ❌ 违反单一职责原则
- ❌ 难以支持一对多关系（如多次诊疗、多个处方）

**为什么未采纳**: 过度简化，不符合业务建模原则

---

## 🏗️ 架构例外（Architecture Exceptions）

### 例外：辅助方法保留（complete-step1, complete-step2）

- **违反规则**: RESTful设计原则（资源导向）
- **影响模块**: `MedicalCaseController`
- **批准理由**:
  - 向导式UI需要明确的步骤划分
  - 辅助方法仅是聚合根方法的语法糖
  - 底层仍调用标准的UpdateConsultation/CreatePrescription
- **补救措施**:
  - 在API文档中明确标注为"辅助方法"
  - 未来可根据使用情况决定是否保留

---

## 📚 参考资料（References）

- **相关Epic**: #1612
- **相关Issue**: #1658-#1666 (Phase 3任务)
- **设计文档**: `docs/design/medicalcase-consultation-prescription-refactoring-design.md`
- **需求文档**: `docs/requirements/medicalcase-consultation-prescription-refactoring-requirements.md`
- **架构文档**: `docs/architecture/server/README.md`
- **DDD参考**: Eric Evans《Domain-Driven Design》

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 架构分析和设计（已完成 ✅）
- [x] 分析ARCH-001违规项
- [x] 设计聚合根模式方案
- [x] 设计动态流程和RadioBox决策点
- [x] 编写需求文档和设计文档

### Phase 2: Server端重构（进行中 ⏳）
- [x] 实现MedicalCaseService聚合根方法
- [ ] 实现MedicalCaseController新端点
- [ ] 标记废弃端点为Obsolete
- [ ] Server端单元测试和集成测试

### Phase 3: Client端重构（已完成 ✅）
- [x] 创建MedicalCaseConsultationViewModel
- [x] 创建MedicalCaseConsultationView.xaml（含RadioBox）
- [x] 实现LoadAsync（继续看诊）
- [x] 实现SaveDraft（暂存病案）
- [x] 实现RadioBox自动保存逻辑

### Phase 4: 验证和文档（进行中 ⏳）
- [x] 物理删除废弃端点（验证已在Issue #1600完成）
- [ ] 端到端功能测试（4个核心场景）
- [x] 更新ADR文档（本文档）
- [ ] 更新API文档
- [ ] 创建用户手册

---

## ✅ 验收标准（Acceptance Criteria）

**Phase 2验收**：
- [ ] Server端编译通过（0 errors, 0 warnings）
- [ ] Service层业务规则测试通过（覆盖率≥80%）
- [ ] API端点测试通过（Postman/Swagger）
- [ ] 通过lybtzyzs-arch-compliance检查（0违规）

**Phase 3验收**：
- [x] Client端编译通过（0 errors, 0 warnings）
- [x] ViewModel实现完整（辨证字段、RadioBox、命令）
- [x] View.xaml实现完整（10个辨证字段、RadioBox、动态面板）
- [x] LoadAsync恢复所有数据（辨证、处方、RadioBox状态）
- [x] SaveDraft保存完整数据

**Phase 4验收**：
- [ ] 端到端测试4个场景全部通过
- [x] 废弃端点物理删除（验证完成）
- [x] ADR文档创建完成
- [ ] API文档更新完成
- [ ] 用户手册创建完成

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-27 | v1.0 | 初始创建 | Claude Code |

---

**创建者**: Claude Code
**审核者**: 待评审
**批准者**: 项目团队
