# implement-local-mode Tasks

## Overview

- **变更类型**: Feature + Refactor
- **风险等级**: Medium-High
- **总任务数**: 31
- **架构**: DataSource 抽象层 (方案 C)

---

## Phase 1: 基础设施层

### 1.1 创建 LYBT.Desktop.LocalData 项目
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/LYBT.Desktop.LocalData.csproj`
- **变更**:
  - 创建 .NET 8 类库项目
  - 添加到 `LYBT.Desktop.sln`
  - 配置项目引用:
    - `LYBT.Entities`
    - `LYBT.Shared.Models`
    - `LYBT.Desktop.Contracts`
  - 添加 NuGet 包:
    - `Microsoft.EntityFrameworkCore.Sqlite`
    - `Riok.Mapperly`
    - `BCrypt.Net-Next`
- **验证**: 项目编译通过，引用正确

### 1.2 定义 IDataSource 接口族
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/*.cs`
- **变更**:
  - 创建 `IDataSourceBase<TEntity>` 基础接口
  - 创建 `IPatientDataSource` (含 SearchAsync, GetByIdNumberAsync, RestoreAsync, BatchDeleteAsync)
  - 创建 `IHerbDataSource` (含分类过滤重载, ToggleStatusAsync, RestoreAsync)
  - 创建 `IFormulaDataSource` (含 CloneAsync, ToggleStatusAsync, RestoreAsync)
  - 创建 `IMedicalCaseDataSource` (含 SaveAsync, CloseCaseAsync, CancelAsync, QueryAsync)
  - 创建 `IUserDataSource` (含 GetByUsernameAsync, ChangePasswordAsync, ToggleStatusAsync)
- **验证**: 接口定义完整，泛型约束正确

### 1.3 实现 LocalDbContext
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/LocalDbContext.cs`
- **变更**:
  - 继承 `DbContext`
  - 定义 DbSet 属性 (Patients, Users, Herbs, Formulas, MedicalCases, Consultations, Prescriptions)
  - 实现 `OnModelCreating`:
    - 应用 Entity 配置
    - 设置全局查询过滤器 (IsDeleted)
    - **SQLite 适配**: 忽略 RowVersion 字段
    - **SQLite 适配**: decimal → double ValueConverter
  - 实现 `SaveChangesAsync` 审计字段填充
  - 注入 `ICurrentUserProvider` 获取当前用户
- **验证**: DbContext 可正常创建，SQLite 适配正确

### 1.4 实现 DatabaseInitializer + SeedData
- **文件**:
  - `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/DatabaseInitializer.cs`
  - `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/SeedData.cs`
- **变更**:
  - DatabaseInitializer:
    - `InitializeAsync()` 调用 `EnsureCreatedAsync()`
    - 数据库文件路径: `%APPDATA%\LYBTZYZS\lybtzyzs.db`
    - 确保目录存在
  - SeedData:
    - 默认管理员账户 (admin/Admin@123)
    - BCrypt 加密密码
    - 检查是否已存在避免重复创建
- **验证**: 首次运行创建数据库和管理员，二次运行跳过

### 1.5 实现 LocalAuthService
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/LocalAuthService.cs`
- **变更**:
  - 实现 `ILocalAuthService` 接口
  - `ValidateAsync(username, password)`:
    - 查询用户
    - BCrypt.Verify 验证密码
    - 返回 User Entity 或 null
  - `GetCurrentUserAsync()`:
    - 从内存会话获取当前用户
  - 无 JWT Token 逻辑
- **验证**: 本地登录成功，密码验证正确

### 1.6 编译验证 Phase 1
- **命令**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- **验证**: 零编译错误

---

## Phase 2: DataSource 实现

### 远程 DataSource (重构)

### 2.1 RemotePatientDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemotePatientDataSource.cs`
- **变更**:
  - 从 `PatientRepository` 提取 API 调用逻辑
  - 注入 `IPatientApi`
  - 实现所有 `IPatientDataSource` 方法
  - 解包 `ApiResponse<T>`
  - 添加 DTO → Entity 映射 (使用 IDataSourceMapper)
- **依赖**: Phase 1 完成
- **验证**: 远程模式数据获取正常

### 2.2 RemoteHerbDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteHerbDataSource.cs`
- **变更**: 同 2.1，针对 Herb

### 2.3 RemoteFormulaDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteFormulaDataSource.cs`
- **变更**: 同 2.1，针对 Formula

### 2.4 RemoteMedicalCaseDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteMedicalCaseDataSource.cs`
- **变更**:
  - 同 2.1，针对 MedicalCase
  - 特殊处理: SaveAsync 聚合保存
  - 特殊处理: QueryAsync 统一查询

### 2.5 RemoteUserDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/Remote/RemoteUserDataSource.cs`
- **变更**: 同 2.1，针对 User

### 本地 DataSource (新建)

### 2.6 LocalPatientDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalPatientDataSource.cs`
- **变更**:
  - 注入 `LocalDbContext`
  - 实现所有 `IPatientDataSource` 方法
  - EF Core 查询 + LINQ
  - 分页: Skip/Take
  - 搜索: Contains 关键词匹配
- **验证**: 本地 CRUD 操作正常

### 2.7 LocalHerbDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalHerbDataSource.cs`
- **变更**:
  - 同 2.6，针对 Herb
  - 分类过滤 (Category 字段)
  - ToggleStatus 状态切换

### 2.8 LocalFormulaDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalFormulaDataSource.cs`
- **变更**:
  - 同 2.6，针对 Formula
  - CloneAsync: 深拷贝 Formula + FormulaHerbItems
  - Include(f => f.Herbs) 加载关联

### 2.9 LocalMedicalCaseDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalMedicalCaseDataSource.cs`
- **变更**:
  - 同 2.6，针对 MedicalCase
  - Include 加载 Consultation + Prescription + Items
  - SaveAsync: 聚合保存（事务）
  - CloseCaseAsync: 更新状态
  - CancelAsync: 更新状态 + 原因

### 2.10 LocalUserDataSource
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/LocalUserDataSource.cs`
- **变更**:
  - 同 2.6，针对 User
  - GetByUsernameAsync: 用户名查询
  - ChangePasswordAsync: BCrypt 验证 + 更新

### 2.11 编译验证 Phase 2
- **命令**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- **验证**: 零编译错误

---

## Phase 3: Repository 重构

### 3.1 重构 PatientRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
- **变更**:
  - 移除 `RepositoryBase` 继承
  - 移除 `IPatientApi` 依赖
  - 注入 `IPatientDataSource`
  - 保留 `PatientMapper` (Entity ↔ DTO)
  - 实现所有 `IPatientRepository` 方法
  - 映射逻辑: DataSource 返回 Entity → Mapper → DTO
- **验证**: 接口契约不变，功能正常

### 3.2 重构 HerbRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- **变更**: 同 3.1，针对 Herb

### 3.3 重构 FormulaRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Repositories/FormulaRepository.cs`
- **变更**: 同 3.1，针对 Formula

### 3.4 重构 MedicalCaseRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**:
  - 同 3.1，针对 MedicalCase
  - 特殊处理: SaveAsync 聚合映射
  - 特殊处理: QueryAsync 查询参数映射

### 3.5 重构 UserRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`
- **变更**: 同 3.1，针对 User

### 3.6 编译验证 Phase 3
- **命令**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- **验证**: 零编译错误，接口契约不变

---

## Phase 4: 集成与切换

### 4.1 DI 注册框架
- **文件**: `src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs`
- **变更**:
  - 创建 `RegisterDataSources(IContainerRegistry, ConnectionMode)` 扩展方法
  - Remote 模式: 注册 Remote*DataSource
  - Local 模式: 注册 Local*DataSource + LocalDbContext + DatabaseInitializer
  - Repository 统一注册（不依赖模式）

### 4.2 ConnectionMode 选择逻辑激活
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
- **变更**:
  - 移除 "开发中" 对话框显示
  - `SelectLocalModeCommand` 激活本地模式选择
  - 更新 `IApplicationStateService.ConnectionMode`

### 4.3 LoginCoordinator 适配
- **文件**: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`
- **变更**:
  - 检查 `ConnectionMode`
  - Local 模式: 调用 `ILocalAuthService.ValidateAsync`
  - Remote 模式: 保持现有 API 认证逻辑
  - 统一设置用户会话

### 4.4 健康检查适配
- **文件**: `src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs`
- **变更**:
  - 检查 `ConnectionMode`
  - Local 模式: 检查 SQLite 文件是否存在
  - Remote 模式: 保持 API 健康检查

### 4.5 编译验证 Phase 4
- **命令**: `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- **验证**: 零编译错误，本地模式可登录

---

## Phase 5: 测试与文档

### 5.1 单元测试
- **文件**: `tests/Desktop/LYBT.Desktop.LocalData.Tests/*.cs`
- **变更**:
  - `LocalPatientDataSourceTests`
  - `LocalHerbDataSourceTests`
  - `LocalMedicalCaseDataSourceTests`
  - `LocalAuthServiceTests`
  - 使用 InMemory SQLite 测试

### 5.2 集成测试
- **文件**: `tests/Desktop/LYBT.Desktop.Integration.Tests/*.cs`
- **变更**:
  - 本地模式登录流程测试
  - 本地模式 CRUD 端到端测试
  - 远程模式回归测试

### 5.3 文档更新
- **文件**:
  - `docs/architecture/desktop-data-layer.md` (更新)
  - `docs/user-guide/local-mode.md` (新建)
- **变更**:
  - 架构文档: 添加 DataSource 层描述
  - 用户指南: 本地模式使用说明

---

## Phase 6: 数据同步

### 6.1 SyncLog 表设计
- **文件**:
  - `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Entities/SyncLog.cs`
  - `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Configurations/SyncLogConfiguration.cs`
- **变更**:
  - SyncLog Entity: Id, EntityType, EntityId, Operation, Timestamp, IsSynced
  - LocalDbContext 添加 DbSet<SyncLog>

### 6.2 同步 API 端点 (Server 端)
- **文件**:
  - `src/Server/Modules/LYBT.Module.Sync/Controllers/SyncController.cs`
  - `src/Server/Modules/LYBT.Module.Sync/Services/SyncService.cs`
- **变更**:
  - POST /api/v1/sync/upload - 批量上传
  - GET /api/v1/sync/pull?since={timestamp} - 增量拉取

### 6.3 OfflineFirstDataSource 实现
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/OfflineFirstDataSource.cs`
- **变更**:
  - 组合 LocalDataSource + RemoteDataSource
  - 读操作: 先查本地，无数据查远程并缓存
  - 写操作: 本地优先，记录 SyncLog
  - 后台同步队列

### 6.4 同步冲突解决策略
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Sync/ConflictResolver.cs`
- **变更**:
  - 时间戳对比
  - 冲突标记 (ConflictStatus 字段)
  - 手动解决界面 (ViewModel + View)

### 6.5 编译验证 Phase 6
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **验证**: Desktop + Server 编译通过

---

## Dependencies

```
Phase 1 ──────────────────────────────────────┐
                                              │
Phase 2 (依赖 Phase 1) ───────────────────────┼──> Phase 4 (依赖 1,2,3)
                                              │         │
Phase 3 (依赖 Phase 1,2) ─────────────────────┘         │
                                                        ↓
                                               Phase 5 (依赖 4)
                                                        │
                                               Phase 6 (可选，依赖 5)
```

- Phase 1 是所有后续工作的基础
- Phase 2 和 Phase 3 部分可并行，但每模块的 DataSource 必须先于 Repository
- Phase 4 需要 1-3 全部完成
- Phase 5 需要 4 完成
- Phase 6 独立于 1-5，可选实施

---

## Validation Checklist

- [ ] Desktop 解决方案编译通过
- [ ] Server 解决方案编译通过 (Phase 6)
- [ ] 本地模式登录成功
- [ ] 本地模式 Patient CRUD 正常
- [ ] 本地模式 Herb CRUD 正常
- [ ] 本地模式 Formula CRUD 正常
- [ ] 本地模式 MedicalCase CRUD 正常
- [ ] 远程模式回归测试通过
- [ ] 数据同步功能正常 (Phase 6)

---

**生成时间**: 2026-02-03 14:41
**状态**: 完整版 (已完成设计阶段细化)
