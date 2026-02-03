# Task Plan: implement-local-mode

## Objective
实现 LYBTZYZS 桌面应用的本地模式，支持离线运行（SQLite本地存储），通过 DataSource 抽象层实现远程/本地模式切换。

## Current Status Summary
**Phase 1**: 基础设施层 - **完成**
**Phase 2**: DataSource 实现 - **完成** (Local + Remote 全部完成)
**Phase 3**: Repository 重构 - **完成** (5个 Repository 全部重构)
**Phase 4**: 集成与切换 - **完成** (DI注册、LoginCoordinator、HealthCheck适配)
**Phase 5**: 测试与文档 - **完成** (63测试全通过，文档已更新)
**Phase 6**: 数据同步 - **可选，未开始**

---

## Phases

### Phase 1: 基础设施层 [COMPLETE]
- [x] 1.1 创建 LYBT.Desktop.LocalData 项目
- [x] 1.2 定义 IDataSource 接口族 (6个接口已创建)
- [x] 1.3 实现 LocalDbContext
- [x] 1.4 实现 DatabaseInitializer + SeedData
- [x] 1.5 实现 LocalAuthService
- [x] 1.6 编译验证 Phase 1

### Phase 2: DataSource 实现 [COMPLETE]

#### 远程 DataSource (重构)
- [x] 2.1 RemotePatientDataSource (含 PatientDataSourceMapper)
- [x] 2.2 RemoteHerbDataSource (含 HerbDataSourceMapper)
- [x] 2.3 RemoteFormulaDataSource (含 FormulaDataSourceMapper)
- [x] 2.4 RemoteMedicalCaseDataSource (含 MedicalCaseDataSourceMapper)
- [x] 2.5 RemoteUserDataSource (含 UserDataSourceMapper)

#### 本地 DataSource (新建)
- [x] 2.6 LocalPatientDataSource
- [x] 2.7 LocalHerbDataSource
- [x] 2.8 LocalFormulaDataSource
- [x] 2.9 LocalMedicalCaseDataSource
- [x] 2.10 LocalUserDataSource
- [x] 2.11 编译验证 Phase 2

### Phase 3: Repository 重构 [COMPLETE]
- [x] 3.1 重构 PatientRepository (之前已完成)
- [x] 3.2 重构 HerbRepository (之前已完成)
- [x] 3.3 重构 FormulaRepository (之前已完成)
- [x] 3.4 重构 MedicalCaseRepository (之前已完成)
- [x] 3.5 重构 UserRepository (本次会话完成)
- [x] 3.6 编译验证 Phase 3 (0 errors, 0 warnings)

### Phase 4: 集成与切换 [COMPLETE]
- [x] 4.1 DI 注册框架 (DataSourceRegistrationExtensions)
- [x] 4.2 ConnectionMode 选择逻辑激活 (从配置文件读取)
- [x] 4.3 LoginCoordinator 适配 (支持本地模式认证)
- [x] 4.4 健康检查适配 (本地模式跳过API检查)
- [x] 4.5 编译验证 Phase 4 (0 errors, 0 warnings)

### Phase 5: 测试与文档 [COMPLETE]
- [x] 5.1 单元测试 (47 个测试全部通过)
  - LocalPatientDataSourceTests (17 个测试)
  - LocalAuthServiceTests (17 个测试)
  - LocalHerbDataSourceTests (13 个测试)
- [x] 5.2 集成测试 (16 个测试全部通过)
  - DataSourceIntegrationTests (9 个测试): DI解析、CRUD端到端、分页、DbContext共享
  - LoginFlowIntegrationTests (7 个测试): 本地登录、密码修改、账户锁定、初始化幂等性
- [x] 5.3 文档更新
  - README.md: 核心特性、架构图、本地模式配置说明
  - CHANGELOG.md: 本地模式功能记录

### Phase 6: 数据同步 [OPTIONAL]
- [ ] 6.1 SyncLog 表设计
- [ ] 6.2 同步 API 端点
- [ ] 6.3 OfflineFirstDataSource 实现
- [ ] 6.4 同步冲突解决策略
- [ ] 6.5 编译验证 Phase 6

---

## Key Decisions
1. **方案选择**: DataSource 抽象层 (方案 C)，Repository 持有 IDataSource 接口
2. **本地存储**: SQLite + EF Core，路径 `%APPDATA%\LYBTZYZS\lybtzyzs.db`
3. **密码加密**: BCrypt
4. **SQLite 适配**: decimal→double ValueConverter, 忽略 RowVersion

## Dependencies
```
Phase 1 → Phase 2 (Local + Remote) → Phase 3 → Phase 4 → Phase 5
                                                          ↓
                                                     Phase 6 (可选)
```

## Next Actions
1. **Phase 5 完成** - 可选择归档提案或继续 Phase 6 数据同步
2. 归档命令: `/lybtzyzs-openspec-archive-finalize`
3. 或继续 Phase 6 数据同步（可选功能）

## Risks & Mitigations
| 风险 | 缓解措施 |
|------|----------|
| Repository 重构影响现有功能 | 保持接口契约不变，逐模块渐进式重构 |
| 本地/远程数据不一致 | Phase 6 同步机制解决 |

---
*Created: 2026-02-03*
*Last Updated: 2026-02-03*
