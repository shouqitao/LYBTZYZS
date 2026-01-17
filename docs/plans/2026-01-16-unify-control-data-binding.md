# Unify Control Data Binding Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reduce 293 DependencyProperties to ~100 by replacing scattered properties with aggregated object models (DisplayModel, EditModel, ViewState, ControlOptions).

**Architecture:** Create reusable State/Options classes in Infrastructure, then refactor high-property controls to use object-based bindings. Each control gets 3-6 object properties instead of 15-26 discrete properties.

**Tech Stack:** WPF, CommunityToolkit.Mvvm ([ObservableProperty]), Mapperly, C# records

---

## Phase A: Infrastructure Foundation

### Task A.1: Create PaginationState

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/PaginationState.cs`
- Test: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/PaginationStateTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/PaginationStateTests.cs
using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.State;

public class PaginationStateTests
{
    [Fact]
    public void TotalPages_CalculatesCorrectly()
    {
        var state = new PaginationState { TotalCount = 55, PageSize = 20 };
        Assert.Equal(3, state.TotalPages);
    }

    [Fact]
    public void HasPrevious_FalseOnFirstPage()
    {
        var state = new PaginationState { CurrentPage = 1 };
        Assert.False(state.HasPrevious);
    }

    [Fact]
    public void HasNext_TrueWhenNotOnLastPage()
    {
        var state = new PaginationState { CurrentPage = 1, TotalCount = 50, PageSize = 20 };
        Assert.True(state.HasNext);
    }

    [Fact]
    public void GoToPage_ClampsToValidRange()
    {
        var state = new PaginationState { TotalCount = 50, PageSize = 20 };
        state.GoToPage(10);
        Assert.Equal(3, state.CurrentPage);
    }

    [Fact]
    public void Reset_SetsDefaultValues()
    {
        var state = new PaginationState { CurrentPage = 5, TotalCount = 100 };
        state.Reset();
        Assert.Equal(1, state.CurrentPage);
        Assert.Equal(0, state.TotalCount);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~PaginationStateTests" -v n`
Expected: FAIL with "type or namespace 'PaginationState' could not be found"

**Step 3: Write minimal implementation**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/PaginationState.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的分页状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class PaginationState : ObservableObject
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;

    /// <summary>总页数</summary>
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;

    /// <summary>是否有上一页</summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>是否有下一页</summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>跳转到指定页（自动限制范围）</summary>
    public void GoToPage(int page) => CurrentPage = Math.Clamp(page, 1, TotalPages);

    /// <summary>重置分页状态</summary>
    public void Reset()
    {
        CurrentPage = 1;
        TotalCount = 0;
    }

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(HasPrevious));
    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNext));
    }
    partial void OnPageSizeChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNext));
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~PaginationStateTests" -v n`
Expected: PASS (5 passed)

**Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/PaginationState.cs
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/PaginationStateTests.cs
git commit -m "feat(infrastructure): add PaginationState for unified pagination handling

OpenSpec: unify-control-data-binding - Phase A.1"
```

---

### Task A.2: Create LoadingState

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/LoadingState.cs`
- Test: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/LoadingStateTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/LoadingStateTests.cs
using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.State;

public class LoadingStateTests
{
    [Fact]
    public void Start_SetsIsLoadingAndMessage()
    {
        var state = new LoadingState();
        state.Start("Loading data...");
        Assert.True(state.IsLoading);
        Assert.Equal("Loading data...", state.Message);
    }

    [Fact]
    public void Start_UsesDefaultMessage()
    {
        var state = new LoadingState();
        state.Start();
        Assert.True(state.IsLoading);
        Assert.Equal("加载中...", state.Message);
    }

    [Fact]
    public void Stop_ClearsState()
    {
        var state = new LoadingState { IsLoading = true, Message = "test" };
        state.Stop();
        Assert.False(state.IsLoading);
        Assert.Null(state.Message);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~LoadingStateTests" -v n`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/LoadingState.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的加载状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class LoadingState : ObservableObject
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _message;

    /// <summary>开始加载</summary>
    public void Start(string? message = "加载中...")
    {
        IsLoading = true;
        Message = message;
    }

    /// <summary>停止加载</summary>
    public void Stop()
    {
        IsLoading = false;
        Message = null;
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~LoadingStateTests" -v n`
Expected: PASS (3 passed)

**Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/LoadingState.cs
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/LoadingStateTests.cs
git commit -m "feat(infrastructure): add LoadingState for unified loading handling

OpenSpec: unify-control-data-binding - Phase A.2"
```

---

### Task A.3: Create SearchState

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/SearchState.cs`
- Test: `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/SearchStateTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/SearchStateTests.cs
using LYBT.Desktop.Infrastructure.Models.State;
using Xunit;

namespace LYBT.Desktop.Infrastructure.Tests.Models.State;

public class SearchStateTests
{
    [Fact]
    public void HasKeyword_TrueWhenNotEmpty()
    {
        var state = new SearchState { Keyword = "test" };
        Assert.True(state.HasKeyword);
    }

    [Fact]
    public void HasKeyword_FalseWhenEmpty()
    {
        var state = new SearchState { Keyword = "  " };
        Assert.False(state.HasKeyword);
    }

    [Fact]
    public void Clear_ResetsAllProperties()
    {
        var state = new SearchState { Keyword = "test", IsSearching = true };
        state.Clear();
        Assert.Empty(state.Keyword);
        Assert.False(state.IsSearching);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~SearchStateTests" -v n`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/SearchState.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的搜索状态对象
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class SearchState : ObservableObject
{
    [ObservableProperty] private string _keyword = string.Empty;
    [ObservableProperty] private bool _isSearching;

    /// <summary>是否有搜索关键词</summary>
    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);

    /// <summary>清除搜索状态</summary>
    public void Clear()
    {
        Keyword = string.Empty;
        IsSearching = false;
    }

    partial void OnKeywordChanged(string value) => OnPropertyChanged(nameof(HasKeyword));
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests --filter "FullyQualifiedName~SearchStateTests" -v n`
Expected: PASS (3 passed)

**Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/State/SearchState.cs
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/Models/State/SearchStateTests.cs
git commit -m "feat(infrastructure): add SearchState for unified search handling

OpenSpec: unify-control-data-binding - Phase A.3"
```

---

### Task A.4: Create DisplayOptions

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/DisplayOptions.cs`

**Step 1: Write implementation (no test needed for record)**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/DisplayOptions.cs
namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 通用显示选项
/// OpenSpec: unify-control-data-binding
/// </summary>
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true,
    bool IsReadOnly = false
);
```

**Step 2: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/DisplayOptions.cs
git commit -m "feat(infrastructure): add DisplayOptions record for control configuration

OpenSpec: unify-control-data-binding - Phase A.4"
```

---

### Task A.5: Create PaginationOptions

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/PaginationOptions.cs`

**Step 1: Write implementation**

```csharp
// src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/PaginationOptions.cs
namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 分页选项配置
/// OpenSpec: unify-control-data-binding
/// </summary>
public record PaginationOptions(
    bool ShowPageSize = true,
    bool ShowTotalCount = true,
    int[] PageSizeOptions = null!
)
{
    public int[] PageSizeOptions { get; init; } = PageSizeOptions ?? new[] { 10, 20, 50, 100 };
}
```

**Step 2: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Models/Options/PaginationOptions.cs
git commit -m "feat(infrastructure): add PaginationOptions record for pagination configuration

OpenSpec: unify-control-data-binding - Phase A.5"
```

---

## Phase B: High Priority Controls

### Task B.1: Create ConsultationEditModel

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/ConsultationEditModel.cs`
- Test: `tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/ConsultationEditModelTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/ConsultationEditModelTests.cs
using LYBT.Desktop.MedicalCase.Models.Edit;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Models.Edit;

public class ConsultationEditModelTests
{
    [Fact]
    public void IsValid_TrueWhenTcmDiagnosisSet()
    {
        var model = new ConsultationEditModel { TcmDiagnosis = "肝郁脾虚" };
        Assert.True(model.IsValid);
    }

    [Fact]
    public void IsValid_FalseWhenTcmDiagnosisEmpty()
    {
        var model = new ConsultationEditModel();
        Assert.False(model.IsValid);
    }

    [Fact]
    public void Reset_ClearsAllProperties()
    {
        var model = new ConsultationEditModel
        {
            PresentIllness = "test",
            TongueDiagnosis = "test",
            PulseDiagnosis = "test",
            TcmDiagnosis = "test"
        };
        model.Reset();
        Assert.Null(model.PresentIllness);
        Assert.Null(model.TongueDiagnosis);
        Assert.Null(model.PulseDiagnosis);
        Assert.Null(model.TcmDiagnosis);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests --filter "FullyQualifiedName~ConsultationEditModelTests" -v n`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/ConsultationEditModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.MedicalCase.Models.Edit;

/// <summary>
/// 诊断编辑数据模型
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class ConsultationEditModel : ObservableObject
{
    [ObservableProperty] private string? _presentIllness;
    [ObservableProperty] private string? _tongueDiagnosis;
    [ObservableProperty] private string? _pulseDiagnosis;
    [ObservableProperty] private string? _tcmDiagnosis;

    /// <summary>诊断是否有效（至少有中医诊断）</summary>
    public bool IsValid => !string.IsNullOrEmpty(TcmDiagnosis);

    /// <summary>重置所有诊断信息</summary>
    public void Reset()
    {
        PresentIllness = null;
        TongueDiagnosis = null;
        PulseDiagnosis = null;
        TcmDiagnosis = null;
    }

    partial void OnTcmDiagnosisChanged(string? value) => OnPropertyChanged(nameof(IsValid));
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests --filter "FullyQualifiedName~ConsultationEditModelTests" -v n`
Expected: PASS (3 passed)

**Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/ConsultationEditModel.cs
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/ConsultationEditModelTests.cs
git commit -m "feat(medicalcase): add ConsultationEditModel for diagnosis editing

OpenSpec: unify-control-data-binding - Phase B.1"
```

---

### Task B.2: Create PrescriptionEditModel

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/PrescriptionEditModel.cs`
- Test: `tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/PrescriptionEditModelTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/PrescriptionEditModelTests.cs
using System.Collections.ObjectModel;
using LYBT.Desktop.MedicalCase.Models.Edit;
using LYBT.Shared.Models.DTOs.Herbs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Models.Edit;

public class PrescriptionEditModelTests
{
    [Fact]
    public void HerbCount_ReturnsItemsCount()
    {
        var model = new PrescriptionEditModel();
        model.HerbItems.Add(new HerbItemDto { HerbName = "Test" });
        model.HerbItems.Add(new HerbItemDto { HerbName = "Test2" });
        Assert.Equal(2, model.HerbCount);
    }

    [Fact]
    public void IsValid_TrueWhenHasItems()
    {
        var model = new PrescriptionEditModel();
        model.HerbItems.Add(new HerbItemDto { HerbName = "Test" });
        Assert.True(model.IsValid);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var model = new PrescriptionEditModel();
        model.HerbItems.Add(new HerbItemDto { HerbName = "Test" });
        model.DoseCount = 5;
        model.Clear();
        Assert.Empty(model.HerbItems);
        Assert.Equal(3, model.DoseCount); // Default value
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests --filter "FullyQualifiedName~PrescriptionEditModelTests" -v n`
Expected: FAIL

**Step 3: Write minimal implementation**

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/PrescriptionEditModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.DTOs.Herbs;

namespace LYBT.Desktop.MedicalCase.Models.Edit;

/// <summary>
/// 处方编辑数据模型
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class PrescriptionEditModel : ObservableObject
{
    [ObservableProperty] private int _doseCount = 3;
    [ObservableProperty] private string? _usage;
    [ObservableProperty] private string? _formulaSource;
    [ObservableProperty] private decimal _totalPrice;

    public ObservableCollection<HerbItemDto> HerbItems { get; } = new();

    /// <summary>药材数量</summary>
    public int HerbCount => HerbItems.Count;

    /// <summary>处方是否有效（至少有一味药）</summary>
    public bool IsValid => HerbItems.Count > 0;

    /// <summary>清空处方</summary>
    public void Clear()
    {
        HerbItems.Clear();
        DoseCount = 3;
        Usage = null;
        FormulaSource = null;
        TotalPrice = 0;
        OnPropertyChanged(nameof(HerbCount));
        OnPropertyChanged(nameof(IsValid));
    }

    public PrescriptionEditModel()
    {
        HerbItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HerbCount));
            OnPropertyChanged(nameof(IsValid));
        };
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests --filter "FullyQualifiedName~PrescriptionEditModelTests" -v n`
Expected: PASS (3 passed)

**Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/Edit/PrescriptionEditModel.cs
git add tests/UnitTests/Client/Desktop/LYBT.Desktop.MedicalCase.Tests/Models/Edit/PrescriptionEditModelTests.cs
git commit -m "feat(medicalcase): add PrescriptionEditModel for prescription editing

OpenSpec: unify-control-data-binding - Phase B.2"
```

---

### Task B.3: Create PatientDetailDisplayModel

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/Display/PatientDetailDisplayModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mappers/PatientMapper.cs`

**Step 1: Write implementation**

```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/Display/PatientDetailDisplayModel.cs
namespace LYBT.Desktop.Patients.Models.Display;

/// <summary>
/// 患者详情展示模型（用于PatientViewControl）
/// OpenSpec: unify-control-data-binding
/// </summary>
public class PatientDetailDisplayModel
{
    // 基本信息
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PinYinCode { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public string? IdNumber { get; set; }
    public string? IdType { get; set; }
    public string? MaritalStatus { get; set; }
    public string? BloodType { get; set; }

    // 联系信息
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    // 紧急联系人
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }

    // 病史信息
    public string? AllergyHistory { get; set; }
    public string? MedicalHistory { get; set; }

    // 就诊信息
    public DateTime? LastVisitTime { get; set; }
    public int VisitCount { get; set; }

    // 系统信息
    public string Status { get; set; } = string.Empty;
    public string? DisableReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 计算属性
    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";
    public string GenderDisplay => Gender switch
    {
        "Male" => "男",
        "Female" => "女",
        _ => Gender
    };
    public string StatusDisplay => Status switch
    {
        "Active" => "正常",
        "Disabled" => "已禁用",
        _ => Status
    };
    public string BasicInfoSummary => $"{Name} {GenderDisplay} {AgeDisplay}";
    public bool HasEmergencyContact => !string.IsNullOrEmpty(EmergencyContactName);
}
```

**Step 2: Add Mapper method**

```csharp
// 在 PatientMapper.cs 中添加
[MapperIgnoreSource(nameof(PatientDetailDto.MedicalCases))]
public partial PatientDetailDisplayModel ToDetailDisplayModel(PatientDetailDto dto);
```

**Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/Display/PatientDetailDisplayModel.cs
git add src/Client/Desktop/Modules/LYBT.Desktop.Patients/Mappers/PatientMapper.cs
git commit -m "feat(patients): add PatientDetailDisplayModel for patient view control

OpenSpec: unify-control-data-binding - Phase B.3"
```

---

## Summary

**Phase A Tasks:** A.1-A.5 (Infrastructure State/Options classes)
**Phase B Tasks:** B.1-B.3 (High priority EditModel/DisplayModel)

**Remaining tasks** (to be continued in subsequent plan updates):
- B.4-B.5: Refactor MedicalCaseEditControl, PatientViewControl XAML
- Phase C: Medium priority controls
- Phase D: Low priority controls

---

**Plan complete and saved to `docs/plans/2026-01-16-unify-control-data-binding.md`. Two execution options:**

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**
