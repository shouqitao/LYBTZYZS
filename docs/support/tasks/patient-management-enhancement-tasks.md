# 患者管理功能完善 - 任务分解文档

## 📋 元数据

| 属性 | 值 |
|-----|-----|
| **Epic** | TBD（待创建） |
| **设计文档** | [docs/explanation/patient-management-enhancement-design.md](../explanation/patient-management-enhancement-design.md) v1.0 |
| **需求文档** | [docs/explanation/patient-management-enhancement-requirements.md](../explanation/patient-management-enhancement-requirements.md) v1.0 |
| **总工作量** | 8-12天（64-96小时） |
| **实施阶段** | Phase 1-4 |
| **任务总数** | 20个 |
| **创建日期** | 2025-11-09 |
| **状态** | ✅ 待审查 |

---

## 🎯 任务清单（Task Checklist）

### Phase 1: 基础架构与数据模型（预计16-24小时，2-3天）

**目标**：建立批量导入/导出的基础设施
**依赖**：无
**优先级**：🔴 P0（关键路径）

---

#### Task 1.1: 创建批量导入相关DTOs

**工作量**：2-3小时
**依赖**：无
**类型**：DTO设计
**优先级**：🔴 P0

**文件范围**：
- `src/Shared/LYBT.Shared.DTOs/Patients/BatchImportResultDto.cs`（新建）
- `src/Shared/LYBT.Shared.DTOs/Patients/ImportFailureDetailDto.cs`（新建）
- `src/Shared/LYBT.Shared.DTOs/Patients/ExportTemplateDto.cs`（新建）

**实现要点**：
1. **BatchImportResultDto**：
   - SuccessCount（成功数量）
   - FailureCount（失败数量）
   - SkippedCount（跳过数量）
   - Failures（List<ImportFailureDetailDto>）
   - ImportTime（DateTime）

2. **ImportFailureDetailDto**：
   - OriginalRowNumber（Excel原始行号）
   - FailureReason（失败原因）
   - FieldName（失败字段）
   - OriginalValue（原始值）
   - SuggestedFix（修复建议）
   - DataSnapshot（PatientInputDto）

3. **ExportTemplateDto**：
   - IncludeSampleData（bool）
   - SampleRowCount（int，默认3）

**验收标准**：
- [ ] 所有DTO类编译通过（0 errors, 0 warnings）
- [ ] 所有属性包含中文注释和DisplayName特性
- [ ] 数据注解（如StringLength、Range）正确配置
- [ ] DTO类位于正确的命名空间

**技术要点**：
- 使用System.ComponentModel.DataAnnotations命名空间
- 所有字段包含中文注释说明用途
- 符合Shared.DTOs项目结构规范

---

#### Task 1.2: 创建PatientInputDto统一输入DTO

**工作量**：1.5-2小时
**依赖**：无
**类型**：DTO设计
**优先级**：🔴 P0

**文件范围**：
- `src/Shared/LYBT.Shared.DTOs/Patients/PatientInputDto.cs`（新建）

**实现要点**：
1. **Epic #1736 InputDto统一模式**：
   - Id字段可选（Guid?）
   - 创建时Id为null，更新时Id有值
   - 所有业务字段包含DataAnnotations
   - 必填字段：Name, Gender, DateOfBirth, PhoneNumber
   - 可选字段：IdNumber, Address, Allergies, MedicalHistory

2. **字段定义**：
   ```csharp
   public Guid? Id { get; set; }
   [Required] [StringLength(50)] public string Name { get; set; }
   [Required] public Gender Gender { get; set; }
   [Required] public DateTime DateOfBirth { get; set; }
   [Required] [Phone] [StringLength(20)] public string PhoneNumber { get; set; }
   [StringLength(18)] public string? IdNumber { get; set; }
   [StringLength(200)] public string? Address { get; set; }
   [StringLength(500)] public string? Allergies { get; set; }
   [StringLength(1000)] public string? MedicalHistory { get; set; }
   ```

**验收标准**：
- [ ] PatientInputDto编译通过
- [ ] 所有必填字段标记[Required]
- [ ] 字符串长度限制符合数据库Schema
- [ ] 电话号码字段使用[Phone]特性
- [ ] 所有字段包含DisplayName和中文注释

**技术要点**：
- 遵循Epic #1736 InputDto统一模式
- Id字段为Guid?类型（可空）
- Gender使用枚举类型（非字符串）

---

#### Task 1.3: 创建PatientInputDtoValidator

**工作量**：3-4小时
**依赖**：Task 1.2
**类型**：Validator实现
**优先级**：🔴 P0

**文件范围**：
- `src/Shared/LYBT.Shared.Validators/Patients/PatientInputDtoValidator.cs`（新建）
- `tests/UnitTests/Shared/Validators/PatientInputDtoValidatorTests.cs`（新建）

**实现要点**：
1. **Epic #1773 Validators共享原则**：
   - 使用FluentValidation框架
   - 前后端共享验证规则
   - 一次定义、两端使用

2. **BR-001 8个验证点**：
   - 姓名验证：非空、长度≤50、中文姓名格式
   - 性别验证：枚举值有效性
   - 出生日期验证：非空、≤当前日期、年龄0-150岁
   - 手机号验证：非空、11位正则格式`^1[3-9]\d{9}$`
   - 身份证号验证（可选）：长度18、格式正则、校验位算法
   - 地址验证（可选）：长度≤200
   - 过敏史验证（可选）：长度≤500
   - 既往病史验证（可选）：长度≤1000

3. **验证器实现示例**：
   ```csharp
   RuleFor(x => x.Name)
       .NotEmpty().WithMessage("患者姓名不能为空")
       .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符")
       .Must(BeValidChineseName).WithMessage("姓名格式不正确，请输入中文姓名");

   RuleFor(x => x.PhoneNumber)
       .NotEmpty().WithMessage("手机号不能为空")
       .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确，请输入11位有效手机号");
   ```

**验收标准**：
- [ ] PatientInputDtoValidator编译通过
- [ ] 8个验证点全部覆盖
- [ ] 单元测试通过（至少20个测试用例）
- [ ] 测试覆盖所有验证规则（成功+失败场景）
- [ ] 中文错误消息清晰准确

**技术要点**：
- 身份证号校验位算法（GB 11643-1999）
- 中文姓名正则：`^[\u4e00-\u9fa5·]{2,50}$`
- When条件验证（可选字段仅在有值时验证）

---

#### Task 1.4: 扩展IPatientRepository接口

**工作量**：1-1.5小时
**依赖**：无
**类型**：Repository接口
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Repositories/IPatientRepository.cs`（修改）

**实现要点**：
在现有IPatientRepository接口中新增3个方法：

```csharp
/// <summary>
/// BR-004：检查手机号是否已存在
/// </summary>
Task<bool> ExistsByPhoneAsync(string phoneNumber);

/// <summary>
/// FR-002：获取所有患者（批量导出）
/// </summary>
Task<List<Patient>> GetAllAsync(int maxCount = 10000);

/// <summary>
/// 获取患者总数
/// </summary>
Task<int> GetTotalCountAsync();
```

**验收标准**：
- [ ] 接口方法签名正确
- [ ] 所有方法包含中文XML注释
- [ ] 标注对应的需求编号（如FR-002、BR-004）
- [ ] 编译通过（0 errors, 0 warnings）

**技术要点**：
- 所有方法返回Task（异步）
- maxCount参数提供默认值
- 方法命名遵循Async后缀规范

---

#### Task 1.5: 实现PatientRepository新方法

**工作量**：2-3小时
**依赖**：Task 1.4
**类型**：Repository实现
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Repositories/PatientRepository.cs`（修改）
- `tests/UnitTests/Server/Modules/Patients/PatientRepositoryTests.cs`（新建）

**实现要点**：
1. **ExistsByPhoneAsync实现**：
   ```csharp
   public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
   {
       return await _context.Patients
           .AnyAsync(p => p.PhoneNumber == phoneNumber);
   }
   ```

2. **GetAllAsync实现**：
   ```csharp
   public async Task<List<Patient>> GetAllAsync(int maxCount = 10000)
   {
       return await _context.Patients
           .OrderBy(p => p.CreatedAt)
           .Take(maxCount)
           .ToListAsync();
   }
   ```

3. **GetTotalCountAsync实现**：
   ```csharp
   public async Task<int> GetTotalCountAsync()
   {
       return await _context.Patients.CountAsync();
   }
   ```

**验收标准**：
- [ ] 所有方法实现正确
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 单元测试通过（Mock DbContext）
- [ ] 测试覆盖3个新方法
- [ ] Epic #1600约束：Repository类internal可见性

**技术要点**：
- 使用EF Core异步查询方法（AnyAsync, ToListAsync, CountAsync）
- GetAllAsync使用Take限制返回数量
- 单元测试使用InMemory数据库或Mock DbContext

---

#### Task 1.6: 配置AutoMapper映射规则

**工作量**：1.5-2小时
**依赖**：Task 1.2
**类型**：Configuration
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/MappingProfiles/PatientMappingProfile.cs`（修改）
- `tests/UnitTests/Server/Modules/Patients/PatientMappingProfileTests.cs`（新建）

**实现要点**：
在现有PatientMappingProfile中新增映射配置：

```csharp
// PatientInputDto → Patient（创建时）
CreateMap<PatientInputDto, Patient>()
    .ForMember(dest => dest.Id, opt => opt.Ignore()) // Service层生成ID
    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

// PatientInputDto → Patient（更新时，仅映射业务属性）
CreateMap<PatientInputDto, Patient>()
    .ForMember(dest => dest.Id, opt => opt.Ignore()) // 保持原ID不变
    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))
    .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
```

**验收标准**：
- [ ] AutoMapper配置编译通过
- [ ] AutoMapper.AssertConfigurationIsValid()测试通过
- [ ] 单元测试验证映射正确性
- [ ] Id、CreatedAt、UpdatedAt字段映射规则正确

**技术要点**：
- Epic #1736规范：Id字段由Service层管理，Mapper不处理
- 更新时UpdatedAt自动设置为当前时间
- 审计字段（CreatedAt、IsDeleted）不映射

---

#### Task 1.7: 添加EPPlus NuGet包依赖

**工作量**：0.5-1小时
**依赖**：无
**类型**：Infrastructure
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/LYBT.Server.Patients.csproj`（修改）
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj`（修改）

**实现要点**：
1. **Server端添加EPPlus**：
   ```xml
   <PackageReference Include="EPPlus" Version="7.0.0" />
   ```

2. **Client端添加EPPlus**（如果需要本地Excel生成）：
   ```xml
   <PackageReference Include="EPPlus" Version="7.0.0" />
   ```

3. **许可证配置**：
   ```csharp
   // 在Startup.cs或Program.cs中配置
   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
   ```

**验收标准**：
- [ ] NuGet包成功安装
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] EPPlus.LicenseContext配置正确
- [ ] 符合MVP Constitution（EPPlus MIT许可，允许使用）

**技术要点**：
- EPPlus 7.x版本（MIT许可）
- 在应用启动时配置LicenseContext
- 非商业使用设置为NonCommercial

---

### Phase 2: Server端业务逻辑（预计24-32小时，3-4天）

**目标**：实现完整的批量导入/导出API
**依赖**：Phase 1完成
**优先级**：🔴 P0（关键路径）

---

#### Task 2.1: 实现IPatientService.BatchImportAsync

**工作量**：6-8小时
**依赖**：Task 1.5, Task 1.6, Task 1.7
**类型**：Service核心业务逻辑
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Services/PatientService.cs`（修改）
- `tests/UnitTests/Server/Modules/Patients/PatientServiceTests.cs`（新建）

**实现要点**：
1. **核心流程**：
   - 使用EPPlus读取Excel文件
   - 跳过标题行（第1行）
   - 逐行解析为PatientInputDto
   - 使用PatientInputDtoValidator验证（8个验证点）
   - BR-004：手机号重复性检查（ExistsByPhoneAsync）
   - BR-002：部分成功模式（失败不中断流程）
   - BR-003：最多1000行限制
   - 失败详情生成（原始行号+修复建议）
   - 批量保存成功记录（一次SaveChanges）

2. **Excel列映射**：
   - A列：姓名
   - B列：性别
   - C列：出生日期
   - D列：手机号
   - E列：身份证号
   - F列：地址
   - G列：过敏史
   - H列：既往病史

3. **失败详情生成**：
   ```csharp
   new ImportFailureDetailDto
   {
       OriginalRowNumber = rowIndex,
       FailureReason = firstError.ErrorMessage,
       FieldName = firstError.PropertyName,
       OriginalValue = firstError.AttemptedValue?.ToString() ?? string.Empty,
       SuggestedFix = GetSuggestedFix(firstError.PropertyName, firstError.ErrorMessage),
       DataSnapshot = input
   };
   ```

4. **修复建议生成**：
   ```csharp
   private string GetSuggestedFix(string fieldName, string errorMessage)
   {
       return fieldName switch
       {
           nameof(PatientInputDto.Name) => "请输入2-50个中文字符的姓名",
           nameof(PatientInputDto.PhoneNumber) => "请输入11位有效手机号，如：13800138000",
           nameof(PatientInputDto.IdNumber) => "请输入18位有效身份证号",
           nameof(PatientInputDto.DateOfBirth) => "请输入有效日期，格式：yyyy-MM-dd",
           _ => "请检查数据格式"
       };
   }
   ```

**验收标准**：
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 单元测试通过（Mock Repository和Validator）
- [ ] 测试场景覆盖：
  - [ ] 100条全部成功
  - [ ] 100条部分成功（70成功+30失败）
  - [ ] 重复数据跳过（10条重复）
  - [ ] 超过1000行限制（返回错误）
  - [ ] 空文件/无数据行
- [ ] 性能测试：1000条导入 ≤ 30秒

**技术要点**：
- 使用EPPlus ExcelPackage类
- 异步I/O操作（async/await）
- 事务管理（UnitOfWork.SaveChangesAsync）
- 异常处理和日志记录

---

#### Task 2.2: 实现IPatientService.BatchExportAsync

**工作量**：3-4小时
**依赖**：Task 1.5, Task 1.7
**类型**：Service业务逻辑
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Services/PatientService.cs`（修改）

**实现要点**：
1. **核心流程**：
   - 调用Repository.GetAllAsync（最多10000条）
   - 使用EPPlus创建Excel
   - 生成标题行（8列）
   - 遍历患者数据，填充数据行
   - 性别枚举转中文（Male→男，Female→女）
   - 日期格式化（yyyy-MM-dd）
   - 自适应列宽（AutoFitColumns）
   - 返回byte[]

2. **Excel结构**：
   - 标题行：姓名、性别、出生日期、手机号、身份证号、地址、过敏史、既往病史
   - 标题行加粗（Font.Bold = true）
   - 数据行从第2行开始

**验收标准**：
- [ ] 编译通过
- [ ] 单元测试通过
- [ ] 导出Excel文件格式正确
- [ ] 日期和性别格式化正确
- [ ] 性能测试：10000条导出 ≤ 60秒

**技术要点**：
- EPPlus Worksheet操作
- 枚举转中文显示
- 列宽自适应算法

---

#### Task 2.3: 实现IPatientService.ExportFailuresAsync

**工作量**：2-3小时
**依赖**：Task 1.7
**类型**：Service业务逻辑
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Services/PatientService.cs`（修改）

**实现要点**：
1. **Excel结构**（BR-002步骤3）：
   - 前5列：失败信息列（行号、失败原因、失败字段、原始值、修复建议）
   - 后8列：数据快照列（姓名、性别、出生日期、手机号、身份证号、地址、过敏史、既往病史）
   - 标题行背景色：浅灰色
   - 失败原因列背景色：浅黄色（高亮）

2. **核心流程**：
   - 遍历ImportFailureDetailDto列表
   - 填充失败信息列
   - 填充数据快照列
   - 应用样式（背景色、加粗）
   - 自适应列宽

**验收标准**：
- [ ] 编译通过
- [ ] 单元测试通过
- [ ] Excel格式符合BR-002规范
- [ ] 包含原始行号（快速定位）
- [ ] 包含修复建议（可操作）

**技术要点**：
- EPPlus样式设置（Fill.BackgroundColor）
- 13列布局设计

---

#### Task 2.4: 实现IPatientService.ExportTemplateAsync

**工作量**：2-3小时
**依赖**：Task 1.7
**类型**：Service业务逻辑
**优先级**：🟡 P1

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Services/PatientService.cs`（修改）

**实现要点**：
1. **Excel结构**：
   - 标题行：姓名*、性别*、出生日期*、手机号*、身份证号、地址、过敏史、既往病史
   - 标题行背景色：浅蓝色
   - 示例数据行（3行）：背景色浅黄色
   - 说明行（6行）：填写规则说明

2. **示例数据**：
   ```
   张三, 男, 1990-01-01, 13800138000, 110101199001011234, 北京市朝阳区, 青霉素过敏, 高血压
   李四, 女, 1985-05-15, 13900139000, 110101198505150012, 上海市浦东新区, 无, 糖尿病
   王五, 男, 2000-12-20, 13700137000, , 广东省深圳市, 海鲜过敏,
   ```

**验收标准**：
- [ ] 编译通过
- [ ] Excel模板格式友好
- [ ] 包含3行示例数据
- [ ] 包含填写说明

**技术要点**：
- 可配置示例行数（1-10）
- 必填列标记*号

---

#### Task 2.5: 实现PatientsController新端点

**工作量**：4-5小时
**依赖**：Task 2.1, Task 2.2, Task 2.3, Task 2.4
**类型**：Controller实现
**优先级**：🔴 P0

**文件范围**：
- `src/Server/Modules/LYBT.Server.Patients/Controllers/PatientsController.cs`（修改）

**实现要点**：
新增4个API端点：

1. **POST /api/patients/batch-import**：
   - 接收IFormFile（.xlsx文件）
   - 文件大小限制：10MB
   - 文件类型验证：仅.xlsx
   - 返回BatchImportResultDto

2. **GET /api/patients/batch-export**：
   - 无参数（导出所有患者）
   - 返回FileContentResult（Excel文件）

3. **POST /api/patients/export-failures**：
   - 接收List<ImportFailureDetailDto>（失败列表）
   - 返回FileContentResult（Excel文件）

4. **GET /api/patients/export-template**：
   - 查询参数：includeSampleData（bool），sampleRowCount（int）
   - 返回FileContentResult（Excel模板）

**验收标准**：
- [ ] 编译通过
- [ ] Swagger文档生成正确
- [ ] 所有端点包含中文注释和XML文档
- [ ] 参数验证正确（文件类型、大小）
- [ ] 异常处理和日志记录
- [ ] [Authorize]特性正确配置

**技术要点**：
- [Consumes("multipart/form-data")]特性
- FileContentResult返回类型
- MIME类型：application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

---

#### Task 2.6: 定义IPatientApi接口

**工作量**：1-1.5小时
**依赖**：Task 2.5
**类型**：API接口定义
**优先级**：🔴 P0

**文件范围**：
- `src/Shared/LYBT.Shared.APIs/IPatientApi.cs`（修改）

**实现要点**：
在现有IPatientApi接口中新增4个方法：

```csharp
[Multipart]
[Post("/api/patients/batch-import")]
Task<BatchImportResultDto> BatchImportAsync([AliasAs("file")] StreamPart stream);

[Get("/api/patients/batch-export")]
Task<byte[]> BatchExportAsync();

[Post("/api/patients/export-failures")]
Task<byte[]> ExportFailuresAsync([Body] List<ImportFailureDetailDto> failures);

[Get("/api/patients/export-template")]
Task<byte[]> ExportTemplateAsync([Query] bool includeSampleData = true, [Query] int sampleRowCount = 3);
```

**验收标准**：
- [ ] 接口方法签名正确
- [ ] Refit特性正确配置
- [ ] 编译通过

**技术要点**：
- Refit StreamPart用于文件上传
- [Multipart]特性标记文件上传
- [Body]和[Query]特性正确使用

---

#### Task 2.7: 实现PatientApi (Refit)

**工作量**：1-1.5小时
**依赖**：Task 2.6
**类型**：Client API实现
**优先级**：🔴 P0

**文件范围**：
- `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/APIs/PatientApi.cs`（修改）

**实现要点**：
使用Refit自动生成的实现，无需手动编写代码。仅需在DI容器中注册：

```csharp
services.AddRefitClient<IPatientApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration["ApiBaseUrl"]))
    .AddHttpMessageHandler<AuthenticationHandler>();
```

**验收标准**：
- [ ] Refit客户端注册正确
- [ ] HttpClient配置正确（BaseAddress）
- [ ] AuthenticationHandler添加（如果需要认证）
- [ ] 编译通过

**技术要点**：
- Refit.HttpClientFactory集成
- HttpClient配置和Handler链

---

### Phase 3: Client端UI集成（预计16-24小时，2-3天）

**目标**：完成用户界面和交互流程
**依赖**：Phase 2完成
**优先级**：🟡 P1

---

#### Task 3.1: 扩展PatientManagementViewModel

**工作量**：5-6小时
**依赖**：Task 2.7
**类型**：ViewModel实现
**优先级**：🟡 P1

**文件范围**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientManagementViewModel.cs`（修改）

**实现要点**：
1. **新增Command**：
   ```csharp
   public DelegateCommand ImportPatientsCommand { get; }
   public DelegateCommand ExportTemplateCommand { get; }
   public DelegateCommand ExportPatientsCommand { get; }
   ```

2. **ImportPatientsCommand实现**（FR-001）：
   - 调用IFileDialogService.OpenFileDialog选择Excel文件
   - 调用IPatientApi.BatchImportAsync上传文件
   - BR-002步骤2：显示ImportResultDialog
   - 失败时提示用户导出失败数据
   - 成功时刷新列表

3. **ExportTemplateCommand实现**（FR-002）：
   - 调用IFileDialogService.SaveFileDialog选择保存路径
   - 调用IPatientApi.ExportTemplateAsync下载模板
   - 保存文件到本地
   - 显示成功提示

4. **ExportPatientsCommand实现**（FR-002）：
   - 调用IFileDialogService.SaveFileDialog
   - 调用IPatientApi.BatchExportAsync
   - 保存文件
   - 显示导出成功提示

5. **ExportFailuresAsync方法**（BR-002步骤3）：
   - 接收失败详情列表
   - 调用IPatientApi.ExportFailuresAsync
   - 保存失败数据Excel
   - 提示用户修复后重新导入

**验收标准**：
- [ ] 编译通过
- [ ] 所有Command可正常触发
- [ ] 文件对话框正常显示
- [ ] API调用成功
- [ ] 异常处理正确
- [ ] Loading状态显示

**技术要点**：
- 依赖注入IPatientApi、IDialogService、IFileDialogService
- async/await异步处理
- try-catch异常处理
- Loading状态管理

---

#### Task 3.2: 创建ImportResultDialog

**工作量**：4-5小时
**依赖**：无
**类型**：Dialog实现
**优先级**：🟡 P1

**文件范围**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/ImportResultDialog.xaml`（新建）
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/ImportResultDialog.xaml.cs`（新建）
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/ImportResultDialogViewModel.cs`（新建）

**实现要点**：
1. **Dialog布局**（BR-002步骤2）：
   - 标题：导入结果
   - 成功/失败/跳过数量统计（卡片显示）
   - 失败详情列表（DataGrid）
   - 底部按钮：导出失败数据、关闭

2. **失败详情列表列**：
   - 行号
   - 失败原因
   - 失败字段
   - 原始值
   - 修复建议

3. **ViewModel绑定**：
   ```csharp
   public int SuccessCount { get; set; }
   public int FailureCount { get; set; }
   public int SkippedCount { get; set; }
   public ObservableCollection<ImportFailureDetailDto> Failures { get; set; }
   public DelegateCommand ExportFailuresCommand { get; }
   public DelegateCommand CloseCommand { get; }
   ```

**验收标准**：
- [ ] Dialog显示正确
- [ ] 数据绑定生效
- [ ] 失败详情列表滚动流畅
- [ ] 导出失败数据按钮可用
- [ ] 用户体验友好

**技术要点**：
- Prism DialogService集成
- IDialogAware接口实现
- MVVM数据绑定

---

#### Task 3.3: 创建IFileDialogService接口和实现

**工作量**：2-3小时
**依赖**：无
**类型**：Infrastructure服务
**优先级**：🟡 P1

**文件范围**：
- `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Services/IFileDialogService.cs`（新建）
- `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Services/FileDialogService.cs`（新建）

**实现要点**：
```csharp
public interface IFileDialogService
{
    string? OpenFileDialog(string title, string filter);
    string? SaveFileDialog(string title, string defaultFileName, string filter);
}

public class FileDialogService : IFileDialogService
{
    public string? OpenFileDialog(string title, string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = filter
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFileDialog(string title, string defaultFileName, string filter)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
```

**验收标准**：
- [ ] 编译通过
- [ ] 文件对话框正常显示
- [ ] 返回值正确（选择文件路径或null）
- [ ] DI容器注册

**技术要点**：
- Microsoft.Win32.OpenFileDialog
- Microsoft.Win32.SaveFileDialog

---

#### Task 3.4: 修改PatientManagementView.xaml

**工作量**：3-4小时
**依赖**：Task 3.1
**类型**：View实现
**优先级**：🟡 P1

**文件范围**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientManagementView.xaml`（修改）

**实现要点**：
1. **FR-006：工具栏新增3个按钮**：
   ```xaml
   <Button Content="📥 导入患者"
           Style="{StaticResource SecondaryButton}"
           Command="{Binding ImportPatientsCommand}"
           ToolTip="从Excel文件批量导入患者" />
   <Button Content="📄 导出模板"
           Style="{StaticResource InfoButton}"
           Command="{Binding ExportTemplateCommand}"
           ToolTip="下载患者导入模板（含示例数据）" />
   <Button Content="📤 导出患者"
           Style="{StaticResource WarningButton}"
           Command="{Binding ExportPatientsCommand}"
           ToolTip="导出患者数据到Excel文件" />
   ```

2. **FR-005：列表UI优化**：
   - 手机号和身份证号列改为自适应宽度（Width="*"）
   - 操作列扩展为3个按钮（查看、编辑、删除）
   - 操作列按钮右对齐（HorizontalAlignment="Right"）
   - Margin调整（Margin="0,0,20,0"）

**验收标准**：
- [ ] 编译通过
- [ ] 工具栏3个新按钮显示正确
- [ ] 按钮样式符合设计（图标+文字）
- [ ] 列表列宽自适应窗口
- [ ] 操作列3个按钮右对齐
- [ ] 响应式布局流畅

**技术要点**：
- UnifiedManagementToolBar控件使用
- UnifiedManagementTable控件使用
- XAML数据绑定

---

#### Task 3.5: 配置DI容器注册

**工作量**：1-1.5小时
**依赖**：Task 2.7, Task 3.3
**类型**：Configuration
**优先级**：🟡 P1

**文件范围**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`（修改）
- `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/InfrastructureModule.cs`（修改）

**实现要点**：
1. **InfrastructureModule注册**：
   ```csharp
   containerRegistry.RegisterSingleton<IFileDialogService, FileDialogService>();
   ```

2. **PatientsModule注册**（如果需要）：
   ```csharp
   // IPatientApi已在Infrastructure全局注册，无需重复
   ```

3. **验证注册**：
   - 启动时检查所有依赖是否正确解析
   - 无循环依赖

**验收标准**：
- [ ] 编译通过
- [ ] 应用启动无DI异常
- [ ] IFileDialogService可正常注入
- [ ] IPatientApi可正常注入

**技术要点**：
- Prism Unity Container
- Singleton vs Transient生命周期

---

### Phase 4: 质量保障与文档（预计8-16小时，1-2天）

**目标**：确保代码质量和文档完整
**依赖**：Phase 3完成
**优先级**：🟢 P2

---

#### Task 4.1: 编写单元测试

**工作量**：4-6小时
**依赖**：Phase 1-3完成
**类型**：UnitTest
**优先级**：🟢 P2

**文件范围**：
- `tests/UnitTests/Server/Modules/Patients/PatientServiceTests.cs`（扩展）
- `tests/UnitTests/Server/Modules/Patients/PatientRepositoryTests.cs`（扩展）
- `tests/UnitTests/Shared/Validators/PatientInputDtoValidatorTests.cs`（已在Task 1.3创建）

**实现要点**：
1. **PatientServiceTests**（重点测试BatchImportAsync）：
   - 测试用例1：100条数据全部成功
   - 测试用例2：100条部分成功（70成功+30失败）
   - 测试用例3：重复数据跳过（10条重复）
   - 测试用例4：超过1000行限制
   - 测试用例5：空文件
   - 测试用例6：无效Excel格式

2. **PatientRepositoryTests**（已在Task 1.5部分完成）：
   - ExistsByPhoneAsync测试
   - GetAllAsync测试
   - GetTotalCountAsync测试

**验收标准**：
- [ ] 代码覆盖率 ≥ 80%
- [ ] 所有测试用例通过
- [ ] AAA模式（Arrange-Act-Assert）
- [ ] Mock对象正确使用（NSubstitute）

**技术要点**：
- xUnit测试框架
- NSubstitute Mock框架
- InMemory数据库或Mock DbContext

---

#### Task 4.2: 编写集成测试

**工作量**：3-4小时
**依赖**：Phase 2完成
**类型**：IntegrationTest
**优先级**：🟢 P2

**文件范围**：
- `tests/IntegrationTests/API/PatientsControllerTests.cs`（新建）
- `tests/IntegrationTests/E2E/PatientBatchImportE2ETests.cs`（新建）

**实现要点**：
1. **PatientsControllerTests**：
   - POST /api/patients/batch-import集成测试
   - GET /api/patients/batch-export集成测试
   - POST /api/patients/export-failures集成测试

2. **PatientBatchImportE2ETests**：
   - 完整导入流程：上传Excel → 验证结果 → 导出失败数据 → 修复 → 重新导入

**验收标准**：
- [ ] 所有集成测试通过
- [ ] 使用真实数据库（TestContainers或InMemory）
- [ ] WebApplicationFactory配置正确

**技术要点**：
- WebApplicationFactory
- HttpClient API调用
- MultipartFormDataContent文件上传

---

#### Task 4.3: 更新文档

**工作量**：2-3小时
**依赖**：Phase 1-3完成
**类型**：Documentation
**优先级**：🟢 P2

**文件范围**：
- `docs/how-to/patient-management.md`（更新）
- `docs/reference/api/patients-api.md`（更新）
- `docs/how-to/batch-import-patients.md`（新建）

**实现要点**：
1. **patient-management.md更新**：
   - 新增批量导入章节
   - 新增批量导出章节
   - 更新操作流程

2. **patients-api.md更新**：
   - 新增4个API端点文档
   - 请求/响应示例
   - 错误码说明

3. **batch-import-patients.md新建**（用户指南）：
   - 导入前准备（下载模板）
   - 填写Excel数据
   - 上传导入
   - 查看导入结果
   - 失败数据修复流程（BR-002 6步）

**验收标准**：
- [ ] 文档更新完整
- [ ] 示例代码正确
- [ ] 截图清晰（如果有）
- [ ] lybtzyzs-doc-sync检查通过

**技术要点**：
- Markdown格式规范
- Diátaxis文档框架
- 用户友好的写作风格

---

#### Task 4.4: 性能测试

**工作量**：2-3小时
**依赖**：Phase 2完成
**类型**：PerformanceTest
**优先级**：🟢 P2

**文件范围**：
- `tests/PerformanceTests/PatientBatchImportPerformanceTests.cs`（新建）

**实现要点**：
1. **性能测试用例**：
   - 1000条数据导入性能测试（目标：≤ 30秒）
   - 10000条数据导出性能测试（目标：≤ 60秒）
   - 并发导入测试（5个用户同时导入100条）

2. **性能指标收集**：
   - 执行时间
   - 内存占用
   - CPU使用率

**验收标准**：
- [ ] 1000条导入 ≤ 30秒
- [ ] 10000条导出 ≤ 60秒
- [ ] 并发测试无数据冲突

**技术要点**：
- BenchmarkDotNet框架
- Stopwatch计时
- 性能数据可视化

---

#### Task 4.5: 质量检查

**工作量**：1-2小时
**依赖**：Phase 1-4所有任务完成
**类型**：QualityCheck
**优先级**：🟢 P2

**文件范围**：
- 无（运行Skills工具）

**实现要点**：
1. **lybtzyzs-arch-compliance检查**：
   - 三层架构职责划分
   - 依赖方向正确
   - Repository内部可见性
   - Service直接实现接口

2. **lybtzyzs-mvp-compliance检查**：
   - 无禁用技术（Redis、CQRS等）
   - EPPlus许可证合规

3. **lybtzyzs-quality-reporter生成质量报告**：
   - 编译结果
   - 测试覆盖率
   - 代码质量评分
   - 自动合并决策建议

**验收标准**：
- [ ] lybtzyzs-arch-compliance检查通过
- [ ] lybtzyzs-mvp-compliance检查通过
- [ ] 质量报告评分 ≥ 85分
- [ ] 所有验收标准达标

**技术要点**：
- 调用lybtzyzs-* Skills
- 解读质量报告
- 根据建议优化代码

---

## 📊 任务统计

| 统计项 | 数值 |
|-------|------|
| **总任务数** | 20个 |
| **总工作量** | 64-96小时（8-12天） |
| **Phase数量** | 4个阶段 |
| **关键路径长度** | 8个任务 |
| **P0任务** | 14个（70%） |
| **P1任务** | 5个（25%） |
| **P2任务** | 1个（5%） |

### Phase工作量分布

| Phase | 任务数 | 工作量 | 占比 |
|-------|-------|--------|------|
| **Phase 1** | 7个 | 16-24小时 | 25% |
| **Phase 2** | 7个 | 24-32小时 | 38% |
| **Phase 3** | 5个 | 16-24小时 | 25% |
| **Phase 4** | 5个 | 8-16小时 | 12% |

---

## 🔗 依赖关系图

### Phase 1 内部依赖

```
Task 1.1 (无依赖) ──┐
Task 1.2 (无依赖) ──┼──→ Task 1.3 (依赖1.2)
Task 1.4 (无依赖) ──┤
                     ├──→ Task 1.5 (依赖1.4)
                     ├──→ Task 1.6 (依赖1.2)
Task 1.7 (无依赖) ───┘
```

**并行任务**：Task 1.1, 1.2, 1.4, 1.7可同时开始

---

### Phase 2 内部依赖

```
Task 2.1 (依赖1.5, 1.6, 1.7) ──┐
Task 2.2 (依赖1.5, 1.7) ────────┤
Task 2.3 (依赖1.7) ─────────────┼──→ Task 2.5 (依赖2.1-2.4) ──→ Task 2.6 ──→ Task 2.7
Task 2.4 (依赖1.7) ─────────────┘
```

**关键路径**：Task 2.1 → Task 2.5 → Task 2.6 → Task 2.7

---

### Phase 3 内部依赖

```
Task 3.1 (依赖2.7) ──┐
Task 3.2 (无依赖) ────┼──→ Task 3.4 (依赖3.1)
Task 3.3 (无依赖) ────┤
                      └──→ Task 3.5 (依赖2.7, 3.3)
```

**并行任务**：Task 3.2和Task 3.3可以在Phase 2期间提前开始

---

### 跨Phase依赖

```
Phase 1 (完成) ──→ Phase 2 (完成) ──→ Phase 3 (完成) ──→ Phase 4
    │                  │                  │
    └─ Task 1.5 ───────┼─ Task 2.1       │
    └─ Task 1.6 ───────┘                 │
    └─ Task 1.7 ──────────────────────────┘
```

---

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：

1. **Task 1.2**: 创建PatientInputDto（1.5-2小时）
2. **Task 1.3**: 创建PatientInputDtoValidator（3-4小时）
3. **Task 1.5**: 实现PatientRepository新方法（2-3小时）
4. **Task 1.6**: 配置AutoMapper映射规则（1.5-2小时）
5. **Task 2.1**: 实现IPatientService.BatchImportAsync（6-8小时）⚡ 最耗时
6. **Task 2.5**: 实现PatientsController新端点（4-5小时）
7. **Task 2.6**: 定义IPatientApi接口（1-1.5小时）
8. **Task 2.7**: 实现PatientApi（1-1.5小时）
9. **Task 3.1**: 扩展PatientManagementViewModel（5-6小时）⚡ 耗时
10. **Task 3.4**: 修改PatientManagementView.xaml（3-4小时）

**关键路径总工作量**：30-40小时（3.75-5天）

---

## 📝 实施建议

### 优先级排序

**🔴 高优先级（P0）**：关键路径任务，必须优先完成
- Phase 1: Task 1.1-1.7（全部）
- Phase 2: Task 2.1, 2.5, 2.6, 2.7（关键路径）

**🟡 中优先级（P1）**：功能完整性任务
- Phase 2: Task 2.2, 2.3, 2.4（导出功能）
- Phase 3: Task 3.1-3.5（UI集成）

**🟢 低优先级（P2）**：质量保障任务
- Phase 4: Task 4.1-4.5（测试和文档）

---

### 并行策略

**阶段1（Phase 1同时进行）**：
- 开发者A：Task 1.1 + Task 1.2 + Task 1.3
- 开发者B：Task 1.4 + Task 1.5
- 开发者C：Task 1.6 + Task 1.7

**阶段2（Phase 2同时进行）**：
- 开发者A：Task 2.1（核心业务逻辑，最耗时）
- 开发者B：Task 2.2 + Task 2.3 + Task 2.4（导出功能）
- 开发者C：Task 3.2 + Task 3.3（提前开始Phase 3准备工作）

**阶段3（Phase 3顺序进行）**：
- Task 2.5 → Task 2.6 → Task 2.7 → Task 3.1 → Task 3.4 → Task 3.5

---

### 风险提示

**⚠️ 高风险任务**：
- **Task 2.1（BatchImportAsync）**：
  - 风险：业务逻辑复杂，Excel解析可能遇到格式问题
  - 缓解：提前准备测试Excel文件，包含各种边界情况
  - 预留缓冲：建议预留2小时缓冲时间

- **Task 3.1（PatientManagementViewModel）**：
  - 风险：API调用失败处理、Dialog交互复杂
  - 缓解：提前Mock API测试ViewModel逻辑
  - 预留缓冲：建议预留1小时缓冲时间

**⚠️ 依赖风险**：
- Task 3.1需要Task 2.7的API可用，建议Phase 2完整测试后再开始Phase 3
- Task 2.5依赖Task 2.1-2.4的Service实现，建议完整实现Service后再开发Controller

---

### 里程碑规划

| 里程碑 | 完成时间 | 验收标准 |
|-------|---------|---------|
| **里程碑1** | Day 3 | Phase 1完成，所有DTO/Validator/Repository就绪 |
| **里程碑2** | Day 7 | Phase 2完成，API端点可用且Swagger文档正确 |
| **里程碑3** | Day 10 | Phase 3完成，用户可通过UI完成批量导入/导出 |
| **里程碑4** | Day 12 | Phase 4完成，质量报告评分≥85分 |

---

## 🧪 测试策略

### 单元测试

**Phase 1测试**：
- Task 1.3: PatientInputDtoValidatorTests（8个验证点，20+测试用例）
- Task 1.5: PatientRepositoryTests（3个新方法测试）
- Task 1.6: AutoMapper配置测试

**Phase 2测试**：
- Task 2.1: PatientServiceTests.BatchImportAsync（6个测试场景）
- Task 2.2-2.4: 导出功能单元测试

**目标覆盖率**：≥ 80%

---

### 集成测试

**Phase 2集成测试**：
- Task 4.2: PatientsControllerTests（API端点测试）
- 使用WebApplicationFactory
- 真实数据库或TestContainers

**Phase 3 E2E测试**：
- Task 4.2: PatientBatchImportE2ETests（完整流程测试）
- 模拟用户操作：上传 → 查看结果 → 导出失败 → 修复 → 重新导入

---

### 性能测试

**性能指标**（Task 4.4）：
- ✅ 1000条导入 ≤ 30秒
- ✅ 10000条导出 ≤ 60秒
- ✅ 并发导入无数据冲突

---

## 📦 交付物清单

### 代码交付物

**Shared层**：
- [ ] BatchImportResultDto.cs
- [ ] ImportFailureDetailDto.cs
- [ ] ExportTemplateDto.cs
- [ ] PatientInputDto.cs
- [ ] PatientInputDtoValidator.cs
- [ ] IPatientApi.cs (扩展)

**Server层**：
- [ ] PatientRepository.cs (扩展)
- [ ] PatientService.cs (扩展)
- [ ] PatientsController.cs (扩展)
- [ ] PatientMappingProfile.cs (扩展)

**Client层**：
- [ ] PatientManagementViewModel.cs (扩展)
- [ ] PatientManagementView.xaml (修改)
- [ ] ImportResultDialog.xaml (新建)
- [ ] ImportResultDialogViewModel.cs (新建)
- [ ] FileDialogService.cs (新建)
- [ ] PatientApi.cs (扩展)

**测试**：
- [ ] PatientInputDtoValidatorTests.cs
- [ ] PatientRepositoryTests.cs
- [ ] PatientServiceTests.cs
- [ ] PatientsControllerTests.cs
- [ ] PatientBatchImportE2ETests.cs
- [ ] PatientBatchImportPerformanceTests.cs

---

### 文档交付物

- [ ] docs/how-to/patient-management.md (更新)
- [ ] docs/how-to/batch-import-patients.md (新建)
- [ ] docs/reference/api/patients-api.md (更新)
- [ ] docs/tasks/patient-management-enhancement-tasks.md (本文档)

---

### 质量报告

- [ ] lybtzyzs-arch-compliance验证报告
- [ ] lybtzyzs-mvp-compliance验证报告
- [ ] lybtzyzs-quality-reporter质量报告（评分≥85分）
- [ ] 代码覆盖率报告（≥80%）
- [ ] 性能测试报告

---

## 💡 下一步操作

### 即将执行

1. ✅ **审查本task文档**：确认任务拆分合理、依赖关系正确
2. ⏭️ **调整任务粒度**（如果需要）：拆分过大任务（>6小时）或合并过小任务（<1小时）
3. ⏭️ **运行lybtzyzs-issue-template**：批量生成GitHub Issues（基于本task文档）
4. ⏭️ **开始Phase 1实施**：创建feature分支，按顺序完成Task 1.1-1.7

### 实施流程

```
审查task文档
  ↓
调整任务粒度（可选）
  ↓
批量生成GitHub Issues (lybtzyzs-issue-template)
  ↓
创建feature分支: feature/patient-management-enhancement
  ↓
Phase 1实施 (Task 1.1-1.7)
  ↓
Phase 1验收 (里程碑1)
  ↓
Phase 2实施 (Task 2.1-2.7)
  ↓
Phase 2验收 (里程碑2)
  ↓
Phase 3实施 (Task 3.1-3.5)
  ↓
Phase 3验收 (里程碑3)
  ↓
Phase 4质量保障 (Task 4.1-4.5)
  ↓
Phase 4验收 (里程碑4)
  ↓
创建Pull Request
  ↓
Code Review
  ↓
合并到master分支
```

---

## 📌 附录

### A. 任务编号规范

**格式**：`Task {Phase}.{Sequence}`

**示例**：
- Task 1.1：Phase 1的第1个任务
- Task 2.5：Phase 2的第5个任务

### B. 优先级定义

- **🔴 P0**：关键路径任务，阻塞后续任务，必须优先完成
- **🟡 P1**：重要功能任务，影响功能完整性，次优先完成
- **🟢 P2**：质量保障任务，不阻塞核心功能，最后完成

### C. 工作量估算说明

**估算基准**：
- 简单任务（1-1.5小时）：创建简单DTO、接口定义
- 中等任务（2-4小时）：Repository实现、Controller实现、ViewModel扩展
- 复杂任务（5-8小时）：核心Service业务逻辑、复杂UI实现

**区间估算**：X-Y小时表示乐观-悲观估算，实际工作量可能在此区间内

### D. 验收标准说明

所有任务的通用验收标准：
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 代码符合项目规范（命名、注释、编码）
- [ ] Git提交信息规范
- [ ] 无遗留TODO或HACK注释

---

**文档版本**：v1.0
**创建日期**：2025-11-09
**维护者**：Claude Code
**下一步**：调用lybtzyzs-issue-template批量生成GitHub Issues
