# Tasks: cleanup-interface-duplication

**Change ID**: cleanup-interface-duplication
**Total Tasks**: 4
**Completed**: 0/4

---

## Task List

### Task 1: Analyze Interface References
- **ID**: CID-001
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min

**Description**: 分析Infrastructure/Services/Interfaces/中每个接口的引用情况

**Acceptance Criteria**:
- [ ] 列出所有重复接口
- [ ] 确认Contracts中的定义是最新版本
- [ ] 识别所有引用这些接口的文件

**Files**:
- `Infrastructure/Services/Interfaces/*.cs`
- `Contracts/Services/MasterDetail/*.cs`

---

### Task 2: Update Namespace References
- **ID**: CID-002
- **Status**: pending
- **Priority**: P0
- **Effort**: 45min
- **Depends On**: CID-001

**Description**: 将所有引用更新到Contracts命名空间

**Acceptance Criteria**:
- [ ] 更新所有using语句
- [ ] 编译通过无命名空间错误

**Files**:
- 所有引用Infrastructure接口的.cs文件

---

### Task 3: Delete Duplicate Interface Files
- **ID**: CID-003
- **Status**: pending
- **Priority**: P0
- **Effort**: 15min
- **Depends On**: CID-002

**Description**: 删除Infrastructure中的重复接口文件

**Files to Delete**:
```
Infrastructure/Services/Interfaces/
├── IDetailEditorService.cs
├── IDialogManager.cs
├── IErrorHandler.cs
├── ILoadingStateManager.cs
├── IMasterDetailServices.cs
├── IPaginationService.cs
├── ISearchService.cs
├── ISelectionService.cs
└── IViewNavigationService.cs
```

**Files to Keep**:
```
Infrastructure/Services/Interfaces/
├── IAsyncExecutor.cs
└── IListViewServices.cs
```

---

### Task 4: Verify Build and Tests
- **ID**: CID-004
- **Status**: pending
- **Priority**: P0
- **Effort**: 30min
- **Depends On**: CID-003

**Description**: 验证编译和测试通过

**Acceptance Criteria**:
- [ ] `dotnet build LYBT.All.sln -c Release` 通过
- [ ] 所有Desktop相关单元测试通过
- [ ] 无namespace相关警告

---

## Progress Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Analysis | CID-001 | pending |
| Migration | CID-002 | pending |
| Cleanup | CID-003 | pending |
| Validation | CID-004 | pending |
