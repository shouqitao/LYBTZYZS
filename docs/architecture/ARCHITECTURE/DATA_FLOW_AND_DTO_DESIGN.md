# 数据流转架构与DTO设计思路

## 一、整体架构概览

### 1.1 分层架构
```
┌─────────────────┐
│   前端界面(UI)   │  WPF/Web
├─────────────────┤
│   Web API层     │  Controllers
├─────────────────┤
│   业务逻辑层    │  Services
├─────────────────┤
│   数据访问层    │  Repositories
├─────────────────┤
│   数据库层      │  SQL Server
└─────────────────┘
```

### 1.2 数据模型分层
```
数据库表 ←→ Entity Model ←→ DTO ←→ 前端Model
```

## 二、数据模型详解

### 2.1 Entity Model（实体模型）
- **位置**：`LYBT.Models` 项目
- **作用**：直接映射数据库表结构
- **特点**：
  - 包含所有数据库字段
  - 包含导航属性（外键关系）
  - 由Entity Framework管理

**示例**：
```csharp
// 用户实体
public class UserModel {
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }  // 敏感信息
    public string RealName { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? LastLoginTime { get; set; }
    // ... 其他数据库字段
}
```

### 2.2 DTO（数据传输对象）
- **位置**：`LYBT.Shared.Models` 项目
- **作用**：API层数据传输
- **特点**：
  - 只包含需要传输的字段
  - 隐藏敏感信息
  - 可能包含计算属性

#### DTO类型设计

##### 1. 列表DTO（简化版）
```csharp
public class PatientDto {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Gender { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? LastVisitTime { get; set; }
    public int VisitCount { get; set; }
    // 不包含详细病历等信息
}
```

##### 2. 详情DTO（完整版）
```csharp
public class PatientDetailDto {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Gender { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public string MedicalHistory { get; set; }
    public string AllergyHistory { get; set; }
    // 包含所有需要显示的详细信息
}
```

##### 3. 创建DTO（新增用）
```csharp
public class PatientCreateDto {
    public string Name { get; set; }
    public int Gender { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    // 不包含Id、CreateTime等自动生成字段
}
```

##### 4. 更新DTO（编辑用）
```csharp
public class PatientUpdateDto {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Gender { get; set; }
    public int Age { get; set; }
    // 不包含CreateTime等不可修改字段
}
```

##### 5. 查询DTO（搜索用）
```csharp
public class PatientPagedQueryDto {
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

## 三、数据流转过程

### 3.1 查询数据流程（数据库→前端）

```
1. 前端发起请求
   GET /api/v1/patients

2. Controller接收请求
   [HttpGet]
   public async Task<IActionResult> GetList()

3. Service层处理业务逻辑
   var models = await _repository.GetListAsync();
   
4. Repository层查询数据库
   return await _context.Patients
       .Where(p => !p.IsDeleted)
       .ToListAsync();

5. AutoMapper转换
   var dtos = _mapper.Map<List<PatientDto>>(models);

6. 返回API响应
   return Ok(ApiResponse<List<PatientDto>>.Success(dtos));

7. 前端接收并显示
   将DTO转换为前端ViewModel
```

### 3.2 创建数据流程（前端→数据库）

```
1. 前端提交表单
   POST /api/v1/patients/add
   Body: PatientCreateDto

2. Controller接收并验证
   public async Task<IActionResult> Add([FromBody] PatientCreateDto dto)

3. AutoMapper转换为Entity
   var model = _mapper.Map<PatientModel>(dto);
   
4. Service层补充业务数据
   model.Id = Guid.NewGuid();
   model.CreateTime = DateTime.Now;
   model.CreatedBy = currentUserId;

5. Repository层保存到数据库
   await _context.Patients.AddAsync(model);
   await _context.SaveChangesAsync();

6. 返回操作结果
   return Ok(ApiResponse<object>.Success("创建成功"));
```

## 四、AutoMapper配置示例

### 4.1 基础映射
```csharp
public class PatientMappingProfile : Profile {
    public PatientMappingProfile() {
        // Entity → 列表DTO
        CreateMap<PatientModel, PatientDto>();
        
        // Entity → 详情DTO
        CreateMap<PatientModel, PatientDetailDto>();
        
        // 创建DTO → Entity
        CreateMap<PatientCreateDto, PatientModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now));
            
        // 更新DTO → Entity
        CreateMap<PatientUpdateDto, PatientModel>()
            .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
            .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => DateTime.Now));
    }
}
```

### 4.2 复杂映射（包含关联数据）
```csharp
// 医生Entity包含User导航属性
CreateMap<DoctorModel, DoctorDto>()
    .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.Username))
    .ForMember(d => d.RealName, opt => opt.MapFrom(s => s.User.RealName))
    .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.User.PhoneNumber));
```

## 五、设计原则与最佳实践

### 5.1 DTO设计原则
1. **单一职责**：每个DTO只服务于特定场景
2. **最小化原则**：只包含必要的字段
3. **安全性**：不暴露敏感信息（如密码、内部ID等）
4. **扁平化**：避免深层嵌套，提高传输效率

### 5.2 命名规范
- 列表展示：`[Entity]Dto`
- 详细信息：`[Entity]DetailDto`
- 创建操作：`[Entity]CreateDto`
- 更新操作：`[Entity]UpdateDto`
- 分页查询：`[Entity]PagedQueryDto`

### 5.3 共享DTO vs本地DTO
- **共享DTO**（推荐）：放在 `LYBT.Shared.Models` 中，前后端共用
- **本地DTO**：放在各模块内部，仅模块内使用

### 5.4 API响应包装
```csharp
public class ApiResponse<T> {
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
}
```

## 六、实际案例分析

### 6.1 用户列表查询
```csharp
// Controller
[HttpGet]
public async Task<IActionResult> GetList() {
    var users = await _userService.GetListAsync();
    return Ok(ApiResponse<List<UserDto>>.Success(users));
}

// Service
public async Task<List<UserDto>> GetListAsync() {
    var models = await _repository.GetListAsync();
    return _mapper.Map<List<UserDto>>(models);
}

// Repository
public async Task<List<UserModel>> GetListAsync() {
    return await _context.Users
        .Where(u => u.IsActive)
        .OrderBy(u => u.CreateTime)
        .ToListAsync();
}
```

### 6.2 分页查询
```csharp
// Controller
[HttpPost("paged")]
public async Task<IActionResult> GetPaged([FromBody] UserPagedQueryDto query) {
    var result = await _userService.GetPagedAsync(query);
    return Ok(ApiResponse<PaginatedResult<UserDto>>.Success(result));
}

// Service
public async Task<PaginatedResult<UserDto>> GetPagedAsync(UserPagedQueryDto query) {
    var (models, total) = await _repository.GetPagedAsync(query);
    var dtos = _mapper.Map<List<UserDto>>(models);
    return new PaginatedResult<UserDto> {
        Items = dtos,
        TotalCount = total
    };
}
```

## 七、性能优化建议

1. **使用投影查询**：减少数据传输
   ```csharp
   var dtos = await _context.Patients
       .Select(p => new PatientDto {
           Id = p.Id,
           Name = p.Name,
           // 只选择需要的字段
       })
       .ToListAsync();
   ```

2. **延迟加载vs预加载**：根据需要选择
   ```csharp
   // 预加载关联数据
   var doctors = await _context.Doctors
       .Include(d => d.User)
       .ToListAsync();
   ```

3. **缓存策略**：对不常变动的数据使用缓存
   ```csharp
   if (!_cache.TryGetValue(cacheKey, out List<HerbDto> herbs)) {
       herbs = await _herbService.GetListAsync();
       _cache.Set(cacheKey, herbs, TimeSpan.FromMinutes(10));
   }
   ```

## 八、总结

这个分层架构通过DTO实现了：
1. **解耦**：前端不依赖后端数据结构
2. **安全**：控制数据暴露范围
3. **灵活**：不同场景使用不同DTO
4. **高效**：只传输必要数据
5. **可维护**：清晰的职责划分

通过AutoMapper自动化映射，减少了手工转换代码，提高了开发效率。