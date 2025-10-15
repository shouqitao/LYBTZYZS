# 快速开发指南

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **最后更新**: 2025-10-15
> **维护者**: 开发团队
> **适用范围**: LYBT 项目所有开发人员
> **相关文档**: [模块文档模板](../modules/template/module-document-template.md) | [模块文档编写指南](../modules/template/module-document-writing-guide.md) | [Server端架构标准](../architecture/server-module-design-standard.md) | [Client端设计标准](../architecture/client/unified-design-standard.md)

## 📋 指南概述

本文档为 LYBT 项目开发人员提供快速开发的完整指南，充分利用 Phase 1-3 标准化成果和现有模块模板，显著提升开发效率。指南包含模块开发、代码生成、依赖注入、配置管理等关键开发环节的最佳实践和工具使用方法。

## 🎯 指南目标

### 主要目标
- **提升开发效率**: 新模块开发时间减少 30% 以上
- **保证代码质量**: 自动化质量检查，代码符合项目标准
- **降低学习成本**: 新开发人员能快速上手项目开发
- **减少技术争论**: 明确的开发规范和最佳实践指导

### 适用场景
- **新模块开发**: 创建新的业务功能模块
- **功能扩展**: 在现有模块基础上添加新功能
- **代码重构**: 优化现有代码结构和实现
- **问题解决**: 快速定位和解决开发中的技术问题

## 🚀 快速开发流程

### Phase 1: 需求分析与设计

#### 1.1 需求确认清单
```bash
□ 业务需求明确，功能边界清晰
□ 技术需求确认，性能指标明确
□ 集成需求分析，依赖关系明确
□ 用户体验要求，界面交互规范
□ 合规要求检查，医疗数据保护
□ 时间进度安排，里程碑设定
```

#### 1.2 架构设计要点
- **三层架构**: 严格按照 Server-Repository-Client 三层架构
- **依赖注入**: 使用构造函数注入，避免 Service Locator
- **接口统一**: 所有 Service 接口统一命名和返回类型
- **异步编程**: I/O 操作必须使用 async/await
- **异常处理**: 统一异常处理机制和错误返回格式

#### 1.3 数据库设计规范
- **实体命名**: 使用 PascalCase，避免缩写
- **主键设计**: 统一使用 Guid 主键
- **关系映射**: 正确配置 1:1、1:N、N:M 关系
- **索引优化**: 为查询字段添加适当索引
- **审计字段**: 统一使用 BaseEntity 的审计字段

### Phase 2: 模块快速创建

#### 2.1 使用模块模板
1. **复制模板目录**:
   ```bash
   # 复制模板到新模块目录
   cp -r docs/modules/template docs/modules/[ModuleName]
   ```

2. **更新模板内容**:
   - 替换 `[模块名称]` 为实际模块名称
   - 更新版本信息和维护者
   - 根据实际功能调整文档结构

3. **参考现有模块**:
   - `docs/modules/patients/README.md` - 患者管理模块
   - `docs/modules/medicalcase/README.md` - 病案管理模块
   - `docs/modules/consultation/README.md` - 诊疗管理模块
   - `docs/modules/prescriptions/README.md` - 处方管理模块

#### 2.2 代码结构快速生成

##### Server 端模块结构
```
src/Server/Modules/LYBT.Module.[ModuleName]/
├── Interfaces/
│   ├── I[ModuleName]Repository.cs
│   └── I[ModuleName]Service.cs
├── Services/
│   └── [ModuleName]Service.cs
├── Repositories/
│   └── [ModuleName]Repository.cs
├── Mapping/
│   └── [ModuleName]MappingProfile.cs
├── Validators/
│   ├── [ModuleName]CreateDtoValidator.cs
│   └── [ModuleName]UpdateDtoValidator.cs
├── [ModuleName]Module.cs
└── LYBT.Module.[ModuleName].csproj
```

##### Client 端模块结构
```
src/Client/Desktop/Modules/LYBT.Desktop.[ModuleName]/
├── Interfaces/
│   └── I[ModuleName]Repository.cs
├── ViewModels/
│   ├── [ModuleName]ManagementViewModel.cs
│   ├── [ModuleName]ListViewModel.cs
│   ├── [ModuleName]DetailViewModel.cs
│   └── [ModuleName]CreateViewModel.cs
├── Views/
│   ├── [ModuleName]ManagementView.xaml
│   ├── [ModuleName]ListView.xaml
│   ├── [ModuleName]DetailView.xaml
│   └── [ModuleName]CreateView.xaml
├── Models/
│   └── [ModuleName]Item.cs
├── Repositories/
│   └── [ModuleName]Repository.cs
├── [ModuleName]Module.cs
└── LYBT.Desktop.[ModuleName].csproj
```

#### 2.3 核心代码模板

##### Service 接口模板
```csharp
public interface I[ModuleName]Service
{
    Task<ServiceResult<PagedResult<[ModuleName]Dto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<[ModuleName]Dto>> GetByIdAsync(Guid id);
    Task<ServiceResult<[ModuleName]Dto>> CreateAsync([ModuleName]CreateDto dto);
    Task<ServiceResult<[ModuleName]Dto>> UpdateAsync(Guid id, [ModuleName]UpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

##### Service 实现模板
```csharp
public class [ModuleName]Service : I[ModuleName]Service
{
    private readonly I[ModuleName]Repository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<[ModuleName]Service> _logger;

    public [ModuleName]Service(
        I[ModuleName]Repository repository,
        IMapper mapper,
        ILogger<[ModuleName]Service> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResult<[ModuleName]Dto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        try
        {
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            var dto = new PagedResult<[ModuleName]Dto>
            {
                Items = _mapper.Map<List<[ModuleName]Dto>>(pagedResult.Items),
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return ServiceResult<PagedResult<[ModuleName]Dto>>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取[模块名称]列表失败");
            return ServiceResult<PagedResult<[ModuleName]Dto>>.Failure("获取[模块名称]列表失败");
        }
    }

    // 其他方法实现...
}
```

##### Repository 模板
```csharp
public class [ModuleName]Repository : BaseRepository<[ModuleName]Entity>, I[ModuleName]Repository
{
    public [ModuleName]Repository(ApplicationDbContext dbContext, ILogger<[ModuleName]Repository> logger)
        : base(dbContext, logger)
    {
    }

    public async Task<PagedResult<[ModuleName]Entity>> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        var query = DbSet.AsQueryable();
        
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(e => e.Name.Contains(keyword));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<[ModuleName]Entity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    // 其他方法实现...
}
```

### Phase 3: 依赖注入配置

#### 3.1 Server 端 DI 配置
```csharp
// 在 [ModuleName]Module.cs 中
public static class [ModuleName]Module
{
    public static IServiceCollection Add[ModuleName]Module(this IServiceCollection services)
    {
        // 仓储层
        services.AddScoped<I[ModuleName]Repository, [ModuleName]Repository>();

        // 服务层
        services.AddScoped<I[ModuleName]Service, [ModuleName]Service>();

        // 验证器
        services.AddValidatorsFromAssemblyContaining<[ModuleName]CreateDtoValidator>();

        // AutoMapper 配置
        services.AddAutoMapper(typeof([ModuleName]MappingProfile));

        return services;
    }
}
```

#### 3.2 Client 端 DI 配置
```csharp
// 在 [ModuleName]Module.cs 中
public class [ModuleName]Module : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var container = containerProvider.GetContainer();
        
        // 注册仓储
        container.Register<I[ModuleName]Repository, [ModuleName]Repository>(Lifetime.Singleton);
        
        // 注册 ViewModel
        container.Register<[ModuleName]ManagementViewModel>(Lifetime.Singleton);
        container.Register<[ModuleName]ListViewModel>(Lifetime.Singleton);
        container.Register<[ModuleName]DetailViewModel>(Lifetime.Singleton);
        container.Register<[ModuleName]CreateViewModel>(Lifetime.Singleton);
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册视图
        containerRegistry.RegisterForNavigation<[ModuleName]ManagementView>();
        containerRegistry.RegisterForNavigation<[ModuleName]ListView>();
        containerRegistry.RegisterForNavigation<[ModuleName]DetailView>();
        containerRegistry.RegisterForNavigation<[ModuleName]CreateView>();
    }
}
```

#### 3.3 AutoMapper 配置模板
```csharp
public class [ModuleName]MappingProfile : Profile
{
    public [ModuleName]MappingProfile()
    {
        // Entity 到 DTO 映射
        CreateMap<[ModuleName]Entity, [ModuleName]Dto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // CreateDto 到 Entity 映射
        CreateMap<[ModuleName]CreateDto, [ModuleName]Entity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // UpdateDto 到 Entity 映射
        CreateMap<[ModuleName]UpdateDto, [ModuleName]Entity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now));
    }
}
```

### Phase 4: 控制器和 API 开发

#### 4.1 控制器模板
```csharp
[ApiController]
[Route("api/[controller]")]
public class [ModuleName]Controller : BaseApiController
{
    private readonly I[ModuleName]Service _moduleNameService;

    public [ModuleName]Controller(I[ModuleName]Service moduleNameService)
    {
        _moduleNameService = moduleNameService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<PagedResult<[ModuleName]Dto>>>> GetPaged([FromQuery] [ModuleName]QueryDto query)
    {
        var result = await _moduleNameService.GetPagedAsync(query.PageNumber, query.PageSize, query.Keyword);
        return Ok(ApiResult<PagedResult<[ModuleName]Dto>>.Success(result.Data));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<[ModuleName]Dto>>> GetById(Guid id)
    {
        var result = await _moduleNameService.GetByIdAsync(id);
        return result.IsSuccess ? Ok(ApiResult<[ModuleName]Dto>.Success(result.Data)) : BadRequest(ApiResult<[ModuleName]Dto>.Failure(result.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<[ModuleName]Dto>>> Create([FromBody] [ModuleName]CreateDto dto)
    {
        var result = await _moduleNameService.CreateAsync(dto);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, ApiResult<[ModuleName]Dto>.Success(result.Data)) : BadRequest(ApiResult<[ModuleName]Dto>.Failure(result.Message));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResult<[ModuleName]Dto>>> Update(Guid id, [FromBody] [ModuleName]UpdateDto dto)
    {
        var result = await _moduleNameService.UpdateAsync(id, dto);
        return result.IsSuccess ? Ok(ApiResult<[ModuleName]Dto>.Success(result.Data)) : BadRequest(ApiResult<[ModuleName]Dto>.Failure(result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResult>> Delete(Guid id)
    {
        var result = await _moduleNameService.DeleteAsync(id);
        return result.IsSuccess ? Ok(ApiResult.Success()) : BadRequest(ApiResult.Failure(result.Message));
    }
}
```

#### 4.2 DTO 设计模板
```csharp
// 基础 DTO
public class [ModuleName]Dto : StatusDto, IRemarkable
{
    [DisplayName("名称")]
    [Required]
    [StringLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("描述")]
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }

    [DisplayName("备注")]
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Remark { get; set; }
}

// Create DTO
public class [ModuleName]CreateDto
{
    [DisplayName("名称")]
    [Required(ErrorMessage = "名称不能为空")]
    [StringLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("描述")]
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }
}

// Update DTO
public class [ModuleName]UpdateDto : IIdentifiable<Guid>, IRemarkable
{
    [Required(ErrorMessage = "ID不能为空")]
    public Guid Id { get; set; }

    [DisplayName("名称")]
    [StringLength(100, ErrorMessage = "名称长度不能超过100个字符")]
    public string? Name { get; set; }

    [DisplayName("描述")]
    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }
}
```

#### 4.3 验证器模板
```csharp
public class [ModuleName]CreateDtoValidator : AbstractValidator<[ModuleName]CreateDto>
{
    public [ModuleName]CreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("名称不能为空")
            .MaximumLength(100).WithMessage("名称长度不能超过100个字符");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述长度不能超过500个字符");

        // 自定义验证规则
        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) =>
            {
                // 检查名称是否重复
                return !await IsNameExistsAsync(name);
            })
            .WithMessage("名称已存在");
    }

    private async Task<bool> IsNameExistsAsync(string name)
    {
        // 实现名称重复检查逻辑
        return await Task.FromResult(false);
    }
}
```

### Phase 5: Client 端开发

#### 5.1 ViewModel 模板
```csharp
public class [ModuleName]ManagementViewModel : UnifiedViewModelBase
{
    private readonly I[ModuleName]Repository _moduleNameRepository;

    #region 属性
    private ObservableCollection<[ModuleName]Dto> _items;
    public ObservableCollection<[ModuleName]Dto> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    private [ModuleName]Dto? _selectedItem;
    public [ModuleName]Dto? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }
    #endregion

    #region 命令
    public DelegateCommand LoadItemsCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    #endregion

    #region 构造函数
    public [ModuleName]ManagementViewModel(
        I[ModuleName]Repository moduleNameRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _moduleNameRepository = moduleNameRepository ?? throw new ArgumentNullException(nameof(moduleNameRepository));

        // 初始化命令
        LoadItemsCommand = new DelegateCommand(async () => await LoadItemsAsync());
        SearchCommand = new DelegateCommand(async () => await SearchItemsAsync());
        CreateCommand = new DelegateCommand(Create);
        EditCommand = new DelegateCommand<object>(ExecuteEdit);
        DeleteCommand = new DelegateCommand<object>(async (item) => await ExecuteDeleteAsync(item));
        RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
    }
    #endregion

    #region 命令实现
    private async Task LoadItemsAsync()
    {
        try
        {
            SetIsBusy(true, "正在加载数据...");

            var result = await _moduleNameRepository.GetPagedAsync(1, 50);
            if (result.IsSuccess)
            {
                Items = new ObservableCollection<[ModuleName]Dto>(result.Data.Items);
            }
            else
            {
                ShowErrorMessage(result.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载数据失败");
            ShowErrorMessage("加载数据失败");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private async Task SearchItemsAsync()
    {
        try
        {
            SetIsBusy(true, "正在搜索...");

            var result = await _moduleNameRepository.SearchAsync(SearchKeyword);
            if (result.IsSuccess)
            {
                Items = new ObservableCollection<[ModuleName]Dto>(result.Data);
            }
            else
            {
                ShowErrorMessage(result.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "搜索失败");
            ShowErrorMessage("搜索失败");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private void Create()
    {
        NavigateTo("MainRegion", "[ModuleName]CreateView");
    }

    private void ExecuteEdit(object item)
    {
        if (item is [ModuleName]Dto dto)
        {
            var parameters = new NavigationParameters
            {
                { "Id", dto.Id },
                { "IsReadOnly", false }
            };
            NavigateTo("MainRegion", "[ModuleName]DetailView", parameters);
        }
    }

    private async Task ExecuteDeleteAsync(object item)
    {
        if (item is [ModuleName]Dto dto)
        {
            var confirmed = await ShowConfirmDialogAsync("确认删除", $"确定要删除 '{dto.Name}' 吗？");
            if (confirmed)
            {
                try
                {
                    SetIsBusy(true, "正在删除...");

                    var result = await _moduleNameRepository.DeleteAsync(dto.Id);
                    if (result.IsSuccess)
                    {
                        await ShowSuccessMessageAsync("删除成功");
                        await LoadItemsAsync();
                    }
                    else
                    {
                        ShowErrorMessage(result.Message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "删除失败");
                    ShowErrorMessage("删除失败");
                }
                finally
                {
                    SetIsBusy(false);
                }
            }
        }
    }

    private async Task RefreshAsync()
    {
        await LoadItemsAsync();
        await ShowSuccessMessageAsync("刷新成功");
    }
    #endregion

    #region 生命周期
    protected override async Task InitializeAsync(NavigationParameters parameters)
    {
        await base.InitializeAsync(parameters);
        await LoadItemsAsync();
    }
    #endregion
}
```

#### 5.2 Repository 模板
```csharp
public class [ModuleName]Repository : RepositoryBase<[ModuleName]Dto, [ModuleName]CreateDto, [ModuleName]UpdateDto, I[ModuleName]Api>, I[ModuleName]Repository
{
    public [ModuleName]Repository(I[ModuleName]Api api, IMapper mapper, ILogger<[ModuleName]Repository> logger)
        : base(api, mapper, logger)
    {
    }

    public async Task<ServiceResult<List<[ModuleName]Dto>>> SearchAsync(string keyword)
    {
        try
        {
            var result = await Api.SearchAsync(new SearchRequest { Keyword = keyword });
            return ServiceResult<List<[ModuleName]Dto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索失败");
            return ServiceResult<List<[ModuleName]Dto>>.Failure("搜索失败");
        }
    }
}
```

## 🔧 开发工具和最佳实践

### 代码生成工具

#### 1. 使用代码片段 (Code Snippets)
```json
{
    "Service Interface": {
        "prefix": "service-interface",
        "body": [
            "public interface I$1Service",
            "{",
            "    Task<ServiceResult<PagedResult<$1Dto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);",
            "    Task<ServiceResult<$1Dto>> GetByIdAsync(Guid id);",
            "    Task<ServiceResult<$1Dto>> CreateAsync($1CreateDto dto);",
            "    Task<ServiceResult<$1Dto>> UpdateAsync(Guid id, $1UpdateDto dto);",
            "    Task<ServiceResult> DeleteAsync(Guid id);",
            "}"
        ],
        "description": "创建服务接口代码片段"
    }
}
```

#### 2. 使用模板引擎
- **Scaffold-DbContext**: 根据数据库生成实体和上下文
- **dotnet new**: 使用项目模板创建新项目
- **T4 Templates**: 自定义代码生成模板

### 开发环境配置

#### 1. VS Code 扩展推荐
```json
{
    "recommendations": [
        "ms-dotnettools.csharp",
        "ms-azuretools.vscode-docker",
        "ms-vscode.vscode-typescript-next",
        "formulahendry.auto-rename-tag",
        "christian-kohler.path-intellisense",
        "ms-vscode.hexeditor",
        "streetsidesoftware.code-spell-checker"
    ]
}
```

#### 2. 项目设置文件 (.vscode/settings.json)
```json
{
    "editor.formatOnSave": true,
    "editor.insertSpaces": true,
    "editor.tabSize": 4,
    "editor.detectIndentation": false,
    "files.trimTrailingWhitespace": true,
    "files.insertFinalNewline": true,
    "omnisharp.enableRoslynAnalyzers": true,
    "omnisharp.enableEditorConfigSupport": true,
    "omnisharp.enableImportCompletion": true,
    "csharp.semanticHighlighting.enabled": true,
    "csharp.semanticHighlighting.generatedCode": true
}
```

#### 3. 调试配置 (.vscode/launch.json)
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/WebAPI.LYBT/bin/Debug/net8.0/LYBT.WebAPI.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/WebAPI.LYBT",
            "stopAtEntry": false,
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            }
        }
    ]
}
```

### 代码质量检查

#### 1. EditorConfig 配置 (.editorconfig)
```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,vb}]
indent_size = 4
insert_final_newline = false

[*.{json,xml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

#### 2. StyleCop 配置
```xml
<RuleSet Name="LYBTStyleRules" Description="LYBT 项目代码风格规则">
    <Rules>
        <!-- 命名规则 -->
        <Rule Identifier="NamingRules">
            <Properties>
                <Property Name="AllowedHungarianPrefixes" Value="" />
                <Property Name="InheritDocIfItMust" Value="true" />
            </Properties>
        </Rule>
        
        <!-- 文档规则 -->
        <Rule Identifier="DocumentationRules">
            <Properties>
                <Property Name="CompanyFileHeader" Value="false" />
                <Property Name="CopyrightFileHeader" Value="false" />
            </Properties>
        </Rule>
        
        <!-- 布局规则 -->
        <Rule Identifier="LayoutRules">
            <Properties>
                <Property Name="NamespaceIndentation" Value="inner" />
                <Property Name="Indentation" Value="spaces" />
                <Property Name="IndentationSize" Value="4" />
            </Properties>
        </Rule>
    </Rules>
</RuleSet>
```

## 🚀 快速开发检查清单

### 开发前检查
```bash
□ 需求分析完成，功能边界清晰
□ 架构设计确认，符合项目标准
□ 技术选型确定，无技术黑名单项目
□ 依赖关系分析，集成方案明确
□ 开发环境配置，工具安装完成
□ 代码模板准备，参考模块确认
```

### 开发过程检查
```bash
□ 代码结构符合三层架构标准
□ 接口命名统一，返回类型一致
□ 依赖注入正确，无 Service Locator
□ 异步编程规范，I/O 操作异步
□ 异常处理完整，错误返回统一
□ 单元测试覆盖，核心逻辑测试
□ 代码注释完整，关键逻辑说明
```

### 开发完成后检查
```bash
□ 功能测试完成，业务流程验证
□ 集成测试通过，模块间协作正常
□ 性能测试达标，响应时间符合要求
□ 安全测试通过，权限控制正确
□ 文档编写完成，符合模板要求
□ 代码审查通过，质量检查通过
□ 部署测试成功，环境配置正确
```

## 📚 参考资料

### 相关文档
- [模块文档模板](../modules/template/module-document-template.md)
- [模块文档编写指南](../modules/template/module-document-writing-guide.md)
- [Server端架构标准](../architecture/server-module-design-standard.md)
- [Client端设计标准](../architecture/client/unified-design-standard.md)
- [依赖注入配置指南](repository-dependency-injection-guide.md)

### 技术文档
- [.NET 架构模式](https://docs.microsoft.com/en-us/dotnet/standard/modern-web-apps-azure-architecture/)
- [ASP.NET Core 最佳实践](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/)
- [Entity Framework Core 指南](https://docs.microsoft.com/en-us/ef/core/)
- [AutoMapper 文档](https://automapper.readthedocs.io/)

### 工具文档
- [Visual Studio Code 文档](https://code.visualstudio.com/docs)
- [.NET CLI 文档](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Git 使用指南](https://git-scm.com/doc)

## 📞 技术支持

### 开发团队联系方式
- **架构支持**: 项目架构师
- **技术支持**: 高级开发工程师
- **代码审查**: 技术负责人
- **工具支持**: DevOps 工程师

### 问题解决流程
1. **查阅文档**: 首先查阅相关技术文档和指南
2. **团队讨论**: 在团队群组中讨论技术问题
3. **代码审查**: 提交 PR 进行代码审查
4. **专家咨询**: 联系相关技术专家

---

*本文档遵循 LYBT 项目文档标准编写，如有疑问请参考相关模板或联系技术支持团队。*