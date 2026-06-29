# Shared 架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: 8 个共享项目

## [O1] 当前架构问题

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | ExceptionHandling 引入 ASP.NET Core + EF Core | LYBT.Shared.ExceptionHandling.csproj | Desktop 依赖过重 |
| H2 | 部分 DTO 重复定义 | Shared.Models + Desktop.Models | 维护成本高 |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | Shared.Utilities 包含 BCrypt 包装 | BcryptPasswordService.cs | 跨切面关注点 |
| M2 | Shared.Validators 仅用于 Server 端 | 各 Validator | Desktop 无验证 |

## [O2] 共享项目清单

| 项目 | 职责 | 依赖 |
|------|------|------|
| LYBT.Shared.Models | DTO、枚举、契约 | 无 |
| LYBT.Shared.Configuration | 配置 Options | 无 |
| LYBT.Shared.ExceptionHandling | 异常处理 | ASP.NET Core, EF Core |
| LYBT.Shared.Logging | 日志 | Serilog |
| LYBT.Shared.Primitives | 基础类型、错误码 | 无 |
| LYBT.Shared.Utilities | 工具类 | BCrypt |
| LYBT.Shared.Validators | FluentValidation 验证 | FluentValidation |
| LYBT.Shared.Components | 共享组件 | 无 |

## [O3] 优化方案

### 方案 1: ExceptionHandling 拆分（H1）

```markdown
当前问题: LYBT.Shared.ExceptionHandling 引入 ASP.NET Core + EF Core
影响: Desktop 客户端依赖过重

解决方案: 拆分为 Desktop 和 Server 两个版本

LYBT.Shared.ExceptionHandling           # 仅基础异常类型
├── AppException.cs
├── BusinessException.cs
├── NotFoundException.cs
├── ConflictException.cs
├── ErrorCode 枚举
└── ErrorMessages

LYBT.Shared.ExceptionHandling.AspNetCore # ASP.NET Core 处理器（Server 用）
├── BusinessExceptionHandler.cs
├── SystemExceptionHandler.cs
├── ProblemDetailsConfiguration.cs
└── CorrelationIdMiddleware.cs

LYBT.Shared.ExceptionHandling.EFCore    # EF Core 处理器（Server 用）
└── DbUpdateConcurrencyExceptionHandler.cs

迁移步骤:
1. 创建 LYBT.Shared.ExceptionHandling.AspNetCore 项目
2. 移动 ASP.NET Core 相关代码
3. 创建 LYBT.Shared.ExceptionHandling.EFCore 项目
4. 移动 EF Core 相关代码
5. 更新项目引用
```

### 方案 2: 统一 DTO 定义（H2）

```markdown
当前问题: 部分 DTO 在 Shared.Models 和 Desktop.Models 中重复定义

解决方案: 统一在 Shared.Models 中定义

1. 审计所有 Desktop.Models 中的 DTO
2. 将重复的 DTO 移动到 Shared.Models
3. Desktop.Models 仅保留 WPF 特有的 Model（如 ValidatableModelBase）
4. 使用 Mapperly 在 Desktop 端映射 DTO → Model

示例:
// Shared.Models (共享)
public class PatientListDto { ... }

// Desktop.Models (WPF 特有)
public class PatientDetailModel : ValidatableModelBase
{
    // WPF 绑定特有属性
    public string DisplayText => $"{Name} {Gender}";
}

// Desktop 端映射
[Mapper]
public partial class PatientMapper
{
    public partial PatientDetailModel ToModel(PatientListDto dto);
}
```

### 方案 3: 评估 BCrypt 包装（M1）

```markdown
当前问题: Shared.Utilities 包含 BcryptPasswordService.cs

评估:
- BCrypt 仅用于密码哈希
- 密码哈希是认证关注点，不是通用工具
- 建议移动到 LYBT.Module.Auth 或 LYBT.Shared.Security

决策: 移动到 LYBT.Shared.Security（新项目）
```

### 方案 4: 验证策略统一（M2）

```markdown
当前问题: Shared.Validators 仅用于 Server 端

解决方案: Desktop 端也使用 Shared.Validators

1. Desktop 端注入 IValidator<T>
2. 在 ViewModel 中调用验证
3. 统一验证规则，避免 Server/Desktop 不一致

示例:
public class PatientEditorViewModel
{
    private readonly IValidator<PatientInputDto> _validator;
    
    public async Task<bool> ValidateAsync(PatientInputDto dto)
    {
        var result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            // 显示验证错误
        }
        return result.IsValid;
    }
}
```

## [O4] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | H1: ExceptionHandling 拆分 | 高 | 中 |
| 2 | H2: 统一 DTO 定义 | 中 | 低 |
| 3 | M1: 移动 BCrypt 包装 | 低 | 低 |
| 4 | M2: 验证策略统一 | 中 | 低 |

## [O5] 成功标准

1. **ExceptionHandling**: Desktop 不依赖 ASP.NET Core/EF Core
2. **DTO**: 无重复定义，统一在 Shared.Models
3. **BCrypt**: 移动到合适的位置
4. **验证**: Server/Desktop 使用相同的验证规则
