# ADR-005: 聚合根架构设计长期原则（立足于未来3-5年演进）

**日期**: 2025-10-25
**状态**: ✅ Accepted（已批准）
**决策者**: 架构团队
**标签**: #架构 #server #ddd #aggregate-root #long-term

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-005 |
| **创建日期** | 2025-10-25 |
| **最后更新** | 2025-10-25 |
| **状态** | ✅ Accepted（已批准） |
| **决策者** | 架构团队 |
| **影响范围** | Server端（所有模块） |
| **时间跨度** | 2025-2030（5年演进规划） |
| **相关ADR** | ADR-002（MedicalCase DDD聚合根模式） |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

在Epic #1611的架构审查中，发现当前项目在聚合根实现上缺乏**长期演进规划**：

**当前状态**：
- **实现方式**：Service层协调 + EF Core Change Tracking自动级联
- **业务复杂度**：14条核心规则，1:1:1简单关系
- **团队规模**：单人开发（用户 + Claude Code）
- **符合MVP原则**：✅ 当前实现简洁高效

**长期挑战**：
- **业务演进**：未来3-5年可能出现复杂业务规则（>20条）
- **关系复杂化**：1:1:1可能演变为1:N或M:N
- **团队扩大**：可能从1人增长到5人+
- **架构债务**：如果未定义演进原则，未来重构成本可能高达数月

**核心矛盾**：
- ⚖️ **MVP需求** vs **长期扩展性**
- ⚖️ **简化实现** vs **富领域模型**
- ⚖️ **快速交付** vs **架构质量**

### 用户要求

> **"调整必须立足于长期目标。这个是必须坚持的原则。"**

用户明确要求：架构设计不能只考虑当前MVP阶段，必须为未来3-5年的演进留下平滑升级路径。

### 当前架构验证

通过代码审查验证了当前的三层架构实现：

**Controller职责**（仅参数验证和路由）：
```csharp
[HttpPost("{id}/prescription")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CreatePrescription(
    Guid id, [FromBody] PrescriptionCreateDto dto)
{
    // 1. 参数验证（Controller职责）
    var idValidation = ValidateGuid<PrescriptionDto>(id, "病案ID");
    if (idValidation != null) return idValidation;

    // 2. 委托给Service层（业务逻辑在Service）
    var result = await _medicalCaseService.CreatePrescriptionAsync(id, dto);

    // 3. 响应封装（Controller职责）
    return HandleServiceResult(result, "处方创建成功");
}
```

**Service职责**（业务规则验证 + 聚合根协调）：
```csharp
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
    Guid medicalCaseId, PrescriptionCreateDto dto)
{
    // 1. 获取聚合根
    var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);

    // 2. 业务规则验证（Service层集中管理）
    if (medicalCase.Consultation == null)
        return ServiceResult<PrescriptionDto>.Failure("病案的诊断信息不存在，请先完成诊断");
    if (medicalCase.Prescription != null)
        return ServiceResult<PrescriptionDto>.Failure("病案已存在处方，请使用更新接口");

    // 3. 操作聚合根
    medicalCase.Prescription = prescription;

    // 4. EF Core自动级联保存
    await _repository.UpdateAsync(medicalCase);
}
```

**✅ 结论**：当前架构清晰，职责分离，符合MVP原则。

---

## ✅ 决策（Decision）

制定**7条聚合根架构设计长期原则**，确保未来3-5年的平滑演进：

### 原则1：渐进式演进而非推倒重来

**核心理念**：架构演进应该是渐进的（incremental），而非革命性的（revolutionary）。

**当前实施**：
- ✅ 采用"Service层协调模式"，符合当前MVP阶段需求
- ✅ 业务规则在Service层集中验证
- ✅ EF Core Change Tracking自动处理级联保存

**未来演进路径**（当触发条件时）：
```
阶段1（当前）：Service层协调
  ↓ 触发条件：业务规则 >20条 或 Service方法 >100行
阶段2（未来）：富领域模型
  ↓ 触发条件：出现复杂状态机 或 需要领域事件
阶段3（长期）：完整DDD + 领域事件
```

**演进成本评估**：
- Service层协调 → 富领域模型：5-8天（可控）
- 富领域模型 → 领域事件：10-15天（需要评估ROI）

**禁止行为**：
- ❌ 禁止为"未来可能需要"而提前实施富领域模型
- ❌ 禁止因为"教科书推荐DDD"而盲目重构
- ❌ 禁止未经量化评估就进行架构升级

---

### 原则2：架构边界清晰而非过度抽象

**三层职责定义**（严格遵守）：

| 层级 | 职责范围 | 禁止内容 |
|------|---------|---------|
| **Controller** | 参数验证、路由、日志、响应封装 | ❌ 业务规则验证<br>❌ 数据访问<br>❌ 聚合根操作 |
| **Service** | 业务规则验证、聚合根协调、事务管理 | ❌ HTTP相关逻辑<br>❌ 数据库查询语法 |
| **Repository** | 数据访问、Include预加载、查询优化 | ❌ 业务规则验证<br>❌ 聚合根状态管理 |

**示例对比**：

```csharp
// ✅ 正确：职责清晰
// Controller - 只负责参数验证
[HttpPost("{id}/prescription")]
public async Task<IActionResult> CreatePrescription(Guid id, PrescriptionCreateDto dto)
{
    var validation = ValidateGuid(id);
    if (validation != null) return validation;

    var result = await _service.CreatePrescriptionAsync(id, dto);
    return HandleServiceResult(result);
}

// Service - 只负责业务规则和聚合根协调
public async Task<ServiceResult> CreatePrescriptionAsync(Guid id, PrescriptionCreateDto dto)
{
    var medicalCase = await _repository.GetByIdWithDetailsAsync(id);

    // 业务规则验证
    if (medicalCase.Consultation == null) return Failure(...);
    if (medicalCase.Prescription != null) return Failure(...);

    // 聚合根操作
    medicalCase.Prescription = prescription;
    await _repository.UpdateAsync(medicalCase);
}

// Repository - 只负责数据访问
public async Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id)
{
    return await _context.MedicalCases
        .Include(m => m.Consultation)
        .Include(m => m.Prescription)
        .FirstOrDefaultAsync(m => m.Id == id);
}
```

```csharp
// ❌ 错误：职责混淆
// Controller包含业务规则（违反原则）
[HttpPost("{id}/prescription")]
public async Task<IActionResult> CreatePrescription(Guid id, PrescriptionCreateDto dto)
{
    var medicalCase = await _service.GetByIdAsync(id);

    // ❌ 业务规则应该在Service层
    if (medicalCase.Consultation == null)
        return BadRequest("必须先完成诊断");

    await _service.CreatePrescriptionAsync(id, dto);
}

// Repository包含业务规则（违反原则）
public async Task<MedicalCaseEntity> GetByIdWithDetailsAsync(Guid id)
{
    var entity = await _context.MedicalCases.FindAsync(id);

    // ❌ 业务规则应该在Service层
    if (entity.Status == MedicalCaseStatus.Closed)
        throw new BusinessException("已关闭的病案不能操作");

    return entity;
}
```

---

### 原则3：业务规则集中管理

**集中管理原则**：
- ✅ 所有业务规则验证必须在**Service层**集中实现
- ✅ 业务规则必须在`docs/business-rules.md`中**文档化**
- ✅ 业务规则必须通过**单元测试**验证

**规则分类**：
```
DC (Data Constraints)：数据约束规则（如ID非空、日期有效）
BF (Business Flow)：业务流程规则（如必须先诊断再开处方）
AR (Aggregate Root)：聚合根规则（如防重复创建）
AC (Access Control)：访问控制规则（如医生只能查看自己的病案）
CR (Calculation Rules)：计算规则（如处方总价计算）
```

**示例**：
```csharp
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(...)
{
    // BF-001: 必须先完成诊断
    if (medicalCase.Consultation == null)
        return Failure("病案的诊断信息不存在，请先完成诊断");

    // AR-003: 防止重复创建处方
    if (medicalCase.Prescription != null)
        return Failure("病案已存在处方，请使用更新接口");

    // DC-002: 处方药材数量验证
    if (dto.Items.Count == 0)
        return Failure("处方至少需要一味药材");

    // 业务逻辑实现...
}
```

**文档化要求**（`docs/business-rules.md`）：
```markdown
### BF-001: 诊断前置规则

**规则描述**：
- **约束**：创建处方前必须先完成诊断
- **检查时机**：`MedicalCaseService.CreatePrescriptionAsync()`

**验证逻辑**：
```csharp
if (medicalCase.Consultation == null)
    return ServiceResult.Failure("病案的诊断信息不存在，请先完成诊断");
```
```

**禁止场景**：
- ❌ 业务规则分散在Controller/Repository/Entity
- ❌ 业务规则通过注释或口头传达，未文档化
- ❌ 业务规则缺少单元测试

---

### 原则4：聚合根边界稳定

**边界稳定原则**：
- ✅ MedicalCase作为聚合根，边界不随意调整
- ✅ 子实体（Consultation/Prescription）只能通过聚合根操作
- ✅ 1:1:1关系通过共享主键维护
- ❌ 禁止直接暴露子实体的CRUD端点

**当前聚合根设计**：
```
MedicalCase（聚合根）
├── Consultation（子实体，1:1）
└── Prescription（子实体，1:1，可选）

共享主键：
MedicalCase.Id == Consultation.Id == Prescription.Id
```

**API端点设计**：
```
✅ 正确（通过聚合根操作）：
POST   /api/v1/medicalcases/{id}/consultation   - 创建诊断
PUT    /api/v1/medicalcases/{id}/consultation   - 更新诊断
POST   /api/v1/medicalcases/{id}/prescription   - 创建处方
PUT    /api/v1/medicalcases/{id}/prescription   - 更新处方
DELETE /api/v1/medicalcases/{id}/prescription   - 删除处方

❌ 错误（绕过聚合根）：
POST   /api/v1/consultations                    - 独立创建诊断
PUT    /api/v1/consultations/{id}               - 独立更新诊断
POST   /api/v1/prescriptions                    - 独立创建处方
```

**边界调整触发条件**（需要创建ADR记录）：
1. 业务需求明确要求Consultation或Prescription独立管理
2. 子实体关系从1:1演变为1:N（如一个病案多个处方）
3. 子实体生命周期与聚合根完全独立

---

### 原则5：技术选型符合Constitution

**当前阶段约束**（严格遵守）：
- ❌ 禁止Redis（使用IMemoryCache）
- ❌ 禁止CQRS（使用统一的Service + Repository）
- ❌ 禁止MediatR（直接调用Service方法）
- ❌ 禁止Event Sourcing（使用标准CRUD）
- ❌ 禁止Docker/微服务（单体应用）
- ❌ 禁止GraphQL（使用RESTful API）

**技术选型原则**：
- ✅ 使用EF Core Change Tracking自动级联（而非手动管理Entity状态）
- ✅ 使用IMemoryCache本地缓存（而非Redis分布式缓存）
- ✅ 使用ASP.NET Core内置DI（而非第三方IoC容器）
- ✅ 使用SQL Server单库（而非分库分表）

**未来演进考虑**：
当业务增长到一定规模（如日均病案>10000），可以考虑：
- 数据库读写分离（但仍禁止CQRS架构）
- Redis缓存（需要更新Constitution并创建ADR）
- 微服务拆分（需要评估ROI并更新Constitution）

---

### 原则6：演进触发条件明确

**触发条件矩阵**（满足任一条即触发演进）：

| 触发条件 | 当前状态 | 阈值 | 演进方向 | 估算成本 |
|---------|---------|------|---------|---------|
| **业务规则复杂度** | 14条 | >20条 | Service层 → 富领域模型 | 5-8天 |
| **Service方法长度** | 平均50行 | >100行 | Service层 → 富领域模型 | 5-8天 |
| **聚合根关系复杂化** | 1:1:1 | 1:N或M:N | 调整聚合根边界 | 10-15天 |
| **状态机复杂度** | 无状态机 | 状态转换 >8种 | 引入状态机模式 | 3-5天 |
| **团队规模** | 1人 | >5人 | 富领域模型 + 代码评审 | 5-8天 |
| **数据量增长** | <1000病案/月 | >10000病案/月 | 引入缓存层 | 2-3天 |

**演进决策流程**：
1. **检测触发条件**：定期评估（每季度）
2. **创建ADR文档**：记录演进决策背景
3. **评估成本收益**：量化收益 >2倍成本
4. **渐进式实施**：分阶段重构，避免大爆炸式改动
5. **验证和回滚**：每个阶段完成后运行时验证，必要时回滚

**示例**（当业务规则达到25条时）：
```
触发条件：业务规则 = 25条（>20条阈值）

决策流程：
1. 创建ADR-010：MedicalCase聚合根演进到富领域模型
2. 评估成本：5-8天重构 + 2-3天测试 = 7-11天
3. 评估收益：减少Service层复杂度50%，提高可测试性30%
4. 决策：收益 >2倍成本，批准演进
5. 实施：
   - Phase 1：创建MedicalCaseEntity领域方法（2天）
   - Phase 2：迁移业务规则到聚合根（3天）
   - Phase 3：简化Service层为薄层协调（2天）
   - Phase 4：更新单元测试（2天）
6. 验证：运行时验证 + 性能测试
```

---

### 原则7：Constitution约束可基于充分证据调整

**调整原则**：
- ✅ Constitution是"当前阶段"的强约束，而非"永久不变"的铁律
- ✅ 调整需要充分证据支持，避免技术驱动
- ✅ 调整需要明确的业务价值和ROI评估

**调整条件**（必须同时满足）：
1. **业务需求明确**：业务需求证明必要性（而非"技术更先进"）
2. **MVP替代方案评估**：已评估所有MVP替代方案，确认无法满足
3. **量化收益**：收益 >2倍成本（如引入技术后效率提升50%+）
4. **团队能力**：团队有能力学习和维护新技术，避免技术债

**调整流程**：
1. **创建ADR**：记录调整背景、评估过程、决策依据
2. **更新Constitution**：同步更新`.spec-workflow/steering/constitution.md`
3. **更新例外清单**：记录到`docs/architecture/exceptions.md`
4. **团队培训**：确保团队掌握新技术

**示例场景**（引入Redis缓存）：
```
业务需求：日均病案量增长到15000，数据库查询成为性能瓶颈

MVP替代方案评估：
- ✅ 已尝试IMemoryCache本地缓存 → 单机内存不足
- ✅ 已尝试数据库索引优化 → 查询时间仍>2秒
- ✅ 已尝试读写分离 → 成本高于Redis

量化收益评估：
- 成本：引入Redis（3天部署 + 2天开发 = 5天）
- 收益：查询时间从2秒降低到100ms（性能提升20倍）
- ROI：收益 >4倍成本 ✅

团队能力：
- 团队已掌握Redis基础知识
- 有运维能力维护Redis集群

决策：批准引入Redis，更新Constitution第1条
```

**禁止场景**：
- ❌ 因为"Redis更流行"而引入（技术驱动）
- ❌ 因为"未来可能需要"而引入（过度设计）
- ❌ 未评估MVP替代方案就引入（盲目跟风）

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **长期稳定**：为未来3-5年演进提供清晰路径
- ✅ **成本可控**：每次演进成本5-15天，可预测
- ✅ **避免推倒重来**：渐进式演进，避免大爆炸式重构
- ✅ **决策透明**：明确的触发条件和决策流程
- ✅ **团队共识**：7条原则易于理解和遵守
- ✅ **符合MVP**：当前保持简化实现，未来按需演进

### 缺点（Cons）

- ❌ **需要定期评估**：每季度需要检查触发条件（增加管理成本）
- ❌ **可能过早优化**：如果触发条件设置不当，可能导致过早演进
- ❌ **文档维护成本**：需要维护ADR、Constitution、例外清单

### 风险与缓解措施

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 触发条件误判 | 过早或延迟演进 | 中 | 每季度评估 + 量化收益评估 |
| 演进成本超预期 | 项目延期 | 低 | 分阶段实施 + 及时回滚机制 |
| Constitution过于严格 | 限制技术选型 | 低 | 允许基于充分证据调整（原则7） |
| 团队不理解原则 | 违反架构约束 | 中 | 更新CLAUDE.md + 定期培训 |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: 立即实施富领域模型（未采纳）

**描述**: 现在就将所有业务规则迁移到聚合根Entity方法中

**优点**:
- ✅ 符合经典DDD理论
- ✅ 领域逻辑高度内聚

**缺点**:
- ❌ 重构成本5-8天（违反MVP原则）
- ❌ 当前业务规则仅14条，未达到复杂度阈值
- ❌ 为"未来可能需要"而提前设计（过度设计）

**为什么未采纳**:
- MVP阶段应专注快速交付，而非架构完美性
- 当前Service层协调模式已足够简洁高效
- 未来可以通过渐进式演进平滑升级（成本可控）

---

### 方案B: 混合模式（部分富领域模型）（未采纳）

**描述**: 关键业务规则放在Entity，辅助规则保留在Service

**优点**:
- ✅ 平衡MVP和DDD
- ✅ 重构成本较低（2-3天）

**缺点**:
- ❌ 业务规则分散在Service和Entity（职责不清）
- ❌ 新成员难以判断规则应该放在哪里
- ❌ 增加维护复杂度

**为什么未采纳**:
- 混合模式会导致职责边界模糊
- MVP阶段应保持简单，统一在Service层管理
- 如果未来需要，可以一次性完整迁移到富领域模型

---

### 方案C: 无演进规划（保持现状）（未采纳）

**描述**: 不制定长期原则，按需调整

**优点**:
- ✅ 极致灵活，无约束

**缺点**:
- ❌ 缺乏长期规划，未来重构成本不可控
- ❌ 可能导致技术债务累积
- ❌ 团队缺乏共识，容易产生架构争议
- ❌ 违反用户要求："调整必须立足于长期目标"

**为什么未采纳**:
- 用户明确要求立足长期目标
- 无规划会导致未来重构成本指数级增长
- 7条原则提供了清晰的演进路径

---

## 📚 参考资料（References）

- **相关ADR**:
  - ADR-002: MedicalCase DDD聚合根模式
  - ADR-003: Prescriptions/Consultation Repository层简化
  - ADR-004: Component设计指南
- **架构文档**:
  - `docs/architecture/server/README.md` - Server端三层架构
  - `docs/architecture/evolution.md` - 架构演进时间线
  - `docs/architecture/compliance-checklist.md` - 架构合规性检查
  - `docs/business-rules.md` - 14条核心业务规则
- **Constitution**:
  - `.spec-workflow/steering/constitution.md` - 项目强制性原则
- **外部资源**:
  - [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
  - [Implementing DDD - Vaughn Vernon](https://vaughnvernon.com/)
  - [Evolutionary Architecture - ThoughtWorks](https://www.thoughtworks.com/insights/blog/evolutionary-architecture)

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 文档更新（立即执行）
- [x] 创建ADR-005记录长期原则
- [ ] 更新CLAUDE.md（添加"立足长期目标"原则）
- [ ] 更新`docs/architecture/evolution.md`（添加演进触发条件）
- [ ] 更新`docs/architecture/server/README.md`（引用ADR-005）

### Phase 2: 触发条件监控（季度执行）
- [ ] 创建监控脚本：统计业务规则数量、Service方法长度
- [ ] 设置提醒：当接近触发条件时自动通知
- [ ] 季度评估会议：评估是否达到演进触发条件

### Phase 3: 演进准备（按需执行）
- [ ] 当触发条件满足时，创建演进ADR
- [ ] 评估成本收益，获得批准后实施
- [ ] 分阶段重构，每阶段完成后验证

---

## ✅ 验收标准（Acceptance Criteria）

- [x] ADR-005已创建并批准
- [ ] CLAUDE.md已更新（包含长期目标原则）
- [ ] 7条原则已明确定义和文档化
- [ ] 演进触发条件已量化（6个指标 + 阈值）
- [ ] 演进成本已评估（5-15天范围）
- [ ] Constitution调整流程已定义
- [ ] 团队已理解并认可7条原则

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建（基于Epic #1611架构审查 + 用户要求"立足长期目标"） | Claude Code |

---

**创建者**: Claude Code（资深开发 + 架构师）
**审核者**: 待定
**批准者**: 架构团队（用户已批准技术决策）
