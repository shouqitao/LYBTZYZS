# Formula Module (验方管理模块)

## 📋 项目概述

### 项目定位
**Formula 模块**是凌隐宝堂中医诊所系统的**经典验方管理模块**，负责中医经典方剂和个人临床验方的收录、管理和应用。作为中医传承与创新的核心模块，为处方开具提供标准化的方剂模板和临床经验积累平台。

### 核心价值
- 📚 **经典方剂传承**: 收录历代名方，传承中医精髓
- 👨‍⚕️ **个人验方积累**: 医生临床经验方剂的系统化管理
- ⚡ **快速处方开具**: 基于验方模板快速生成处方
- 🔍 **智能方剂检索**: 按症状、功效、药材组合搜索验方
- 📊 **使用效果分析**: 跟踪验方临床应用效果
- 🌐 **协作共享机制**: 支持验方分享和临床讨论

### 业务定位 (v1.0)
```
经典医籍 + 临床经验
    ↓ 录入
Formula (验方模板库) ← 本模块
    ↓ 应用
Prescriptions (处方开具) ← 快速生成处方
    ↓ 反馈
临床效果评价 ← 验方疗效统计
```

## 🏗️ 技术架构

### UltraThink双层架构实现
```
FormulaService (主服务 - 纯委托层)
├── FormulaQueryService (查询专业层)
│   ├── 验方检索搜索 (名称、功效、症状、药材)
│   ├── 分类统计分析 (功效分类、药材使用频次)
│   ├── 使用效果统计 (应用次数、临床反馈)
│   └── 验方对比分析 (相似方剂、组成差异)
└── FormulaBusinessService (业务逻辑层)
    ├── 验方信息管理 (CRUD操作、状态控制)
    ├── 方剂组成管理 (药材配伍、用量调整)
    ├── 临床应用记录 (使用统计、效果反馈)
    ├── 验方分享管理 (权限控制、协作功能)
    └── 智能推荐服务 (相似方剂、适症推荐)
```

### 技术栈配置
```csharp
// 基础技术栈
- .NET 8.0
- Entity Framework Core 8.0.17
- AutoMapper 配置
- BCrypt.Net 密码处理
- 依赖注入模式

// 模块注册 (Program.cs)
builder.Services.AddFormulaModule();

public static class FormulaModuleExtensions
{
    public static IServiceCollection AddFormulaModule(this IServiceCollection services)
    {
        // Repository Layer
        services.AddScoped<IFormulaRepository, FormulaRepository>();
        services.AddScoped<IFormulaItemRepository, FormulaItemRepository>();
        services.AddScoped<IFormulaUsageLogRepository, FormulaUsageLogRepository>();
        
        // Service Layer - UltraThink双层架构
        services.AddScoped<FormulaQueryService>();
        services.AddScoped<FormulaBusinessService>();
        services.AddScoped<IFormulaService, FormulaService>(); // 纯委托
        
        // 专业服务
        services.AddScoped<IFormulaRecommendationService, FormulaRecommendationService>();
        services.AddScoped<IFormulaAnalysisService, FormulaAnalysisService>();
        
        return services;
    }
}
```

### 核心实体模型
```csharp
public class FormulaModel : BaseEntity
{
    // 基本信息
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }            // 验方名称
    
    [MaxLength(50)]
    public string? Source { get; set; }         // 方剂来源(医籍、医家)
    
    [MaxLength(50)]
    public string? Dynasty { get; set; }        // 朝代
    
    // 分类信息
    [MaxLength(50)]
    public string? Category { get; set; }       // 方剂分类
    
    public FormulaType Type { get; set; }       // 方剂类型
    
    // 功效信息
    [MaxLength(300)]
    public string? Functions { get; set; }      // 功效主治
    
    [MaxLength(200)]
    public string? Indications { get; set; }    // 适应症
    
    [MaxLength(200)]
    public string? Symptoms { get; set; }       // 主症
    
    [MaxLength(200)]
    public string? Contraindications { get; set; } // 禁忌症
    
    // 方解信息
    [MaxLength(500)]
    public string? Composition { get; set; }    // 方义方解
    
    [MaxLength(200)]
    public string? Preparation { get; set; }    // 制法
    
    [MaxLength(200)]
    public string? Usage { get; set; }          // 用法用量
    
    [MaxLength(300)]
    public string? Instructions { get; set; }   // 服用说明
    
    // 临床信息
    [MaxLength(300)]
    public string? ClinicalNote { get; set; }   // 临床应用说明
    
    [MaxLength(200)]
    public string? Modifications { get; set; }  // 加减变化
    
    [MaxLength(300)]
    public string? ModernApplication { get; set; } // 现代应用
    
    // 创建和分享信息
    [Required]
    public Guid CreatedBy { get; set; }         // 创建者
    public FormulaSource SourceType { get; set; } // 来源类型
    public FormulaVisibility Visibility { get; set; } // 可见性
    public bool IsShared { get; set; } = false; // 是否共享
    
    // 统计信息
    public int UsageCount { get; set; } = 0;    // 使用次数
    public DateTime? LastUsedAt { get; set; }   // 最后使用时间
    public decimal? EffectivenessRating { get; set; } // 有效性评分
    public int RatingCount { get; set; } = 0;   // 评分人数
    
    // 状态信息
    public FormulaStatus Status { get; set; }   // 方剂状态
    public bool IsActive { get; set; } = true;  // 是否启用
    
    // 导航属性
    public virtual ICollection<FormulaItemModel> Items { get; set; } = [];
    public virtual ICollection<FormulaUsageLogModel> UsageLogs { get; set; } = [];
    public virtual ICollection<PrescriptionModel> Prescriptions { get; set; } = [];
}

public class FormulaItemModel : BaseEntity
{
    [Required]
    public Guid FormulaId { get; set; }         // 验方ID
    [Required] 
    public Guid HerbId { get; set; }            // 药材ID
    
    // 药材使用信息
    public decimal Dosage { get; set; }         // 标准用量(克)
    public decimal? MinDosage { get; set; }     // 最小用量
    public decimal? MaxDosage { get; set; }     // 最大用量
    
    [MaxLength(50)]
    public string? Unit { get; set; }           // 计量单位
    
    public HerbRole Role { get; set; }          // 药物角色(君臣佐使)
    
    [MaxLength(100)]
    public string? Usage { get; set; }          // 特殊用法
    
    [MaxLength(200)]
    public string? Notes { get; set; }          // 备注说明
    
    public int? Order { get; set; }             // 排序序号
    
    // 导航属性
    public virtual FormulaModel Formula { get; set; }
    public virtual HerbModel Herb { get; set; }
}

public class FormulaUsageLogModel : BaseEntity
{
    [Required]
    public Guid FormulaId { get; set; }         // 验方ID
    [Required]
    public Guid UsedBy { get; set; }            // 使用医生
    public Guid? PrescriptionId { get; set; }   // 关联处方
    public Guid? PatientId { get; set; }        // 患者ID
    
    // 使用信息
    public DateTime UsageDate { get; set; }     // 使用日期
    public decimal? DosageAdjustment { get; set; } // 用量调整倍数
    
    [MaxLength(300)]
    public string? Modifications { get; set; }   // 加减变化
    
    [MaxLength(300)]
    public string? ClinicalNote { get; set; }   // 临床观察
    
    // 效果评价
    public EffectivenessLevel? Effectiveness { get; set; } // 有效性
    
    [MaxLength(300)]
    public string? EffectNote { get; set; }     // 效果说明
    
    public DateTime? FollowUpDate { get; set; } // 随访日期
    
    // 导航属性
    public virtual FormulaModel Formula { get; set; }
}

// 枚举定义
public enum FormulaType
{
    Decoction = 0,    // 汤剂
    Powder = 1,       // 散剂  
    Pill = 2,         // 丸剂
    Paste = 3,        // 膏剂
    Other = 99        // 其他
}

public enum FormulaSource
{
    Classical = 0,    // 经典方剂
    Personal = 1,     // 个人验方
    Modern = 2,       // 现代方剂
    Folk = 3          // 民间验方
}

public enum FormulaVisibility
{
    Private = 0,      // 私有
    Shared = 1,       // 共享
    Public = 2        // 公开
}

public enum FormulaStatus
{
    Draft = 0,        // 草稿
    Active = 1,       // 活跃
    Archived = 2,     // 归档
    Deprecated = 3    // 弃用
}

public enum HerbRole
{
    Monarch = 0,      // 君药
    Minister = 1,     // 臣药
    Assistant = 2,    // 佐药
    Guide = 3         // 使药
}

public enum EffectivenessLevel
{
    Excellent = 5,    // 显效
    Good = 4,         // 有效
    Fair = 3,         // 一般
    Poor = 2,         // 无效
    Adverse = 1       // 不良
}
```

## 🎯 功能规范

### 核心业务功能

#### 1. 验方信息管理
```csharp
// 业务服务实现
public class FormulaBusinessService
{
    // 创建验方
    public async Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto dto)
    {
        // 检查验方名称重复
        var existing = await _repository.GetByNameAndCreatorAsync(dto.Name, dto.CreatedBy);
        if (existing != null)
            return ServiceResult<FormulaDto>.Failure($"您已创建了名为【{dto.Name}】的验方");
        
        // 验证方剂组成
        if (dto.Items?.Any() != true)
            return ServiceResult<FormulaDto>.Failure("验方必须包含至少一味药材");
        
        // 验证君臣佐使配伍
        var herbRoleValidation = ValidateHerbRoles(dto.Items);
        if (!herbRoleValidation.IsValid)
            return ServiceResult<FormulaDto>.Failure(herbRoleValidation.ErrorMessage);
        
        var formula = _mapper.Map<FormulaModel>(dto);
        formula.Status = FormulaStatus.Draft;
        formula.UsageCount = 0;
        
        await _repository.CreateAsync(formula);
        
        // 创建方剂组成
        foreach (var itemDto in dto.Items)
        {
            var item = _mapper.Map<FormulaItemModel>(itemDto);
            item.FormulaId = formula.Id;
            await _formulaItemRepository.CreateAsync(item);
        }
        
        var result = await GetFormulaWithItemsAsync(formula.Id);
        return ServiceResult<FormulaDto>.Success(result);
    }
    
    // 验证君臣佐使配伍
    private ValidationResult ValidateHerbRoles(List<FormulaItemCreateDto> items)
    {
        var roles = items.GroupBy(i => i.Role).ToDictionary(g => g.Key, g => g.Count());
        
        // 君药检查：必须有且仅有1-2味
        if (!roles.ContainsKey(HerbRole.Monarch))
            return ValidationResult.Failure("验方必须包含君药");
        if (roles[HerbRole.Monarch] > 2)
            return ValidationResult.Failure("君药不宜超过2味");
        
        // 臣药检查：通常需要有臣药
        if (!roles.ContainsKey(HerbRole.Minister))
            return ValidationResult.Failure("验方通常需要包含臣药");
        
        // 总药味数检查：不宜过多
        if (items.Count > 20)
            return ValidationResult.Failure("验方药味数不宜超过20味");
        if (items.Count < 3)
            return ValidationResult.Failure("验方药味数不宜少于3味");
        
        return ValidationResult.Success();
    }
    
    // 添加验方药材
    public async Task<ServiceResult<FormulaDto>> AddFormulaItemAsync(
        Guid formulaId, FormulaItemCreateDto dto)
    {
        var formula = await _repository.GetByIdWithItemsAsync(formulaId);
        if (formula == null)
            return ServiceResult<FormulaDto>.Failure("验方不存在");
            
        if (formula.Status != FormulaStatus.Draft)
            return ServiceResult<FormulaDto>.Failure("只能修改草稿状态的验方");
        
        // 验证药材存在
        var herb = await _herbRepository.GetByIdAsync(dto.HerbId);
        if (herb == null)
            return ServiceResult<FormulaDto>.Failure("药材不存在");
        
        // 检查重复药材
        if (formula.Items.Any(i => i.HerbId == dto.HerbId))
            return ServiceResult<FormulaDto>.Failure("验方中已包含该药材");
        
        var item = new FormulaItemModel
        {
            FormulaId = formulaId,
            HerbId = dto.HerbId,
            Dosage = dto.Dosage,
            MinDosage = dto.MinDosage,
            MaxDosage = dto.MaxDosage,
            Role = dto.Role,
            Usage = dto.Usage,
            Notes = dto.Notes,
            Order = formula.Items.Count + 1
        };
        
        await _formulaItemRepository.CreateAsync(item);
        
        var result = await GetFormulaWithItemsAsync(formulaId);
        return ServiceResult<FormulaDto>.Success(result);
    }
    
    // 发布验方
    public async Task<ServiceResult<bool>> PublishFormulaAsync(Guid id)
    {
        var formula = await _repository.GetByIdWithItemsAsync(id);
        if (formula == null)
            return ServiceResult<FormulaDto>.Failure("验方不存在");
        
        // 验证方剂完整性
        var validation = ValidateFormulaCompleteness(formula);
        if (!validation.IsValid)
            return ServiceResult<bool>.Failure(validation.ErrorMessage);
        
        formula.Status = FormulaStatus.Active;
        formula.UpdateTime = DateTime.Now;
        
        await _repository.UpdateAsync(formula);
        return ServiceResult<bool>.Success(true);
    }
}
```

#### 2. 临床应用记录
```csharp
// 使用记录管理
public async Task<ServiceResult<bool>> RecordFormulaUsageAsync(
    FormulaUsageRecordDto dto)
{
    var formula = await _repository.GetByIdAsync(dto.FormulaId);
    if (formula == null)
        return ServiceResult<bool>.Failure("验方不存在");
    
    // 创建使用记录
    var usageLog = new FormulaUsageLogModel
    {
        FormulaId = dto.FormulaId,
        UsedBy = dto.UsedBy,
        PrescriptionId = dto.PrescriptionId,
        PatientId = dto.PatientId,
        UsageDate = DateTime.Now,
        DosageAdjustment = dto.DosageAdjustment,
        Modifications = dto.Modifications,
        ClinicalNote = dto.ClinicalNote
    };
    
    await _usageLogRepository.CreateAsync(usageLog);
    
    // 更新使用统计
    formula.UsageCount++;
    formula.LastUsedAt = DateTime.Now;
    await _repository.UpdateAsync(formula);
    
    return ServiceResult<bool>.Success(true);
}

// 效果反馈记录
public async Task<ServiceResult<bool>> RecordEffectivenessAsync(
    Guid usageLogId, EffectivenessRecordDto dto)
{
    var usageLog = await _usageLogRepository.GetByIdAsync(usageLogId);
    if (usageLog == null)
        return ServiceResult<bool>.Failure("使用记录不存在");
    
    // 更新效果记录
    usageLog.Effectiveness = dto.Effectiveness;
    usageLog.EffectNote = dto.EffectNote;
    usageLog.FollowUpDate = DateTime.Now;
    
    await _usageLogRepository.UpdateAsync(usageLog);
    
    // 更新验方总体评分
    await UpdateFormulaEffectivenessRatingAsync(usageLog.FormulaId);
    
    return ServiceResult<bool>.Success(true);
}

private async Task UpdateFormulaEffectivenessRatingAsync(Guid formulaId)
{
    var effectivenessRecords = await _usageLogRepository
        .GetEffectivenessRecordsAsync(formulaId);
    
    if (effectivenessRecords.Any())
    {
        var formula = await _repository.GetByIdAsync(formulaId);
        formula.EffectivenessRating = effectivenessRecords
            .Average(r => (decimal)r.Effectiveness);
        formula.RatingCount = effectivenessRecords.Count;
        
        await _repository.UpdateAsync(formula);
    }
}
```

#### 3. 验方分享管理
```csharp
// 分享验方
public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, FormulaVisibility visibility)
{
    var formula = await _repository.GetByIdAsync(id);
    if (formula == null)
        return ServiceResult<bool>.Failure("验方不存在");
    
    // 检查分享权限
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (formula.CreatedBy != currentUserId)
        return ServiceResult<bool>.Failure("只能分享自己创建的验方");
    
    if (formula.Status != FormulaStatus.Active)
        return ServiceResult<bool>.Failure("只能分享已发布的验方");
    
    formula.Visibility = visibility;
    formula.IsShared = visibility != FormulaVisibility.Private;
    formula.UpdateTime = DateTime.Now;
    
    await _repository.UpdateAsync(formula);
    return ServiceResult<bool>.Success(true);
}

// 复制验方(基于共享验方创建个人副本)
public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid sourceId, string newName)
{
    var sourceFormula = await _repository.GetByIdWithItemsAsync(sourceId);
    if (sourceFormula == null)
        return ServiceResult<FormulaDto>.Failure("源验方不存在");
    
    // 检查访问权限
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (sourceFormula.Visibility == FormulaVisibility.Private && 
        sourceFormula.CreatedBy != currentUserId)
        return ServiceResult<FormulaDto>.Failure("无权访问该验方");
    
    // 创建副本
    var clonedFormula = new FormulaModel
    {
        Name = newName,
        Source = sourceFormula.Source,
        Category = sourceFormula.Category,
        Type = sourceFormula.Type,
        Functions = sourceFormula.Functions,
        Indications = sourceFormula.Indications,
        Symptoms = sourceFormula.Symptoms,
        Composition = sourceFormula.Composition,
        Usage = sourceFormula.Usage,
        Instructions = sourceFormula.Instructions,
        CreatedBy = currentUserId,
        SourceType = FormulaSource.Personal,
        Visibility = FormulaVisibility.Private,
        Status = FormulaStatus.Draft
    };
    
    await _repository.CreateAsync(clonedFormula);
    
    // 复制药材组成
    foreach (var sourceItem in sourceFormula.Items)
    {
        var clonedItem = new FormulaItemModel
        {
            FormulaId = clonedFormula.Id,
            HerbId = sourceItem.HerbId,
            Dosage = sourceItem.Dosage,
            MinDosage = sourceItem.MinDosage,
            MaxDosage = sourceItem.MaxDosage,
            Role = sourceItem.Role,
            Usage = sourceItem.Usage,
            Notes = sourceItem.Notes,
            Order = sourceItem.Order
        };
        
        await _formulaItemRepository.CreateAsync(clonedItem);
    }
    
    var result = await GetFormulaWithItemsAsync(clonedFormula.Id);
    return ServiceResult<FormulaDto>.Success(result);
}
```

### 查询服务专业功能

#### 1. 智能方剂检索
```csharp
public class FormulaQueryService
{
    // 高级搜索
    public async Task<ServiceResult<PagedResult<FormulaSearchResultDto>>> SearchFormulasAsync(
        FormulaSearchDto criteria)
    {
        var query = _repository.GetQueryable()
            .Include(f => f.Items)
            .ThenInclude(i => i.Herb);
        
        // 访问权限过滤
        var currentUserId = _currentUserService.GetCurrentUserId();
        query = query.Where(f => f.Visibility == FormulaVisibility.Public ||
                                f.CreatedBy == currentUserId ||
                                (f.Visibility == FormulaVisibility.Shared && f.IsShared));
        
        // 名称搜索
        if (!string.IsNullOrEmpty(criteria.Name))
            query = query.Where(f => f.Name.Contains(criteria.Name));
        
        // 功效搜索
        if (!string.IsNullOrEmpty(criteria.Functions))
            query = query.Where(f => f.Functions != null && f.Functions.Contains(criteria.Functions));
        
        // 症状搜索
        if (!string.IsNullOrEmpty(criteria.Symptoms))
            query = query.Where(f => f.Symptoms != null && f.Symptoms.Contains(criteria.Symptoms) ||
                                   f.Indications != null && f.Indications.Contains(criteria.Symptoms));
        
        // 药材组合搜索
        if (criteria.HerbIds?.Any() == true)
        {
            foreach (var herbId in criteria.HerbIds)
            {
                query = query.Where(f => f.Items.Any(i => i.HerbId == herbId));
            }
        }
        
        // 分类过滤
        if (!string.IsNullOrEmpty(criteria.Category))
            query = query.Where(f => f.Category == criteria.Category);
        
        // 类型过滤
        if (criteria.Type.HasValue)
            query = query.Where(f => f.Type == criteria.Type.Value);
        
        // 来源过滤
        if (criteria.SourceType.HasValue)
            query = query.Where(f => f.SourceType == criteria.SourceType.Value);
        
        // 排序
        switch (criteria.SortBy)
        {
            case FormulaSortBy.UsageCount:
                query = criteria.SortDirection == SortDirection.Descending 
                    ? query.OrderByDescending(f => f.UsageCount)
                    : query.OrderBy(f => f.UsageCount);
                break;
            case FormulaSortBy.EffectivenessRating:
                query = query.Where(f => f.EffectivenessRating.HasValue)
                           .OrderByDescending(f => f.EffectivenessRating);
                break;
            case FormulaSortBy.CreateTime:
                query = criteria.SortDirection == SortDirection.Descending 
                    ? query.OrderByDescending(f => f.CreateTime)
                    : query.OrderBy(f => f.CreateTime);
                break;
            default:
                query = query.OrderBy(f => f.Name);
                break;
        }
        
        var pagedResult = await _repository.GetPagedAsync(query, criteria.Page, criteria.PageSize);
        return ServiceResult<PagedResult<FormulaSearchResultDto>>.Success(pagedResult);
    }
    
    // 相似方剂推荐
    public async Task<ServiceResult<List<SimilarFormulaDto>>> GetSimilarFormulasAsync(
        Guid formulaId, int limit = 5)
    {
        var sourceFormula = await _repository.GetByIdWithItemsAsync(formulaId);
        if (sourceFormula == null)
            return ServiceResult<List<SimilarFormulaDto>>.Failure("源验方不存在");
        
        var sourceHerbIds = sourceFormula.Items.Select(i => i.HerbId).ToHashSet();
        
        // 查找包含相同药材的其他方剂
        var candidateFormulas = await _repository.GetFormulasWithSimilarHerbsAsync(
            sourceHerbIds, formulaId);
        
        var similarities = candidateFormulas.Select(f => new
        {
            Formula = f,
            CommonHerbs = f.Items.Count(i => sourceHerbIds.Contains(i.HerbId)),
            TotalHerbs = f.Items.Count,
            SimilarityScore = CalculateSimilarityScore(sourceFormula.Items, f.Items)
        })
        .Where(s => s.SimilarityScore > 0.3) // 相似度阈值
        .OrderByDescending(s => s.SimilarityScore)
        .Take(limit)
        .ToList();
        
        var results = similarities.Select(s => new SimilarFormulaDto
        {
            FormulaId = s.Formula.Id,
            Name = s.Formula.Name,
            Functions = s.Formula.Functions,
            CommonHerbCount = s.CommonHerbs,
            TotalHerbCount = s.TotalHerbs,
            SimilarityScore = s.SimilarityScore,
            UsageCount = s.Formula.UsageCount
        }).ToList();
        
        return ServiceResult<List<SimilarFormulaDto>>.Success(results);
    }
    
    // 验方统计分析
    public async Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync()
    {
        var formulas = await _repository.GetAllActiveFormulasAsync();
        
        var statistics = new FormulaStatisticsDto
        {
            TotalCount = formulas.Count,
            ClassicalCount = formulas.Count(f => f.SourceType == FormulaSource.Classical),
            PersonalCount = formulas.Count(f => f.SourceType == FormulaSource.Personal),
            SharedCount = formulas.Count(f => f.IsShared),
            
            // 分类统计
            CategoryStats = formulas.GroupBy(f => f.Category ?? "未分类")
                .Select(g => new CategoryStatDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AverageUsage = g.Average(f => f.UsageCount)
                })
                .OrderByDescending(s => s.Count)
                .ToList(),
                
            // 最受欢迎的验方
            MostUsedFormulas = formulas
                .OrderByDescending(f => f.UsageCount)
                .Take(10)
                .Select(f => new PopularFormulaDto
                {
                    Name = f.Name,
                    UsageCount = f.UsageCount,
                    EffectivenessRating = f.EffectivenessRating
                })
                .ToList(),
                
            // 药材使用频次统计
            HerbUsageStats = formulas
                .SelectMany(f => f.Items)
                .GroupBy(i => new { i.HerbId, i.Herb.Name })
                .Select(g => new HerbUsageInFormulaDto
                {
                    HerbName = g.Key.Name,
                    FormulaCount = g.Count(),
                    AverageDosage = g.Average(i => i.Dosage),
                    RoleDistribution = g.GroupBy(i => i.Role)
                        .ToDictionary(r => r.Key.ToString(), r => r.Count())
                })
                .OrderByDescending(h => h.FormulaCount)
                .Take(20)
                .ToList()
        };
        
        return ServiceResult<FormulaStatisticsDto>.Success(statistics);
    }
}
```

#### 2. 智能推荐服务
```csharp
public class FormulaRecommendationService : IFormulaRecommendationService
{
    // 基于症状推荐验方
    public async Task<ServiceResult<List<FormulaRecommendationDto>>> RecommendFormulasBySymptoms(
        List<string> symptoms)
    {
        if (!symptoms.Any())
            return ServiceResult<List<FormulaRecommendationDto>>.Failure("请提供症状信息");
        
        var formulas = await _formulaRepository.GetActiveFormulasWithSymptomMatchAsync(symptoms);
        
        var recommendations = formulas.Select(f => new
        {
            Formula = f,
            MatchScore = CalculateSymptomMatchScore(f, symptoms),
            UsageWeight = Math.Log(f.UsageCount + 1), // 使用频次权重
            EffectivenessWeight = f.EffectivenessRating ?? 3.0m // 有效性权重
        })
        .Where(r => r.MatchScore > 0.5) // 匹配度阈值
        .Select(r => new FormulaRecommendationDto
        {
            FormulaId = r.Formula.Id,
            Name = r.Formula.Name,
            Functions = r.Formula.Functions,
            MatchedSymptoms = GetMatchedSymptoms(r.Formula, symptoms),
            RecommendationScore = r.MatchScore * 0.6m + 
                                 r.UsageWeight * 0.2m + 
                                 r.EffectivenessWeight * 0.2m,
            UsageCount = r.Formula.UsageCount,
            EffectivenessRating = r.Formula.EffectivenessRating,
            ReasonCode = GetRecommendationReason(r.MatchScore, r.UsageWeight)
        })
        .OrderByDescending(r => r.RecommendationScore)
        .Take(10)
        .ToList();
        
        return ServiceResult<List<FormulaRecommendationDto>>.Success(recommendations);
    }
    
    // 基于处方历史推荐验方
    public async Task<ServiceResult<List<FormulaRecommendationDto>>> RecommendFormulasForDoctor(
        Guid doctorId)
    {
        // 获取医生最近的处方用药习惯
        var recentPrescriptions = await _prescriptionRepository
            .GetRecentPrescriptionsByDoctorAsync(doctorId, 30); // 最近30个处方
        
        var frequentHerbs = recentPrescriptions
            .SelectMany(p => p.Items)
            .GroupBy(i => i.HerbId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();
        
        // 推荐包含常用药材的验方
        var recommendations = await _formulaRepository
            .GetFormulasContainingHerbsAsync(frequentHerbs);
        
        var results = recommendations.Select(f => new FormulaRecommendationDto
        {
            FormulaId = f.Id,
            Name = f.Name,
            Functions = f.Functions,
            RecommendationScore = CalculatePersonalizedScore(f, frequentHerbs),
            ReasonCode = "基于您的用药习惯推荐"
        })
        .OrderByDescending(r => r.RecommendationScore)
        .Take(8)
        .ToList();
        
        return ServiceResult<List<FormulaRecommendationDto>>.Success(results);
    }
}
```

### 主服务委托层
```csharp
public class FormulaService : IFormulaService
{
    private readonly FormulaQueryService _queryService;
    private readonly FormulaBusinessService _businessService;
    
    public FormulaService(
        FormulaQueryService queryService,
        FormulaBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }
    
    // 纯委托实现 - 查询功能
    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);
        
    public async Task<ServiceResult<PagedResult<FormulaSearchResultDto>>> SearchFormulasAsync(
        FormulaSearchDto criteria)
        => await _queryService.SearchFormulasAsync(criteria);
        
    public async Task<ServiceResult<List<SimilarFormulaDto>>> GetSimilarFormulasAsync(
        Guid formulaId, int limit = 5)
        => await _queryService.GetSimilarFormulasAsync(formulaId, limit);
    
    // 纯委托实现 - 业务功能
    public async Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto dto)
        => await _businessService.CreateFormulaAsync(dto);
        
    public async Task<ServiceResult<bool>> PublishFormulaAsync(Guid id)
        => await _businessService.PublishFormulaAsync(id);
        
    public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, FormulaVisibility visibility)
        => await _businessService.ShareFormulaAsync(id, visibility);
}
```

## 🔧 开发标准

### 代码质量要求
- **零编译警告**: 严格遵循.NET 8最佳实践
- **异步优先**: 所有数据库操作使用async/await
- **LINQ安全**: 杜绝原生SQL，防止注入攻击
- **异常处理**: 完整的try-catch和错误日志记录

### 数据传输对象 (DTOs)
```csharp
// 创建验方DTO
public class FormulaCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [MaxLength(50)]
    public string? Source { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public FormulaType Type { get; set; } = FormulaType.Decoction;
    
    [MaxLength(300)]
    public string? Functions { get; set; }
    
    [MaxLength(200)]
    public string? Indications { get; set; }
    
    [Required]
    public Guid CreatedBy { get; set; }
    
    public FormulaSource SourceType { get; set; } = FormulaSource.Personal;
    
    public List<FormulaItemCreateDto> Items { get; set; } = [];
}

// 验方药材项创建DTO
public class FormulaItemCreateDto
{
    [Required]
    public Guid HerbId { get; set; }
    
    [Required]
    [Range(0.1, 200)]
    public decimal Dosage { get; set; }
    
    public decimal? MinDosage { get; set; }
    public decimal? MaxDosage { get; set; }
    
    public HerbRole Role { get; set; }
    
    public string? Usage { get; set; }
    public string? Notes { get; set; }
}

// 验方搜索DTO
public class FormulaSearchDto : PagedRequestDto
{
    public string? Name { get; set; }
    public string? Functions { get; set; }
    public string? Symptoms { get; set; }
    public string? Category { get; set; }
    public FormulaType? Type { get; set; }
    public FormulaSource? SourceType { get; set; }
    public List<Guid>? HerbIds { get; set; }
    public FormulaSortBy SortBy { get; set; } = FormulaSortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}

public enum FormulaSortBy
{
    Name = 0,
    UsageCount = 1,
    EffectivenessRating = 2,
    CreateTime = 3
}
```

## 🔗 集成接口

### API控制器实现
```csharp
[ApiController]
[ApiVersion("1")]  
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class FormulasController : BaseApiController
{
    private readonly IFormulaService _formulaService;
    private readonly IFormulaRecommendationService _recommendationService;
    
    public FormulasController(
        IFormulaService formulaService,
        IFormulaRecommendationService recommendationService,
        ILogger<FormulasController> logger,
        IMemoryCache cache) : base(logger, cache)
    {
        _formulaService = formulaService;
        _recommendationService = recommendationService;
    }
    
    /// <summary>
    /// 搜索验方
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<FormulaSearchResultDto>>>> SearchFormulas(
        [FromQuery] FormulaSearchDto criteria)
    {
        try
        {
            var result = await _formulaService.SearchFormulasAsync(criteria);
            return HandleServiceResult(result, "搜索验方成功");
        }
        catch (Exception ex)
        {
            return HandleException<PagedResult<FormulaSearchResultDto>>(ex, "搜索验方");
        }
    }
    
    /// <summary>
    /// 创建验方
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> CreateFormula([FromBody] FormulaCreateDto dto)
    {
        try
        {
            var validation = ValidateModel<FormulaDto>(dto);
            if (validation != null) return validation;
            
            var result = await _formulaService.CreateFormulaAsync(dto);
            return HandleServiceResult(result, "创建验方成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<FormulaDto>(ex, "创建验方", dto.Name);
        }
    }
    
    /// <summary>
    /// 基于症状推荐验方
    /// </summary>
    [HttpPost("recommend-by-symptoms")]
    public async Task<ActionResult<ApiResponse<List<FormulaRecommendationDto>>>> RecommendBySymptoms(
        [FromBody] SymptomBasedRecommendationDto dto)
    {
        try
        {
            if (!dto.Symptoms?.Any() == true)
                return BadRequest(ApiResponse<List<FormulaRecommendationDto>>.Failure("请提供症状信息"));
            
            var result = await _recommendationService.RecommendFormulasBySymptoms(dto.Symptoms);
            return HandleServiceResult(result, "获取推荐验方成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<FormulaRecommendationDto>>(ex, "症状推荐验方");
        }
    }
    
    /// <summary>
    /// 获取相似验方
    /// </summary>
    [HttpGet("{id:guid}/similar")]
    public async Task<ActionResult<ApiResponse<List<SimilarFormulaDto>>>> GetSimilarFormulas(
        Guid id, [FromQuery] int limit = 5)
    {
        try
        {
            var validation = ValidateGuid<List<SimilarFormulaDto>>(id, "验方ID");
            if (validation != null) return validation;
            
            var result = await _formulaService.GetSimilarFormulasAsync(id, limit);
            return HandleServiceResult(result, "获取相似验方成功");
        }
        catch (Exception ex)
        {
            return HandleException<List<SimilarFormulaDto>>(ex, "获取相似验方", id);
        }
    }
    
    /// <summary>
    /// 分享验方
    /// </summary>
    [HttpPost("{id:guid}/share")]
    public async Task<ActionResult<ApiResponse<bool>>> ShareFormula(
        Guid id, [FromBody] ShareFormulaDto dto)
    {
        try
        {
            var validation = ValidateGuid<bool>(id, "验方ID");
            if (validation != null) return validation;
            
            var result = await _formulaService.ShareFormulaAsync(id, dto.Visibility);
            return HandleServiceResult(result, "分享验方成功");
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, "分享验方", id);
        }
    }
    
    /// <summary>
    /// 复制验方
    /// </summary>
    [HttpPost("{id:guid}/clone")]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> CloneFormula(
        Guid id, [FromBody] CloneFormulaDto dto)
    {
        try
        {
            var validation = ValidateGuid<FormulaDto>(id, "验方ID");
            if (validation != null) return validation;
            
            var result = await _formulaService.CloneFormulaAsync(id, dto.NewName);
            return HandleServiceResult(result, "复制验方成功", true);
        }
        catch (Exception ex)
        {
            return HandleException<FormulaDto>(ex, "复制验方", id);
        }
    }
}
```

### 与其他模块集成

#### 1. Prescriptions集成
```csharp
// Prescriptions模块调用验方信息
public async Task<ServiceResult<List<FormulaDto>>> GetAvailableFormulasAsync()
{
    var criteria = new FormulaSearchDto
    {
        PageSize = 1000 // 获取所有可用验方
    };
    
    var result = await _formulaService.SearchFormulasAsync(criteria);
    return ServiceResult<List<FormulaDto>>.Success(result.Data?.Items?.ToList() ?? []);
}

// 记录验方使用
public async Task<ServiceResult<bool>> RecordFormulaUsageAsync(Guid formulaId, Guid prescriptionId)
{
    var usageRecord = new FormulaUsageRecordDto
    {
        FormulaId = formulaId,
        PrescriptionId = prescriptionId,
        UsedBy = _currentUserService.GetCurrentUserId()
    };
    
    return await _formulaService.RecordFormulaUsageAsync(usageRecord);
}
```

#### 2. Herbs集成
```csharp
// 验证验方中的药材有效性
public async Task<ServiceResult<bool>> ValidateFormulaHerbsAsync(List<Guid> herbIds)
{
    var herbs = await _herbService.GetHerbsByIdsAsync(herbIds);
    var unavailableHerbs = herbIds.Where(id => 
        !herbs.Data?.Any(h => h.Id == id && h.Status == HerbStatus.Available) == true).ToList();
    
    if (unavailableHerbs.Any())
        return ServiceResult<bool>.Failure("验方包含不可用的药材");
        
    return ServiceResult<bool>.Success(true);
}
```

## ⚙️ 配置管理

### 验方管理配置选项
```csharp
public class FormulaOptions
{
    public const string SectionName = "Formula";
    
    /// <summary>
    /// 最大药味数量
    /// </summary>
    public int MaxHerbCount { get; set; } = 20;
    
    /// <summary>
    /// 最小药味数量
    /// </summary>
    public int MinHerbCount { get; set; } = 3;
    
    /// <summary>
    /// 相似度计算阈值
    /// </summary>
    public decimal SimilarityThreshold { get; set; } = 0.3m;
    
    /// <summary>
    /// 推荐验方数量限制
    /// </summary>
    public int RecommendationLimit { get; set; } = 10;
    
    /// <summary>
    /// 启用智能推荐
    /// </summary>
    public bool EnableSmartRecommendation { get; set; } = true;
    
    /// <summary>
    /// 效果评价有效期(天)
    /// </summary>
    public int EffectivenessValidDays { get; set; } = 30;
    
    /// <summary>
    /// 默认方剂分类
    /// </summary>
    public List<string> DefaultCategories { get; set; } = 
    [
        "解表剂", "泻下剂", "和解剂", "清热剂", "祛暑剂",
        "温里剂", "补益剂", "固涩剂", "安神剂", "开窍剂",
        "理血剂", "理气剂", "消食剂", "驱虫剂", "涌吐剂",
        "祛风湿剂", "祛痰剂", "治燥剂", "治风剂"
    ];
}
```

### 应用配置
```json
{
  "Formula": {
    "MaxHerbCount": 20,
    "MinHerbCount": 3,
    "SimilarityThreshold": 0.3,
    "RecommendationLimit": 10,
    "EnableSmartRecommendation": true,
    "EffectivenessValidDays": 30,
    "DefaultCategories": [
      "解表剂", "泻下剂", "和解剂", "清热剂"
    ]
  },
  "Logging": {
    "LogLevel": {
      "LYBT.Module.Formula": "Information"
    }
  }
}
```

## 🧪 测试规范

### 单元测试要求

#### 1. 业务服务测试
```csharp
[Test]
public async Task CreateFormulaAsync_ValidFormula_ReturnsSuccess()
{
    // Arrange
    var dto = new FormulaCreateDto
    {
        Name = "麻黄汤",
        Source = "伤寒论",
        Category = "解表剂",
        Type = FormulaType.Decoction,
        Functions = "发汗解表，宣肺平喘",
        CreatedBy = Guid.NewGuid(),
        Items = [
            new FormulaItemCreateDto { HerbId = Guid.NewGuid(), Dosage = 9, Role = HerbRole.Monarch },
            new FormulaItemCreateDto { HerbId = Guid.NewGuid(), Dosage = 6, Role = HerbRole.Minister }
        ]
    };
    
    _formulaRepositoryMock
        .Setup(x => x.GetByNameAndCreatorAsync(dto.Name, dto.CreatedBy))
        .ReturnsAsync((FormulaModel?)null);
    
    // Act
    var result = await _businessService.CreateFormulaAsync(dto);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data.Name, Is.EqualTo(dto.Name));
}

[Test]
public async Task ValidateHerbRoles_NoMonarch_ReturnsFailure()
{
    // Arrange
    var items = new List<FormulaItemCreateDto>
    {
        new() { HerbId = Guid.NewGuid(), Role = HerbRole.Minister },
        new() { HerbId = Guid.NewGuid(), Role = HerbRole.Assistant }
    };
    
    // Act
    var result = _businessService.ValidateHerbRoles(items);
    
    // Assert
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.ErrorMessage, Contains.Substring("必须包含君药"));
}
```

#### 2. 推荐服务测试
```csharp
[Test]
public async Task RecommendFormulasBySymptoms_ValidSymptoms_ReturnsRecommendations()
{
    // Arrange
    var symptoms = new List<string> { "头痛", "恶寒", "发热" };
    var formulas = new List<FormulaModel>
    {
        new() { Name = "麻黄汤", Symptoms = "恶寒发热，头痛身痛", UsageCount = 10 }
    };
    
    _formulaRepositoryMock
        .Setup(x => x.GetActiveFormulasWithSymptomMatchAsync(symptoms))
        .ReturnsAsync(formulas);
    
    // Act
    var result = await _recommendationService.RecommendFormulasBySymptoms(symptoms);
    
    // Assert
    Assert.That(result.Success, Is.True);
    Assert.That(result.Data, Is.Not.Empty);
    Assert.That(result.Data.First().Name, Is.EqualTo("麻黄汤"));
}
```

### 集成测试
```csharp
[Test]
public async Task FormulaWorkflow_CompleteLifecycle_Success()
{
    // 1. 创建验方
    var createDto = new FormulaCreateDto
    {
        Name = "银翘散",
        Category = "解表剂",
        CreatedBy = _testDoctorId,
        Items = [
            new FormulaItemCreateDto { HerbId = _testHerbId1, Dosage = 10, Role = HerbRole.Monarch },
            new FormulaItemCreateDto { HerbId = _testHerbId2, Dosage = 6, Role = HerbRole.Minister }
        ]
    };
    var createResult = await _formulaService.CreateFormulaAsync(createDto);
    Assert.That(createResult.Success, Is.True);
    
    // 2. 发布验方
    var publishResult = await _formulaService.PublishFormulaAsync(createResult.Data.Id);
    Assert.That(publishResult.Success, Is.True);
    
    // 3. 分享验方
    var shareResult = await _formulaService.ShareFormulaAsync(
        createResult.Data.Id, FormulaVisibility.Shared);
    Assert.That(shareResult.Success, Is.True);
    
    // 4. 搜索验证
    var searchResult = await _formulaService.SearchFormulasAsync(
        new FormulaSearchDto { Name = "银翘散" });
    Assert.That(searchResult.Success, Is.True);
    Assert.That(searchResult.Data.Items.First().IsShared, Is.True);
}
```

## 🚀 部署说明

### 数据库迁移
```bash
# Formula模块相关迁移
dotnet ef migrations add AddFormulaModule --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### 配置检查清单
- [ ] FormulaOptions配置正确
- [ ] 数据库连接字符串有效
- [ ] Herbs模块依赖可用
- [ ] AutoMapper映射配置完整
- [ ] 智能推荐服务启用
- [ ] 日志记录级别适当
- [ ] 缓存策略配置合理

## 📚 相关文档

### 架构文档
- [UltraThink双层架构标准](../../architecture/ultrathink-dual-layer-architecture.md)
- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)  
- [API响应标准](../../architecture/ultrathink-api-response-standards-20250817.md)

### 业务文档
- [Prescriptions模块文档](./prescriptions.md) - 验方应用集成
- [Herbs模块文档](./herbs.md) - 药材信息引用
- [中医验方管理指南](../../guides/formula-management-guide.md)

### 开发指南
- [模块开发规范](../../development/MODULE_DEVELOPMENT_STANDARDS.md)
- [智能推荐算法设计](../../development/RECOMMENDATION_ALGORITHM_DESIGN.md)
- [测试指南](../../testing/MODULE_TESTING_GUIDE.md)
- [部署指南](../../deployment/MODULE_DEPLOYMENT_GUIDE.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 后端开发组