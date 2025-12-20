# Design: consolidate-shared-utilities

## Architecture Overview

```
src/Shared/
├── LYBT.Shared.Models/
│   └── Constants/
│       └── ValidationConstants.cs    ← 保留，作为唯一来源
├── LYBT.Shared.Utilities/            ← 已有，可跨项目复用
│   ├── Configuration/
│   ├── Extensions/
│   ├── Security/
│   └── Text/
└── LYBT.Shared.Validators/
    └── Common/
        └── ValidationConstants.cs    ← 删除，迁移差异值

src/Client/Desktop/Core/
├── LYBT.Desktop.Utilities/           ← 新建，Desktop专用工具
│   ├── Configuration/
│   ├── Constants/
│   ├── Excel/
│   ├── Http/
│   ├── Localization/
│   ├── Logging/
│   └── Security/
├── LYBT.Desktop.Infrastructure/      ← 迁出工具类后保留
│   ├── Behaviors/                    (DataGridSelectionBehavior保留)
│   ├── Constants/                    (RegionNames保留)
│   └── Converters/                   (WPF Converters保留)
└── LYBT.Desktop.Foundation/          ← 迁出RetryPolicyExtensions
```

## Decision Records

### DR-1: ValidationConstants合并策略

**背景**:
- `LYBT.Shared.Models/Constants/ValidationConstants.cs` - 全面定义
- `LYBT.Shared.Validators/Common/ValidationConstants.cs` - FluentValidation特定

**差异分析**:

| 常量 | Models版 | Validators版 | 决定 |
|------|----------|--------------|------|
| NameMaxLength | 50 | 100 | 保留100 (更宽松) |
| RemarkMaxLength | 500 | 1000 | 保留1000 (更宽松) |
| LongRemarkMaxLength | 1000 | 2000 | 保留2000 |
| PasswordMaxLength | 128 | 100 | 保留128 (安全) |
| AgeMaxValue | 150 | 200 | 保留150 (合理) |
| PriceMaxValue | 999999.99 | 100000 | 保留999999.99 |
| IdCardMaxLength | 无 | 18 | 添加 |
| UserNameMaxLength | 无 | 50 | 添加 |
| HerbCodeMaxLength | 无 | 50 | 添加 |
| PrescriptionNumberMaxLength | 无 | 50 | 添加 |
| EmailRegex | 无 | 有 | 添加 |
| 验证消息格式 | DataAnnotation格式 `{0}` | FluentValidation格式 `{PropertyName}` | 统一FluentValidation |

**决定**:
1. 保留Models版作为主版本
2. 添加Validators版独有的常量
3. 使用更宽松的长度限制
4. **统一使用FluentValidation消息格式**（删除DataAnnotation格式）

### DR-2: SimpleMapper删除

**背景**: `SimpleMapper`经Serena分析确认0引用。

**决定**: 直接删除，无需迁移。

### DR-3: 工具类放置规范

| 类型 | 放置位置 | 示例 |
|------|----------|------|
| 纯工具(无依赖) | Shared.Utilities | PinYinHelper, ConfigurationHelper |
| 平台特定 | Desktop.Utilities | ExcelHelper, SensitiveInfoFilter |
| 领域逻辑 | 领域模块 | MedicalCaseValidationHelper |
| DI扩展 | 各层 | ServiceCollectionExtensions |
| 验证常量 | Shared.Models | ValidationConstants |
| FluentValidation | Shared.Validators | XxxDtoValidator |

### DR-4: 创建LYBT.Desktop.Utilities项目

**背景**:
- 项目增大，代码管理需要更清晰的组织
- Desktop专用工具分散在Infrastructure/Foundation等项目
- 用户要求尽可能将工具类迁移到统一工具集

**分析**:

| 备选方案 | 优点 | 缺点 |
|----------|------|------|
| A. 保持现状 | 无需改动 | 工具类分散，难以管理 |
| B. 在Infrastructure中分目录 | 改动小 | 仍不够清晰，职责混合 |
| C. 新建Desktop.Utilities | 职责清晰，便于复用 | 需创建新项目 |

**决定**: 选择方案C，创建`LYBT.Desktop.Utilities`项目

**理由**:
1. 与`LYBT.Shared.Utilities`形成对称的两层架构
2. 明确区分"可跨项目复用"和"Desktop专用"
3. 便于未来维护和发现工具类
4. 减轻Infrastructure项目职责

**迁移范围**:

| 类名 | 原项目 | 迁移原因 |
|------|--------|---------|
| ExcelHelper | Infrastructure | 纯工具，无UI耦合 |
| ConfigurationExtensions | Infrastructure | 配置工具 |
| SystemConstants | Infrastructure | 常量定义 |
| ClientErrorMessageMapper | Infrastructure | 本地化工具 |
| DesktopSerilogConfiguration | Infrastructure | 日志配置 |
| SensitiveInfoFilter | Infrastructure | 安全过滤 |
| RetryPolicyExtensions | Foundation | HTTP工具 |

**不迁移**:

| 类名 | 理由 |
|------|------|
| DataGridSelectionBehavior | WPF附加属性，与XAML紧密耦合 |
| RegionNames | Prism导航常量，Shell启动依赖 |
| WPF Converters | IValueConverter实现，XAML绑定 |

### DR-5: 统一验证策略为FluentValidation

**背景**:
- 项目同时使用DataAnnotation和FluentValidation两套验证系统
- DataAnnotation: 47处`[Required]`，121处`[StringLength]`
- FluentValidation: 74处`.WithMessage(...)`
- 两套消息格式不一致，维护成本高

**分析**:

| 方案 | 优点 | 缺点 |
|------|------|------|
| A. 保持两套 | 无需改动 | 维护成本高，易不一致 |
| B. 统一DataAnnotation | 声明式，简单 | 复杂规则难实现 |
| C. 统一FluentValidation | 灵活，可测试，规则集中 | 需要迁移 |

**决定**: 选择方案C，统一使用FluentValidation

**理由**:
1. FluentValidation已覆盖核心验证逻辑
2. 支持复杂业务规则（条件验证、跨字段验证）
3. 验证规则可单元测试
4. 消息格式统一，维护简单

**实施步骤**:
1. 确保每个DTO都有对应的FluentValidator
2. 对比DataAnnotation规则，补充FluentValidator中缺失的规则
3. 移除DTO上的DataAnnotation验证特性
4. 删除DataAnnotation格式的消息常量
5. 验证所有API端点的验证行为

## API Changes

### Before (Validators)

```csharp
using LYBT.Shared.Validators.Common;
// ValidationConstants.NameMaxLength = 100
```

### After (Models)

```csharp
using LYBT.Shared.Models.Constants;
// ValidationConstants.NameMaxLength = 100 (合并后)
```

### Before (Infrastructure)

```csharp
using LYBT.Desktop.Infrastructure.Helpers;
ExcelHelper.ExportToExcel(data, path);
```

### After (Utilities)

```csharp
using LYBT.Desktop.Utilities.Excel;
ExcelHelper.ExportToExcel(data, path);
```

## Migration Path

### Phase 1: 创建项目
1. 创建`LYBT.Desktop.Utilities.csproj`
2. 创建目录结构
3. 添加到解决方案

### Phase 2: 迁移工具类
1. 逐个移动文件到新项目
2. 更新命名空间
3. 添加项目引用
4. 编译验证

### Phase 3: 清理
1. 删除SimpleMapper
2. 合并ValidationConstants
3. 删除原文件

### Phase 4: 验证
1. 全量编译
2. 运行测试
3. 确认无警告

### Phase 5: 统一验证格式
1. 审查所有DTO的DataAnnotation特性
2. 确保对应FluentValidator覆盖所有规则
3. 移除DataAnnotation验证特性
4. 删除DataAnnotation格式的消息常量
5. 测试验证行为

## Risks

- **值冲突**: 两处定义值不同 → 选择更宽松/安全的值
- **循环依赖**: Utilities依赖分析 → 预先确认依赖链
- **引用断裂**: 逐步迁移 → 每次编译验证
- **验证规则遗漏**: DataAnnotation移除后规则丢失 → 对比审查，补充FluentValidator
- **API行为变化**: 验证响应格式可能变化 → 测试验证
