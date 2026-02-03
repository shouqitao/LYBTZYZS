# implement-local-mode Tasks

> 状态: 草稿
> 架构: DataSource 抽象层 (方案 C)
> 总任务数: 31

---

## Phase 1: 基础设施层 (5 个任务)

- [ ] 1.1 创建 LYBT.Desktop.LocalData 项目
  - 添加到 LYBT.Desktop.sln
  - 配置项目引用 (LYBT.Entities, LYBT.Shared.Models)
  - 添加 NuGet 包 (Microsoft.EntityFrameworkCore.Sqlite, Riok.Mapperly, BCrypt.Net-Next)

- [ ] 1.2 定义 IDataSource 接口族 (Contracts 项目)
  - IPatientDataSource
  - IHerbDataSource
  - IFormulaDataSource
  - IMedicalCaseDataSource
  - IUserDataSource

- [ ] 1.3 实现 LocalDbContext
  - SQLite 数据库配置
  - 适配 RowVersion (忽略)
  - 适配 decimal (ValueConverter)
  - 审计字段填充 (SaveChangesAsync)
  - 全局查询过滤器

- [ ] 1.4 实现 DatabaseInitializer + SeedData
  - EnsureCreated 创建数据库
  - 数据库文件位置: %APPDATA%\LYBTZYZS\lybtzyzs.db
  - 种子数据: 默认管理员账户

- [ ] 1.5 实现 LocalAuthService
  - BCrypt 密码验证
  - 本地会话管理 (无 JWT)

- [ ] 1.6 编译验证 Phase 1

---

## Phase 2: DataSource 实现 (10 个任务)

### 远程 DataSource (重构)

- [ ] 2.1 RemotePatientDataSource
  - 从 PatientRepository 提取 API 调用逻辑
  - 解包 ApiResponse
  - 返回 Entity (DTO → Entity 映射)

- [ ] 2.2 RemoteHerbDataSource

- [ ] 2.3 RemoteFormulaDataSource

- [ ] 2.4 RemoteMedicalCaseDataSource

- [ ] 2.5 RemoteUserDataSource

### 本地 DataSource (新建)

- [ ] 2.6 LocalPatientDataSource
  - EF Core 查询 SQLite
  - 分页、搜索、CRUD

- [ ] 2.7 LocalHerbDataSource
  - 分类过滤
  - 状态切换

- [ ] 2.8 LocalFormulaDataSource
  - 克隆功能
  - 状态管理

- [ ] 2.9 LocalMedicalCaseDataSource
  - 聚合根 CRUD
  - 生命周期管理
  - Consultation/Prescription 关系

- [ ] 2.10 LocalUserDataSource

- [ ] 2.11 编译验证 Phase 2

---

## Phase 3: Repository 重构 (5 个任务)

- [ ] 3.1 重构 PatientRepository
  - 移除 IPatientApi 依赖
  - 注入 IPatientDataSource
  - 保留 Entity → DTO 映射

- [ ] 3.2 重构 HerbRepository

- [ ] 3.3 重构 FormulaRepository

- [ ] 3.4 重构 MedicalCaseRepository

- [ ] 3.5 重构 UserRepository

- [ ] 3.6 编译验证 Phase 3

---

## Phase 4: 集成与切换 (4 个任务)

- [ ] 4.1 DI 注册框架
  - RegisterDataSources 扩展方法
  - 根据 ConnectionMode 注册对应 DataSource
  - Repository 统一注册

- [ ] 4.2 ConnectionMode 选择逻辑激活
  - 移除 LoginView "开发中" 对话框
  - 启用本地模式选择

- [ ] 4.3 LoginCoordinator 适配
  - 本地模式: LocalAuthService
  - 远程模式: 现有逻辑

- [ ] 4.4 健康检查适配
  - 本地模式: SQLite 文件可用性检查
  - 远程模式: API 健康检查

- [ ] 4.5 编译验证 Phase 4

---

## Phase 5: 测试与文档 (3 个任务)

- [ ] 5.1 单元测试
  - DataSource 测试 (Remote + Local)
  - Repository 测试
  - LocalAuthService 测试

- [ ] 5.2 集成测试
  - 本地模式端到端流程
  - 远程模式回归测试

- [ ] 5.3 文档更新
  - 设计文档
  - 用户指南

---

## Phase 6: 数据同步 (4 个任务)

- [ ] 6.1 SyncLog 表设计
  - 变更追踪表结构
  - LocalDbContext 添加同步元数据

- [ ] 6.2 同步 API 端点 (Server 端)
  - 批量同步上传
  - 增量数据拉取

- [ ] 6.3 OfflineFirstDataSource 实现
  - 先查本地，后查远程
  - 写操作本地优先
  - 后台同步队列

- [ ] 6.4 同步冲突解决策略
  - 时间戳对比
  - 冲突标记
  - 手动解决界面

- [ ] 6.5 编译验证 Phase 6

---

## Validation Checklist

- [ ] Desktop 解决方案编译通过
- [ ] Server 解决方案编译通过 (Phase 6)
- [ ] 本地模式登录成功
- [ ] 本地模式 CRUD 功能正常
- [ ] 远程模式回归测试通过
- [ ] 数据同步功能正常 (Phase 6)

---

**注意**: 此为草稿版本，将在设计阶段细化任务列表

**提案版本**: v2.0
**更新日期**: 2026-02-03
