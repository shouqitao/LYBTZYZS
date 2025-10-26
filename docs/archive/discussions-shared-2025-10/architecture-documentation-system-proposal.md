# 架构文档体系完善方案

**创建日期**: 2025-10-25
**来源**: Issue #1608架构问题回顾
**状态**: 提案（Proposal）

## 📋 问题背景

### 1. Issue #1608暴露的架构问题

在完成Issue #1608（Prescriptions模块重构）过程中，发现了两个关键架构问题：

**问题1：Desktop三层架构违反**
- **现象**: Issue #1606删除`IPrescriptionRepository`，ViewModel直接依赖`IPrescriptionApi` (Refit)
- **后果**: 违反Desktop三层架构（View→ViewModel→**Repository**→ApiClient）
- **根本原因**: 缺少架构决策记录（ADR），导致简化决策未被正式评审和记录

**问题2：Component过度设计**
- **现象**: `PrescriptionCommandHandler`设计了5个互相依赖的Component类
  - `PrescriptionCommandHandler` (命令处理器)
  - `PrescriptionDataManager` (数据管理器)
  - `PrescriptionCalculator` (价格计算器)
  - `PrescriptionValidator` (验证器)
  - `PrescriptionEventCoordinator` (事件协调器)
- **后果**: 违反KISS/YAGNI原则，MedicalCase聚合根后这些组件不再必要
- **根本原因**: 缺少Component设计指南和过度设计检测机制

### 2. 根本性缺陷：架构治理体系缺失

当前项目缺少：
1. **架构决策记录（ADR）**：无法追溯重要架构决策的原因和背景
2. **架构原则分级**：没有区分强制性(Mandatory)、推荐性(Recommended)、指导性(Guideline)
3. **架构例外管理**：没有正式机制跟踪已批准的架构偏离
4. **架构模式库**：缺少标准化的实现模式参考
5. **架构合规检查清单**：没有系统化的检查流程

---

## 🎯 解决方案：建立完整的架构文档体系

### 方案A：完整架构治理体系（推荐）⭐

建立系统化的架构文档体系，包含5个核心组件：

#### 1. ADR（Architecture Decision Record）系统

**目录结构**：
```
docs/architecture/decisions/
├── README.md                # ADR索引和使用指南
├── template.md              # ADR模板
├── ADR-001-three-tier-alignment.md
├── ADR-002-ddd-aggregate-root.md
├── ADR-003-repository-simplification.md  # Issue #1606决策
└── ADR-004-component-design-guidelines.md  # Issue #1608提案
```

**ADR-003: Repository层简化（Issue #1606）**
```markdown
# ADR-003: Prescriptions/Consultation Repository层简化

**日期**: 2025-XX-XX
**状态**: Accepted（有条件接受）
**决策者**: 开发团队

## 背景

MedicalCase采用DDD聚合根模式后，Prescription和Consultation不应独立操作。

## 决策

删除`IPrescriptionRepository`和`IConsultationRepository`：
- **Read操作**: ViewModel → `IPrescriptionApi` (Refit)
- **Write操作**: ViewModel → `IMedicalCaseRepository.CreatePrescriptionAsync/UpdateConsultationAsync`

## 后果

**优点**:
- 简化代码，减少20%Repository层代码
- 强制聚合根模式，防止跨聚合直接操作

**缺点**:
- **违反Desktop三层架构**（View→ViewModel→Repository→ApiClient）
- 未来难以添加缓存/离线支持
- 单元测试需Mock Refit接口

**架构例外**:
- Desktop三层架构违反（Prescriptions/Consultation模块）
- 批准理由：DDD聚合根优先级高于分层架构
- 补救措施：未来可恢复Read-only Repository（见Issue #XXXX）
```

**ADR-004: Component设计指南（Issue #1608提案）**
```markdown
# ADR-004: Component设计指南

**日期**: 2025-10-25
**状态**: Proposed（待批准）
**决策者**: TBD

## 背景

`PrescriptionCommandHandler`过度设计（5个Component），违反KISS/YAGNI。

## 提案

**Component设计原则**:
1. **最小充分原则**: 只有**复用跨2+ViewModel**时才抽取Component
2. **纯工具类优先**: 优先设计无状态纯工具类（Calculator/Validator）
3. **禁止Component互相依赖**: 5个Component互相依赖是设计错误
4. **Event over Command**: UI事件直接在ViewModel处理，不要抽取CommandHandler

**Prescription模块重构建议**:
- **删除**: `PrescriptionCommandHandler`, `PrescriptionEventCoordinator`
- **保留**: `PrescriptionCalculator`（纯函数）, `PrescriptionValidator`（纯函数）
- **重构**: `PrescriptionDataManager` → 合并到`PrescriptionViewModel`

## 后果

- 删除13个冗余命令和事件
- 代码行数减少30%
- 提高可维护性

## 决策

[ ] 批准 - 创建Issue实施重构
[ ] 拒绝 - 保持现状
```

#### 2. 架构原则分级（Principles）

**文件**: `docs/architecture/principles.md`

**⭐ 强制性文档读取规则**：
- **需求分析前**：必须先阅读 `docs/index.md` → 相关架构文档 → 业务规则文档
- **设计文档前**：必须先阅读对应模块的架构指南（Server/Client/Shared）
- **架构调整前**：必须先更新 ADR → 架构文档 → 再开始代码变更
- **违反处理**：拒绝执行任何未读取文档的需求分析或设计任务

**三级分类**:
```markdown
# 架构原则

## Level 1: Mandatory（强制性）- 违反需Epic批准

### Server端
- ✅ **依赖方向**: Application→Domain→Infrastructure ❌反向依赖
- ✅ **聚合根边界**: Prescription/Consultation通过MedicalCase聚合根操作
- ✅ **Repository粒度**: 每个聚合根对应一个Repository

### Client端
- ✅ **三层架构**: View→ViewModel→Repository→ApiClient
  - ⚠️ **例外**: Prescriptions/Consultation模块（ADR-003批准）
- ✅ **依赖注入**: 仅构造函数注入，禁止ServiceLocator
- ✅ **MVVM模式**: View不直接调用API

## Level 2: Recommended（推荐性）- 违反需Issue说明

- ✅ **Component设计**: 复用跨2+ViewModel时抽取
- ✅ **异步优先**: I/O操作必须async/await
- ✅ **单文件体量**: ≤500行（ViewModel可放宽到800行）

## Level 3: Guideline（指导性）- 建议遵守

- 📘 **命名规范**: PascalCase类型，_camelCase私有字段
- 📘 **注释规范**: 公开API必须XML注释
```

#### 3. 架构例外清单（Exceptions）

**文件**: `docs/architecture/exceptions.md`

```markdown
# 架构例外清单

**说明**: 本文件记录所有已批准的架构规则偏离。

## 活跃例外（Active Exceptions）

### EXC-001: Prescriptions/Consultation Desktop三层架构违反

- **违反规则**: Desktop三层架构（View→ViewModel→Repository→ApiClient）
- **影响模块**: `LYBT.Desktop.Prescriptions`, `LYBT.Desktop.Consultation`
- **批准日期**: 2025-XX-XX（ADR-003）
- **批准理由**: DDD聚合根模式优先级高于分层架构
- **补救措施**:
  - [ ] Issue #XXXX - 恢复Read-only Repository（可选）
  - [ ] Issue #XXXX - 添加缓存层时强制恢复Repository

### EXC-002: （示例）MVP阶段无Repository单元测试

- **违反规则**: "所有Repository必须有单元测试"
- **影响模块**: 所有Repository
- **批准日期**: 2025-XX-XX
- **批准理由**: MVP阶段优先功能交付
- **补救措施**:
  - [ ] Epic #1343完成后补充测试

## 已解决例外（Resolved Exceptions）

（无）
```

#### 4. 架构模式库（Patterns）

**目录结构**:
```
docs/architecture/patterns/
├── README.md
├── repository-pattern.md       # Repository标准模式
├── component-pattern.md        # Component设计模式
├── aggregate-root-pattern.md   # DDD聚合根模式
└── mvvm-pattern.md             # WPF MVVM模式
```

**示例**: `repository-pattern.md`
```markdown
# Repository模式标准

## 标准结构

### Server端Repository
```csharp
// Interface: 领域接口（放在Domain或Shared.Interfaces）
public interface IMedicalCaseRepository
{
    Task<MedicalCase?> GetByIdAsync(Guid id);
    Task<MedicalCase> CreateAsync(MedicalCase entity);
    Task<MedicalCase> UpdateAsync(MedicalCase entity);
    Task DeleteAsync(Guid id);
}

// Implementation: 基础设施实现
public class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly AppDbContext _context;
    // ... 实现
}
```

### Client端Repository（Read-Write分离）

```csharp
// Read操作: 调用API
var response = await _api.GetMedicalCasesAsync(page, size, keyword);
var cases = response.Data?.Items ?? new List<MedicalCaseDto>();

// Write操作: 通过聚合根Repository
await _medicalCaseRepository.CreatePrescriptionAsync(caseId, dto);
```

## 反模式（Anti-patterns）

❌ **跨聚合直接操作**
```csharp
// 错误：直接操作Prescription（跨聚合根）
await _prescriptionRepository.CreateAsync(dto);

// 正确：通过MedicalCase聚合根操作
await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, dto);
```
```

#### 5. 架构合规检查清单（Compliance Checklist）

**文件**: `docs/architecture/compliance-checklist.md`

```markdown
# 架构合规检查清单

**用途**: PR Review、Issue设计阶段、架构审查会

## 1. 依赖方向检查（Mandatory）

Server端:
- [ ] Application不依赖Infrastructure
- [ ] Domain不依赖Application
- [ ] 使用`dotnet-arch-test`自动化验证

Client端:
- [ ] ViewModel不直接依赖ApiClient（除已批准例外）
- [ ] View不直接调用Repository/API

## 2. 聚合根边界检查（Mandatory）

- [ ] Prescription/Consultation通过MedicalCase聚合根操作
- [ ] Repository粒度符合1聚合根=1Repository

## 3. Component设计检查（Recommended）

- [ ] Component复用跨2+ViewModel
- [ ] 无Component互相依赖
- [ ] 优先纯工具类设计

## 4. MVVM模式检查（Mandatory）

- [ ] ViewModel继承`UnifiedViewModelBase`
- [ ] 使用`ICommand`而非直接事件处理
- [ ] 数据绑定而非代码操作UI

## 5. 过度设计检查（YAGNI）

- [ ] 无"可能将来需要"的抽象
- [ ] 无3层以上继承链
- [ ] 无未使用的接口方法

## 6. Constitution技术黑名单检查（Mandatory）

- [ ] 无Redis/CQRS/MediatR/Docker/GraphQL等黑名单技术
- [ ] 数据库仅SQL Server
- [ ] 消息队列仅RabbitMQ（如需要）
```

---

### 方案B：简化方案（最小补救）

仅创建2个文档：
1. `ADR-003-repository-simplification.md`：记录Issue #1606决策
2. `architecture-exceptions.md`：记录Desktop三层架构违反

**优点**: 快速实施
**缺点**: 未解决根本问题，未来仍会重复发生

---

## 🚀 实施计划

### 阶段1：核心ADR和例外清单（1-2小时）

1. **创建ADR目录结构**
   ```bash
   mkdir -p docs/architecture/decisions
   touch docs/architecture/decisions/README.md
   touch docs/architecture/decisions/template.md
   ```

2. **编写ADR-003**（Issue #1606决策回顾）
   - 记录Repository层简化的背景和后果
   - 明确架构例外和补救措施

3. **编写ADR-004**（Issue #1608提案）
   - Component设计指南
   - 过度设计重构建议

4. **创建架构例外清单**
   - `docs/architecture/exceptions.md`
   - 记录Desktop三层架构违反

**输出**:
- 4个ADR文件
- 1个架构例外清单
- 更新`docs/architecture/README.md`索引

### 阶段2：架构原则和模式库（2-3小时）

1. **编写架构原则分级**
   - `docs/architecture/principles.md`
   - 明确Mandatory/Recommended/Guideline三级

2. **创建架构模式库**
   ```bash
   mkdir -p docs/architecture/patterns
   touch docs/architecture/patterns/{repository,component,aggregate-root,mvvm}-pattern.md
   ```

3. **编写4个核心模式文档**
   - Repository模式
   - Component设计模式
   - 聚合根模式
   - MVVM模式

**输出**:
- 1个原则文档
- 4个模式文档
- 更新索引

### 阶段3：合规检查清单和历史文档（1小时）

1. **编写合规检查清单**
   - `docs/architecture/compliance-checklist.md`
   - 6大类检查项

2. **创建架构演进历史**
   - `docs/architecture/evolution.md`
   - 记录重大架构变更时间线

**输出**:
- 2个文档
- 完整架构文档体系

---

## 📋 后续技术债务Issue

基于本方案，建议创建以下Issue：

### Issue #1: 恢复Desktop Repository层一致性（可选）

**优先级**: P2 (Medium)
**工作量**: 2-3天

**内容**:
- 创建Read-only `IPrescriptionRepository`接口
- 实现`PrescriptionRepository`（薄封装API调用）
- 更新所有ViewModel依赖

**价值**:
- 恢复Desktop三层架构一致性
- 便于未来添加缓存/离线支持
- 改善单元测试Mock能力

### Issue #2: PrescriptionCommandHandler重构（推荐）⭐

**优先级**: P1 (High)
**工作量**: 1-2天

**内容**:
- **删除**: `PrescriptionCommandHandler`, `PrescriptionEventCoordinator`（5个Component → 2个）
- **保留**: `PrescriptionCalculator`, `PrescriptionValidator`（纯工具类）
- **重构**: `PrescriptionDataManager` → 合并到`PrescriptionViewModel`

**价值**:
- 删除30%冗余代码
- 符合KISS/YAGNI原则
- 提高可维护性

---

## ✅ 决策

### 推荐方案

✅ **方案A：完整架构治理体系**

**理由**:
1. 根本性解决问题，避免未来重复
2. 提升团队架构意识
3. 建立长期可维护的文档体系
4. 投入4-6小时，长期收益显著

### 实施时机

建议在**Issue #1608合并后立即实施**，趁热打铁记录架构问题和解决方案。

---

## 📚 参考资料

- [ADR工具和最佳实践](https://adr.github.io/)
- [C4 Model架构文档](https://c4model.com/)
- [ThoughtWorks技术雷达](https://www.thoughtworks.com/radar)
- 项目现有架构文档：`docs/architecture/README.md`

---

**创建者**: Claude Code
**审核者**: TBD
**批准者**: TBD
