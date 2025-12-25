# Design: sync-entity-dto-fields

## 概述
本设计定义从Entity到UI的全数据流字段同步规范，确保字段属性（类型、必填、标签）在各层保持一致。

## 数据流架构

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Entity    │───▶│    DTO      │───▶│  ViewModel  │───▶│    XAML     │
│ (Server)    │    │  (Shared)   │    │ (Desktop)   │    │   (View)    │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
      │                  │                  │                  │
      ▼                  ▼                  ▼                  ▼
 [Required]         decimal?           decimal?          TargetNullValue
 [StringLength]     验证规则           DependencyProperty   StringFormat
 [DisplayName]      ErrorMessage       PropertyChanged     Label Text
```

## 1. DTO字段选择标准

### 1.1 ListDto（列表简略信息）
**目的**: 列表展示，仅包含识别和筛选所需的关键字段

**必须包含**:
| 字段类型 | 示例 | 说明 |
|----------|------|------|
| 主键 | Id | 唯一标识 |
| 名称 | Name | 主要识别字段 |
| 状态 | Status | 启用/禁用筛选 |
| 关键业务字段 | Price, Phone | 列表必要信息 |

**不包含**:
- 大文本字段（Remark, Description, Effect, Usage）
- 审计字段（CreatedAt, UpdatedBy除非业务需要）
- 关联实体详情（仅保留ID或简要信息）

### 1.2 DetailDto（详情完整信息）
**目的**: 详情展示，包含Entity全部业务字段

**必须包含**:
- Entity的所有业务字段
- 状态字段
- 审计字段（CreatedAt, CreatedBy, UpdatedAt, UpdatedBy）

**示例结构**:
```csharp
public class HerbDetailDto
{
    // 基本信息
    public int Id { get; set; }
    public string Name { get; set; }
    public string? PinYinCode { get; set; }
    public string? Origin { get; set; }
    public string? Spec { get; set; }
    public string Unit { get; set; }

    // 价格信息
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }  // 可空与Entity一致

    // 功效用法
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Remark { get; set; }

    // 状态
    public CommonStatus Status { get; set; }

    // 审计信息
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 1.3 InputDto（用户输入）
**目的**: 创建/更新请求，包含用户可编辑字段

**包含**:
- 用户可编辑的业务字段
- 可选的Id字段（更新时使用）

**不包含**:
- 系统管理字段（Status, CreatedAt等）
- 自动计算字段
- RowVersion（由服务端管理）

## 2. 字段类型同步规则

### 2.1 可空类型传递
```
Entity(decimal?)  →  DTO(decimal?)  →  DependencyProperty(decimal?)
Entity(string?)   →  DTO(string?)   →  DependencyProperty(string)  // XAML自动处理null
Entity(DateTime?) →  DTO(DateTime?) →  DependencyProperty(DateTime?)
```

### 2.2 必填字段标识
```
Entity([Required])  →  DTO([Required])  →  Validator(NotEmpty)  →  XAML(标签带*)
```

### 2.3 字符串长度
```
Entity([StringLength(100)])  →  DTO([StringLength(100)])  →  Validator(MaxLength)  →  TextBox.MaxLength
```

## 3. 标签文本同步

### 3.1 DisplayName作为单一来源
```csharp
// Entity
[DisplayName("成本价")]
public decimal? CostPrice { get; set; }

// XAML
<TextBlock Text="成本价" />  // 必须与DisplayName一致

// 必填字段
[Required]
[DisplayName("药材名称")]
public string Name { get; set; }

// XAML
<TextBlock Text="药材名称 *" />  // 带*表示必填
```

### 3.2 标签文本规范
| Entity属性 | XAML标签 | 说明 |
|------------|----------|------|
| [Required] + [DisplayName("X")] | "X *" | 必填带星号 |
| [DisplayName("X")] | "X" | 可选不带星号 |
| 无DisplayName | 使用属性名 | 不推荐 |

## 4. 验证规则同步

### 4.1 验证层级
```
DataAnnotations (Entity/DTO)
       ↓
FluentValidation (Server)
       ↓
ViewModel验证 (Desktop)
       ↓
XAML Binding验证 (View)
```

### 4.2 验证规则一致性
| Entity注解 | FluentValidator | ViewModel验证 |
|------------|-----------------|---------------|
| [Required] | NotEmpty() | if (string.IsNullOrWhiteSpace) |
| [StringLength(100)] | MaximumLength(100) | TextBox.MaxLength="100" |
| [Range(0, 999999.99)] | GreaterThanOrEqualTo(0).LessThanOrEqualTo(999999.99) | if (value < 0 \|\| value > 999999.99) |
| nullable (无Required) | 无规则或仅范围检查 | if (value.HasValue && value <= 0) |

## 5. 实施优先级

### High Priority (立即修复)
1. 类型不匹配导致的运行时错误
2. 必填属性不一致导致的验证冲突

### Medium Priority (逐步统一)
1. 标签文本不一致
2. 验证规则分散

### Low Priority (持续优化)
1. 字符串长度限制
2. 注释完善

## 6. 验证检查清单

每个模块完成后验证：
- [ ] Entity字段类型与DTO一致
- [ ] Entity可空属性与DTO一致
- [ ] Entity [Required]与Validator NotEmpty一致
- [ ] Entity [DisplayName]与XAML标签一致
- [ ] DependencyProperty类型与DTO一致
- [ ] ViewModel验证与Validator规则一致
- [ ] 编译无警告
- [ ] 创建/编辑/查看功能正常
