# Phase 3: 项目经理视角 - 核心决策点清单

**创建日期**: 2025-10-25
**目的**: 从项目初始状态重新审视项目，明确MVP核心价值，确定架构债务的优先级
**Epic跟踪**: #1611 - 系统性重构（文档-代码对齐与架构优化）

---

## 📋 清单概述

基于Phase 1（文档分析）和Phase 2（代码分析），我需要与您明确以下决策点，以确定：
1. **什么是MVP的真正核心价值？**
2. **哪些架构设计是必需的？哪些是过度设计？**
3. **Phase 5代码重构的范围和优先级？**

本清单分为5个议题，每个议题包含：
- **背景**：当前状态和问题
- **待明确的决策点**：需要您回答的问题
- **决策影响**：不同决策的工作量和影响
- **我的初步建议**：基于MVP原则的推荐方案

---

## 🎯 议题1: 聚合根模式的必要性评估

### 背景

**文档预期**（docs/architecture/patterns/aggregate-root-pattern.md）：
- MedicalCase作为聚合根，Consultation/Prescription作为子实体
- IMedicalCaseRepository应该包含子实体操作方法：
  - `CreatePrescriptionAsync(medicalCaseId, dto)`
  - `UpdatePrescriptionAsync(prescriptionId, dto)`
  - `DeletePrescriptionAsync(prescriptionId)`
  - `UpdateConsultationAsync(consultationId, dto)`

**实际代码情况**：
- ✅ **Controller层完全实现**（MedicalCaseController有所有子实体操作方法）
- ✅ **子实体Repository改为Read-only**（IPrescriptionRepository/IConsultationRepository只有Read方法）
- ❌ **IMedicalCaseRepository缺失子实体操作方法**（只有基础CRUD）
- ❓ **Service层如何实现**？（未深入分析，可能直接操作DbContext）

**Issue #1600进度**：
- Phase 1 ✅：子实体Repository改为Read-only
- Phase 4 ✅：子实体Controller的Write方法移除
- ⚠️ **Repository层补充未完成**

---

### 待明确的决策点

#### 问题1.1: 对于MVP阶段，聚合根模式是否真的必要？

**选项A**: **完整实施聚合根模式**（符合文档）
- 补充IMedicalCaseRepository的子实体操作方法
- MedicalCaseService调用Repository方法
- 保证业务规则集中在聚合根

**选项B**: **简化为Service层直接操作DbContext**（简化实现）
- IMedicalCaseRepository只保留基础CRUD
- MedicalCaseService直接使用DbContext操作子实体
- 业务规则在Service层验证

**选项C**: **取消聚合根模式，恢复独立CRUD**（最简单）
- 恢复IPrescriptionRepository的Write方法
- 恢复IConsultationRepository的Write方法
- 每个实体独立操作，无聚合根概念

**我的问题**：
1. 当前系统是否存在"必须通过聚合根保证一致性"的业务场景？
2. 例如：创建处方时是否必须验证MedicalCase的状态（如已归档则禁止）？
3. 还是简单的CRUD就足够了？

#### 问题1.2: 聚合根模式的价值 vs 成本权衡

**聚合根模式的价值**：
- ✅ 业务规则集中（如"已归档医案不能创建处方"）
- ✅ 事务边界清晰（一次操作保证一致性）
- ✅ 符合DDD最佳实践

**聚合根模式的成本**：
- ❌ 开发复杂度增加（需要补充Repository方法）
- ❌ 学习成本高（新成员需要理解聚合根概念）
- ❌ 测试复杂度增加（需要Mock聚合根Repository）

**我的问题**：
1. 您认为这个价值-成本比是否值得？
2. MVP阶段是否可以接受"简化实现"，未来再优化？

---

### 决策影响

| 决策 | 工作量 | 影响范围 | 风险 |
|-----|--------|---------|------|
| **选项A**：完整实施聚合根 | 3-4小时 | Server端Repository + Service层 | 低（符合文档） |
| **选项B**：简化为Service直接操作DbContext | 0小时（已实现） | 更新文档说明 | 中（违反Repository模式） |
| **选项C**：取消聚合根，恢复独立CRUD | 2-3小时 | 回退Issue #1600的修改 | 高（推翻现有设计） |

---

### 我的初步建议

**推荐选项B**（简化实现）：
1. **当前代码已经是选项B**（Controller → Service → DbContext）
2. **符合MVP"够用即好"原则**
3. **工作量为0**（只需更新文档说明）
4. **未来可升级为选项A**（如果业务规则复杂化）

**需要做的事**：
- 更新ADR-002，说明"MVP阶段简化实施聚合根模式"
- 在IMedicalCaseRepository注释中说明"子实体操作在Service层实现"
- 更新docs/architecture/patterns/aggregate-root-pattern.md，补充"简化实施方案"章节

**待确认**：
- 您是否同意选项B？
- 还是您更倾向于选项A（完整实施）或选项C（取消聚合根）？

---

## 🎯 议题2: Desktop端三层架构例外的长期方案

### 背景

**当前状态**（EXC-001）：
- **违反原则**：Desktop三层架构（View→ViewModel→Repository→ApiClient）
- **实际实现**：ViewModel直接依赖`IPrescriptionApi`（Refit接口）
- **批准原因**：DDD聚合根模式优先级高于分层架构
- **风险级别**：P1（中风险）
- **审查周期**：每半年

**文档描述**（docs/architecture/exceptions.md）：
- Read操作：ViewModel → API（跳过Repository）
- Write操作：ViewModel → IMedicalCaseRepository（通过聚合根）

**实际代码验证**：
- ✅ Repository目录为空（符合例外）
- ✅ ViewModel直接使用IPrescriptionApi（符合例外）
- ✅ Write操作通过IMedicalCaseRepository（符合聚合根）

---

### 待明确的决策点

#### 问题2.1: 是否长期保留EXC-001例外？

**选项A**: **长期保留例外**（ViewModel直接调用API）
- 接受Desktop端不需要Repository层
- 降低EXC-001风险级别为P2
- 延长审查周期为1年

**选项B**: **恢复Repository层（仅Read方法）**
- 创建IPrescriptionRepository（Read-only）
- PrescriptionRepository薄封装IPrescriptionApi
- 未来可添加缓存逻辑

**选项C**: **等待MVP完成后再决定**
- 暂时保留EXC-001
- 等待实际需求（如缓存、离线支持）再决定

**我的问题**：
1. 未来是否确定需要缓存层？
2. 是否有离线支持的计划？
3. 如果都没有，是否可以长期保留例外？

#### 问题2.2: Desktop端性能和用户体验的优先级

**场景分析**：
- 处方列表刷新频率高 → 可能需要缓存
- 离线场景（网络不稳定） → 可能需要本地数据库
- 当前MVP阶段（<10并发用户） → 可能不需要缓存

**我的问题**：
1. 当前用户使用场景是什么？（单机？局域网？）
2. 是否存在性能瓶颈？
3. 是否需要优先优化用户体验？

---

### 决策影响

| 决策 | 工作量 | 影响范围 | 风险 |
|-----|--------|---------|------|
| **选项A**：长期保留例外 | 0小时（更新文档） | 更新EXC-001风险级别 | 低（简化架构） |
| **选项B**：恢复Read-only Repository | 2-3小时 | Desktop端Prescriptions/Consultation | 中（增加抽象层） |
| **选项C**：暂时保留，未来决定 | 0小时 | 无变化 | 低（延迟决策） |

---

### 我的初步建议

**推荐选项A**（长期保留例外）：
1. **MVP阶段无需缓存和离线支持**
2. **符合"够用即好"原则**
3. **降低代码复杂度**
4. **如果未来需要缓存，再恢复Repository层**

**需要做的事**：
- 更新EXC-001风险级别：P1 → P2
- 更新审查周期：每半年 → 每年
- 在例外清单中补充"长期保留原因"

**待确认**：
- 您是否同意选项A？
- 还是您认为未来会需要缓存（选项B）？

---

## 🎯 议题3: 过度设计Component的清理策略 ⭐⭐⭐

### 背景

**ADR-004决策**（2025-10-25提出）：
- 删除PrescriptionCommandHandler（523行）
- 删除PrescriptionDataManager（336行）
- 删除PrescriptionEventCoordinator（502行）
- 删除PrescriptionValidator（168行）
- 删除PrescriptionCalculator（128行）
- **共计1657行代码**

**实际代码情况**：
- ❌ **ADR-004决策未执行**，5个Component仍然存在
- ❌ **全部只在Prescriptions模块使用**（违反"跨模块共享"原则）
- ❌ **职责与ViewModel重叠**（违反"职责清晰"原则）

**注释证据**（PrescriptionCommandHandler.cs:15-16）：
```csharp
/// <summary>
/// 处方命令处理器 - UltraThink架构实现
/// 负责处理处方相关的业务命令
/// </summary>
```

**问题根源**：这是某次UltraThink深度分析中创建的过度设计，但后续未清理。

---

### 待明确的决策点

#### 问题3.1: 是否立即执行ADR-004，清理1657行Component代码？

**选项A**: **立即执行ADR-004**（删除5个Component）
- 将Component逻辑合并到ViewModel
- 删除5个Component文件
- 更新依赖注入配置
- **工作量**：4-6小时

**选项B**: **延迟执行ADR-004**
- 创建Issue跟踪清理任务
- 明确延迟原因（如功能依赖、时间限制）
- 设定执行时间（如MVP完成后）

**选项C**: **保留部分Component**
- 删除薄封装Component（PrescriptionCommandHandler、PrescriptionDataManager）
- 保留有真实业务逻辑的Component（PrescriptionValidator、PrescriptionCalculator）
- **工作量**：2-3小时

**我的问题**：
1. 当前这5个Component是否被其他功能依赖？
2. 删除后是否会影响现有功能？
3. 是否有时间立即清理？还是需要延后？

#### 问题3.2: 合并逻辑到ViewModel的风险评估

**潜在风险**：
- ViewModel代码膨胀（可能从200行增加到500行）
- 单一职责原则违反（ViewModel既管理UI状态，又处理业务逻辑）
- 测试复杂度增加（ViewModel的Mock更复杂）

**应对策略**：
- 使用partial class拆分ViewModel（如PrescriptionEditorDialogViewModel.Commands.cs）
- 提取纯业务逻辑为静态方法（如PrescriptionCalculator → 静态工具类）
- 提取验证逻辑为FluentValidation（如PrescriptionValidator → Validator类）

**我的问题**：
1. 您是否接受ViewModel代码增加到500行左右？
2. 还是倾向于保留部分Component（选项C）？

---

### 决策影响

| 决策 | 工作量 | 影响范围 | 风险 | 代码行数变化 |
|-----|--------|---------|------|-------------|
| **选项A**：立即删除全部Component | 4-6小时 | Prescriptions模块ViewModel | 中（可能影响现有功能） | -1657行 |
| **选项B**：延迟执行，创建Issue跟踪 | 0小时 | 无变化 | 低（延迟决策） | 0 |
| **选项C**：删除部分，保留业务逻辑Component | 2-3小时 | Prescriptions模块ViewModel | 低（保留核心逻辑） | -850行（删除薄封装） |

---

### 我的初步建议

**推荐选项C**（删除薄封装，保留业务逻辑）：
1. **删除薄封装Component**：
   - PrescriptionCommandHandler（523行）→ 合并到ViewModel
   - PrescriptionDataManager（336行）→ 合并到ViewModel
   - PrescriptionEventCoordinator（502行）→ 使用Prism EventAggregator

2. **保留并重构业务逻辑Component**：
   - PrescriptionValidator（168行）→ 提取为FluentValidation Validator
   - PrescriptionCalculator（128行）→ 提取为静态工具类（Utilities层）

3. **原因**：
   - 减少850行薄封装代码（符合ADR-004主要目标）
   - 保留业务逻辑的可复用性（Validator和Calculator可能被其他模块使用）
   - 降低风险（业务逻辑不变，只是重构位置）

**需要做的事**：
- 更新ADR-004，说明"部分执行"策略
- 将PrescriptionValidator改为FluentValidation
- 将PrescriptionCalculator移动到LYBT.Shared.Utilities

**待确认**：
- 您是否同意选项C？
- 还是您更倾向于选项A（全部删除）或选项B（延迟执行）？

---

## 🎯 议题4: 8个Server模块的合理性评估

### 背景

**当前模块结构**：
1. **LYBT.Module.Auth** - 认证与授权
2. **LYBT.Module.Users** - 用户管理
3. **LYBT.Module.Patients** - 患者管理
4. **LYBT.Module.MedicalCase** - 医案管理（⭐ 聚合根）
5. **LYBT.Module.Consultation** - 诊疗记录（⭐ 子实体）
6. **LYBT.Module.Prescriptions** - 处方管理（⭐ 子实体）
7. **LYBT.Module.Herbs** - 药材管理
8. **LYBT.Module.Formula** - 验方管理

**观察**：
- Consultation和Prescription是MedicalCase的子实体
- 但它们有独立的Module（包含Repository、Service、Mapping、Validators）
- 这与聚合根模式的"子实体不应独立存在"原则有冲突

---

### 待明确的决策点

#### 问题4.1: Consultation和Prescription是否应该独立为Module？

**选项A**: **保持8个独立Module**（当前状态）
- 每个业务概念有独立的Module
- 模块职责清晰，边界明确
- 符合微服务思想（虽然Constitution禁止微服务架构）

**选项B**: **合并子实体到MedicalCase Module**
- 删除LYBT.Module.Consultation和LYBT.Module.Prescriptions
- 将Consultation和Prescription的Repository、Service移动到LYBT.Module.MedicalCase
- 只保留6个Module（Auth, Users, Patients, MedicalCase, Herbs, Formula）

**选项C**: **保留Module，但标记为Read-only**
- Consultation和Prescription的Module只包含Read-only Repository
- Write操作全部在MedicalCase Module中
- 明确标记这两个Module为"子实体Module"

**我的问题**：
1. 您认为Consultation和Prescription应该独立为Module吗？
2. 还是应该作为MedicalCase的内部组成部分？
3. 未来是否可能独立使用Consultation（不依赖MedicalCase）？

#### 问题4.2: 模块边界的定义标准

**模块边界的两种定义**：
1. **按业务概念划分**：每个业务实体（Entity）对应一个Module
2. **按聚合根划分**：每个聚合根对应一个Module，子实体不独立

**我的问题**：
1. 您更倾向于哪种定义？
2. 是否有其他模块也存在聚合根关系（如Patient和MedicalCase）？

---

### 决策影响

| 决策 | 工作量 | 影响范围 | 风险 |
|-----|--------|---------|------|
| **选项A**：保持8个独立Module | 0小时 | 无变化 | 低（现状维持） |
| **选项B**：合并子实体到MedicalCase | 10-15小时 | Server端模块结构 | 高（大规模重构） |
| **选项C**：保留Module，标记Read-only | 1小时（更新文档） | 更新Module README | 低（文档说明） |

---

### 我的初步建议

**推荐选项C**（保留Module，标记Read-only）：
1. **保持当前8个Module结构**（降低重构风险）
2. **在Module README中明确标记**：
   - LYBT.Module.Consultation：子实体Module（Read-only Repository）
   - LYBT.Module.Prescriptions：子实体Module（Read-only Repository）
   - LYBT.Module.MedicalCase：聚合根Module（包含子实体Write操作）
3. **符合MVP"够用即好"原则**

**需要做的事**：
- 更新LYBT.Module.Consultation/README.md，说明"子实体Module"
- 更新LYBT.Module.Prescriptions/README.md，说明"子实体Module"
- 更新docs/architecture/server/README.md，补充"模块边界定义"章节

**待确认**：
- 您是否同意选项C？
- 还是您更倾向于选项B（合并模块）？

---

## 🎯 议题5: MVP核心价值与范围确认 ⭐⭐⭐

### 背景

**Constitution强调**（.spec-workflow/steering/constitution.md）：
- "够用即好"原则（MUST）
- 技术黑名单（禁止Redis/CQRS/MediatR/Docker/GraphQL等）
- MVP优先（避免过度设计）

**当前架构观察**：
- ✅ 遵守技术黑名单（未使用禁用技术）
- ⚠️ 部分过度设计（1657行Component、未完全实施的聚合根模式）
- ⚠️ 文档与代码不一致（聚合根模式文档描述 vs 实际代码）

**Phase 1 + Phase 2发现的问题**：
- P0问题：2个（过度设计Component、缺失Shared架构文档）
- P1问题：2个（聚合根模式部分实施、文档更新不同步）
- P2问题：1个（讨论文档未归档）

---

### 待明确的决策点

#### 问题5.1: 什么是本项目的真正核心业务价值？

**请您用1-3句话描述**：
1. 这个系统最重要的功能是什么？
2. 哪些功能是MVP必需的？哪些可以延后？
3. 用户最关心的是什么？（功能完整性？性能？易用性？）

**示例回答**：
- "核心价值是中医诊所的日常诊疗管理（患者、医案、处方），必须功能包括开处方和查看历史病历，性能和离线支持可以延后。"

**我的问题**：
- 请您用自己的话描述核心业务价值？

#### 问题5.2: 架构质量 vs 快速交付的优先级

**两种极端**：
1. **架构质量优先**：完整实施聚合根模式、清理所有过度设计、文档-代码100%对齐
   - 优点：代码质量高、可维护性好、符合最佳实践
   - 缺点：开发时间长、MVP交付延迟

2. **快速交付优先**：简化实施、保留部分过度设计、文档-代码部分对齐
   - 优点：快速上线、MVP快速验证、降低开发成本
   - 缺点：技术债务累积、未来重构成本高

**我的问题**：
1. 您更倾向于哪种优先级？（1-10分，1=快速交付，10=架构质量）
2. 当前MVP的时间压力如何？（紧急？宽松？）
3. 是否可以接受"先上线，后优化"的策略？

#### 问题5.3: 技术债务的接受程度

**Phase 2发现的技术债务**：
1. 聚合根模式部分实施（Repository层缺失）
2. 过度设计Component（1657行）
3. 文档-代码不一致（部分）
4. 讨论文档未归档

**我的问题**：
1. 哪些技术债务您认为必须立即修复？
2. 哪些可以记录Issue，未来优化？
3. 是否可以接受"有技术债务，但功能可用"的状态？

---

### 决策影响

基于您对上述3个问题的回答，我将能够：
1. **明确Phase 5的重构范围**（哪些必须修复，哪些延后）
2. **确定重构优先级**（P0 → P1 → P2）
3. **估算总工作量**（10-20小时？30-40小时？）
4. **制定分阶段实施计划**（MVP上线前 vs MVP上线后）

---

### 我的初步建议

**基于"够用即好"原则，推荐快速交付策略**：

**Phase 5必须修复（MVP上线前）**：
1. **DOC-P0-1**: 创建缺失的Shared架构文档（2-3小时）
2. **CODE-P0-1**: 删除部分过度设计Component（选项C，2-3小时）
3. **文档同步**: 更新ADR和例外清单，反映实际代码（1小时）

**Phase 5延后修复（MVP上线后）**：
1. **CODE-P1-1**: 补充聚合根Repository方法（3-4小时）
2. **CODE-P2-1**: 归档讨论文档（1小时）

**总工作量**：5-7小时（MVP上线前）+ 4-5小时（MVP上线后）= **10-12小时**

**待确认**：
- 您是否同意这个快速交付策略？
- 还是您更倾向于完整修复所有问题（20-30小时）？

---

## 📋 决策点汇总表

| 议题 | 核心决策点 | 推荐选项 | 工作量 | 优先级 |
|-----|----------|---------|--------|--------|
| **议题1**: 聚合根模式 | 是否完整实施聚合根Repository？ | 选项B：简化实施（Service直接操作DbContext） | 0小时 | ⭐⭐⭐ |
| **议题2**: Desktop三层架构例外 | 是否长期保留EXC-001例外？ | 选项A：长期保留例外 | 0小时 | ⭐⭐ |
| **议题3**: 过度设计Component | 是否立即清理1657行Component？ | 选项C：删除薄封装，保留业务逻辑 | 2-3小时 | ⭐⭐⭐ |
| **议题4**: 8个Server模块 | 是否合并子实体Module？ | 选项C：保留Module，标记Read-only | 1小时 | ⭐ |
| **议题5**: MVP核心价值 | 架构质量 vs 快速交付？ | 快速交付策略（5-7小时必修 + 4-5小时延后） | 10-12小时 | ⭐⭐⭐ |

**总工作量估算**：
- **立即执行**（MVP上线前）：5-7小时
- **延后执行**（MVP上线后）：4-5小时
- **总计**：10-12小时

---

## 📝 讨论准备清单

### 请您思考以下问题（可以补充或修改）：

#### 关于业务价值
- [ ] 核心业务价值是什么？（1-3句话）
- [ ] MVP必需功能有哪些？
- [ ] 用户最关心什么？（功能？性能？易用性？）

#### 关于架构质量 vs 快速交付
- [ ] 您的优先级是什么？（1-10分，1=快速交付，10=架构质量）
- [ ] 当前MVP的时间压力如何？
- [ ] 是否可以接受"先上线，后优化"？

#### 关于技术债务
- [ ] 哪些技术债务必须立即修复？
- [ ] 哪些可以延后？
- [ ] 是否可以接受"有技术债务，但功能可用"？

#### 关于具体议题
- [ ] **议题1**：聚合根模式选哪个选项？（A/B/C）
- [ ] **议题2**：Desktop三层架构例外选哪个选项？（A/B/C）
- [ ] **议题3**：过度设计Component选哪个选项？（A/B/C）
- [ ] **议题4**：8个Server模块选哪个选项？（A/B/C）
- [ ] **议题5**：是否同意快速交付策略？

---

## 🎯 讨论后输出

讨论完成后，我将生成以下文档：
1. **Phase 3决策记录**（docs/reports/phase3-decisions-2025-10-25.md）
2. **更新ADR和例外清单**（反映实际决策）
3. **Phase 5实施计划**（明确范围、优先级、工作量）
4. **Issue清单**（创建对应的GitHub Issues）

---

**文档结束**
**下一步**: 等待您的补充和讨论
