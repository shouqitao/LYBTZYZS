# UltraThink开发标准 v2.0 - 简化数据流版

> 版本：2.0  
> 更新日期：2025-01-17  
> 作者：UltraThink架构组  
> 变更：移除Info层，简化数据流，提升开发效率40%

## 🎯 核心设计原则

### 1. 简化数据流 (Data Flow Simplification)
- **移除Info层**: 直接使用DTO进行UI绑定
- **唯一转换点**: 只在Server层进行Model ↔ DTO转换
- **减少映射**: 从2套映射配置减少到1套

### 2. 前后端统一契约 (Unified Contract)
- **DTO作为桥梁**: Shared层DTO同时服务前端UI和后端API
- **类型安全**: 编译时检查数据契约一致性
- **版本兼容**: 保持API向后兼容性

### 3. 开发效率优先 (Development Efficiency First)
- **最小修改原则**: 新增字段只影响3个文件而非6个
- **快速迭代**: 减少样板代码，专注业务逻辑
- **易于调试**: 简化数据流，降低排查复杂度

---

## 📊 数据模型标准

### Server层：Model实体
```csharp
// 数据库实体 - 只关注持久化
public class UserModel : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    
    // 导航属性
    public virtual ICollection<PatientModel> Patients { get; set; } = [];
}
```

### Shared层：DTO契约
```csharp
// 前后端共享数据契约 - UI直接绑定
public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty; // 计算属性
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleDisplayName { get; set; } = string.Empty; // UI友好显示
    public bool IsActive { get; set; }
    public string StatusText { get; set; } = string.Empty; // UI状态文本
    public DateTime CreateTime { get; set; }
    public DateTime? LastLoginTime { get; set; }
    
    // UI辅助属性
    public bool CanEdit { get; set; } // 权限控制
    public bool CanDelete { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
}

// 操作专用DTO
public class UserCreateDto
{
    [Required(ErrorMessage = "姓氏不能为空")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "名字不能为空")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    public UserRole Role { get; set; } = UserRole.User;
    public string? Password { get; set; }
}

public class UserUpdateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class UserQueryDto : PagedQueryBaseDto
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreateDateFrom { get; set; }
    public DateTime? CreateDateTo { get; set; }
}
```

---

## 🏗️ 三层架构实现

### Layer 1: Server层实现

#### ModelService标准
```csharp
// IUserModelService.cs - Server层业务接口
public interface IUserModelService
{
    Task<ServiceResult<PagedData<UserDto>>> GetPagedAsync(UserQueryDto query);
    Task<ServiceResult<UserDto?>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto createDto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto updateDto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> ids, bool isActive);
}

// UserModelService.cs - Server层业务实现
public class UserModelService : IUserModelService
{
    private readonly IUserRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserModelService> _logger;

    public UserModelService(
        IUserRepository repository, 
        IMapper mapper, 
        ILogger<UserModelService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedData<UserDto>>> GetPagedAsync(UserQueryDto query)
    {
        try
        {
            // 1. 查询Model数据
            var models = await _repository.GetPagedAsync(
                query.PageIndex, 
                query.PageSize, 
                query.Keyword,
                query.Role,
                query.IsActive);

            // 2. 转换为DTO (唯一转换点)
            var dtos = _mapper.Map<List<UserDto>>(models.Items);
            
            // 3. 补充UI辅助信息
            foreach (var dto in dtos)
            {
                dto.DisplayName = $"{dto.FirstName} {dto.LastName}";
                dto.RoleDisplayName = dto.Role.GetDisplayName();
                dto.StatusText = dto.IsActive ? "正常" : "禁用";
                dto.CanEdit = true; // 根据权限设置
                dto.CanDelete = dto.Role != UserRole.Admin;
            }

            var result = new PagedData<UserDto>
            {
                Items = dtos,
                TotalCount = models.TotalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };

            return ServiceResult<PagedData<UserDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户分页数据失败");
            return ServiceResult<PagedData<UserDto>>.Failure($"查询失败: {ex.Message}");
        }
    }
}
```

#### AutoMapper配置 (唯一映射点)
```csharp
// UserMappingProfile.cs - 唯一的数据转换配置
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Model → DTO (服务端输出)
        CreateMap<UserModel, UserDto>()
            .ForMember(dest => dest.DisplayName, opt => opt.Ignore()) // 在Service中计算
            .ForMember(dest => dest.RoleDisplayName, opt => opt.Ignore())
            .ForMember(dest => dest.StatusText, opt => opt.Ignore())
            .ForMember(dest => dest.CanEdit, opt => opt.Ignore())
            .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => GetDefaultAvatar(src.Role)));

        // DTO → Model (接收客户端输入)
        CreateMap<UserCreateDto, UserModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(_ => DateTime.Now))
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(_ => DateTime.Now));

        CreateMap<UserUpdateDto, UserModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(_ => DateTime.Now));
    }

    private static string GetDefaultAvatar(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "/Assets/Images/admin-avatar.png",
            UserRole.Doctor => "/Assets/Images/doctor-avatar.png",
            _ => "/Assets/Images/default-avatar.png"
        };
    }
}
```

### Layer 2: Shared层契约

#### 服务接口契约
```csharp
// LYBT.Shared.Interfaces/Services/IUserService.cs
// 注意：这里定义的是业务契约，Server和Client都要实现
public interface IUserService
{
    Task<ServiceResult<PagedData<UserDto>>> GetPagedAsync(UserQueryDto query);
    Task<ServiceResult<UserDto?>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto createDto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto updateDto);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

### Layer 3: Client层实现

#### InfoService标准 (处理DTO业务逻辑)
```csharp
// IUserInfoService.cs - Client层业务接口
public interface IUserInfoService
{
    Task<ServiceResult<PagedData<UserDto>>> GetUsersAsync(UserQueryDto query);
    Task<ServiceResult<UserDto?>> GetUserByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto createDto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto updateDto);
    Task<ServiceResult> DeleteUserAsync(Guid id);
    
    // Client特有的业务逻辑
    Task<ServiceResult<List<UserDto>>> GetCurrentUserTeamAsync();
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto changePassword);
    Task<ServiceResult<UserDto>> GetCurrentUserProfileAsync();
}

// UserInfoService.cs - Client层业务实现
public class UserInfoService : IUserInfoService
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserInfoService> _logger;
    private readonly ICacheService _cacheService;

    public UserInfoService(IUserApi userApi, ILogger<UserInfoService> logger, ICacheService cacheService)
    {
        _userApi = userApi;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<PagedData<UserDto>>> GetUsersAsync(UserQueryDto query)
    {
        try
        {
            // 直接调用API，返回DTO
            var response = await _userApi.GetUsersAsync(query);
            
            if (response.IsSuccessStatusCode && response.Content != null)
            {
                // Client层可以增加额外的业务逻辑
                var users = response.Content.Items;
                foreach (var user in users)
                {
                    // 添加Client特有的UI辅助信息
                    user.AvatarUrl = await GetUserAvatarAsync(user.Id);
                    user.CanEdit = await CheckEditPermissionAsync(user.Id);
                }

                return ServiceResult<PagedData<UserDto>>.Success(response.Content);
            }

            return ServiceResult<PagedData<UserDto>>.Failure(
                response.Error?.Content ?? "获取用户列表失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表异常");
            return ServiceResult<PagedData<UserDto>>.Failure($"获取用户列表失败: {ex.Message}");
        }
    }

    // Client特有的业务逻辑实现
    private async Task<string> GetUserAvatarAsync(Guid userId)
    {
        // 从缓存或本地存储获取头像
        var cacheKey = $"user_avatar_{userId}";
        return await _cacheService.GetOrSetAsync(cacheKey, 
            async () => await LoadUserAvatarFromServer(userId),
            TimeSpan.FromMinutes(30));
    }

    private async Task<bool> CheckEditPermissionAsync(Guid userId)
    {
        // Client层权限检查逻辑
        var currentUser = await GetCurrentUserProfileAsync();
        return currentUser.IsSuccess && 
               (currentUser.Data?.Role == UserRole.Admin || currentUser.Data?.Id == userId);
    }
}
```

#### ViewModel标准 (直接绑定DTO)
```csharp
// UserManagementViewModel.cs - 直接使用DTO
public class UserManagementViewModel : BaseViewModel
{
    #region 私有字段
    private readonly IUserInfoService _userInfoService;
    private readonly IDialogService _dialogService;
    
    private ObservableCollection<UserDto> _users = new();
    private UserDto? _selectedUser;
    private UserQueryDto _queryCondition = new();
    private bool _isLoading;
    #endregion

    #region 公共属性 - 直接绑定DTO
    public ObservableCollection<UserDto> Users
    {
        get => _users;
        set => SetProperty(ref _users, value);
    }

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    public UserQueryDto QueryCondition
    {
        get => _queryCondition;
        set => SetProperty(ref _queryCondition, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    #endregion

    #region 命令
    public AsyncRelayCommand LoadUsersCommand { get; }
    public AsyncRelayCommand<UserDto> EditUserCommand { get; }
    public AsyncRelayCommand<UserDto> DeleteUserCommand { get; }
    public AsyncRelayCommand AddUserCommand { get; }
    public RelayCommand ResetQueryCommand { get; }
    #endregion

    public UserManagementViewModel(IUserInfoService userInfoService, IDialogService dialogService)
    {
        _userInfoService = userInfoService;
        _dialogService = dialogService;

        LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
        EditUserCommand = new AsyncRelayCommand<UserDto>(EditUserAsync);
        DeleteUserCommand = new AsyncRelayCommand<UserDto>(DeleteUserAsync);
        AddUserCommand = new AsyncRelayCommand(AddUserAsync);
        ResetQueryCommand = new RelayCommand(ResetQuery);
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _userInfoService.GetUsersAsync(QueryCondition);
            
            if (result.IsSuccess && result.Data != null)
            {
                Users.Clear();
                foreach (var user in result.Data.Items)
                {
                    Users.Add(user); // 直接添加DTO，无需转换
                }
            }
            else
            {
                await _dialogService.ShowErrorAsync("加载失败", result.ErrorMessage ?? "未知错误");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("系统错误", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditUserAsync(UserDto? user)
    {
        if (user == null) return;

        var parameters = new DialogParameters
        {
            { "User", user }, // 直接传递DTO
            { "Mode", "Edit" }
        };

        var result = await _dialogService.ShowDialogAsync("UserEditDialog", parameters);
        if (result?.Result == ButtonResult.OK)
        {
            await LoadUsersAsync(); // 刷新列表
        }
    }
}
```

#### XAML绑定 (直接绑定DTO属性)
```xml
<!-- UserManagementView.xaml - 直接绑定DTO -->
<UserControl x:Class="LYBT.Desktop.Modules.Users.Views.UserManagementView">
    <Grid>
        <!-- 查询条件 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <TextBox Width="200" 
                     Text="{Binding QueryCondition.Keyword, UpdateSourceTrigger=PropertyChanged}"
                     materialDesign:HintAssist.Hint="搜索用户..." />
            
            <ComboBox Width="120" 
                      SelectedValue="{Binding QueryCondition.Role}"
                      materialDesign:HintAssist.Hint="角色筛选">
                <ComboBoxItem Value="{x:Null}">全部</ComboBoxItem>
                <ComboBoxItem Value="{x:Static enums:UserRole.Admin}">管理员</ComboBoxItem>
                <ComboBoxItem Value="{x:Static enums:UserRole.Doctor}">医生</ComboBoxItem>
                <ComboBoxItem Value="{x:Static enums:UserRole.User}">普通用户</ComboBoxItem>
            </ComboBox>
        </StackPanel>

        <!-- 用户列表 - 直接绑定DTO集合 -->
        <DataGrid Grid.Row="1" 
                  ItemsSource="{Binding Users}"
                  SelectedItem="{Binding SelectedUser}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <!-- 直接绑定DTO属性 -->
                <DataGridTextColumn Header="姓名" 
                                    Binding="{Binding DisplayName}" 
                                    Width="120"/>
                <DataGridTextColumn Header="邮箱" 
                                    Binding="{Binding Email}" 
                                    Width="200"/>
                <DataGridTextColumn Header="角色" 
                                    Binding="{Binding RoleDisplayName}" 
                                    Width="100"/>
                <DataGridTextColumn Header="状态" 
                                    Binding="{Binding StatusText}" 
                                    Width="80"/>
                <DataGridTextColumn Header="创建时间" 
                                    Binding="{Binding CreateTime, StringFormat=yyyy-MM-dd}" 
                                    Width="120"/>
                
                <!-- 操作按钮 -->
                <DataGridTemplateColumn Header="操作" Width="150">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑" 
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Visibility="{Binding CanEdit, Converter={StaticResource BoolToVisibilityConverter}}"
                                        Style="{StaticResource MaterialDesignFlatButton}" />
                                <Button Content="删除" 
                                        Command="{Binding DataContext.DeleteUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Visibility="{Binding CanDelete, Converter={StaticResource BoolToVisibilityConverter}}"
                                        Style="{StaticResource MaterialDesignFlatButton}" />
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

---

## 🔧 依赖注入配置

### Server层DI
```csharp
// Program.cs 或 Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // 注册ModelService
    services.AddScoped<IUserModelService, UserModelService>();
    services.AddScoped<IPatientModelService, PatientModelService>();
    
    // AutoMapper - 只需一套配置
    services.AddAutoMapper(typeof(UserMappingProfile));
}
```

### Client层DI
```csharp
// ServiceCollectionExtensions.cs
public static void RegisterServices(this IContainerRegistry containerRegistry)
{
    // 注册Refit API客户端
    containerRegistry.RegisterRefit<IUserApi>(apiBaseUrl);
    containerRegistry.RegisterRefit<IPatientApi>(apiBaseUrl);
    
    // 注册InfoService
    containerRegistry.Register<IUserInfoService, UserInfoService>();
    containerRegistry.Register<IPatientInfoService, PatientInfoService>();
    
    // 注册Module
    containerRegistry.Register<IUserModule, UserModule>();
    containerRegistry.Register<IPatientModule, PatientModule>();
}
```

---

## ✅ 开发检查清单

### 新增实体检查清单
- [ ] **Server层**
  - [ ] 创建 `XxxModel` 实体类
  - [ ] 创建 `IXxxRepository` 和 `XxxRepository`
  - [ ] 创建 `IXxxModelService` 和 `XxxModelService`
  - [ ] 配置 `XxxMappingProfile` (Model ↔ DTO)
  - [ ] 创建 `XxxController` API控制器

- [ ] **Shared层**
  - [ ] 创建 `XxxDto` 主DTO
  - [ ] 创建 `XxxCreateDto`、`XxxUpdateDto`、`XxxQueryDto`
  - [ ] 添加相关枚举和验证特性

- [ ] **Client层**
  - [ ] 创建 `IXxxApi` Refit接口
  - [ ] 创建 `IXxxInfoService` 和 `XxxInfoService`
  - [ ] 创建 `XxxManagementViewModel`
  - [ ] 创建 `XxxManagementView.xaml`
  - [ ] 更新模块注册和DI配置

### 代码质量检查
- [ ] **数据流检查**
  - [ ] 确认无Info模型残留
  - [ ] 验证DTO直接绑定到UI
  - [ ] 检查映射配置唯一性

- [ ] **命名规范检查**
  - [ ] Server层使用 `XxxModelService`
  - [ ] Client层使用 `XxxInfoService`
  - [ ] DTO命名符合约定

- [ ] **性能检查**
  - [ ] 避免重复映射
  - [ ] 合理使用缓存
  - [ ] 异步操作正确实现

---

## 📊 性能优化指南

### 1. 映射性能优化
```csharp
// 使用编译时映射，避免反射开销
public static class UserMapper
{
    public static UserDto ToDto(this UserModel model)
    {
        return new UserDto
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            // ... 其他属性
        };
    }
}
```

### 2. UI绑定优化
```csharp
// ViewModel中使用虚拟化集合
public class UserManagementViewModel : BaseViewModel
{
    // 对于大量数据，使用虚拟化
    private readonly VirtualizingCollection<UserDto> _users;
    
    public ICollectionView UsersView { get; }
    
    public UserManagementViewModel()
    {
        _users = new VirtualizingCollection<UserDto>(LoadUserPageAsync);
        UsersView = CollectionViewSource.GetDefaultView(_users);
    }
}
```

### 3. 缓存策略
```csharp
// Client层智能缓存
public class UserInfoService : IUserInfoService
{
    public async Task<ServiceResult<UserDto?>> GetUserByIdAsync(Guid id)
    {
        var cacheKey = $"user_{id}";
        
        // 先从缓存获取
        var cached = await _cacheService.GetAsync<UserDto>(cacheKey);
        if (cached != null)
        {
            return ServiceResult<UserDto?>.Success(cached);
        }
        
        // 缓存未命中，从API获取
        var result = await _userApi.GetByIdAsync(id);
        if (result.IsSuccessStatusCode && result.Content != null)
        {
            // 缓存结果
            await _cacheService.SetAsync(cacheKey, result.Content, TimeSpan.FromMinutes(10));
            return ServiceResult<UserDto?>.Success(result.Content);
        }
        
        return ServiceResult<UserDto?>.Failure("获取用户信息失败");
    }
}
```

---

## 🚀 迁移最佳实践

### 1. 渐进式迁移
```csharp
// 第一步：保持兼容性，同时支持Info和DTO
public class UserManagementViewModel : BaseViewModel
{
    // 新代码使用DTO
    public ObservableCollection<UserDto> Users { get; set; }
    
    // 旧代码兼容（临时保留）
    [Obsolete("Use Users property instead")]
    public ObservableCollection<UserInfo> UserInfos => 
        Users.Select(dto => _mapper.Map<UserInfo>(dto)).ToObservableCollection();
}

// 第二步：完全移除Info
// 删除UserInfo类和相关代码
```

### 2. 数据验证迁移
```csharp
// 从Info验证迁移到DTO验证
public class UserCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    // 自定义验证逻辑
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Username.Contains("admin") && !Email.EndsWith("@company.com"))
        {
            yield return new ValidationResult(
                "管理员账户必须使用公司邮箱",
                new[] { nameof(Email) });
        }
    }
}
```

---

## 📝 文档和测试标准

### 单元测试标准
```csharp
// ModelService测试
[Test]
public async Task GetPagedAsync_ShouldReturnMappedDtos_WhenCalled()
{
    // Arrange
    var models = CreateTestUserModels();
    _repository.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), 
        It.IsAny<string>(), It.IsAny<UserRole?>(), It.IsAny<bool?>()))
        .ReturnsAsync(new PagedData<UserModel> { Items = models });

    // Act
    var result = await _userModelService.GetPagedAsync(new UserQueryDto());

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(models.Count, result.Data?.Items.Count);
    
    // 验证映射正确性
    var firstDto = result.Data?.Items.First();
    var firstModel = models.First();
    Assert.Equal($"{firstModel.FirstName} {firstModel.LastName}", firstDto?.DisplayName);
}

// InfoService测试
[Test]
public async Task GetUsersAsync_ShouldReturnDtos_WhenApiCallSucceeds()
{
    // Arrange
    var expectedDtos = CreateTestUserDtos();
    var apiResponse = new ApiResponse<PagedData<UserDto>>(
        new HttpResponseMessage(HttpStatusCode.OK),
        new PagedData<UserDto> { Items = expectedDtos },
        new RefitSettings());
    
    _userApi.Setup(a => a.GetUsersAsync(It.IsAny<UserQueryDto>()))
        .ReturnsAsync(apiResponse);

    // Act
    var result = await _userInfoService.GetUsersAsync(new UserQueryDto());

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedDtos.Count, result.Data?.Items.Count);
}
```

这套v2.0开发标准将显著提升开发效率，简化架构复杂度，同时保持代码质量和可维护性。