# 测试设计方案 - LYBT.Desktop.Infrastructure.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.Infrastructure.Tests/` |
| **现有测试数** | 40 |
| **目标测试数** | 90 |
| **新增测试数** | +50 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 Services (23个类)

| 服务类 | 现有测试 | 目标测试 | 新增 |
|--------|----------|----------|------|
| ApplicationTickService | 8 | 8 | 0 |
| UserActivityTracker | 5 | 5 | 0 |
| NotificationService | 0 | 8 | +8 |
| ErrorHandler | 0 | 6 | +6 |
| SelectionService | 0 | 5 | +5 |
| SearchService | 0 | 5 | +5 |
| PaginationService | 0 | 6 | +6 |
| LoadingStateManager | 0 | 4 | +4 |
| ClinicSettingsService | 0 | 4 | +4 |

### 2.2 DataSources

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| RemoteUserDataSource | 0 | 8 | +8 |
| RemotePatientDataSource | 0 | 6 | +6 |
| RemoteHerbDataSource | 0 | 6 | +6 |
| RemoteFormulaDataSource | 0 | 6 | +6 |
| RemoteMedicalCaseDataSource | 0 | 6 | +6 |

### 2.3 Mappers

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| UserDataSourceMapper | 0 | 3 | +3 |
| PatientDataSourceMapper | 0 | 3 | +3 |
| HerbDataSourceMapper | 0 | 3 | +3 |
| FormulaDataSourceMapper | 0 | 3 | +3 |
| MedicalCaseDataSourceMapper | 0 | 3 | +3 |

---

## 3. NotificationService 测试设计 (8个)

```
ShowInfo_ShouldRaiseNotificationRequested
ShowSuccess_ShouldRaiseNotificationRequested
ShowWarning_ShouldRaiseNotificationRequested
ShowError_ShouldRaiseNotificationRequested
ShowConfirmAsync_WithYes_ShouldReturnTrue
ShowConfirmAsync_WithNo_ShouldReturnFalse
ShowLoading_ShouldSetLoadingState
HideLoading_ShouldClearLoadingState
```

---

## 4. ErrorHandler 测试设计 (6个)

```
HandleAsync_WithValidationException_ShouldReturnValidationError
HandleAsync_WithAuthException_ShouldCallHandleAuthError
HandleAsync_WithGenericException_ShouldReturnGenericError
HandleAuthError_WithExpiredToken_ShouldNavigateToLogin
HandleAuthError_WithInvalidToken_ShouldShowError
HandleAsync_ShouldLogException
```

---

## 5. SelectionService 测试设计 (5个)

```
Select_ShouldSetSelectedItem
Select_ShouldRaiseSelectionChanged
ClearSelection_ShouldClearSelectedItem
ClearSelection_ShouldRaiseSelectionChanged
Select_WithSameItem_ShouldNotRaiseEvent
```

---

## 6. SearchService 测试设计 (5个)

```
SearchAsync_WithKeyword_ShouldFilter
SearchAsync_WithEmptyKeyword_ShouldReturnAll
ApplyFilter_ShouldFilterResults
ApplyFilter_WithMultipleFilters_ShouldApplyAll
SearchAsync_ShouldRaiseSearchCompleted
```

---

## 7. PaginationService 测试设计 (6个)

```
GoToPage_WithValidPage_ShouldNavigate
GoToPage_WithInvalidPage_ShouldNotNavigate
NextPage_ShouldIncrementPage
PreviousPage_ShouldDecrementPage
NextPage_OnLastPage_ShouldNotNavigate
PreviousPage_OnFirstPage_ShouldNotNavigate
```

---

## 8. Remote DataSource 测试设计 (32个)

### 8.1 RemoteUserDataSource (8个)

```
GetByIdAsync_WithExistingId_ShouldReturnUser
GetByIdAsync_WithNonExistentId_ShouldReturnNull
CreateAsync_WithValidInput_ShouldCreate
UpdateAsync_WithExistingId_ShouldUpdate
DeleteAsync_WithExistingId_ShouldDelete
GetPagedAsync_ShouldReturnPagedResult
GetPagedAsync_WithApiError_ShouldThrow
CreateAsync_WithApiError_ShouldThrow
```

### 8.2 其他 DataSource (各6个，共24个)

每个 DataSource 测试相同的模式：
- GetByIdAsync 成功/失败
- CreateAsync 成功/失败
- UpdateAsync 成功/失败
- DeleteAsync 成功/失败
- GetPagedAsync 成功/失败
- API错误处理

---

## 9. Mapper 测试设计 (15个)

### 每个 Mapper 3个测试

```
ToEntity_ShouldMapAllProperties
ToDto_ShouldMapAllProperties
RoundTrip_ShouldPreserveData
```

---

## 10. 测试数据设计

### 10.1 Mock API 响应

```csharp
public static class MockApiResponses
{
    public static ApiResponse<UserDetailDto> CreateUserResponse(
        UserDetailDto? user = null)
    {
        return new ApiResponse<UserDetailDto>
        {
            Success = user != null,
            Data = user,
            Message = user != null ? "Success" : "Not found"
        };
    }

    public static ApiResponse<PagedResult<UserListDto>> CreatePagedResponse(
        List<UserListDto>? items = null,
        int totalCount = 0)
    {
        return new ApiResponse<PagedResult<UserListDto>>
        {
            Success = true,
            Data = new PagedResult<UserListDto>
            {
                Items = items ?? new List<UserListDto>(),
                TotalCount = totalCount
            }
        };
    }
}
```

---

## 11. Mock 策略

```csharp
public class RemoteUserDataSourceTests
{
    private readonly Mock<IUserApi> _apiMock;
    private readonly RemoteUserDataSource _sut;

    public RemoteUserDataSourceTests()
    {
        _apiMock = new Mock<IUserApi>();

        // 默认: API 返回成功
        _apiMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockApiResponses.CreateUserResponse(new UserDetailDto()));

        _sut = new RemoteUserDataSource(
            _apiMock.Object,
            NullLogger<RemoteUserDataSource>.Instance);
    }
}
```

---

## 12. 验收标准

| 指标 | 目标 |
|------|------|
| Services 测试数 | 51 |
| DataSources 测试数 | 32 |
| Mappers 测试数 | 15 |
| 总测试数 | 98 |
| 全部测试通过 | 100% |

---

## 13. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | NotificationService 测试 (8个) | 20min |
| 2 | ErrorHandler 测试 (6个) | 20min |
| 3 | SelectionService 测试 (5个) | 15min |
| 4 | SearchService 测试 (5个) | 15min |
| 5 | PaginationService 测试 (6个) | 15min |
| 6 | LoadingStateManager 测试 (4个) | 10min |
| 7 | RemoteDataSource 测试 (32个) | 60min |
| 8 | Mapper 测试 (15个) | 30min |
| 9 | 编译验证和修复 | 15min |
| **总计** | | **~3.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
