# ADR-015: 废弃UltraThink双层架构

**日期**: 2025-11-19
**状态**: ✅ 已接受
**决策者**: LYBTZYZS开发团队

---

## 背景

UltraThink双层架构（Module委托层 + QueryService/BusinessService）曾在LYBTZYZS项目早期规划中提出，计划用于Desktop层的数据访问和业务逻辑分层。

然而，通过2025-11-19的代码审查发现：
- **代码搜索**: 搜索`QueryService|BusinessService`返回空结果
- **实际实现**: 所有Desktop模块均采用`ViewModel → Repository → Refit API`架构
- **历史状态**: UltraThink架构从未在代码中实施

这导致文档与代码不一致，可能误导新开发者。

---

## 决策

正式**废弃UltraThink双层架构**，明确LYBTZYZS项目Desktop层采用以下实际架构：

### 1. 标准架构（聚合根模块）

```
ViewModel (UnifiedListViewModelBase<T>)
    ↓
Repository (RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>)
    ↓
Refit API (IXxxApi)
```

**适用模块**: Users, Patients, MedicalCase, Herbs, Formula

**特点**:
- 使用统一的ViewModel基类和Repository基类
- 通过Refit进行HTTP API调用
- 遵循MVVM模式，清晰的职责分离

---

### 2. 认证服务架构（无Repository）

```
ViewModel (UnifiedViewModelBase)
    ↓
Refit API (IAuthenticationApi)
```

**适用模块**: Auth

**设计理由**:
- JWT认证是无状态RPC服务，不涉及数据实体持久化
- 无需Repository层，直接调用API更简洁
- 符合认证服务的特性（验证而非数据管理）

---

### 3. DDD聚合根架构（从属实体）

```
ViewModel (UnifiedViewModelBase)
    ↓
聚合根Repository (MedicalCaseRepository)
    ↓
Refit API (IMedicalCaseApi)
```

**适用模块**: Consultation, Prescriptions

**设计理由**:
- 基于Issue #1606的DDD聚合根模式
- MedicalCase是聚合根，Consultation和Prescription是从属实体
- 所有写操作通过MedicalCaseRepository统一管理，保证聚合一致性
- 读操作可直接使用各自的API接口

**代码证据**:
```csharp
// ConsultationModule.cs Line 8
/// Issue #1606 Phase 3: 移除ConsultationRepository/ApiClient（已迁移至MedicalCaseRepository聚合根）

// PrescriptionsModule.cs Line 13
/// Issue #1606 Phase 3: 移除IPrescriptionRepository（已迁移至MedicalCaseRepository聚合根）
```

---

### 4. Epic #1773组件化架构（可选增强）

```
ViewModel
    ↓
CommandHandler（业务命令协调）
    ↓ ↓ ↓
DataManager + Validator + Repository
    ↓
Refit API
```

**组件职责**:
- **CommandHandler** - 业务命令协调者（Save/Delete/Navigate）
- **DataManager** - 数据管理封装（Repository/API调用）
- **Validator** - 业务规则验证

**已实施模块**: Consultation, Formula, MedicalCase, Patients, Prescriptions, Users（6个模块）

**设计理由**:
- 将业务逻辑从ViewModel抽离，提高可测试性
- 使用命令模式统一业务操作接口
- 支持复杂业务场景的扩展

**代码证据**:
```csharp
// ICommandHandler.cs
/// <summary>
/// 命令处理器接口 - 组件化MVVM架构核心接口
/// Issue #1776 Task 3: 组件化基础设施搭建
/// </summary>
```

---

## 后果

### 正面影响

1. **文档与代码一致** - 移除虚假架构描述，避免误导新开发者
2. **架构清晰明确** - 三种实际架构模式（标准/认证/DDD）有明确适用场景
3. **有意设计获得认可** - Auth无Repository、DDD聚合根等设计得到官方文档支持
4. **组件化架构背书** - Epic #1773的组件化模式成为官方推荐架构

### 负面影响

**无** - UltraThink架构从未实施，废弃无任何影响

### 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| 误解UltraThink工作流程名称 | 低 | 低 | CLAUDE.md保留"UltraThink四阶段执行流程"（工作方法论，非架构） |
| 文档更新遗漏 | 低 | 中 | 更新三个文档（CLAUDE.md、ADR-015、client/README.md）并交叉验证 |

---

## 相关文档

- [Desktop架构完善调查报告](../archive/reports/desktop-architecture-investigation-2025-11-19.md) - 架构差异调查详细过程
- [Epic #1773: Component-Based架构](https://github.com/shouqitao/LYBTZYZS/issues/1773) - 组件化架构设计
- [Issue #1606: DDD聚合根重构](https://github.com/shouqitao/LYBTZYZS/issues/1606) - MedicalCase聚合根设计
- [Desktop架构文档](../explanation/architecture/client/README.md) - Desktop层完整架构说明
- [CLAUDE.md](../../CLAUDE.md) - 项目工作流程和配置

---

## 注释

### 为什么不同模块架构不一致？

这不是架构债务，而是**有意设计**：

1. **Auth无Repository** - 认证是RPC服务，非数据实体管理
2. **Consultation/Prescriptions无Repository** - DDD聚合根模式，从属实体通过MedicalCaseRepository操作
3. **CommandHandler可选** - Epic #1773的组件化增强，非强制要求

### 如何选择架构模式？

| 场景 | 推荐架构 | 原因 |
|------|---------|------|
| 新增聚合根模块（如Patient） | 标准架构 | 简单直接，职责清晰 |
| 新增认证/授权功能 | 无Repository架构 | RPC服务特性 |
| 新增从属实体（如处方项） | DDD聚合根架构 | 保证聚合一致性 |
| 复杂业务逻辑模块 | 组件化架构 | 提高可测试性和扩展性 |

---

**版本历史**:
- v1.0 (2025-11-19): 初始版本，基于Desktop架构完善调查结果
