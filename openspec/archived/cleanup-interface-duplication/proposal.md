# OpenSpec Proposal: cleanup-interface-duplication

**Change ID**: cleanup-interface-duplication
**Status**: archived
**Priority**: Critical
**Estimated Effort**: 2h
**Created**: 2025-12-30
**Cancelled**: 2025-12-30
**Archived**: 2026-01-05

---

## 0. Cancellation Notice

**取消原因**: 2025-12-30

经深入分析，发现两套接口并非简单重复，而是两种不同设计：
- **Contracts版本**: 简化的契约接口，面向服务调用
- **Infrastructure版本**: MVVM友好的实现接口，支持ObservableCollection和INotifyPropertyChanged

两套接口服务于不同架构层次，不应删除任何一方。如需统一，需要新的整合提案。

---

## 1. Problem Statement

### 1.1 Current Situation

Desktop层存在严重的接口重复定义问题。相同的接口在两个不同位置定义：

| 接口名称 | Contracts位置 | Infrastructure位置 |
|---------|---------------|-------------------|
| IViewNavigationService | Services/MasterDetail/ | Services/Interfaces/ |
| ISelectionService | Services/MasterDetail/ | Services/Interfaces/ |
| IMasterDetailServices | Services/MasterDetail/ | Services/Interfaces/ |
| IPaginationService | Services/MasterDetail/ | Services/Interfaces/ |
| ISearchService | Services/MasterDetail/ | Services/Interfaces/ |
| IDetailEditorService | Services/MasterDetail/ | Services/Interfaces/ |
| IDialogManager | Services/MasterDetail/ | Services/Interfaces/ |
| IErrorHandler | Services/MasterDetail/ | Services/Interfaces/ |
| ILoadingStateManager | Services/MasterDetail/ | Services/Interfaces/ |

### 1.2 Root Cause

在refactor-viewmodel-composition重构过程中，接口从Infrastructure迁移到Contracts时，未删除原有定义。

### 1.3 Impact

- **DRY违反**: 相同代码存在两份
- **维护风险**: 修改接口时可能只改一处
- **编译歧义**: 可能引用错误的命名空间
- **架构混乱**: 违反单一真相源原则

---

## 2. Solution

### 2.1 Design Decision

**保留Contracts中的定义，删除Infrastructure中的重复定义**

理由：
1. Contracts层是接口契约的正确位置（依赖倒置原则）
2. Infrastructure层应只包含实现，不应定义接口
3. Contracts中的定义更新、更完整

### 2.2 Implementation Steps

1. 检查Infrastructure/Services/Interfaces/中每个接口的引用
2. 将所有引用更新为Contracts/Services/MasterDetail/中的接口
3. 删除Infrastructure/Services/Interfaces/中的重复接口文件
4. 保留Infrastructure特有的接口（如IAsyncExecutor、IListViewServices）

### 2.3 Files to Delete

```
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Interfaces/
├── IDetailEditorService.cs      [DELETE - duplicated]
├── IDialogManager.cs            [DELETE - duplicated]
├── IErrorHandler.cs             [DELETE - duplicated]
├── ILoadingStateManager.cs      [DELETE - duplicated]
├── IMasterDetailServices.cs     [DELETE - duplicated]
├── IPaginationService.cs        [DELETE - duplicated]
├── ISearchService.cs            [DELETE - duplicated]
├── ISelectionService.cs         [DELETE - duplicated]
├── IViewNavigationService.cs    [DELETE - duplicated]
├── IAsyncExecutor.cs            [KEEP - unique]
└── IListViewServices.cs         [KEEP - unique]
```

---

## 3. Validation

### 3.1 Acceptance Criteria

- [ ] 所有重复接口文件已删除
- [ ] 所有引用已更新到正确命名空间
- [ ] 编译通过（0 errors, 0 warnings related to interfaces）
- [ ] 单元测试全部通过

### 3.2 Test Commands

```bash
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test tests/UnitTests/Client/Desktop/**/*.csproj
```

---

## 4. Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| 遗漏引用更新 | Low | Medium | 编译器会报错 |
| 删除非重复接口 | Low | High | 仔细对比两边定义 |

---

## 5. Dependencies

- None (独立提案)

---

## 6. Timeline

| Phase | Task | Duration |
|-------|------|----------|
| 1 | 分析引用关系 | 30min |
| 2 | 更新命名空间引用 | 45min |
| 3 | 删除重复文件 | 15min |
| 4 | 验证编译和测试 | 30min |
