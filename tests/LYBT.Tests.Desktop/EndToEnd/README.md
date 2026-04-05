# LYBT Desktop E2E 测试文档

## 概述

本文档描述 LYBT 凌隐宝堂中医诊所管理系统的 Desktop E2E（端到端）测试套件，包含 74 个测试用例，覆盖 9 个 API 模块。

## 测试架构

```
tests/LYBT.Tests.Desktop/EndToEnd/
├── Infrastructure/
│   ├── WebApiE2ETestBase.cs              # 测试基类（DI、Refit 客户端、Token 管理）
│   └── AuthenticationDelegatingHandler.cs # 认证委托处理程序
├── Foundation/
│   ├── AuthTests.cs                      # 认证测试（5 个）
│   └── HealthCheckTests.cs               # 健康检查测试（2 个）
└── Modules/
    ├── UserTests.cs                      # 用户管理测试（9 个）
    ├── PatientTests.cs                   # 患者管理测试（9 个）
    ├── HerbTests.cs                      # 药材管理测试（11 个）
    ├── FormulaTests.cs                   # 方剂管理测试（10 个）
    ├── MedicalCaseTests.cs               # 医案管理测试（10 个）
    ├── SyncTests.cs                      # 同步功能测试（8 个）
    └── RegistrationTests.cs              # 挂号管理测试（10 个）
```

## 测试统计

| 模块 | 测试数 | 主要功能 |
|------|--------|----------|
| Auth | 5 | 登录、验证、刷新令牌、登出 |
| Health | 2 | 健康检查端点 |
| User | 9 | CRUD、切换状态、批量删除、关键词搜索 |
| Patient | 9 | CRUD、导出模板、批量删除、关键词搜索 |
| Herb | 11 | CRUD、分类过滤、导出模板、切换状态 |
| Formula | 10 | CRUD、克隆、切换状态、关键词搜索 |
| MedicalCase | 10 | CRUD、会诊、处方、权限、待处理案件 |
| Sync | 8 | 比较、上传、下载、删除、完整工作流 |
| Registration | 10 | 创建挂号、队列查询、接诊、取消挂号、完整生命周期 |
| **总计** | **74** | |

## 测试基类

### WebApiE2ETestBase

所有 E2E 测试的基类，提供：

- **依赖注入容器**：使用 `Microsoft.Extensions.DependencyInjection`
- **Refit 客户端**：自动注册所有 API 接口
- **Token 管理**：登录后自动同步 Token 到 `TokenHolder`
- **API 客户端属性**：`AuthApi`, `UserApi`, `PatientApi`, `HerbApi`, `FormulaApi`, `MedicalCaseApi`, `SyncApi`, `RegistrationApi`

**关键方法**：
```csharp
protected async Task<LoginResponse> LoginAsSysadminAsync()
```

### AuthenticationDelegatingHandler

处理 HTTP 请求的认证头：
- 跳过 `/auth/login` 和 `/auth/register` 路径
- 其他请求自动添加 `Authorization: Bearer {token}` 头

### TokenHolder

单例模式，用于在测试基类和委托处理程序之间共享访问令牌。

## 配置

### appsettings.Test.json

```json
{
  "WebAPI": {
    "BaseUrl": "https://localhost:5001"
  },
  "TestCredentials": {
    "Username": "sysadmin",
    "Password": "TestAdmin2025@"
  }
}
```

## 测试模式

### 标准测试结构

```csharp
public class ExampleTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;
    
    public ExampleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "ExampleManagement")]
    public async Task Method_Scenario_ExpectedResult()
    {
        // Arrange
        await LoginAsSysadminAsync();
        
        // Act
        var response = await SomeApi.SomeMethodAsync(...);
        
        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
    }
}
```

### API 响应模式

- **成功响应**：`response.Success == true`，数据在 `response.Data`
- **失败响应**：`response.Success == false`，错误信息在 `response.Message`
- **分页结果**：`PagedResult<T>` 包含 `Items`, `TotalCount`, `CurrentPage`, `PageSize`
- **文件导出**：返回 `HttpResponseMessage`，使用 `IsSuccessStatusCode` 验证

### 生命周期测试模式

每个模块都有完整的生命周期测试，验证：
1. 创建实体
2. 查询详情
3. 更新实体
4. 切换状态（如适用）
5. 删除实体
6. 恢复实体

## 各模块测试详情

### AuthTests (Foundation)

| 测试名称 | 描述 |
|----------|------|
| Login_WithValidCredentials_ShouldReturnToken | 有效凭据登录返回令牌 |
| Login_WithInvalidCredentials_ShouldReturnUnauthorized | 无效凭据返回未授权 |
| ValidateToken_AfterLogin_ShouldReturnUserInfo | 验证令牌返回用户信息 |
| RefreshToken_WithValidToken_ShouldReturnNewToken | 刷新令牌返回新令牌 |
| Logout_AfterLogin_ShouldSucceed | 登出成功 |

### HealthCheckTests (Foundation)

| 测试名称 | 描述 |
|----------|------|
| HealthEndpoint_ShouldReturnHealthy | 健康端点返回健康状态 |
| HealthDetailedEndpoint_ShouldReturnDatabaseStatus | 详细健康端点返回数据库状态 |

### UserTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreateUser_ValidInput_ReturnsCreatedUser | 创建用户 |
| GetUserById_ExistingUser_ReturnsUserDetail | 根据 ID 获取用户 |
| UpdateUser_ValidInput_ReturnsUpdatedUser | 更新用户 |
| GetUsers_WithPagination_ReturnsPagedResult | 分页获取用户列表 |
| ToggleStatus_EnabledUser_DisablesUser | 切换用户状态 |
| DeleteAndRestore_User_CompletesSuccessfully | 删除并恢复用户 |
| BatchDelete_MultipleUsers_ReturnsOperationResult | 批量删除用户 |
| GetUsers_WithKeyword_FiltersResults | 关键词搜索用户 |
| UserFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed | 完整生命周期 |

### PatientTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreatePatient_ValidInput_ReturnsCreatedPatient | 创建患者 |
| GetPatientById_ExistingPatient_ReturnsDetail | 根据 ID 获取患者 |
| UpdatePatient_ValidInput_ReturnsUpdatedPatient | 更新患者 |
| GetPatients_WithPagination_ReturnsPagedResult | 分页获取患者列表 |
| DeleteAndRestore_Patient_CompletesSuccessfully | 删除并恢复患者 |
| BatchDelete_MultiplePatients_ReturnsOperationResult | 批量删除患者 |
| GetPatients_WithKeyword_FiltersResults | 关键词搜索患者 |
| ExportTemplate_ReturnsFileResponse | 导出患者模板 |
| PatientFullLifecycle_CreateUpdateDeleteRestore_AllSucceed | 完整生命周期 |

### HerbTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreateHerb_ValidInput_ReturnsCreatedHerb | 创建药材 |
| GetHerbById_ExistingHerb_ReturnsDetail | 根据 ID 获取药材 |
| UpdateHerb_ValidInput_ReturnsUpdatedHerb | 更新药材 |
| GetHerbs_WithPagination_ReturnsPagedResult | 分页获取药材列表 |
| ToggleStatus_EnabledHerb_TogglesSuccessfully | 切换药材状态 |
| DeleteAndRestore_Herb_CompletesSuccessfully | 删除并恢复药材 |
| BatchDelete_MultipleHerbs_ReturnsOperationResult | 批量删除药材 |
| GetHerbs_WithKeyword_FiltersResults | 关键词搜索药材 |
| GetHerbs_WithCategory_FiltersResults | 分类过滤药材 |
| ExportTemplate_ReturnsFileResponse | 导出药材模板 |
| HerbFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed | 完整生命周期 |

### FormulaTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreateFormula_ValidInput_ReturnsCreatedFormula | 创建方剂 |
| GetFormulaById_ExistingFormula_ReturnsDetail | 根据 ID 获取方剂 |
| UpdateFormula_ValidInput_ReturnsUpdatedFormula | 更新方剂 |
| GetFormulas_WithPagination_ReturnsPagedResult | 分页获取方剂列表 |
| CloneFormula_ExistingFormula_ReturnsClonedFormula | 克隆方剂 |
| ToggleStatus_EnabledFormula_TogglesSuccessfully | 切换方剂状态 |
| DeleteAndRestore_Formula_CompletesSuccessfully | 删除并恢复方剂 |
| BatchDelete_MultipleFormulas_ReturnsOperationResult | 批量删除方剂 |
| GetFormulas_WithKeyword_FiltersResults | 关键词搜索方剂 |
| FormulaFullLifecycle_CreateCloneToggleDeleteRestore_AllSucceed | 完整生命周期 |

### MedicalCaseTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreateMedicalCase_WithConsultation_ReturnsCreatedCase | 创建带会诊的医案 |
| GetMedicalCaseById_ExistingCase_ReturnsDetail | 根据 ID 获取医案 |
| GetMedicalCases_WithPagination_ReturnsPagedResult | 分页获取医案列表 |
| SaveMedicalCase_UpdateConsultation_Succeeds | 保存更新会诊 |
| SaveMedicalCase_WithPrescription_Succeeds | 保存带处方的医案 |
| GetPendingCases_ReturnsListSuccessfully | 获取待处理医案 |
| GetPermissions_ExistingCase_ReturnsPermissions | 获取医案权限 |
| DeleteMedicalCase_ExistingCase_Succeeds | 删除医案 |
| BatchDelete_MultipleCases_ReturnsOperationResult | 批量删除医案 |
| MedicalCaseFullLifecycle_CreateSavePrescriptionClose_AllSucceed | 完整生命周期 |

### SyncTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| GetEntityTypes_ReturnsSupportedTypes | 获取支持的实体类型 |
| GetMetadata_WithHerbType_ReturnsMetadataList | 获取药材元数据 |
| Compare_WithEmptyLocalList_ReturnsServerOnlyDiffs | 比较空本地列表 |
| Compare_WithSampleLocalData_ReturnsDiffResult | 比较样本数据 |
| Upload_WithInvalidEntity_ReturnsErrorResult | 上传无效实体 |
| Download_WithEmptyList_ReturnsEmptyResult | 下载空列表 |
| Delete_WithNonExistentIds_ReturnsEmptySuccess | 删除不存在的 ID |
| SyncFullWorkflow_CompareUploadDownloadDelete_Succeeds | 完整同步工作流 |

### RegistrationTests (Modules)

| 测试名称 | 描述 |
|----------|------|
| CreateRegistration_ValidInput_ReturnsCreatedRegistration | 创建挂号 |
| GetRegistrationById_ExistingRegistration_ReturnsDetail | 根据 ID 获取挂号详情 |
| GetRegistrations_WithPagination_ReturnsPagedResult | 分页获取挂号列表 |
| GetQueue_WithDoctorFilter_ReturnsWaitingList | 按医生筛选获取等待队列 |
| GetQueue_WithoutDoctorFilter_ReturnsAllWaitingList | 获取全部等待队列 |
| StartVisit_WaitingRegistration_ChangesToInProgress | 接诊：Waiting -> InProgress |
| CancelRegistration_WaitingRegistration_CancelsSuccessfully | 取消挂号 |
| GetRegistrations_WithKeyword_FiltersResults | 关键词搜索挂号 |
| RegistrationFullLifecycle_CreateStartVisitCancel_AllSucceed | 完整生命周期测试 |
| RegistrationFullLifecycle_ReceptionistFlow_Succeeds | 前台挂号流程测试 |

## 运行测试

### 前提条件

1. WebAPI 服务器必须运行在配置的地址（默认 https://localhost:5001）
2. 数据库已初始化并包含测试数据
3. 测试用户 `sysadmin` 存在且密码为 `TestAdmin2025@`

### 运行所有 E2E 测试

```bash
dotnet test tests/LYBT.Tests.Desktop --filter "Category=E2E"
```

### 运行特定模块测试

```bash
# 用户管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=UserManagement"

# 患者管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=PatientManagement"

# 药材管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=HerbManagement"

# 方剂管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=FormulaManagement"

# 医案管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=MedicalCaseManagement"

# 同步功能测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=SyncManagement"

# 挂号管理测试
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=RegistrationManagement"
```

### 运行 Foundation 测试

```bash
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=Authentication"
dotnet test tests/LYBT.Tests.Desktop --filter "Phase=HealthCheck"
```

## 故障排除

### 连接被拒绝错误

```
System.Net.Http.HttpRequestException : 由于目标计算机积极拒绝，无法连接。 (localhost:5001)
```

**原因**：WebAPI 服务器未运行

**解决**：
```bash
cd src/Server/Services/LYBT.WebAPI
dotnet run
```

### Token 无效错误

**原因**：`TokenHolder` 未正确同步

**检查**：
1. 确认 `LoginAsSysadminAsync()` 成功返回
2. 确认 `TokenHolderInstance.AccessToken` 已设置
3. 检查 `AuthenticationDelegatingHandler` 是否正确注入 `TokenHolder`

### 配置未找到错误

**原因**：`appsettings.Test.json` 未复制到输出目录

**解决**：
确保 `.csproj` 文件包含：
```xml
<ItemGroup>
  <Content Include="appsettings.Test.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## 最佳实践

1. **每个测试独立**：测试之间不共享状态，每个测试都重新登录
2. **使用 ITestOutputHelper**：记录测试日志便于调试
3. **清理测试数据**：生命周期测试会在最后删除创建的实体
4. **使用 FluentAssertions**：提供清晰的断言失败信息
5. **标记 Trait**：使用 `Category` 和 `Phase` 便于筛选测试

## 扩展测试

添加新模块测试的步骤：

1. 在 `Modules` 目录创建 `{Module}Tests.cs`
2. 继承 `WebApiE2ETestBase`
3. 注入 `ITestOutputHelper`
4. 使用 `[Fact]` 和 `[Trait]` 标记测试方法
5. 调用 `await LoginAsSysadminAsync()` 进行认证
6. 使用对应的 API 客户端进行测试

## 参考

- **Refit**: https://github.com/reactiveui/refit
- **xUnit**: https://xunit.net/
- **FluentAssertions**: https://fluentassertions.com/
