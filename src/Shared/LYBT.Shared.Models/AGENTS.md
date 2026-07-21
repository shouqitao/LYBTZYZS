# LYBT.Shared.Models - Shared DTOs, Contracts, Enums, Validators, and Utilities

**Purpose**: 跨层共享的数据契约、枚举、验证规则和工具类。Server 和 Desktop 共用。

## Structure

```
LYBT.Shared.Models/
├── Contracts/           # DTOs 和请求/响应模型
│   ├── Common/          # Result<T>, ApiResponse, PagedResult, OperationResultDto
│   ├── Auth/            # 登录/Token/会话相关
│   ├── Consultation/    # 诊断相关
│   ├── Formula/         # 验方相关
│   ├── Herbs/           # 药材相关 + IHerbItem, HerbValidatorBase
│   ├── MedicalCase/     # 医案相关
│   ├── Patients/        # 患者相关
│   ├── Prescriptions/   # 处方相关
│   ├── Registration/    # 挂号相关
│   ├── Reports/         # 报表相关
│   └── Users/           # 用户相关
├── DTOs/                # 辅助 DTO（如 UserBasicDto）
├── Enums/               # 共享枚举（Gender, HerbRole, CaseStatus 等）
├── Extensions/          # DtoConversionExtensions
├── Primitives/          # ErrorCode, ErrorCategory, ValidationConstants
├── Utilities/           # 共享工具类（Server + Desktop 共用）
│   ├── Security/        # PasswordHelper, PasswordPolicyValidator（BCrypt）
│   ├── Text/            # PinYinHelper（拼音转换）
│   └── Extensions/      # CacheExtensions（IMemoryCache 扩展）
├── Validators/          # FluentValidation 验证器（已合并自 LYBT.Shared.Validators）
└── Common/              # IIdentifiable 等基础接口
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| 共享 DTO | `Contracts/` | 按领域分子目录 |
| 共享枚举 | `Enums/` | Gender, HerbRole, CaseStatus, UserRole 等 |
| 错误码 | `Primitives/ErrorCodes/` | ErrorCode 枚举 + 扩展 |
| 密码工具 | `Utilities/Security/` | PasswordHelper（BCrypt 哈希） |
| 拼音工具 | `Utilities/Text/` | PinYinHelper（拼音首字母） |
| 缓存扩展 | `Utilities/Extensions/` | IMemoryCache.RemoveByPrefix/Clear |
| 验证器 | `Validators/` | FluentValidation 规则 |

## CONVENTIONS

- **DTO = 纯数据** — 无业务逻辑，无外部依赖
- **Utilities = Server + Desktop 共用** — 工具类放这里是因为两端都需要
- **Validators = FluentValidation** — 已合并自 LYBT.Shared.Validators（不再独立项目）

## ANTI-PATTERNS

- **Entity 类型放这里** — Entity 属于 `LYBT.Entities`
- **DTO 上加业务方法** — DTO 只承载数据
- **只有一端用的工具** — 如果只有 Server 或只有 Desktop 用，不该放 Shared

## 依赖说明

以下包看似不属"Models"，但被 Server + Desktop 共用，是合理的共享依赖：
- `BCrypt.Net-Next` — PasswordHelper（密码哈希，Server 验证 + Desktop 本地验证）
- `hyjiacan.pinyin4net` — PinYinHelper（拼音，Server 批量导入 + Desktop 搜索）
- `Microsoft.Extensions.Caching.Memory` — CacheExtensions（缓存清理，两端共用）
