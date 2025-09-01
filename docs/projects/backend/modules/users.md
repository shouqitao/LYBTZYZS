# LYBT.Module.Users 项目文档

## 📋 项目概述

**LYBT.Module.Users**是凌隐宝堂中医诊所系统的用户管理核心模块，负责系统内所有用户（医生和管理员）的完整生命周期管理。作为人员管理的基础，Users模块提供用户档案维护、角色权限管理、账户状态控制和用户行为监控等功能，为整个诊所的人员管理提供可靠支撑。

### 项目职责
- **用户档案管理**: 医生和管理员的基础信息维护和更新
- **角色权限管理**: Admin和Doctor角色的分配和权限控制
- **账户生命周期**: 用户创建、激活、禁用和删除的完整流程
- **批量操作支持**: 用户批量导入、导出和状态批量更新
- **用户行为监控**: 登录历史、操作记录和异常行为追踪
- **密码管理**: 初始密码设置、重置和安全策略执行

### 在系统中的位置
Users模块位于Auth模块之上，为认证系统提供用户实体数据。同时与所有业务模块关联，为医疗案例、诊断记录等业务数据提供操作者信息。它是连接系统安全与业务操作的重要桥梁。

### 关键业务价值
- **人员规范管理**: 确保诊所人员信息的准确性和完整性
- **权限精确控制**: 通过角色管理实现最小权限原则
- **操作可追溯**: 完整的用户操作日志支持审计和问责
- **管理效率提升**: 批量操作和自动化流程减少管理工作量

## 🏗️ 技术架构

### 项目架构设计
Users模块采用UltraThink双层架构标准，通过专业化分层实现高内聚低耦合：

```
UserService (委托协调层) - 实现IUserService接口
    ├── UserQueryService (查询专业层)
    │   ├── 分页搜索和多条件筛选
    │   ├── 用户统计和角色分布查询
    │   ├── 医生可用性验证查询
    │   └── 用户名邮箱唯一性验证
    └── UserBusinessService (业务逻辑层)
        ├── 用户创建和更新CRUD
        ├── 状态管理和批量操作
        ├── 密码重置和档案变更
        └── 业务规则验证和事务管理
```

**架构特点**：
- **委托模式**: UserService纯委托实现，零业务逻辑
- **专业分离**: Query层专注查询优化，Business层专注业务处理
- **接口契约**: 通过Infrastructure传递依赖实现LYBT.Shared.Interfaces.IUserService
- **依赖注入**: 使用C# 12主构造函数注入模式

### 核心技术栈
- **.NET 8.0**: 现代C#语言特性和高性能运行时
- **Entity Framework Core 8.0.17**: ORM框架，支持LINQ查询和批量操作
- **Microsoft.EntityFrameworkCore.Design 8.0.17**: EF Core设计时工具和迁移支持
- **Microsoft.EntityFrameworkCore.SqlServer 8.0.17**: SQL Server数据库提供程序
- **System.Linq.Dynamic.Core 1.6.6**: 动态LINQ查询构建和表达式解析
- **AutoMapper 15.0.1**: 实体和DTO自动映射，简化数据转换
- **BCrypt.Net**: 密码安全哈希加密
- **FluentValidation**: 强类型业务规则验证
- **Microsoft.Extensions.Logging**: 结构化日志记录
- **Microsoft.Extensions.Caching.Memory**: 用户信息缓存优化

### 依赖项目列表
**直接依赖**:
- `LYBT.Infrastructure` - 数据访问和基础服务支持（通过此项目传递依赖LYBT.Shared.Interfaces）
- `LYBT.Entities` - UserModel实体定义
- `LYBT.Shared.Models` - 用户相关DTO定义
- `LYBT.Shared.Utilities` - 密码处理和验证工具

**间接依赖**:
- `LYBT.Shared.Interfaces` - 用户服务接口契约（通过Infrastructure项目传递）

**被依赖项目**:
- `LYBT.Module.Auth` - 用户认证和身份验证
- `LYBT.Module.MedicalCase` - 医案创建者关联
- `LYBT.Module.Consultation` - 诊断医生关联
- `LYBT.WebAPI` - 控制器层调用用户服务

### 设计模式采用
- **Repository Pattern**: 通过Infrastructure的统一数据访问
- **Service Pattern**: UltraThink双层服务架构
- **Specification Pattern**: 复杂查询条件的组合
- **Factory Pattern**: 用户创建和初始化工厂
- **Observer Pattern**: 用户状态变更事件通知

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 用户CRUD核心功能
- ✅ **创建用户**: 新医生/管理员账户创建，自动生成初始密码
- ✅ **更新用户信息**: 基础信息修改，角色调整，联系方式更新
- ✅ **用户详情查询**: 完整用户信息展示，包含关联数据统计
- ✅ **删除用户**: 软删除机制，保留历史数据和操作记录
- ✅ **用户列表查询**: 分页查询，支持多条件筛选和排序

#### 2. 高级查询功能
- ✅ **用户搜索**: 按姓名、用户名、邮箱、角色等条件搜索
- ✅ **角色筛选**: 按Admin/Doctor角色筛选用户列表
- ✅ **状态筛选**: 按活跃/禁用/删除状态筛选
- ✅ **创建时间范围查询**: 按注册时间区间查询
- ✅ **复合条件查询**: 多个筛选条件的组合查询

#### 3. 批量操作功能
- ✅ **批量状态更新**: 批量激活、禁用或删除用户账户
- ✅ **批量角色分配**: 批量修改用户角色权限
- ✅ **批量导入用户**: Excel/CSV格式用户数据批量导入
- ✅ **批量导出用户**: 用户列表导出为Excel/CSV格式
- ✅ **批量密码重置**: 批量重置用户密码并发送通知

#### 4. 用户安全管理
- ✅ **密码重置**: 管理员为用户重置密码功能
- ✅ **账户锁定/解锁**: 异常账户的临时锁定和解锁
- ✅ **首次登录标识**: 跟踪用户首次登录状态
- ✅ **密码过期管理**: 密码有效期管理和强制修改
- ✅ **登录历史跟踪**: 用户登录记录和异常行为监控

#### 5. 用户统计分析
- ✅ **用户数量统计**: 按角色、状态分类的用户统计
- ✅ **活跃度分析**: 用户登录频率和使用情况分析
- ✅ **增长趋势**: 用户注册和使用趋势报表
- ✅ **权限分布**: 角色权限分配情况统计
- ✅ **异常行为报告**: 可疑登录和操作行为统计

### 接口定义规范

#### IUserService主服务接口
```csharp
/// <summary>
/// 用户服务接口 - UltraThink统一标准
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 分页查询用户
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
    
    /// <summary>
    /// 创建新用户 - UltraThink优化：使用统一变更DTO
    /// </summary>
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);
    
    /// <summary>
    /// 更新用户信息 - UltraThink优化：消除ID参数重复
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto);
    
    /// <summary>
    /// 删除用户（软删除）
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    
    /// <summary>
    /// 启用用户
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid id);
    
    /// <summary>
    /// 禁用用户
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid id);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    
    /// <summary>
    /// 批量启用用户
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
    
    /// <summary>
    /// 批量禁用用户
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
    
    /// <summary>
    /// 重置用户密码
    /// </summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
    
    /// <summary>
    /// 修改用户密码
    /// </summary>
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    
    /// <summary>
    /// 修改用户个人信息 - UltraThink优化：使用DTO模式保持一致性
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto);
    
    /// <summary>
    /// 获取所有角色列表
    /// </summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();
    
    /// <summary>
    /// 获取活跃用户列表
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    
    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 验证用户名是否可用
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
    
    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);
}
```

#### IUserQueryService查询服务接口
```csharp
public interface IUserQueryService
{
    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 分页获取用户列表
    /// </summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    
    /// <summary>
    /// 获取启用的用户列表
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    
    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取系统所有角色
    /// </summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();
    
    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);
    
    /// <summary>
    /// 验证用户名是否可用
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
    
    /// <summary>
    /// 获取所有医生
    /// </summary>
    Task<ServiceResult<List<UserDto>>> GetDoctorsAsync();
    
    /// <summary>
    /// 检查医生是否可用
    /// </summary>
    Task<ServiceResult<bool>> IsDoctorAvailableAsync(Guid doctorId);
}
```

#### IUserBusinessService业务服务接口
```csharp
public interface IUserBusinessService
{
    /// <summary>
    /// 禁用用户
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid id);
    
    /// <summary>
    /// 启用用户
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid id);
    
    /// <summary>
    /// 批量禁用用户
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
    
    /// <summary>
    /// 批量启用用户
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
    
    /// <summary>
    /// 重置密码
    /// </summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
    
    /// <summary>
    /// 更改密码
    /// </summary>
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    
    /// <summary>
    /// 修改个人信息
    /// </summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(Guid userId, string realName, string phoneNumber);
    
    /// <summary>
    /// 创建用户 - 完整业务流程
    /// </summary>
    Task<ServiceResult<UserDto>> CreateUserAsync(UserMutationDto dto);
    
    /// <summary>
    /// 更新用户 - 完整业务流程
    /// </summary>
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserMutationDto dto);
    
    /// <summary>
    /// 删除用户 - 完整业务流程
    /// </summary>
    Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
}
```

### 数据模型定义

#### UserMutationDto用户变更操作
```csharp
/// <summary>
/// 用户变更DTO - UltraThink架构优化：统一创建和更新操作
/// 消除95%的代码重复，密码字段可选（创建时必须，更新时可选）
/// </summary>
public class UserMutationDto : BaseDto
{
    /// <summary>用户名</summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
    [DisplayName("用户名")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>密码 - 创建时必须，更新时可选（null=不更新密码）</summary>
    [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
    [DisplayName("密码")]
    public string? Password { get; set; }
    
    /// <summary>确认密码 - 仅当提供密码时需要</summary>
    [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
    [DisplayName("确认密码")]
    public string? ConfirmPassword { get; set; }
    
    /// <summary>真实姓名</summary>
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;
    
    /// <summary>用户角色</summary>
    [Required(ErrorMessage = "用户角色不能为空")]
    [DisplayName("用户角色")]
    public string Role { get; set; } = "Doctor";
    
    /// <summary>电话号码</summary>
    [Phone(ErrorMessage = "电话号码格式不正确")]
    [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }
    
    /// <summary>邮箱地址</summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
    [DisplayName("邮箱地址")]
    public string? Email { get; set; }
    
    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    
    /// <summary>操作类型标识 - 用于区分创建或更新操作</summary>
    [DisplayName("操作类型")]
    public bool IsCreateOperation { get; set; }
}
```

#### UserPagedQueryDto用户分页查询
```csharp
/// <summary>
/// 用户分页查询DTO - 继承完整查询基类 + 编码接口
/// 用于用户管理的分页查询和筛选
/// </summary>
public class UserPagedQueryDto : ExtendedQueryDto, ICodeable
{
    /// <summary>用户名关键词</summary>
    [DisplayName("用户名")]
    public string? Username { get; set; }
    
    /// <summary>真实姓名关键词</summary>
    [DisplayName("真实姓名")]
    public string? RealName { get; set; }
    
    /// <summary>角色筛选</summary>
    [DisplayName("用户角色")]
    public string? Role { get; set; }
    
    /// <summary>邮箱关键词</summary>
    [DisplayName("邮箱")]
    public string? Email { get; set; }
    
    /// <summary>电话关键词</summary>
    [DisplayName("电话")]
    public string? PhoneNumber { get; set; }
    
    /// <summary>最后登录日期范围-开始日期</summary>
    [DisplayName("登录开始日期")]
    public DateTime? LoginStartDate { get; set; }
    
    /// <summary>最后登录日期范围-结束日期</summary>
    [DisplayName("登录结束日期")]
    public DateTime? LoginEndDate { get; set; }
    
    /// <summary>编码字段 - 支持拼音编码搜索</summary>
    [DisplayName("编码")]
    public string? Code { get; set; }
}
```

#### UserDto用户信息DTO
```csharp
/// <summary>
/// 用户信息DTO - UltraThink v2.0简化版
/// 与User实体对齐，删除时间字段和不存在字段
/// </summary>
public class UserDto : StatusDto
{
    /// <summary>用户名</summary>
    [DisplayName("用户名")]
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>真实姓名</summary>
    [DisplayName("真实姓名")]
    public string RealName { get; set; } = string.Empty;
    
    /// <summary>用户角色</summary>
    [DisplayName("用户角色")]
    public string Role { get; set; } = "Doctor";
    
    /// <summary>电话号码</summary>
    [DisplayName("电话号码")]
    public string? PhoneNumber { get; set; }
    
    /// <summary>邮箱地址</summary>
    [DisplayName("邮箱地址")]
    public string? Email { get; set; }
    
    /// <summary>拼音码</summary>
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }
    
    /// <summary>账号启用状态 - UltraThink兼容性别名</summary>
    [DisplayName("账号启用状态")]
    public bool IsActive => Status == CommonStatus.Enabled;
    
    /// <summary>用户名(兼容性别名)</summary>
    [DisplayName("用户名")]
    [JsonPropertyName("userDisplayName")]
    public string UserName => RealName ?? Username;
}
```

#### UserSearchDto用户搜索条件
```csharp
public class UserSearchDto : BaseSearchDto
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public CommonStatus? Status { get; set; }
    public DateTime? CreateTimeStart { get; set; }
    public DateTime? CreateTimeEnd { get; set; }
    public DateTime? LastLoginStart { get; set; }
    public DateTime? LastLoginEnd { get; set; }
    public bool? IsFirstLogin { get; set; }
    public string? SortBy { get; set; } = "CreateTime";
    public bool SortDescending { get; set; } = true;
}
```

#### UserStatisticsDto用户统计
```csharp
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int DoctorUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int UsersLoggedInToday { get; set; }
    public int FirstLoginUsers { get; set; }
    public double AverageLoginFrequency { get; set; }
    
    public List<UserRoleStatDto> RoleDistribution { get; set; } = new();
    public List<UserActivityTrendDto> ActivityTrend { get; set; } = new();
    public List<UserCreationTrendDto> CreationTrend { get; set; } = new();
}
```

### 业务规则约束
1. **用户名唯一性**: 系统内用户名必须唯一，不区分大小写
2. **邮箱唯一性**: 每个邮箱地址只能关联一个用户账户
3. **角色限制**: 只支持Admin和Doctor两种角色，不支持自定义角色
4. **软删除策略**: 用户删除使用软删除，保留历史数据和关联关系
5. **默认密码策略**: 新用户自动生成8位随机密码，强制首次登录修改
6. **管理员权限**: 至少保留一个活跃的Admin角色用户
7. **批量操作限制**: 单次批量操作不超过1000个用户

## 📋 开发规范

### 代码结构要求
```
src/Server/Modules/LYBT.Module.Users/
├── Services/
│   ├── UserQueryService.cs         # 查询专业层
│   ├── UserBusinessService.cs      # 业务逻辑层
│   └── UserService.cs              # 纯委托层
├── Controllers/
│   └── UsersController.cs          # API控制器
├── DTOs/
│   ├── UserCreateDto.cs            # 用户创建DTO
│   ├── UserUpdateDto.cs            # 用户更新DTO
│   ├── UserDto.cs                  # 用户信息DTO
│   ├── UserSearchDto.cs            # 搜索条件DTO
│   └── UserStatisticsDto.cs        # 统计信息DTO
├── Validators/
│   ├── UserCreateValidator.cs      # 创建验证器
│   ├── UserUpdateValidator.cs      # 更新验证器
│   └── UserSearchValidator.cs      # 搜索验证器
├── Mapping/
│   └── UserMappingProfile.cs       # AutoMapper配置
├── Exceptions/
│   ├── UserNotFoundException.cs    # 用户不存在异常
│   ├── DuplicateUsernameException.cs  # 用户名重复异常
│   └── UserValidationException.cs  # 用户验证异常
└── UsersModule.cs                  # 模块依赖注入注册
```

### UltraThink双层架构实现

#### UserService主服务(纯委托)
```csharp
public class UserService : IUserService
{
    private readonly IUserQueryService _queryService;
    private readonly IUserBusinessService _businessService;
    private readonly ILogger<UserService> _logger;
    
    public UserService(IUserQueryService queryService,
                      IUserBusinessService businessService,
                      ILogger<UserService> logger)
    {
        _queryService = queryService;
        _businessService = businessService;
        _logger = logger;
    }
    
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
    
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
        => await _queryService.SearchUsersAsync(criteria);
    
    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        => await _businessService.CreateUserAsync(dto);
    
    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        => await _businessService.UpdateUserAsync(id, dto);
    
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteUserAsync(id);
    
    public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        => await _businessService.BatchUpdateStatusAsync(dto);
    
    public async Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync()
        => await _queryService.GetUserStatisticsAsync();
    
    // 其他方法类似的纯委托实现...
}
```

#### UserQueryService查询专业层
```csharp
public class UserQueryService : IUserQueryService
{
    private readonly IRepository<UserModel> _userRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserQueryService> _logger;
    
    public UserQueryService(IRepository<UserModel> userRepository,
                           IMapper mapper,
                           IMemoryCache cache,
                           ILogger<UserQueryService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
    {
        try
        {
            _logger.LogInformation("执行用户搜索查询: {@Criteria}", criteria);
            
            var users = await _userRepository.GetAllAsync();
            var query = users.AsQueryable();
            
            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(criteria.Keyword))
            {
                var keyword = criteria.Keyword.ToLower();
                query = query.Where(u => 
                    u.Username.ToLower().Contains(keyword) ||
                    u.FullName.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword));
            }
            
            if (criteria.Role.HasValue)
            {
                query = query.Where(u => u.Role == criteria.Role.Value);
            }
            
            if (criteria.Status.HasValue)
            {
                query = query.Where(u => u.Status == criteria.Status.Value);
            }
            
            if (criteria.CreateTimeStart.HasValue)
            {
                query = query.Where(u => u.CreateTime >= criteria.CreateTimeStart.Value);
            }
            
            if (criteria.CreateTimeEnd.HasValue)
            {
                query = query.Where(u => u.CreateTime <= criteria.CreateTimeEnd.Value);
            }
            
            if (criteria.IsFirstLogin.HasValue)
            {
                query = query.Where(u => u.IsFirstLogin == criteria.IsFirstLogin.Value);
            }
            
            // 应用排序
            query = criteria.SortBy?.ToLower() switch
            {
                "username" => criteria.SortDescending ? 
                    query.OrderByDescending(u => u.Username) : 
                    query.OrderBy(u => u.Username),
                "fullname" => criteria.SortDescending ? 
                    query.OrderByDescending(u => u.FullName) : 
                    query.OrderBy(u => u.FullName),
                "role" => criteria.SortDescending ? 
                    query.OrderByDescending(u => u.Role) : 
                    query.OrderBy(u => u.Role),
                "status" => criteria.SortDescending ? 
                    query.OrderByDescending(u => u.Status) : 
                    query.OrderBy(u => u.Status),
                _ => criteria.SortDescending ? 
                    query.OrderByDescending(u => u.CreateTime) : 
                    query.OrderBy(u => u.CreateTime)
            };
            
            // 分页处理
            var totalCount = query.Count();
            var pagedUsers = query
                .Skip((criteria.PageNumber - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToList();
            
            // 映射到DTO
            var userDtos = _mapper.Map<List<UserDto>>(pagedUsers);
            
            // 构建分页结果
            var pagedResult = new PagedResult<UserDto>
            {
                Items = userDtos,
                PageNumber = criteria.PageNumber,
                PageSize = criteria.PageSize,
                TotalRecords = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
            };
            
            _logger.LogInformation("用户搜索完成: 找到 {TotalCount} 个结果", totalCount);
            return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户搜索查询失败: {@Criteria}", criteria);
            return ServiceResult<PagedResult<UserDto>>.Failure("搜索用户失败");
        }
    }
    
    public async Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync()
    {
        try
        {
            const string cacheKey = "user_statistics";
            if (_cache.TryGetValue(cacheKey, out UserStatisticsDto? cachedStats))
                return ServiceResult<UserStatisticsDto>.Success(cachedStats!);
            
            var users = await _userRepository.GetAllAsync();
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            
            var statistics = new UserStatisticsDto
            {
                TotalUsers = users.Count(),
                ActiveUsers = users.Count(u => u.Status == CommonStatus.Active),
                InactiveUsers = users.Count(u => u.Status == CommonStatus.Inactive),
                AdminUsers = users.Count(u => u.Role == UserRole.Admin),
                DoctorUsers = users.Count(u => u.Role == UserRole.Doctor),
                NewUsersThisMonth = users.Count(u => u.CreateTime >= thirtyDaysAgo),
                FirstLoginUsers = users.Count(u => u.IsFirstLogin),
                
                // 角色分布统计
                RoleDistribution = users.GroupBy(u => u.Role)
                    .Select(g => new UserRoleStatDto
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        Percentage = (double)g.Count() / users.Count() * 100
                    }).ToList(),
                
                // 创建趋势统计
                CreationTrend = users.Where(u => u.CreateTime >= thirtyDaysAgo)
                    .GroupBy(u => u.CreateTime.Date)
                    .Select(g => new UserCreationTrendDto
                    {
                        Date = g.Key,
                        Count = g.Count()
                    }).OrderBy(x => x.Date).ToList()
            };
            
            _cache.Set(cacheKey, statistics, TimeSpan.FromMinutes(15));
            return ServiceResult<UserStatisticsDto>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户统计信息失败");
            return ServiceResult<UserStatisticsDto>.Failure("获取统计信息失败");
        }
    }
}
```

#### UserBusinessService业务逻辑层
```csharp
public class UserBusinessService : IUserBusinessService
{
    private readonly IRepository<UserModel> _userRepository;
    private readonly IPasswordHelper _passwordHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<UserBusinessService> _logger;
    
    public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto)
    {
        try
        {
            _logger.LogInformation("开始创建用户: {Username}", dto.Username);
            
            // 1. 验证用户名唯一性
            var existingUsers = await _userRepository.GetAllAsync();
            if (existingUsers.Any(u => u.Username.Equals(dto.Username, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("用户创建失败 - 用户名已存在: {Username}", dto.Username);
                return ServiceResult<UserDto>.Failure("用户名已存在");
            }
            
            // 2. 验证邮箱唯一性
            if (existingUsers.Any(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("用户创建失败 - 邮箱已存在: {Email}", dto.Email);
                return ServiceResult<UserDto>.Failure("邮箱地址已存在");
            }
            
            // 3. 生成初始密码
            var initialPassword = GenerateInitialPassword();
            var passwordHash = _passwordHelper.HashPassword(initialPassword);
            
            // 4. 创建用户实体
            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Role = dto.Role,
                PasswordHash = passwordHash,
                Status = dto.IsActive ? CommonStatus.Active : CommonStatus.Inactive,
                IsFirstLogin = true,
                CreateTime = DateTime.UtcNow
            };
            
            // 5. 保存到数据库
            var createdUser = await _userRepository.CreateAsync(user);
            
            // 6. 记录操作日志
            _logger.LogInformation("用户创建成功: {Username}, UserId: {UserId}, 初始密码: {Password}", 
                dto.Username, createdUser.Id, initialPassword);
            
            // 7. 映射返回结果
            var userDto = _mapper.Map<UserDto>(createdUser);
            
            // 8. 发送初始密码通知（实际项目中可能需要邮件或短信通知）
            await NotifyUserCreationAsync(createdUser, initialPassword);
            
            return ServiceResult<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户异常: {Username}", dto.Username);
            return ServiceResult<UserDto>.Failure("创建用户失败，请稍后重试");
        }
    }
    
    public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
    {
        try
        {
            _logger.LogInformation("开始批量更新用户状态: {UserIds}, 新状态: {Status}", 
                string.Join(",", dto.UserIds), dto.Status);
            
            if (dto.UserIds.Count > 1000)
            {
                return ServiceResult<bool>.Failure("批量操作数量不能超过1000个");
            }
            
            // 验证要更新的用户是否存在
            var users = await _userRepository.GetAllAsync();
            var existingUsers = users.Where(u => dto.UserIds.Contains(u.Id)).ToList();
            
            if (existingUsers.Count != dto.UserIds.Count)
            {
                var missingIds = dto.UserIds.Except(existingUsers.Select(u => u.Id)).ToList();
                _logger.LogWarning("批量更新失败 - 部分用户不存在: {MissingIds}", string.Join(",", missingIds));
                return ServiceResult<bool>.Failure("部分用户不存在");
            }
            
            // 检查是否会导致所有管理员被禁用
            if (dto.Status == CommonStatus.Inactive)
            {
                var adminUsers = existingUsers.Where(u => u.Role == UserRole.Admin).ToList();
                var remainingActiveAdmins = users.Where(u => 
                    u.Role == UserRole.Admin && 
                    u.Status == CommonStatus.Active && 
                    !dto.UserIds.Contains(u.Id)).ToList();
                
                if (adminUsers.Any() && !remainingActiveAdmins.Any())
                {
                    return ServiceResult<bool>.Failure("不能禁用所有管理员账户");
                }
            }
            
            // 执行批量更新
            var affectedRows = await _userRepository.ExecuteUpdateAsync(
                u => dto.UserIds.Contains(u.Id),
                setters => setters
                    .SetProperty(u => u.Status, dto.Status)
                    .SetProperty(u => u.UpdateTime, DateTime.UtcNow));
            
            if (affectedRows > 0)
            {
                _logger.LogInformation("批量状态更新成功: 更新了 {Count} 个用户", affectedRows);
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                _logger.LogWarning("批量状态更新失败: 没有用户被更新");
                return ServiceResult<bool>.Failure("没有用户状态被更新");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新用户状态异常: {UserIds}", string.Join(",", dto.UserIds));
            return ServiceResult<bool>.Failure("批量更新失败");
        }
    }
    
    private string GenerateInitialPassword()
    {
        // 生成8位随机密码，包含大小写字母、数字和特殊字符
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    private async Task NotifyUserCreationAsync(UserModel user, string initialPassword)
    {
        // 这里可以实现邮件或短信通知新用户
        // 在实际项目中，应该集成邮件服务或短信服务
        _logger.LogInformation("用户创建通知 - 用户名: {Username}, 初始密码: {Password}", 
            user.Username, initialPassword);
    }
}
```

### 命名规范
- **服务类**: PascalCase + Service后缀 (UserService, UserQueryService)
- **DTO类**: PascalCase + Dto后缀 (UserCreateDto, UserSearchDto)
- **验证器**: PascalCase + Validator后缀 (UserCreateValidator)
- **异常类**: PascalCase + Exception后缀 (UserNotFoundException)
- **接口**: I前缀 + PascalCase (IUserService, IUserQueryService)
- **方法**: PascalCase，异步方法Async后缀，批量操作Batch前缀

### 质量标准
- **数据验证**: 所有输入DTO必须有完整的验证注解和业务规则验证
- **异常处理**: 所有public方法必须有异常处理，不暴露内部实现细节
- **日志记录**: 关键业务操作记录详细日志，包含操作者和操作对象信息
- **缓存策略**: 用户统计信息缓存15分钟，频繁查询的数据适当缓存
- **性能要求**: 用户搜索<2秒，批量操作<5秒，单用户CRUD<1秒
- **并发安全**: 支持多用户同时操作，避免并发更新冲突

### 测试要求
- **单元测试覆盖率**: >85%，特别是业务逻辑和验证规则
- **集成测试**: 完整的用户CRUD流程和批量操作流程
- **性能测试**: 大量用户数据的查询性能和批量操作性能
- **边界测试**: 极端输入数据和边界条件的处理

## 🔌 集成接口

### 对外提供的接口

#### 1. RESTful API接口
```http
# 获取用户列表
GET /api/v1/users?pageNumber=1&pageSize=10&keyword=张&role=Doctor&status=Active
Authorization: Bearer <access_token>

# 响应
{
    "success": true,
    "data": {
        "items": [
            {
                "id": "123e4567-e89b-12d3-a456-426614174000",
                "username": "doctor01",
                "email": "doctor01@lybt.com",
                "fullName": "张医生",
                "role": "Doctor",
                "status": "Active",
                "createTime": "2025-01-15T10:30:00Z",
                "totalMedicalCases": 25,
                "lastLoginTime": "2025-09-01T08:00:00Z"
            }
        ],
        "pageNumber": 1,
        "pageSize": 10,
        "totalRecords": 15,
        "totalPages": 2
    }
}

# 创建新用户
POST /api/v1/users
Authorization: Bearer <access_token>
{
    "username": "doctor02",
    "email": "doctor02@lybt.com",
    "fullName": "李医生",
    "phone": "13800138002",
    "role": "Doctor",
    "isActive": true
}

# 更新用户信息
PUT /api/v1/users/{id}
Authorization: Bearer <access_token>
{
    "email": "new-email@lybt.com",
    "fullName": "张主任医师",
    "phone": "13800138001",
    "role": "Doctor",
    "status": "Active"
}

# 批量状态更新
POST /api/v1/users/batch/status
Authorization: Bearer <access_token>
{
    "userIds": [
        "123e4567-e89b-12d3-a456-426614174000",
        "234e5678-e89b-12d3-a456-426614174001"
    ],
    "status": "Inactive"
}

# 获取用户统计
GET /api/v1/users/statistics
Authorization: Bearer <access_token>

# 重置用户密码
POST /api/v1/users/{id}/reset-password
Authorization: Bearer <access_token>
```

#### 2. 内部服务接口
```csharp
// 其他业务模块可以通过依赖注入使用
public class MedicalCaseBusinessService
{
    private readonly IUserService _userService;
    
    public async Task<bool> ValidateDoctor(Guid doctorId)
    {
        var result = await _userService.GetByIdAsync(doctorId);
        return result.IsSuccess && 
               result.Data?.Role == UserRole.Doctor && 
               result.Data?.Status == CommonStatus.Active;
    }
}
```

### 依赖的外部接口
- **IRepository<UserModel>**: Infrastructure提供的用户数据访问接口
- **IPasswordHelper**: Shared.Utilities提供的密码处理工具
- **IMapper**: AutoMapper对象映射服务
- **IMemoryCache**: .NET内存缓存服务
- **ILogger<T>**: .NET结构化日志服务
- **IEmailService**: 邮件通知服务（可选）

### 数据传输格式

#### 用户列表响应格式
```json
{
    "success": true,
    "message": "查询用户列表成功",
    "data": {
        "items": [
            {
                "id": "guid",
                "username": "string",
                "email": "string",
                "fullName": "string",
                "phone": "string",
                "role": "Admin|Doctor",
                "roleName": "管理员|医生",
                "status": "Active|Inactive|Deleted",
                "statusName": "活跃|禁用|已删除",
                "isFirstLogin": boolean,
                "createTime": "datetime",
                "updateTime": "datetime",
                "lastLoginTime": "datetime",
                "totalMedicalCases": number,
                "totalConsultations": number
            }
        ],
        "pageNumber": number,
        "pageSize": number,
        "totalRecords": number,
        "totalPages": number
    }
}
```

#### 用户统计响应格式
```json
{
    "success": true,
    "data": {
        "totalUsers": 25,
        "activeUsers": 20,
        "inactiveUsers": 5,
        "adminUsers": 2,
        "doctorUsers": 23,
        "newUsersThisMonth": 3,
        "firstLoginUsers": 1,
        "roleDistribution": [
            {
                "role": "Admin",
                "count": 2,
                "percentage": 8.0
            },
            {
                "role": "Doctor", 
                "count": 23,
                "percentage": 92.0
            }
        ],
        "creationTrend": [
            {
                "date": "2025-08-01",
                "count": 1
            }
        ]
    }
}
```

### 错误处理规范
- **400 Bad Request**: 请求参数验证失败或业务规则违反
- **404 Not Found**: 指定的用户不存在
- **409 Conflict**: 用户名或邮箱冲突，违反唯一性约束
- **422 Unprocessable Entity**: 复杂业务验证失败（如不能删除最后一个管理员）
- **500 Internal Server Error**: 服务器内部错误

## ⚙️ 配置管理

### 配置项定义

#### 用户管理相关配置
```json
{
  "UserManagementOptions": {
    "InitialPasswordLength": 8,
    "InitialPasswordComplexity": true,
    "EnableBatchOperations": true,
    "MaxBatchSize": 1000,
    "EnableUserImportExport": true,
    "RequireEmailVerification": false,
    "AutoDeactivateInactiveUsers": false,
    "InactiveUserDays": 90,
    "MinAdminUsers": 1,
    "DefaultUserStatus": "Active",
    "EnableUserStatisticsCache": true,
    "StatisticsCacheMinutes": 15
  },
  "UserValidationOptions": {
    "UsernameMinLength": 3,
    "UsernameMaxLength": 50,
    "UsernameAllowedChars": "^[a-zA-Z0-9_]+$",
    "FullNameMinLength": 2,
    "FullNameMaxLength": 20,
    "EmailRequired": true,
    "PhoneRequired": false,
    "RemarkMaxLength": 200
  },
  "UserSecurityOptions": {
    "ForcePasswordChangeOnFirstLogin": true,
    "EnablePasswordExpiration": false,
    "PasswordExpirationDays": 90,
    "EnableAccountLockout": true,
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 15
  }
}
```

### 环境变量要求
```bash
# 用户管理配置
USERMANAGEMENTOPTIONS__INITIALPASWORDLENGTH=8
USERMANAGEMENTOPTIONS__ENABLEBATCHOPERATIONS=true
USERMANAGEMENTOPTIONS__MAXBATCHSIZE=1000
USERMANAGEMENTOPTIONS__MINADMINUSERS=1

# 用户验证配置
USERVALIDATIONOPTIONS__USERNAMEMINLENGTH=3
USERVALIDATIONOPTIONS__FULLNAMEMINLENGTH=2
USERVALIDATIONOPTIONS__EMAILREQUIRED=true

# 用户安全配置
USERSECURITYOPTIONS__FORCEPASSWORDCHANGEONFIRSTLOGIN=true
USERSECURITYOPTIONS__ENABLEACCOUNTLOCKOUT=true
USERSECURITYOPTIONS__MAXFAILEDLOGINATTEMPTS=5

# 默认用户配置
DEFAULT_USER_PASSWORD_LENGTH=8
DEFAULT_ADMIN_COUNT=1
```

### 部署配置说明
1. **开发环境**: 使用简单的用户验证规则，方便测试
2. **测试环境**: 使用接近生产的验证规则，但可以降低安全要求
3. **生产环境**: 严格的用户验证和安全配置，启用所有安全特性
4. **高可用部署**: 考虑用户数据的缓存一致性和会话共享

## 🧪 测试规范

### 单元测试要求

#### 用户业务逻辑测试
```csharp
public class UserBusinessServiceTests : IDisposable
{
    private readonly Mock<IRepository<UserModel>> _mockUserRepository;
    private readonly Mock<IPasswordHelper> _mockPasswordHelper;
    private readonly Mock<IMapper> _mockMapper;
    private readonly UserBusinessService _service;
    
    public UserBusinessServiceTests()
    {
        _mockUserRepository = new Mock<IRepository<UserModel>>();
        _mockPasswordHelper = new Mock<IPasswordHelper>();
        _mockMapper = new Mock<IMapper>();
        
        var logger = Mock.Of<ILogger<UserBusinessService>>();
        
        _service = new UserBusinessService(
            _mockUserRepository.Object,
            _mockPasswordHelper.Object,
            _mockMapper.Object,
            logger);
    }
    
    [Fact]
    public async Task CreateUserAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "newdoctor",
            Email = "newdoctor@lybt.com",
            FullName = "新医生",
            Role = UserRole.Doctor,
            IsActive = true
        };
        
        _mockUserRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<UserModel>());
        
        _mockPasswordHelper.Setup(p => p.HashPassword(It.IsAny<string>()))
                          .Returns("hashed_password");
        
        var createdUser = new UserModel
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            Email = dto.Email,
            FullName = dto.FullName,
            Role = dto.Role
        };
        
        _mockUserRepository.Setup(r => r.CreateAsync(It.IsAny<UserModel>()))
                          .ReturnsAsync(createdUser);
        
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<UserModel>()))
                  .Returns(new UserDto { Id = createdUser.Id, Username = dto.Username });
        
        // Act
        var result = await _service.CreateUserAsync(dto);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Username.Should().Be(dto.Username);
        
        _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<UserModel>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateUserAsync_DuplicateUsername_ReturnsFailure()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "existinguser",
            Email = "new@lybt.com",
            FullName = "新用户",
            Role = UserRole.Doctor
        };
        
        var existingUser = new UserModel
        {
            Username = "existinguser",
            Email = "existing@lybt.com"
        };
        
        _mockUserRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<UserModel> { existingUser });
        
        // Act
        var result = await _service.CreateUserAsync(dto);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名已存在");
        
        _mockUserRepository.Verify(r => r.CreateAsync(It.IsAny<UserModel>()), Times.Never);
    }
    
    [Fact]
    public async Task BatchUpdateStatusAsync_DisableAllAdmins_ReturnsFailure()
    {
        // Arrange
        var adminUser1 = new UserModel { Id = Guid.NewGuid(), Role = UserRole.Admin, Status = CommonStatus.Active };
        var adminUser2 = new UserModel { Id = Guid.NewGuid(), Role = UserRole.Admin, Status = CommonStatus.Active };
        var doctorUser = new UserModel { Id = Guid.NewGuid(), Role = UserRole.Doctor, Status = CommonStatus.Active };
        
        _mockUserRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<UserModel> { adminUser1, adminUser2, doctorUser });
        
        var dto = new BatchStatusUpdateDto
        {
            UserIds = new List<Guid> { adminUser1.Id, adminUser2.Id },
            Status = CommonStatus.Inactive
        };
        
        // Act
        var result = await _service.BatchUpdateStatusAsync(dto);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("不能禁用所有管理员账户");
    }
}
```

### 查询服务测试
```csharp
public class UserQueryServiceTests
{
    [Fact]
    public async Task SearchUsersAsync_WithKeyword_ReturnsMatchingUsers()
    {
        // 测试关键词搜索功能
    }
    
    [Fact]
    public async Task GetUserStatisticsAsync_ValidCall_ReturnsCorrectStatistics()
    {
        // 测试用户统计功能
    }
    
    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Doctor)]
    public async Task GetUsersByRoleAsync_ValidRole_ReturnsUsersWithRole(UserRole role)
    {
        // 测试按角色查询功能
    }
}
```

### 集成测试要求

#### 用户API集成测试
```csharp
public class UsersApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    [Fact]
    public async Task GET_Users_WithValidAuth_ReturnsUserList()
    {
        // 测试获取用户列表API
    }
    
    [Fact]
    public async Task POST_Users_ValidData_CreatesUser()
    {
        // 测试创建用户API
    }
    
    [Fact]
    public async Task PUT_Users_ValidData_UpdatesUser()
    {
        // 测试更新用户API
    }
    
    [Fact]
    public async Task POST_BatchStatus_ValidData_UpdatesMultipleUsers()
    {
        // 测试批量状态更新API
    }
}
```

### 性能测试要求
```csharp
public class UserPerformanceTests
{
    [Fact]
    public async Task SearchUsers_LargeDataset_CompletesWithinTimeLimit()
    {
        // 测试大数据量用户搜索性能
        // 目标: 10000个用户的搜索在2秒内完成
    }
    
    [Fact]
    public async Task BatchUpdate_1000Users_CompletesWithinTimeLimit()
    {
        // 测试批量更新性能
        // 目标: 1000个用户的批量更新在5秒内完成
    }
}
```

### 测试覆盖率目标
- **核心业务逻辑**: >90%覆盖率
- **查询服务**: >85%覆盖率
- **验证规则**: 100%覆盖率
- **API端点**: >80%覆盖率
- **异常处理**: >75%覆盖率

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译Users模块
- **AutoMapper依赖**: 确保对象映射库正确配置
- **FluentValidation依赖**: 确保验证框架正确安装

### 部署步骤

#### 1. 模块部署验证
```bash
# 验证Users模块编译
dotnet build LYBT.Module.Users.csproj

# 验证服务注册
dotnet run --project LYBT.WebAPI
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/users
```

#### 2. 初始用户数据验证
```bash
# 验证数据库中有默认管理员
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# 查询管理员用户
curl -H "Authorization: Bearer <admin_token>" \
  "http://localhost:5000/api/v1/users?role=Admin"
```

#### 3. 批量操作功能测试
```bash
# 测试批量状态更新
curl -X POST http://localhost:5000/api/v1/users/batch/status \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"userIds":["guid1","guid2"],"status":"Inactive"}'
```

### 环境依赖
- **数据库访问**: 需要UserModel表的完整读写权限
- **缓存服务**: 用户统计信息需要缓存服务支持
- **文件系统**: 批量导入导出功能需要临时文件存储权限
- **邮件服务**: 用户创建通知需要邮件服务支持（可选）

### 运行监控

#### 用户管理性能监控
```http
# 用户操作性能指标
GET /api/v1/monitoring/users/performance

# 用户增长趋势
GET /api/v1/monitoring/users/growth-trend?period=30d

# 批量操作执行状态
GET /api/v1/monitoring/users/batch-operations
```

#### 用户行为监控
```http
# 活跃用户统计
GET /api/v1/monitoring/users/activity?period=7d

# 角色分布监控
GET /api/v1/monitoring/users/role-distribution

# 异常用户行为
GET /api/v1/monitoring/users/suspicious-activity
```

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Module.Auth项目文档](./auth.md) - 用户认证和权限验证
- [LYBT.Infrastructure项目文档](../core/infrastructure.md) - 数据访问和基础设施
- [LYBT.Entities项目文档](../core/entities.md) - UserModel实体定义

### API文档链接
- [用户管理API规范](../../../api/users-api.md) - 完整的用户管理REST API
- [批量操作API](../../../api/batch-operations-api.md) - 批量操作接口规范
- [用户统计API](../../../api/user-statistics-api.md) - 用户数据统计接口

### 技术规范引用
- [UltraThink双层架构标准](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md) - 架构实施标准
- [批量操作最佳实践](../../../development/batch-operations-guide.md) - 批量操作设计指南
- [用户权限管理规范](../../../security/user-permission-management.md) - 角色权限设计
- [数据验证标准](../../../development/data-validation-standard.md) - 输入验证规范

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过