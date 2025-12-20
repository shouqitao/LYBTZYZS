# Proposal: consolidate-shared-utilities

## Summary

整合Solution中分散的Helper/Utility/Extension工具类，建立清晰的工具集分层架构。

**核心目标**：
1. 创建 `LYBT.Desktop.Utilities` 项目，统一管理Desktop专用工具
2. 清理未使用代码
3. 消除重复定义
4. 尽可能将工具类迁移到统一工具集，方便代码管理
5. 统一验证消息格式为FluentValidation风格，移除DataAnnotation验证

## Motivation

**用户需求**：
- 项目在增大，方便代码管理是重要指标
- `LYBT.Shared.Utilities` 作为可跨项目复用的服务性代码
- Desktop专用工具需要明确的放置位置
- 尽可能将工具类、帮助类、扩展类迁移到工具集合中

**调研发现**：

| 位置 | 发现静态类 | 可迁移 | 保持原位 |
|------|-----------|--------|---------|
| Desktop.Core | 10个 | 6个 | 4个 |
| Desktop.Shell | 2个 | 0个 | 2个 |
| Desktop.Modules | 1个 | 0个 | 1个 |
| Shared.Utilities | 10个 | - | 已在位 |

## Scope

### Phase 1: 创建 LYBT.Desktop.Utilities 项目

新建项目，建立标准目录结构：

```
src/Client/Desktop/Core/LYBT.Desktop.Utilities/
├── LYBT.Desktop.Utilities.csproj
├── Configuration/        # 配置相关工具
├── Excel/                # Excel操作工具
├── Http/                 # HTTP相关扩展
├── Localization/         # 本地化工具
├── Logging/              # 日志配置
├── Security/             # 安全过滤
└── Constants/            # Desktop专用常量
```

### Phase 2: 迁移工具类

**迁移到 Desktop.Utilities**：

| 原位置 | 类名 | 目标目录 | 依赖分析 |
|--------|------|----------|---------|
| Infrastructure/Helpers | `ExcelHelper` | Excel/ | NPOI依赖 |
| Infrastructure/Configuration | `ConfigurationExtensions` | Configuration/ | 纯配置扩展 |
| Infrastructure/Constants | `SystemConstants` | Constants/ | 无依赖 |
| Infrastructure/Localization | `ClientErrorMessageMapper` | Localization/ | 无依赖 |
| Infrastructure/Logging | `DesktopSerilogConfiguration` | Logging/ | Serilog依赖 |
| Infrastructure/Security | `SensitiveInfoFilter` | Security/ | 无依赖 |
| Foundation/Http | `RetryPolicyExtensions` | Http/ | Polly依赖 |

**保持原位（有明确理由）**：

| 类名 | 位置 | 理由 |
|------|------|------|
| `DataGridSelectionBehavior` | Infrastructure/Behaviors | WPF附加属性，与XAML紧密耦合 |
| `RegionNames` | Infrastructure/Constants | Prism导航常量，Shell依赖 |
| `ErrorHandlingServiceExtensions` | Shell/Extensions | DI配置，就近原则 |
| `ServiceCollectionExtensions` | Shell/Extensions | DI配置，就近原则 |
| `RegionNames` | MedicalCase/Constants | 模块私有常量 |

### Phase 3: 清理与合并

**删除未使用代码**：
- `LYBT.Desktop.Models/Mappers/SimpleMapper.cs` (0引用)

**合并重复定义**：
- 合并 `ValidationConstants` 到 `LYBT.Shared.Models`
- 删除 `LYBT.Shared.Validators/Common/ValidationConstants.cs`

### Phase 4: 更新引用

- 更新所有使用迁移类的项目引用
- 添加对 `LYBT.Desktop.Utilities` 的依赖

### Phase 5: 统一验证消息格式

**背景**：项目同时使用DataAnnotation和FluentValidation两套验证系统，消息格式不一致。

**当前状态**：
- DataAnnotation: 47处 `[Required(ErrorMessage=...)]`，121处 `[StringLength(ErrorMessage=...)]`
- FluentValidation: 74处 `.WithMessage(...)`

**统一为FluentValidation**：
1. 移除DTO属性上的DataAnnotation验证特性（`[Required]`, `[StringLength]`等）
2. 确保FluentValidator覆盖所有验证规则
3. 统一使用 `{PropertyName}` 格式的消息常量
4. 删除DataAnnotation格式的消息常量

## Technical Approach

### 项目创建

```xml
<!-- LYBT.Desktop.Utilities.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NPOI" Version="..." />
    <PackageReference Include="Polly" Version="..." />
    <PackageReference Include="Serilog" Version="..." />
  </ItemGroup>
</Project>
```

### 迁移步骤

1. 创建项目和目录结构
2. 移动文件到新位置，更新命名空间
3. 更新所有引用项目的依赖
4. 编译验证
5. 删除原文件

## Success Criteria

- [ ] LYBT.Desktop.Utilities 项目创建成功
- [ ] 7个工具类成功迁移
- [ ] SimpleMapper删除
- [ ] ValidationConstants统一为FluentValidation格式
- [ ] DTO上的DataAnnotation验证特性移除
- [ ] FluentValidator覆盖所有验证规则
- [ ] 编译通过，无警告
- [ ] 所有测试通过

## Risks & Mitigations

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| 迁移后引用断裂 | 中 | 中 | 逐个迁移，每次编译验证 |
| 循环依赖 | 低 | 高 | 预先分析依赖关系 |
| 命名空间冲突 | 低 | 低 | 统一使用新命名空间 |
| 验证规则遗漏 | 中 | 高 | 对比DataAnnotation与FluentValidator，确保覆盖 |
| 客户端验证失效 | 低 | 中 | FluentValidation支持客户端验证，测试验证 |

## Architecture

**两层工具集架构**：

```
LYBT.Shared.Utilities          ← 可跨项目复用的服务性代码
├── Configuration/                (ConfigurationHelper, EnvironmentHelper)
├── Extensions/                   (ApplicationInitializationExtensions)
├── Security/                     (PasswordHelper, ClaimsHelper, RoleHelper)
└── Text/                         (PinYinHelper)

LYBT.Desktop.Utilities         ← Desktop专用工具
├── Configuration/                (ConfigurationExtensions)
├── Constants/                    (SystemConstants)
├── Excel/                        (ExcelHelper)
├── Http/                         (RetryPolicyExtensions)
├── Localization/                 (ClientErrorMessageMapper)
├── Logging/                      (DesktopSerilogConfiguration)
└── Security/                     (SensitiveInfoFilter)
```

**分类标准**：

| 放置位置 | 条件 |
|----------|------|
| Shared.Utilities | 无平台依赖，可跨项目复用 |
| Desktop.Utilities | 依赖WPF/Windows，仅Desktop使用 |
| 领域模块 | 包含业务逻辑 |
| 各层DI配置 | ServiceCollectionExtensions，保持就近原则 |

## Created

- Date: 2025-12-20
- Author: Claude Code
