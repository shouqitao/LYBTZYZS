# Research Findings: implement-local-mode

## Overview
本地模式开发提案的调研发现和代码分析结果。

## Code Analysis

### 已完成的代码结构

#### Phase 1 - 基础设施层
```
src/Client/Desktop/Core/LYBT.Desktop.LocalData/
├── Context/
│   └── LocalDbContext.cs            # SQLite DbContext
├── Initialization/
│   ├── DatabaseInitializer.cs       # 数据库初始化
│   └── SeedData.cs                  # 种子数据（admin用户）
└── Services/
    └── LocalAuthService.cs          # 本地认证服务

src/Client/Desktop/Core/LYBT.Desktop.Contracts/DataSources/
├── IDataSourceBase.cs               # 基础接口
├── IPatientDataSource.cs
├── IHerbDataSource.cs
├── IFormulaDataSource.cs
├── IMedicalCaseDataSource.cs
└── IUserDataSource.cs
```

#### Phase 2 - DataSource 实现
```
Local DataSources (已完成):
src/Client/Desktop/Core/LYBT.Desktop.LocalData/DataSources/
├── LocalPatientDataSource.cs
├── LocalHerbDataSource.cs
├── LocalFormulaDataSource.cs
├── LocalMedicalCaseDataSource.cs
└── LocalUserDataSource.cs

Remote DataSources (已完成):
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DataSources/
├── Mappers/
│   ├── PatientDataSourceMapper.cs
│   ├── HerbDataSourceMapper.cs
│   ├── FormulaDataSourceMapper.cs
│   ├── MedicalCaseDataSourceMapper.cs
│   └── UserDataSourceMapper.cs
└── Remote/
    ├── RemotePatientDataSource.cs
    ├── RemoteHerbDataSource.cs
    ├── RemoteFormulaDataSource.cs
    ├── RemoteMedicalCaseDataSource.cs
    └── RemoteUserDataSource.cs
```

### Phase 2 已完成
所有 Local 和 Remote DataSource 实现已完成并通过编译验证。

### Phase 4 - 集成与切换
```
新增文件:
src/Client/Desktop/Shell/Extensions/DataSourceRegistrationExtensions.cs  # DI注册扩展
src/Client/Desktop/Shell/Services/Session/SessionBasedCurrentUserProvider.cs  # ICurrentUserProvider实现

修改文件:
src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj  # 添加LocalData引用+SQLite包
src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs  # 集成DataSource注册
src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs  # 本地模式认证支持
src/Client/Desktop/Shell/Services/HealthCheck/HealthCheckCoordinator.cs  # 本地模式跳过API检查
```

### 如何启用本地模式
在 `appsettings.json` 中添加：
```json
{
  "ConnectionMode": "Local"
}
```

默认不配置时使用 Remote 模式。

## Architecture Insights

### DataSource 抽象层设计
```
IRepository ←── Repository ←── IDataSource ←┬── RemoteDataSource (API)
                                            └── LocalDataSource (SQLite)
```

### 关键设计决策
1. **Repository 不变**: ViewModel 仍依赖 IRepository，无感知切换
2. **DataSource 可替换**: 通过 DI 注入不同实现
3. **Entity 返回**: DataSource 返回 Entity，Repository 负责 DTO 转换

## Technical Notes

### SQLite 适配
- decimal 类型需要 ValueConverter 转 double
- RowVersion 字段需要忽略（SQLite 不支持 rowversion）
- 使用 EnsureCreated() 而非 Migrations

### 登录流程
- Local 模式: LoginCoordinator → LocalAuthService.ValidateAsync()
- Remote 模式: LoginCoordinator → API 认证

## References
- [OpenSpec Proposal](openspec/changes/implement-local-mode/proposal.md)
- [OpenSpec Design](openspec/changes/implement-local-mode/design.md)
- [OpenSpec Tasks](openspec/changes/implement-local-mode/tasks.md)

---
*Created: 2026-02-03*
*Last Updated: 2026-02-03*
