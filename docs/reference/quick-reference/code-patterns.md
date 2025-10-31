# 代码模式参考

**基于8个实际业务模块的完整代码模式** - 解决日常90%的代码开发需求

## 🔐 认证模块模式 (Auth Module)

### 双轨认证模式
```csharp
/// <summary>
/// 超级管理员认证 - 隐藏端点
/// 独立存储在AdminSecrets表，与Users表物理隔离
/// </summary>
private async Task<bool> IsSuperAdminCredentials(string username, string password, CancellationToken cancellationToken = default)
{
    // 从配置获取超级管理员用户名（不在数据库中存储）
    var configUsername = _configuration["Lybt:Business:SystemAdmin:Username"];
    if (!string.Equals(username, configUsername, StringComparison.OrdinalIgnoreCase))
        return false;

    // 从AdminSecrets表获取密码哈希
    var adminSecret = await _dbContext.AdminSecrets.FirstOrDefaultAsync(cancellationToken);
    if (adminSecret == null) return false;

    // 使用BCrypt验证密码
    return BCrypt.Net.BCrypt.Verify(password, adminSecret.PasswordHash);
}

/// <summary>
/// 普通用户认证模式
/// 直接使用Repository层，避免过度抽象
/// </summary>
public async Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
{
    // 优先检查超级管理员
    if (await IsSuperAdminCredentials(request.Username, request.Password, cancellationToken))
        return ServiceResult<string>.Success("SUPER_ADMIN:" + request.Username);

    // 普通用户认证
    var userEntity = await _userRepository.GetByUsernameAsync(request.Username);
    if (userEntity == null) return ServiceResult<string>.Failure("用户名或密码错误");

    // BCrypt密码验证
    if (BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
        return ServiceResult<string>.Success(userEntity.Id.ToString());
    
    return ServiceResult<string>.Failure("用户名或密码错误");
}
```

### JWT令牌生成模式
```csharp
/// <summary>
/// 登录响应生成模式
/// 超级管理员使用特殊ID和声明
/// </summary>
public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
{
    var credentialsResult = await VerifyCredentialsAsync(request, cancellationToken);
    if (!credentialsResult.IsSuccess) 
        return ServiceResult<LoginResponse>.Failure(credentialsResult.Message);

    // 检查是否是超级管理员
    if (credentialsResult.Data.StartsWith("SUPER_ADMIN:"))
    {
        var token = _jwtService.GenerateToken(
            "00000000-0000-0000-0000-000000000000", // 特殊ID
            sysAdminUsername,
            UserRole.Admin,
            new Dictionary<string, string>
            {
                { "IsSuperAdmin", "true" },
                { "AuthSource", "AdminSecrets" }
            });
        
        // 构建超级管理员登录响应
        // ...
    }
    else
    {
        // 普通用户登录流程
        var userEntity = await _userRepository.GetByUsernameAsync(request.Username);
        var token = _jwtService.GenerateToken(userDto.Id.ToString(), userDto.UserName, userDto.Role);
        
        // 构建普通用户登录响应
        // ...
    }
}
```

## 👥 用户管理模块模式 (Users Module)

### CRUD基础模式
```csharp
/// <summary>
/// 标准分页查询模式
/// </summary>
public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
{
    try
    {
        var pagedResult = await _repository.GetPagedAsync(page, pageSize);
        var dto = new PagedResult<UserDto>
        {
            Items = _mapper.Map<List<UserDto>>(pagedResult.Items),
            TotalCount = pagedResult.TotalCount,
            CurrentPage = pagedResult.CurrentPage,
            PageSize = pagedResult.PageSize
        };
        return ServiceResult<PagedResult<UserDto>>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取用户列表失败");
        return ServiceResult<PagedResult<UserDto>>.Failure("获取用户列表失败");
    }
}

/// <summary>
/// 标准创建模式
/// </summary>
public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
{
    try
    {
        var entity = _mapper.Map<User>(dto);
        var result = await _repository.AddAsync(entity);
        var resultDto = _mapper.Map<UserDto>(result);
        return ServiceResult<UserDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建用户失败");
        return ServiceResult<UserDto>.Failure("创建用户失败");
    }
}

/// <summary>
/// 标准更新模式
/// </summary>
public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
{
    try
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return ServiceResult<UserDto>.Failure("用户不存在");

        _mapper.Map(dto, entity);
        var result = await _repository.UpdateAsync(entity);
        var resultDto = _mapper.Map<UserDto>(result);
        return ServiceResult<UserDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新用户失败");
        return ServiceResult<UserDto>.Failure("更新用户失败");
    }
}
```

## 🏥 患者管理模块模式 (Patients Module)

### Excel批量导入模式
```csharp
/// <summary>
/// Excel导入通用模式
/// 使用EPPlus库处理Excel文件
/// </summary>
public async Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
{
    var result = new ImportResultDto<PatientDto> { FileName = fileName, ImportTime = DateTime.Now };
    
    try
    {
        // 设置EPPlus许可证上下文
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null) return ServiceResult<ImportResultDto<PatientDto>>.Failure("Excel文件格式错误");
        
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        if (rowCount <= 1) return ServiceResult<ImportResultDto<PatientDto>>.Success(result);
        
        result.TotalCount = rowCount - 1; // 排除表头
        
        // 逐行处理数据
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                // 提取数据
                var name = worksheet.Cells[row, 1].Text?.Trim();
                var genderText = worksheet.Cells[row, 2].Text?.Trim();
                var phoneNumber = worksheet.Cells[row, 5].Text?.Trim();
                
                // 数据验证
                if (string.IsNullOrWhiteSpace(name))
                {
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                    {
                        RecordIdentifier = $"第{row}行",
                        ErrorMessage = "姓名不能为空"
                    });
                    continue;
                }
                
                if (phoneNumber.Length != 11 || !phoneNumber.All(char.IsDigit))
                {
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                    {
                        RecordIdentifier = $"第{row}行",
                        ErrorMessage = "联系电话格式错误（需要11位数字）"
                    });
                    continue;
                }
                
                // 创建实体并保存
                var patient = new Patient { /* 映射数据 */ };
                var savedPatient = await _repository.AddAsync(patient);
                var patientDto = _mapper.Map<PatientDto>(savedPatient);
                
                result.SuccessCount++;
                result.SuccessfulIds.Add(savedPatient.Id);
                result.ImportedData.Add(patientDto);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                {
                    RecordIdentifier = $"第{row}行",
                    ErrorMessage = $"导入失败：{ex.Message}"
                });
            }
        }
        
        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";
        return ServiceResult<ImportResultDto<PatientDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入患者数据时发生错误");
        return ServiceResult<ImportResultDto<PatientDto>>.Failure($"导入失败：{ex.Message}");
    }
}

/// <summary>
/// 生成导入模板模式
/// </summary>
public MemoryStream GenerateImportTemplate()
{
    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    var stream = new MemoryStream();
    
    using (var package = new ExcelPackage(stream))
    {
        var worksheet = package.Workbook.Worksheets.Add("患者信息");
        
        // 设置表头
        worksheet.Cells[1, 1].Value = "姓名*";
        worksheet.Cells[1, 2].Value = "性别";
        worksheet.Cells[1, 3].Value = "出生日期";
        worksheet.Cells[1, 4].Value = "身份证号";
        worksheet.Cells[1, 5].Value = "联系电话*";
        worksheet.Cells[1, 6].Value = "地址";
        
        // 表头样式
        using (var range = worksheet.Cells[1, 1, 1, 6])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }
        
        // 添加示例数据
        worksheet.Cells[2, 1].Value = "张三";
        worksheet.Cells[2, 2].Value = "男";
        worksheet.Cells[2, 3].Value = "1980-01-01";
        worksheet.Cells[2, 4].Value = "110101198001011234";
        worksheet.Cells[2, 5].Value = "13800138000";
        worksheet.Cells[2, 6].Value = "北京市朝阳区";
        
        worksheet.Cells.AutoFitColumns();
        package.Save();
    }
    
    stream.Position = 0;
    return stream;
}
```

## 📋 医案管理模块模式 (MedicalCase Module)

### 状态机管理模式
```csharp
/// <summary>
/// 医案状态流转模式
/// 状态：登记 → 诊疗中 → 已完成 → 已归档
/// </summary>
public async Task<ServiceResult<MedicalCaseDto>> UpdateStatusAsync(Guid id, MedicalCaseStatusUpdateDto dto)
{
    try
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return ServiceResult<MedicalCaseDto>.Failure("医案不存在");
        
        // 状态验证和转换
        var validTransitions = GetValidTransitions(entity.Status);
        if (!validTransitions.Contains(dto.NewStatus))
        {
            return ServiceResult<MedicalCaseDto>.Failure($"无法从{entity.Status}转换到{dto.NewStatus}");
        }
        
        // 记录状态变更
        var oldStatus = entity.Status;
        entity.Status = dto.NewStatus;
        entity.UpdatedAt = DateTime.Now;
        
        // 特殊状态处理
        if (dto.NewStatus == MedicalCaseStatus.Completed)
        {
            entity.CompletedAt = DateTime.Now;
        }
        else if (dto.NewStatus == MedicalCaseStatus.Archived)
        {
            entity.ArchivedAt = DateTime.Now;
        }
        
        var result = await _repository.UpdateAsync(entity);
        var resultDto = _mapper.Map<MedicalCaseDto>(result);
        
        _logger.LogInformation("医案状态更新成功: {MedicalCaseId} {OldStatus} → {NewStatus}", 
            id, oldStatus, dto.NewStatus);
        
        return ServiceResult<MedicalCaseDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "更新医案状态失败");
        return ServiceResult<MedicalCaseDto>.Failure("更新医案状态失败");
    }
}

/// <summary>
/// 获取有效状态转换
/// </summary>
private static HashSet<MedicalCaseStatus> GetValidTransitions(MedicalCaseStatus currentStatus)
{
    return currentStatus switch
    {
        MedicalCaseStatus.Registered => new HashSet<MedicalCaseStatus> 
        { 
            MedicalCaseStatus.InTreatment, 
            MedicalCaseStatus.Cancelled 
        },
        MedicalCaseStatus.InTreatment => new HashSet<MedicalCaseStatus> 
        { 
            MedicalCaseStatus.Completed, 
            MedicalCaseStatus.Cancelled 
        },
        MedicalCaseStatus.Completed => new HashSet<MedicalCaseStatus> 
        { 
            MedicalCaseStatus.Archived 
        },
        _ => new HashSet<MedicalCaseStatus>()
    };
}
```

### Service层聚合根协调模式 (Epic #1612) ⭐

```csharp
/// <summary>
/// 聚合根协调模式 - 通过MedicalCase聚合根管理Consultation和Prescription生命周期
/// 业务规则：AR-001（聚合根管理）、AR-003（一诊一方）、BF-002（三步流程）
/// </summary>
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalCaseService> _logger;

    public MedicalCaseService(
        IMedicalCaseRepository repository,
        IMapper mapper,
        ILogger<MedicalCaseService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Write Layer: 更新辨证信息（三步流程 Step 1）
    /// 通过聚合根协调Consultation的创建和更新
    /// </summary>
    public async Task<MedicalCase?> UpdateConsultationAsync(
        Guid medicalCaseId,
        UpdateConsultationRequest request)
    {
        // 1. 获取聚合根（预加载Consultation）
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException($"MedicalCase {medicalCaseId} not found");

        // 2. 业务规则验证
        if (medicalCase.Status != MedicalCaseStatus.Active)
            throw new InvalidOperationException(
                "只能在Active状态下更新辨证信息");

        // 3. 更新或创建Consultation（聚合根管理）
        if (medicalCase.Consultation == null)
        {
            medicalCase.Consultation = new Consultation
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = medicalCaseId
            };
        }

        // 4. 映射数据
        _mapper.Map(request, medicalCase.Consultation);
        medicalCase.Consultation.Step1CompletedAt = DateTime.UtcNow;

        // 5. 保存聚合根
        await _repository.UpdateAsync(medicalCase);

        _logger.LogInformation(
            "辨证信息更新成功: MedicalCase={MedicalCaseId}",
            medicalCaseId);

        return medicalCase;
    }

    /// <summary>
    /// Write Layer: 创建处方（三步流程 Step 3a）
    /// 验证AR-003一诊一方约束
    /// </summary>
    public async Task<Prescription?> CreatePrescriptionAsync(
        Guid medicalCaseId,
        CreatePrescriptionRequest request)
    {
        // 1. 获取聚合根（预加载Prescription）
        var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
        if (medicalCase == null)
            throw new NotFoundException($"MedicalCase {medicalCaseId} not found");

        // 2. 业务规则验证: AR-003一诊一方约束
        if (medicalCase.Prescription != null)
            throw new InvalidOperationException(
                "该病案已有处方，请先删除现有处方（AR-003约束）");

        // 3. 业务规则验证: BF-002三步流程
        if (medicalCase.Consultation?.Step1CompletedAt == null)
            throw new InvalidOperationException(
                "未完成辨证（Step 1），无法开处方");

        if (medicalCase.Consultation?.Step2CompletedAt == null)
            throw new InvalidOperationException(
                "未标记处方需求（Step 2），无法开处方");

        if (!medicalCase.NeedsPrescription)
            throw new InvalidOperationException(
                "已标记不需要处方，无法开处方");

        // 4. 创建处方（通过聚合根）
        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = medicalCaseId,
            PrescriptionNumber = request.PrescriptionNumber
                ?? GeneratePrescriptionNumber(),
            Indication = request.Indication,
            DosageCount = request.DosageCount,
            Usage = request.Usage,
            Discount = request.Discount,
            Status = PrescriptionStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = GetCurrentUserId()
        };

        // 5. 创建处方明细
        prescription.PrescriptionItems = request.Items
            .Select(item => new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Quantity * item.UnitPrice,
                Remark = item.Remark
            })
            .ToList();

        // 6. 关联到聚合根
        medicalCase.Prescription = prescription;

        // 7. 保存聚合根
        await _repository.UpdateAsync(medicalCase);

        _logger.LogInformation(
            "处方创建成功: MedicalCase={MedicalCaseId}, Prescription={PrescriptionId}",
            medicalCaseId, prescription.Id);

        return prescription;
    }

    /// <summary>
    /// Read Layer: 获取病案详情（预加载关联实体）
    /// </summary>
    public async Task<MedicalCase?> GetByIdAsync(Guid medicalCaseId)
    {
        return await _repository.GetByIdWithDetailsAsync(medicalCaseId);
    }

    /// <summary>
    /// Helper Layer: 验证病案是否可编辑
    /// </summary>
    public async Task<CanEditResponse> CanEditAsync(Guid medicalCaseId)
    {
        var medicalCase = await _repository.GetByIdAsync(medicalCaseId);
        if (medicalCase == null)
            return new CanEditResponse
            {
                CanEdit = false,
                Reason = "病案不存在"
            };

        // 业务规则：只能编辑Active状态的病案
        if (medicalCase.Status != MedicalCaseStatus.Active)
            return new CanEditResponse
            {
                CanEdit = false,
                Reason = $"病案状态为{medicalCase.Status}，无法编辑"
            };

        // 业务规则：当天创建的可编辑
        if (medicalCase.CreatedAt.Date != DateTime.Today)
            return new CanEditResponse
            {
                CanEdit = false,
                Reason = "只能编辑当天创建的病案"
            };

        return new CanEditResponse { CanEdit = true, Reason = string.Empty };
    }
}
```

### Repository层预加载模式 (Epic #1612) ⭐

```csharp
/// <summary>
/// Repository层预加载模式 - 避免N+1查询
/// 使用EF Core的Include预加载关联实体
/// </summary>
public class MedicalCaseRepository : IMedicalCaseRepository
{
    private readonly LYBTClinicDbContext _context;

    public MedicalCaseRepository(LYBTClinicDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 预加载模式：一次查询获取完整聚合根
    /// 避免N+1查询问题
    /// </summary>
    public async Task<MedicalCase?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.MedicalCases
            .Include(mc => mc.Consultation)  // 预加载Consultation
            .Include(mc => mc.Prescription)   // 预加载Prescription
                .ThenInclude(p => p.PrescriptionItems)  // 预加载PrescriptionItems
            .FirstOrDefaultAsync(mc => mc.Id == id && !mc.IsDeleted);
    }

    /// <summary>
    /// 分页查询预加载模式
    /// </summary>
    public async Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(
        int page,
        int pageSize,
        MedicalCaseStatus? status = null,
        Guid? patientId = null)
    {
        var query = _context.MedicalCases
            .Include(mc => mc.Consultation)
            .Include(mc => mc.Prescription)
            .Where(mc => !mc.IsDeleted);

        // 条件过滤
        if (status.HasValue)
            query = query.Where(mc => mc.Status == status.Value);

        if (patientId.HasValue)
            query = query.Where(mc => mc.PatientId == patientId.Value);

        // 总数统计
        var totalCount = await query.CountAsync();

        // 分页数据
        var items = await query
            .OrderByDescending(mc => mc.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<MedicalCase>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 按患者ID查询（用于BR-001业务规则验证）
    /// </summary>
    public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.MedicalCases
            .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
            .OrderByDescending(mc => mc.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 标准更新模式（保存聚合根）
    /// </summary>
    public async Task<MedicalCase> UpdateAsync(MedicalCase medicalCase)
    {
        _context.MedicalCases.Update(medicalCase);
        await _context.SaveChangesAsync();
        return medicalCase;
    }
}
```

### Controller三层分离模式 (Epic #1612) ⭐

```csharp
/// <summary>
/// Controller三层分离模式：Write/Read/Helper Layer
/// Write Layer: 写操作（创建、更新、删除）
/// Read Layer: 读操作（查询、列表）
/// Helper Layer: 辅助操作（验证、权限）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MedicalCaseController : ControllerBase
{
    private readonly IMedicalCaseService _service;
    private readonly IMapper _mapper;
    private readonly ILogger<MedicalCaseController> _logger;

    public MedicalCaseController(
        IMedicalCaseService service,
        IMapper mapper,
        ILogger<MedicalCaseController> logger)
    {
        _service = service;
        _mapper = mapper;
        _logger = logger;
    }

    // ========== Write Layer: 写操作 ==========

    /// <summary>
    /// 创建新病案
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> CreateMedicalCase(
        [FromBody] CreateMedicalCaseRequest request)
    {
        try
        {
            var medicalCase = await _service.CreateAsync(
                request.PatientId,
                request.VisitDate);

            return Ok(ApiResponse<MedicalCase>.Success(
                medicalCase,
                "病案创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "创建病案失败: {Message}", ex.Message);
            return BadRequest(ApiResponse<MedicalCase>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建病案异常");
            return StatusCode(500, ApiResponse<MedicalCase>.Failure(
                "创建病案失败，请稍后重试"));
        }
    }

    /// <summary>
    /// 更新辨证信息（Step 1）
    /// </summary>
    [HttpPut("{id}/consultation")]
    public async Task<ActionResult<ApiResponse<MedicalCase>>> UpdateConsultation(
        Guid id,
        [FromBody] UpdateConsultationRequest request)
    {
        try
        {
            var medicalCase = await _service.UpdateConsultationAsync(id, request);
            return Ok(ApiResponse<MedicalCase>.Success(
                medicalCase,
                "辨证信息更新成功"));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<MedicalCase>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MedicalCase>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新辨证信息异常: MedicalCaseId={Id}", id);
            return StatusCode(500, ApiResponse<MedicalCase>.Failure(
                "更新辨证信息失败"));
        }
    }

    /// <summary>
    /// 创建处方（Step 3a）
    /// </summary>
    [HttpPost("{id}/prescriptions")]
    public async Task<ActionResult<ApiResponse<Prescription>>> CreatePrescription(
        Guid id,
        [FromBody] CreatePrescriptionRequest request)
    {
        try
        {
            var prescription = await _service.CreatePrescriptionAsync(id, request);
            return Ok(ApiResponse<Prescription>.Success(
                prescription,
                "处方创建成功"));
        }
        catch (NotFoundException ex)
        {
            return NotFound(ApiResponse<Prescription>.Failure(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            // 422: AR-003违规或BF-002流程违规
            return UnprocessableEntity(ApiResponse<Prescription>.Failure(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建处方异常: MedicalCaseId={Id}", id);
            return StatusCode(500, ApiResponse<Prescription>.Failure(
                "创建处方失败"));
        }
    }

    // ========== Read Layer: 读操作 ==========

    /// <summary>
    /// 获取病案详情（预加载Consultation和Prescription）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MedicalCaseDetailDto>>> GetById(Guid id)
    {
        try
        {
            var medicalCase = await _service.GetByIdAsync(id);
            if (medicalCase == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.Failure(
                    "病案不存在"));

            var dto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);
            return Ok(ApiResponse<MedicalCaseDetailDto>.Success(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取病案详情异常: Id={Id}", id);
            return StatusCode(500, ApiResponse<MedicalCaseDetailDto>.Failure(
                "获取病案详情失败"));
        }
    }

    /// <summary>
    /// 查询病案列表（分页 + 过滤）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MedicalCaseDto>>>> GetList(
        [FromQuery] MedicalCaseStatus? status = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 参数验证
            if (page < 1 || pageSize < 1 || pageSize > 100)
                return BadRequest(ApiResponse<PagedResult<MedicalCaseDto>>.Failure(
                    "分页参数无效（page≥1, 1≤pageSize≤100）"));

            var pagedResult = await _service.GetListAsync(
                status, patientId, page, pageSize);

            var dto = new PagedResult<MedicalCaseDto>
            {
                Items = _mapper.Map<List<MedicalCaseDto>>(pagedResult.Items),
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };

            return Ok(ApiResponse<PagedResult<MedicalCaseDto>>.Success(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询病案列表异常");
            return StatusCode(500,
                ApiResponse<PagedResult<MedicalCaseDto>>.Failure(
                    "查询病案列表失败"));
        }
    }

    // ========== Helper Layer: 辅助操作 ==========

    /// <summary>
    /// 验证病案是否可编辑
    /// </summary>
    [HttpGet("{id}/can-edit")]
    public async Task<ActionResult<ApiResponse<CanEditResponse>>> CanEdit(Guid id)
    {
        try
        {
            var response = await _service.CanEditAsync(id);
            return Ok(ApiResponse<CanEditResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证病案可编辑性异常: Id={Id}", id);
            return StatusCode(500, ApiResponse<CanEditResponse>.Failure(
                "验证失败"));
        }
    }

    /// <summary>
    /// 验证处方是否可删除
    /// </summary>
    [HttpGet("{id}/prescriptions/{prescriptionId}/can-delete")]
    public async Task<ActionResult<ApiResponse<CanDeleteResponse>>> CanDeletePrescription(
        Guid id,
        Guid prescriptionId)
    {
        try
        {
            var response = await _service.CanDeletePrescriptionAsync(
                id, prescriptionId);
            return Ok(ApiResponse<CanDeleteResponse>.Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "验证处方可删除性异常: MedicalCaseId={Id}, PrescriptionId={PrescriptionId}",
                id, prescriptionId);
            return StatusCode(500, ApiResponse<CanDeleteResponse>.Failure(
                "验证失败"));
        }
    }
}
```

**关键设计原则**:
- ✅ **Write/Read/Helper分离**: 清晰的端点职责划分
- ✅ **聚合根协调**: Service层通过MedicalCase聚合根管理子实体
- ✅ **预加载优化**: Repository层使用Include避免N+1查询
- ✅ **业务规则验证**: Service层验证AR-001/AR-003/BF-002/BR-001
- ✅ **异常处理分层**: Controller处理HTTP异常，Service处理业务异常

## 🩺 诊疗记录模块模式 (Consultation Module)

### 四诊合参录入模式
```csharp
/// <summary>
/// 四诊合参数据录入模式
/// 望闻问切四诊信息结构化存储
/// </summary>
public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
{
    try
    {
        var entity = _mapper.Map<Consultation>(dto);
        
        // 四诊信息验证
        ValidateFourDiagnostics(entity);
        
        // 辨证论治验证
        ValidateDiagnosis(entity);
        
        var result = await _repository.AddAsync(entity);
        var resultDto = _mapper.Map<ConsultationDto>(result);
        
        return ServiceResult<ConsultationDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建诊疗记录失败");
        return ServiceResult<ConsultationDto>.Failure("创建诊疗记录失败");
    }
}

/// <summary>
/// 四诊信息验证
/// </summary>
private void ValidateFourDiagnostics(Consultation consultation)
{
    // 望诊验证
    if (string.IsNullOrWhiteSpace(consultation.Inspection))
    {
        throw new ValidationException("望诊信息不能为空");
    }
    
    // 闻诊验证
    if (string.IsNullOrWhiteSpace(consultation.Auscultation))
    {
        throw new ValidationException("闻诊信息不能为空");
    }
    
    // 问诊验证
    if (string.IsNullOrWhiteSpace(consultation.Inquiry))
    {
        throw new ValidationException("问诊信息不能为空");
    }
    
    // 切诊验证
    if (string.IsNullOrWhiteSpace(consultation.Palpation))
    {
        throw new ValidationException("切诊信息不能为空");
    }
}

/// <summary>
/// 辨证论治验证
/// </summary>
private void ValidateDiagnosis(Consultation consultation)
{
    if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
    {
        throw new ValidationException("诊断结果不能为空");
    }
    
    if (string.IsNullOrWhiteSpace(consultation.TreatmentPrinciple))
    {
        throw new ValidationException("治法不能为空");
    }
}
```

## 💊 处方管理模块模式 (Prescriptions Module)

### 价格计算模式
```csharp
/// <summary>
/// 处方价格计算模式
/// 支持折扣和帖数计算
/// </summary>
private decimal CalculateTotalAmount(IEnumerable<PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
{
    decimal total = 0;
    
    foreach (var item in items)
    {
        // 基础价格计算：单价 × 数量 × 帖数
        var itemTotal = item.UnitPrice * item.Quantity * dosageCount;
        total += itemTotal;
    }
    
    // 应用折扣
    return total * discount;
}

/// <summary>
/// 处方克隆模式
/// 用于复制历史处方
/// </summary>
public async Task<ServiceResult<PrescriptionDto>> CloneAsync(Guid prescriptionId)
{
    try
    {
        // 获取原始处方（包含药材项）
        var originalPrescription = await _repository.GetByIdWithItemsAsync(prescriptionId);
        if (originalPrescription == null)
            return ServiceResult<PrescriptionDto>.Failure("未找到要克隆的处方");
        
        // 创建克隆处方
        var clonedPrescription = new Prescription
        {
            Id = Guid.NewGuid(),
            MedicalCaseId = originalPrescription.MedicalCaseId,
            PatientId = originalPrescription.PatientId,
            UserId = originalPrescription.UserId,
            Indication = originalPrescription.Indication,
            DosageCount = originalPrescription.DosageCount,
            Discount = originalPrescription.Discount,
            Advice = originalPrescription.Advice,
            Status = PrescriptionStatus.Draft, // 克隆的处方默认为草稿状态
            CreatedAt = DateTime.Now,
            Items = new List<PrescriptionItem>()
        };
        
        var savedPrescription = await _repository.AddAsync(clonedPrescription);
        
        // 复制药材项
        if (originalPrescription.Items != null && originalPrescription.Items.Any())
        {
            foreach (var item in originalPrescription.Items)
            {
                savedPrescription.Items.Add(new PrescriptionItem
                {
                    Id = Guid.NewGuid(),
                    PrescriptionId = savedPrescription.Id,
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Usage = item.Usage,
                    Remark = item.Remark
                });
            }
        }
        
        await _repository.SaveChangesAsync();
        var prescriptionDto = _mapper.Map<PrescriptionDto>(savedPrescription);
        return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "克隆处方时发生错误，处方ID：{PrescriptionId}", prescriptionId);
        return ServiceResult<PrescriptionDto>.Failure($"克隆处方失败：{ex.Message}");
    }
}

/// <summary>
/// 生成处方编号模式
/// 格式：RX + YYYYMMDD + 4位序号
/// </summary>
public async Task<ServiceResult<string>> GeneratePrescriptionNoAsync()
{
    try
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = "RX";
        
        // MVP阶段：简单数据库计数方案
        var allPrescriptions = await _repository.GetAllAsync();
        var todayPrescriptions = allPrescriptions
            .Where(p => p.CreatedAt.Date == DateTime.Today)
            .ToList();
        
        var sequence = todayPrescriptions.Count + 1;
        var prescriptionNo = $"{prefix}{today}{sequence:D4}";
        
        return ServiceResult<string>.Success(prescriptionNo);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成处方编号失败");
        return ServiceResult<string>.Failure("生成处方编号失败");
    }
}
```

## 🌿 药材管理模块模式 (Herbs Module)

### 拼音码生成模式
```csharp
/// <summary>
/// 药材拼音码生成模式
/// 用于快速检索
/// </summary>
public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
{
    try
    {
        var entity = _mapper.Map<Herb>(dto);
        
        // 生成拼音码
        entity.PinYinCode = GeneratePinyinCode(entity.Name);
        
        var result = await _repository.AddAsync(entity);
        var resultDto = _mapper.Map<HerbDto>(result);
        return ServiceResult<HerbDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建药材失败");
        return ServiceResult<HerbDto>.Failure("创建药材失败");
    }
}

/// <summary>
/// 简化的拼音码生成
/// MVP版本：取每个汉字拼音首字母
/// </summary>
private string GeneratePinyinCode(string herbName)
{
    if (string.IsNullOrWhiteSpace(herbName)) return string.Empty;
    
    // 简化实现：常见药材的拼音码映射
    var commonHerbs = new Dictionary<string, string>
    {
        ["人参"] = "RS",
        ["当归"] = "DG",
        ["黄芪"] = "HQ",
        ["白术"] = "BZ",
        ["茯苓"] = "FL",
        ["甘草"] = "GC",
        ["白芍"] = "BS",
        ["川芎"] = "CX",
        ["熟地黄"] = "SDH",
        ["何首乌"] = "HSW"
    };
    
    // 如果是常见药材，直接返回映射
    if (commonHerbs.TryGetValue(herbName, out var code))
    {
        return code;
    }
    
    // 简化拼音生成（实际项目应使用拼音库）
    var pinyin = new StringBuilder();
    foreach (var c in herbName.Take(4)) // 最多取前4个字符
    {
        pinyin.Append(GetPinyinFirstLetter(c));
    }
    
    return pinyin.ToString();
}

/// <summary>
/// 获取汉字拼音首字母（简化版）
/// </summary>
private char GetPinyinFirstLetter(char c)
{
    // 简化实现：常见汉字拼音首字母映射
    return c switch
    {
        '当' => 'D', '归' => 'G',
        '人' => 'R', '参' => 'S',
        '黄' => 'H', '芪' => 'Q',
        '白' => 'B', '术' => 'Z',
        '茯' => 'F', '苓' => 'L',
        '甘' => 'G', '草' => 'C',
        '川' => 'C', '芎' => 'X',
        '熟' => 'S', '地' => 'D',
        '何' => 'H', '首' => 'S', '乌' => 'W',
        _ => c.ToString().ToUpper().FirstOrDefault()
    };
}

/// <summary>
/// 药材搜索模式
/// 支持名称和拼音码搜索
/// </summary>
public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
{
    try
    {
        var entities = await _repository.FindAsync(h =>
            h.Name.Contains(keyword) ||
            (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
        
        var dtos = _mapper.Map<List<HerbDto>>(entities);
        return ServiceResult<List<HerbDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
        return ServiceResult<List<HerbDto>>.Failure("搜索药材失败");
    }
}
```

### 批量操作模式
```csharp
/// <summary>
/// 批量删除通用模式
/// </summary>
public async Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
{
    const int MAX_BATCH_SIZE = 100;
    
    try
    {
        // 批量大小限制
        if (ids.Count > MAX_BATCH_SIZE)
        {
            return ServiceResult<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
        }
        
        var result = new BatchOperationResultDto
        {
            TotalCount = ids.Count,
            IsSuccess = true,
            Message = "批量删除完成"
        };
        
        foreach (var herbId in ids)
        {
            try
            {
                var herb = await _repository.GetByIdAsync(herbId);
                if (herb == null)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(herbId);
                    result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                    {
                        RecordIdentifier = herbId.ToString(),
                        ErrorMessage = "药材不存在"
                    });
                    continue;
                }
                
                var deleteResult = await _repository.DeleteAsync(herbId);
                if (deleteResult)
                {
                    result.SuccessCount++;
                    result.SuccessfulIds.Add(herbId);
                }
                else
                {
                    result.FailureCount++;
                    result.FailedIds.Add(herbId);
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedIds.Add(herbId);
                result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                {
                    RecordIdentifier = herbId.ToString(),
                    ErrorMessage = ex.Message
                });
            }
        }
        
        result.IsSuccess = result.FailureCount == 0;
        return ServiceResult<BatchOperationResultDto>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "批量删除药材异常");
        return ServiceResult<BatchOperationResultDto>.Failure("批量删除药材失败");
    }
}
```

## 📜 验方管理模块模式 (Formula Module)

### 智能推荐模式
```csharp
/// <summary>
/// 基于症状的验方推荐模式
/// </summary>
public async Task<ServiceResult<List<FormulaDto>>> RecommendBySymptomsAsync(SymptomRecommendationDto dto)
{
    try
    {
        var allFormulas = await _repository.GetAllAsync();
        var recommendedFormulas = new List<Formula>();
        
        // 症状匹配算法
        foreach (var formula in allFormulas)
        {
            var matchScore = CalculateSymptomMatchScore(dto.Symptoms, formula);
            if (matchScore > 0.6) // 匹配度阈值
            {
                recommendedFormulas.Add(formula);
            }
        }
        
        // 按匹配度排序
        recommendedFormulas = recommendedFormulas
            .OrderByDescending(f => CalculateSymptomMatchScore(dto.Symptoms, f))
            .Take(10) // 最多推荐10个
            .ToList();
        
        var dtos = _mapper.Map<List<FormulaDto>>(recommendedFormulas);
        return ServiceResult<List<FormulaDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "症状推荐验方失败");
        return ServiceResult<List<FormulaDto>>.Failure("推荐验方失败");
    }
}

/// <summary>
/// 症状匹配度计算
/// </summary>
private double CalculateSymptomMatchScore(List<string> symptoms, Formula formula)
{
    if (symptoms == null || !symptoms.Any() || string.IsNullOrWhiteSpace(formula.Indications))
        return 0;
    
    var formulaSymptoms = formula.Indications.Split('、', '，', ',', ';')
        .Select(s => s.Trim())
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToList();
    
    if (!formulaSymptoms.Any()) return 0;
    
    // 计算匹配度
    var matchCount = 0;
    foreach (var symptom in symptoms)
    {
        if (formulaSymptoms.Any(fs => fs.Contains(symptom) || symptom.Contains(fs)))
        {
            matchCount++;
        }
    }
    
    return (double)matchCount / symptoms.Count;
}

/// <summary>
/// 基于诊断的验方推荐模式
/// </summary>
public async Task<ServiceResult<List<FormulaDto>>> RecommendByDiagnosisAsync(DiagnosisRecommendationDto dto)
{
    try
    {
        var allFormulas = await _repository.GetAllAsync();
        var recommendedFormulas = new List<Formula>();
        
        // 诊断匹配算法
        foreach (var formula in allFormulas)
        {
            var matchScore = CalculateDiagnosisMatchScore(dto.Diagnosis, dto.TreatmentPrinciple, formula);
            if (matchScore > 0.7) // 诊断匹配度阈值更高
            {
                recommendedFormulas.Add(formula);
            }
        }
        
        // 按匹配度排序
        recommendedFormulas = recommendedFormulas
            .OrderByDescending(f => CalculateDiagnosisMatchScore(dto.Diagnosis, dto.TreatmentPrinciple, f))
            .Take(5) // 诊断推荐更精确，数量更少
            .ToList();
        
        var dtos = _mapper.Map<List<FormulaDto>>(recommendedFormulas);
        return ServiceResult<List<FormulaDto>>.Success(dtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "诊断推荐验方失败");
        return ServiceResult<List<FormulaDto>>.Failure("推荐验方失败");
    }
}

/// <summary>
/// 诊断匹配度计算
/// </summary>
private double CalculateDiagnosisMatchScore(string diagnosis, string treatmentPrinciple, Formula formula)
{
    var score = 0.0;
    
    // 诊断匹配（权重0.6）
    if (!string.IsNullOrWhiteSpace(diagnosis) && !string.IsNullOrWhiteSpace(formula.Indications))
    {
        var diagnosisMatch = formula.Indications.Contains(diagnosis) ? 1.0 : 0.0;
        score += diagnosisMatch * 0.6;
    }
    
    // 治法匹配（权重0.4）
    if (!string.IsNullOrWhiteSpace(treatmentPrinciple) && !string.IsNullOrWhiteSpace(formula.Functions))
    {
        var treatmentMatch = formula.Functions.Contains(treatmentPrinciple) ? 1.0 : 0.0;
        score += treatmentMatch * 0.4;
    }
    
    return score;
}
```

## 🔍 验证器模式 (FluentValidation)

> **⚠️ 重要演进**：Epic #1731集成FluentValidation + Epic #1736 InputDto统一模式适配

### 验证器架构演进历史

#### Epic #1731 (2025-10-31): FluentValidation集成

**演进动机**:
- ❌ **旧方案**：DataAnnotations特性验证
  - **局限性**：复杂验证逻辑难以表达（如跨字段验证）
  - **可维护性差**：验证逻辑分散在DTO类中，难以测试
  - **扩展性差**：无法灵活配置验证规则

- ✅ **新方案**：FluentValidation框架
  - **表达力强**：支持复杂验证逻辑和自定义规则
  - **易于测试**：独立的Validator类，可单元测试
  - **集成到Pipeline**：自动验证，返回标准400 ProblemDetails

#### Epic #1736 (2025-11-01): InputDto统一模式的Validator适配

**演进动机**:
- ❌ **旧模式**：CreateValidator + UpdateValidator（重复验证规则）
  - **代码重复**：Create和Update的验证规则90%相同
  - **维护成本高**：修改验证规则需要同步两个Validator
  - **违背DRY原则**：Don't Repeat Yourself

- ✅ **新模式**：统一InputDtoValidator（一个Validator服务Create和Update）
  - **消除重复**：验证规则只写一次
  - **易于维护**：修改验证规则只需改一处
  - **符合DRY**：遵循最佳实践

**已删除的UpdateValidator**（Epic #1736清理）:
- ~~ConsultationUpdateDtoValidator~~ → 使用 `ConsultationInputDtoValidator`
- ~~FormulaUpdateDtoValidator~~ → 使用 `FormulaInputDtoValidator`
- ~~HerbUpdateDtoValidator~~ → 使用 `HerbInputDtoValidator`
- ~~PatientUpdateDtoValidator~~ → 使用 `PatientInputDtoValidator`
- ~~UserUpdateDtoValidator~~ → 使用 `UserInputDtoValidator`

**新增的Auth模块Validator**（Epic #1731补全）:
- ✅ `LoginRequestValidator` - 登录请求验证
- ✅ `ChangePasswordRequestValidator` - 修改密码验证
- ✅ `SuperAdminLoginRequestValidator` - 超级管理员登录验证

---

### InputDto统一Validator模式 ⭐

**核心理念**：一个Validator同时服务Create和Update操作

#### 标准模式（Patients模块示例）

```csharp
/// <summary>
/// 患者输入DTO验证器 - 统一服务创建和更新
/// Epic #1736 Phase 3: 合并Create/Update Validators
/// </summary>
public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
{
    public PatientInputDtoValidator()
    {
        // ========== 通用验证规则（创建和更新共享） ==========

        // 必填字段验证
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名最多50个字符");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("联系电话不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("联系电话格式错误（需要11位手机号）");

        // 可选字段验证
        RuleFor(x => x.IdCardNumber)
            .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式错误")
            .When(x => !string.IsNullOrEmpty(x.IdCardNumber)); // 仅在提供时验证

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式错误")
            .When(x => !string.IsNullOrEmpty(x.Email));

        // 枚举验证
        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值无效");

        // ========== 创建时的特殊验证（条件规则） ==========

        // 创建时ID必须为null
        RuleFor(x => x.Id)
            .Null().WithMessage("创建时不应提供ID")
            .When(x => x.Id.HasValue); // 仅在提供时验证

        // ========== 跨字段验证（高级规则） ==========

        // 出生日期不能晚于今天
        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateTime.Today).WithMessage("出生日期不能晚于今天")
            .When(x => x.BirthDate.HasValue);

        // 年龄合理性验证（0-150岁）
        RuleFor(x => x)
            .Must(dto => {
                if (!dto.BirthDate.HasValue) return true;
                var age = DateTime.Today.Year - dto.BirthDate.Value.Year;
                return age >= 0 && age <= 150;
            })
            .WithMessage("年龄必须在0-150岁之间")
            .When(x => x.BirthDate.HasValue);
    }
}
```

**Service层使用模式**:
```csharp
/// <summary>
/// 创建患者（Validator自动验证）
/// Epic #1731 Phase 3: FluentValidation Pipeline自动验证
/// </summary>
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientInputDto input)
{
    // ⚠️ 无需手动验证！FluentValidation Pipeline自动验证
    // 如果验证失败，会自动返回400 ProblemDetails

    // 验证通过后的逻辑
    var entity = _mapper.Map<Patient>(input);
    entity.Id = Guid.NewGuid();  // Service层生成ID

    var result = await _repository.AddAsync(entity);
    return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(result));
}

/// <summary>
/// 更新患者（使用同一个Validator）
/// </summary>
public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientInputDto input)
{
    // ⚠️ 同样无需手动验证！FluentValidation Pipeline自动验证

    var existing = await _repository.GetByIdAsync(id);
    if (existing == null)
        return ServiceResult<PatientDto>.Error("患者不存在");

    _mapper.Map(input, existing);  // 仅映射业务属性，不覆盖Id

    var result = await _repository.UpdateAsync(existing);
    return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(result));
}
```

---

### Auth模块Validator示例（Epic #1731新增）

#### 1. 登录请求验证器

```csharp
/// <summary>
/// 登录请求验证器
/// Epic #1731 Phase 1: Auth模块Validators补全
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // 用户名验证
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .MinimumLength(3).WithMessage("用户名至少3个字符")
            .MaximumLength(50).WithMessage("用户名最多50个字符")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        // 密码验证
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码至少6个字符")
            .MaximumLength(100).WithMessage("密码最多100个字符");
    }
}
```

#### 2. 修改密码验证器

```csharp
/// <summary>
/// 修改密码请求验证器
/// Epic #1731 Phase 1: Auth模块Validators补全
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        // 旧密码验证
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("旧密码不能为空");

        // 新密码验证
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("新密码不能为空")
            .MinimumLength(6).WithMessage("新密码至少6个字符")
            .MaximumLength(100).WithMessage("新密码最多100个字符");

        // 新密码确认验证
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("确认密码不能为空")
            .Equal(x => x.NewPassword).WithMessage("两次输入的新密码不一致");

        // 跨字段验证：新旧密码不能相同
        RuleFor(x => x)
            .Must(req => req.NewPassword != req.OldPassword)
            .WithMessage("新密码不能与旧密码相同")
            .When(x => !string.IsNullOrEmpty(x.OldPassword) && !string.IsNullOrEmpty(x.NewPassword));
    }
}
```

#### 3. 超级管理员登录验证器

```csharp
/// <summary>
/// 超级管理员登录请求验证器
/// Epic #1731 Phase 1: Auth模块Validators补全
/// 额外验证：超级管理员密码强度要求更高
/// </summary>
public class SuperAdminLoginRequestValidator : AbstractValidator<SuperAdminLoginRequest>
{
    public SuperAdminLoginRequestValidator()
    {
        // 用户名验证（超级管理员）
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Equal("sysadmin").WithMessage("超级管理员用户名固定为sysadmin");

        // 密码验证（更严格）
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(12).WithMessage("超级管理员密码至少12个字符")
            .Matches(@"[A-Z]").WithMessage("密码必须包含大写字母")
            .Matches(@"[a-z]").WithMessage("密码必须包含小写字母")
            .Matches(@"\d").WithMessage("密码必须包含数字")
            .Matches(@"[@$!%*?&#]").WithMessage("密码必须包含特殊字符");

        // MFA Code验证（如果启用）
        RuleFor(x => x.MfaCode)
            .Matches(@"^\d{6}$").WithMessage("MFA验证码必须是6位数字")
            .When(x => !string.IsNullOrEmpty(x.MfaCode));
    }
}
```

---

### FluentValidation Pipeline集成 (Epic #1731 Phase 3)

#### Program.cs / ServiceCollectionExtensions配置

```csharp
/// <summary>
/// 注册FluentValidation到ASP.NET Core Pipeline
/// Epic #1731 Phase 3: 集成FluentValidation到Pipeline
/// </summary>
public static IServiceCollection RegisterControllerServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ========== FluentValidation全局自动验证 ==========
    services.AddFluentValidationAutoValidation(config =>
    {
        // 保留DataAnnotations验证（与FluentValidation共存）
        config.DisableDataAnnotationsValidation = false;
    });
    services.AddFluentValidationClientsideAdapters();

    // ========== 配置自动模型验证行为 ==========
    services.Configure<ApiBehaviorOptions>(options =>
    {
        // 启用自动400响应（模型验证失败时）
        options.SuppressModelStateInvalidFilter = false;

        // 自定义400响应格式（使用ProblemDetails）
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "模型验证失败",
                Detail = "请求数据包含验证错误，请检查输入",
                Instance = context.HttpContext.Request.Path
            };

            // 添加追踪信息
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

    // 控制器和JSON配置
    services.AddControllers()
        .AddJsonOptions(options => { /* JSON配置 */ });

    return services;
}
```

**Pipeline自动验证效果**:
```
请求 → FluentValidation验证 → 验证失败？
                         ↓
                    ✅ 通过 → Controller方法执行
                         ↓
                    ❌ 失败 → 自动返回400 ProblemDetails
```

**返回的ProblemDetails示例**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "模型验证失败",
  "status": 400,
  "detail": "请求数据包含验证错误，请检查输入",
  "instance": "/api/v1/patients",
  "traceId": "0HN4...",
  "timestamp": "2025-11-01T10:30:00Z",
  "errors": {
    "Name": ["患者姓名不能为空"],
    "PhoneNumber": ["联系电话格式错误（需要11位手机号）"]
  }
}
```

---

### Validator单元测试模式

```csharp
/// <summary>
/// Validator单元测试标准模式
/// 使用FluentValidation.TestHelper
/// </summary>
public class PatientInputDtoValidatorTests
{
    private readonly PatientInputDtoValidator _validator;

    public PatientInputDtoValidatorTests()
    {
        _validator = new PatientInputDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Name = "张三",
            PhoneNumber = "13800138000",
            Gender = Gender.Male,
            BirthDate = DateTime.Parse("1980-01-01")
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Name = "",  // ❌ 违反验证规则
            PhoneNumber = "13800138000"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("患者姓名不能为空");
    }

    [Fact]
    public void Validate_WithInvalidPhoneNumber_FailsValidation()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Name = "张三",
            PhoneNumber = "12345"  // ❌ 格式错误
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithIdOnCreate_FailsValidation()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Id = Guid.NewGuid(),  // ❌ 创建时不应提供ID
            Name = "张三",
            PhoneNumber = "13800138000"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("创建时不应提供ID");
    }
}
```

---

### Validator注册模式（模块级）

#### 每个模块的ModuleServiceExtensions注册Validators

```csharp
/// <summary>
/// Patients模块服务注册扩展
/// Epic #1731: 注册FluentValidation Validators
/// </summary>
public static class PatientsModuleServiceExtensions
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册Repository
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 注册Service
        services.AddScoped<IPatientService, PatientService>();

        // Epic #1731: 注册Validators
        services.AddScoped<IValidator<PatientInputDto>, PatientInputDtoValidator>();

        return services;
    }
}
```

**所有模块Validator注册清单**:
- ✅ Auth: `LoginRequestValidator`, `ChangePasswordRequestValidator`, `SuperAdminLoginRequestValidator`
- ✅ Users: `UserInputDtoValidator`
- ✅ Patients: `PatientInputDtoValidator`
- ✅ Herbs: `HerbInputDtoValidator`
- ✅ Consultation: `ConsultationInputDtoValidator`
- ✅ Prescriptions: `PrescriptionCreateDtoValidator`, `PrescriptionEditDtoValidator`
- ✅ Formula: `FormulaInputDtoValidator`

---

## 🔧 通用模式集合

### 错误处理模式
```csharp
/// <summary>
/// 标准错误处理模式
/// </summary>
public async Task<ServiceResult<T>> ExecuteWithErrorHandling<T>(Func<Task<T>> operation, string operationName)
{
    try
    {
        var result = await operation();
        return ServiceResult<T>.Success(result);
    }
    catch (ValidationException ex)
    {
        _logger.LogWarning(ex, "{OperationName}验证失败: {Message}", operationName, ex.Message);
        return ServiceResult<T>.Failure(ex.Message);
    }
    catch (BusinessException ex)
    {
        _logger.LogError(ex, "{OperationName}业务异常: {Message}", operationName, ex.Message);
        return ServiceResult<T>.Failure(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "{OperationName}系统异常", operationName);
        return ServiceResult<T>.Failure($"{operationName}失败，请稍后重试");
    }
}
```

### 日志记录模式
```csharp
/// <summary>
/// 标准日志记录模式
/// </summary>
private void LogOperationStart(string operation, object parameters = null)
{
    if (parameters != null)
    {
        _logger.LogInformation("开始{Operation}: {Parameters}", operation, parameters);
    }
    else
    {
        _logger.LogInformation("开始{Operation}", operation);
    }
}

private void LogOperationSuccess(string operation, string identifier = null)
{
    if (!string.IsNullOrWhiteSpace(identifier))
    {
        _logger.LogInformation("{Operation}成功: {Identifier} [时间: {Timestamp}]", 
            operation, identifier, DateTime.UtcNow);
    }
    else
    {
        _logger.LogInformation("{Operation}成功 [时间: {Timestamp}]", operation, DateTime.UtcNow);
    }
}

private void LogOperationFailure(string operation, string reason, string identifier = null)
{
    if (!string.IsNullOrWhiteSpace(identifier))
    {
        _logger.LogWarning("{Operation}失败: {Identifier} [原因: {Reason}] [时间: {Timestamp}]", 
            operation, identifier, reason, DateTime.UtcNow);
    }
    else
    {
        _logger.LogWarning("{Operation}失败: [原因: {Reason}] [时间: {Timestamp}]", 
            operation, reason, DateTime.UtcNow);
    }
}
```

### 参数验证模式
```csharp
/// <summary>
/// 标准参数验证模式
/// </summary>
private void ValidatePaginationParameters(int page, int pageSize)
{
    if (page < 1)
        throw new ValidationException("页码必须大于0");
    
    if (pageSize < 1 || pageSize > 100)
        throw new ValidationException("每页大小必须在1-100之间");
}

private void ValidateIdParameter(Guid id, string entityName)
{
    if (id == Guid.Empty)
        throw new ValidationException($"{entityName}ID不能为空");
}

private void ValidateStringParameter(string value, string parameterName, bool required = false, int maxLength = 0)
{
    if (required && string.IsNullOrWhiteSpace(value))
        throw new ValidationException($"{parameterName}不能为空");
    
    if (maxLength > 0 && !string.IsNullOrWhiteSpace(value) && value.Length > maxLength)
        throw new ValidationException($"{parameterName}长度不能超过{maxLength}个字符");
}
```

## 📝 DTO映射模式

### AutoMapper配置模式
```csharp
/// <summary>
/// 标准DTO映射配置
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // 用户映射
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.ToString()));
        
        CreateMap<UserCreateDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        
        // 患者映射
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));
        
        CreateMap<PatientCreateDto, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        
        // 处方映射（包含价格计算）
        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => CalculatePrescriptionTotal(src)));
        
        // 处方项映射
        CreateMap<PrescriptionItem, PrescriptionItemDto>()
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.UnitPrice * src.Quantity));
    }
    
    /// <summary>
    /// 计算年龄
    /// </summary>
    private int CalculateAge(DateTime? birthDate)
    {
        if (!birthDate.HasValue) return 0;
        
        var today = DateTime.Today;
        var age = today.Year - birthDate.Value.Year;
        
        if (birthDate.Value.Date > today.AddYears(-age))
        {
            age--;
        }
        
        return age;
    }
    
    /// <summary>
    /// 计算处方总价
    /// </summary>
    private decimal CalculatePrescriptionTotal(Prescription prescription)
    {
        if (prescription.Items == null || !prescription.Items.Any())
            return 0;
        
        var total = prescription.Items.Sum(item => item.UnitPrice * item.Quantity * prescription.DosageCount);
        return total * prescription.Discount;
    }
}
```

---

## 🎯 使用指南

### 1. 选择合适的模式
- **认证相关** → Auth Module 模式
- **CRUD操作** → Users Module 基础模式
- **Excel处理** → Patients Module 导入模式
- **状态管理** → MedicalCase Module 状态机模式
- **价格计算** → Prescriptions Module 计算模式
- **搜索功能** → Herbs Module 搜索模式
- **推荐算法** → Formula Module 推荐模式

### 2. 代码复用原则
- 优先使用通用模式
- 根据业务需求调整细节
- 保持命名和结构一致性
- 添加适当的错误处理和日志记录

### 3. MVP约束
- 避免过度复杂的设计
- 优先实现核心功能
- 使用简单高效的算法
- 保持代码可读性和可维护性

---

*此代码模式文档基于实际8个业务模块代码生成，确保100%准确性。如有疑问，请查看具体模块实现代码。*