## 📋 概述

修复 Desktop 项目编译错误，主要涉及 Formula 模块缺少必要的 using 指令和类型引用。

## 🎯 背景

在 Issue #821 执行过程中，编译验证时发现 Desktop 解决方案存在编译错误。这些错误与 Issue #821 的架构优化无关，而是既有代码质量问题（可能在 Issue #820 架构重构时引入）。

## 🐛 错误详情

### Formula 模块编译错误

**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/`

#### 错误 1: FormulaDetailViewModel.cs (第 271-272 行)
```
error CS0246: 未能找到类型或命名空间名"ISessionManager"(是否缺少 using 指令或程序集引用?)
error CS0246: 未能找到类型或命名空间名"IErrorHandlingService"(是否缺少 using 指令或程序集引用?)
```

#### 错误 2: FormulaManagementViewModel.cs
```
error CS0246: 未能找到类型或命名空间名"UnifiedListViewModelBase<>"(是否缺少 using 指令或程序集引用?)
error CS0246: 未能找到类型或命名空间名"ISessionManager"(是否缺少 using 指令或程序集引用?)
error CS0246: 未能找到类型或命名空间名"IErrorHandlingService"(是否缺少 using 指令或程序集引用?)
```

## 🔍 问题分析

### 根本原因

1. **缺少 using 指令**: ViewModel 文件中没有导入必要的命名空间
2. **可能的命名空间变化**: Issue #820 的架构重构可能导致命名空间路径变化

### 受影响的类型

| 类型 | 可能的命名空间 | 所在项目 |
|------|---------------|----------|
| `ISessionManager` | `LYBT.Desktop.Services` 或 `LYBT.Desktop.Infrastructure` | Core 项目 |
| `IErrorHandlingService` | `LYBT.Desktop.Services` 或 `LYBT.Desktop.Infrastructure` | Core 项目 |
| `UnifiedListViewModelBase<>` | `LYBT.Desktop.Models` 或 `LYBT.Desktop.Infrastructure` | Core 项目 |

## ✅ 解决方案

### 步骤 1: 定位类型所在位置

- [ ] **[LOCATE-1]** 使用 `mcp__serena__find_symbol` 查找 `ISessionManager` 定义位置
- [ ] **[LOCATE-2]** 使用 `mcp__serena__find_symbol` 查找 `IErrorHandlingService` 定义位置
- [ ] **[LOCATE-3]** 使用 `mcp__serena__find_symbol` 查找 `UnifiedListViewModelBase<>` 定义位置

### 步骤 2: 修复 FormulaDetailViewModel.cs

- [ ] **[FIX-1]** 在 `FormulaDetailViewModel.cs` 顶部添加缺失的 using 指令
  - 验收: 文件包含正确的命名空间导入
  - 位置: 文件开头 using 语句块

### 步骤 3: 修复 FormulaManagementViewModel.cs

- [ ] **[FIX-2]** 在 `FormulaManagementViewModel.cs` 顶部添加缺失的 using 指令
  - 包括: `ISessionManager`, `IErrorHandlingService`, `UnifiedListViewModelBase<>`
  - 验收: 文件包含所有必要的命名空间导入

### 步骤 4: 编译验证

- [ ] **[BUILD-1]** 编译 Formula 模块
  - 命令: `dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Formula/LYBT.Desktop.Formula.csproj -c Release`
  - 验收: 0 errors

- [ ] **[BUILD-2]** 编译 LYBT.Desktop.sln
  - 命令: `dotnet build LYBT.Desktop.sln -c Release`
  - 验收: 0 errors（警告数与基线一致）

- [ ] **[BUILD-3]** 编译 LYBT.All.sln
  - 命令: `dotnet build LYBT.All.sln -c Release`
  - 验收: 0 errors（警告数与基线一致）

### 步骤 5: Git 提交

- [ ] **[COMMIT-1]** 创建 Git 提交
  - 提交信息: "fix(desktop): 修复 Formula 模块编译错误 - Issue #822"
  - 包含: FormulaDetailViewModel.cs, FormulaManagementViewModel.cs 的 using 修复
  - 验收: `git log` 显示提交

## 🔬 补充调查（可选）

如果简单添加 using 无法解决问题，可能需要进一步调查：

- [ ] **[INVESTIGATE-1]** 检查 Formula.csproj 项目引用是否完整
- [ ] **[INVESTIGATE-2]** 检查其他模块（Herbs, Patients, Users 等）是否有类似问题
- [ ] **[INVESTIGATE-3]** 确认 Issue #820 的命名空间重构是否一致

## ⚠️ 风险评估

| 风险项 | 严重程度 | 影响范围 | 缓解措施 |
|--------|----------|----------|----------|
| 类型不存在 | 低 | Formula 模块 | 通过符号搜索确认类型存在 |
| 命名空间错误 | 低 | 单个文件 | 参考其他模块的正确导入 |
| 项目引用缺失 | 中 | Formula 模块 | 检查并添加必要的 ProjectReference |

## 📚 参考资料

- Issue #821: Desktop 架构清理 - 删除未使用文件夹并优化资源位置
- Issue #820: Desktop 架构优化 - 统一文件夹命名规范
- `docs/development/standards.md` - 编码规范

---

**创建时间**: 2025-09-30
**预计工作量**: 0.5 小时
**优先级**: 高（阻塞编译）
**依赖**: Issue #821 (已完成)
**相关**: Issue #820