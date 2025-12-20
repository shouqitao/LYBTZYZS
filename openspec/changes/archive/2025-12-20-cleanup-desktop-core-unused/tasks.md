# Tasks: cleanup-desktop-core-unused

## Phase 1: 删除Foundation项目无用文件

### Task 1.1: 删除CommandFactory
- [x] 删除 `Foundation/Commands/CommandFactory.cs`
- [x] 删除空目录 `Foundation/Commands/`
- [x] 删除测试文件 `CommandFactoryTests.cs`
- [x] 验证编译通过

### Task 1.2: 删除DiagnosticService
- [x] 删除 `Foundation/Diagnostics/DiagnosticService.cs`
- [x] 删除空目录 `Foundation/Diagnostics/`
- [x] 验证编译通过

### Task 1.3: 删除SecurityService
- [x] 删除 `Foundation/Security/SecurityService.cs`
- [x] 移除DI注册 (FoundationServiceCollectionExtensions.cs)
- [x] 验证编译通过
- 注：保留其他Security文件(IAuthenticationService等正在使用)

### Task 1.4: 删除IUnifiedApiClientManager
- [x] 删除 `Foundation/Api/Managers/IUnifiedApiClientManager.cs`
- [x] 删除空目录 `Foundation/Api/Managers/`
- [x] 删除空目录 `Foundation/Api/`
- [x] 验证编译通过

### Task 1.5: 删除BaseApiRepository
- [x] 删除 `Foundation/Repositories/BaseApiRepository.cs`
- [x] 删除空目录 `Foundation/Repositories/`
- [x] 验证编译通过

## Phase 2: 删除Infrastructure项目无用文件

### Task 2.1: 删除Components目录
- [x] 删除 `Infrastructure/Components/CommandHandlerBase.cs`
- [x] 删除 `Infrastructure/Components/ComponentValidatorBase.cs`
- [x] 删除空目录 `Infrastructure/Components/`
- [x] 验证编译通过

### Task 2.2: 删除EnhancedNavigationService
- [x] 删除 `Infrastructure/Services/Navigation/EnhancedNavigationService.cs`
- [x] 删除空目录 `Infrastructure/Services/Navigation/`
- [x] 验证编译通过

### Task 2.3: 删除冗余CorrelationIdContext
- [x] 删除 `Infrastructure/Logging/CorrelationIdContext.cs`
- [x] 更新引用使用Foundation版本:
  - CorrelationIdEnricher.cs
  - CorrelationIdDelegatingHandler.cs
  - ClientErrorMessageMapper.cs
  - ViewModelBase.cs
- [x] 验证编译通过
- 注：保留CorrelationIdEnricher和DesktopSerilogConfiguration

## Phase 3: 删除Presentation项目无用文件

### Task 3.1: 删除ThemeService
- [x] 删除 `Presentation/Theming/ThemeService.cs`
- [x] 保留 `Presentation/Theming/MedicalCaseStyles.xaml`
- [x] 验证编译通过

### Task 3.2: 删除INavigationService
- [x] 删除 `Presentation/Navigation/INavigationService.cs`
- [x] 删除空目录 `Presentation/Navigation/`
- [x] 验证编译通过

## Phase 4: 清理DI注册

### Task 4.1: 更新PresentationServiceCollectionExtensions
- [x] 移除 `using LYBT.Desktop.Presentation.Theming`
- [x] 移除 `AddSingleton<IThemeService, ThemeService>()` 注册
- [x] 验证编译通过

### Task 4.2: 更新FoundationServiceCollectionExtensions
- [x] 移除 `AddSingleton<SecurityService>()` 注册
- [x] 移除相关注释
- [x] 验证编译通过

## Phase 5: 验证

### Task 5.1: 全量编译验证
- [x] 运行 `dotnet build LYBT.All.sln -c Release`
- [x] 确认0错误0警告

### Task 5.2: 更新proposal状态
- [x] 更新proposal.md状态为Implemented

---

## 完成摘要

### 删除的文件 (12个)

**Foundation项目 (5个)**:
- Commands/CommandFactory.cs
- Diagnostics/DiagnosticService.cs
- Security/SecurityService.cs
- Api/Managers/IUnifiedApiClientManager.cs
- Repositories/BaseApiRepository.cs

**Infrastructure项目 (4个)**:
- Components/CommandHandlerBase.cs
- Components/ComponentValidatorBase.cs
- Services/Navigation/EnhancedNavigationService.cs
- Logging/CorrelationIdContext.cs

**Presentation项目 (2个)**:
- Theming/ThemeService.cs
- Navigation/INavigationService.cs

**测试项目 (1个)**:
- CommandFactoryTests.cs

### 修改的文件 (6个)

1. **PresentationServiceCollectionExtensions.cs** - 移除ThemeService注册
2. **FoundationServiceCollectionExtensions.cs** - 移除SecurityService注册
3. **CorrelationIdEnricher.cs** - 使用Foundation.Logging.CorrelationIdContext
4. **CorrelationIdDelegatingHandler.cs** - 使用Foundation.Logging.CorrelationIdContext
5. **ClientErrorMessageMapper.cs** - 使用Foundation.Logging.CorrelationIdContext
6. **ViewModelBase.cs** - 使用Foundation.Logging.CorrelationIdContext

### 净删除代码量
约900行无用代码
