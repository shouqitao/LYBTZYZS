# Solution 架构评估规格

> 日期: 2026-06-29
> 状态: 待审
> 来源: 资深架构师全面评估（49 项目，284K 行代码）
> 范围: Solution 级架构优化方向

## [S1] 项目全景

### 1.1 项目统计

| 层级 | 项目数 | 代码量 | 职责 |
|------|--------|--------|------|
| Shared Libraries | 8 | ~19,100 | 跨层数据契约、工具 |
| Server Core | 2 | ~12,400 | 领域实体 + 基础设施 |
| Server Modules | 8 | ~9,200 | 业务逻辑（CQRS） |
| Server Host | 1 | ~27,200 | API 宿主 |
| Desktop Core | 10 | ~75,800 | WPF 基础设施 |
| Desktop Modules | 8 | ~89,300 | 业务 UI |
| Desktop Roles | 4 | ~10,200 | 角色工作区 |
| Tests | 3 | 226 文件 | 集成测试 |

### 1.2 依赖方向（已验证无循环）

```
Server: Modules → Infrastructure → Entities → Shared
Desktop: Roles → Modules → Infrastructure → Foundation → Contracts → Shared
```

## [S2] 架构优点

| 优点 | 说明 |
|------|------|
| 模块隔离良好 | Server 模块间无直接引用（已修复） |
| 依赖方向正确 | 无循环依赖，DAG 结构 |
| 分层清晰 | Shared 纯契约，Server 三层，Desktop MVVM |
| 测试架构 | Testing Trophy：Server(真实SQL) + Desktop(LocalDB) + Architecture(守护) |

## [S3] 架构问题

### CRITICAL

| # | 问题 | 位置 | 行数 | 影响 |
|---|------|------|------|------|
| C1 | MedicalCaseCommandService 职责过重 | Server/Modules/MedicalCase | 773 | 应拆分为 CommandHandler |
| C2 | ErrorCode 枚举膨胀 | Shared/Primitives | 746 | 应按模块分区 |
| C3 | MedicalCase 模块过大 | Server/Modules/MedicalCase | 3,283 | 最大 Server 模块 |

### HIGH

| # | 问题 | 位置 | 行数 | 影响 |
|---|------|------|------|------|
| H1 | WebAPI Host 过大 | Server/Host | 27,200 | 控制器+中间件混杂 |
| H2 | Desktop.Controls 过大 | Desktop/Core | 17,146 | 控件库应拆分 |
| H3 | Desktop.Infrastructure 过重 | Desktop/Core | 13,350 | ViewModel 基类应提取 |
| H4 | Desktop.MedicalCase 过大 | Desktop/Modules | 18,807 | 最大 Desktop 模块 |
| H5 | 测试覆盖不均 | Tests | — | Server 101 vs Desktop 117 |
| H6 | Desktop.Registration 引用 3 模块 | Desktop/Modules | — | 模块隔离违规 |

### MEDIUM

| # | 问题 | 位置 | 行数 | 影响 |
|---|------|------|------|------|
| M1 | BaseRepository 过重 | Infrastructure | 594 | 职责过多 |
| M2 | HttpClientApiClient 过大 | Desktop/Foundation | 612 | HTTP 客户端臃肿 |
| M3 | LoginViewModel 过大 | Desktop/Auth | 637 | 登录逻辑复杂 |
| M4 | Printing 模块过大 | Desktop/Core | 11,588 | 打印模块臃肿 |
| M5 | LocalData 跨层引用 | Desktop/Core | — | 已文档化例外 |
| M6 | Controller 重复代码 | WebAPI/LocalWebAPI | — | 10 对重复 |

## [S4] 优化方案

### 4.1 分解大文件

| 文件 | 当前行数 | 方案 | 目标行数 |
|------|----------|------|----------|
| MedicalCaseCommandService | 773 | 拆分为 Create/Update/Delete CommandHandler | <300 每个 |
| ErrorCode | 746 | 按模块分区（Auth/Patient/Herb/...） | <100 每个分区 |
| WebAPI Host | 27,200 | 提取中间件到独立项目 | <10,000 |
| Desktop.Controls | 17,146 | 按功能拆分（Grid/Form/Dialog） | <5,000 每个 |
| Desktop.Infrastructure | 13,350 | 提取 ViewModel 基类到独立项目 | <8,000 |

### 4.2 模块隔离修复

| 问题 | 方案 | 工作量 |
|------|------|--------|
| Desktop.Registration 引用 3 模块 | 通过 Contracts 接口通信 | 中 |

### 4.3 测试覆盖

| 项目 | 当前 | 目标 | 差距 |
|------|------|------|------|
| Server | 101 文件 | 150+ | 缺少 UserService/UserController 测试 |
| Desktop | 117 文件 | 150+ | 缺少新模块测试 |
| Architecture | 8 文件 | 15+ | 缺少模块间引用检查 |

## [S5] 实施优先级

| 批次 | 任务 | 工作量 | 收益 |
|------|------|--------|------|
| 1 | 修复架构测试（BaseUsersController 位置） | 低 | 高 |
| 2 | MedicalCaseCommandService 拆分 | 高 | 高 |
| 3 | ErrorCode 按模块分区 | 中 | 中 |
| 4 | Desktop.Registration 模块隔离 | 中 | 中 |
| 5 | Desktop.Controls 拆分 | 高 | 中 |
| 6 | 测试覆盖补全 | 中 | 中 |

## [S6] 验收标准

1. 所有架构测试通过
2. 无文件超过 500 行（自动生成除外）
3. 模块间无直接引用
4. 测试覆盖率达到目标
5. `dotnet build` 0 错误
