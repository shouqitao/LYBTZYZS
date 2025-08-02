# 模型分层设计标准

## 设计原则

**以代码为准，Code First开发模式**：所有数据库结构变更通过EF Core迁移管理，Entity模型是数据库结构的权威定义。

## 分层架构

```
数据库 ←→ Entity Model ←→ DTO ←→ 前端Model
```

## 各层职责定义

### 1. Entity Model（实体层）
**位置**：`src/Backend/Core/LYBT.Models/`
**职责**：
- 数据库表结构的直接映射
- 包含完整的业务字段（包括敏感字段）
- EF Core注解和配置
- 导航属性和关系定义

**命名规范**：
- 文件：`{模块名}Model.cs`
- 类名：`{模块名}Model`
- 示例：`UserModel`, `PatientModel`, `HerbModel`

**字段原则**：
- 包含所有业务需要的字段
- 敏感字段（如PasswordHash）仅在Entity层存在
- 使用EF Core注解进行数据库映射
- 支持审计字段（CreateTime, UpdateTime等）

### 2. DTO层（数据传输对象）
**位置**：`src/Shared/LYBT.Shared.Models/Contracts/`
**职责**：
- API接口数据传输
- 去除敏感信息
- 客户端安全的数据结构
- 前后端数据契约

**命名规范**：
- 文件：`{模块名}Dto.cs`, `{模块名}CreateDto.cs`, `{模块名}UpdateDto.cs`
- 类名：`{模块名}Dto`, `{模块名}CreateDto`, `{模块名}UpdateDto`
- 示例：`UserDto`, `UserCreateDto`, `UserUpdateDto`

**字段原则**：
- 排除敏感字段（密码、内部状态等）
- 包含前端需要的业务字段
- 可以包含计算属性和格式化字段
- 支持验证注解

### 3. 前端Model（可选）
**位置**：前端项目中
**职责**：
- 前端特定的数据结构
- UI状态管理
- 前端业务逻辑
- 可组合多个DTO

## AutoMapper配置规范

### 映射配置位置
**位置**：`src/Backend/Modules/LYBT.Module.{模块名}/Mapping/`
**文件**：`{模块名}MappingProfile.cs`

### 映射规则
```csharp
// Entity → DTO：排除敏感字段
CreateMap<UserModel, UserDto>()
    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
    .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore());

// CreateDto → Entity：设置默认值
CreateMap<UserCreateDto, UserModel>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.UtcNow));

// UpdateDto → Entity：部分更新
CreateMap<UserUpdateDto, UserModel>()
    .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
```

## 数据库迁移规范

### Code First工作流
1. **修改Entity Model**：添加/修改业务字段
2. **创建迁移**：`dotnet ef migrations add {描述} --project LYBT.Infrastructure --startup-project LYBT.WebAPI`
3. **审查迁移**：检查生成的SQL是否正确
4. **应用迁移**：`dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI`

### 迁移命名规范
- 使用描述性名称：`Add_User_Department_Position_Fields`
- 使用动词开头：`Add`, `Update`, `Remove`, `Fix`
- 包含影响的表名和字段

## 安全原则

### 敏感字段处理
**绝不暴露的字段**：
- 密码相关：`PasswordHash`, `PasswordSalt`
- 安全状态：`FailedLoginCount`, `LockoutEnd`
- 内部标识：数据库ID（可选择性暴露）
- 审计字段：某些内部审计信息

### DTO安全检查清单
- [ ] 密码字段已排除
- [ ] 敏感个人信息已脱敏
- [ ] 内部状态字段已隐藏
- [ ] 权限相关字段已控制

## 实施检查清单

### Entity Model检查
- [ ] 包含完整业务字段
- [ ] EF Core注解正确
- [ ] 导航属性定义完整
- [ ] AppDbContext配置完善

### DTO检查
- [ ] 敏感字段已排除
- [ ] 前端需要字段已包含
- [ ] 验证注解已添加
- [ ] AutoMapper配置正确

### API Controller检查
- [ ] 只使用DTO进行数据传输
- [ ] 不直接暴露Entity
- [ ] AutoMapper转换正确
- [ ] 错误处理完善

## 模块应用清单

需要应用此标准的所有模块：
- [x] Users（用户）
- [ ] Patients（患者）
- [ ] Doctors（医生）
- [ ] Herbs（药材）
- [ ] Prescriptions（处方）
- [ ] Registration（挂号）
- [ ] Queueing（排队）
- [ ] DiagnosisTreatment（诊断治疗）
- [ ] Pharmacy（药房）
- [ ] Billing（计费）
- [ ] Records（病历）
- [ ] TreatmentRoom（诊室）
- [ ] Sync（同步）
- [ ] FormulaTemplates（验方模板）

## 版本记录
- v1.0 (2025-08-01): 初始版本，确立分层架构原则
- 后续版本将根据实施经验进行优化调整