# Shared 层设计

> Shared 层是跨 Server/Desktop 的公共基础，位于 `src/Shared/`。2026-08 实体源统一后从 Core 层移出。

---

## 项目清单

| 项目 | 职责 | 关键类 |
|------|------|--------|
| **LYBT.Entities** | 领域实体定义（贫血模型为主） | `BaseEntity`, `MedicalCaseModel`(聚合根) |
| **LYBT.Shared.Models** | 共享 DTO/枚举/原语 | `ApiResponse<T>`, `PagedResult<T>`, `ErrorCodes` |
| **LYBT.Shared.Configuration** | 配置管理 | `ConnectionStringResolver`, `ConfigurationValidation` |
| **LYBT.Shared.ExceptionHandling** | 异常处理链 | `IExceptionHandler`, `NotFoundException`, `BusinessException` |
| **LYBT.Shared.Logging** | Serilog 日志 | `SensitiveDataMasker`, CorrelationId, Bootstrap |

---

## LYBT.Entities

所有领域实体继承 `BaseEntity`，默认贫血模型。

**BaseEntity 字段**: `Id`(Guid), `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `RowVersion`(byte[]), `IsDeleted`

**例外**: `MedicalCaseModel` 是唯一 DDD 聚合根（充血模型），包含 `Complete()`, `Suspend()`, `SoftDelete()`, `UpdateConsultation()` 域方法。

**关键目录**: `Auth/`, `Common/`, `Formulas/`, `Herbs/`, `MedicalCases/`, `Patients/`, `Registrations/`, `Users/`

> 无外部依赖，仅引用 .NET BCL。

---

## LYBT.Shared.Models

**统一响应格式**:
- `ApiResponse<T>` — API 统一响应（success/data/message/timestamp）
- `Result<T>` — Service 层操作结果（Success/Failure）
- `PagedResult<T>` — 分页查询（items/totalCount/currentPage/pageSize/totalPages）

**错误码**: 5 位数字 MCCEE 格式（模块1位+子类别2位+序号2位），位于 `Primitives/ErrorCodes/`

**枚举**: `UserRole`, `CommonStatus`, `MedicalCaseStatus`, `RegistrationSource` 等

---

## LYBT.Shared.Configuration

- `ConnectionStringResolver` — 三级回退获取连接串（环境变量→appsettings→默认）
- `ConfigurationPostProcessor` — 占位符展开验证（`${VAR}` 未展开→回退下一级）
- `ConfigurationValidator` — 启动时配置契约校验（JWT/密码/连接串）

---

## LYBT.Shared.ExceptionHandling

异常→HTTP 映射由 `IExceptionHandler` 处理器链完成：

| 异常类型 | HTTP 状态码 | 场景 |
|----------|-----------|------|
| `ValidationException` | 400 | FluentValidation 失败 |
| `BusinessException` | 400 | 业务规则违反 |
| `NotFoundException` | 404 | 资源不存在 |
| `ForbiddenException` | 403 | 权限不足 |
| `ConflictException` | 409 | 乐观锁冲突 |

---

## LYBT.Shared.Logging

- **Bootstrap**: Serilog 两阶段初始化（启动阶段→模块注册后）
- **CorrelationId**: W3C Activity，全链路追踪
- **SensitiveDataMasker**: 日志脱敏（身份证、手机号等）
- **Http**: HTTP 请求日志增强

---

## 设计依据

| 决策 | 依据 |
|------|------|
| 实体下沉 Shared | 2026-08-02：Server/Desktop 共用实体，避免重复定义 |
| BaseEntity 字段约定 | ADR-0001 + 04-data-model.md |
| 错误码 MCCEE 格式 | 5位数字：模块(1)+子类别(2)+序号(2)，90+ 错误场景 |
| ConfigurationValidator | 2026-08-12：启动拦截未配置项，避免隐晦错误 |
