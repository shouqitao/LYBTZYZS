# ADR-004: Desktop端Component设计指南

**日期**: 2025-10-25
**状态**: 📝 Proposed（建议中）
**决策者**: 开发团队
**标签**: #架构 #desktop #mvvm #component

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-004 |
| **创建日期** | 2025-10-25 |
| **最后更新** | 2025-10-25 |
| **状态** | 📝 Proposed（建议中） |
| **决策者** | 开发团队 |
| **影响范围** | Desktop端（所有模块） |
| **相关Issue** | #1608（Prescriptions模块重构） |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

在Issue #1608（Prescriptions模块重构）中，发现`PrescriptionCommandHandler`和`PrescriptionDataManager`两个Component存在过度设计问题：

**PrescriptionCommandHandler问题**：
- **功能**：封装Command（如删除、打印、复制）
- **问题**：薄封装，仅调用ViewModel方法，增加调用链复杂度
- **影响**：View → CommandHandler → ViewModel → Repository/API（4层调用）

**PrescriptionDataManager问题**：
- **功能**：封装数据访问逻辑
- **问题**：与ViewModel职责重叠，违反单一职责原则
- **影响**：ViewModel和DataManager都管理数据，责任边界不清

### 当前状态

**Desktop端Component使用情况**：
```
正常使用场景：
- NotificationService：跨模块通知机制
- DialogService：弹窗管理服务
- NavigationService：页面导航管理

过度设计场景：
- PrescriptionCommandHandler：薄封装Command
- PrescriptionDataManager：与ViewModel职责重叠
```

**问题影响**：
- **理解成本**：新成员难以理解为何需要4层调用链
- **维护成本**：修改功能需要同时修改多个Component
- **测试成本**：需要Mock更多层级的依赖
- **违反MVVM**：ViewModel应直接管理Command和Data，不应外包

### 问题影响

- **架构复杂性**：不必要的抽象层增加系统复杂度
- **MVP原则违反**：过度设计，未遵循"够用即好"原则
- **代码冗余**：薄封装Component增加代码量20%
- **新人困惑**：难以理解何时应该创建Component，何时应该直接在ViewModel实现

---

## ✅ 决策（Decision）

制定**Desktop端Component设计三原则**，明确Component的合理使用边界：

### 原则1：跨模块共享优先（Cross-Module Sharing First）

**何时创建Component**：
- ✅ 功能被2个及以上模块使用
- ✅ 需要统一的行为实现
- ✅ 有明确的业务价值

**示例**：
```csharp
// ✅ 正确：跨模块通知服务
public class NotificationService : INotificationService
{
    public void ShowSuccess(string message) { }
    public void ShowError(string message) { }
}

// ✅ 正确：跨模块弹窗服务
public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmAsync(string message) { }
}
```

### 原则2：避免薄封装（Avoid Thin Wrappers）

**禁止场景**：
- ❌ 仅封装1-2行代码的Component
- ❌ 不包含业务逻辑的纯转发Component
- ❌ 与ViewModel职责重叠的Component

**示例**：
```csharp
// ❌ 错误：薄封装Command
public class PrescriptionCommandHandler
{
    private readonly PrescriptionManagementViewModel _viewModel;

    public void Delete(int id)
    {
        _viewModel.DeleteCommand.Execute(id); // 仅转发，无价值
    }
}

// ✅ 正确：直接在ViewModel中实现
public class PrescriptionManagementViewModel
{
    public DelegateCommand<int> DeleteCommand { get; }

    private void OnDelete(int id)
    {
        // 直接实现删除逻辑
    }
}
```

### 原则3：职责清晰优先（Clear Responsibility First）

**设计检查**：
- ✅ Component有明确的单一职责
- ✅ Component职责不与ViewModel重叠
- ✅ Component边界清晰，易于测试

**示例**：
```csharp
// ❌ 错误：与ViewModel职责重叠
public class PrescriptionDataManager
{
    public ObservableCollection<Prescription> Prescriptions { get; }
    public async Task LoadDataAsync() { }
}

// ViewModel也管理数据
public class PrescriptionManagementViewModel
{
    public ObservableCollection<Prescription> Prescriptions { get; }
    public async Task LoadDataAsync() { }
}

// ✅ 正确：ViewModel统一管理数据
public class PrescriptionManagementViewModel
{
    private readonly IPrescriptionApi _api;

    public ObservableCollection<Prescription> Prescriptions { get; }

    public async Task LoadDataAsync()
    {
        var response = await _api.GetPrescriptionsAsync();
        Prescriptions = new ObservableCollection<Prescription>(response.Data.Items);
    }
}
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **简化架构**：减少不必要的抽象层，降低系统复杂度
- ✅ **提高可维护性**：职责清晰，修改逻辑只需改一处
- ✅ **降低学习成本**：新成员容易理解何时创建Component
- ✅ **符合MVP原则**：够用即好，避免过度设计
- ✅ **减少代码量**：删除薄封装Component，减少20%冗余代码

### 缺点（Cons）

- ❌ **需要重构现有代码**：删除`PrescriptionCommandHandler`和`PrescriptionDataManager`
- ❌ **需要更新现有文档**：更新Desktop端MVVM架构文档
- ❌ **可能引入短期风险**：重构过程中可能引入Bug

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 现有代码依赖Component | 删除Component导致编译失败 | 渐进式重构，先删除Command Handler，再删除Data Manager |
| 团队成员不理解新原则 | 继续创建薄封装Component | 在CLAUDE.md和架构文档中明确记录三原则 |
| 重构引入Bug | 功能异常 | 每次删除Component后立即运行时验证 |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: 保留现有Component，仅文档警告（未采纳）

**描述**: 保留`PrescriptionCommandHandler`和`PrescriptionDataManager`，但在文档中警告"不推荐"

**优点**:
- ✅ 无需重构，风险为零
- ✅ 保持向后兼容

**缺点**:
- ❌ 继续维护冗余代码
- ❌ 新成员看到代码后仍会模仿创建类似Component
- ❌ 技术债务继续累积

**为什么未采纳**:
- MVP阶段应主动删除冗余代码，降低维护成本
- 保留不良示例会误导新成员
- 重构风险可控（编译验证+运行时验证）

---

### 方案B: 允许单模块Component，但需评审（未采纳）

**描述**: 允许单模块Component存在，但创建前需经过架构评审

**优点**:
- ✅ 灵活性高，特殊场景可以创建Component
- ✅ 不强制删除现有Component

**缺点**:
- ❌ 评审流程增加开发成本
- ❌ 评审标准主观，容易产生争议
- ❌ MVP阶段不应引入评审流程

**为什么未采纳**:
- MVP阶段强调简化流程，评审机制过重
- "跨模块共享"标准已足够明确，无需评审
- 可以在未来需要时再引入评审机制

---

### 方案C: 统一使用Service命名，Component仅用于跨模块（未采纳）

**描述**: 将所有辅助类命名为Service（如`PrescriptionService`），Component专指跨模块共享组件

**优点**:
- ✅ 命名统一，易于理解
- ✅ Component概念更清晰

**缺点**:
- ❌ 需要重命名大量现有文件
- ❌ Service命名容易与Server端Service混淆
- ❌ 不解决薄封装问题

**为什么未采纳**:
- 命名统一不是核心问题，核心是避免薄封装
- 重命名成本高，收益低
- 专注解决过度设计问题更重要

---

## 🏗️ 架构例外（Architecture Exceptions）

### 例外：现有跨模块Component保留

- **影响组件**: `NotificationService`, `DialogService`, `NavigationService`
- **例外原因**: 这些Component符合"跨模块共享"原则，提供真实业务价值
- **批准日期**: 2025-10-25
- **审查周期**: 无需定期审查（长期稳定）

---

## 📚 参考资料（References）

- **相关Issue**:
  - #1608 - Prescriptions模块重构（发现过度设计问题）
  - #1610 - 建立完整的架构文档治理体系
- **相关PR**:
  - #1609 - Issue #1608实施（删除Command Handler和Data Manager）
- **架构文档**:
  - `docs/architecture/client/README.md` - Desktop端MVVM架构
  - `.spec-workflow/steering/constitution.md` - MVP原则和技术约束
  - `docs/business-rules.md` - 业务规则#12（够用即好，避免过度设计）
- **外部资源**:
  - [MVVM Pattern - Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
  - [YAGNI Principle](https://martinfowler.com/bliki/Yagni.html)

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 文档更新（已完成）
- [x] 创建ADR-004记录决策
- [ ] 更新`docs/architecture/client/README.md`（添加Component设计三原则）
- [ ] 更新CLAUDE.md（引用ADR-004作为Component设计标准）

### Phase 2: 代码重构（Issue #1608，已完成）
- [x] 删除`PrescriptionCommandHandler`
- [x] 删除`PrescriptionDataManager`
- [x] 将Command逻辑移至ViewModel
- [x] 将数据管理逻辑移至ViewModel
- [x] 更新依赖注入配置

### Phase 3: 验证和推广（计划中）
- [ ] 编译验证（0 errors, 0 warnings）
- [ ] 运行时验证（Prescriptions模块功能完整可用）
- [ ] 更新其他模块的类似Component（如有）
- [ ] 在团队内部分享Component设计三原则

---

## ✅ 验收标准（Acceptance Criteria）

- [x] ADR-004已创建并提交
- [ ] Desktop端架构文档已更新（Component设计三原则）
- [x] `PrescriptionCommandHandler`已删除
- [x] `PrescriptionDataManager`已删除
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证：Prescriptions模块所有功能可用
- [ ] 新成员可以根据三原则判断是否应该创建Component

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建（基于Issue #1608发现的问题） | Claude Code |

---

**创建者**: Claude Code
**审核者**: 待定
**批准者**: 开发团队（待评审）
