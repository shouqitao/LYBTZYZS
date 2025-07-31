# LYBT.Module.Patients 功能说明文档

## 模块概述
患者管理模块负责患者档案的完整生命周期管理，包括患者建档、信息维护、档案查询、特殊患者权限管理等功能。本模块采用软删除策略，支持智能搜索和权限控制。

## 数据模型

### PatientModel (患者实体)
**文件位置**: `Models/PatientModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 |
|--------|------|------|----------|
| Id | Guid | 患者唯一标识（主键） | 必填 |
| Name | string | 患者姓名 | 最长64字符，必填 |
| Gender | Gender | 性别 | 枚举值，必填 |
| Age | int? | 年龄 | 可为空，系统可自动计算 |
| PhoneNumber | string | 手机号码 | 最长20字符，必填 |
| IDNumber | string | 证件号码 | 最长32字符，必填 |
| Address | string | 家庭住址 | 最长256字符 |
| AllergyHistory | string | 过敏史 | 最长256字符 |
| Ethnicity | string | 民族 | 最长32字符 |
| Education | string | 学历 | 最长32字符 |
| Profession | string | 职业 | 最长64字符 |
| IDType | string | 证件类型 | 默认"身份证" |
| MaritalStatus | string | 婚姻状况 | 最长16字符 |
| DateOfBirth | DateTime? | 出生日期 | 可从身份证自动解析 |
| Status | PatientStatus | 患者状态 | 枚举值，默认Normal |
| DisableReason | string | 禁用原因 | 最长128字符 |
| IsSpecial | bool | 是否为特殊患者 | 特殊患者仅特定医生可见 |
| Remark | string | 备注信息 | 最长256字符 |
| CreatedAt | DateTime | 创建时间 | 系统自动设置 |
| UpdatedAt | DateTime | 更新时间 | 系统自动维护 |
| PinyinCode | string | 姓名拼音码 | 系统自动生成，用于快速检索 |
| SpecialPatientDoctors | ICollection&lt;SpecialPatientDoctor&gt; | 特殊患者医生关系 | 仅IsSpecial为true时有效 |

### SpecialPatientDoctor (特殊患者医生关系)
**功能**: 管理特殊患者的访问权限，只有被授权的医生才能查看特殊患者信息
**使用场景**: VIP患者、特殊疾病患者的隐私保护

## DTO 数据传输对象

### PatientDto (患者列表展示)
**使用场景**: 患者列表展示、简单患者信息返回
**特点**: 包含主要展示信息，不包含敏感详情
```csharp
- Id: 患者ID
- Name: 姓名
- Gender: 性别
- Age: 年龄
- AllergyHistory: 过敏史
- Ethnicity: 民族
- Address: 地址
- PhoneNumber: 手机号
- Education: 学历
- Profession: 职业
- IDType: 证件类型
- IDNumber: 证件号
- MaritalStatus: 婚姻状况
- PinyinCode: 拼音码
- IsSpecial: 是否特殊患者
```

### PatientDetailDto (患者详情)
**使用场景**: 患者详情展示、患者信息编辑
**特点**: 包含患者完整信息，用于创建和更新操作
```csharp
- 包含PatientDto的所有字段
- Status: 患者状态
- DisableReason: 禁用原因
- DateOfBirth: 出生日期
- Remark: 备注信息
- CreatedAt: 创建时间
- UpdatedAt: 更新时间
```

### PatientPagedQueryDto (患者分页查询)
**使用场景**: 患者列表的分页查询和条件筛选
```csharp
- Keyword: 关键词（模糊匹配姓名、手机号、身份证号、拼音码）
- Page: 页码（继承自PaginationRequest）
- PageSize: 每页大小（继承自PaginationRequest）
```

### QuickPatientCreateDto (快速患者创建)
**使用场景**: 快速看诊场景下的患者快速建档
**特点**: 只包含必要信息，简化录入流程
```csharp
- Name: 姓名（必填）
- Gender: 性别（必填）
- PhoneNumber: 手机号（可选）
- IDNumber: 身份证号（可选）
- Address: 地址（可选）
- Age: 年龄（如果没有身份证号时手动输入）
```

### AssignDoctorDto (医生授权)
**使用场景**: 特殊患者授权给特定医生
```csharp
- PatientId: 患者ID
- DoctorId: 医生ID
```

### BatchIdsDto (批量操作)
**使用场景**: 批量启用/禁用患者
```csharp
- Ids: 患者ID列表
```

## 服务层 (IPatientService & PatientService)

### 基础CRUD方法

#### AddAsync
```csharp
Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName)
```
**功能**: 创建新患者档案
**业务逻辑**: 
- 数据完整性验证
- 身份证号唯一性检查
- 手机号唯一性检查
- 自动生成拼音码
- 从身份证自动解析出生日期和年龄
- 记录操作日志
**使用场景**: 前台登记新患者、导入患者数据

#### UpdateAsync
```csharp
Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName)
```
**功能**: 更新患者信息
**业务逻辑**: 
- 记录修改前后数据对比
- 重新验证数据唯一性（排除自身）
- 自动更新拼音码
- 重新解析身份证信息
- 记录详细操作日志
**使用场景**: 患者信息变更、资料完善

#### QuickCreateAsync
```csharp
Task<PatientDetailDto> QuickCreateAsync(QuickPatientCreateDto dto, Guid operatorId, string operatorName)
```
**功能**: 快速创建患者档案
**业务逻辑**: 
- 简化的数据验证
- 自动年龄计算
- 快速建档流程
**使用场景**: 紧急看诊、临时患者登记

### 查询类方法

#### GetByIdAsync
```csharp
Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole)
```
**功能**: 根据ID获取患者详情
**权限控制**: 管理员可查看所有患者（包括禁用），普通用户只能查看启用患者
**使用场景**: 患者详情页面、编辑前数据加载

#### GetPagedAsync
```csharp
Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole)
```
**功能**: 分页条件查询患者列表
**查询特性**: 
- 关键词模糊匹配（姓名、手机号、身份证号、拼音码）
- 权限控制（禁用患者仅管理员可见）
- 分页支持
**使用场景**: 患者管理页面的列表展示

#### SearchAsync
```csharp
Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole)
```
**功能**: 根据关键词搜索患者
**特点**: 简单模糊搜索，不分页
**使用场景**: 快速患者查找、下拉选择

#### SmartSearchAsync
```csharp
Task<List<PatientDetailDto>> SmartSearchAsync(string keyword, UserRole currentUserRole)
```
**功能**: 智能搜索患者
**智能特性**: 
- 先进行精确匹配
- 如无精确结果，进行模糊搜索
- 如有精确结果，补充相关模糊结果
- 结果限制在20条内
**使用场景**: 挂号时的患者搜索、诊疗时的患者查找

#### GetActivePatientsAsync
```csharp
Task<List<PatientDetailDto>> GetActivePatientsAsync()
```
**功能**: 获取所有启用患者列表
**使用场景**: 患者选择下拉框、统计报表

### 状态管理方法

#### EnableAsync / DisableAsync
```csharp
Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName)
Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName)
```
**功能**: 单个患者启用/禁用
**业务逻辑**: 软删除策略，仅修改Status状态
**使用场景**: 患者状态管理

#### BatchEnableAsync / BatchDisableAsync
```csharp
Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName)
Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName)
```
**功能**: 批量患者启用/禁用
**业务逻辑**: 
- 返回实际影响的记录数
- 记录批量操作日志
**使用场景**: 批量患者状态管理

### 特殊功能方法

#### GetForDoctorAsync
```csharp
Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId, UserRole currentUserRole)
```
**功能**: 获取指定医生可访问的患者
**权限控制**: 包含特殊患者的访问权限验证
**使用场景**: 医生查看自己权限范围内的患者

#### AssignDoctorAsync
```csharp
Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
```
**功能**: 将特殊患者授权给指定医生
**业务逻辑**: 
- 创建SpecialPatientDoctor关系
- 记录授权操作日志
**使用场景**: VIP患者的医生授权管理

#### GetHistoryRecordsAsync
```csharp
Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId)
```
**功能**: 获取患者历史病历记录
**跨模块调用**: 调用Records模块服务
**使用场景**: 患者病史查看、诊疗参考

### 数据验证方法

#### ValidatePatientAsync
```csharp
Task<ValidationResult> ValidatePatientAsync(PatientDetailDto dto, bool isUpdate = false)
```
**功能**: 患者数据完整性验证
**验证内容**: 
- 姓名非空验证
- 身份证号格式和唯一性验证
- 手机号唯一性验证
- 更新时排除自身的重复检查
**返回**: ValidationResult包含所有验证错误信息

### 导入导出方法

#### ImportAsync
```csharp
Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName)
```
**功能**: 批量导入患者数据
**业务逻辑**: 
- 逐条验证和导入
- 失败记录跳过，继续处理
- 返回成功导入的数量
**使用场景**: 患者数据批量导入

#### ExportAsync
```csharp
Task<List<PatientDetailDto>> ExportAsync(UserRole currentUserRole)
```
**功能**: 导出患者数据
**权限控制**: 根据用户角色决定是否包含禁用患者
**使用场景**: 患者数据导出、备份

## 仓储层 (IPatientRepository & PatientRepository)

### 基础CRUD方法

#### AddAsync / UpdateAsync
```csharp
Task<bool> AddAsync(PatientModel patient)
Task<bool> UpdateAsync(PatientModel patient)
```
**功能**: 基础的增加和更新操作
**使用场景**: 服务层调用的底层数据操作

#### EnableAsync / DisableAsync
```csharp
Task<bool> EnableAsync(Guid id)
Task<bool> DisableAsync(Guid id)
```
**功能**: 软删除策略的启用/禁用操作
**实现**: 直接修改Status字段

### 查询方法

#### GetListAsync
```csharp
Task<List<PatientModel>> GetListAsync(string? keyword, int page, int pageSize, bool includeDisabled = false)
```
**功能**: 分页条件查询
**查询条件**: 
- 关键词模糊匹配（姓名、手机号、身份证号、拼音码）
- 可选择是否包含禁用患者
**排序**: 按创建时间倒序

#### SearchAsync
```csharp
Task<List<PatientModel>> SearchAsync(string keyword, bool includeDisabled = false)
```
**功能**: 模糊搜索患者
**使用场景**: 通用的患者搜索功能

#### ExactSearchAsync
```csharp
Task<List<PatientModel>> ExactSearchAsync(string keyword, bool includeDisabled = false)
```
**功能**: 精确匹配搜索
**匹配字段**: 姓名、手机号、身份证号的精确匹配
**使用场景**: 智能搜索的精确匹配部分

### 特殊查询方法

#### GetForDoctorAsync
```csharp
Task<List<PatientModel>> GetForDoctorAsync(Guid doctorId, bool includeDisabled = false)
```
**功能**: 获取医生可访问的患者
**权限逻辑**: 
- 所有普通患者
- 该医生被授权的特殊患者
**使用场景**: 医生权限范围内的患者查询

#### IsIDNumberExistsAsync
```csharp
Task<bool> IsIDNumberExistsAsync(string idNumber, Guid? excludeId = null)
```
**功能**: 检查身份证号是否已存在
**特点**: 支持排除指定ID（用于更新时验证）
**使用场景**: 数据唯一性验证

#### IsPhoneNumberExistsAsync
```csharp
Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, Guid? excludeId = null)
```
**功能**: 检查手机号是否已存在
**特点**: 支持排除指定ID（用于更新时验证）
**使用场景**: 数据唯一性验证

### 批量操作方法

#### BatchEnableAsync / BatchDisableAsync
```csharp
Task<int> BatchEnableAsync(List<Guid> ids)
Task<int> BatchDisableAsync(List<Guid> ids)
```
**功能**: 批量更新患者状态
**特点**: 使用EF Core的批量更新，性能优化
**返回**: 实际影响的记录数

#### AssignDoctorAsync
```csharp
Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId)
```
**功能**: 建立特殊患者和医生的授权关系
**使用场景**: 特殊患者权限管理

## 权限控制策略

### 角色级别权限
- **管理员(Admin)**: 可查看和操作所有患者（包括禁用患者和特殊患者）
- **医生**: 可查看启用的普通患者和被授权的特殊患者
- **普通用户**: 只能查看启用的普通患者

### 特殊患者权限
- 特殊患者(`IsSpecial = true`)对普通用户不可见
- 只有被明确授权的医生才能查看特殊患者
- 管理员可以查看和管理所有特殊患者

### 数据隐私保护
- 禁用患者对普通用户不可见
- 特殊患者实现访问控制
- 所有管理操作都需要记录操作者信息

## 智能功能

### 身份证解析
- 自动从18位身份证号解析出生日期
- 自动计算年龄
- 验证身份证号格式正确性

### 拼音码生成
- 自动生成姓名拼音码
- 支持拼音码模糊搜索
- 提高中文姓名检索效率

### 智能搜索
- 精确匹配优先策略
- 模糊搜索补充机制
- 搜索结果智能排序

## 数据验证

### 唯一性验证
- 身份证号全局唯一
- 手机号全局唯一
- 更新时排除自身的重复检查

### 格式验证
- 身份证号格式验证
- 手机号格式验证
- 必填字段非空验证

### 业务规则验证
- 患者姓名不能为空
- 性别必须选择
- 联系方式至少填写一项

## 日志审计

### 操作日志记录
所有患者管理操作都会记录详细的审计日志，包括：
- 患者创建、编辑、启用/禁用
- 特殊患者授权
- 批量操作
- 数据导入导出

### 日志内容
- 操作者信息（ID和姓名）
- 操作类型和描述
- 操作对象信息
- 修改前后数据对比
- 操作时间

## 使用示例

### 新建患者档案
```csharp
var dto = new PatientDetailDto {
    Name = "张三",
    Gender = Gender.Male,
    PhoneNumber = "13800138000",
    IDNumber = "110101199001011234",
    Address = "北京市朝阳区"
};
await patientService.AddAsync(dto, operatorId, "管理员");
```

### 快速建档
```csharp
var quickDto = new QuickPatientCreateDto {
    Name = "李四",
    Gender = Gender.Female,
    PhoneNumber = "13900139000"
};
var patient = await patientService.QuickCreateAsync(quickDto, operatorId, "护士");
```

### 智能搜索患者
```csharp
var results = await patientService.SmartSearchAsync("张", currentUserRole);
```

### 分页查询患者
```csharp
var query = new PatientPagedQueryDto {
    Keyword = "138",
    Page = 1,
    PageSize = 20
};
var pagedResult = await patientService.GetPagedAsync(query, currentUserRole);
```

### 特殊患者授权
```csharp
await patientService.AssignDoctorAsync(patientId, doctorId, adminId, "管理员");
```

### 批量状态管理
```csharp
var ids = new List<Guid> { patientId1, patientId2, patientId3 };
var count = await patientService.BatchDisableAsync(ids, operatorId, "管理员");
```