# LYBT.Module.Doctors 功能说明文档

## 模块概述

医生管理模块负责医生档案的完整生命周期管理，包括医生信息建档、档案维护、执业状态管理、工作状态跟踪等功能。本模块采用软删除策略，与用户系统深度集成，支持基于角色的权限控制和智能搜索功能。

## 数据模型

### DoctorModel (医生实体)

**文件位置**: `Models/DoctorModel.cs`

| 字段名             | 类型                                      | 说明         | 验证规则         |
| --------------- | --------------------------------------- | ---------- | ------------ |
| Id              | Guid                                    | 医生唯一标识（主键） | 必填           |
| UserId          | Guid                                    | 关联用户ID     | 必填，外键关联      |
| User            | UserModel                               | 关联用户实体     | 导航属性         |
| Gender          | Gender                                  | 性别         | 枚举值，必填       |
| Birthday        | DateTime                                | 出生日期       | 必填           |
| Title           | DoctorTitle                             | 职称         | 枚举值，必填       |
| LicenseNumber   | string?                                 | 执业证号       | 最长32字符，可选    |
| Specialty       | string                                  | 专科         | 最长64字符，必填    |
| Status          | DoctorStatus                            | 在职状态       | 枚举值，默认Active |
| WorkStatus      | DoctorWorkStatus                        | 工作状态       | 枚举值，默认OnDuty |
| PinyinCode      | string                                  | 拼音码        | 最长32字符，自动生成  |
| Remark          | string?                                 | 备注信息       | 最长256字符，可选   |
| ContactNumber   | string?                                 | 联系电话       | 最长32字符，可选    |
| CreatedTime     | DateTime                                | 创建时间       | 系统自动设置       |
| Age             | int                                     | 年龄（计算属性）   | 根据出生日期自动计算   |
| SpecialPatients | ICollection&lt;SpecialPatientDoctor&gt; | 特殊患者关系     | 导航属性，一对多关系   |

### 枚举类型

#### DoctorStatus (医生状态)

- `Active (1)`: 在职状态
- `Inactive (0)`: 停用状态  
- `Deleted (-1)`: 已删除（软删除）

#### DoctorTitle (医生职称)

- `ChiefPhysician (1)`: 主任医师
- `AssociateChiefPhysician (2)`: 副主任医师
- `AttendingPhysician (3)`: 主治医师
- `ResidentPhysician (4)`: 住院医师
- `Physician (5)`: 医师
- `InternPhysician (6)`: 实习医师

#### DoctorWorkStatus (工作状态)

- `OnDuty (1)`: 在岗
- `OffDuty (0)`: 离岗
- `OnLeave (2)`: 休假
- `Away (3)`: 外出

## DTO 数据传输对象

### DoctorDto (医生列表展示)

**使用场景**: 医生列表展示、简单医生信息返回
**特点**: 包含医生基本信息和关联用户信息

```csharp
- Id: 医生ID
- UserId: 关联用户ID
- Gender: 性别
- Birthday: 出生日期
- Title: 职称
- LicenseNumber: 执业证号
- Specialty: 专科
- Status: 在职状态
- WorkStatus: 工作状态
- PinyinCode: 拼音码
- Remark: 备注
- ContactNumber: 联系电话
- CreatedTime: 创建时间
- Age: 年龄（计算属性）
// 关联用户信息
- UserName: 用户名
- RealName: 真实姓名
- PhoneNumber: 手机号
- Email: 邮箱
```

### DoctorDetailDto (医生详情)

**使用场景**: 医生详情展示、医生信息创建和编辑
**特点**: 包含完整的医生信息和验证规则

```csharp
- Id: 医生ID
- UserId: 关联用户ID（必填）
- Gender: 性别（必填）
- Birthday: 出生日期（必填）
- Title: 职称（必填）
- LicenseNumber: 执业证号（可选，最长32字符）
- Specialty: 专科（必填，最长64字符）
- Status: 在职状态
- WorkStatus: 工作状态
- PinyinCode: 拼音码（最长32字符）
- Remark: 备注（最长256字符）
- ContactNumber: 联系电话（手机格式验证，最长32字符）
- CreatedTime: 创建时间
- Age: 年龄（计算属性）
// 关联用户信息（只读）
- UserName: 用户名
- RealName: 真实姓名
- PhoneNumber: 手机号（手机格式验证）
- Email: 邮箱（邮箱格式验证）
```

### DoctorQueryDto (医生查询条件)

**使用场景**: 医生列表的分页查询和条件筛选

```csharp
- Keyword: 关键词（模糊匹配姓名、拼音码、手机号）
- IsActive: 在职状态筛选（true=在职，false=停用，null=全部）
- Page: 页码（默认1）
- PageSize: 每页大小（默认20，最大100）
```

### BatchIdsDto (批量操作)

**使用场景**: 批量启用/禁用医生

```csharp
- Ids: 医生ID列表
```

## 服务层 (IDoctorService & DoctorService)

### 查询类方法

#### GetByIdAsync

```csharp
Task<ApiResponse<DoctorDetailDto>> GetByIdAsync(Guid id, UserRole currentUserRole)
```

**功能**: 根据ID获取医生详情
**权限控制**: 管理员可查看所有医生（包括停用），普通用户只能查看在职医生
**使用场景**: 医生详情页面、编辑前数据加载

#### GetByUserIdAsync

```csharp
Task<ApiResponse<DoctorDetailDto>> GetByUserIdAsync(Guid userId, UserRole currentUserRole)
```

**功能**: 根据用户ID获取医生详情
**权限控制**: 同GetByIdAsync
**使用场景**: 通过用户ID查找对应的医生档案

#### SearchAsync

```csharp
Task<ApiResponse<List<DoctorDto>>> SearchAsync(string keyword, UserRole currentUserRole)
```

**功能**: 根据关键词搜索医生
**搜索范围**: 姓名、拼音码、手机号模糊匹配
**权限控制**: 根据用户角色决定是否包含停用医生
**使用场景**: 快速医生查找、下拉选择

#### GetPagedAsync

```csharp
Task<ApiResponse<PagedResultDto<DoctorDto>>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole)
```

**功能**: 分页条件查询医生列表
**查询特性**: 

- 关键词模糊匹配（姓名、拼音码、手机号）
- 在职状态筛选
- 权限控制（停用医生仅管理员可见）
- 分页支持（每页最大100条）

**使用场景**: 医生管理页面的列表展示

#### GetActiveDoctorsAsync

```csharp
Task<ApiResponse<List<DoctorDto>>> GetActiveDoctorsAsync()
```

**功能**: 获取所有在职医生列表
**特点**: 不分页，仅返回在职状态的医生
**使用场景**: 医生选择下拉框、关联选择等场景

### 管理类方法

#### AddAsync

```csharp
Task<ApiResponse<bool>> AddAsync(DoctorDetailDto dto)
```

**功能**: 创建新医生档案
**业务逻辑**: 

- 关联用户ID必填验证
- 专科必填验证
- 检查关联用户是否存在
- 检查用户是否已关联医生档案（防重复）
- 自动生成拼音码
- 设置创建时间

**使用场景**: 为已有用户创建医生档案

#### UpdateAsync

```csharp
Task<ApiResponse<bool>> UpdateAsync(DoctorDetailDto dto)
```

**功能**: 更新医生信息
**业务逻辑**: 

- 医生ID必填验证
- 专科必填验证
- 获取现有医生信息
- 更新可修改字段（不更新UserId等关键字段）
- 重新生成拼音码

**使用场景**: 医生信息维护和更新

#### DisableAsync / EnableAsync

```csharp
Task<ApiResponse<bool>> DisableAsync(Guid id)
Task<ApiResponse<bool>> EnableAsync(Guid id)
```

**功能**: 单个医生停用/启用
**业务逻辑**: 软删除策略，仅修改Status状态
**使用场景**: 医生在职状态管理

#### BatchDisableAsync / BatchEnableAsync

```csharp
Task<ApiResponse<int>> BatchDisableAsync(List<Guid> ids)
Task<ApiResponse<int>> BatchEnableAsync(List<Guid> ids)
```

**功能**: 批量医生停用/启用
**业务逻辑**: 

- 批量ID列表验证
- 返回实际影响的记录数
- 操作结果统计

**使用场景**: 批量医生状态管理

#### IsUserLinkedToDoctorAsync

```csharp
Task<ApiResponse<bool>> IsUserLinkedToDoctorAsync(Guid userId)
```

**功能**: 检查用户是否已关联医生档案
**使用场景**: 创建医生档案前的重复检查、用户角色判断

### 权限控制方法

#### CanViewDisabledDoctors

- 私有方法，判断当前用户是否可以查看停用医生
- 仅管理员可查看停用医生

## 仓储层 (IDoctorRepository & DoctorRepository)

### 基础CRUD方法

#### AddAsync / UpdateAsync

```csharp
Task<bool> AddAsync(DoctorModel model)
Task<bool> UpdateAsync(DoctorModel model)
```

**功能**: 基础的增加和更新操作
**使用场景**: 服务层调用的底层数据操作

#### DisableAsync / EnableAsync

```csharp
Task<bool> DisableAsync(Guid id)
Task<bool> EnableAsync(Guid id)
```

**功能**: 软删除策略的停用/启用操作
**实现**: 直接修改Status字段

### 查询方法

#### GetByIdAsync

```csharp
Task<DoctorModel?> GetByIdAsync(Guid id, bool includeDisabled = false)
```

**功能**: 根据ID查找医生
**权限控制**: 可选择是否包含停用医生

#### GetByUserIdAsync

```csharp
Task<DoctorModel?> GetByUserIdAsync(Guid userId, bool includeDisabled = false)
```

**功能**: 根据用户ID查找医生
**权限控制**: 可选择是否包含停用医生
**使用场景**: 用户与医生档案关联查询

#### GetPagedAsync

```csharp
Task<(List<DoctorModel> list, int total)> GetPagedAsync(DoctorQueryDto query, bool includeDisabled = false)
```

**功能**: 分页条件查询
**查询条件**: 

- 关键词模糊匹配（姓名、拼音码、手机号）
- 在职状态筛选
- 权限控制参数

**排序**: 按创建时间倒序

#### SearchAsync

```csharp
Task<List<DoctorModel>> SearchAsync(string keyword, bool includeDisabled = false)
```

**功能**: 模糊搜索医生
**搜索范围**: 姓名、拼音码、手机号
**使用场景**: 通用的医生搜索功能

#### SearchByPinyinAsync

```csharp
Task<List<DoctorModel>> SearchByPinyinAsync(string pinyin, bool includeDisabled = false)
```

**功能**: 根据拼音码搜索医生
**使用场景**: 拼音输入法快速查找

#### GetActiveDoctorsAsync

```csharp
Task<List<DoctorModel>> GetActiveDoctorsAsync()
```

**功能**: 获取所有在职医生
**排序**: 按真实姓名排序
**使用场景**: 需要医生选择的下拉框场景

### 批量操作方法

#### BatchDisableAsync / BatchEnableAsync

```csharp
Task<int> BatchDisableAsync(List<Guid> ids)
Task<int> BatchEnableAsync(List<Guid> ids)
```

**功能**: 批量更新医生状态
**特点**: 使用EF Core的批量更新，性能优化
**返回**: 实际影响的记录数

#### ExistsAsync

```csharp
Task<bool> ExistsAsync(Guid id)
```

**功能**: 检查医生是否存在（包括停用的医生）
**使用场景**: 数据验证和引用完整性检查

## 权限控制策略

### 角色级别权限

- **管理员(Admin)**: 可查看和操作所有医生（包括停用医生）
- **普通用户**: 只能查看在职的医生，不能进行管理操作

### 数据隐藏策略

- 停用医生对普通用户不可见
- 所有管理操作都需要记录操作者信息
- 软删除策略，数据物理保留

### 操作权限

- 只有管理员可以创建、编辑、停用/启用医生
- 医生档案与用户系统紧密关联，需要先有用户才能创建医生
- 用户与医生档案一对一关系，防止重复关联

## 业务规则

### 数据完整性

- **用户关联**: 医生必须关联一个有效用户
- **唯一性**: 每个用户只能关联一个医生档案
- **必填字段**: 性别、出生日期、职称、专科为必填项
- **数据格式**: 手机号、邮箱需符合相应格式

### 状态管理

- **在职状态**: 控制医生是否可用
- **工作状态**: 跟踪医生当前工作状况
- **软删除**: 采用状态标记而非物理删除

### 智能功能

- **拼音码生成**: 自动生成姓名拼音码，支持快速检索
- **年龄计算**: 根据出生日期自动计算年龄
- **模糊搜索**: 支持姓名、拼音码、手机号的模糊搜索

## 集成依赖

### 模块依赖

- **LYBT.Module.Users**: 用户模块（用户信息和关联）
- **LYBT.Module.Patients**: 患者模块（特殊患者关系）
- **LYBT.Infrastructure**: 基础设施（日志、缓存、配置）

### 技术依赖

- **AutoMapper**: 对象映射
- **Entity Framework Core**: 数据访问
- **CommonHelper**: 拼音码生成工具

## 使用示例

### 创建医生档案

```csharp
var doctorDto = new DoctorDetailDto {
    UserId = userId, // 已存在的用户ID
    Gender = Gender.Male,
    Birthday = new DateTime(1980, 5, 15),
    Title = DoctorTitle.AttendingPhysician,
    LicenseNumber = "DOC001234",
    Specialty = "心血管内科",
    ContactNumber = "13800138000",
    Remark = "主治心血管疾病"
};

var result = await doctorService.AddAsync(doctorDto);
```

### 查询医生列表

```csharp
var query = new DoctorQueryDto {
    Keyword = "张",
    IsActive = true,
    Page = 1,
    PageSize = 20
};

var result = await doctorService.GetPagedAsync(query, currentUserRole);
```

### 搜索医生

```csharp
var doctors = await doctorService.SearchAsync("心血管", currentUserRole);
```

### 批量状态管理

```csharp
var ids = new List<Guid> { doctorId1, doctorId2, doctorId3 };
var count = await doctorService.BatchDisableAsync(ids);
```

### 检查用户关联状态

```csharp
var isLinked = await doctorService.IsUserLinkedToDoctorAsync(userId);
if (!isLinked.Success || !isLinked.Data) {
    // 用户未关联医生档案，可以创建
    await doctorService.AddAsync(doctorDto);
}
```

## 扩展建议

### 功能扩展

- **排班管理**: 集成医生排班功能
- **科室管理**: 添加科室分类和管理
- **执业资质**: 完善执业证书和资质管理
- **绩效统计**: 医生工作量和绩效统计

### 技术优化

- **缓存策略**: 对常用医生列表进行缓存
- **搜索优化**: 集成全文搜索引擎
- **图片管理**: 支持医生头像和证件照片
- **日志审计**: 增加详细的操作日志记录