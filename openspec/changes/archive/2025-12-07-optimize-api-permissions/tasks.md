# Implementation Tasks: optimize-api-permissions

> **Status: COMPLETED** - 2025-12-07 19:45 CST

## Phase 1: Authorization Infrastructure (P1-基础设施) - DONE

### Task 1.1: 注册授权策略 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`
- **描述:** 添加`AdminOnly`和`DoctorOrAdmin`授权策略
- **完成:** 策略已注册，支持SuperAdmin/Admin/Doctor角色

### Task 1.2: 创建Formula授权基础设施 ✅
- **文件:**
  - `src/Server/Services/LYBT.WebAPI/Authorization/FormulaOperations.cs` (新建)
  - `src/Server/Services/LYBT.WebAPI/Authorization/FormulaAuthorizationHandler.cs` (新建)
- **完成:** Handler已注入DI容器

## Phase 2: Controller层权限配置 (P2-Controller) - DONE

### Task 2.1: UsersController添加AdminOnly策略 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`
- **完成:** 已添加`[Authorize(Policy = "AdminOnly")]`

### Task 2.2: PatientsController添加DoctorOrAdmin策略 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`
- **完成:** 已添加`[Authorize(Policy = "DoctorOrAdmin")]`

### Task 2.3: HerbsController添加DoctorOrAdmin策略 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`
- **完成:** 已添加`[Authorize(Policy = "DoctorOrAdmin")]`

### Task 2.4: FormulasController权限配置 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`
- **完成:** 类级`[Authorize(Policy = "DoctorOrAdmin")]`已添加

### Task 2.5: MedicalCaseController权限增强 ✅
- **文件:** `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **完成:** Create通过MedicalCasePermissionService限制Admin

## Phase 3: Service层查询过滤 (P3-Service) - DONE

### Task 3.1: FormulaService角色过滤 ✅
- **文件:**
  - `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`
  - `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- **完成:** GetPagedAsync添加currentUserId和isAdmin参数，Doctor只能看到自己的或IsShared验方

### Task 3.2: MedicalCaseQueryService角色过滤 ✅
- **完成:** 已有角色过滤逻辑，无需修改

## Phase 4: Authorization Handler增强 (P4-Handler) - DONE

### Task 4.1: MedicalCasePermissionService增强 ✅
- **文件:** `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCasePermissionService.cs`
- **完成:** CanCreate方法已修改，Admin不能创建医案，只有Doctor可以

## Phase 5: 测试与验证 (P5-Testing) - DONE

### Task 5.1: 单元测试更新 ✅
- **文件:** `tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/Services/FormulaServiceTests.cs`
- **完成:** 更新GetPagedAsync测试适配新方法签名

### Task 5.2: 编译与测试验证 ✅
- **MedicalCase测试:** 42/42 通过
- **Formula测试:** 22/22 通过
- **编译:** 0错误

---

## Summary

| Phase | Tasks | 预估工作量 |
|-------|-------|-----------|
| P1-基础设施 | 2 | 1.5h |
| P2-Controller | 5 | 2.25h |
| P3-Service | 2 | 1.5h |
| P4-Handler | 1 | 1h |
| P5-Testing | 3 | 5h |
| **Total** | **13** | **11.25h** |

## Dependencies

```
P1 ─┬─► P2 ─┬─► P5
    │       │
    └─► P3 ─┤
            │
    P4 ─────┘
```

- P2/P3依赖P1(策略注册)
- P5依赖P2/P3/P4完成
