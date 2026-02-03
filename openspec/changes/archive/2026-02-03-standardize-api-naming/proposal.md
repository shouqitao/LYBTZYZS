# standardize-api-naming

## Why

API接口代码审查发现多处命名和设计不一致，影响代码可维护性和API契约清晰度。

### 发现的问题

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| IHerbApi.BatchImportAsync | 传输方式 | `[Multipart]` StreamPart | `[Body]` JSON |
| IHerbApi.BatchImportAsync | URL路径 | `/api/v1/herbs/import` | `/api/v1/herbs/batch-import` |
| IFormulaApi.BatchImportAsync | URL路径 | `/api/v1/formulas/import` | `/api/v1/formulas/batch-import` |
| IMedicalCaseApi.DeleteMedicalCaseAsync | 返回类型 | `Refit.IApiResponse` | `ApiResponse` |
| IAuthApi.ChangeSysAdminPasswordAsync | URL命名 | `/changeSysAdminPassword` | `/change-sysadmin-password` |

### 影响分析

1. **IHerbApi BatchImport** - 其他批量导入接口（如患者）使用JSON Body，药材导入使用Multipart不一致
2. **IFormulaApi BatchImport** - URL路径使用`/import`而非标准的`/batch-import`，与批量操作命名规范不一致
3. **IMedicalCaseApi Delete返回类型** - 其他API统一使用`ApiResponse`/`ApiResponse<T>`，此处使用底层`IApiResponse`
4. **IAuthApi URL风格** - 项目URL统一使用kebab-case，此处使用camelCase

## What Changes

### Phase 1: 修复IAuthApi URL命名（Low Risk）

修改IAuthApi.ChangeSysAdminPasswordAsync的URL路径：
- Before: `/api/v1/auth/changeSysAdminPassword`
- After: `/api/v1/auth/change-sysadmin-password`

**注意**: 需要同步修改Server端Controller路由

### Phase 2: 修复IMedicalCaseApi返回类型（Low Risk）

修改IMedicalCaseApi.DeleteMedicalCaseAsync的返回类型：
- Before: `Task<Refit.IApiResponse>`
- After: `Task<ApiResponse>`

此修改不影响调用方，`ApiResponse`是`IApiResponse`的包装类型。

### Phase 3: 统一批量导入URL路径（Medium Risk）

统一 IHerbApi 和 IFormulaApi 的批量导入URL路径，遵循 `/batch-{action}` 命名规范。

**IHerbApi选项:**

**选项A: 统一为JSON Body**
- 修改Desktop端接口为`[Body] HerbBatchImportInputDto`
- 修改Server端Controller接受JSON
- 优点：与其他批量操作一致
- 缺点：需要修改Server端

**选项B: 仅修复URL路径（推荐）**
- 保留Multipart方式
- 仅将`/import`改为`/batch-import`
- 优点：改动小
- 缺点：传输方式仍不一致

**建议**: 采用选项B，仅修复URL命名，减少改动范围。Multipart方式适合文件上传场景，语义上合理。

**IFormulaApi修改:**
- Before: `/api/v1/formulas/import`
- After: `/api/v1/formulas/batch-import`
- 同步修改Server端Controller路由

### 不修改的内容

**BatchDeleteInputDto命名**
- 当前：`BatchDeleteInputDto`用于批量删除、启用、禁用操作
- 分析：该DTO仅包含`Ids`字段，作为通用的"批量ID操作"输入是合理的
- 决定：暂不修改，若需语义清晰可在未来重构为`BatchIdsInputDto`

## Architecture

### API命名规范（确认）

| 类别 | 规范 | 示例 |
|------|------|------|
| URL路径 | kebab-case | `/api/v1/herbs/batch-import` |
| 批量操作 | `/batch-{action}` | `batch-delete`, `batch-enable` |
| 返回类型 | `ApiResponse<T>` 或 `ApiResponse` | 不使用`IApiResponse` |
| 方法命名 | `{Action}{Entity}Async` | `CreateHerbAsync`, `BatchDeleteAsync` |

### 变更影响范围

```
Desktop端（本次修改）:
├── LYBT.Desktop.Contracts/Api/
│   ├── IAuthApi.cs              - URL路径修改
│   ├── IHerbApi.cs              - URL路径修改
│   ├── IFormulaApi.cs           - URL路径修改
│   └── IMedicalCaseApi.cs       - 返回类型修改

Server端（需同步修改）:
├── LYBT.Module.Auth/
│   └── Controllers/AuthController.cs - 路由修改
├── LYBT.Module.Herbs/
│   └── Controllers/HerbsController.cs - 路由修改
└── LYBT.Module.Formula/
    └── Controllers/FormulasController.cs - 路由修改
```

## Impact

- **文件变更**: Desktop 4个文件 + Server 3个文件
- **风险等级**: Medium（多处API契约变更，Breaking Change）
- **测试要求**: 手动测试导入功能（药材、验方）、密码修改功能、医案删除功能

## Risks

| 风险 | 缓解措施 |
|------|----------|
| Server端路由未同步 | 同步修改Server端Controller（Auth、Herbs、Formula） |
| 调用方未适配返回类型 | ApiResponse兼容IApiResponse |
| 药材批量导入功能中断 | 保留Multipart方式，仅改URL |
| 验方批量导入功能中断 | 同步修改Server端FormulasController路由 |

## References

- Code Review Report: API命名一致性审查 (2026-01-07)
- OpenSpec: standardize-api-architecture (2026-01-07归档)
