# 测试分层策略 - 避免重复，确保覆盖

## 核心原则

```
单元测试 = 隔离测试单个组件的业务逻辑
集成测试 = 验证组件间的协作和数据流
```

**黄金法则**: 不要在两层测试中重复测试相同的逻辑

---

## 1. 测试职责划分

### 1.1 单元测试职责

| 测试对象 | 职责 | Mock 范围 |
|----------|------|----------|
| **Service** | 业务规则、边界条件、异常处理 | Repository, 外部服务 |
| **Repository** | 查询逻辑、过滤条件 | DbContext (InMemory) |
| **Helper/Utility** | 算法正确性、边界值 | 无依赖 |
| **Validator** | 验证规则、错误消息 | 无依赖 |
| **ViewModel** | 命令逻辑、状态转换 | Service, Navigation |

### 1.2 集成测试职责

| 测试对象 | 职责 | 真实组件 |
|----------|------|----------|
| **API Endpoint** | HTTP 流程、认证、序列化 | Controller → Service → Repository → DB |
| **Data Flow** | DI 解析、数据持久化 | DataSource → DbContext → SQLite/SQL Server |
| **Cross-Module** | 模块间协作、事务边界 | 多个 Service 协作 |
| **Authentication** | Token 验证、权限检查 | JWT Handler → Claims |

---

## 2. Sync 模块测试设计

### 2.1 单元测试 (SyncServiceTests)

**测试内容**: 业务逻辑的正确性

```
✅ 应该测试:
- ChecksumHelper 算法正确性 (各字段变更影响)
- CompareAsync 差异算法 (LocalOnly, ServerOnly, Modified 判断)
- UploadAsync 实体处理逻辑 (新建 vs 更新 vs 冲突)
- DeleteAsync 引用检查逻辑 (Mock IHerbService.CheckReferenceAsync)
- 边界条件 (null, empty, 特殊字符)
- 异常处理 (无效类型抛出 ArgumentException)

❌ 不应该测试:
- HTTP 状态码
- 认证/授权
- JSON 序列化格式
- 数据库持久化
```

### 2.2 集成测试 (SyncControllerIntegrationTests)

**测试内容**: 端到端数据流

```
✅ 应该测试:
- API 端点可访问性 (路由正确)
- 认证要求 (无 Token 返回 401)
- 请求验证 (空 EntityIds 返回 400)
- 响应格式 (ApiResponse<T> 结构)
- 数据持久化 (Upload 后 Download 可获取)
- 完整同步流程 (Metadata → Compare → Download/Upload → Verify)

❌ 不应该测试:
- Checksum 算法细节 (单元测试覆盖)
- 每个字段的变更影响 (单元测试覆盖)
- Mock 验证 (集成测试使用真实组件)
```

### 2.3 测试矩阵

| 测试场景 | 单元测试 | 集成测试 | 说明 |
|----------|:--------:|:--------:|------|
| Checksum 相同数据返回相同值 | ✅ | - | 算法测试 |
| Checksum 字段变更影响 | ✅ | - | 算法测试 |
| Compare LocalOnly 判断 | ✅ | - | 逻辑测试 |
| Compare ServerOnly 判断 | ✅ | - | 逻辑测试 |
| Compare Modified 判断 | ✅ | - | 逻辑测试 |
| Upload 新实体创建 | ✅ | ✅ | 单元测试逻辑，集成测试持久化 |
| Upload 冲突处理 | ✅ | - | 逻辑测试 |
| Delete 引用检查 | ✅ | - | Mock 测试 |
| API 认证要求 | - | ✅ | 端点测试 |
| API 请求验证 | - | ✅ | 端点测试 |
| 完整同步流程 | - | ✅ | 端到端测试 |
| 并发上传处理 | - | ✅ | 集成测试 |

---

## 3. 重构后的测试结构

### 3.1 ChecksumHelperTests (单元测试)

```csharp
#region 算法正确性测试
// ComputeHerbChecksum_WithSameData_ShouldReturnSameChecksum
// ComputeHerbChecksum_MultipleCallsSameData_ShouldReturnSame (确定性)
// ComputeHerbChecksum_WithDifferentName_ShouldReturnDifferent
// ComputeHerbChecksum_WithDifferentPrice_ShouldReturnDifferent
// ComputeHerbChecksum_WithDifferentStatus_ShouldReturnDifferent
// ... 每个业务字段一个测试

#region 审计字段排除测试
// ComputeHerbChecksum_WithDifferentCreatedAt_ShouldReturnSame
// ComputeHerbChecksum_WithDifferentUpdatedAt_ShouldReturnSame
// ComputeHerbChecksum_WithDifferentCreatedBy_ShouldReturnSame

#region 边界条件测试
// ComputeHerbChecksum_WithNullName_ShouldHandle
// ComputeHerbChecksum_WithEmptyString_ShouldHandle
// ComputeHerbChecksum_WithSpecialCharacters_ShouldHandle
// ComputeHerbChecksum_WithMaxDecimalPrecision_ShouldHandle

#region 类型路由测试
// ComputeChecksum_WithValidTypes_ShouldRoute
// ComputeChecksum_WithInvalidType_ShouldThrow
```

### 3.2 SyncServiceTests (单元测试)

```csharp
#region GetMetadataAsync 测试 (逻辑验证)
// GetMetadataAsync_WithValidType_ShouldReturnMetadataList
// GetMetadataAsync_WithInvalidType_ShouldReturnFailure
// GetMetadataAsync_ShouldExcludeAuditFieldsFromChecksum (验证元数据正确性)

#region CompareAsync 测试 (核心算法)
// CompareAsync_WithLocalOnlyEntity_ShouldReturnLocalOnlyDiff
// CompareAsync_WithServerOnlyEntity_ShouldReturnServerOnlyDiff
// CompareAsync_WithModifiedEntity_ShouldReturnModifiedDiff
// CompareAsync_WithIdenticalChecksum_ShouldReturnNoDiff
// CompareAsync_WithMixedDiffs_ShouldReturnAllTypes
// CompareAsync_WithEmptyLocalEntities_ShouldReturnAllServerOnly
// CompareAsync_WithInvalidType_ShouldReturnFailure

#region UploadAsync 测试 (业务逻辑)
// UploadAsync_WithNewEntity_ShouldCreate
// UploadAsync_WithExistingEntity_OverwriteTrue_ShouldUpdate
// UploadAsync_WithExistingEntity_OverwriteFalse_ShouldReturnConflict
// UploadAsync_WithInvalidJson_ShouldReturnError
// UploadAsync_WithBatchEntities_ShouldProcessAll
// UploadAsync_WithFormulaAndHerbs_ShouldCreateRelated
// UploadAsync_WithInvalidType_ShouldReturnFailure

#region DownloadAsync 测试 (数据获取)
// DownloadAsync_WithExistingIds_ShouldReturnEntities
// DownloadAsync_WithNonExistentIds_ShouldReturnEmpty
// DownloadAsync_WithMixedIds_ShouldReturnExistingOnly
// DownloadAsync_WithFormulaId_ShouldIncludeHerbs
// DownloadAsync_WithInvalidType_ShouldReturnFailure

#region DeleteAsync 测试 (引用检查)
// DeleteAsync_HerbWithNoReferences_ShouldSoftDelete
// DeleteAsync_HerbWithReferences_ShouldReject
// DeleteAsync_PatientWithNoReferences_ShouldSoftDelete
// DeleteAsync_PatientWithReferences_ShouldReject
// DeleteAsync_Formula_ShouldSoftDeleteDirectly (无引用检查)
// DeleteAsync_AlreadyDeleted_ShouldReject
// DeleteAsync_BatchWithMixedResults_ShouldReportCorrectly
// DeleteAsync_WithInvalidType_ShouldReturnFailure
```

### 3.3 SyncControllerIntegrationTests (集成测试)

```csharp
#region 端点可访问性测试
// GetEntityTypes_WithAuthentication_ShouldReturnTypes
// GetEntityTypes_WithoutAuthentication_ShouldReturn401

#region 请求验证测试
// GetMetadata_WithEmptyEntityType_ShouldReturn400
// Compare_WithNullInput_ShouldReturn400
// Download_WithEmptyEntityIds_ShouldReturn400
// Upload_WithEmptyEntities_ShouldReturn400
// Delete_WithEmptyEntityIds_ShouldReturn400

#region 端到端数据流测试
// FullSyncFlow_Upload_ThenDownload_ShouldReturnSameData
// FullSyncFlow_Upload_ThenCompare_ShouldShowNoChanges
// FullSyncFlow_Upload_ThenDelete_ThenCompare_ShouldShowDeleted

#region 响应格式测试
// ApiResponse_Success_ShouldHaveCorrectStructure
// ApiResponse_Failure_ShouldHaveErrorMessage
```

---

## 4. Desktop 测试分层

### 4.1 单元测试 (LocalPatientDataSourceTests)

```csharp
// GetByIdAsync_WithExistingId_ShouldReturnEntity
// GetByIdAsync_WithNonExistentId_ShouldReturnNull
// GetPagedAsync_WithKeyword_ShouldFilter
// CreateAsync_WithValidData_ShouldCreate
// UpdateAsync_WithChanges_ShouldPersist
// DeleteAsync_ShouldSoftDelete
// RestoreAsync_ShouldRecover
```

### 4.2 集成测试 (DataSourceIntegrationTests)

```csharp
// DI_DataSources_CanBeResolved (DI 容器测试)
// PatientDataSource_CRUD_EndToEnd (完整数据流)
// MultipleDataSources_ShareDbContext (数据隔离)
// DataSource_AfterLogout_ShouldClearData (会话管理)
```

---

## 5. 实施指南

### 5.1 如何决定测试归属

```
问题1: 这个测试是否需要真实的 HTTP 请求?
  → 是 → 集成测试
  → 否 → 继续

问题2: 这个测试是否需要验证多个组件的协作?
  → 是 → 集成测试
  → 否 → 继续

问题3: 这个测试是否可以通过 Mock 完全隔离?
  → 是 → 单元测试
  → 否 → 集成测试
```

### 5.2 避免重复的检查清单

- [ ] 单元测试不测试 HTTP 状态码
- [ ] 集成测试不测试算法细节
- [ ] 单元测试不测试 DI 解析
- [ ] 集成测试不使用 Mock
- [ ] 边界条件只在单元测试中覆盖
- [ ] 端到端流程只在集成测试中覆盖

### 5.3 测试文件命名约定

```
单元测试:
  {ClassName}Tests.cs
  例: SyncServiceTests.cs, ChecksumHelperTests.cs

集成测试:
  {ClassName}IntegrationTests.cs
  例: SyncControllerIntegrationTests.cs, DataSourceIntegrationTests.cs
```

---

## 6. 覆盖率目标

| 层级 | 单元测试覆盖率 | 集成测试场景 |
|------|---------------|--------------|
| Service | 80%+ | 核心端到端流程 |
| Repository | 70%+ | DI 解析、数据持久化 |
| Helper | 90%+ | - |
| Controller | 20%+ | 全部端点测试 |
| DataSource | 70%+ | CRUD 端到端 |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
