# Desktop Phase 1 调整实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 完成 Phase 1 剩余工作：SyncViewModel 代码质量改进、MedicalCaseCommandsViewModel 评估确认、全量验证和文档更新

**Architecture:** 基于架构师评估，SyncViewModel 保持整体但提取辅助类提升内聚性；MedicalCaseCommandsViewModel 已是良好设计的 Child VM，无需拆分

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, xUnit, NSubstitute

---

## 前置检查

**依赖项确认**:
- Task 1.1-1.3 已完成（数据库初始化、API健康检查、PatientMasterDetailViewModel拆分）
- Desktop 测试项目可正常编译运行

**测试验证命令**:
```bash
dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
dotnet test tests/LYBT.Tests.Desktop --list-tests 2>/dev/null | wc -l
```

---

## Task 1.4: SyncViewModel 代码质量改进

**目标**: 提取内聚的辅助类，提升可测试性和可维护性，而非拆分 ViewModel

**架构决策**:
- 保持 SyncViewModel 整体（Phase-based 工作流已高度内聚）
- 提取 `SyncErrorClassifier` - 错误分类逻辑
- 提取 `SyncResolutionBuilder` - 同步决议构建逻辑
- 提取 `SyncItemViewModelFactory` - 同步项创建工厂

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncErrorClassifier.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncResolutionBuilder.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncItemViewModelFactory.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/SyncModule.cs`
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Sync/SyncErrorClassifierTests.cs` (新建)
- Test: `tests/LYBT.Tests.Desktop/PureLogic/Sync/SyncResolutionBuilderTests.cs` (新建)

---

### Step 1: 创建 SyncErrorClassifier

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncErrorClassifier.cs`

```csharp
using System.Net;
using LYBT.Shared.Models.Contracts.Sync;
using Refit;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步错误分类器 - 将异常分类为可重试/不可重试类型
/// </summary>
public static class SyncErrorClassifier
{
    /// <summary>
    /// 根据异常类型和状态码分类错误
    /// </summary>
    public static SyncErrorCategory Classify(Exception ex)
    {
        if (ex is HttpRequestException or TaskCanceledException)
            return SyncErrorCategory.TransientNetwork;

        if (ex is ApiException apiEx)
        {
            return apiEx.StatusCode switch
            {
                HttpStatusCode.Unauthorized => SyncErrorCategory.AuthExpired,
                HttpStatusCode.Conflict => SyncErrorCategory.ConflictChanged,
                >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError
                    => SyncErrorCategory.BusinessReject,
                _ => SyncErrorCategory.Unknown
            };
        }

        return SyncErrorCategory.Unknown;
    }

    /// <summary>
    /// 判断错误类别是否支持重试
    /// </summary>
    public static bool IsRetryable(SyncErrorCategory category)
    {
        return category is SyncErrorCategory.TransientNetwork
            or SyncErrorCategory.ConflictChanged
            or SyncErrorCategory.AuthExpired;
    }
}
```

---

### Step 2: 创建 SyncResolutionBuilder

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncResolutionBuilder.cs`

```csharp
using System.Collections.ObjectModel;
using LYBT.Desktop.Sync.ViewModels;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步决议构建器 - 从选中项构建同步决议
/// </summary>
public static class SyncResolutionBuilder
{
    /// <summary>
    /// 从差异列表构建同步决议
    /// </summary>
    public static SyncResolution Build(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        var resolution = new SyncResolution();

        resolution.ToUpload.AddRange(
            localOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        resolution.ToDownload.AddRange(
            serverOnlyItems.Where(x => x.IsSelected).Select(x => x.EntityId));

        foreach (var conflict in conflictItems.Where(x => x.IsSelected && x.ResolutionDecision.HasValue))
            resolution.ConflictResolutions[conflict.EntityId] = conflict.ResolutionDecision!.Value;

        resolution.Skipped.AddRange(
            conflictItems.Where(x => !x.IsSelected).Select(x => x.EntityId));

        return resolution;
    }

    /// <summary>
    /// 检查是否有数据需要同步
    /// </summary>
    public static bool HasDataToSync(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        return localOnlyItems.Any(x => x.IsSelected) ||
               serverOnlyItems.Any(x => x.IsSelected) ||
               conflictItems.Any(x => x.IsSelected);
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public static (int UploadCount, int DownloadCount, int ConflictCount, int TotalCount) GetCounts(
        ObservableCollection<SyncItemViewModel> localOnlyItems,
        ObservableCollection<SyncItemViewModel> serverOnlyItems,
        ObservableCollection<SyncItemViewModel> conflictItems)
    {
        var uploadCount = localOnlyItems.Count(x => x.IsSelected);
        var downloadCount = serverOnlyItems.Count(x => x.IsSelected);
        var conflictCount = conflictItems.Count;
        var totalCount = localOnlyItems.Count + serverOnlyItems.Count + conflictItems.Count;

        return (uploadCount, downloadCount, conflictCount, totalCount);
    }
}
```

---

### Step 3: 创建 SyncItemViewModelFactory

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Services/SyncItemViewModelFactory.cs`

```csharp
using System.ComponentModel;
using LYBT.Desktop.Sync.ViewModels;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Desktop.Sync.Services;

/// <summary>
/// 同步项 ViewModel 工厂
/// </summary>
public class SyncItemViewModelFactory
{
    private Action? _onSelectionChanged;

    /// <summary>
    /// 设置选择变更回调
    /// </summary>
    public void SetSelectionChangedCallback(Action callback)
    {
        _onSelectionChanged = callback;
    }

    /// <summary>
    /// 从差异 DTO 创建 SyncItemViewModel
    /// </summary>
    public SyncItemViewModel Create(SyncDiffDto diff, bool isSelected)
    {
        var item = new SyncItemViewModel
        {
            EntityId = diff.EntityId,
            EntityType = diff.EntityType,
            EntityName = diff.EntityName ?? diff.EntityId.ToString(),
            DiffType = diff.DiffType,
            LocalChecksum = diff.LocalChecksum,
            ServerChecksum = diff.ServerChecksum,
            LocalChangedAt = diff.LocalChangedAt,
            ServerChangedAt = diff.ServerChangedAt,
            ChangedFields = diff.ChangedFields,
            IsSelected = isSelected
        };

        if (_onSelectionChanged != null)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SyncItemViewModel.IsSelected))
                    _onSelectionChanged();
            };
        }

        return item;
    }
}
```

---

### Step 4: 修改 SyncViewModel 使用新服务

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs`

修改要点：
1. 删除 `ClassifyException` 方法（使用 SyncErrorClassifier）
2. 删除 `BuildSyncResolution` 方法（使用 SyncResolutionBuilder）
3. 删除 `CreateSyncItemViewModel` 方法（使用 SyncItemViewModelFactory）
4. 简化 `HasDataToSync` 等计算属性

```csharp
// 构造函数添加工厂依赖
public SyncViewModel(
    IViewModelServices services,
    ISyncService syncService,
    IDialogService dialogService,
    IApiHealthCheckService healthCheckService,
    SyncItemViewModelFactory itemFactory) : base(services)
{
    _syncService = syncService;
    _dialogService = dialogService;
    _healthCheckService = healthCheckService;
    _itemFactory = itemFactory;
    _itemFactory.SetSelectionChangedCallback(NotifyCountsChanged);

    PageTitle = "数据同步";
}

// 简化计算属性
public bool HasDataToSync =>
    SyncResolutionBuilder.HasDataToSync(LocalOnlyItems, ServerOnlyItems, ConflictItems);

// 简化错误处理
ErrorCategory = SyncErrorClassifier.Classify(ex);
CanRetry = SyncErrorClassifier.IsRetryable(ErrorCategory);

// 简化决议构建
var resolution = SyncResolutionBuilder.Build(LocalOnlyItems, ServerOnlyItems, ConflictItems);
var (uploadCount, downloadCount, conflictCount, totalCount) =
    SyncResolutionBuilder.GetCounts(LocalOnlyItems, ServerOnlyItems, ConflictItems);

// 使用工厂创建项
foreach (var diff in result.LocalOnly)
    LocalOnlyItems.Add(_itemFactory.Create(diff, true));
```

---

### Step 5: 注册新服务

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.Sync/SyncModule.cs`

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ... 现有注册 ...

    // 注册新的辅助服务
    containerRegistry.RegisterSingleton<SyncItemViewModelFactory>();
}
```

---

### Step 6: 编写 SyncErrorClassifier 测试

**File:** `tests/LYBT.Tests.Desktop/PureLogic/Sync/SyncErrorClassifierTests.cs`

```csharp
using LYBT.Desktop.Sync.Services;
using LYBT.Shared.Models.Contracts.Sync;
using Refit;
using System.Net;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

public class SyncErrorClassifierTests
{
    [Fact]
    public void Classify_HttpRequestException_ReturnsTransientNetwork()
    {
        // Act
        var result = SyncErrorClassifier.Classify(new HttpRequestException());

        // Assert
        Assert.Equal(SyncErrorCategory.TransientNetwork, result);
    }

    [Fact]
    public void Classify_TaskCanceledException_ReturnsTransientNetwork()
    {
        // Act
        var result = SyncErrorClassifier.Classify(new TaskCanceledException());

        // Assert
        Assert.Equal(SyncErrorCategory.TransientNetwork, result);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, SyncErrorCategory.AuthExpired)]
    [InlineData(HttpStatusCode.Conflict, SyncErrorCategory.ConflictChanged)]
    [InlineData(HttpStatusCode.BadRequest, SyncErrorCategory.BusinessReject)]
    [InlineData(HttpStatusCode.InternalServerError, SyncErrorCategory.Unknown)]
    public void Classify_ApiException_ReturnsCorrectCategory(HttpStatusCode statusCode, SyncErrorCategory expected)
    {
        // Arrange
        var ex = new ApiException(new HttpResponseMessage(statusCode), "test");

        // Act
        var result = SyncErrorClassifier.Classify(ex);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(SyncErrorCategory.TransientNetwork, true)]
    [InlineData(SyncErrorCategory.AuthExpired, true)]
    [InlineData(SyncErrorCategory.ConflictChanged, true)]
    [InlineData(SyncErrorCategory.BusinessReject, false)]
    [InlineData(SyncErrorCategory.Unknown, false)]
    public void IsRetryable_ReturnsExpected(SyncErrorCategory category, bool expected)
    {
        // Act
        var result = SyncErrorClassifier.IsRetryable(category);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

---

### Step 7: 编写 SyncResolutionBuilder 测试

**File:** `tests/LYBT.Tests.Desktop/PureLogic/Sync/SyncResolutionBuilderTests.cs`

```csharp
using System.Collections.ObjectModel;
using LYBT.Desktop.Sync.Services;
using LYBT.Desktop.Sync.ViewModels;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.Tests.Desktop.PureLogic.Sync;

public class SyncResolutionBuilderTests
{
    [Fact]
    public void Build_WithSelectedItems_BuildsCorrectResolution()
    {
        // Arrange
        var localItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateItem(Guid.NewGuid(), true),
            CreateItem(Guid.NewGuid(), false)
        };
        var serverItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateItem(Guid.NewGuid(), true)
        };
        var conflictItems = new ObservableCollection<SyncItemViewModel>
        {
            CreateItem(Guid.NewGuid(), true, true)
        };

        // Act
        var result = SyncResolutionBuilder.Build(localItems, serverItems, conflictItems);

        // Assert
        Assert.Single(result.ToUpload);
        Assert.Single(result.ToDownload);
        Assert.Single(result.ConflictResolutions);
        Assert.Empty(result.Skipped);
    }

    [Fact]
    public void HasDataToSync_WithSelectedItems_ReturnsTrue()
    {
        // Arrange
        var items = new ObservableCollection<SyncItemViewModel>
        {
            CreateItem(Guid.NewGuid(), true)
        };

        // Act
        var result = SyncResolutionBuilder.HasDataToSync(items, new(), new());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasDataToSync_WithNoSelectedItems_ReturnsFalse()
    {
        // Arrange
        var items = new ObservableCollection<SyncItemViewModel>
        {
            CreateItem(Guid.NewGuid(), false)
        };

        // Act
        var result = SyncResolutionBuilder.HasDataToSync(items, new(), new());

        // Assert
        Assert.False(result);
    }

    private static SyncItemViewModel CreateItem(Guid id, bool isSelected, bool? resolution = null)
    {
        return new SyncItemViewModel
        {
            EntityId = id,
            EntityType = "Test",
            EntityName = "Test",
            DiffType = SyncDiffType.LocalOnly,
            IsSelected = isSelected,
            ResolutionDecision = resolution
        };
    }
}
```

---

### Step 8: 运行测试验证

```bash
# 编译项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Sync/LYBT.Desktop.Sync.csproj

# 运行新增测试
dotnet test tests/LYBT.Tests.Desktop --filter "FullyQualifiedName~SyncErrorClassifierTests" -v n
dotnet test tests/LYBT.Tests.Desktop --filter "FullyQualifiedName~SyncResolutionBuilderTests" -v n

# 运行全量 Desktop 测试
dotnet test tests/LYBT.Tests.Desktop -v n
```

**预期结果**: 所有测试通过

---

### Step 9: Commit

```bash
git add -A
git commit -m "refactor(sync): extract helper classes from SyncViewModel

- Add SyncErrorClassifier for error categorization
- Add SyncResolutionBuilder for sync resolution construction
- Add SyncItemViewModelFactory for item creation
- Simplify SyncViewModel by delegating to helper classes
- Add unit tests for new services

Improves testability and maintains cohesion"
```

---

## Task 1.5: MedicalCaseCommandsViewModel 评估确认

**目标**: 确认 MedicalCaseCommandsViewModel 无需拆分，添加架构注释

**架构评估**:
- 已是 ChildViewModelBase 子类，粒度合适
- 9个命令高度耦合（共享 _context, _medicalCaseService）
- 委托模式（GetConsultationData 等）已与父 VM 解耦

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`
- Update: `task_plan.md` - 标记 Task 1.5 完成
- Update: `progress.md` - 记录评估结论

---

### Step 1: 添加架构注释

**File:** `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`

在类文档注释中添加架构说明：

```csharp
/// <summary>
/// Child VM for aggregate root commands (save/suspend/complete/print/import/clear).
/// All operations go through the MedicalCase aggregate root via IMedicalCaseService.
/// Import operations (formula/history/clear) are handled directly, replacing PrescriptionImportHandler callbacks.
/// </summary>
/// <remarks>
/// ARCHITECTURE-NOTE: This VM is intentionally kept as a single cohesive unit despite having 9 commands.
/// The commands are highly coupled (sharing _context, _medicalCaseService, delegates from parent)
/// and represent a single responsibility: "Medical Case Lifecycle Commands".
/// Attempting to split would introduce unnecessary complexity and cross-VM coordination overhead.
/// See: Phase 1 Architecture Review 2026-03-15
/// </remarks>
```

---

### Step 2: 更新 Task Plan

**File:** `task_plan.md`

将 Task 1.5 标记为完成，添加注释：

```markdown
| 1.5 | MedicalCaseCommandsViewModel 评估 | P0 | **已完成 - 架构评估确认无需拆分** |
```

---

### Step 3: Commit

```bash
git add -A
git commit -m "docs: add architecture note to MedicalCaseCommandsViewModel

- Document why this VM is intentionally kept as single unit
- Commands are cohesive lifecycle operations
- Splitting would introduce unnecessary complexity

Phase 1 Task 1.5 complete"
```

---

## Task 1.6: 验证和文档更新

**目标**: 验证所有 Phase 1 工作完成，更新进度文档

**Files:**
- Modify: `progress.md`
- Modify: `task_plan.md`
- Modify: `docs/plans/2026-03-14-desktop-refactoring-phase1.md` - 添加调整说明

---

### Step 1: 运行全量测试

```bash
dotnet test tests/LYBT.Tests.Desktop --verbosity normal
# 预期: 所有现有测试通过 + 新添加的测试通过
```

---

### Step 2: 更新 progress.md

添加 Phase 1 完成总结：

```markdown
### 2026-03-15 - Phase 1 完成

**Completed Tasks**:
- [✓] Task 1.1: 延迟数据库初始化
- [✓] Task 1.2: 异步 API 健康检查
- [✓] Task 1.3: PatientMasterDetailViewModel 拆分
- [✓] Task 1.4: SyncViewModel 代码质量改进（提取辅助类）
- [✓] Task 1.5: MedicalCaseCommandsViewModel 评估确认

**架构决策调整**:
- SyncViewModel: 保持整体，提取 SyncErrorClassifier/SyncResolutionBuilder/SyncItemViewModelFactory
- MedicalCaseCommandsViewModel: 确认已是良好粒度，无需拆分

**Metrics**:
- 新增文件: 5 个 (3 服务类 + 2 测试类)
- 修改文件: 4 个
- 测试通过率: 100%
- SyncViewModel 代码行减少: ~60 行（提取到辅助类）

**Next**: Phase 2 - 测试覆盖
```

---

### Step 3: 更新 task_plan.md

更新 Phase 1 状态为完成：

```markdown
### Phase 1: 紧急修复

**Status**: ✅ **COMPLETE**
```

---

### Step 4: Commit

```bash
git add -A
git commit -m "docs: complete Phase 1 documentation

- Update progress.md with completion summary
- Update task_plan.md Phase 1 status
- Document architecture decisions

Phase 1 complete - ready for Phase 2"
```

---

## 执行检查清单

**Before Starting**:
- [ ] 确认当前分支干净
- [ ] 确认 Task 1.1-1.3 已完成
- [ ] WebAPI 可正常启动（用于集成测试）

**During Implementation**:
- [ ] 每个 Task 完成后立即运行相关测试
- [ ] 确保没有破坏现有功能
- [ ] 保持提交信息清晰描述

**After Completion**:
- [ ] 全量测试通过
- [ ] 更新三文件 (task_plan, findings, progress)
- [ ] 准备 Phase 2 计划

---

## 风险与回滚

**Risk 1: 辅助类引入 regression**
- 缓解: 保持原有行为不变，仅移动代码
- 检测: 全量测试验证
- 回滚: 恢复 SyncViewModel.cs 到修改前

**Risk 2: SyncItemViewModelFactory 回调泄漏**
- 缓解: 使用 SetSelectionChangedCallback 模式，生命周期由 VM 控制
- 检测: 内存分析工具检查
- 回滚: 恢复内联创建模式
