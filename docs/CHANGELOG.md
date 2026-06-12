# 变更记录

> v1 开发中。按功能模块组织，最新在前。

---

## [2026-06] 文档完善 & 双向同步

### 文档
- 新增 `DEVELOPER-GUIDE.md` — 统一开发者入口（架构图、模块索引、文档导航）
- 新增 `ONBOARDING.md` — 新人引导（7步从clone到第一个PR）
- 新增 `docs/07-concepts/` — 46项技术概念文档（wiki同步）
  - 35核心概念: 双模式架构、临床工作流、测试策略、错误处理、认证授权等
  - 8模块概述: Auth, Patient, Herb, Formula, MedicalCase, Registration, Sync, Printing
  - 3开发指南: 构建运行、常见陷阱、术语表
- 文档清理: 归档~50份过时文档，删除~15份噪音文件
- MVP术语清理: 活跃文档中4处MVP引用替换为v1/当前版本
- 24份文档同步到 LLM Wiki raw sources（ADR + Standards + requirements等）
- GitNexus代码智能集成到 AGENTS.md

### 代码
- WPF数据适配器重设计 — 统一 IApiClient 抽象层
- ApiResponse 辅助类 — Remote/Local 返回类型统一
- LocalWebAPI与Server对齐 — 修复ADR-0004违规，更新API文档
- 消除全部编译警告（xUnit1012, CS8604, CS8602, CS0067）
- PRD审计6项高优先级修复
- MedicalCase: 备注编辑器 + 现病史自动补全
- CI集成Postman/Newman测试运行器

---

## [2026-05] 离线同步 & LocalWebAPI

### 离线同步 (Offline Sync)
- ApiRouter接口 — 远程/本地API自动切换
- 6个模块Repository双路径支持（IApiRouter + 本地Refit接口）
- MedicalCase同步: 服务端元数据/上传/下载/删除，客户端JSON序列化/保存
- LocalWebAPI认证: JWT + AuthController + UsersController
- LocalWebAPI健康检查: ping + details端点
- DI注册: ApiRouter、本地Refit客户端、OfflineMode配置
- UI状态指示器 + 旧ConnectionMode清理
- 离线覆盖率扩展至68%
- Herb批量导入: Stream → DTO模式对齐

### LocalWebAPI (新建)
- 项目创建: `LYBT.LocalWebAPI`（net8, Library + Web）
- LocalWebApiDbContext + SQLite种子数据
- 6个Controller: Patients, Herbs, Formulas, Registrations, MedicalCases, Health
- JWT认证 + AuthController
- LocalWebApiHost: Kestrel生命周期管理
- HTTP代理Repository（6个）
- DI接入三模式工厂（Remote/Local/LocalWebAPI）
- 独立运行支持（Program.cs入口点）
- ID不匹配防护、ModelState验证、分页边界检查

### 三模式架构
- ConnectionMode.LocalWebAPI 枚举值
- 三模式切换验证（Remote / Local / LocalWebAPI）
- 双模式架构文档更新为三模式

### 测试
- LocalWebAPI测试: DbContext、JWT、HTTP代理Repository（~85 tests）
- Desktop LocalAPI测试覆盖
- HTTP Repository测试加强: 验证HTTP方法和请求体
- Postman测试集合（LocalWebAPI）

---

## [2026-04] CI/CD & 基础设施

### CI
- 分层CI架构: architecture → backend → frontend
- GitHub Actions: Linux SQL Server容器集成测试
- Windows runner: 架构测试 + Desktop编译
- 多轮CI稳定性修复（路径、依赖、配置）

### 依赖
- 58个NuGet包安全升级（同major版本最新）

### 代码
- 编译警告清理: 84 → 2
- CA1001修复: 测试Fixture实现IDisposable
- 导航架构改进
- MedicalCase统一为Compact模式 + UX改进
- 多轮XAML/Toast编译修复

---

## [2026-03] 项目初始化

### 架构
- 三层架构搭建: Controller → Service → Repository → DbContext
- MVVM + Prism框架: DryIoc DI容器
- EF Core 8 + SQL Server
- Central Package Management
- Serilog结构化日志（两阶段bootstrap）
- 全局异常处理中间件

### 模块
- Auth模块: JWT认证、角色授权、Token刷新
- Patient模块: CRUD、拼音搜索、批量导入导出
- MedicalCase模块: 医案CRUD、诊断、处方、锁定
- Herb模块: 中药材管理、分类、搜索
- Formula模块: 验方管理、复制到处方
- Registration模块: 挂号、排队、状态管理
- Sync模块: 数据同步

### 桌面客户端
- Prism模块化加载
- 角色工作区: Admin / Clinical / Receptionist
- 8个桌面模块
- 嵌入式Kestrel（本地WebAPI宿主）

### 文档
- 产品文档（01-product）
- PRD 15模块138 User Stories（02-requirements）
- 架构文档 + 8 ADR（03-architecture）
- API参考 100+端点（04-api-reference）
- 开发指南 + 编码标准（05-development）
- 部署运维文档（06-operations）

---

## 架构决策记录 (ADR)

| ADR | 标题 | 日期 |
|-----|------|------|
| 0001 | 双模式架构 (远程/本地) | 2026-02 |
| 0002 | 嵌入式Kestrel本地WebAPI | 2026-03 |
| 0003 | Central Package Management | 2026-02 |
| 0004 | API响应信封 ApiResponse<T> | 2026-02 |
| 0005 | 测试策略: 集成优先零Mock | 2026-03 |
| 0006 | Mapperly源生成映射 | 2026-03 |
| 0007 | Serilog两阶段Bootstrap | 2026-02 |
| 0008 | 功能开关 (Feature Toggles) | 2026-04 |
