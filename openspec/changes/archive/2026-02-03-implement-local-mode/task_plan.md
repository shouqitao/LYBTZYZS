# Task Plan: WPF 本地模式开发 (DataSource 抽象架构)

> 状态: **CONFIRMED** - 所有决策已确认，准备创建 OpenSpec 提案
> 创建时间: 2026-02-03 11:23
> 最后更新: 2026-02-03 14:05
> 架构方案: **C - DataSource 抽象层** (设计最优解)

## 目标

基于当前远程模式架构，通过引入 **DataSource 抽象层**，实现本地模式与远程模式的优雅切换。
WPF 客户端可直连本地 SQLite 数据库，无需启动 WebAPI Server。

---

## 核心设计决策

| 决策 | 选择 | 说明 |
|------|------|------|
| 架构模式 | **DataSource 抽象层** | Repository 统一，DataSource 可切换 |
| 数据库 | **SQLite** | 零配置，EF Core 原生支持 |
| 认证 | **简化本地认证** | BCrypt 直接验证，无 JWT |
| 数据同步 | **Phase 6 实现** | 先实现独立运行(Phase 1-5)，后实现同步(Phase 6) |

---

## 架构设计

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

[方案 C - DataSource 抽象层]
ViewModel → Service → IRepository → Repository → IDataSource → RemoteDataSource/LocalDataSource
                                        ↓                              ↓
                                   统一映射逻辑                    数据获取策略
```

### 设计优势

| 维度 | 说明 |
|------|------|
| **职责分离** | Repository 负责业务映射，DataSource 负责数据获取 |
| **代码复用** | Repository 只有一套实现，映射逻辑集中管理 |
| **扩展性** | 可组合多数据源（缓存、离线优先、同步等） |
| **测试性** | 可独立 Mock 各层进行测试 |

---

## 项目结构

### 新增项目

```
LYBT.Desktop.LocalData (新项目)
├── 引用 LYBT.Entities              # 复用 Entity 定义
├── 引用 LYBT.Shared.Models         # 复用 DTO 定义
├── 引用 Microsoft.EntityFrameworkCore.Sqlite
│
├── Context/
│   └── LocalDbContext.cs           # SQLite DbContext
│
├── Initialization/
│   ├── DatabaseInitializer.cs      # 数据库创建/迁移
│   └── SeedData.cs                 # 种子数据
│
└── DataSources/
    ├── LocalPatientDataSource.cs
    ├── LocalHerbDataSource.cs
    ├── LocalFormulaDataSource.cs
    ├── LocalMedicalCaseDataSource.cs
    └── LocalUserDataSource.cs
```

### 修改项目

```
LYBT.Desktop.Contracts (修改)
├── DataSources/                    # 新增目录
│   ├── IPatientDataSource.cs
│   ├── IHerbDataSource.cs
│   ├── IFormulaDataSource.cs
│   ├── IMedicalCaseDataSource.cs
│   └── IUserDataSource.cs

LYBT.Desktop.Infrastructure (修改)
├── DataSources/                    # 新增目录
│   └── Remote/
│       ├── RemotePatientDataSource.cs
│       ├── RemoteHerbDataSource.cs
│       └── ...

LYBT.Desktop.Xxx (各模块修改)
├── Repositories/
│   └── XxxRepository.cs            # 重构：依赖 IDataSource
```

---

## Phase 分解

### Phase 1: 基础设施层 (5 个任务)

- [ ] 1.1 创建 LYBT.Desktop.LocalData 项目
- [ ] 1.2 定义 IDataSource 接口族（Contracts 项目）
- [ ] 1.3 实现 LocalDbContext（SQLite 配置）
- [ ] 1.4 实现 DatabaseInitializer + SeedData
- [ ] 1.5 实现本地认证服务（BCrypt）

### Phase 2: DataSource 实现 (10 个任务)

**远程 DataSource（重构现有代码）**
- [ ] 2.1 RemotePatientDataSource
- [ ] 2.2 RemoteHerbDataSource
- [ ] 2.3 RemoteFormulaDataSource
- [ ] 2.4 RemoteMedicalCaseDataSource
- [ ] 2.5 RemoteUserDataSource

**本地 DataSource（新实现）**
- [ ] 2.6 LocalPatientDataSource
- [ ] 2.7 LocalHerbDataSource
- [ ] 2.8 LocalFormulaDataSource
- [ ] 2.9 LocalMedicalCaseDataSource
- [ ] 2.10 LocalUserDataSource

### Phase 3: Repository 重构 (5 个任务)

- [ ] 3.1 重构 PatientRepository（依赖 IPatientDataSource）
- [ ] 3.2 重构 HerbRepository
- [ ] 3.3 重构 FormulaRepository
- [ ] 3.4 重构 MedicalCaseRepository
- [ ] 3.5 重构 UserRepository

### Phase 4: 集成与切换 (4 个任务)

- [ ] 4.1 DI 注册框架（根据 ConnectionMode 注入对应 DataSource）
- [ ] 4.2 ConnectionMode 选择逻辑激活
- [ ] 4.3 LoginCoordinator 适配
- [ ] 4.4 健康检查适配

### Phase 5: 测试与文档 (3 个任务)

- [ ] 5.1 单元测试（DataSource + Repository）
- [ ] 5.2 集成测试（端到端流程）
- [ ] 5.3 文档更新

### Phase 6: 数据同步 (4 个任务)

- [ ] 6.1 SyncLog 表设计（变更追踪）
- [ ] 6.2 同步 API 端点（Server 端）
- [ ] 6.3 OfflineFirstDataSource 实现
- [ ] 6.4 同步冲突解决策略

---

## 关键接口设计

### IDataSource 接口

```csharp
/// <summary>
/// 数据源抽象接口 - 返回 Entity 或原始数据
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
```

### Repository 重构示例

```csharp
public class PatientRepository : IPatientRepository
{
    private readonly IPatientDataSource _dataSource;
    private readonly IMapper _mapper;

    public PatientRepository(IPatientDataSource dataSource, IMapper mapper)
    {
        _dataSource = dataSource;
        _mapper = mapper;
    }

    public async Task<PatientDetailDto?> GetByIdAsync(Guid id)
    {
        var entity = await _dataSource.GetByIdAsync(id);
        return entity == null ? null : _mapper.ToDetailDto(entity);
    }

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page, int pageSize, string? keyword)
    {
        var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword);
        return new PagedResult<PatientListDto>
        {
            Items = items.Select(_mapper.ToListDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
```

### DI 注册

```csharp
// 根据 ConnectionMode 注册对应的 DataSource
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

---

## SQLite 适配要点

| 问题 | 方案 |
|------|------|
| RowVersion | 本地单用户，忽略并发控制 |
| decimal 精度 | ValueConverter 转 double |
| 审计字段 | LocalDbContext 重写 SaveChangesAsync |
| 全局过滤器 | SQLite 完全支持 HasQueryFilter |

---

## 关键约束

1. **Repository 统一** - 只有一套 Repository 实现，不因模式而异
2. **DataSource 可替换** - 通过 DI 注入不同实现
3. **映射集中** - Entity → DTO 映射只在 Repository 层
4. **Service/ViewModel 零改动** - 上层完全不感知数据源变化

---

## 风险点

1. **重构范围较大** - 需要重构所有 Repository
2. **API 解包逻辑迁移** - 现有 Repository 的 ApiResponse 处理需迁移到 RemoteDataSource
3. **Entity 在 Desktop 端的依赖** - 需验证 LYBT.Entities 可被 Desktop 引用
4. **测试覆盖** - 需要为新架构编写充分的单元测试

---

## 待确认事项

1. [x] 架构方案 - 已确认采用方案 C（DataSource 抽象层）
2. [x] Phase 6 数据同步 - 不作为独立提案，纳入本提案 Phase 6
3. [x] 数据库文件存储位置 - `%APPDATA%\LYBTZYZS\lybtzyzs.db`
