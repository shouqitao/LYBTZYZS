# Findings: 项目架构优化一轮迭代

## 项目规模
- 56 个项目 (42 源码 + 26 测试), 2359 测试方法
- Server: 7 业务模块, 15 Service, 5 Repository
- Desktop: 8 Core + 7 Module + 2 Role + 1 Shell = 18 项目
- Shared: 8 共享库
- Test: 5 新结构 + 18 旧结构 + 3 特殊 = 26 项目

## 项目级架构分析 (2026-02-23)

### 依赖方向: 无循环依赖, 整体健康
- Shared 层: 层级清晰, Primitives→Models→Validators/Utilities/Components
- Server 模块: Herbs/Patients/Formula/Users/Auth 均独立
- Desktop 模块: 遵循 Foundation→Infrastructure→Models→Contracts 分层

### 发现的架构问题

#### P1: Server Sync 模块直接引用 3 个业务模块
- LYBT.Module.Sync → Herbs, Patients, Formula (用于引用检查)
- 违反模块独立性原则, 应通过 ICrossModuleService 接口

#### P2: Server MedicalCase 直接引用 2 个业务模块
- LYBT.Module.MedicalCase → Patients, Users
- 已有注释标记移除计划, 运行时通过 ICrossModuleService

#### P3: Desktop MedicalCase 直接引用 2 个业务模块
- LYBT.Desktop.MedicalCase → Herbs, Formula
- D5-3 设计: 应通过 IHerbSearchProvider/IFormulaSearchProvider 接口

#### P4: 架构测试重复
- LYBT.ArchTests (旧, 5文件41测试) vs LYBT.Tests.Architecture (新, 6文件41测试)
- 测试内容互补但项目分裂

#### P5: 空壳目录残留
- src/Server/Modules/LYBT.Module.Consultation/ (仅 obj/)
- src/Server/Modules/LYBT.Module.Prescriptions/ (仅 obj/)

#### P6: 包版本管理
- 待确认是否使用 Directory.Packages.props 中央管理

### 测试架构: 健康, 无重复
- 新旧结构互补: 新=层级测试, 旧=模块测试
- Desktop Unit 649 测试全在新结构 (旧结构无 Desktop 单测)
- 仅 Architecture 项目建议合并

## 设计 vs 代码差距摘要

### S1 安全加固 (33项, P0-P1, 无前置依赖)
- Token Family 撤销 6 场景未实现
- 引用检查 CheckReference 硬编码 true
- 权限矩阵不完整 (Admin→Receptionist, 自删保护)
- 密码哈希 Bug (L458 旧密码优先)

### S2 核心功能 (51项, P1, 依赖 S1)
- 打印字段在 Prescription 层级 (应提升到 MedicalCase)
- 15 个字段验证值与 PRD 不一致
- TotalPrice 始终为 0, FormulaMapper Herbs 忽略

### S3 体系统一 (85项, P1, 依赖 S2)
- 错误码 MCCEE 未全量统一
- ICrossModuleService 未 ISP 拆分 (设计已确认)
- 分页筛选仍在内存过滤 (6 处)

### S4 本地模式 (62项, P1-P2)
- IDataSource 方法缺失 (~22 处)
- 打印模板与 PRD 不一致 (字体/边距/内容)

### S5 细节完善 (98项, P2-P3)
- BaseService 继承未统一 (3 个 Service)
- SyncService 返回类型 ServiceResult → Result<T>
- Desktop 跨模块依赖未解耦
