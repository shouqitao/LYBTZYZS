# Findings: WPF 本地模式开发研究

> 最后更新: 2026-02-03 13:58
> 架构方案: **C - DataSource 抽象层**

---

## Finding 1: 远程模式完整数据流

```
Desktop 数据流 (远程模式):
ViewModel → Service → IXxxRepository → XxxRepository
  → IXxxApi (Refit) → HTTP Handler 链 → WebAPI Server → EF Core → SQL Server
```

---

## Finding 2: IXxxApi 返回 ApiResponse<T> 不适合本地实现

Desktop 的 Refit API 接口返回类型：
```csharp
Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);
```

`ApiResponse<T>` 是 Refit 库的专有类型，包含：
- `T Content` - 实际数据
- `HttpResponseMessage` - HTTP 响应
- `bool IsSuccessStatusCode` - 状态码

**结论**: 本地模式构造 ApiResponse 需要伪造 HttpResponseMessage，是反模式。

---

## Finding 3: IXxxRepository 接口返回纯 DTO

Desktop 的 Repository 接口返回类型：
```csharp
Task<PatientDetailDto?> GetByIdAsync(Guid id);
Task<PagedResult<PatientListDto>> GetPagedAsync(int page, int pageSize, string? keyword);
```

这些接口可以被任何数据源实现，是天然的抽象层。

---

## Finding 4: 当前 Repository 职责混乱

当前 `PatientRepository` 做了两件事：
1. 调用 Refit API 获取数据
2. 解包 ApiResponse 并处理错误

这违反了单一职责原则。

---

## Finding 5: 方案对比分析

| 方案 | 切换层 | 技术障碍 | 设计纯度 |
|------|--------|----------|----------|
| A: API 层 | IXxxApi | 高（构造 ApiResponse） | 低 |
| B: Repository 层 | IXxxRepository | 低 | 中 |
| **C: DataSource 层** | IXxxDataSource | 无 | **高** |

---

## Finding 6: 方案 C 的核心优势

### 6.1 职责完全分离

```
DataSource: 只负责数据获取（返回 Entity）
Repository: 只负责业务映射（Entity → DTO）
```

### 6.2 Repository 代码复用

方案 B 需要两套 Repository（Remote + Local），方案 C 只需一套。

### 6.3 支持数据源组合

```csharp
// 未来可实现离线优先策略
public class OfflineFirstDataSource : IPatientDataSource
{
    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        var local = await _local.GetByIdAsync(id);
        if (local != null) return local;

        var remote = await _remote.GetByIdAsync(id);
        if (remote != null) await _sync.CacheLocallyAsync(remote);
        return remote;
    }
}
```

---

## Finding 7: Entity 项目可被 Desktop 直接引用

LYBT.Entities.csproj 依赖:
- Microsoft.EntityFrameworkCore (无 provider)
- LYBT.Shared.Models

是 provider-agnostic 的，Desktop 端可以安全引用。

---

## Finding 8: SQLite + EF Core 限制

经 Microsoft 官方文档确认:
- SQLite 3.46.1+ 支持 EF Core 8/9/10
- 不支持: Schemas, Sequences, database-generated concurrency tokens
- RowVersion (byte[] Timestamp) 需要忽略
- decimal 需要 ValueConverter 转 double
- HasQueryFilter 完全支持

---

## Finding 9: 远程 DataSource 需要处理 ApiResponse 解包

RemoteDataSource 需要：
1. 调用 Refit API
2. 解包 ApiResponse
3. 处理 HTTP 错误
4. 返回 Entity（需要 DTO → Entity 反向映射或直接存储 Entity）

**设计决策**: RemoteDataSource 返回 Entity 还是 DTO？

选项 A: 返回 Entity
- 需要 DTO → Entity 反向映射
- Repository 统一处理 Entity → DTO

选项 B: 返回 DTO（原始响应）
- RemoteDataSource 直接返回 API 响应内容
- Repository 需要判断数据类型

**推荐选项 A**: 保持 DataSource 接口一致性，所有实现都返回 Entity。

---

## Finding 10: IDataSource 接口设计考量

### 返回类型选择

| 选项 | 返回类型 | 适用场景 |
|------|----------|----------|
| Entity | `Patient` | 本地 DataSource 直接返回 |
| DTO | `PatientDetailDto` | 远程 DataSource 直接返回 API 响应 |
| 混合 | 泛型 `T` | 灵活但复杂 |

**推荐**: 返回 Entity，保持一致性。远程 DataSource 需要 DTO → Entity 映射。

### 方法签名设计

```csharp
public interface IPatientDataSource
{
    // 基础 CRUD
    Task<Patient?> GetByIdAsync(Guid id);
    Task<(List<Patient> Items, int Total)> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<Patient> CreateAsync(Patient entity);
    Task<Patient> UpdateAsync(Patient entity);
    Task DeleteAsync(Guid id);

    // 业务特定方法
    Task<List<Patient>> SearchAsync(string keyword);
    Task<Patient?> GetByIdNumberAsync(string idNumber);
}
```

---

## Finding 11: DryIoc 支持运行时切换

Prism DryIoc 支持：
- `Register<T>(serviceKey)` - keyed 注册
- `RegisterDelegate<T>(factory)` - 工厂委托注册

可以在应用启动时根据 ConnectionMode 注册对应的 DataSource 实现。

---

## 待研究

- [x] SQLite + EF Core 集成 → 官方完全支持
- [x] DryIoc 运行时切换 → Keyed 注册 + 工厂委托
- [x] 架构方案选择 → 方案 C (DataSource 抽象层)
- [ ] RemoteDataSource 的 DTO → Entity 映射策略
- [ ] 数据库文件存储位置最佳实践
- [ ] BCrypt.Net 在 Desktop 端的引用情况
