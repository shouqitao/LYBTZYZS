# Desktop Layer Architecture Issues Analysis

**分析时间**: 2025-12-30
**分析工具**: Claude Code Agent (code-analyzer + frontend-architect)
**状态**: 已拆分为专项提案

---

## Executive Summary

通过深度分析Desktop层架构，发现以下主要问题：

| 问题类别 | 严重度 | 提案 | 工作量 |
|---------|--------|------|--------|
| 接口定义重复 | Critical | cleanup-interface-duplication | 2h |
| Converter组织混乱 | High | standardize-converter-organization | 1.5h |
| 服务命名不一致 | Medium | standardize-service-naming | 3h |
| ViewModel规模过大 | High | (已在refactor-viewmodel-composition中处理) | - |

---

## 1. Interface Duplication (Critical)

### 问题描述

9个接口在两个位置重复定义：

```
LYBT.Desktop.Contracts/Services/MasterDetail/     [正确位置]
├── IViewNavigationService.cs
├── ISelectionService.cs
├── IMasterDetailServices.cs
├── IPaginationService.cs
├── ISearchService.cs
├── IDetailEditorService.cs
├── IDialogManager.cs
├── IErrorHandler.cs
└── ILoadingStateManager.cs

LYBT.Desktop.Infrastructure/Services/Interfaces/  [应删除]
├── IViewNavigationService.cs      [DUPLICATE]
├── ISelectionService.cs           [DUPLICATE]
├── IMasterDetailServices.cs       [DUPLICATE]
├── IPaginationService.cs          [DUPLICATE]
├── ISearchService.cs              [DUPLICATE]
├── IDetailEditorService.cs        [DUPLICATE]
├── IDialogManager.cs              [DUPLICATE]
├── IErrorHandler.cs               [DUPLICATE]
├── ILoadingStateManager.cs        [DUPLICATE]
├── IAsyncExecutor.cs              [KEEP - unique]
└── IListViewServices.cs           [KEEP - unique]
```

### 根因

refactor-viewmodel-composition重构时，接口从Infrastructure迁移到Contracts，但未删除原文件。

### 解决方案

提案: `cleanup-interface-duplication`
- 保留Contracts中的定义
- 删除Infrastructure中的重复定义
- 更新所有引用

---

## 2. Converter Organization (High)

### 问题描述

#### 问题2.1: 单文件多类

```csharp
// Shell/Converters/BoolToTranslateXConverter.cs
public class BoolToTranslateXConverter : IValueConverter { ... }
public class BoolToOpacityConverter : IValueConverter { ... }  // 应该单独文件
```

#### 问题2.2: 目录位置错误

```
Infrastructure/Controls/PatientCardDisplayModeToVisibilityConverter.cs
  - 命名空间: LYBT.Desktop.Infrastructure.Controls
  - 应该在: Infrastructure/Converters/
```

#### 问题2.3: 通用vs专用边界不清

Shell有专用Converter，但BoolToOpacityConverter其实是通用的。

### 解决方案

提案: `standardize-converter-organization`
- 一文件一类
- Converter统一放Converters目录
- 通用Converter放Infrastructure

---

## 3. Service Naming Inconsistency (Medium)

### 问题描述

服务类命名后缀使用不一致：

| 后缀 | 数量 | 示例 |
|------|------|------|
| Service | 20+ | AuthenticationService, HerbService |
| Handler | 8+ | TokenRefreshHandler, HerbCommandHandler |
| Manager | 5+ | TokenManager, LoadingStateManager |
| Coordinator | 2 | MedicalCaseWorkspaceCoordinator |

### 解决方案

提案: `standardize-service-naming`
- 制定命名规范文档
- Service: 业务逻辑（无状态）
- Handler: HTTP/事件处理
- Manager: 状态管理（有状态）
- Coordinator: 复杂编排

---

## 4. Large ViewModel (High) - Already Addressed

### 问题描述

```bash
$ wc -l MedicalCaseWorkspaceViewModel.cs
1183 lines
```

### 状态

已在 `refactor-viewmodel-composition` 提案中处理，采用组合模式拆分。

---

## 5. Proposals Index

| Proposal | Priority | Status | Est. Effort |
|----------|----------|--------|-------------|
| [cleanup-interface-duplication](changes/cleanup-interface-duplication/) | Critical | draft | 2h |
| [standardize-converter-organization](changes/standardize-converter-organization/) | High | draft | 1.5h |
| [standardize-service-naming](changes/standardize-service-naming/) | Medium | draft | 3h |

---

## 6. Recommended Execution Order

1. **cleanup-interface-duplication** (Critical) - 解决编译歧义风险
2. **standardize-converter-organization** (High) - 统一代码组织
3. **standardize-service-naming** (Medium) - 建立长期规范

---

## 7. Additional Observations

### 7.1 Code Reuse Opportunities

- Models/Items目录模式在各模块重复
- 验证逻辑分散在多处
- 建议后续统一基类

### 7.2 DI Registration

- Shell注册过于集中
- 模块应该更加独立
- 建议后续优化模块注册

### 7.3 Directory Structure Inconsistencies

- 部分模块有Interfaces/目录，部分没有
- 部分模块有Models/Items/，部分没有
- 建议制定模块目录结构模板

---

**文档维护者**: Claude Code
**最后更新**: 2025-12-30
