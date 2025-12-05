# 技术设计: cleanup-infrastructure-dead-code

## 1. 设计概述

本设计文档描述LYBT.Infrastructure项目死代码清理和代码质量提升的具体技术方案。

## 2. Phase 1: 删除死代码

### 2.1 删除 LogSanitizer.cs

**当前状态**:
```csharp
[Obsolete("请使用 LYBT.Infrastructure.Logging.SensitiveDataMasker 替代。此类将在未来版本中移除。")]
public static class LogSanitizer
```

**操作**: 直接删除文件 `src/Server/Core/LYBT.Infrastructure/Utilities/LogSanitizer.cs`

**验证**:
```bash
grep -r "LogSanitizer" src/ # 应返回0结果
dotnet build LYBT.All.sln
```

### 2.2 删除 IRepositoryLegacy.cs.deleted

**当前状态**: 残留的删除标记文件，不是有效的C#源代码

**操作**: 直接删除文件 `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepositoryLegacy.cs.deleted`

### 2.3 清理 SeedDataService.cs

**当前状态**:
- `Seed()` 方法未被任何代码调用
- `SuperAdminId` 常量可能被外部引用

**操作**:
1. 检查 `SuperAdminId` 引用
2. 如无外部引用，删除整个文件
3. 如有引用，保留常量，删除未使用的方法

**验证**:
```bash
grep -r "SeedDataService" src/
grep -r "SuperAdminId" src/
```

## 3. Phase 2: 消除冗余代码

### 3.1 删除自定义 ServiceLifetime 枚举

**当前位置**: `RepositoryServiceCollectionExtensions.cs:137-142`

**问题**: 与 `Microsoft.Extensions.DependencyInjection.ServiceLifetime` 完全重复

**修改方案**:
```csharp
// 删除自定义枚举定义
// public enum ServiceLifetime { ... }

// 修改方法签名，使用全限定名或using别名
public static IServiceCollection AddRepository<TRepository, TImplementation>(
    this IServiceCollection services,
    Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime =
        Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)
```

### 3.2 删除空方法

**目标方法**:
- `AddServerRepositories` - 方法体仅包含注释，无实际代码
- `AddRepositorySupportServices` - 方法体仅包含注释，无实际代码

**验证**:
```bash
grep -r "AddServerRepositories\|AddRepositorySupportServices" src/
```

## 4. Phase 3: ValidationHelper 迁移

### 4.1 迁移计划

**源文件**: `src/Server/Core/LYBT.Infrastructure/Utilities/ValidationHelper.cs`

**目标文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseValidationHelper.cs`

### 4.2 迁移步骤

1. **创建目标文件**:
```csharp
namespace LYBT.Module.MedicalCase.Services;

/// <summary>
/// 病案验证工具类
/// 从Infrastructure层迁移，遵循DDD原则
/// </summary>
public static class MedicalCaseValidationHelper
{
    /// <summary>
    /// 验证病案状态流转是否合法
    /// </summary>
    public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
    {
        return (from, to) switch
        {
            (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,
            (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,
            (MedicalCaseStatus.Active, MedicalCaseStatus.Completed) => true,
            _ => false
        };
    }
}
```

2. **更新引用**:
```csharp
// MedicalCaseStateService.cs
// 原: ValidationHelper.IsValidMedicalCaseStatusTransition(from, to)
// 新: MedicalCaseValidationHelper.IsValidStatusTransition(from, to)
```

3. **删除原文件**: 确认无其他引用后删除

## 5. Phase 4: 代码简化 (可选)

### 5.1 ApiErrorCodes 简化

**当前状态**: 70+ 错误码常量，仅1个被使用

**建议方案**:
- 保留通用错误码（VALIDATION_ERROR, NOT_FOUND, UNAUTHORIZED等）
- 删除未使用的业务特定错误码
- 或标记为"按需添加"，暂不删除

### 5.2 ConfigurationExtensions 修复

**问题**: `MapToLegacyMemoryCacheConfig` 返回空对象

**修复方案**:
```csharp
private static MemoryCacheConfig MapToLegacyMemoryCacheConfig(MemoryCacheConfiguration config)
{
    return new MemoryCacheConfig
    {
        DefaultExpiration = config.DefaultExpirationMinutes,
        SizeLimit = config.SizeLimit
        // 根据实际MemoryCacheConfig类的属性进行映射
    };
}
```

## 6. 测试策略

### 6.1 编译验证
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

### 6.2 单元测试
```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/
```

### 6.3 集成测试
```bash
dotnet test tests/IntegrationTests/
```

## 7. 回滚计划

每个Phase完成后创建Git提交，如需回滚：
```bash
git revert <commit-hash>
```

## 8. 文件清单

### 待删除文件
- `src/Server/Core/LYBT.Infrastructure/Utilities/LogSanitizer.cs`
- `src/Server/Core/LYBT.Infrastructure/Interfaces/IRepositoryLegacy.cs.deleted`
- `src/Server/Core/LYBT.Infrastructure/Utilities/ValidationHelper.cs` (迁移后)
- `src/Server/Core/LYBT.Infrastructure/Data/Seeding/SeedDataService.cs` (如无引用)

### 待修改文件
- `src/Server/Core/LYBT.Infrastructure/DependencyInjection/RepositoryServiceCollectionExtensions.cs`
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`

### 待创建文件
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseValidationHelper.cs`
