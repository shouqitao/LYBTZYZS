# implement-local-mode

## Why

### 背景

当前 LYBTZYZS Desktop 客户端仅支持**远程模式**，必须连接 WebAPI Server 才能使用。这在以下场景存在问题：

1. **网络故障** - 诊所网络中断时无法使用系统
2. **外出诊疗** - 医生外出时无法访问患者数据
3. **单机部署** - 小型诊所不需要 Server 端的复杂部署
4. **开发调试** - 开发时需要同时启动 Server 端

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| LoginView | 功能缺失 | 本地模式按钮显示"开发中" | 可选择并使用本地模式 |
| Desktop Repository | 职责混乱 | 混合数据获取与 API 解包 | 职责分离 |
| 数据访问架构 | 架构限制 | Repository 直接耦合 Refit API | DataSource 抽象可切换 |
| 认证流程 | 架构限制 | 依赖 Server 端 JWT 认证 | 本地模式使用简化认证 |

### 架构分析

当前 Repository 存在**职责混乱**问题：
- 调用 Refit API 获取数据
- 解包 ApiResponse 并处理错误
- 执行 Entity → DTO 映射

这违反了**单一职责原则**，导致本地模式实现困难。

### 解决方案

引入 **DataSource 抽象层**：
- **IDataSource** - 数据源契约，返回 Entity
- **RemoteDataSource** - 远程数据源（Refit + HTTP）
- **LocalDataSource** - 本地数据源（EF Core + SQLite）
- **Repository** - 统一实现，负责 Entity → DTO 映射

### 影响分析

- **变更类型**: Feature + Refactor（新增功能 + 架构重构）
- **变更范围**: Desktop 端全面重构 + 新建本地数据层
- **风险等级**: Medium-High（重构所有 Repository，但保持接口不变）

## What Changes

### Phase 1: 基础设施层 (5 个任务)

创建 `LYBT.Desktop.LocalData` 项目和 `IDataSource` 接口族：

1. **创建 LYBT.Desktop.LocalData 项目**
   - 引用 `LYBT.Entities` 复用 Entity 定义
   - 引用 `LYBT.Shared.Models` 复用 DTO 定义
   - 添加 `Microsoft.EntityFrameworkCore.Sqlite`

2. **定义 IDataSource 接口族**（Contracts 项目）
   - `IPatientDataSource`
   - `IHerbDataSource`
   - `IFormulaDataSource`
   - `IMedicalCaseDataSource`
   - `IUserDataSource`

3. **实现 LocalDbContext**
   - SQLite 数据库上下文
   - 适配 SQLite 限制（忽略 RowVersion，decimal 转换）
   - 实现本地审计字段填充

4. **实现 DatabaseInitializer + SeedData**
   - EnsureCreated 创建数据库
   - 种子数据（默认管理员账户）
   - 数据库文件存储在 `%APPDATA%\LYBTZYZS\lybtzyzs.db`

5. **实现 LocalAuthService**
   - BCrypt 密码验证
   - 无 JWT Token（本地单用户场景）

### Phase 2: DataSource 实现 (10 个任务)

**远程 DataSource（重构现有代码）**

将现有 Repository 的 API 调用逻辑提取到 RemoteDataSource：

- `RemotePatientDataSource` - 调用 IPatientApi，解包 ApiResponse
- `RemoteHerbDataSource`
- `RemoteFormulaDataSource`
- `RemoteMedicalCaseDataSource`
- `RemoteUserDataSource`

**本地 DataSource（新实现）**

使用 EF Core 直接访问 SQLite：

- `LocalPatientDataSource`
- `LocalHerbDataSource`
- `LocalFormulaDataSource`
- `LocalMedicalCaseDataSource`
- `LocalUserDataSource`

### Phase 3: Repository 重构 (5 个任务)

重构所有 Repository，依赖 IDataSource 而非 IApi：

- `PatientRepository` → 依赖 `IPatientDataSource`
- `HerbRepository` → 依赖 `IHerbDataSource`
- `FormulaRepository` → 依赖 `IFormulaDataSource`
- `MedicalCaseRepository` → 依赖 `IMedicalCaseDataSource`
- `UserRepository` → 依赖 `IUserDataSource`

**关键变更**：
- 移除 API 调用和 ApiResponse 解包逻辑
- 保留 Entity → DTO 映射逻辑
- Repository 只有一套实现，通过 DI 注入不同 DataSource

### Phase 4: 集成与切换 (4 个任务)

1. **DI 注册框架**
   - 根据 ConnectionMode 注册对应的 DataSource
   - Repository 统一注册

2. **ConnectionMode 选择逻辑激活**
   - 移除"开发中"对话框
   - 启用本地模式选择

3. **LoginCoordinator 适配**
   - 本地模式使用 LocalAuthService
   - 远程模式保持现有逻辑

4. **健康检查适配**
   - 本地模式检查 SQLite 文件可用性
   - 远程模式保持 API 健康检查

### Phase 5: 测试与文档 (3 个任务)

1. **单元测试**
   - DataSource 单元测试
   - Repository 单元测试
   - 本地认证测试

2. **集成测试**
   - 端到端本地模式流程
   - 远程模式回归测试

3. **文档更新**
   - 设计文档
   - 用户指南

### Phase 6: 数据同步 (4 个任务)

1. **SyncLog 表设计**
   - 变更追踪表结构
   - 本地数据库添加同步元数据

2. **同步 API 端点（Server 端）**
   - 批量同步上传
   - 增量数据拉取

3. **OfflineFirstDataSource 实现**
   - 先查本地，后查远程
   - 写操作本地优先，后台同步

4. **同步冲突解决策略**
   - 时间戳对比
   - 冲突标记与手动解决

## Architecture

### 目标架构图

```
┌─────────────────────────────────────────────────────────┐
│                    ViewModel 层                          │
│              (UI 绑定、用户交互)                          │
└─────────────────────────┬───────────────────────────────┘
                          │ 依赖
┌─────────────────────────▼───────────────────────────────┐
│                    Service 层                            │
│           (业务编排、跨聚合协调)                          │
└─────────────────────────┬───────────────────────────────┘
                          │ 依赖
┌─────────────────────────▼───────────────────────────────┐
│               IRepository 接口                           │
│         (业务数据契约，返回 DTO)                         │
└─────────────────────────┬───────────────────────────────┘
                          │ 实现
┌─────────────────────────▼───────────────────────────────┐
│                Repository 实现                           │
│     (数据组装、Entity→DTO 映射、缓存策略)                │  ← 统一实现
└─────────────────────────┬───────────────────────────────┘
                          │ 依赖
┌─────────────────────────▼───────────────────────────────┐
│               IDataSource 接口                           │
│      (数据源契约，返回 Entity 或原始数据)                │  ← 新增抽象层
└──────────────┬─────────────────────────┬────────────────┘
               │                         │
┌──────────────▼──────────┐  ┌───────────▼───────────────┐
│   RemoteDataSource      │  │    LocalDataSource        │
│   (Refit + HTTP)        │  │    (EF Core + SQLite)     │
└─────────────────────────┘  └───────────────────────────┘
```

### 与当前架构对比

```
[当前架构 - Repository 直接耦合 API]
ViewModel → Service → IRepository → Repository → IApi (Refit) → HTTP → Server

[新架构 - DataSource 抽象层]
ViewModel → Service → IRepository → Repository → IDataSource → Remote/Local DataSource
                                        ↓                              ↓
                                   统一映射逻辑                    数据获取策略
```

### 项目结构

```
src/Client/Desktop/
├── Core/
│   ├── LYBT.Desktop.Contracts/
│   │   └── DataSources/                    # 新增目录
│   │       ├── IPatientDataSource.cs
│   │       ├── IHerbDataSource.cs
│   │       ├── IFormulaDataSource.cs
│   │       ├── IMedicalCaseDataSource.cs
│   │       └── IUserDataSource.cs
│   │
│   ├── LYBT.Desktop.Infrastructure/
│   │   └── DataSources/                    # 新增目录
│   │       └── Remote/
│   │           ├── RemotePatientDataSource.cs
│   │           ├── RemoteHerbDataSource.cs
│   │           └── ...
│   │
│   └── LYBT.Desktop.LocalData/             # 新建项目
│       ├── Context/
│       │   └── LocalDbContext.cs
│       ├── Initialization/
│       │   ├── DatabaseInitializer.cs
│       │   └── SeedData.cs
│       ├── DataSources/
│       │   ├── LocalPatientDataSource.cs
│       │   ├── LocalHerbDataSource.cs
│       │   └── ...
│       └── Services/
│           └── LocalAuthService.cs
│
├── Modules/
│   └── LYBT.Desktop.Xxx/
│       └── Repositories/
│           └── XxxRepository.cs            # 重构：依赖 IDataSource
│
└── Shell/
    └── Extensions/
        └── ServiceCollectionExtensions.cs  # 修改: DataSource 注册
```

### 关键接口设计

```csharp
/// <summary>
/// 数据源抽象接口 - 返回 Entity
/// </summary>
public interface IPatientDataSource
{
    Task<Patient?> GetByIdAsync(Guid id);
    Task<(List<Patient> Items, int Total)> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<Patient> CreateAsync(Patient entity);
    Task<Patient> UpdateAsync(Patient entity);
    Task DeleteAsync(Guid id);
    Task<List<Patient>> SearchAsync(string keyword);
    Task<Patient?> GetByIdNumberAsync(string idNumber);
}

/// <summary>
/// 重构后的 Repository - 依赖 IDataSource
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly IPatientDataSource _dataSource;
    private readonly IMapper _mapper;

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id)
    {
        var entity = await _dataSource.GetByIdAsync(id);
        return entity == null ? null : _mapper.ToDetailDto(entity);
    }
}
```

### DI 注册示例

```csharp
public static void RegisterDataSources(this IContainerRegistry registry, ConnectionMode mode)
{
    if (mode == ConnectionMode.Remote)
    {
        registry.Register<IPatientDataSource, RemotePatientDataSource>();
        registry.Register<IHerbDataSource, RemoteHerbDataSource>();
        // ...
    }
    else
    {
        registry.Register<IPatientDataSource, LocalPatientDataSource>();
        registry.Register<IHerbDataSource, LocalHerbDataSource>();
        // ...
    }

    // Repository 统一注册（不变）
    registry.Register<IPatientRepository, PatientRepository>();
    registry.Register<IHerbRepository, HerbRepository>();
    // ...
}
```

## Impact

- **新建文件**: ~25 个（LocalData 项目 + DataSource 接口 + Remote DataSource）
- **修改文件**: ~10 个（Repository 重构 + DI 注册）
- **风险等级**: Medium-High
- **测试要求**:
  - DataSource 单元测试（Remote + Local）
  - Repository 单元测试
  - 端到端本地模式流程集成测试
  - 远程模式回归测试（确保无影响）

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 重构范围较大 | 分 Phase 执行，每 Phase 编译验证 |
| API 解包逻辑迁移 | 逐个 DataSource 迁移，保持测试覆盖 |
| SQLite 与 SQL Server 行为差异 | Entity 配置适配 + 充分测试 |
| MedicalCase 聚合根复杂度 | 参考 Server 端实现，逐步验证 |
| 数据丢失风险 | 默认存储在 %APPDATA%，卸载应用不删除数据 |

## Design Benefits

| 维度 | 说明 |
|------|------|
| **职责分离** | Repository 负责业务映射，DataSource 负责数据获取 |
| **代码复用** | Repository 只有一套实现，映射逻辑集中管理 |
| **扩展性** | 可组合多数据源（缓存、离线优先、同步等） |
| **测试性** | 可独立 Mock 各层进行测试 |

## Key Constraints

1. **Repository 统一** - 只有一套 Repository 实现，不因模式而异
2. **DataSource 可替换** - 通过 DI 注入不同实现
3. **映射集中** - Entity → DTO 映射只在 Repository 层
4. **Service/ViewModel 零改动** - 上层完全不感知数据源变化

## SQLite 适配要点

| 问题 | 方案 |
|------|------|
| RowVersion | 本地单用户，忽略并发控制 |
| decimal 精度 | ValueConverter 转 double |
| 审计字段 | LocalDbContext 重写 SaveChangesAsync |
| 全局过滤器 | SQLite 完全支持 HasQueryFilter |

## References

- 规划文档: `task_plan.md`, `findings.md`, `progress.md`
- 架构分析: Sequential Thinking 深度分析
- SQLite 限制: [Microsoft Docs - SQLite EF Core Limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)

---

**提案版本**: v2.0 (DataSource 抽象架构)
**更新日期**: 2026-02-03
