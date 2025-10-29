# ADR-XXX: [简短描述性标题]

**日期**: YYYY-MM-DD
**状态**: Proposed | Accepted | Deprecated | Superseded
**决策者**: [姓名/角色]
**标签**: #架构 #性能 #重构 #安全

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-XXX |
| **创建日期** | YYYY-MM-DD |
| **最后更新** | YYYY-MM-DD |
| **状态** | Proposed &#124; Accepted &#124; Deprecated |
| **决策者** | [团队成员/架构师] |
| **影响范围** | [Server/Client/Shared/全系统] |
| **相关Issue** | #XXXX |
| **取代ADR** | [如果取代了旧ADR，填写编号] |

---

## 🎯 背景（Context）

### 问题描述

[清晰描述遇到的问题或需要解决的挑战]

**示例**：
> MedicalCase采用DDD聚合根模式后，Prescription和Consultation不应独立操作。但现有Desktop端仍保留`IPrescriptionRepository`和`IConsultationRepository`，导致ViewModel可以绕过聚合根直接修改处方和诊疗记录，违反聚合根边界原则。

### 当前状态

[描述当前的架构、实现方式或存在的问题]

**示例**：
- Desktop端依赖关系：ViewModel → IPrescriptionRepository → IPrescriptionApi
- 可以直接调用`_prescriptionRepository.UpdateAsync(dto)`绕过MedicalCase聚合根
- 违反了DDD聚合根边界原则

### 问题影响

[描述问题的影响范围和严重程度]

**示例**：
- **业务逻辑风险**：处方修改未通过MedicalCase聚合根，可能绕过业务规则验证
- **数据一致性风险**：跨聚合直接操作，可能导致MedicalCase与Prescription状态不一致
- **维护成本**：Repository层代码20%冗余，增加维护负担

---

## ✅ 决策（Decision）

[清晰、简洁地描述做出的决策]

**示例**：
删除`IPrescriptionRepository`和`IConsultationRepository`：
- **Read操作**: ViewModel → `IPrescriptionApi` (Refit，直接调用WebAPI)
- **Write操作**: ViewModel → `IMedicalCaseRepository.CreatePrescriptionAsync/UpdateConsultationAsync`（通过聚合根）

**技术实现**：
```csharp
// ✅ Read操作：直接使用API
var response = await _prescriptionApi.GetPrescriptionByIdAsync(id);
var prescription = response.Data;

// ✅ Write操作：通过聚合根Repository
await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, createDto);
await _medicalCaseRepository.UpdatePrescriptionAsync(prescriptionId, updateDto);
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ [列出决策的优点]
- ✅ [每个优点单独一行]

**示例**：
- ✅ 简化代码，减少20%Repository层代码
- ✅ 强制聚合根模式，防止跨聚合直接操作
- ✅ 提高代码可维护性，减少接口数量

### 缺点（Cons）

- ❌ [列出决策的缺点]
- ❌ [每个缺点单独一行]

**示例**：
- ❌ **违反Desktop三层架构**（View→ViewModel→Repository→ApiClient）
- ❌ 未来难以添加缓存/离线支持（需要恢复Repository层）
- ❌ 单元测试需要Mock Refit接口（而非Repository接口）

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| [具体风险] | [影响范围] | [如何缓解] |

**示例**：
| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| ViewModel直接依赖API | 违反三层架构 | 记录为架构例外，批准理由：DDD聚合根优先级高于分层架构 |
| 未来难以添加缓存 | 性能优化受限 | 补救措施：Issue #XXXX - 恢复Read-only Repository（可选） |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: [方案名称]

**描述**: [方案详细描述]

**优点**:
- ✅ [优点1]
- ✅ [优点2]

**缺点**:
- ❌ [缺点1]
- ❌ [缺点2]

**为什么未采纳**: [说明原因]

---

### 方案B: [方案名称]

**描述**: [方案详细描述]

**优点**:
- ✅ [优点1]

**缺点**:
- ❌ [缺点1]

**为什么未采纳**: [说明原因]

---

## 🏗️ 架构例外（Architecture Exceptions）

[如果此决策违反了现有架构原则，需要在此说明]

**示例**：
### 例外：Desktop三层架构违反

- **违反规则**: Desktop三层架构（View→ViewModel→Repository→ApiClient）
- **影响模块**: `LYBT.Desktop.Prescriptions`, `LYBT.Desktop.Consultation`
- **批准理由**: DDD聚合根模式优先级高于分层架构
- **补救措施**:
  - [ ] Issue #XXXX - 恢复Read-only Repository（可选，未来添加缓存时强制恢复）
  - [ ] 在架构文档中明确记录此例外

---

## 📚 参考资料（References）

- **相关Issue**: #XXXX
- **相关PR**: #XXXX
- **架构文档**: `docs/explanation/architecture/server/README.md`
- **业务规则**: `docs/explanation/business-rules.md`
- **外部资源**: [链接]

---

## 📝 实施计划（Implementation Plan）

[如果决策需要分阶段实施，描述实施步骤]

**示例**：
### Phase 1: 服务端聚合根实现（已完成）
- [x] 实现`IMedicalCaseRepository.CreatePrescriptionAsync`
- [x] 实现`IMedicalCaseRepository.UpdatePrescriptionAsync`
- [x] 实现`IMedicalCaseRepository.DeletePrescriptionAsync`

### Phase 2: Desktop端重构
- [ ] 删除`IPrescriptionRepository`接口
- [ ] 更新所有ViewModel依赖（改为`IPrescriptionApi` + `IMedicalCaseRepository`）
- [ ] 更新单元测试（Mock Refit接口）

### Phase 3: 验证和文档
- [ ] 编译验证（0 errors, 0 warnings）
- [ ] 运行时验证（完整功能可用）
- [ ] 更新架构文档和例外清单

---

## ✅ 验收标准（Acceptance Criteria）

- [ ] [标准1]
- [ ] [标准2]
- [ ] [标准3]

**示例**：
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 所有ViewModel已重构为API+Repository模式
- [ ] 单元测试通过
- [ ] 运行时验证：处方创建/更新/删除功能完整可用
- [ ] 架构例外清单已更新

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| YYYY-MM-DD | v1.0 | 初始创建 | [姓名] |
| YYYY-MM-DD | v1.1 | 更新实施计划 | [姓名] |

---

**创建者**: [姓名]
**审核者**: [姓名]（如需评审）
**批准者**: [姓名]（重大决策需批准）
