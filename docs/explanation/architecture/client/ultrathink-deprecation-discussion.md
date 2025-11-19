# UltraThink双层架构废弃需求讨论

**版本**: v1.0
**创建日期**: 2025-11-19
**状态**: 📝 需求讨论
**相关文档**: [Desktop架构完善调查](../../../archive/reports/desktop-architecture-investigation-2025-11-19.md)

---

## 📋 需求概述

**业务目标**: 标记UltraThink双层架构为废弃，更新项目文档以反映实际使用的架构模式。

**核心原则**:
1. ✅ **基于实际代码** - UltraThink的QueryService和BusinessService在代码中已不存在
2. ✅ **文档与代码一致** - 避免文档误导新开发者
3. ✅ **最小化改动** - 仅修改文档，不涉及代码变更

**现状分析**（基于2025-11-19调查）:
- **CLAUDE.md提及**: "UltraThink双层架构 (Module委托层 + QueryService/BusinessService)"
- **代码验证**: 搜索`QueryService|BusinessService`返回空结果
- **实际架构**: `ViewModel → Repository → Refit API`
- **组件化架构**: Epic #1773引入的`DataManager + Validator + CommandHandler`三件套

**目标用户**: LYBTZYZS项目开发团队

**核心任务**:
1. 更新CLAUDE.md移除UltraThink架构引用
2. 新增ADR文档说明UltraThink废弃原因和迁移路径
3. 更新Desktop层架构文档说明实际架构

---

## ✨ 功能性需求

### FR-001: 更新CLAUDE.md文档（必需）

**描述**: 移除CLAUDE.md中对UltraThink双层架构的引用，更新为实际使用的架构

**User Story**:
```
作为 开发者
我想要 CLAUDE.md准确描述实际架构
以便 正确理解Desktop层的架构设计
```

**当前问题**:
```markdown
# CLAUDE.md当前内容（错误）
技术栈:
- 前端: UltraThink双层架构 (Module委托层 + QueryService/BusinessService)

架构特点:
- 前端: UltraThink双层架构 (Module委托层 + QueryService/BusinessService)
- 后端: 传统三层架构 (Repository + Service + Controller)
```

**目标内容**:
```markdown
# CLAUDE.md修订后（正确）
技术栈:
- 前端: WPF (.NET 8), Prism.DryIoc 9.0, Refit
- 后端: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0

架构特点:
- 前端: MVVM + 组件化架构 (ViewModel → Repository → Refit API)
  - Epic #1773组件化: DataManager + Validator + CommandHandler三件套
- 后端: 传统三层架构 (Controller → Service → Repository)
- 统一接口: IService接口，无重复IModule

注: UltraThink双层架构（QueryService/BusinessService）已废弃，
   详见 docs/adr/ADR-015-deprecate-ultrathink-architecture.md
```

**验收标准**:
- [x] CLAUDE.md移除所有UltraThink架构引用
- [x] 更新为实际架构描述（ViewModel → Repository → Refit API）
- [x] 添加Epic #1773组件化架构说明
- [x] 添加废弃说明指向ADR-015文档

**预估工作量**: 15分钟

---

### FR-002: 新增ADR-015文档（必需）

**描述**: 创建ADR-015文档说明UltraThink架构废弃原因、实际架构、迁移路径

**User Story**:
```
作为 开发者
我想要 了解UltraThink架构废弃的历史背景
以便 理解当前架构的设计决策
```

**文档结构**:
```markdown
# ADR-015: 废弃UltraThink双层架构

## 状态
已接受 - 2025-11-19

## 背景
UltraThink双层架构（Module委托层 + QueryService/BusinessService）曾在项目早期规划中提出，
但在实际开发中从未实施。代码中不存在QueryService和BusinessService实现。

## 决策
正式废弃UltraThink双层架构，采用以下实际架构：

### Desktop层实际架构
ViewModel → Repository → Refit API

### Epic #1773组件化架构（2025年引入）
ViewModel → CommandHandler → DataManager/Validator → Repository → Refit API

## 后果
### 正面
- 文档与代码一致，避免误导
- 明确实际架构模式
- 为Epic #1773组件化架构提供官方背书

### 负面
- 无（UltraThink从未实施）

## 相关文档
- Epic #1773: Component-Based架构重构
- docs/explanation/architecture/client/README.md
```

**验收标准**:
- [x] 创建`docs/adr/ADR-015-deprecate-ultrathink-architecture.md`
- [x] 说明废弃原因（从未实施）
- [x] 记录实际架构模式
- [x] 说明Epic #1773组件化架构
- [x] 添加相关文档链接

**预估工作量**: 20分钟

---

### FR-003: 更新Desktop架构文档（必需）

**描述**: 更新`docs/explanation/architecture/client/README.md`准确描述Desktop层架构

**User Story**:
```
作为 开发者
我想要 准确的Desktop层架构文档
以便 开发新功能时遵循正确的架构模式
```

**目标内容**:
```markdown
# Desktop层架构说明

## 核心架构模式

### 标准架构（聚合根模块）
ViewModel → Repository → Refit API

适用模块：
- Users（用户管理）
- Patients（患者管理）
- MedicalCase（医案管理 - 聚合根）
- Herbs（药材管理）
- Formula（方剂管理）

### 认证服务架构（无Repository）
ViewModel → Refit API

适用模块：
- Auth（JWT认证 - 无状态服务，无需Repository）

### DDD聚合根架构（从属实体无Repository）
ViewModel → 聚合根Repository → Refit API

适用模块：
- Consultation（诊断 - MedicalCase的从属实体，通过MedicalCaseRepository操作）
- Prescriptions（处方 - MedicalCase的从属实体，通过MedicalCaseRepository操作）

设计依据：Issue #1606 DDD聚合根模式

## Epic #1773组件化架构（可选）

### 组件化三件套
ViewModel → CommandHandler → DataManager/Validator → Repository → Refit API

组件职责：
- **CommandHandler** - 业务命令协调（Save/Delete/Navigate）
- **DataManager** - 数据管理封装（Repository/API调用）
- **Validator** - 业务规则验证

已实施模块：
- Consultation, Formula, MedicalCase, Patients, Prescriptions, Users

设计依据：Epic #1773 Component-Based架构

## 废弃架构

### ❌ UltraThink双层架构（已废弃）
Module委托层 + QueryService/BusinessService

废弃原因：从未在代码中实施
详见：docs/adr/ADR-015-deprecate-ultrathink-architecture.md
```

**验收标准**:
- [x] 更新`docs/explanation/architecture/client/README.md`
- [x] 准确描述三种架构模式（标准/认证/DDD）
- [x] 说明Epic #1773组件化架构
- [x] 标注UltraThink为废弃架构
- [x] 添加设计依据（Issue #1606, Epic #1773）

**预估工作量**: 30分钟

---

## 🔒 非功能性需求

### NFR-001: 文档准确性
- 所有架构描述基于实际代码验证
- 术语使用一致（ViewModel、Repository、Refit API）
- 避免误导性描述

### NFR-002: 可追溯性
- ADR文档包含决策时间和背景
- 引用具体的Issue和Epic编号
- 提供相关文档链接

### NFR-003: 可维护性
- 使用Markdown格式便于版本控制
- 清晰的章节结构
- 代码示例使用代码块格式

---

## 📐 业务规则

### BR-001: 文档更新原则
- **规则**: 所有架构文档必须与实际代码一致
- **理由**: 避免误导新开发者
- **实现**: 更新前先验证代码，搜索关键类名确认存在性

### BR-002: ADR文档规范
- **规则**: ADR文档必须包含状态、背景、决策、后果四部分
- **理由**: 遵循ADR文档标准格式
- **实现**: 使用ADR模板创建ADR-015

### BR-003: 废弃标记规范
- **规则**: 废弃的架构必须明确标注"❌"和废弃原因
- **理由**: 清晰区分当前架构和历史架构
- **实现**: 在文档中使用"❌ 废弃"标记

---

## 🗃️ 数据模型

**无数据模型变更** - 本次仅涉及文档更新，不涉及代码和数据库

---

## 🏗️ 架构约束

### 技术栈（无变更）
- ✅ **前端框架**: WPF + Prism.DryIoc 9.0
- ✅ **MVVM基类**: UnifiedViewModelBase, UnifiedListViewModelBase<T>
- ✅ **Repository基类**: RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>
- ✅ **HTTP客户端**: Refit
- ✅ **组件化**: DataManager + Validator + CommandHandler（Epic #1773）

### 实际架构模式（基于代码验证）

**1. 标准架构（聚合根）**
```
ViewModel (UnifiedListViewModelBase<T>)
    ↓
Repository (RepositoryBase<...>)
    ↓
Refit API (IXxxApi)
```
适用：Users, Patients, MedicalCase, Herbs, Formula

**2. 认证服务架构（无Repository）**
```
ViewModel (UnifiedViewModelBase)
    ↓
Refit API (IAuthenticationApi)
```
适用：Auth（JWT认证 - 无状态RPC）

**3. DDD聚合根架构（从属实体）**
```
ViewModel (UnifiedViewModelBase)
    ↓
聚合根Repository (MedicalCaseRepository)
    ↓
Refit API (IMedicalCaseApi)
```
适用：Consultation, Prescriptions（Issue #1606 DDD模式）

**4. Epic #1773组件化架构（可选）**
```
ViewModel
    ↓
CommandHandler（业务协调）
    ↓ ↓ ↓
DataManager + Validator + Repository
    ↓
Refit API
```
已实施：6个模块（Consultation, Formula, MedicalCase, Patients, Prescriptions, Users）

### 废弃架构（已验证不存在）
```
❌ UltraThink双层架构（已废弃）
   Module委托层 + QueryService/BusinessService
   → 代码搜索结果：空（QueryService|BusinessService不存在）
```

---

## 📋 任务清单

### Task 1: 更新CLAUDE.md（15分钟）
- [ ] 移除UltraThink架构引用
- [ ] 更新为实际架构描述
- [ ] 添加Epic #1773组件化架构说明
- [ ] 添加废弃说明指向ADR-015

### Task 2: 创建ADR-015文档（20分钟）
- [ ] 创建`docs/adr/ADR-015-deprecate-ultrathink-architecture.md`
- [ ] 填写状态、背景、决策、后果
- [ ] 说明实际架构模式
- [ ] 添加相关文档链接

### Task 3: 更新Desktop架构文档（30分钟）
- [ ] 更新`docs/explanation/architecture/client/README.md`
- [ ] 描述三种架构模式（标准/认证/DDD）
- [ ] 说明Epic #1773组件化架构
- [ ] 标注UltraThink为废弃
- [ ] 添加设计依据

### Task 4: 验证文档准确性（10分钟）
- [ ] 交叉验证三个文档一致性
- [ ] 确认所有链接有效
- [ ] 检查术语使用一致性

---

## ✅ 验收标准

### AC-001: CLAUDE.md准确性
- [x] 移除所有UltraThink架构引用
- [x] 更新为实际架构（ViewModel → Repository → Refit API）
- [x] 添加Epic #1773组件化架构说明
- [x] 添加废弃说明指向ADR-015

### AC-002: ADR-015完整性
- [x] 文档存在于`docs/adr/ADR-015-deprecate-ultrathink-architecture.md`
- [x] 包含状态、背景、决策、后果四部分
- [x] 说明废弃原因（从未实施）
- [x] 记录实际架构模式
- [x] 引用Epic #1773和Issue #1606

### AC-003: Desktop架构文档准确性
- [x] 准确描述三种架构模式
- [x] 说明Epic #1773组件化架构
- [x] 标注UltraThink为废弃
- [x] 提供设计依据和相关文档链接

### AC-004: 文档一致性
- [x] 三个文档对架构的描述一致
- [x] 术语使用统一（ViewModel、Repository、Refit API）
- [x] 所有链接有效

---

## ⚠️ 风险与缓解

### R-001: 误删有用信息（低风险）
**描述**: 更新CLAUDE.md时可能误删其他有用信息

**缓解措施**:
1. 仅修改架构相关章节
2. Git提交前仔细review差异
3. 保留其他所有内容不变

**回滚方案**: Git revert即可恢复

---

### R-002: ADR文档不完整（低风险）
**描述**: ADR-015文档遗漏重要信息

**缓解措施**:
1. 使用ADR标准模板
2. 引用Epic #1773和Issue #1606
3. 由用户review确认

**补救方案**: 后续补充ADR文档内容

---

## ❓ 开放问题

### Q1: 是否需要创建Epic/Issue？
**问题**: 这是一个纯文档更新任务，是否需要创建GitHub Epic和Issue？

**选项**:
- A. 创建Epic #XXXX（正式流程）
- B. 直接修改文档，commit即可（简化流程）

**建议**: 选B（直接修改文档）
- 理由：工作量仅1小时，无代码变更，无需Epic管理
- 优势：减少流程开销，快速完成
- commit信息：`docs: 废弃UltraThink架构，更新Desktop层架构文档`

---

### Q2: 是否需要通知所有开发者？
**问题**: 文档更新后是否需要主动通知团队成员？

**选项**:
- A. 发送团队通知邮件/消息
- B. 仅在commit message中说明

**建议**: 选A（发送通知）
- 理由：架构文档更新属于重要变更，避免误解
- 内容：说明UltraThink从未实施，当前实际架构是什么

---

## 📎 参考资料

- [Desktop架构完善调查报告](../../../archive/reports/desktop-architecture-investigation-2025-11-19.md) - 架构差异调查完整过程
- [Epic #1773: Component-Based架构](https://github.com/shouqitao/LYBTZYZS/issues/1773) - 组件化架构设计
- [Issue #1606: DDD聚合根重构](https://github.com/shouqitao/LYBTZYZS/issues/1606) - MedicalCase聚合根设计
- [ADR模板](../adr/template.md) - ADR文档标准格式

---

## 📊 统计数据

### 工作量估算
| 任务 | 工作量 | 优先级 |
|------|--------|--------|
| Task 1: 更新CLAUDE.md | 15分钟 | P0 |
| Task 2: 创建ADR-015 | 20分钟 | P0 |
| Task 3: 更新Desktop架构文档 | 30分钟 | P0 |
| Task 4: 验证文档准确性 | 10分钟 | P0 |
| **总计** | **75分钟** | **P0** |

### 影响范围
- **修改文件**: 3个（CLAUDE.md, ADR-015, client/README.md）
- **新增文件**: 1个（ADR-015）
- **代码变更**: 0行
- **文档变更**: 约200行

---

**下一步**:
1. ✅ 用户确认需求文档
2. 📝 直接执行文档更新（无需Epic/Issue）
3. 💾 Git commit并push
4. 📢 通知团队成员（可选）

---

**版本历史**:
- v1.0 (2025-11-19): 初始版本，基于Desktop架构完善调查结果
