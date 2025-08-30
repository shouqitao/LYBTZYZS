# AutoMapper使用规范 - UltraThink强制标准

基于MedicalCase字段更新遗漏等严重问题的修复经验，制定的AutoMapper强制使用规范。

## 🚨 强制性要求

### 1. 必须使用AutoMapper
**所有DTO与Entity之间的映射必须使用AutoMapper，禁止手动字段映射。**

```csharp
// ✅ 正确 - 使用AutoMapper
public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
{
    var model = await _repository.GetByIdAsync(id, true);
    if (model == null) return ServiceResult<PatientDto>.Failure("记录不存在");
    
    // 使用AutoMapper确保字段更新完整性
    _mapper.Map(dto, model);
    
    var result = await _repository.UpdateAsync(model);
    return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(result));
}

// ❌ 错误 - 手动映射（容易遗漏字段）
public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
{
    var model = await _repository.GetByIdAsync(id, true);
    
    // 危险！只更新了2个字段，遗漏了其他13个字段
    if (!string.IsNullOrWhiteSpace(dto.Status)) { model.Status = status; }
    if (!string.IsNullOrWhiteSpace(dto.Remark)) { model.Remark = dto.Remark; }
    
    return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(result));
}
```

### 2. 禁止的危险模式

#### ❌ 手动字段赋值
```csharp
// 禁止：容易遗漏字段
model.Name = dto.Name;
model.Age = dto.Age;
model.Address = dto.Address;
// ... 可能遗漏其他字段
```

#### ❌ 条件式手动映射
```csharp
// 禁止：MedicalCase曾经的错误模式
if (!string.IsNullOrWhiteSpace(dto.ChiefComplaint)) { model.ChiefComplaint = dto.ChiefComplaint; }
if (!string.IsNullOrWhiteSpace(dto.Status)) { model.Status = ParseStatus(dto.Status); }
// 只更新了2/15个字段！
```

#### ❌ 选择性字段更新
```csharp
// 禁止：容易产生不一致
if (dto.Name != null) model.Name = dto.Name;
if (dto.Price.HasValue) model.Price = dto.Price.Value;
// 缺少统一的null处理策略
```

## ✅ 正确使用方式

### 1. 标准更新模式
```csharp
public async Task<ServiceResult<TDto>> UpdateAsync(Guid id, TUpdateDto dto)
{
    try
    {
        var model = await _repository.GetByIdAsync(id, true);
        if (model == null) return ServiceResult<TDto>.Failure("记录不存在");
        
        // 数据验证（如需要）
        await ValidateForUpdate(id, dto);
        
        // 使用AutoMapper进行完整字段映射
        _mapper.Map(dto, model);
        
        // 特殊处理（如拼音码生成）
        if (!string.IsNullOrWhiteSpace(model.Name))
        {
            model.PinYinCode = GeneratePinYinCode(model.Name);
        }
        
        var result = await _repository.UpdateAsync(model);
        return ServiceResult<TDto>.Success(_mapper.Map<TDto>(result));
    }
    catch (Exception ex)
    {
        return ServiceResult<TDto>.Failure("更新失败", ex);
    }
}
```

### 2. 创建操作模式
```csharp
public async Task<ServiceResult<TDto>> CreateAsync(TCreateDto dto)
{
    try
    {
        // 验证数据
        await ValidateForCreate(dto);
        
        // 使用AutoMapper创建模型
        var model = _mapper.Map<TModel>(dto);
        model.Id = Guid.NewGuid();
        
        // 业务处理
        ProcessBusinessLogic(model);
        
        var result = await _repository.AddAsync(model);
        return ServiceResult<TDto>.Success(_mapper.Map<TDto>(result));
    }
    catch (Exception ex)
    {
        return ServiceResult<TDto>.Failure("创建失败", ex);
    }
}
```

### 3. 查询结果映射
```csharp
public async Task<ServiceResult<TDto>> GetByIdAsync(Guid id)
{
    var model = await _repository.GetByIdAsync(id);
    if (model == null) return ServiceResult<TDto>.Failure("记录不存在");
    
    // 直接使用AutoMapper映射
    var dto = _mapper.Map<TDto>(model);
    return ServiceResult<TDto>.Success(dto);
}
```

## 🔧 映射配置标准

### 1. Profile配置规范
```csharp
public class ExampleMappingProfile : Profile
{
    public ExampleMappingProfile()
    {
        // Entity ↔ Dto 双向映射
        CreateMap<ExampleModel, ExampleDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
            .ReverseMap()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<CommonStatus>(src.Status)));
            
        // CreateDto → Entity 单向映射
        CreateMap<ExampleCreateDto, ExampleModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.Ignore());
            
        // UpdateDto → Entity 单向映射（重点！）
        CreateMap<ExampleUpdateDto, ExampleModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForSourceMember(src => src.SomeExtraField, opt => opt.DoNotValidate());
    }
}
```

### 2. 特殊类型转换
```csharp
// 枚举转换
.ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
.ForMember(dest => dest.Role, opt => opt.MapFrom(src => Enum.Parse<UserRole>(src.Role)))

// 复杂对象转换
.ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))

// DTO字段不在Entity中的处理
.ForSourceMember(src => src.ExtraField, opt => opt.DoNotValidate())
```

### 3. 构造函数注入
```csharp
public class ExampleService
{
    private readonly IMapper _mapper;
    
    public ExampleService(IMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }
}
```

## 🧪 测试验证要求

### 1. 字段更新完整性测试
```csharp
[Fact]
public void UpdateMapping_ShouldMapAllFields()
{
    // Arrange
    var existing = new ExampleModel { /* 初始值 */ };
    var updateDto = new ExampleUpdateDto { /* 更新值 */ };
    
    // Act - 使用AutoMapper更新
    _mapper.Map(updateDto, existing);
    
    // Assert - 验证所有字段都被正确更新
    Assert.Equal(updateDto.Field1, existing.Field1);
    Assert.Equal(updateDto.Field2, existing.Field2);
    // ... 验证每个字段
}
```

### 2. 映射配置验证
```csharp
[Fact]
public void AutoMapperConfiguration_ShouldBeValid()
{
    _mapper.ConfigurationProvider.AssertConfigurationIsValid();
}
```

### 3. 往返映射一致性
```csharp
[Fact]
public void RoundTripMapping_ShouldMaintainConsistency()
{
    var original = new ExampleModel { /* 测试数据 */ };
    var dto = _mapper.Map<ExampleDto>(original);
    var roundTrip = _mapper.Map<ExampleModel>(dto);
    
    Assert.Equal(original.Id, roundTrip.Id);
    Assert.Equal(original.Name, roundTrip.Name);
}
```

## 🚨 常见错误及修复

### 1. 字段遗漏问题
**问题**：手动映射导致字段更新不完整
```csharp
// ❌ 错误：MedicalCase曾经的问题
if (!string.IsNullOrWhiteSpace(dto.Status)) { model.Status = status; }
if (!string.IsNullOrWhiteSpace(dto.Remark)) { model.Remark = dto.Remark; }
// 遗漏了13个其他字段！
```

**修复**：使用AutoMapper完整映射
```csharp
// ✅ 修复
_mapper.Map(dto, model);  // 自动映射所有配置的字段
```

### 2. 类型转换错误
**问题**：枚举字符串转换失败
```csharp
// ❌ 可能出错
model.Status = (CommonStatus)Enum.Parse(typeof(CommonStatus), dto.Status);
```

**修复**：在Profile中配置转换
```csharp
// ✅ 在MappingProfile中配置
.ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
    Enum.TryParse<CommonStatus>(src.Status, out var status) ? status : CommonStatus.Enabled))
```

### 3. 空值处理不当
**问题**：空值判断逻辑复杂易错
```csharp
// ❌ 复杂易错
if (dto.Name != null) model.Name = dto.Name;
if (dto.Age.HasValue) model.Age = dto.Age.Value;
```

**修复**：让AutoMapper处理
```csharp
// ✅ AutoMapper自动处理null值
_mapper.Map(dto, model);
```

## 📋 检查清单

### 开发阶段
- [ ] 是否使用AutoMapper进行所有DTO映射？
- [ ] 是否配置了所有必要的字段映射？
- [ ] 是否处理了特殊类型转换（枚举、复杂对象）？
- [ ] 是否忽略了不应映射的字段（Id、CreateTime等）？

### 代码审查阶段  
- [ ] 是否存在手动字段赋值模式？
- [ ] 是否存在条件式映射逻辑？
- [ ] UpdateDto映射是否可能遗漏字段？
- [ ] 是否有对应的映射测试？

### 测试阶段
- [ ] AutoMapper配置验证是否通过？
- [ ] 字段更新完整性测试是否覆盖？
- [ ] 往返映射一致性是否验证？
- [ ] 边界条件（null、空值）是否测试？

## 📖 相关文档

- [UltraThink重构案例](../ultrathink/)
- [代码质量门禁规则](QUALITY_GATES.md)
- [DTO映射测试框架](../tests/mapping/)
- [业务助手重构指南](../architecture/BUSINESS_HELPER_REFACTORING.md)

---

**最后更新**: 2025-08-28  
**版本**: v2.0 - 基于UltraThink重构经验  
**维护者**: 系统架构团队