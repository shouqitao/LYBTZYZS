# DTO架构规范

> 创建日期: 2025-12-18
> 适用范围: LYBTZYZS 项目全栈
> 状态: 已采纳

## 概述

本文档定义了项目中数据传输对象(DTO)的设计规范，确保数据流清晰、类型安全、易于维护。

### 核心原则

1. **单一职责**: 每个DTO只用于一种场景（列表/详情/输入）
2. **命名清晰**: 从名称即可知道DTO用途
3. **扁平化优先**: 避免不必要的继承链
4. **禁止别名**: 不使用空继承作为类型别名

## DTO分类

### 四种核心DTO类型

| 类型 | 命名格式 | 用途 | 特点 |
|------|----------|------|------|
| **ListDto** | `{Entity}ListDto` | 分页列表、搜索结果 | 精简字段，仅包含列表必需信息 |
| **DetailDto** | `{Entity}DetailDto` | 单条记录详情 | 完整字段，包含所有可展示信息 |
| **InputDto** | `{Entity}InputDto` | 创建/更新输入 | 包含验证规则，Id可选 |
| **OperationDto** | `{Operation}Dto` | 特定业务操作 | 操作特定字段 |

### 示例

```csharp
// 列表DTO - 精简字段
public class UserListDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string RealName { get; set; }
    public UserRole Role { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 详情DTO - 完整字段
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string RealName { get; set; }
    public UserRole Role { get; set; }
    public CommonStatus Status { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? PinYinCode { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// 输入DTO - 创建/更新统一
public class UserInputDto
{
    public Guid? Id { get; set; }  // 创建时null，更新时必填

    [StringLength(32, MinimumLength = 3)]
    public string? UserName { get; set; }

    [StringLength(50)]
    public string? RealName { get; set; }

    public UserRole? Role { get; set; }
    // ... 其他可修改字段
}

// 操作DTO - 特定业务
public class ChangePasswordDto
{
    public Guid UserId { get; set; }
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}
```

## 命名规范

### 规范命名

| 场景 | 命名格式 | 示例 |
|------|----------|------|
| 列表视图 | `{Entity}ListDto` | `UserListDto`, `PatientListDto` |
| 详情视图 | `{Entity}DetailDto` | `UserDetailDto`, `PatientDetailDto` |
| 创建/更新 | `{Entity}InputDto` | `UserInputDto`, `PatientInputDto` |
| 仅创建 | `{Entity}CreateDto` | `PrescriptionCreateDto` |
| 仅更新 | `{Entity}UpdateDto` | `PrescriptionUpdateDto` |
| 业务操作 | `{Operation}Dto` | `ChangePasswordDto`, `ResetPasswordRequestDto` |
| 操作响应 | `{Operation}ResponseDto` | `ResetPasswordResponseDto` |

### 禁止的命名

```csharp
// 禁止：模糊的 {Entity}Dto
public class UserDto { }  // 不清楚是列表还是详情

// 禁止：空继承别名
public class UserDto : UserDetailDto { }  // 反模式，导致类型转换问题

// 禁止：职责不清的命名
public class UserInfo { }  // 不是DTO命名规范
public class UserModel { }  // Model是实体层概念
```

## 继承策略

### 扁平化优先原则

1. **ListDto** - **独立定义，不继承**
   - 字段与DetailDto差异大
   - 避免继承带来的不必要字段

2. **DetailDto** - **可选继承基类**
   - 可继承 `TimestampDto` 获得审计字段
   - 可继承 `StatusDto` 获得状态字段
   - 也可完全独立定义

3. **InputDto** - **不继承响应DTO**
   - 输入和输出职责不同
   - 输入包含验证规则，Id可选
   - 输出包含计算属性，Id必有

4. **OperationDto** - **独立定义**
   - 每个操作有特定字段需求

### 基类选择

```csharp
// 推荐：需要审计字段时继承TimestampDto
public class PatientDetailDto : TimestampDto
{
    public string Name { get; set; }
    // ... 继承了 Id, CreatedAt, UpdatedAt, CreatedBy
}

// 推荐：需要状态字段时继承StatusDto
public class HerbDetailDto : StatusDto
{
    public string Name { get; set; }
    // ... 继承了 Id, CreatedAt, UpdatedAt, CreatedBy, Status, IsEnabled
}

// 也可以：完全独立定义（User模块）
public class UserDetailDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... 所有字段显式定义
}
```

## API返回类型规范

| API操作 | 返回类型 | 说明 |
|---------|----------|------|
| 分页列表 | `PagedResult<{Entity}ListDto>` | 精简字段，适合列表展示 |
| 搜索 | `List<{Entity}ListDto>` | 同列表 |
| 单条查询 | `{Entity}DetailDto` | 完整字段，适合详情/编辑 |
| 创建 | `{Entity}DetailDto` | 返回创建后的完整数据 |
| 更新 | `{Entity}DetailDto` | 返回更新后的完整数据 |
| 删除 | `Result` | 仅状态，无数据 |
| 特定操作 | `{Operation}ResponseDto` | 操作特定响应 |

### 示例

```csharp
// Service层接口
public interface IUserService
{
    // 列表查询 - 返回ListDto
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(int page, int pageSize);

    // 单条查询 - 返回DetailDto
    Task<Result<UserDetailDto>> GetByIdAsync(Guid id);

    // 创建 - 接收InputDto，返回DetailDto
    Task<Result<UserDetailDto>> CreateAsync(UserInputDto dto);

    // 更新 - 接收InputDto，返回DetailDto
    Task<Result<UserDetailDto>> UpdateAsync(Guid id, UserInputDto dto);

    // 删除 - 仅返回状态
    Task<Result> DeleteAsync(Guid id);

    // 特定操作 - 使用专用DTO
    Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request);
}
```

## Desktop层使用

### 列表视图

```csharp
// 方式1：直接使用ListDto（推荐用于简单场景）
public ObservableCollection<UserListDto> Users { get; }

// 方式2：转换为Item模型（推荐用于复杂UI交互）
public ObservableCollection<UserItem> Users { get; }

// UserItem继承BindableBase，支持属性变更通知
public class UserItem : BindableBase
{
    // 从ListDto或DetailDto创建
    public static UserItem FromDto(UserListDto dto) { ... }
    public static UserItem FromDto(UserDetailDto dto) { ... }
}
```

### 编辑视图

```csharp
// 编辑时使用DetailDto获取完整数据
var detail = await _userApi.GetUserByIdAsync(id);

// 提交时转换为InputDto
var input = new UserInputDto
{
    Id = detail.Id,
    UserName = detail.UserName,
    RealName = detail.RealName,
    // ...
};
await _userApi.UpdateUserAsync(id, input);
```

## 数据流图

```
┌─────────────────────────────────────────────────────────────┐
│                         Client                               │
├─────────────────────────────────────────────────────────────┤
│  ListView          DetailView           EditView            │
│     │                  │                    │               │
│     ▼                  ▼                    ▼               │
│  ListDto           DetailDto            InputDto            │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                         API                                  │
├─────────────────────────────────────────────────────────────┤
│  GET /list         GET /{id}         POST / PUT             │
│     │                  │                    │               │
│     ▼                  ▼                    ▼               │
│  ListDto           DetailDto            InputDto            │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                       Service                                │
├─────────────────────────────────────────────────────────────┤
│            AutoMapper: Entity <-> DTO                        │
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                       Database                               │
│                       Entity                                 │
└─────────────────────────────────────────────────────────────┘
```

## 迁移指南

### 统一重构策略

**原则**: 删除所有模糊命名的 `{Entity}Dto`，统一使用 `{Entity}DetailDto`

**问题分析**:
1. **重复定义**: `{Entity}Dto` 和 `{Entity}DetailDto` 属性几乎相同，维护成本高
2. **命名模糊**: `{Entity}Dto` 无法区分是列表还是详情用途
3. **继承混乱**: 部分使用继承(StatusDto)，部分独立定义，风格不一致

**解决方案**:
- 删除所有 `{Entity}Dto` 类
- 保留 `{Entity}DetailDto` 作为标准详情DTO
- `{Entity}DetailDto` 统一采用**独立定义**风格（显式声明所有字段）

### 模块迁移清单

| 模块 | 删除 | 保留 | 操作 | 状态 |
|------|------|------|------|------|
| User | `UserDto` | `UserDetailDto` | 已完成 | ✅ |
| Patient | `PatientDto` | `PatientDetailDto` | 删除Dto，替换引用 | 待执行 |
| Herb | `HerbDto` | `HerbDetailDto` | 删除Dto，替换引用 | 待执行 |
| Formula | `FormulaDto` | `FormulaDetailDto` | 删除Dto，替换引用 | 待执行 |
| Prescription | `PrescriptionDto` | `PrescriptionDetailDto` | 删除Dto，替换引用 | 待执行 |
| MedicalCase | `MedicalCaseDto` | `MedicalCaseDetailDto` | 重命名，替换引用 | 待执行 |
| Consultation | `ConsultationDto` | `ConsultationDetailDto` | 重命名，替换引用 | 待执行 |

### 迁移步骤（每模块）

1. **分析依赖**: 确认 `{Entity}Dto` 的所有引用位置
2. **补全DetailDto**: 确保 `{Entity}DetailDto` 包含所有必要属性
3. **删除/重命名**: 删除 `{Entity}Dto` 或重命名为 `{Entity}DetailDto`
4. **替换引用**: 全局替换所有 `{Entity}Dto` → `{Entity}DetailDto`
5. **更新映射**: 调整AutoMapper配置
6. **编译验证**: 确保0错误

### User模块迁移（已完成示例）

**变更记录**:
- 删除 `public class UserDto : UserDetailDto { }` 空继承
- 全局替换 `UserDto` → `UserDetailDto`（约40文件）
- 测试通过：348+测试全部绿色

## 检查清单

在代码审查时，确认DTO设计是否符合规范：

- [ ] DTO命名是否使用规范后缀（ListDto/DetailDto/InputDto）
- [ ] 是否避免了模糊的 `{Entity}Dto` 命名
- [ ] 是否避免了空继承别名
- [ ] ListDto是否只包含列表必需字段
- [ ] DetailDto是否包含完整可展示字段
- [ ] InputDto是否包含验证规则
- [ ] API返回类型是否正确（列表返回ListDto，详情返回DetailDto）
- [ ] 继承关系是否合理（扁平化优先）

## 相关文档

- [命名规范](./naming-conventions.md)
- [API设计规范](../reference/api/)
- [AutoMapper配置指南](../how-to-guides/development/)

## 变更历史

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2025-12-18 | v1.0 | 初始版本，定义DTO分类、命名规范、继承策略 |
