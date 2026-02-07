# 测试设计方案 - 集成测试综合文档

## 1. 模块概述

集成测试验证组件间的协作和端到端数据流。

| 模块 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| LYBT.WebAPI.IntegrationTests | ~20 | 40 | +20 |
| LYBT.Desktop.Foundation.IntegrationTests | ~10 | 25 | +15 |
| LYBT.Desktop.LocalData.IntegrationTests | ~15 | 30 | +15 |
| LYBT.Module.Formula.IntegrationTests | ~10 | 20 | +10 |
| **总计** | **~55** | **115** | **+60** |

---

## 2. LYBT.WebAPI.IntegrationTests (+20)

### 2.1 认证端点测试 (8个)

```
Auth_Login_WithValidCredentials_ShouldReturn200
Auth_Login_WithInvalidCredentials_ShouldReturn401
Auth_Logout_WithValidToken_ShouldReturn200
Auth_RefreshToken_WithValidRefreshToken_ShouldReturn200
Auth_RefreshToken_WithExpiredToken_ShouldReturn401
Auth_Protected_WithoutToken_ShouldReturn401
Auth_Protected_WithValidToken_ShouldReturn200
Auth_Protected_WithExpiredToken_ShouldReturn401
```

### 2.2 CRUD 端点测试 (8个)

```
Herbs_GetPaged_ShouldReturnPagedResult
Herbs_Create_WithValidInput_ShouldReturn201
Herbs_Update_WithValidInput_ShouldReturn200
Herbs_Delete_WithExistingId_ShouldReturn204
Patients_GetPaged_ShouldReturnPagedResult
Formulas_GetPaged_ShouldReturnPagedResult
Users_GetPaged_RequiresAdminRole
MedicalCases_GetPaged_ShouldReturnPagedResult
```

### 2.3 同步端点测试 (4个)

```
Sync_GetMetadata_ShouldReturnAllTypes
Sync_Compare_ShouldReturnDifferences
Sync_Upload_ShouldCreateEntities
Sync_Download_ShouldReturnEntities
```

---

## 3. LYBT.Desktop.Foundation.IntegrationTests (+15)

### 3.1 认证流程测试 (6个)

```
AuthenticationFlow_Login_ThenLogout_ShouldWork
AuthenticationFlow_TokenRefresh_ShouldExtendSession
AuthenticationFlow_SessionTimeout_ShouldTriggerLogout
AuthenticationFlow_RememberCredentials_ShouldPersist
AuthenticationFlow_AutoLogin_ShouldWork
AuthenticationFlow_MultipleDevices_ShouldHandleConflicts
```

### 3.2 Token 管理测试 (5个)

```
TokenManager_SetAndGet_ShouldWork
TokenManager_Expiration_ShouldTriggerRefresh
TokenManager_Revocation_ShouldPreventAccess
TokenStorageService_PersistAndRetrieve_ShouldWork
CredentialVault_EncryptDecrypt_ShouldWork
```

### 3.3 应用状态测试 (4个)

```
ApplicationState_ApiHealthCheck_ShouldUpdateStatus
ApplicationState_ConnectionLoss_ShouldNotifyUser
ApplicationState_Reconnection_ShouldRestoreState
ApplicationState_ModeSwitch_ShouldPersist
```

---

## 4. LYBT.Desktop.LocalData.IntegrationTests (+15)

### 4.1 数据源集成测试 (6个)

```
LocalDataSource_CRUD_ShouldPersistToSQLite
LocalDataSource_Query_ShouldFilterCorrectly
LocalDataSource_SoftDelete_ShouldMarkDeleted
LocalDataSource_Restore_ShouldUndelete
LocalDataSource_Transaction_ShouldRollbackOnError
LocalDataSource_ConcurrentAccess_ShouldHandle
```

### 4.2 同步服务测试 (5个)

```
SyncService_Upload_ShouldSendToServer
SyncService_Download_ShouldSaveLocally
SyncService_Compare_ShouldDetectChanges
SyncService_Conflict_ShouldResolve
SyncService_OfflineChanges_ShouldQueueForSync
```

### 4.3 Checksum 一致性测试 (4个)

```
Checksum_LocalAndServer_ShouldMatch
Checksum_HerbFields_ShouldAffectResult
Checksum_AuditFields_ShouldNotAffectResult
Checksum_Consistency_ShouldBeIdempotent
```

---

## 5. LYBT.Module.Formula.IntegrationTests (+10)

### 5.1 验方药材集成测试 (5个)

```
Formula_CreateWithHerbs_ShouldCreateRelations
Formula_UpdateHerbs_ShouldUpdateRelations
Formula_DeleteHerbs_ShouldCascade
Formula_HerbValidation_ShouldMatchSystem
Formula_Clone_ShouldCopyAllRelations
```

### 5.2 跨模块查询测试 (5个)

```
CrossModule_HerbLookup_ShouldFindByName
CrossModule_HerbLookup_ShouldFindByPinyin
CrossModule_FormulaInPrescription_ShouldCheckReference
CrossModule_BatchImport_ShouldMatchHerbs
CrossModule_Export_ShouldIncludeHerbDetails
```

---

## 6. 测试基础设施

### 6.1 WebApplicationFactory 配置

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 替换数据库为 InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

            // 配置测试认证
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
        });
    }
}
```

### 6.2 LocalDbContext 测试配置

```csharp
public class LocalDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    public LocalDbContext Context { get; }

    public LocalDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LocalDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new LocalDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
```

---

## 7. 验收标准

| 指标 | 目标 |
|------|------|
| WebAPI 集成测试数 | 40 |
| Foundation 集成测试数 | 25 |
| LocalData 集成测试数 | 30 |
| Formula 集成测试数 | 20 |
| 总测试数 | 115 |
| 端到端流程覆盖 | 100% |

---

## 8. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 测试基础设施搭建 | 1h |
| 2 | WebAPI 集成测试 (20个) | 3h |
| 3 | Foundation 集成测试 (15个) | 2h |
| 4 | LocalData 集成测试 (15个) | 2h |
| 5 | Formula 集成测试 (10个) | 1.5h |
| 6 | 编译验证和修复 | 1h |
| **总计** | | **~10.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
