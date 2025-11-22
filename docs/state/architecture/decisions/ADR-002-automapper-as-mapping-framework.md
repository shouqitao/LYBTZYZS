# ADR-002: AutoMapper作为统一映射框架

**日期**: 2025-10-26（追溯决策记录）
**状态**: Accepted
**决策者**: 项目架构团队
**标签**: #架构 #映射 #技术选型

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-002 |
| **创建日期** | 2025-10-26 |
| **最后更新** | 2025-10-26 |
| **状态** | Accepted（追溯记录） |
| **决策者** | 项目架构团队 |
| **影响范围** | Server端全系统 |
| **相关Issue** | 无（早期技术选型） |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

Server端需要统一的对象映射框架，用于：
1. **Entity ↔ DTO映射**：实体与数据传输对象之间的转换
2. **DTO ↔ ViewModel映射**：DTO与视图模型之间的转换（Client端）
3. **减少样板代码**：避免手动编写大量的属性赋值代码
4. **映射逻辑集中管理**：统一管理复杂的映射规则（如字段重命名、类型转换、条件映射）

### 当前状态（选型前）

有三种可选映射方案：
1. **手动映射**：手动编写对象赋值代码
2. **AutoMapper**：第三方映射框架，基于约定自动映射
3. **Mapster**：高性能映射框架，编译期生成映射代码

### 问题影响

如果不统一映射框架，会导致：
- **代码冗余**：大量重复的属性赋值代码
- **可维护性差**：Entity结构变更需要手动更新所有映射代码
- **易出错**：手动映射容易遗漏字段或写错属性名
- **测试困难**：手动映射逻辑分散在Service层，难以统一测试

---

## ✅ 决策（Decision）

**选择AutoMapper作为Server端统一映射框架**：

### 核心原则

1. **约定优于配置**：属性名相同时自动映射，无需手动配置
2. **Profile集中管理**：每个模块一个MappingProfile，集中定义映射规则
3. **与依赖注入集成**：通过`IMapper`接口注入，支持单元测试
4. **双向映射**：支持Entity→DTO和DTO→Entity双向转换

### 技术实现

**项目结构**：
```
LYBT.Application/
├── Mappings/
│   ├── PatientMappingProfile.cs
│   ├── MedicalCaseMappingProfile.cs
│   ├── ConsultationMappingProfile.cs
│   └── PrescriptionMappingProfile.cs
└── ServiceCollectionExtensions.cs（注册AutoMapper）
```

**映射配置示例**：
```csharp
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // 基础映射（约定：属性名相同自动映射）
        CreateMap<Patient, PatientDto>();
        CreateMap<CreatePatientDto, Patient>();
        CreateMap<UpdatePatientDto, Patient>();

        // 复杂映射（字段重命名、类型转换）
        CreateMap<Patient, PatientDetailDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)))
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => $"{src.Province}{src.City}{src.Address}"));

        // 忽略特定字段
        CreateMap<UpdatePatientDto, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

**依赖注入配置**：
```csharp
// Program.cs or ServiceCollectionExtensions.cs
services.AddAutoMapper(typeof(PatientMappingProfile).Assembly);
```

**Service层使用示例**：
```csharp
public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;

    public PatientService(IPatientRepository patientRepository, IMapper mapper)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        // DTO → Entity
        var patient = _mapper.Map<Patient>(dto);
        patient = await _patientRepository.AddAsync(patient);

        // Entity → DTO
        return _mapper.Map<PatientDto>(patient);
    }

    public async Task<PatientDto> UpdateAsync(int id, UpdatePatientDto dto)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
            throw new NotFoundException($"患者{id}不存在");

        // DTO → Entity（更新现有实体）
        _mapper.Map(dto, patient);
        patient = await _patientRepository.UpdateAsync(patient);

        return _mapper.Map<PatientDto>(patient);
    }
}
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **减少样板代码**：自动映射减少80%+的手动属性赋值代码
- ✅ **约定优于配置**：属性名相同时无需配置，自动映射
- ✅ **映射逻辑集中**：所有映射规则在Profile中定义，易于维护
- ✅ **支持复杂映射**：字段重命名、类型转换、条件映射、集合映射等
- ✅ **编译期验证**：`AssertConfigurationIsValid()`可验证映射配置正确性
- ✅ **测试友好**：`IMapper`接口易于Mock，支持单元测试
- ✅ **社区活跃**：AutoMapper是.NET生态最成熟的映射框架（10k+ stars）

### 缺点（Cons）

- ❌ **第三方依赖**：需要引入NuGet包（AutoMapper、AutoMapper.Extensions.Microsoft.DependencyInjection）
- ❌ **学习成本**：团队需要学习AutoMapper的API和Profile配置
- ❌ **运行时性能**：基于反射，性能略低于手动映射（但差异极小，~1-5μs）
- ❌ **调试困难**：映射错误时，异常信息可能不直观
- ❌ **"黑魔法"风险**：过度依赖约定可能导致隐式映射错误

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| AutoMapper停止维护 | 未来升级困难 | 项目活跃度高（10k+ stars），社区活跃，风险极低 |
| 映射配置错误 | 运行时异常 | 编写单元测试验证映射配置（`_mapper.ConfigurationProvider.AssertConfigurationIsValid()`） |
| 性能问题 | API响应变慢 | 映射操作极快（~1-5μs），对整体性能影响可忽略；如极端场景可切换为手动映射 |
| 隐式映射错误 | 数据丢失或错误 | 禁止使用隐式映射，所有映射必须在Profile中显式配置 |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: 手动映射

**描述**: 在Service层手动编写对象赋值代码

**示例**：
```csharp
public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
{
    // 手动映射：DTO → Entity
    var patient = new Patient
    {
        Name = dto.Name,
        Gender = dto.Gender,
        BirthDate = dto.BirthDate,
        Phone = dto.Phone,
        Address = dto.Address,
        IdCard = dto.IdCard,
        MedicalHistory = dto.MedicalHistory,
        Allergies = dto.Allergies,
        Notes = dto.Notes
    };

    patient = await _patientRepository.AddAsync(patient);

    // 手动映射：Entity → DTO
    return new PatientDto
    {
        Id = patient.Id,
        Name = patient.Name,
        Gender = patient.Gender,
        BirthDate = patient.BirthDate,
        Phone = patient.Phone,
        Address = patient.Address,
        CreatedAt = patient.CreatedAt
    };
}
```

**优点**:
- ✅ 无需第三方依赖
- ✅ 性能最优（无反射开销）
- ✅ 调试直观，编译期类型安全

**缺点**:
- ❌ **代码冗余**：每个Service方法都需要重复编写映射代码
- ❌ **可维护性差**：Entity结构变更需要手动更新所有映射代码
- ❌ **易出错**：容易遗漏字段或写错属性名
- ❌ **代码量大**：项目中有50+个Entity和100+个DTO，手动映射代码量庞大

**为什么未采纳**: 代码冗余、可维护性差，长期维护成本高

---

### 方案B: Mapster（高性能替代方案）

**描述**: 使用Mapster进行对象映射，编译期生成映射代码

**示例**：
```csharp
// 配置
TypeAdapterConfig<Patient, PatientDto>.NewConfig()
    .Map(dest => dest.Age, src => CalculateAge(src.BirthDate));

// 使用
var dto = patient.Adapt<PatientDto>();
```

**优点**:
- ✅ **性能优异**：编译期生成映射代码，性能接近手动映射
- ✅ **API简洁**：`Adapt<T>()`语法比AutoMapper简洁
- ✅ **学习曲线平缓**：API设计比AutoMapper更直观

**缺点**:
- ❌ **社区较小**：相比AutoMapper社区规模小（2k+ stars vs 10k+ stars）
- ❌ **文档较少**：官方文档和社区资源不如AutoMapper丰富
- ❌ **生态不成熟**：与ASP.NET Core生态集成不如AutoMapper完善
- ❌ **团队熟悉度低**：团队更熟悉AutoMapper

**为什么未采纳**: 社区规模和生态成熟度不如AutoMapper，团队熟悉度低，学习成本高

---

### 方案C: 混合方案（AutoMapper + 手动映射）

**描述**: 简单映射用AutoMapper，复杂映射用手动编写

**优点**:
- ✅ 兼顾性能和开发效率

**缺点**:
- ❌ **不统一**：不同Service使用不同映射方式，增加认知负担
- ❌ **维护混乱**：难以确定何时使用AutoMapper，何时手动映射

**为什么未采纳**: 不统一的映射方式会导致长期维护混乱

---

## 🏗️ 架构例外（Architecture Exceptions）

**无架构例外**：AutoMapper符合三层架构原则，映射逻辑位于Application层，职责清晰。

---

## 📚 参考资料（References）

- **官方文档**: [AutoMapper Documentation](https://docs.automapper.org/)
- **NuGet包**:
  - `AutoMapper` (12.x)
  - `AutoMapper.Extensions.Microsoft.DependencyInjection` (12.x)
- **架构文档**: `docs/explanation/architecture/server/README.md`
- **业务规则**: `docs/explanation/business-rules.md`
- **代码位置**: `src/LYBT.Application/Mappings/`

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 基础设施搭建（已完成）
- [x] 引入AutoMapper NuGet包
- [x] 配置依赖注入（Program.cs）
- [x] 创建Mappings目录结构

### Phase 2: 核心模块MappingProfile实现（已完成）
- [x] PatientMappingProfile（Patient、CreatePatientDto、UpdatePatientDto、PatientDto）
- [x] MedicalCaseMappingProfile
- [x] ConsultationMappingProfile
- [x] PrescriptionMappingProfile
- [x] HerbMappingProfile
- [x] FormulaMappingProfile
- [x] UserMappingProfile

### Phase 3: 映射配置单元测试（部分完成）
- [x] AutoMapper配置验证测试（`_mapper.ConfigurationProvider.AssertConfigurationIsValid()`）
- [ ] 复杂映射逻辑单元测试（待补充）

### Phase 4: 文档和规范（本ADR）
- [x] 创建ADR-002记录技术选型
- [ ] 编写MappingProfile编写规范文档

---

## ✅ 验收标准（Acceptance Criteria）

- [x] AutoMapper已集成到依赖注入容器
- [x] 所有核心模块Entity/DTO都有对应MappingProfile
- [x] Service层使用`IMapper`接口进行映射
- [x] 编译通过（0 errors, 0 warnings）
- [x] AutoMapper配置验证通过（`AssertConfigurationIsValid()`）
- [ ] 映射逻辑单元测试覆盖率 ≥60%（待补充）

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-26 | v1.0 | 追溯创建ADR-002，记录AutoMapper选型决策 | Claude/项目团队 |

---

**创建者**: Claude Code（基于项目现状追溯记录）
**审核者**: 待人工审核
**批准者**: 项目架构团队（早期已批准，本ADR追溯记录）

---

## 💡 最佳实践建议

### MappingProfile编写规范

1. **命名规范**：`{模块名}MappingProfile`（如`PatientMappingProfile`）
2. **一个模块一个Profile**：避免一个Profile包含多个不相关模块的映射
3. **双向映射显式声明**：`CreateMap<A, B>()` + `CreateMap<B, A>()`分别声明
4. **复杂映射抽取方法**：如`CalculateAge`抽取为私有静态方法
5. **忽略字段显式声明**：使用`.ForMember(dest => dest.Id, opt => opt.Ignore())`明确忽略
6. **验证配置**：编写单元测试调用`AssertConfigurationIsValid()`

### 示例：完整的MappingProfile

```csharp
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // ===== Entity → DTO =====
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));

        CreateMap<Patient, PatientDetailDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)))
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => $"{src.Province}{src.City}{src.Address}"))
            .ForMember(dest => dest.MedicalCaseCount, opt => opt.MapFrom(src => src.MedicalCases.Count));

        // ===== DTO → Entity =====
        CreateMap<CreatePatientDto, Patient>()
            // Id、CreatedAt、UpdatedAt由系统自动生成，不需要映射
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCases, opt => opt.Ignore());

        CreateMap<UpdatePatientDto, Patient>()
            // 更新时，Id、CreatedAt不允许修改
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.MedicalCases, opt => opt.Ignore());
    }

    // 辅助方法：计算年龄
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

### 测试示例

```csharp
public class AutoMapperConfigurationTests
{
    private readonly IMapper _mapper;

    public AutoMapperConfigurationTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PatientMappingProfile>();
            cfg.AddProfile<MedicalCaseMappingProfile>();
            // ... 添加所有Profile
        });

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_ShouldBeValid()
    {
        // 验证所有映射配置正确
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_PatientToPatientDto_ShouldCalculateAge()
    {
        // Arrange
        var patient = new Patient
        {
            Id = 1,
            Name = "张三",
            BirthDate = new DateTime(1990, 1, 1)
        };

        // Act
        var dto = _mapper.Map<PatientDto>(patient);

        // Assert
        dto.Age.Should().Be(DateTime.Today.Year - 1990);
    }
}
```

### 性能优化建议

1. **避免过度映射**：不要在循环中频繁调用`Map<T>()`，应批量映射
   ```csharp
   // ❌ 错误：循环中逐个映射
   var dtos = new List<PatientDto>();
   foreach (var patient in patients)
   {
       dtos.Add(_mapper.Map<PatientDto>(patient));
   }

   // ✅ 正确：批量映射
   var dtos = _mapper.Map<List<PatientDto>>(patients);
   ```

2. **使用ProjectTo进行查询投影**：EF Core查询时直接投影为DTO，减少内存占用
   ```csharp
   // ❌ 错误：先查询Entity再映射
   var patients = await _context.Patients.ToListAsync();
   var dtos = _mapper.Map<List<PatientDto>>(patients);

   // ✅ 正确：直接投影为DTO
   var dtos = await _context.Patients
       .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
       .ToListAsync();
   ```

3. **缓存映射配置**：单例注册`IMapper`，避免重复创建配置
   ```csharp
   // ✅ 正确：依赖注入单例IMapper
   services.AddAutoMapper(typeof(PatientMappingProfile).Assembly);
   ```

---

## 🔍 常见问题（FAQ）

### Q1: AutoMapper vs Mapster，如何选择？

**回答**：
- **AutoMapper**：社区大、文档全、生态成熟，适合大型项目和团队协作
- **Mapster**：性能优、API简洁，适合性能敏感场景和小型项目
- **本项目选择AutoMapper**：基于社区成熟度和团队熟悉度

### Q2: 何时应该使用手动映射而非AutoMapper？

**回答**：
- **极端性能场景**：如高频API（>10k QPS），可考虑手动映射
- **复杂业务逻辑**：映射规则极其复杂时，手动映射可能更清晰
- **本项目场景**：目前无极端性能需求，统一使用AutoMapper

### Q3: 如何处理循环引用？

**回答**：
- **问题**：Entity之间存在循环引用（如`Patient.MedicalCases`和`MedicalCase.Patient`）
- **解决方案**：在MappingProfile中忽略导航属性，或使用`MaxDepth()`限制深度
  ```csharp
  CreateMap<Patient, PatientDto>()
      .ForMember(dest => dest.MedicalCases, opt => opt.Ignore()); // 忽略导航属性
  ```
