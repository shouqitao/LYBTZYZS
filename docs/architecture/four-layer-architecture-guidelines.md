# 四层架构编程准则

## 概述

本文档定义了LYBT系统的四层架构模式，确保代码层次分明、职责清晰，避免类型混乱和架构退化。

## 四层架构模式

### 第一层：BaseModel（共享基础模型）
- **位置**: `src/Shared/LYBT.Shared.Models/Core/`
- **职责**: 定义前后端共享的核心数据结构
- **特点**: 
  - 只包含业务核心字段
  - 不包含敏感信息
  - 不包含UI状态
  - 不包含后端专用字段

```csharp
// 示例：BaseUser.cs
public class BaseUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Receptionist;
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    // 共享业务字段...
}
```

### 第二层：EntityModel（数据库实体）
- **位置**: `src/Server/Core/LYBT.Entities/`
- **职责**: 数据库映射，包含后端专用字段
- **特点**:
  - 继承对应的BaseModel
  - 包含敏感信息（如PasswordHash）
  - 包含安全状态（如FailedLoginCount）
  - 包含审计字段（如CreatedBy, UpdatedBy）

```csharp
// 示例：UserModel.cs
public class UserModel : BaseUser
{
    public string PasswordHash { get; set; } = string.Empty;  // 敏感信息
    public int FailedLoginCount { get; set; } = 0;           // 安全状态
    public DateTime? LockoutEnd { get; set; }                // 安全状态
}
```

### 第三层：Dto（API传输对象）
- **位置**: `src/Shared/LYBT.Shared.Models/Contracts/`
- **职责**: 前后端数据传输，API优化
- **特点**:
  - 不包含敏感信息
  - 针对传输优化
  - 包含API验证特性
  - 可能包含计算属性（如IsActive）

```csharp
// 示例：UserDto.cs
public class UserDto : FullBaseDto, ICodeable
{
    public string Username { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public bool IsActive => Status == CommonStatus.Enabled;
    // 传输优化字段...
}
```

### 第四层：Info（前端UI模型）
- **位置**: `src/Client/Desktop/Core/Models/`
- **职责**: 前端UI展示，包含UI状态和显示逻辑
- **特点**:
  - 继承BaseModel或包含UI扩展
  - 包含UI状态（如IsSelected）
  - 包含显示逻辑（如DisplayName属性）
  - 包含前端业务逻辑

```csharp
// 示例：UserInfo.cs
public class UserInfo : BaseUser
{
    public bool IsSelected { get; set; }  // UI状态
    public string DisplayName => string.IsNullOrEmpty(RealName) ? Username : RealName;
    public string StatusText => Status.GetDescription();
    public bool IsSysAdmin => Username == "sysadmin";
}
```

## 层间映射规则

### 1. AutoMapper配置
- EntityModel ↔ Dto：在Server端映射配置
- Dto ↔ Info：在Client端映射配置  
- 敏感字段映射时必须明确忽略

```csharp
// Server端映射
CreateMap<UserModel, UserDto>()
    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

// Client端映射  
CreateMap<UserDto, UserInfo>();
```

### 2. 数据流向

```
数据流向：
EntityModel → Dto → Info (查询)
Info → Dto → EntityModel (更新)

API流向：
Client[Info] ←→ Transport[Dto] ←→ Server[EntityModel]
```

## 严禁事项

### ❌ 禁止类型别名（Type Alias）
```csharp
// 严禁这样做 - 违反单一职责原则
using UserInfo = LYBT.Shared.Models.Contracts.Users.UserDto;
```

**原因**: 
- 破坏层次分离
- 创建类型混乱
- 违反UltraThink"不别名乱引用"原则

### ❌ 禁止跨层直接引用
```csharp
// 严禁在Client中直接使用EntityModel
// 严禁在Shared中引用Client的Info类型
```

### ❌ 禁止在错误层级添加字段
```csharp
// 严禁在BaseModel中添加UI字段
// 严禁在Dto中添加敏感字段
// 严禁在Info中添加数据库映射特性
```

## 新模块创建checklist

创建新业务模块时，必须按以下步骤创建四层结构：

### 1. 创建BaseModel
- [ ] 在`src/Shared/LYBT.Shared.Models/Core/`创建`Base{Entity}.cs`
- [ ] 只包含核心业务字段
- [ ] 继承适当的基类（如AuditableModel）

### 2. 创建EntityModel  
- [ ] 在`src/Server/Core/LYBT.Entities/{Entity}/`创建`{Entity}Model.cs`
- [ ] 继承对应的BaseModel
- [ ] 添加后端专用字段（敏感信息、审计字段等）

### 3. 创建Dto
- [ ] 在`src/Shared/LYBT.Shared.Models/Contracts/{Entity}/`创建Dto类
- [ ] 创建对应的Create/Update/Query DTO
- [ ] 确保不包含敏感信息

### 4. 创建Info
- [ ] 在`src/Client/Desktop/Core/Models/{Entity}/`创建`{Entity}Info.cs`
- [ ] 添加UI状态和显示逻辑
- [ ] 配置必要的前端业务逻辑

### 5. 配置映射
- [ ] Server端：EntityModel ↔ Dto映射
- [ ] Client端：Dto ↔ Info映射
- [ ] 测试映射配置正确性

## 架构维护原则

### 1. 单一职责原则
- 每层只关注自己的职责
- 不在错误的层级添加字段或逻辑

### 2. 依赖方向原则
```
Client Info → Shared Dto → Server EntityModel → Shared BaseModel
```

### 3. 数据安全原则
- 敏感信息仅在EntityModel层
- Dto层过滤敏感字段
- Info层不直接处理敏感数据

### 4. 可维护性原则
- 明确的层次边界
- 一致的命名约定
- 完整的映射配置

## 违反架构的常见问题

### 1. 类型别名混乱
**问题**: 使用`using UserInfo = UserDto`尝试统一类型
**影响**: 破坏层次分离，创建双重引用
**解决**: 保持清晰的类型分离，通过映射转换

### 2. 跨层字段污染
**问题**: 在BaseModel中添加UI字段
**影响**: 破坏共享性，增加耦合
**解决**: 在正确的层级添加字段

### 3. 映射配置缺失
**问题**: 缺少AutoMapper配置
**影响**: 手动转换，容易出错
**解决**: 完整配置所有层间映射

## 总结

四层架构确保了：
- **清晰的职责分离**
- **安全的数据处理**  
- **可维护的代码结构**
- **一致的开发模式**

遵循这些准则，避免类型别名等捷径，确保架构的长期可维护性。