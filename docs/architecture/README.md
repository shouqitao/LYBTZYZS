# 架构规范与设计决策

> 创建日期: 2025-11-29
> 适用范围: LYBTZYZS 项目全栈

## 概述

本目录包含项目的**编码规范**、**设计决策**和**架构约束**文档。这些是开发过程中必须遵循的技术标准。

> **注意**：系统架构说明文档（如何理解各模块工作原理）位于 [explanation/architecture/](../explanation/architecture/)。

## 文档索引

### 编码规范

| 文档 | 描述 | 适用场景 |
|------|------|----------|
| [命名规范](./naming-conventions.md) | 各层代码命名规则 | 新增代码、代码审查 |
| [DTO架构规范](./dto-architecture-specification.md) | DTO分类、命名、继承策略 | DTO设计、API接口 |
| [枚举使用规范](./enum-usage-guidelines.md) | 枚举分类和使用场景 | 枚举设计、状态管理 |
| [Status vs IsDeleted](./status-vs-isdeleted.md) | 启用/禁用与软删除的区分 | 实体设计、状态字段 |

### 设计决策记录 (ADR)

架构决策记录 (Architecture Decision Records) 位于 [decisions/](./decisions/) 目录。

| ADR | 标题 | 状态 |
|-----|------|------|
| [ADR-001](./decisions/ADR-001-user-context-propagation-pattern.md) | 用户上下文传播模式 | 已采纳 |

## 快速参考

### 命名规范要点

```csharp
// 类/方法/属性：PascalCase
public class MedicalCaseService { }
public async Task<MedicalCase> GetByIdAsync(Guid id) { }

// 私有字段：下划线前缀
private readonly IMedicalCaseService _medicalCaseService;

// 异步方法：Async后缀
public async Task CreateMedicalCaseAsync(CreateDto dto);

// 接口：I前缀
public interface IMedicalCaseRepository { }
```

### 状态枚举选择

```
实体类型              应使用的状态
─────────────────────────────────
User/Patient/Herb    CommonStatus (Enabled/Disabled)
MedicalCase          MedicalCaseStatus (Draft/Active/Completed/Cancelled)
删除操作             IsDeleted = true (软删除)
```

### DTO 命名后缀

```
用途           后缀              示例
─────────────────────────────────────
列表视图       ListDto           UserListDto, PatientListDto
详情视图       DetailDto         UserDetailDto, PatientDetailDto
创建/更新      InputDto          UserInputDto, PatientInputDto
仅创建         CreateDto         PrescriptionCreateDto
仅更新         UpdateDto         PrescriptionUpdateDto
业务操作       {Operation}Dto    ChangePasswordDto
```

> 详细规范请参阅 [DTO架构规范](./dto-architecture-specification.md)

## 相关文档

- [系统架构说明](../explanation/architecture/) - 各模块工作原理
- [API 参考](../reference/api/) - 接口文档
- [开发指南](../how-to-guides/development/) - 开发操作指南

## 贡献

更新规范文档时请：
1. 在文档顶部更新日期
2. 保持与现有代码一致
3. 提供正确和错误的代码示例
4. 必要时更新本索引文件
