# 患者数据管理问题解决指南

> **目标导向**: 解决患者管理过程中的实际问题和常见错误
> **适合人群**: 开发者、业务用户、系统管理员
> **使用方式**: 问题驱动、按需查找、实用高效

## 🔥 高频问题解决

### 患者注册与信息管理

#### 问题1：患者重名和重复注册

**现象**: 系统中出现同名患者，导致混淆和操作错误

**解决方案**:
```csharp
// 1. 患者查重逻辑
public async Task<bool> CheckPatientDuplicateAsync(PatientCreateDto dto)
{
    // 多维度查重：身份证号 + 姓名 + 出生日期
    var existingPatients = await _repository.FindAsync(p =>
        (p.IdNumber == dto.IdNumber && !string.IsNullOrEmpty(dto.IdNumber)) ||
        (p.Name == dto.Name && p.BirthDate == dto.BirthDate)
    );

    return existingPatients.Any();
}

// 2. 患者合并功能
public async Task<PatientMergeResultDto> MergePatientsAsync(PatientMergeDto dto)
{
    var primaryPatient = await _repository.GetByIdAsync(dto.PrimaryPatientId);
    var duplicatePatient = await _repository.GetByIdAsync(dto.DuplicatePatientId);

    // 迁移医疗记录
    await MigrateMedicalRecordsAsync(duplicatePatient.Id, primaryPatient.Id);
    await MigratePrescriptionsAsync(duplicatePatient.Id, primaryPatient.Id);

    // 删除重复患者
    await _repository.DeleteAsync(duplicatePatient);

    return new PatientMergeResultDto
    {
        Success = true,
        MergedPatientId = primaryPatient.Id,
        MigratedRecordsCount = /* 计算迁移的记录数 */
    };
}
```

**预防措施**:
- 患者注册时强制身份证号验证
- 实时查重提示，显示相似患者列表
- 重要操作前二次确认患者身份

#### 问题2：身份证号格式错误和校验失败

**现象**: 用户输入身份证号格式不正确，系统校验失败

**解决方案**:
```csharp
public class PatientCreateValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateValidator()
    {
        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("身份证号不能为空")
            .Matches(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$")
            .WithMessage("身份证号格式不正确")
            .Must(BeValidIdNumber).WithMessage("身份证号校验失败");
    }

    private bool BeValidIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 18)
            return false;

        // 身份证号校验算法
        var weights = new[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        var checksums = new[] { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

        int sum = 0;
        for (int i = 0; i < 17; i++)
        {
            sum += (idNumber[i] - '0') * weights[i];
        }

        char expectedChecksum = checksums[sum % 11];
        return char.ToUpper(idNumber[17]) == expectedChecksum;
    }
}
```

**最佳实践**:
- 前端实时格式化显示，提升用户体验
- 提供身份证号校验提示和修正建议
- 支持港澳台身份证号的兼容处理

### 患者搜索与查询优化

#### 问题3：中文患者姓名搜索效率低

**现象**: 患者数量增多后，中文姓名搜索响应缓慢

**解决方案**:
```csharp
// 7级拼音码搜索算法优化
public class PatientSearchService
{
    private readonly ISearchIndex _searchIndex;

    public async Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientSearchDto dto)
    {
        var query = BuildSearchQuery(dto);

        // 使用拼音码索引加速搜索
        if (!string.IsNullOrEmpty(dto.Keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(dto.Keyword) ||          // 中文姓名
                p.PinYinCode.Contains(dto.Keyword) ||   // 拼音简码
                p.PinYinFull.Contains(dto.Keyword) ||   // 拼音全码
                p.IdNumber.Contains(dto.Keyword) ||     // 身份证号
                p.PhoneNumber.Contains(dto.Keyword)     // 手机号
            );
        }

        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderBy(p => p.Name)
            .Skip((dto.PageIndex - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ProjectTo<PatientDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<PatientDto>(patients, totalCount, dto.PageIndex, dto.PageSize);
    }

    private IQueryable<Patient> BuildSearchQuery(PatientSearchDto dto)
    {
        var query = _repository.Query();

        // 性别过滤
        if (dto.Gender.HasValue)
            query = query.Where(p => p.Gender == dto.Gender.Value);

        // 年龄范围过滤
        if (dto.MinAge.HasValue)
        {
            var maxBirthDate = DateTime.Today.AddYears(-dto.MinAge.Value);
            query = query.Where(p => p.BirthDate <= maxBirthDate);
        }

        if (dto.MaxAge.HasValue)
        {
            var minBirthDate = DateTime.Today.AddYears(-dto.MaxAge.Value);
            query = query.Where(p => p.BirthDate >= minBirthDate);
        }

        // 注册日期过滤
        if (dto.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= dto.StartDate.Value);

        if (dto.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= dto.EndDate.Value);

        return query;
    }
}
```

**性能优化策略**:
- 建立拼音码全文索引
- 实现搜索结果缓存机制
- 使用数据库索引优化查询性能
- 支持搜索历史记录和热门搜索

#### 问题4：患者信息导出Excel失败或数据丢失

**现象**: 大批量患者数据导出Excel时出现内存溢出或格式错误

**解决方案**:
```csharp
public class PatientExportService
{
    public async Task<byte[]> ExportPatientsToExcelAsync(PatientExportDto dto)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("患者数据");

        // 设置表头
        SetupExcelHeaders(worksheet);

        // 分页查询避免内存溢出
        var pageSize = 1000;
        var totalCount = await GetPatientCountAsync(dto);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var currentRow = 2; // 从第2行开始（第1行是表头）

        for (int page = 1; page <= totalPages; page++)
        {
            var patients = await GetPatientsPagedAsync(dto, page, pageSize);

            foreach (var patient in patients)
            {
                // 应用数据脱敏
                var exportData = ApplyDataMasking(patient, dto.IncludeSensitiveData);

                worksheet.Cell(currentRow, 1).Value = exportData.Name;
                worksheet.Cell(currentRow, 2).Value = exportData.GenderText;
                worksheet.Cell(currentRow, 3).Value = exportData.BirthDate?.ToString("yyyy-MM-dd");
                worksheet.Cell(currentRow, 4).Value = MaskIdNumber(exportData.IdNumber);
                worksheet.Cell(currentRow, 5).Value = MaskPhoneNumber(exportData.PhoneNumber);

                currentRow++;
            }

            // 清理内存
            patients.Clear();
            GC.Collect();
        }

        // 设置列宽
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private PatientExportDto ApplyDataMasking(PatientDto patient, bool includeSensitive)
    {
        return new PatientExportDto
        {
            Name = includeSensitive ? patient.Name : MaskName(patient.Name),
            IdNumber = includeSensitive ? patient.IdNumber : MaskIdNumber(patient.IdNumber),
            PhoneNumber = includeSensitive ? patient.PhoneNumber : MaskPhoneNumber(patient.PhoneNumber),
            Address = includeSensitive ? patient.Address : MaskAddress(patient.Address),
            // 非敏感信息始终显示
            Gender = patient.Gender,
            BirthDate = patient.BirthDate,
            BloodType = patient.BloodType,
            RegistrationDate = patient.RegistrationDate
        };
    }
}
```

**错误处理**:
- 添加异常处理和日志记录
- 实现导出进度反馈机制
- 支持大文件分块下载
- 提供导出失败重试功能

### 医疗数据隐私保护

#### 问题5：患者隐私数据泄露风险

**现象**: 敏感患者信息在系统中明文显示，存在隐私泄露风险

**解决方案**:
```csharp
public class PatientDataProtectionService
{
    private readonly IDataProtector _dataProtector;

    public PatientDataProtectionService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("Patient.SensitiveData");
    }

    // 敏感字段加密存储
    public async Task SavePatientAsync(Patient patient)
    {
        // 加密敏感字段
        if (!string.IsNullOrEmpty(patient.IdNumber))
            patient.IdNumber = _dataProtector.Protect(patient.IdNumber);

        if (!string.IsNullOrEmpty(patient.PhoneNumber))
            patient.PhoneNumber = _dataProtector.Protect(patient.PhoneNumber);

        if (!string.IsNullOrEmpty(patient.Address))
            patient.Address = _dataProtector.Protect(patient.Address);

        await _repository.UpdateAsync(patient);
    }

    // 数据脱敏显示
    public PatientDto GetPatientWithMasking(Patient patient, UserRole userRole)
    {
        var dto = _mapper.Map<PatientDto>(patient);

        // 根据用户角色决定脱敏级别
        switch (userRole)
        {
            case UserRole.Admin:
                // 管理员查看完整信息
                break;

            case UserRole.Doctor:
                // 医生查看部分脱敏信息
                dto.PhoneNumber = MaskPhoneNumber(patient.PhoneNumber);
                dto.Address = MaskAddress(patient.Address);
                break;

            default:
                // 其他角色查看高度脱敏信息
                dto.IdNumber = MaskIdNumber(patient.IdNumber);
                dto.PhoneNumber = MaskPhoneNumber(patient.PhoneNumber);
                dto.Address = MaskAddress(patient.Address);
                break;
        }

        return dto;
    }

    private string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return "***";

        return idNumber.Substring(0, 6) + "********" + idNumber.Substring(idNumber.Length - 4);
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 11)
            return "***";

        return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 4);
    }
}
```

**合规措施**:
- 实施基于角色的访问控制(RBAC)
- 记录所有敏感数据访问日志
- 定期进行数据安全审计
- 建立数据泄露应急响应机制

### 批量操作优化

#### 问题6：大批量患者导入时系统性能严重下降

**现象**: 导入数千条患者记录时，系统响应缓慢甚至崩溃

**解决方案**:
```csharp
public class PatientBatchImportService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public async Task<BatchImportResult> ImportPatientsFromExcelAsync(Stream excelStream, int batchSize = 100)
    {
        var result = new BatchImportResult();

        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets.First();

        var patients = new List<PatientCreateDto>();
        var rowCount = worksheet.Dimension.Rows;

        // 分批处理避免内存溢出
        for (int row = 2; row <= rowCount; row++) // 跳过表头
        {
            try
            {
                var patientData = ExtractPatientFromExcelRow(worksheet, row);
                patients.Add(patientData);

                // 达到批量大小时处理一批
                if (patients.Count >= batchSize)
                {
                    var batchResult = await ProcessBatchAsync(patients);
                    result.Merge(batchResult);

                    patients.Clear(); // 清空当前批次
                    GC.Collect();     // 释放内存
                }
            }
            catch (Exception ex)
            {
                result.AddError(row, ex.Message);
            }
        }

        // 处理最后一批
        if (patients.Any())
        {
            var batchResult = await ProcessBatchAsync(patients);
            result.Merge(batchResult);
        }

        return result;
    }

    private async Task<BatchImportResult> ProcessBatchAsync(List<PatientCreateDto> patientDtos)
    {
        using var scope = _scopeFactory.CreateScope();
        var patientService = scope.ServiceProvider.GetRequiredService<IPatientService>();

        var result = new BatchImportResult();

        foreach (var dto in patientDtos)
        {
            try
            {
                // 数据验证
                var validationResult = await ValidatePatientDataAsync(dto);
                if (!validationResult.IsValid)
                {
                    result.AddValidationError(dto, validationResult.Errors);
                    continue;
                }

                // 检查重复
                var isDuplicate = await patientService.CheckPatientDuplicateAsync(dto);
                if (isDuplicate)
                {
                    result.AddDuplicate(dto);
                    continue;
                }

                // 创建患者
                await patientService.CreateAsync(dto);
                result.AddSuccess(dto);
            }
            catch (Exception ex)
            {
                result.AddError(dto, ex.Message);
            }
        }

        return result;
    }
}
```

**优化策略**:
- 使用批量插入减少数据库往返
- 实现事务处理确保数据一致性
- 添加进度反馈和错误报告
- 支持导入过程暂停和恢复

## 🔧 故障排查指南

### 常见错误及解决方案

#### 错误1：患者保存时"违反唯一约束"

**排查步骤**:
1. 检查身份证号是否重复
2. 验证数据库唯一索引设置
3. 查看并发创建导致的冲突
4. 检查事务回滚情况

**解决方案**:
```csharp
// 乐观锁处理并发冲突
public async Task<Result<Guid>> CreatePatientAsync(PatientCreateDto dto)
{
    var maxRetries = 3;
    var retryCount = 0;

    while (retryCount < maxRetries)
    {
        try
        {
            // 检查是否存在重复
            var exists = await CheckPatientDuplicateAsync(dto);
            if (exists)
                return Result.Failure<Guid>("患者已存在");

            var patient = _mapper.Map<Patient>(dto);
            await _repository.AddAsync(patient);

            return Result.Success(patient.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            retryCount++;
            if (retryCount >= maxRetries)
                return Result.Failure<Guid>("创建患者失败，请重试");

            await Task.Delay(100 * retryCount); // 指数退避
        }
    }

    return Result.Failure<Guid>("创建患者失败");
}
```

#### 错误2：患者搜索返回空结果

**排查步骤**:
1. 检查搜索关键词是否正确
2. 验证拼音码生成是否准确
3. 查看数据库索引是否失效
4. 检查搜索权限设置

**调试工具**:
```csharp
// 搜索诊断工具
public class SearchDiagnostics
{
    public async Task<SearchDiagnosticResult> DiagnoseSearchAsync(string keyword)
    {
        var result = new SearchDiagnosticResult();

        // 1. 检查拼音码生成
        var pinyinCode = await GeneratePinYinCodeAsync(keyword);
        result.PinYinCode = pinyinCode;

        // 2. 检查数据库索引
        var indexExists = await CheckSearchIndexExistsAsync();
        result.IndexExists = indexExists;

        // 3. 测试不同搜索方式
        result.ByNameCount = await CountByNameAsync(keyword);
        result.ByPinYinCount = await CountByPinYinAsync(pinyinCode);
        result.ByIdNumberCount = await CountByIdNumberAsync(keyword);

        // 4. 提供优化建议
        result.Suggestions = GenerateSearchSuggestions(result);

        return result;
    }
}
```

### 性能监控指标

#### 关键性能指标(KPI)

1. **患者搜索响应时间** < 500ms
2. **患者信息加载时间** < 200ms
3. **批量导入吞吐量** > 100条/秒
4. **数据导出成功率** > 99.5%
5. **隐私数据访问延迟** < 100ms

#### 监控实现
```csharp
// 性能监控装饰器
public class PatientServiceWithMetrics : IPatientService
{
    private readonly IPatientService _innerService;
    private readonly IMetrics _metrics;

    public async Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientSearchDto dto)
    {
        using var timer = _metrics.Measure.Timer.Time("patient.search.duration");

        try
        {
            var result = await _innerService.SearchPatientsAsync(dto);

            _metrics.Measure.Counter.Mark("patient.search.success");
            _metrics.Measure.Histogram.Update("patient.search.results_count", result.TotalCount);

            return result;
        }
        catch (Exception ex)
        {
            _metrics.Measure.Counter.Mark("patient.search.error");
            throw;
        }
    }
}
```

## 📋 最佳实践检查清单

### 开发前检查
- [ ] 患者数据模型已通过合规性审查
- [ ] 隐私保护机制已实现
- [ ] 搜索性能优化已完成
- [ ] 批量操作逻辑已测试

### 部署前检查
- [ ] 数据库索引已正确创建
- [ ] 数据脱敏规则已配置
- [ ] 权限控制已验证
- [ ] 监控指标已设置

### 运行时检查
- [ ] 定期检查数据泄露风险
- [ ] 监控系统性能指标
- [ ] 验证备份恢复机制
- [ ] 审计日志完整性检查

---

**文档类型**: How-to Guide
**适用场景**: 患者数据管理问题解决
**更新时间**: 2025-11-22
**相关资源**: [患者管理教程](../tutorials/modules/patients/patient-management-tutorial.md) | [患者API参考](../reference/api/patients.md)