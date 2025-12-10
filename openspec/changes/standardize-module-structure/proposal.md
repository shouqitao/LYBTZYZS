# Proposal: standardize-module-structure

## Status: draft
## Author: Claude
## Created: 2025-12-10
## Issue: N/A

## Summary

统一8个Desktop业务模块的文件夹结构和命名规范，消除模块间的不一致性，提高代码可维护性和可发现性。

## Motivation

通过对8个客户端业务模块的分析，发现以下问题：

### 1. 文件夹命名不一致

| 模块 | 使用 Components/ | 使用 Services/ | 使用 Repositories/ |
|------|-----------------|----------------|-------------------|
| Auth | - | Services/ | - |
| Consultation | Components/ | - | - |
| Formula | - | - | - |
| Herbs | Components/ | - | Repositories/ |
| MedicalCase | Components/ | - | - |
| Patients | Components/ | Services/ | - |
| Prescriptions | - | Services/ | - |
| Users | Components/ | - | - |

**问题**：相同职责的代码放在不同文件夹中，增加了代码定位的难度。

### 2. 接口文件位置不规范

- 部分接口直接放在Services/或Components/中
- 已通过 `cleanup-desktop-empty-directories` 修复了部分问题
- 但仍有一些模块缺少标准的Interfaces/文件夹

### 3. MedicalCase模块过于复杂

MedicalCase模块包含：
- 6个Components类
- 5个Dialog（每个3文件：.xaml, .xaml.cs, ViewModel.cs）
- 2个Controls
- 多个事件和模型

**建议**：考虑提取通用对话框到Core层。

## Proposed Changes

### Phase 1: 统一文件夹命名规范

**标准结构**：
```
LYBT.Desktop.{Module}/
├── {Module}Module.cs          # Prism模块入口
├── Interfaces/                 # 所有接口
├── Services/                   # 服务实现（替代Components/）
├── ViewModels/                 # MVVM ViewModels
├── Views/                      # XAML Views
├── Models/                     # 领域模型和DTO
├── Events/                     # Prism事件（如需要）
├── Converters/                 # XAML转换器（如需要）
├── Controls/                   # 自定义控件（如需要）
└── Dialogs/                    # 对话框（如需要）
```

**重命名规则**：
- `Components/` → `Services/`（统一使用Services命名）
- 所有接口移至 `Interfaces/`

### Phase 2: 各模块具体调整

#### 2.1 Consultation模块
- 重命名 `Components/` → `Services/`
- 更新namespace

#### 2.2 Herbs模块
- 重命名 `Components/` → `Services/`
- 保留 `Repositories/`（符合规范）
- 更新namespace

#### 2.3 MedicalCase模块
- 重命名 `Components/` → `Services/`
- 更新namespace

#### 2.4 Users模块
- 重命名 `Components/` → `Services/`
- 更新namespace

#### 2.5 Patients模块
- 重命名 `Components/` → `Services/`（与现有Services合并）
- 更新namespace

## Non-Goals

- 不进行业务逻辑重构
- 不提取MedicalCase对话框到Core层（可作为后续OpenSpec）
- 不修改API或数据库层

## Impact Analysis

### 影响范围
- 5个模块需要文件夹重命名
- 约15-20个文件的namespace需要更新
- 对应的using语句需要更新

### 风险评估
- **低风险**：纯结构调整，不涉及业务逻辑
- **编译验证**：可通过 `dotnet build` 验证所有引用正确

### 依赖关系
- 无外部依赖
- Shell层可能有少量引用需要更新

## Alternatives Considered

### 方案A：保持Components命名
- 优点：减少改动
- 缺点：与业界惯例不符，Services更能表达意图

### 方案B：完全统一为Components
- 优点：也能达到统一目的
- 缺点：Services在.NET生态中更常见

**选择方案**：使用Services统一命名，符合.NET社区惯例。

## Testing Strategy

1. 每个模块重构后执行 `dotnet build LYBT.Desktop.sln`
2. 验证所有模块能正常加载
3. 运行现有单元测试

## Rollback Plan

Git revert到重构前的commit。

## References

- [cleanup-desktop-empty-directories](../cleanup-desktop-empty-directories/) - 前置清理工作
- [Prism Library Conventions](https://prismlibrary.com/docs/)
