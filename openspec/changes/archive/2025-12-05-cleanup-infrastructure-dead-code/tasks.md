# 任务清单: cleanup-infrastructure-dead-code

## Phase 1: 删除死代码

### Task 1.1: 删除 LogSanitizer.cs
- [x] 确认无外部引用: `grep -r "LogSanitizer" src/`
- [x] 删除文件: `src/Server/Core/LYBT.Infrastructure/Utilities/LogSanitizer.cs`
- [x] 编译验证

### Task 1.2: 删除 IRepositoryLegacy.cs.deleted
- [x] 删除文件: `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepositoryLegacy.cs.deleted`

### Task 1.3: 清理 SeedDataService
- [x] 检查 `SuperAdminId` 引用 (无引用)
- [x] 检查 `Seed()` 方法引用 (无引用)
- [x] 如无引用则删除文件，否则仅删除未使用方法 (已删除整个文件)
- [x] 编译验证

**Phase 1 验收**:
- [x] `dotnet build LYBT.All.sln` 成功
- [x] 无 `[Obsolete]` 标记的代码残留

---

## Phase 2: 消除冗余代码

### Task 2.1: 删除自定义 ServiceLifetime 枚举
- [x] 检查内部使用情况 (仅AddRepository方法使用)
- [x] 修改 `AddRepository` 方法签名使用 .NET 原生枚举 (已使用Microsoft.Extensions.DependencyInjection.ServiceLifetime)
- [x] 删除 `ServiceLifetime` 枚举定义 (lines 137-142)
- [x] 编译验证

### Task 2.2: 删除空方法
- [x] 确认 `AddServerRepositories` 无调用 (有调用，保留)
- [x] 确认 `AddRepositorySupportServices` 无调用 (无调用，已删除)
- [x] 删除未使用的空方法
- [x] 编译验证

**Phase 2 验收**:
- [x] `dotnet build LYBT.All.sln` 成功
- [x] 无冗余枚举定义
- [x] 无空方法体 (AddServerRepositories保留，有外部调用)

---

## Phase 3: ValidationHelper 迁移

### Task 3.1: 创建新文件
- [x] 创建 `MedicalCaseValidationHelper.cs` 在 MedicalCase 模块
- [x] 复制 `IsValidMedicalCaseStatusTransition` 方法
- [x] 重命名为 `IsValidStatusTransition`

### Task 3.2: 更新引用
- [x] 修改 `MedicalCaseStateService.cs` 使用新类
- [x] 编译验证

### Task 3.3: 删除原文件
- [x] 确认无其他引用
- [x] 删除 `src/Server/Core/LYBT.Infrastructure/Utilities/ValidationHelper.cs`
- [x] 编译验证

**Phase 3 验收**:
- [x] `dotnet build LYBT.All.sln` 成功
- [x] MedicalCase 测试通过 (42个测试全部通过)
- [x] Infrastructure 层无领域特定逻辑

---

## Phase 4: 代码简化 (可选)

### Task 4.1: 简化 ApiErrorCodes
- [x] 审查所有错误码使用情况 (仅DATASAVEFAILED被使用)
- [x] 保留实际使用的错误码
- [x] 删除未使用的错误码 (从50+简化到12个核心常量)
- [x] 编译验证

### Task 4.2: 修复 ConfigurationExtensions
- [x] 检查 `MemoryCacheConfig` 类定义
- [x] 实现 `MapToLegacyMemoryCacheConfig` 映射逻辑
- [x] 编译验证

**Phase 4 验收**:
- [x] `dotnet build LYBT.All.sln` 成功
- [x] 无空实现方法

---

## Phase 5: 统一配置清理 (用户反馈追加)

### Task 5.1: 删除遗留配置兼容层
- [x] 用户反馈: "统一配置统一设计，不考虑兼容"
- [x] 检查 CacheOptions/MemoryCacheConfig 使用情况 (无引用)
- [x] 删除 `src/Server/Core/LYBT.Infrastructure/Configuration/Options/CacheOptions.cs`
- [x] 简化 `ConfigurationExtensions.cs` 移除兼容映射逻辑
- [x] 编译验证 (0警告, 0错误)

**Phase 5 验收**:
- [x] `dotnet build LYBT.All.sln` 成功
- [x] 无遗留配置兼容代码

---

## 最终验收

- [x] **CLEAN-001**: 所有 `[Obsolete]` 代码已删除 (LogSanitizer.cs)
- [x] **CLEAN-002**: 无 `.deleted` 后缀文件 (IRepositoryLegacy.cs.deleted)
- [x] **CLEAN-003**: 无未使用的 public 方法 (AddRepositorySupportServices, 自定义ServiceLifetime枚举)
- [x] **CLEAN-004**: Infrastructure 层无领域特定逻辑 (ValidationHelper已迁移到MedicalCase模块)
- [x] **CLEAN-005**: 全量测试通过 (所有测试通过，包括183个Infrastructure测试和42个MedicalCase测试)

```bash
# 最终验证命令
dotnet build LYBT.All.sln -c Release --no-restore
dotnet test tests/ --no-build
grep -r "\[Obsolete" src/Server/Core/LYBT.Infrastructure/
find src/ -name "*.deleted"
```

## 执行记录

- **执行日期**: 2025-12-05
- **执行者**: Claude Code
- **Phase 1-5**: 全部完成

### 变更文件清单

**删除的文件**:
- `src/Server/Core/LYBT.Infrastructure/Utilities/LogSanitizer.cs`
- `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepositoryLegacy.cs.deleted`
- `src/Server/Core/LYBT.Infrastructure/Data/Seeding/SeedDataService.cs`
- `src/Server/Core/LYBT.Infrastructure/Utilities/ValidationHelper.cs`
- `src/Server/Core/LYBT.Infrastructure/Configuration/Options/CacheOptions.cs` (Phase 5)

**修改的文件**:
- `src/Server/Core/LYBT.Infrastructure/DependencyInjection/RepositoryServiceCollectionExtensions.cs`
  - 删除自定义ServiceLifetime枚举
  - 删除空方法AddRepositorySupportServices
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`
  - 更新引用使用MedicalCaseValidationHelper
- `src/Server/Core/LYBT.Infrastructure/Web/ApiErrorCodes.cs`
  - 从50+错误码简化到12个核心常量
  - 保留向后兼容别名DATASAVEFAILED
- `src/Server/Core/LYBT.Infrastructure/Configuration/Extensions/ConfigurationExtensions.cs`
  - Phase 4: 实现MapToLegacyMemoryCacheConfig映射逻辑
  - Phase 5: 移除遗留兼容层，简化为纯LybtOptions配置
- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
  - 修复CS0109警告，移除不必要的new关键字

**新增的文件**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseValidationHelper.cs`
