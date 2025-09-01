# LYBT.Desktop.Patients - 患者档案管理模块

## 📋 项目概览

**项目名称**: LYBT.Desktop.Patients  
**项目类型**: WPF 模块化业务组件  
**技术栈**: .NET 8.0, WPF, Prism.DryIoc 9.0.537  
**架构模式**: MVVM + Prism 模块化架构 + 业务协调器模式  
**业务职责**: 患者档案管理、基础信息维护、患者搜索查询、数据导入导出

### 核心功能

1. **患者档案管理** - 创建、编辑、查看、删除患者基础信息
2. **患者搜索查询** - 按姓名、手机号、身份证号快速搜索
3. **分页数据管理** - 大量患者数据的分页展示和管理
4. **数据验证** - 手机号、身份证号重复性检查和格式验证
5. **数据导入导出** - Excel批量导入患者数据和导出功能
6. **患者状态管理** - 启用/禁用患者档案状态
7. **业务协调** - PatientCoordinator统一协调患者相关业务

### 依赖关系

- **Desktop.Core** - 基础控件和设计系统
- **Desktop.Infrastructure** - 基础服务接口
- **Desktop.Services** - API客户端和数据服务
- **Shared.Models** - 患者相关DTO模型
- **第三方依赖**: Prism.DryIoc 9.0.537, AutoMapper, Refit

## 🏗️ 项目架构

### 目录结构

```
LYBT.Desktop.Patients/
├── Api/                              # API接口定义 (空目录，使用Services层接口)
├── Coordinators/                     # 业务协调器
│   └── PatientCoordinator.cs        # 患者业务协调器
├── Services/                         # 业务服务层
│   └── PatientModule.cs             # 核心患者业务服务
├── ViewModels/                       # MVVM视图模型
│   ├── PatientManagementViewModel.cs # 患者管理主界面视图模型
│   ├── PatientAddEditDialogViewModel.cs # 患者添加/编辑对话框视图模型
│   └── PatientDetailViewModel.cs    # 患者详情查看视图模型
├── Views/                           # WPF视图界面
│   ├── PatientManagementView.xaml  # 患者管理主界面
│   ├── PatientAddEditDialog.xaml   # 患者添加/编辑对话框
│   └── PatientDetailView.xaml      # 患者详情查看界面
├── PatientsModule.cs                # Prism模块注册
└── LYBT.Desktop.Patients.csproj
```

### 架构模式

#### 1. Prism模块化架构
```csharp
// PatientsModule.cs - 模块注册
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // UltraThink修复：模块自己注册服务接口实现
        containerRegistry.RegisterSingleton<PatientModule>();
        containerRegistry.RegisterSingleton<IPatientService>(container => container.Resolve<PatientModule>());
        
        // UltraThink P1重构：注册模块业务协调器
        containerRegistry.RegisterSingleton<PatientCoordinator>();
        
        // 注册视图和视图模型
        containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
        containerRegistry.RegisterForNavigation<PatientAddEditDialog, PatientAddEditDialogViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成后的操作
    }
}
```

#### 2. 业务协调器模式
```csharp
// PatientCoordinator.cs - 业务协调器（简化患者相关业务流程）
public class PatientCoordinator
{
    private readonly PatientModule _patientModule;
    private readonly IEventAggregator _eventAggregator;
    
    public PatientCoordinator(PatientModule patientModule, IEventAggregator eventAggregator)
    {
        _patientModule = patientModule;
        _eventAggregator = eventAggregator;
    }
    
    // 协调患者创建流程
    public async Task<ServiceResult<PatientDto>> CreatePatientWithValidationAsync(PatientCreateDto createDto)
    {
        // 1. 数据验证
        var validationResult = await _patientModule.ValidatePatientAsync(createDto);
        if (!validationResult.IsSuccess)
        {
            return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
        }
        
        // 2. 重复检查
        var duplicateResult = await _patientModule.CheckDuplicatePatientsAsync(createDto.IdNumber, createDto.PhoneNumber);
        if (duplicateResult?.Any() == true)
        {
            return ServiceResult<PatientDto>.Failure("发现重复患者信息");
        }
        
        // 3. 创建患者
        var createResult = await _patientModule.CreateAsync(createDto);
        if (createResult.IsSuccess)
        {
            // 4. 发布患者创建事件
            _eventAggregator.GetEvent<PatientCreatedEvent>().Publish(createResult.Data);
        }
        
        return createResult;
    }
}
```

#### 3. 业务服务模块架构
```csharp
// PatientModule.cs - 核心业务服务
public class PatientModule : IPatientService
{
    private readonly IPatientApi _apiService;
    private readonly IMapper _mapper;
    
    public PatientModule(IPatientApi apiService, IMapper mapper)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    // 基础CRUD操作
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
    {
        // UltraThink v2.0: 直接使用API调用获取DTOs
        var apiResponse = await _apiService.GetPatientsAsync(
            query.PageIndex,
            query.PageSize,
            query.Keyword);
            
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
        
        // UltraThink v2.0: 直接使用DTO，无需映射
        var pagedData = apiResponse.Content;
        var result = new PagedResult<PatientDto>(
            pagedData.Items.ToList(),
            pagedData.TotalCount,
            pagedData.CurrentPage,
            pagedData.PageSize);
        
        return ServiceResult<PagedResult<PatientDto>>.Success(result);
    }
}
```

## 🔧 核心组件

### 1. PatientModule (核心业务服务)

#### 主要功能
- **基础CRUD**: 创建、读取、更新、删除患者档案
- **分页查询**: 高效的分页数据加载和搜索
- **业务验证**: 数据格式验证和重复性检查
- **状态管理**: 患者档案启用/禁用管理
- **数据导入导出**: Excel批量数据处理
- **搜索功能**: 多字段快速搜索和过滤

#### 核心方法

##### 基础CRUD操作
```csharp
// 分页查询患者
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
{
    try
    {
        // UltraThink v2.0: 直接使用API调用获取DTOs
        var apiResponse = await _apiService.GetPatientsAsync(
            query.PageIndex,
            query.PageSize,
            query.Keyword);
            
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
        
        // UltraThink v2.0: 直接使用DTO，无需映射
        var pagedData = apiResponse.Content;
        var result = new PagedResult<PatientDto>(
            pagedData.Items.ToList(),
            pagedData.TotalCount,
            pagedData.CurrentPage,
            pagedData.PageSize);
        
        return ServiceResult<PagedResult<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        return ServiceResult<PagedResult<PatientDto>>.Failure($"获取患者列表异常: {ex.Message}");
    }
}

// 根据ID获取患者详情
public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult<PatientDto>.Failure("患者ID不能为空");
        }
        
        // UltraThink v2.0: API调用直接获取DTO
        var apiResponse = await _apiService.GetPatientByIdAsync(id);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
        
        // UltraThink v2.0: 直接使用统一的PatientDto，无需转换
        return ServiceResult<PatientDto>.Success(apiResponse.Content);
    }
    catch (Exception ex)
    {
        return ServiceResult<PatientDto>.Failure($"获取患者详情异常: {ex.Message}");
    }
}

// 创建新患者
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
{
    try
    {
        // UltraThink v2.0: 直接使用CreateDto进行业务验证
        var validationResult = await ValidateCreateDtoAsync(createDto);
        if (!validationResult.IsSuccess)
        {
            return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
        }
        
        // 检查电话号码是否已存在
        if (!string.IsNullOrEmpty(createDto.PhoneNumber))
        {
            var phoneExistsResult = await IsPhoneExistsAsync(createDto.PhoneNumber);
            if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
            {
                return ServiceResult<PatientDto>.Failure("该电话号码已被使用");
            }
        }
        
        // 检查身份证号是否已存在
        if (!string.IsNullOrEmpty(createDto.IdNumber))
        {
            var idCardExistsResult = await IsIdCardExistsAsync(createDto.IdNumber);
            if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
            {
                return ServiceResult<PatientDto>.Failure("该身份证号已被使用");
            }
        }
        
        // API调用
        var apiResponse = await _apiService.CreatePatientAsync(createDto);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PatientDto>.Failure("创建患者失败");
        }
        
        // UltraThink v2.0: 直接使用DTO，无需映射
        return ServiceResult<PatientDto>.Success(apiResponse.Content);
    }
    catch (Exception ex)
    {
        return ServiceResult<PatientDto>.Failure($"创建患者异常: {ex.Message}");
    }
}

// 更新患者信息
public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
{
    try
    {
        // UltraThink v2.0: 直接使用UpdateDto进行业务验证
        var validationResult = await ValidateUpdateDtoAsync(updateDto);
        if (!validationResult.IsSuccess)
        {
            return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
        }
        
        // 检查电话号码是否已被其他患者使用
        if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
        {
            var phoneExistsResult = await IsPhoneExistsAsync(updateDto.PhoneNumber, id);
            if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
            {
                return ServiceResult<PatientDto>.Failure("该电话号码已被其他患者使用");
            }
        }
        
        // 检查身份证号是否已被其他患者使用
        if (!string.IsNullOrEmpty(updateDto.IdNumber))
        {
            var idCardExistsResult = await IsIdCardExistsAsync(updateDto.IdNumber, id);
            if (idCardExistsResult.IsSuccess && idCardExistsResult.Data)
            {
                return ServiceResult<PatientDto>.Failure("该身份证号已被其他患者使用");
            }
        }
        
        // API调用
        var apiResponse = await _apiService.UpdatePatientAsync(id, updateDto);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PatientDto>.Failure("更新患者失败");
        }
        
        // UltraThink v2.0: 直接使用DTO，无需映射
        return ServiceResult<PatientDto>.Success(apiResponse.Content);
    }
    catch (Exception ex)
    {
        return ServiceResult<PatientDto>.Failure($"更新患者异常: {ex.Message}");
    }
}

// 删除患者
public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("患者ID不能为空");
        }
        
        var apiResponse = await _apiService.DeletePatientAsync(id);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<bool>.Failure("删除患者失败");
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"删除患者异常: {ex.Message}");
    }
}
```

##### 搜索查询功能
```csharp
// 搜索患者
public async Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PagedQueryBaseDto request)
{
    try
    {
        // 转换为PatientPagedQueryDto
        var patientQuery = new PatientPagedQueryDto
        {
            Keyword = request.Keyword,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            SortField = request.SortField,
            IsDescending = request.IsDescending
        };
        
        // 使用GetPagedAsync实现搜索功能
        return await GetPagedAsync(patientQuery);
    }
    catch (Exception ex)
    {
        return ServiceResult<PagedResult<PatientDto>>.Failure($"搜索患者异常: {ex.Message}");
    }
}

// 按关键字搜索患者
public async Task<ServiceResult<IEnumerable<PatientDto>>> SearchByKeywordAsync(string keyword)
{
    try
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return ServiceResult<IEnumerable<PatientDto>>.Success(new List<PatientDto>());
        }
        
        var query = new PatientPagedQueryDto
        {
            PageIndex = 1,
            PageSize = 50, // 限制搜索结果数量
            Keyword = keyword
        };
        
        var result = await GetPagedAsync(query);
        if (!result.IsSuccess)
        {
            return ServiceResult<IEnumerable<PatientDto>>.Failure(result.ErrorMessage ?? "获取数据失败");
        }
        
        return ServiceResult<IEnumerable<PatientDto>>.Success(result.Data?.Items ?? new List<PatientDto>());
    }
    catch (Exception ex)
    {
        return ServiceResult<IEnumerable<PatientDto>>.Failure($"关键字搜索患者异常: {ex.Message}");
    }
}

// 根据身份证号查找患者
public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
{
    try
    {
        if (string.IsNullOrWhiteSpace(idCard))
        {
            return ServiceResult<PatientDto>.Failure("身份证号不能为空");
        }
        
        var searchResult = await SearchByKeywordAsync(idCard);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<PatientDto>.Failure(searchResult.ErrorMessage ?? "查找患者失败");
        }
        
        var patient = searchResult.Data?.FirstOrDefault(p => p.IdNumber == idCard);
        if (patient == null)
        {
            return ServiceResult<PatientDto>.Failure("未找到匹配的患者");
        }
        
        return ServiceResult<PatientDto>.Success(patient);
    }
    catch (Exception ex)
    {
        return ServiceResult<PatientDto>.Failure($"根据身份证号查找患者异常: {ex.Message}");
    }
}

// 根据电话号码查找患者
public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
{
    try
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
        }
        
        var searchResult = await SearchByKeywordAsync(phone);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<List<PatientDto>>.Failure(searchResult.ErrorMessage ?? "查找患者失败");
        }
        
        var patients = searchResult.Data?.Where(p => p.PhoneNumber == phone).ToList() ?? new List<PatientDto>();
        return ServiceResult<List<PatientDto>>.Success(patients);
    }
    catch (Exception ex)
    {
        return ServiceResult<List<PatientDto>>.Failure($"根据电话号码查找患者异常: {ex.Message}");
    }
}
```

##### 数据验证
```csharp
// 创建DTO验证
private Task<ServiceResult> ValidateCreateDtoAsync(PatientCreateDto createDto)
{
    if (createDto == null) return Task.FromResult(ServiceResult.Failure("创建患者信息不能为空"));
    if (string.IsNullOrWhiteSpace(createDto.Name)) return Task.FromResult(ServiceResult.Failure("患者姓名不能为空"));
    if (createDto.Name.Length > 50) return Task.FromResult(ServiceResult.Failure("患者姓名长度不能超过50个字符"));
    
    // UltraThink v2.0: Age是计算属性，不验证存储值
    // 验证出生日期的合理性
    if (createDto.BirthDate.HasValue && createDto.BirthDate.Value > DateTime.Today)
    {
        return Task.FromResult(ServiceResult.Failure("出生日期不能晚于今天"));
    }
    
    return Task.FromResult(ServiceResult.Success());
}

// 更新DTO验证
private Task<ServiceResult> ValidateUpdateDtoAsync(PatientUpdateDto updateDto)
{
    if (updateDto == null) return Task.FromResult(ServiceResult.Failure("更新患者信息不能为空"));
    if (string.IsNullOrWhiteSpace(updateDto.Name)) return Task.FromResult(ServiceResult.Failure("患者姓名不能为空"));
    if (updateDto.Name.Length > 50) return Task.FromResult(ServiceResult.Failure("患者姓名长度不能超过50个字符"));
    
    // UltraThink v2.0: Age是计算属性，不验证存储值
    // 验证出生日期的合理性
    if (updateDto.BirthDate.HasValue && updateDto.BirthDate.Value > DateTime.Today)
    {
        return Task.FromResult(ServiceResult.Failure("出生日期不能晚于今天"));
    }
    
    return Task.FromResult(ServiceResult.Success());
}

// 检查手机号是否存在
public async Task<ServiceResult<bool>> IsPhoneExistsAsync(string phone, Guid? excludeId = null)
{
    try
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ServiceResult<bool>.Success(false);
        }
        
        // 这里应该调用API检查电话号码是否存在
        // 目前模拟实现，实际应该有专门的API
        var searchResult = await SearchByKeywordAsync(phone);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(searchResult.ErrorMessage ?? "检查电话号码失败");
        }
        
        var exists = searchResult.Data?.Any(p => 
            p.PhoneNumber == phone && 
            (excludeId == null || p.Id != excludeId.Value)) ?? false;
        
        return ServiceResult<bool>.Success(exists);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"检查电话号码异常: {ex.Message}");
    }
}

// 检查身份证号是否存在
public async Task<ServiceResult<bool>> IsIdCardExistsAsync(string idCard, Guid? excludeId = null)
{
    try
    {
        if (string.IsNullOrWhiteSpace(idCard))
        {
            return ServiceResult<bool>.Success(false);
        }
        
        // 这里应该调用API检查身份证号是否存在
        // 目前模拟实现，实际应该有专门的API
        var searchResult = await SearchByKeywordAsync(idCard);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(searchResult.ErrorMessage ?? "检查身份证号失败");
        }
        
        var exists = searchResult.Data?.Any(p => 
            p.IdNumber == idCard && 
            (excludeId == null || p.Id != excludeId.Value)) ?? false;
        
        return ServiceResult<bool>.Success(exists);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"检查身份证号异常: {ex.Message}");
    }
}

// 检查重复患者
public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
{
    try
    {
        var duplicates = new List<PatientDto>();
        
        if (!string.IsNullOrEmpty(idNumber))
        {
            var idResult = await GetByIdCardAsync(idNumber);
            if (idResult.IsSuccess && idResult.Data != null)
            {
                duplicates.Add(idResult.Data);
            }
        }
        
        if (!string.IsNullOrEmpty(phoneNumber))
        {
            var phoneResult = await GetByPhoneAsync(phoneNumber);
            if (phoneResult.IsSuccess && phoneResult.Data != null)
            {
                duplicates.AddRange(phoneResult.Data);
            }
        }
        
        return duplicates.Distinct().ToList();
    }
    catch
    {
        return new List<PatientDto>();
    }
}
```

##### 状态管理
```csharp
// 启用患者
public async Task<ServiceResult> EnableAsync(Guid id)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult.Failure("患者ID不能为空");
        }
        
        // 调用API的启用接口
        var apiResponse = await _apiService.ToggleStatusAsync(id);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult.Failure("启用患者失败");
        }
        
        return ServiceResult.Success();
    }
    catch (Exception ex)
    {
        return ServiceResult.Failure($"启用患者异常: {ex.Message}");
    }
}

// 禁用患者
public async Task<ServiceResult> DisableAsync(Guid id)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult.Failure("患者ID不能为空");
        }
        
        // 调用API的禁用接口
        var apiResponse = await _apiService.ToggleStatusAsync(id);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult.Failure("禁用患者失败");
        }
        
        return ServiceResult.Success();
    }
    catch (Exception ex)
    {
        return ServiceResult.Failure($"禁用患者异常: {ex.Message}");
    }
}

// 设置患者状态（启用/禁用）
public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
{
    try
    {
        var result = isActive ? await EnableAsync(id) : await DisableAsync(id);
        return result.IsSuccess;
    }
    catch
    {
        return false;
    }
}
```

##### 数据导入导出
```csharp
// 批量导入患者数据
public async Task<ServiceResult<int>> ImportPatientsAsync(List<PatientImportDto> patients)
{
    try
    {
        if (patients == null || !patients.Any())
        {
            return ServiceResult<int>.Failure("导入患者列表不能为空");
        }

        // API调用批量导入
        var apiResponse = await _apiService.ImportPatientsAsync(patients);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<int>.Failure("批量导入患者失败");
        }

        return ServiceResult<int>.Success(apiResponse.Content);
    }
    catch (Exception ex)
    {
        return ServiceResult<int>.Failure($"批量导入患者异常: {ex.Message}");
    }
}

// 导出患者数据
public async Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync()
{
    try
    {
        // API调用导出
        var apiResponse = await _apiService.ExportPatientsAsync();
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<List<PatientDto>>.Failure("导出患者数据失败");
        }

        return ServiceResult<List<PatientDto>>.Success(apiResponse.Content.ToList());
    }
    catch (Exception ex)
    {
        return ServiceResult<List<PatientDto>>.Failure($"导出患者数据异常: {ex.Message}");
    }
}

// 获取患者导入模板
public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
{
    try
    {
        // API调用获取导入模板
        var apiResponse = await _apiService.GetImportTemplateAsync();
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<byte[]>.Failure("获取患者导入模板失败");
        }

        return ServiceResult<byte[]>.Success(apiResponse.Content);
    }
    catch (Exception ex)
    {
        return ServiceResult<byte[]>.Failure($"获取患者导入模板异常: {ex.Message}");
    }
}
```

### 2. PatientCoordinator (业务协调器)

#### 主要功能
- **流程协调**: 统一患者相关业务流程
- **事件发布**: 患者创建、更新、删除事件通知
- **业务整合**: 整合多个服务完成复杂业务

#### 核心方法
```csharp
public class PatientCoordinator
{
    private readonly PatientModule _patientModule;
    private readonly IEventAggregator _eventAggregator;
    
    public PatientCoordinator(PatientModule patientModule, IEventAggregator eventAggregator)
    {
        _patientModule = patientModule;
        _eventAggregator = eventAggregator;
    }
    
    // 协调患者创建流程
    public async Task<ServiceResult<PatientDto>> CreatePatientWithValidationAsync(PatientCreateDto createDto)
    {
        try
        {
            // 1. 数据验证
            var validationResult = await _patientModule.ValidatePatientAsync(createDto);
            if (!validationResult.IsSuccess)
            {
                return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
            }
            
            // 2. 重复检查
            var duplicatePatients = await _patientModule.CheckDuplicatePatientsAsync(createDto.IdNumber, createDto.PhoneNumber);
            if (duplicatePatients?.Any() == true)
            {
                var duplicateNames = string.Join(", ", duplicatePatients.Select(p => p.Name));
                return ServiceResult<PatientDto>.Failure($"发现重复患者信息: {duplicateNames}");
            }
            
            // 3. 创建患者
            var createResult = await _patientModule.CreateAsync(createDto);
            if (createResult.IsSuccess)
            {
                // 4. 发布患者创建事件
                _eventAggregator.GetEvent<PatientCreatedEvent>().Publish(new PatientCreatedEventArgs
                {
                    PatientId = createResult.Data.Id,
                    PatientName = createResult.Data.Name,
                    CreatedTime = DateTime.Now
                });
            }
            
            return createResult;
        }
        catch (Exception ex)
        {
            return ServiceResult<PatientDto>.Failure($"创建患者流程异常: {ex.Message}");
        }
    }
    
    // 协调患者更新流程
    public async Task<ServiceResult<PatientDto>> UpdatePatientWithValidationAsync(Guid id, PatientUpdateDto updateDto)
    {
        try
        {
            // 1. 获取原始患者信息
            var existingPatientResult = await _patientModule.GetByIdAsync(id);
            if (!existingPatientResult.IsSuccess)
            {
                return ServiceResult<PatientDto>.Failure("患者不存在");
            }
            
            // 2. 数据验证
            var validationResult = await _patientModule.ValidatePatientAsync(new PatientCreateDto
            {
                Name = updateDto.Name,
                Gender = updateDto.Gender,
                BirthDate = updateDto.BirthDate,
                PhoneNumber = updateDto.PhoneNumber,
                IdNumber = updateDto.IdNumber,
                Address = updateDto.Address,
                EmergencyContact = updateDto.EmergencyContact,
                EmergencyPhone = updateDto.EmergencyPhone,
                AllergyHistory = updateDto.AllergyHistory
            });
            
            if (!validationResult.IsSuccess)
            {
                return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);
            }
            
            // 3. 更新患者
            var updateResult = await _patientModule.UpdateAsync(id, updateDto);
            if (updateResult.IsSuccess)
            {
                // 4. 发布患者更新事件
                _eventAggregator.GetEvent<PatientUpdatedEvent>().Publish(new PatientUpdatedEventArgs
                {
                    PatientId = id,
                    PatientName = updateResult.Data.Name,
                    UpdatedTime = DateTime.Now,
                    Changes = ComparePatientChanges(existingPatientResult.Data, updateResult.Data)
                });
            }
            
            return updateResult;
        }
        catch (Exception ex)
        {
            return ServiceResult<PatientDto>.Failure($"更新患者流程异常: {ex.Message}");
        }
    }
    
    // 比较患者变更
    private Dictionary<string, (object OldValue, object NewValue)> ComparePatientChanges(PatientDto oldPatient, PatientDto newPatient)
    {
        var changes = new Dictionary<string, (object, object)>();
        
        if (oldPatient.Name != newPatient.Name)
            changes["姓名"] = (oldPatient.Name, newPatient.Name);
            
        if (oldPatient.PhoneNumber != newPatient.PhoneNumber)
            changes["手机号"] = (oldPatient.PhoneNumber ?? "", newPatient.PhoneNumber ?? "");
            
        if (oldPatient.Address != newPatient.Address)
            changes["地址"] = (oldPatient.Address ?? "", newPatient.Address ?? "");
        
        return changes;
    }
}
```

### 3. PatientManagementViewModel (患者管理主界面)

#### 主要功能
- **患者列表管理**: 分页显示、搜索筛选、刷新加载
- **患者操作**: 添加、编辑、删除、查看详情
- **状态管理**: 启用/禁用患者档案
- **数据导入导出**: 批量导入、导出患者数据

#### 核心属性
```csharp
public class PatientManagementViewModel : ViewModelBase
{
    // 依赖服务
    private readonly PatientModule _patientModule;
    private readonly PatientCoordinator _patientCoordinator;
    private readonly IDialogService _dialogService;
    
    // 数据绑定属性
    public ObservableCollection<PatientDto> Patients { get; set; } = new();
    public PatientDto SelectedPatient { get; set; }
    public List<PatientDto> SelectedPatients { get; set; } = new();
    public string SearchKeyword { get; set; } = string.Empty;
    
    // 分页属性
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // UI状态属性
    public bool IsLoading { get; set; }
    public bool HasPatients => Patients?.Any() == true;
    public bool CanEdit => SelectedPatient != null;
    public bool CanDelete => SelectedPatient != null;
    
    // 命令
    public DelegateCommand LoadPatientsCommand { get; }
    public DelegateCommand AddPatientCommand { get; }
    public DelegateCommand<PatientDto> EditPatientCommand { get; }
    public DelegateCommand<PatientDto> ViewPatientCommand { get; }
    public DelegateCommand<PatientDto> DeletePatientCommand { get; }
    public DelegateCommand<PatientDto> ToggleStatusCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand ImportPatientsCommand { get; }
    public DelegateCommand ExportPatientsCommand { get; }
    
    // 分页命令
    public DelegateCommand FirstPageCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand LastPageCommand { get; }
}
```

#### 核心方法
```csharp
// 加载患者列表
private async Task LoadPatientsAsync()
{
    try
    {
        IsLoading = true;
        
        var query = new PatientPagedQueryDto
        {
            PageIndex = CurrentPage,
            PageSize = PageSize,
            Keyword = SearchKeyword?.Trim()
        };
        
        var result = await _patientModule.GetPagedAsync(query);
        
        if (result.IsSuccess && result.Data != null)
        {
            Patients.Clear();
            foreach (var patient in result.Data.Items)
            {
                Patients.Add(patient);
            }
            
            TotalCount = result.Data.TotalCount;
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(HasPatients));
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "加载患者列表失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"加载患者列表异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsLoading = false;
    }
}

// 添加患者
private void AddPatient()
{
    var dialogParameters = new DialogParameters();
    
    _dialogService.ShowDialog(nameof(PatientAddEditDialog), dialogParameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            // 刷新患者列表
            LoadPatientsCommand.Execute();
        }
    });
}

// 编辑患者
private void EditPatient(PatientDto patient)
{
    if (patient == null) return;
    
    var dialogParameters = new DialogParameters
    {
        { "Patient", patient },
        { "IsEditMode", true }
    };
    
    _dialogService.ShowDialog(nameof(PatientAddEditDialog), dialogParameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            // 刷新患者列表
            LoadPatientsCommand.Execute();
        }
    });
}

// 删除患者
private async void DeletePatient(PatientDto patient)
{
    if (patient == null) return;
    
    // 确认删除
    var confirmResult = MessageBox.Show(
        $"确定要删除患者 '{patient.Name}' 的档案吗？\n\n注意：删除后患者的所有相关记录都将无法访问。",
        "确认删除",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);
    
    if (confirmResult != MessageBoxResult.Yes) return;
    
    try
    {
        var result = await _patientModule.DeleteAsync(patient.Id);
        
        if (result.IsSuccess)
        {
            MessageBox.Show("患者删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPatientsCommand.Execute();
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "删除患者失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"删除患者异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

// 导入患者数据
private async void ImportPatients()
{
    try
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择患者数据文件",
            Filter = "Excel文件 (*.xlsx)|*.xlsx|CSV文件 (*.csv)|*.csv",
            Multiselect = false
        };
        
        if (openFileDialog.ShowDialog() != true) return;
        
        // 这里应该解析文件并转换为PatientImportDto列表
        var importData = await ParseImportFileAsync(openFileDialog.FileName);
        
        if (importData?.Any() != true)
        {
            MessageBox.Show("文件中没有找到有效的患者数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = await _patientModule.ImportPatientsAsync(importData);
        
        if (result.IsSuccess)
        {
            MessageBox.Show($"成功导入 {result.Data} 条患者记录", "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPatientsCommand.Execute();
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "导入患者数据失败", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"导入患者数据异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

// 导出患者数据
private async void ExportPatients()
{
    try
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "导出患者数据",
            Filter = "Excel文件 (*.xlsx)|*.xlsx",
            DefaultExt = "xlsx",
            FileName = $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        
        if (saveFileDialog.ShowDialog() != true) return;
        
        IsLoading = true;
        
        var result = await _patientModule.ExportPatientsAsync();
        
        if (result.IsSuccess && result.Data?.Any() == true)
        {
            // 将数据写入Excel文件
            await WriteToExcelFileAsync(saveFileDialog.FileName, result.Data);
            
            MessageBox.Show($"成功导出 {result.Data.Count} 条患者记录", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 询问是否打开文件
            var openResult = MessageBox.Show("是否立即打开导出的文件？", "导出完成", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (openResult == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = saveFileDialog.FileName,
                    UseShellExecute = true
                });
            }
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "导出患者数据失败", "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"导出患者数据异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsLoading = false;
    }
}
```

### 4. PatientAddEditDialogViewModel (患者添加/编辑对话框)

#### 主要功能
- **患者信息编辑**: 创建/编辑患者基本信息
- **实时验证**: 数据输入验证和重复性检查
- **年龄计算**: 根据出生日期自动计算年龄
- **性别选择**: 性别下拉选择控件

#### 核心属性
```csharp
public class PatientAddEditDialogViewModel : IDialogAware
{
    // 依赖服务
    private readonly PatientModule _patientModule;
    private readonly PatientCoordinator _patientCoordinator;
    
    // 数据绑定属性
    public PatientCreateDto PatientData { get; set; } = new();
    public List<object> AvailableGenders { get; set; } = new();
    public object SelectedGender { get; set; }
    
    // UI状态属性
    public bool IsEditMode { get; set; }
    public bool IsSaving { get; set; }
    public string DialogTitle => IsEditMode ? "编辑患者" : "添加患者";
    public string SaveButtonText => IsEditMode ? "保存" : "创建";
    public int CalculatedAge => PatientData.BirthDate?.CalculateAge() ?? 0;
    
    // 验证属性
    public string NameError { get; set; }
    public string PhoneError { get; set; }
    public string IdNumberError { get; set; }
    public string BirthDateError { get; set; }
    
    // 命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ValidatePhoneCommand { get; }
    public DelegateCommand ValidateIdNumberCommand { get; }
}
```

#### 核心方法
```csharp
// 保存患者
private async void SavePatient()
{
    try
    {
        IsSaving = true;
        
        // 客户端验证
        if (!ValidateInput())
        {
            return;
        }
        
        ServiceResult<PatientDto> result;
        
        if (IsEditMode)
        {
            // 编辑模式：使用协调器更新
            var updateDto = new PatientUpdateDto
            {
                Name = PatientData.Name,
                Gender = PatientData.Gender,
                BirthDate = PatientData.BirthDate,
                PhoneNumber = PatientData.PhoneNumber,
                IdNumber = PatientData.IdNumber,
                Address = PatientData.Address,
                EmergencyContact = PatientData.EmergencyContact,
                EmergencyPhone = PatientData.EmergencyPhone,
                AllergyHistory = PatientData.AllergyHistory
            };
            
            result = await _patientCoordinator.UpdatePatientWithValidationAsync(PatientData.Id, updateDto);
        }
        else
        {
            // 创建模式：使用协调器创建
            result = await _patientCoordinator.CreatePatientWithValidationAsync(PatientData);
        }
        
        if (result.IsSuccess)
        {
            MessageBox.Show(
                IsEditMode ? "患者信息更新成功" : "患者创建成功", 
                "成功", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }
        else
        {
            MessageBox.Show(
                result.ErrorMessage ?? (IsEditMode ? "更新患者失败" : "创建患者失败"), 
                "错误", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"{(IsEditMode ? "更新" : "创建")}患者异常: {ex.Message}", 
            "异常", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
    }
    finally
    {
        IsSaving = false;
    }
}

// 输入验证
private bool ValidateInput()
{
    bool isValid = true;
    
    // 重置错误信息
    NameError = string.Empty;
    PhoneError = string.Empty;
    IdNumberError = string.Empty;
    BirthDateError = string.Empty;
    
    // 姓名验证
    if (string.IsNullOrWhiteSpace(PatientData.Name))
    {
        NameError = "患者姓名不能为空";
        isValid = false;
    }
    else if (PatientData.Name.Length > 50)
    {
        NameError = "患者姓名长度不能超过50个字符";
        isValid = false;
    }
    
    // 手机号验证
    if (!string.IsNullOrWhiteSpace(PatientData.PhoneNumber))
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(PatientData.PhoneNumber, @"^1[3-9]\d{9}$"))
        {
            PhoneError = "请输入有效的手机号码";
            isValid = false;
        }
    }
    
    // 身份证号验证
    if (!string.IsNullOrWhiteSpace(PatientData.IdNumber))
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(PatientData.IdNumber, @"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$"))
        {
            IdNumberError = "请输入有效的身份证号码";
            isValid = false;
        }
    }
    
    // 出生日期验证
    if (PatientData.BirthDate.HasValue && PatientData.BirthDate.Value > DateTime.Today)
    {
        BirthDateError = "出生日期不能晚于今天";
        isValid = false;
    }
    
    return isValid;
}

// 异步验证手机号
private async void ValidatePhone()
{
    if (string.IsNullOrWhiteSpace(PatientData.PhoneNumber))
    {
        PhoneError = string.Empty;
        return;
    }
    
    try
    {
        var excludeId = IsEditMode ? PatientData.Id : (Guid?)null;
        var result = await _patientModule.IsPhoneExistsAsync(PatientData.PhoneNumber, excludeId);
        
        if (result.IsSuccess)
        {
            if (result.Data) // true表示手机号已存在
            {
                PhoneError = "该手机号已被其他患者使用";
            }
            else
            {
                PhoneError = string.Empty;
            }
        }
    }
    catch (Exception ex)
    {
        // 验证异常时不显示错误，避免影响用户体验
        System.Diagnostics.Debug.WriteLine($"验证手机号异常: {ex.Message}");
    }
}

// 异步验证身份证号
private async void ValidateIdNumber()
{
    if (string.IsNullOrWhiteSpace(PatientData.IdNumber))
    {
        IdNumberError = string.Empty;
        return;
    }
    
    try
    {
        var excludeId = IsEditMode ? PatientData.Id : (Guid?)null;
        var result = await _patientModule.IsIdCardExistsAsync(PatientData.IdNumber, excludeId);
        
        if (result.IsSuccess)
        {
            if (result.Data) // true表示身份证号已存在
            {
                IdNumberError = "该身份证号已被其他患者使用";
            }
            else
            {
                IdNumberError = string.Empty;
                // 如果身份证号有效，尝试从中提取出生日期
                ExtractBirthDateFromIdNumber();
            }
        }
    }
    catch (Exception ex)
    {
        // 验证异常时不显示错误，避免影响用户体验
        System.Diagnostics.Debug.WriteLine($"验证身份证号异常: {ex.Message}");
    }
}

// 从身份证号提取出生日期
private void ExtractBirthDateFromIdNumber()
{
    if (string.IsNullOrWhiteSpace(PatientData.IdNumber) || PatientData.IdNumber.Length != 18)
        return;
    
    try
    {
        var yearStr = PatientData.IdNumber.Substring(6, 4);
        var monthStr = PatientData.IdNumber.Substring(10, 2);
        var dayStr = PatientData.IdNumber.Substring(12, 2);
        
        if (int.TryParse(yearStr, out int year) &&
            int.TryParse(monthStr, out int month) &&
            int.TryParse(dayStr, out int day))
        {
            var birthDate = new DateTime(year, month, day);
            if (birthDate <= DateTime.Today)
            {
                PatientData.BirthDate = birthDate;
                RaisePropertyChanged(nameof(PatientData));
                RaisePropertyChanged(nameof(CalculatedAge));
            }
        }
    }
    catch
    {
        // 提取失败时静默处理
    }
}
```

## 🔧 依赖注入配置

### 1. 模块注册
```csharp
// PatientsModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // UltraThink修复：模块自己注册服务接口实现
    containerRegistry.RegisterSingleton<PatientModule>();
    containerRegistry.RegisterSingleton<IPatientService>(container => container.Resolve<PatientModule>());
    
    // UltraThink P1重构：注册模块业务协调器
    containerRegistry.RegisterSingleton<PatientCoordinator>();
    
    // 注册视图和视图模型
    containerRegistry.RegisterForNavigation<PatientManagementView, PatientManagementViewModel>();
    containerRegistry.RegisterForNavigation<PatientAddEditDialog, PatientAddEditDialogViewModel>();
    containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();
}
```

### 2. 服务依赖
```csharp
// PatientModule构造函数依赖
public PatientModule(
    IPatientApi apiService,    // API客户端 (来自Desktop.Services)
    IMapper mapper)            // 对象映射 (AutoMapper)
```

### 3. 协调器依赖
```csharp
// PatientCoordinator构造函数依赖
public PatientCoordinator(
    PatientModule patientModule,           // 患者业务服务
    IEventAggregator eventAggregator)      // 事件聚合器 (Prism)
```

### 4. ViewModel依赖
```csharp
// PatientManagementViewModel构造函数依赖
public PatientManagementViewModel(
    PatientModule patientModule,              // 患者业务服务
    PatientCoordinator patientCoordinator,    // 业务协调器
    IDialogService dialogService,             // 对话框服务 (Prism)
    IEventAggregator eventAggregator)         // 事件聚合器 (Prism)
```

## 📊 性能特性

### 1. 分页查询优化
```csharp
// 分页查询减少内存占用
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
{
    try
    {
        // UltraThink v2.0: 直接使用API调用获取DTOs
        var apiResponse = await _apiService.GetPatientsAsync(
            Math.Max(1, query.PageIndex),                    // 确保页码至少为1
            Math.Min(Math.Max(1, query.PageSize), 100),     // 限制页面大小在1-100之间
            query.Keyword?.Trim());                          // 清理关键字
            
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
        
        // UltraThink v2.0: 直接使用DTO，避免额外的映射开销
        var pagedData = apiResponse.Content;
        var result = new PagedResult<PatientDto>(
            pagedData.Items.ToList(),
            pagedData.TotalCount,
            pagedData.CurrentPage,
            pagedData.PageSize);
        
        return ServiceResult<PagedResult<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        return ServiceResult<PagedResult<PatientDto>>.Failure($"获取患者列表异常: {ex.Message}");
    }
}
```

### 2. 搜索优化
```csharp
// 关键字搜索性能优化
public async Task<ServiceResult<IEnumerable<PatientDto>>> SearchByKeywordAsync(string keyword)
{
    try
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return ServiceResult<IEnumerable<PatientDto>>.Success(new List<PatientDto>());
        }
        
        var query = new PatientPagedQueryDto
        {
            PageIndex = 1,
            PageSize = 50, // 限制搜索结果数量，避免大量数据传输
            Keyword = keyword.Trim() // 清理关键字
        };
        
        var result = await GetPagedAsync(query);
        if (!result.IsSuccess)
        {
            return ServiceResult<IEnumerable<PatientDto>>.Failure(result.ErrorMessage ?? "获取数据失败");
        }
        
        return ServiceResult<IEnumerable<PatientDto>>.Success(result.Data?.Items ?? new List<PatientDto>());
    }
    catch (Exception ex)
    {
        return ServiceResult<IEnumerable<PatientDto>>.Failure($"关键字搜索患者异常: {ex.Message}");
    }
}
```

### 3. 缓存策略
```csharp
// 性别选项缓存，避免重复创建
private static List<object> _cachedGenders;

public List<object> GetAvailableGenders()
{
    if (_cachedGenders == null)
    {
        _cachedGenders = Enum.GetValues(typeof(Gender))
            .Cast<Gender>()
            .Select(g => new { Value = g, Text = g.GetDisplayName() })
            .Cast<object>()
            .ToList();
    }
    
    return _cachedGenders;
}
```

### 4. 异步操作
```csharp
// 所有数据操作都使用异步方法，不阻塞UI线程
private async Task LoadPatientsAsync()
{
    try
    {
        IsLoading = true;
        
        var query = new PatientPagedQueryDto
        {
            PageIndex = CurrentPage,
            PageSize = PageSize,
            Keyword = SearchKeyword?.Trim()
        };
        
        // 异步调用，不阻塞UI
        var result = await _patientModule.GetPagedAsync(query);
        
        // UI更新在主线程执行
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            UpdatePatientList(result);
        });
    }
    finally
    {
        IsLoading = false;
    }
}
```

## 🧪 测试支持

### 1. 单元测试结构
```csharp
[TestClass]
public class PatientModuleTests
{
    private Mock<IPatientApi> _mockPatientApi;
    private Mock<IMapper> _mockMapper;
    private PatientModule _patientModule;

    [TestInitialize]
    public void Setup()
    {
        _mockPatientApi = new Mock<IPatientApi>();
        _mockMapper = new Mock<IMapper>();
        
        _patientModule = new PatientModule(_mockPatientApi.Object, _mockMapper.Object);
    }

    [TestMethod]
    public async Task GetByIdAsync_ValidId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedPatient = new PatientDto 
        { 
            Id = patientId, 
            Name = "测试患者", 
            PhoneNumber = "13800138000" 
        };
        
        _mockPatientApi.Setup(x => x.GetPatientByIdAsync(patientId))
                      .ReturnsAsync(new ApiResponse<PatientDto>(expectedPatient, HttpStatusCode.OK));

        // Act
        var result = await _patientModule.GetByIdAsync(patientId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("测试患者", result.Data.Name);
    }

    [TestMethod]
    public async Task CreateAsync_ValidPatient_ReturnsSuccess()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "新患者",
            Gender = Gender.Male,
            PhoneNumber = "13900139000",
            BirthDate = new DateTime(1990, 1, 1)
        };

        var createdPatient = new PatientDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Gender = createDto.Gender,
            PhoneNumber = createDto.PhoneNumber
        };

        _mockPatientApi.Setup(x => x.CreatePatientAsync(It.IsAny<PatientCreateDto>()))
                      .ReturnsAsync(new ApiResponse<PatientDto>(createdPatient, HttpStatusCode.OK));

        // Act
        var result = await _patientModule.CreateAsync(createDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("新患者", result.Data.Name);
    }

    [TestMethod]
    public async Task IsPhoneExistsAsync_ExistingPhone_ReturnsTrue()
    {
        // Arrange
        var phoneNumber = "13800138000";
        var existingPatients = new List<PatientDto>
        {
            new PatientDto { Id = Guid.NewGuid(), Name = "现有患者", PhoneNumber = phoneNumber }
        };

        var pagedResult = new PagedResult<PatientDto>(existingPatients, 1, 1, 50);
        _mockPatientApi.Setup(x => x.GetPatientsAsync(1, 50, phoneNumber))
                      .ReturnsAsync(new ApiResponse<PagedResult<PatientDto>>(pagedResult, HttpStatusCode.OK));

        // Act
        var result = await _patientModule.IsPhoneExistsAsync(phoneNumber);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.Data);
    }
}
```

### 2. 业务协调器测试
```csharp
[TestClass]
public class PatientCoordinatorTests
{
    private Mock<PatientModule> _mockPatientModule;
    private Mock<IEventAggregator> _mockEventAggregator;
    private PatientCoordinator _patientCoordinator;

    [TestMethod]
    public async Task CreatePatientWithValidationAsync_NoDuplicates_CreatesPatientAndPublishesEvent()
    {
        // Arrange
        var createDto = new PatientCreateDto
        {
            Name = "测试患者",
            PhoneNumber = "13800138000",
            IdNumber = "110101199001011234"
        };

        var createdPatient = new PatientDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name
        };

        _mockPatientModule.Setup(x => x.ValidatePatientAsync(createDto))
                         .ReturnsAsync(ServiceResult<object>.Success(new { IsValid = true }));

        _mockPatientModule.Setup(x => x.CheckDuplicatePatientsAsync(createDto.IdNumber, createDto.PhoneNumber))
                         .ReturnsAsync(new List<PatientDto>());

        _mockPatientModule.Setup(x => x.CreateAsync(createDto))
                         .ReturnsAsync(ServiceResult<PatientDto>.Success(createdPatient));

        var mockPatientCreatedEvent = new Mock<PatientCreatedEvent>();
        _mockEventAggregator.Setup(x => x.GetEvent<PatientCreatedEvent>())
                           .Returns(mockPatientCreatedEvent.Object);

        // Act
        var result = await _patientCoordinator.CreatePatientWithValidationAsync(createDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        mockPatientCreatedEvent.Verify(x => x.Publish(It.IsAny<PatientCreatedEventArgs>()), Times.Once);
    }
}
```

### 3. ViewModel测试
```csharp
[TestClass]
public class PatientManagementViewModelTests
{
    private Mock<PatientModule> _mockPatientModule;
    private Mock<PatientCoordinator> _mockPatientCoordinator;
    private Mock<IDialogService> _mockDialogService;
    private PatientManagementViewModel _viewModel;

    [TestMethod]
    public async Task LoadPatientsCommand_Execute_LoadsPatients()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new PatientDto { Id = Guid.NewGuid(), Name = "患者1", PhoneNumber = "13800138001" },
            new PatientDto { Id = Guid.NewGuid(), Name = "患者2", PhoneNumber = "13800138002" }
        };

        var pagedResult = new PagedResult<PatientDto>(patients, 2, 1, 20);
        var serviceResult = ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);

        _mockPatientModule.Setup(x => x.GetPagedAsync(It.IsAny<PatientPagedQueryDto>()))
                         .ReturnsAsync(serviceResult);

        // Act
        _viewModel.LoadPatientsCommand.Execute();
        await Task.Delay(100); // 等待异步操作完成

        // Assert
        Assert.AreEqual(2, _viewModel.Patients.Count);
        Assert.AreEqual("患者1", _viewModel.Patients[0].Name);
        Assert.AreEqual(2, _viewModel.TotalCount);
    }
}
```

## 📝 使用示例

### 1. 基本患者管理
```csharp
// 创建患者
var newPatient = new PatientCreateDto
{
    Name = "张三",
    Gender = Gender.Male,
    BirthDate = new DateTime(1985, 6, 15),
    PhoneNumber = "13800138000",
    IdNumber = "110101198506151234",
    Address = "北京市朝阳区某某街道",
    EmergencyContact = "李四",
    EmergencyPhone = "13900139000",
    AllergyHistory = "青霉素过敏"
};

var createResult = await patientModule.CreateAsync(newPatient);
if (createResult.IsSuccess)
{
    Console.WriteLine($"患者创建成功: {createResult.Data.Name} (ID: {createResult.Data.Id})");
}

// 查询患者列表
var query = new PatientPagedQueryDto
{
    PageIndex = 1,
    PageSize = 20,
    Keyword = "张"
};

var queryResult = await patientModule.GetPagedAsync(query);
if (queryResult.IsSuccess)
{
    Console.WriteLine($"找到 {queryResult.Data.TotalCount} 个患者");
    foreach (var patient in queryResult.Data.Items)
    {
        Console.WriteLine($"- {patient.Name} ({patient.Age}岁) - {patient.PhoneNumber}");
    }
}

// 更新患者信息
var updateDto = new PatientUpdateDto
{
    Name = "张三丰",
    PhoneNumber = "13800138001",
    Address = "北京市海淀区新地址"
};

var updateResult = await patientModule.UpdateAsync(createResult.Data.Id, updateDto);
if (updateResult.IsSuccess)
{
    Console.WriteLine("患者信息更新成功");
}
```

### 2. 患者搜索
```csharp
// 按关键字搜索
var searchResult = await patientModule.SearchByKeywordAsync("张三");
if (searchResult.IsSuccess)
{
    Console.WriteLine($"搜索到 {searchResult.Data.Count()} 个患者");
    foreach (var patient in searchResult.Data)
    {
        Console.WriteLine($"- {patient.Name} - {patient.PhoneNumber}");
    }
}

// 按身份证号查找
var idCardResult = await patientModule.GetByIdCardAsync("110101198506151234");
if (idCardResult.IsSuccess)
{
    Console.WriteLine($"找到患者: {idCardResult.Data.Name}");
}
else
{
    Console.WriteLine("未找到匹配的患者");
}

// 按手机号查找
var phoneResult = await patientModule.GetByPhoneAsync("13800138000");
if (phoneResult.IsSuccess && phoneResult.Data.Any())
{
    Console.WriteLine($"找到 {phoneResult.Data.Count} 个使用该手机号的患者");
    foreach (var patient in phoneResult.Data)
    {
        Console.WriteLine($"- {patient.Name} (ID: {patient.Id})");
    }
}
```

### 3. 数据验证
```csharp
// 检查手机号是否存在
var phoneExistsResult = await patientModule.IsPhoneExistsAsync("13800138000");
if (phoneExistsResult.IsSuccess)
{
    if (phoneExistsResult.Data)
    {
        Console.WriteLine("该手机号已被使用");
    }
    else
    {
        Console.WriteLine("该手机号可以使用");
    }
}

// 检查身份证号是否存在
var idCardExistsResult = await patientModule.IsIdCardExistsAsync("110101198506151234");
if (idCardExistsResult.IsSuccess)
{
    if (idCardExistsResult.Data)
    {
        Console.WriteLine("该身份证号已被使用");
    }
    else
    {
        Console.WriteLine("该身份证号可以使用");
    }
}

// 检查重复患者
var duplicatePatients = await patientModule.CheckDuplicatePatientsAsync("110101198506151234", "13800138000");
if (duplicatePatients.Any())
{
    Console.WriteLine("发现重复患者:");
    foreach (var patient in duplicatePatients)
    {
        Console.WriteLine($"- {patient.Name} (ID: {patient.Id})");
    }
}
```

### 4. 数据导入导出
```csharp
// 批量导入患者数据
var importData = new List<PatientImportDto>
{
    new PatientImportDto
    {
        Name = "导入患者1",
        GenderText = "男",
        Age = 35,
        PhoneNumber = "13700137000",
        Address = "导入地址1"
    },
    new PatientImportDto
    {
        Name = "导入患者2",
        GenderText = "女",
        Age = 28,
        PhoneNumber = "13700137001",
        Address = "导入地址2"
    }
};

var importResult = await patientModule.ImportPatientsAsync(importData);
if (importResult.IsSuccess)
{
    Console.WriteLine($"成功导入 {importResult.Data} 条患者记录");
}

// 导出患者数据
var exportResult = await patientModule.ExportPatientsAsync();
if (exportResult.IsSuccess)
{
    Console.WriteLine($"导出了 {exportResult.Data.Count} 条患者记录");
    
    // 保存到文件或进行其他处理
    foreach (var patient in exportResult.Data.Take(5)) // 显示前5条
    {
        Console.WriteLine($"- {patient.Name} ({patient.Age}岁) - {patient.PhoneNumber}");
    }
}

// 获取导入模板
var templateResult = await patientModule.GetImportTemplateAsync();
if (templateResult.IsSuccess)
{
    Console.WriteLine($"获取导入模板成功，文件大小: {templateResult.Data.Length} 字节");
    
    // 保存模板文件
    await File.WriteAllBytesAsync("患者导入模板.xlsx", templateResult.Data);
}
```

### 5. 使用业务协调器
```csharp
// 使用协调器创建患者（包含完整验证流程）
var patientCoordinator = containerProvider.Resolve<PatientCoordinator>();

var createDto = new PatientCreateDto
{
    Name = "协调器测试患者",
    Gender = Gender.Female,
    PhoneNumber = "13600136000",
    IdNumber = "110101199001011234"
};

var coordinatorResult = await patientCoordinator.CreatePatientWithValidationAsync(createDto);
if (coordinatorResult.IsSuccess)
{
    Console.WriteLine($"通过协调器创建患者成功: {coordinatorResult.Data.Name}");
    // 同时会自动发布患者创建事件
}
else
{
    Console.WriteLine($"创建失败: {coordinatorResult.ErrorMessage}");
}
```

## 🔄 版本历史

- **v1.0.0** - 初始版本，基础患者CRUD功能
- **v1.1.0** - 添加分页查询和搜索功能
- **v1.2.0** - 添加数据验证和重复检查
- **v1.3.0** - 添加数据导入导出功能
- **v2.0.0** - UltraThink架构重构，优化API调用
- **v2.1.0** - 添加业务协调器PatientCoordinator
- **v2.2.0** - 完善状态管理和事件系统
- **v2.3.0** - 优化用户界面和用户体验
- **v2.4.0** - 添加高级搜索和统计功能

## 📚 相关文档

- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)
- [Desktop.Services文档](../core/desktop-services.md)
- [Shared.Models文档](../../shared/models.md)
- [后端Patients模块文档](../../backend/modules/patients.md)
- [Users模块文档](./users.md)
- [MedicalCase模块文档](./medicalcase.md)