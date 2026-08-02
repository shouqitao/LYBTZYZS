# LYBTZYZS 代码与文档综合审计报告
> 审计日期：2026-08-02 | 审计方式：9 个并行 agent 深度扫描

---

## 一、项目概况

| 指标 | 数值 |
|------|------|
| 技术栈 | .NET 8 / WPF Prism / ASP.NET Core / EF Core / SQL Server |
| Server 模块 | 8 个（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports） |
| Controller | 12 个（10 用 MediatR，2 直接注入） |
| MediatR Handler | 74 个（18 trivial / 32 simple / 26 complex） |
| MediatR 注册点 | 9 个独立 Assembly 扫描 |
| CrossModule 接口 | 6 个 |
| 架构测试 | 85/86 pass（1 skip） |
| Desktop 模块 | 7 个 + 3 个 Core 层 |
| Desktop API 切换 | SwitchingApiClient（Local/Remote 自动切换） |
| 文档总数 | 356 个 .md 文件 |
| Build warnings | 0 |

---

## 二、Server 层架构现状

### 2.1 Controller 调用模式（三套路并存）

| 路线 | 模块 | 调用链 | 问题 |
|------|------|--------|------|
| A | Herbs/Formula/Patients/Users/Registrations | Controller → ISender.Send → Handler → Repository | 40 个 trivial Handler 纯透传 |
| B | MedicalCase | Controller → ISender.Send → Handler → Service → Repository | 三层调用，Handler 只是转发 |
| C | Health/Deploy | Controller → Service | 无 MediatR，正确 |

### 2.2 Handler 分类（74 个）

| 类别 | 数量 | 行数 | 特征 |
|------|------|------|------|
| **Trivial** | 18 | 645 | repo→map→return，零逻辑 |
| **Simple** | 32 | 1,487 | entity方法 + repo更新 |
| **Complex** | 26 | 2,553 | 跨模块/安全/业务规则 |

**Trivial Handler 示例**（所有 Get-by-id 都是这个模式）：
```csharp
var entity = await _repo.GetByIdAsync(request.Id);
if (entity == null) return Result.Failure(ErrorCode.NotFound, "不存在");
return Result.Success(Mapper.ToDetailDto(entity));
```

### 2.3 重复代码指纹

| 模式 | 重复次数 | 可提取 |
|------|---------|--------|
| Create Handler（Herbs/Formula/Patients） | 3 个完全相同 | 泛型 CreateHandler\<T\> |
| Delete Handler | 3 个完全相同 | 泛型 DeleteHandler\<T\> |
| GetById Query Handler | 3 个完全相同 | 泛型 GetByIdHandler\<T\> |
| GetList Query Handler | 3 个完全相同 | 泛型 GetListHandler\<T\> |
| Repository CRUD 方法 | 3 仓库 × 8 方法 | 泛型 BaseRepository\<T\> |
| Controller CRUD 动作 | 4 个控制器 | 用 BaseCrudController 模板方法 |

### 2.4 CrossModule 依赖

| 接口 | 实现 | 消费者 |
|------|------|--------|
| ICrossModuleService（门面） | CrossModuleService | Auth/Formula/MedicalCase/Registration |
| IHerbCrossModuleService | HerbCrossModuleService | 仅 CrossModuleService |
| IPatientCrossModuleService | PatientCrossModuleService | 仅 CrossModuleService |
| IUserCrossModuleService | UserCrossModuleService | 仅 CrossModuleService |
| IRegistrationCrossModuleService | RegistrationCrossModuleService | MedicalCase |
| IMedicalCaseCrossModuleService | MedicalCaseReferenceService | Patients |

**6 个死方法**：PatientExistsAsync / CheckPatientReferenceAsync / CheckHerbReferenceAsync / GetPatientsBasicInfoAsync / UserExistsAsync / UpdateUserPasswordHashAsync — 零调用者。

---

## 三、死代码清单（确认可删除）

### Server 层

| # | 文件/方法 | 行数 | 证据 |
|---|----------|------|------|
| 1 | ISystemLogRepository + SystemLogRepository | 42 | 零注册、零注入 |
| 2 | RepositoryServiceCollectionExtensions | 45 | 零调用 |
| 3 | AggregateRoot.cs + Entity.cs（SharedKernel） | ~60 | 零继承 |
| 4 | DtoBase.cs（5个类/接口） | ~40 | 零继承 |
| 5 | ErrorCodeExtensions.cs | ~30 | 零调用 |
| 6 | UserInputDtoValidator | ~20 | 零注册 |
| 7 | ICrossModuleService 6个死方法 | ~30 | 零调用者 |
| 8 | IRegistrationCrossModuleService.HasWaitingRegistrationAsync | 5 | 零调用者 |
| 9 | MedicalCaseServiceHelper 2个静态方法 | ~80 | 零调用者 |
| 10 | IMedicalCaseStateService.CloseCaseAsync | 5 | 零调用者 |

### Desktop 层

| # | 文件/方法 | 行数 | 证据 |
|---|----------|------|------|
| 11 | PatientListToDetailMapper | ~10 | 零引用 |
| 12 | 6 个 NotSupportedException 桩 | ~30 | 永远抛异常 |
| 13 | IEditable 接口 + 实现 | ~80 | 零调用 |
| 14 | 4 个未绑定 XAML 的 Command | ~60 | 无 UI 绑定 |
| 15 | Desktop 5 个 Service（纯 try/catch 包装） | ~1,700 | 可泛型化 |

### Shared 层

| # | 文件 | 行数 | 证据 |
|---|------|------|------|
| 16 | AggregateRoot.cs | ~30 | 零继承 |
| 17 | Entity.cs | ~30 | 零引用 |

**合计确认死代码**：~537 行（Server+Shared） + ~200 行（Desktop）= **~737 行可直接删除**
**可优化重复代码**：~2,400 行（Handler/Repo/Controller） + ~1,700 行（Desktop Service）= **~4,100 行可泛型化**

---

## 四、架构测试健康度

| 指标 | 数值 |
|------|------|
| 总测试文件 | 7 |
| 总测试方法 | ~82 |
| 有价值的守卫 | ~45（55%） |
| 死/跳过测试 | 4 |
| 重复测试对 | 4 |
| 弱/表面测试 | 5 |
| 测试缺口 | 13 个领域未覆盖 |

### 死测试
1. ContentHosting_Controls_Should_Not_Set_DataContext_In_Constructor — [Skip]
2. Batch2_DirectoryNamespace_Frontend — 只扫后端，永远 pass
3. AR001_MedicalCase_Should_Be_Aggregate_Ruthor — 一次性迁移检查
4. Batch2_SingleSource_Cache_Should_Not_Have_Duplicate_Registration — 回归守卫

### 测试缺口（应测未测）
1. LYBT.Shared.Models 层纯净性
2. Entities 层是否过胖
3. 接口隔离度
4. DI 注册一致性
5. 异步贯穿
6. 可空引用类型
7. 错误处理一致性
8. 上帝类防护
9. 日志模式一致性
10. 数据库表命名一致性
11. HTTP 状态码一致性
12. DI 容器循环引用
13. Desktop 导航抽象完整性

---

## 五、文档健康度

### 5.1 目录结构

| 目录 | 文件数 | 状态 |
|------|--------|------|
| compose/plans/ (active) | 56 | 实际需要的仅 3 个 |
| compose/plans/archive/ | 41 | ✅ 已归档 |
| compose/specs/ (active) | 64 | 大部分已完成 |
| compose/specs/archive/ | 22 | ✅ 已归档 |
| compose/reports/ | 26 | 有参考价值 |
| 03-architecture/ | 24 | ✅ 有效 |
| 03-architecture/decisions/ | 17 | ✅ 有效（ADR） |
| 02-requirements/ | 18 | ✅ 有效 |
| 06-operations/ | 15 | ✅ 有效 |
| 05-development/ | 14 | ✅ 有效 |

### 5.2 问题

| 问题 | 严重度 | 数量 |
|------|--------|------|
| 已完成计划未归档 | 中 | 35 个 |
| 可直接删除的存根 | 低 | 17 个 |
| 断链 | 中 | 27 个 |
| 日期错误（2025→2026） | 低 | 3 个 |
| 孤立文件 | 低 | 4 个 |
| 命名不规范 | 低 | 75+ 个 |

### 5.3 必须保留的技术洞察

| # | 主题 | 关键发现 |
|---|------|---------|
| 1 | BaseCrudController 设计教训 | 子类覆盖 50%+ 抽象方法 → 抽象边界错误 → 改用 virtual 默认 |
| 2 | LocalWebAPI/Server 重复 | 12 个 LocalWebAPI Controller 是 Server 副本 → 需要共享项目 |
| 3 | Desktop→Server 实体依赖 | LocalDbContext 引用 Server 实体 → 需要 ApplicationUser 提取 |
| 4 | DI Singleton 审计 | 48 个 Singleton 无 captive dependency，但 ILogger 生命周期不一致 |
| 5 | Shell 3 个关键 Bug | 登出状态机错误 / 并发登录竞态 / 异常时事件未发布 |
| 6 | Sync-over-Async 死锁 | .GetAwaiter().GetResult() 在 WPF UI 线程上是死锁炸弹 |
| 7 | 离线同步分支放弃 | 3 个致命问题 → 标记 v2.0 重新实现 |
| 8 | Newman 替代 C# 集成测试 | 45 个测试文件（10,731 行）被删除 → Newman 86 端点全覆盖 |

### 5.4 未实现 P0 项

1. 明文密码泄露（SSH/SA/JWT SecretKey）
2. LocalWebAPI/Server Controller 重复
3. Desktop→Server 实体依赖
4. 离线同步 v2.0
5. 文档不同步（5+ 处事实错误）

---

## 六、用户设计目标

> 满足功能 → 安全 → 简洁 → 整齐 → 易读 → 不过度设计

**MediatR 策略**：保留用于复杂业务流程，trivial CRUD 改为直接注入。

**架构测试约束**（不可违反）：
- P07: 模块间禁止直接引用
- P08: 跨模块必须用接口
- P10: Service 禁注入 AppDbContext（仅 Repository/Base 可）

---

## 七、建议的执行顺序

1. **清理文档**（低风险）→ 删除存根、归档完成计划、修复断链
2. **清理死代码**（中风险）→ 删除确认的 737 行死代码
3. **讨论产品需求**（Phase 1 需求澄清）→ 确定功能优先级
4. **架构方案评审**（Phase 2 多角度评审）→ MediatR 简化、重复代码泛型化
5. **逐任务执行**（Mimo Code 派遣）→ 每任务验证
