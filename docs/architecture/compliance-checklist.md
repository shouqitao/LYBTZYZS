# 架构合规性检查清单

**创建日期**: 2025-10-25
**维护者**: 项目架构团队
**目的**: 提供架构合规性检查的标准化清单，确保代码符合架构原则和设计规范

---

## 📋 清单概述

本清单用于架构合规性检查，分为4个检查阶段：
1. **需求分析阶段**：验证需求是否符合MVP和Constitution约束
2. **设计文档阶段**：验证设计是否符合三层架构和DDD原则
3. **代码实现阶段**：验证代码是否符合编码规范和模式标准
4. **代码审查阶段**：验证提交是否符合质量标准

---

## ✅ 需求分析阶段检查

### 1. Constitution合规性（⭐⭐⭐ 强制）

- [ ] **技术黑名单检查**：需求是否要求使用Redis/CQRS/Docker/GraphQL等禁用技术？
- [ ] **MVP原则检查**：需求是否符合"够用即好"原则，避免过度设计？
- [ ] **技术栈检查**：需求的技术选型是否符合当前.NET 8 + WPF + SQL Server技术栈？

### 2. 业务规则检查（⭐⭐ 推荐）

- [ ] **聚合根边界**：需求是否明确聚合根边界（如MedicalCase）？
- [ ] **数据一致性**：需求是否考虑事务边界和数据一致性？
- [ ] **业务约束**：需求是否明确业务约束（如暂存医案不能删除）？

### 3. 文档检查（⭐⭐⭐ 强制）

- [ ] **文档已阅读**：需求分析前是否已阅读`docs/index.md` + `docs/business-rules.md` + 相关架构文档？
- [ ] **需求文档创建**：是否在`docs/requirements/`目录创建了需求文档？
- [ ] **Issue创建**：是否创建了对应的GitHub Issue？

**工具**：lybtzyzs-requirements-arch-guard Skill

---

## ✅ 设计文档阶段检查

### 1. 三层架构检查（⭐⭐⭐ 强制）

#### Server端

- [ ] **依赖方向**：Application层是否只依赖Domain层？（不依赖Presentation）
- [ ] **接口定义**：Repository接口是否在Domain层定义？
- [ ] **DTO使用**：Controller是否使用DTO而非Entity直接返回？

#### Client端

- [ ] **MVVM模式**：View是否只负责UI呈现，业务逻辑在ViewModel？
- [ ] **数据绑定**：View是否通过数据绑定而非Code-Behind访问数据？
- [ ] **Command模式**：用户操作是否通过Command而非事件处理？

### 2. DDD聚合根检查（⭐⭐⭐ 强制）

- [ ] **聚合根识别**：设计是否明确了聚合根（如MedicalCase）？
- [ ] **子实体操作**：设计是否通过聚合根操作子实体（如Prescription）？
- [ ] **Repository设计**：Repository方法是否包含聚合根子实体操作（如`CreatePrescriptionAsync`）？

### 3. Component设计检查（⭐⭐ 推荐）

- [ ] **跨模块共享**：Component是否被2个及以上模块使用？
- [ ] **非薄封装**：Component是否包含真实业务逻辑（不只是1-2行代码）？
- [ ] **职责清晰**：Component职责是否不与ViewModel重叠？

### 4. 设计文档检查（⭐⭐⭐ 强制）

- [ ] **架构文档已阅读**：设计前是否已阅读对应模块的架构指南？
- [ ] **设计文档创建**：是否在`docs/design/`目录创建了设计文档？
- [ ] **ADR创建**：如涉及架构调整，是否创建了ADR？
- [ ] **例外记录**：如违反架构原则，是否在例外清单中记录？

**工具**：lybtzyzs-design-arch-validator Skill

---

## ✅ 代码实现阶段检查

### 1. 编码规范检查（⭐⭐⭐ 强制）

- [ ] **编译通过**：0 errors, 0 warnings
- [ ] **命名规范**：类型用PascalCase，私有字段用\_camelCase，常量用UPPER\_SNAKE\_CASE
- [ ] **UTF-8 BOM**：所有文本文件使用UTF-8 with BOM编码
- [ ] **中文注释**：代码注释使用中文
- [ ] **Emoji禁用**：代码中禁用Emoji（文档允许）

### 2. 依赖注入检查（⭐⭐⭐ 强制）

- [ ] **构造函数注入**：是否仅使用构造函数注入？
- [ ] **禁止ServiceLocator**：代码中是否不存在`ServiceLocator.Current.GetInstance`？
- [ ] **禁止Container.Resolve**：代码中是否不存在`App.Container.Resolve`？

### 3. 异步模式检查（⭐⭐ 推荐）

- [ ] **async/await**：I/O操作是否使用async/await？
- [ ] **方法命名**：异步方法是否以`Async`结尾？
- [ ] **CancellationToken**：长时间操作是否支持CancellationToken？

### 4. Repository/Service检查（⭐⭐⭐ 强制）

#### Server端

- [ ] **Repository实现**：Repository是否只负责数据访问，不包含业务逻辑？
- [ ] **聚合根操作**：子实体的创建/更新/删除是否通过聚合根Repository？
- [ ] **Include预加载**：Repository是否使用Include避免N+1查询？

#### Client端（⚠️ 例外）

- [ ] **Read操作**：Read操作是否直接使用API（`IPrescriptionApi`）？
- [ ] **Write操作**：Write操作是否通过聚合根Repository（`IMedicalCaseRepository`）？
- [ ] **例外记录**：如违反三层架构，是否在ADR-003和例外清单中记录？

### 5. MVVM模式检查（⭐⭐⭐ 强制）

- [ ] **ViewModel独立**：ViewModel是否不依赖View类型？
- [ ] **数据绑定**：属性是否实现`INotifyPropertyChanged`（继承`BindableBase`）？
- [ ] **Command实现**：用户操作是否通过`DelegateCommand`或`AsyncDelegateCommand`？
- [ ] **Code-Behind限制**：Code-Behind是否只包含UI逻辑（动画、焦点控制）？

**工具**：lybtzyzs-mvp-compliance Skill、lybtzyzs-arch-compliance Skill

---

## ✅ 代码审查阶段检查

### 1. 测试覆盖检查（⭐⭐ 推荐）

- [ ] **单元测试**：核心业务逻辑是否有单元测试？
- [ ] **AAA模式**：测试是否遵循Arrange-Act-Assert模式？
- [ ] **Mock配置**：测试是否使用NSubstitute配置Mock对象？

### 2. 文档同步检查（⭐⭐⭐ 强制）

- [ ] **架构文档更新**：架构调整是否同步更新了架构文档？
- [ ] **ADR创建**：重大架构决策是否创建了ADR？
- [ ] **API文档更新**：API变更是否同步更新了Swagger和API文档？
- [ ] **模块README更新**：模块重构是否同步更新了README？

### 3. Git提交检查（⭐⭐⭐ 强制）

- [ ] **Issue关联**：Commit Message是否包含`Fixes #1234`或`Related to Epic #1234`？
- [ ] **Commit格式**：是否遵循`<type>(<scope>): <subject>`格式？
- [ ] **验证说明**：Commit Message是否包含"验证：功能已正常工作"？
- [ ] **Claude Code标记**：是否包含Claude Code Co-Authored-By标记？

### 4. 运行时验证检查（⭐⭐⭐ 强制）

- [ ] **应用启动**：Client + Server是否能正常启动？
- [ ] **功能验证**：是否执行了真实操作场景验证？
- [ ] **数据库验证**：是否验证了数据库状态（必要时）？
- [ ] **用户视角**：是否从用户视角确认功能完整可用？

**工具**：lybtzyzs-code-review Skill、lybtzyzs-doc-sync Skill

---

## 📊 检查清单使用场景

### 场景1：新功能开发

```
需求分析阶段检查
  ↓
设计文档阶段检查
  ↓
代码实现阶段检查
  ↓
代码审查阶段检查
  ↓
运行时验证 + 提交
```

### 场景2：Bug修复

```
跳过需求分析阶段（已有Issue）
  ↓
代码实现阶段检查（部分）
  ↓
代码审查阶段检查（部分）
  ↓
运行时验证 + 提交
```

### 场景3：架构重构

```
需求分析阶段检查（Constitution合规性）
  ↓
设计文档阶段检查（三层架构 + DDD）
  ↓
ADR创建 + 例外记录
  ↓
代码实现阶段检查
  ↓
代码审查阶段检查（文档同步重点）
  ↓
运行时验证 + 提交
```

---

## 🔗 相关资源

- **架构原则**: [principles.md](./principles.md) - 35条架构原则三级分类
- **ADR索引**: [decisions/README.md](./decisions/README.md) - 所有架构决策记录
- **架构例外清单**: [exceptions.md](./exceptions.md) - 已批准的例外
- **设计模式**: [patterns/](./patterns/) - Repository/Component/Aggregate Root/MVVM
- **业务规则**: [../business-rules.md](../business-rules.md) - 14条核心业务规则
- **Constitution**: `.spec-workflow/steering/constitution.md` - 项目强制性原则

---

## 📅 更新日志

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建 | Claude Code |

---

**最后更新**: 2025-10-25
**维护者**: 项目架构团队
