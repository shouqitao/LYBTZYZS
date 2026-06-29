# 剩余架构问题修复规格

> 日期: 2026-06-29
> 状态: 待审
> 来源: 方法级架构审计（19个问题）
> 前置: 已完成 W1-W6, S1-S6 架构优化

## [S1] CRITICAL 问题

### W1: BaseUsersController 胖控制器（588行）

**位置**: `LYBT.Module.Users/Controllers/BaseUsersController.cs`

**问题方法**:
- `GetList()` L44-99: EF Core IQueryable 操作 + N+1 GetRolesAsync
- `Create()` L153-213: Identity 操作 + 角色分配
- `Update()` L223-287: 角色同步逻辑
- `Delete()` L296-334: 权限检查 + 删除
- `ToggleStatus()` L451-491: 锁定/解锁逻辑
- `BatchDelete()` L500-562: 逐项权限检查

**修复方案**: 提取业务逻辑到 `IUserService`，Controller 仅做路由+响应映射

### M1: CrossModuleService God Service

**位置**: `LYBT.Infrastructure/Services/CrossModuleService.cs`

**问题方法**:
- `GetPatientBasicInfoAsync()` L36: 直接查 Patient 表
- `GetPatientsBasicInfoAsync()` L53-85: N+1 foreach 查询
- `CheckPatientReferenceAsync()` L96: 直接查 MedicalCase
- `GetHerbBasicInfoAsync()` L117: 直接查 Herb 表
- `GetHerbPricesAsync()` L166-189: N+1 foreach 查询
- `GetUserBasicInfoAsync()` L224: 直接查 User 表
- `UpdateUserPasswordHashAsync()` L285: 直接更新 User

**修复方案**: 将 Patient 查询移到 `LYBT.Module.Patients` 实现，Herb 查询移到 `LYBT.Module.Herbs`，User 查询移到 `LYBT.Module.Users`

## [S2] HIGH 问题

### W2: AuthController 硬编码 Token 过期

**位置**: `LYBT.WebAPI/Controllers/AuthController.cs:98`
**代码**: `ExpiresAt = DateTime.UtcNow.AddMinutes(60)`
**修复**: 注入 `IOptions<JwtOptions>`，使用配置值

### W3: AuthController 响应模式不一致

**位置**: `LYBT.WebAPI/Controllers/AuthController.cs:143-185`
**问题**: `Unauthorized(ApiResponse<object>.CreateFail(..., new { code = "TokenInvalid" }))` 混用匿名对象；`StatusCode(405, new { message = "..." })` 绕过 ApiResponse
**修复**: 统一使用 `ErrorCode` 枚举 + `ApiResponse` 封装

### W4: RegistrationsController TransactionScope

**位置**: `LYBT.WebAPI/Controllers/RegistrationsController.cs:63-122`
**问题**: TransactionScope + try/catch/throw 在 Controller 层；空 catch 块 `catch { throw; }` 是死代码
**修复**: 将事务逻辑移到 `IRegistrationService.QuickVisitAsync()`

## [S3] MEDIUM 问题

| ID | 位置 | 问题 | 修复 |
|----|------|------|------|
| W5 | DiagnosticsController.cs:129 | `BadRequest(new { error = "..." })` 绕过 ApiResponse | 使用 `ValidationFail()` |
| W6 | HealthController.cs | 匿名类型响应，无类型 DTO | 创建 `HealthStatusDto` |
| M2 | CrossModuleService.cs:53-85 | N+1 查询模式 | 使用 `IN (...)` 批量查询 |
| M3 | BaseUsersController.cs:44-99 | Controller 中 IQueryable 操作 | 移到 Service 层 |
| S1 | MainWindowViewModel.cs:140 | 12 个构造函数依赖 | 考虑聚合服务 |
| I1 | BaseRepository.cs:678-694 | 全局禁用 RowVersion 并发 | 选择性启用 |
| D1 | SettingsService.cs | 仅内存存储，无持久化 | 持久化到 JSON 文件 |

## [S4] LOW 问题

| ID | 位置 | 问题 | 修复 |
|----|------|------|------|
| W7 | 所有 Controller | CreatedAtAction 硬编码 `version = "1"` | 定义常量 |
| S2 | MainWindowViewModel.cs:532 | 魔法数字 `Task.Delay(500)` | 提取常量 |
| S3 | MainWindowViewModel.cs:517-521 | 重复调用 ShowLoginDialog | 简化 |
| I2 | ApiErrorCodes.cs | 仅 1 个错误代码，死代码 | 废弃删除 |
| I3 | BaseApiController.cs | HandleAuthResult 与 HandleResult 重复 | 合并 |
| D2 | SettingsService.cs:63-68 | 硬编码默认设置 | 从配置读取 |
| D3 | IAuthApi.cs:29,67 | 文档矛盾（8h vs 15min vs 60min） | 统一 |

## [S5] 实施优先级

| 批次 | 任务 | 工作量 |
|------|------|--------|
| 1 | W1: BaseUsersController 提取 UserService | 高 |
| 2 | M1: CrossModuleService 拆分到各模块 | 高 |
| 3 | W2+W3: AuthController 安全+一致性 | 中 |
| 4 | W4: RegistrationsController 事务移到 Service | 中 |
| 5 | W5+W6: Diagnostics/Health 响应统一 | 低 |
| 6 | M2+M3: 查询优化 | 中 |
| 7 | S1: MainWindowViewModel 依赖聚合 | 低 |
| 8 | I1: BaseRepository 并发策略 | 中 |
| 9 | D1: SettingsService 持久化 | 低 |
| 10 | W7+S2+S3+I2+I3+D2+D3: LOW 清理 | 低 |

## [S6] 验收标准

1. BaseUsersController 行数 < 100（当前 588）
2. CrossModuleService 不存在，查询分散到各模块
3. AuthController 无硬编码值，响应统一使用 ApiResponse
4. RegistrationsController 无事务逻辑
5. 所有 Controller 响应统一使用 ApiResponse 封装
6. 无 N+1 查询
7. MainWindowViewModel 依赖 < 10 个
8. BaseRepository 并发策略可配置
9. SettingsService 持久化到文件
10. 所有现有测试通过
