# Spec Delta: Shared Utilities Organization

## ADDED Requirements

### Requirement: SU-001 两层工具集架构

项目MUST采用两层工具集架构，明确区分可复用工具和平台专用工具。

**架构规范**:

| 项目 | 位置 | 用途 |
|------|------|------|
| LYBT.Shared.Utilities | src/Shared/ | 可跨项目复用的服务性代码 |
| LYBT.Desktop.Utilities | src/Client/Desktop/Core/ | Desktop专用工具 |

系统SHALL保持两层工具集的职责边界清晰。

#### Scenario: 新增可复用工具
- Given 开发者创建新的工具类
- When 工具类无平台依赖且可跨项目复用
- Then 应放置在LYBT.Shared.Utilities
- And 按功能分类到对应子目录(Configuration/Security/Text等)

#### Scenario: 新增Desktop专用工具
- Given 开发者创建依赖WPF/Windows的工具类
- When 工具类仅在Desktop层使用
- Then 应放置在LYBT.Desktop.Utilities
- And 按功能分类到对应子目录

### Requirement: SU-002 工具类放置规范

工具类MUST按照依赖关系和使用范围放置到正确的项目中。

**分类标准**:

| 类型 | 位置 | 条件 | 示例 |
|------|------|------|------|
| 纯工具类 | Shared.Utilities | 无平台依赖，可跨项目复用 | PinYinHelper, PasswordHelper |
| Desktop工具 | Desktop.Utilities | 依赖WPF/Windows | ExcelHelper, SensitiveInfoFilter |
| WPF行为/转换器 | Desktop.Infrastructure | 与XAML紧密耦合 | DataGridSelectionBehavior, IValueConverter |
| 领域工具 | 领域模块 | 包含业务逻辑 | MedicalCaseValidationHelper |
| DI扩展 | 各层 | 配置依赖注入 | ServiceCollectionExtensions |

系统SHALL遵循此标准组织工具类。

#### Scenario: 判断工具类归属
- Given 开发者需要创建新的工具类
- When 分析工具类的依赖和使用范围
- Then 按分类标准确定目标项目
- And 避免在错误的层级创建工具类

### Requirement: SU-003 常量定义单一来源

项目中的全局常量MUST只有一个定义来源，避免重复定义导致的不一致。

**规范**:
- 验证常量: `LYBT.Shared.Models/Constants/ValidationConstants.cs`
- Desktop系统常量: `LYBT.Desktop.Utilities/Constants/SystemConstants.cs`
- 禁止: 多处定义相同用途的常量

系统SHALL确保常量定义唯一。

#### Scenario: 常量引用
- Given 验证器需要使用长度限制常量
- When 编写FluentValidation规则
- Then 应引用LYBT.Shared.Models.Constants.ValidationConstants
- And 不应在本项目重新定义

### Requirement: SU-004 未使用代码清理

项目MUST定期清理未使用的工具类和代码。

**标准**:
- 0引用的公共类应删除或标记为[Obsolete]
- 工具类应有至少一处有效引用
- 使用Serena的find_referencing_symbols验证引用

系统SHALL不包含未使用的工具代码。

#### Scenario: 发现未使用代码
- Given 代码审查发现工具类0引用
- When 确认不是新添加的代码
- Then 应删除该工具类
- And 提交时说明删除原因

### Requirement: SU-005 Desktop.Utilities项目结构

LYBT.Desktop.Utilities项目MUST遵循标准目录结构。

**目录规范**:

```
LYBT.Desktop.Utilities/
├── Configuration/    # 配置相关工具
├── Constants/        # Desktop专用常量
├── Excel/            # Excel操作工具
├── Http/             # HTTP相关扩展
├── Localization/     # 本地化工具
├── Logging/          # 日志配置
└── Security/         # 安全过滤
```

系统SHALL按此结构组织Desktop工具类。

#### Scenario: 添加新工具类到Desktop.Utilities
- Given 开发者创建新的Desktop专用工具
- When 确定工具的功能分类
- Then 放置到对应的子目录
- And 更新命名空间为LYBT.Desktop.Utilities.{Category}

### Requirement: SU-006 工具类迁移后引用更新

迁移工具类后，MUST更新所有引用方的命名空间。

**规范**:
- 更新using语句
- 更新项目引用
- 验证编译通过

系统SHALL确保迁移后无断裂引用。

#### Scenario: 工具类迁移
- Given 工具类从Infrastructure迁移到Utilities
- When 完成文件移动和命名空间更新
- Then 更新所有引用方的using语句
- And 编译验证无错误

### Requirement: SU-007 统一验证消息格式

项目MUST使用FluentValidation作为唯一的验证框架，消息格式统一为`{PropertyName}`占位符风格。

**规范**:
- 验证框架: FluentValidation（禁止DataAnnotation验证特性）
- 消息格式: `{PropertyName}不能为空`，`{PropertyName}长度不能超过{MaxLength}个字符`
- 常量位置: `LYBT.Shared.Models/Constants/ValidationConstants.cs`
- 验证器位置: `LYBT.Shared.Validators/`

系统SHALL确保验证消息格式统一。

#### Scenario: 新增DTO验证
- Given 开发者创建新的DTO类
- When 需要添加验证规则
- Then 创建对应的FluentValidator
- And 使用ValidationConstants中的消息常量
- And 禁止在DTO上使用DataAnnotation验证特性

#### Scenario: 验证消息常量
- Given 需要定义验证错误消息
- When 编写消息常量
- Then 使用FluentValidation格式 `{PropertyName}` 而非 DataAnnotation格式 `{0}`
- And 将常量放置在ValidationConstants类中
