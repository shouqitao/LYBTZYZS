using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Models.Formula;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方管理服务实现
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly LYBT.Infrastructure.Data.AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            AppDbContext context,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
        {
            var formula = await _context.Formulas
                .Include(f => f.Herbs)
                .ThenInclude(fh => fh.Herb)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formula == null)
                return null;

            var dto = _mapper.Map<FormulaDetailDto>(formula);

            // 获取创建者信息
            if (formula.CreatedById.HasValue)
            {
                var creator = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == formula.CreatedById.Value);
                dto.CreatedByName = creator?.Name ?? "未知医生";
            }

            return dto;
        }

        public async Task<List<FormulaDto>> GetListAsync()
        {
            var formulas = await _context.Formulas
                .Include(f => f.Herbs)
                .OrderByDescending(f => f.CreateTime)
                .Take(100)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 批量获取创建者信息
            var creatorIds = formulas.Where(f => f.CreatedById.HasValue).Select(f => f.CreatedById!.Value).Distinct().ToList();
            var creators = await _context.Doctors
                .Where(d => creatorIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            foreach (var dto in dtos)
            {
                if (dto.CreatedById.HasValue)
                {
                    dto.CreatedByName = creators.GetValueOrDefault(dto.CreatedById.Value, "未知医生");
                }
            }

            return dtos;
        }

        public async Task<PaginatedResult<FormulaDto>> GetPagedAsync(FormulaQueryDto query)
        {
            var queryable = _context.Formulas.AsQueryable();

            // 条件过滤
            if (!string.IsNullOrWhiteSpace(query.Name))
                queryable = queryable.Where(f => f.Name.Contains(query.Name));

            if (!string.IsNullOrWhiteSpace(query.Effect))
                queryable = queryable.Where(f => f.Effect.Contains(query.Effect));

            if (query.IsShared.HasValue)
                queryable = queryable.Where(f => f.IsShared == query.IsShared.Value);

            if (query.CreatedById.HasValue)
                queryable = queryable.Where(f => f.CreatedById == query.CreatedById.Value);

            if (query.StartDate.HasValue)
                queryable = queryable.Where(f => f.CreateTime >= query.StartDate.Value);

            if (query.EndDate.HasValue)
                queryable = queryable.Where(f => f.CreateTime <= query.EndDate.Value);

            if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
            {
                var keyword = query.SearchKeyword.Trim().ToLower();
                queryable = queryable.Where(f =>
                    f.Name.ToLower().Contains(keyword) ||
                    f.Effect.ToLower().Contains(keyword) ||
                    f.Usage.ToLower().Contains(keyword) ||
                    (f.Instructions != null && f.Instructions.ToLower().Contains(keyword)) ||
                    (f.Indications != null && f.Indications.ToLower().Contains(keyword)));
            }

            // 排序
            queryable = query.OrderBy switch
            {
                "Name" => query.IsAscending ? queryable.OrderBy(f => f.Name) : queryable.OrderByDescending(f => f.Name),
                "CreateTime" => query.IsAscending ? queryable.OrderBy(f => f.CreateTime) : queryable.OrderByDescending(f => f.CreateTime),
                "UpdateTime" => query.IsAscending ? queryable.OrderBy(f => f.UpdateTime) : queryable.OrderByDescending(f => f.UpdateTime),
                _ => queryable.OrderByDescending(f => f.CreateTime)
            };

            var totalCount = await queryable.CountAsync();
            var formulas = await queryable
                .Include(f => f.Herbs)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 批量获取创建者信息
            var creatorIds = formulas.Where(f => f.CreatedById.HasValue).Select(f => f.CreatedById!.Value).Distinct().ToList();
            var creators = await _context.Doctors
                .Where(d => creatorIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            foreach (var dto in dtos)
            {
                if (dto.CreatedById.HasValue)
                {
                    dto.CreatedByName = creators.GetValueOrDefault(dto.CreatedById.Value, "未知医生");
                }
            }

            return new PaginatedResult<FormulaDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
            };
        }

        public async Task<FormulaDetailDto?> CreateAsync(FormulaCreateDto dto, Guid operatorId, string operatorName)
        {
            // 验证药材是否存在
            var herbIds = dto.Herbs.Select(h => h.HerbId).Distinct().ToList();
            var existingHerbs = await _context.Herbs
                .Where(h => herbIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, h => h);

            if (existingHerbs.Count != herbIds.Count)
            {
                var missingHerbs = herbIds.Where(id => !existingHerbs.ContainsKey(id)).ToList();
                throw new ArgumentException($"以下药材不存在: {string.Join(", ", missingHerbs)}");
            }

            var formula = _mapper.Map<FormulaModel>(dto);
            formula.Id = Guid.NewGuid();
            formula.CreatedById = operatorId;
            formula.CreateTime = DateTime.Now;
            formula.CreatedBy = operatorName;

            // 创建药材组成项
            var herbItems = new List<FormulaHerbItem>();
            foreach (var herbDto in dto.Herbs)
            {
                var herb = existingHerbs[herbDto.HerbId];
                herbItems.Add(new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    FormulaId = formula.Id,
                    HerbId = herbDto.HerbId,
                    Quantity = herbDto.Quantity,
                    Unit = herb.Unit,
                    Preparation = herbDto.Preparation,
                    Usage = herbDto.Usage,
                    Price = herb.Price,
                    SortOrder = herbDto.SortOrder
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Formulas.Add(formula);
                await _context.SaveChangesAsync();

                _context.FormulaHerbItems.AddRange(herbItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("创建验方成功 - 验方ID: {FormulaId}, 验方名称: {Name}, 操作员: {Operator}",
                    formula.Id, formula.Name, operatorName);

                return await GetByIdAsync(formula.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FormulaDetailDto?> UpdateAsync(Guid id, FormulaUpdateDto dto, Guid operatorId, string operatorName)
        {
            var formula = await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formula == null)
                return null;

            // 权限检查：只有创建者或管理员可以修改
            if (formula.CreatedById != operatorId)
            {
                // 这里可以添加管理员权限检查
                throw new UnauthorizedAccessException("只有验方创建者可以修改验方");
            }

            // 验证新的药材是否存在
            var herbIds = dto.Herbs.Select(h => h.HerbId).Distinct().ToList();
            var existingHerbs = await _context.Herbs
                .Where(h => herbIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, h => h);

            if (existingHerbs.Count != herbIds.Count)
            {
                var missingHerbs = herbIds.Where(id => !existingHerbs.ContainsKey(id)).ToList();
                throw new ArgumentException($"以下药材不存在: {string.Join(", ", missingHerbs)}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 更新基本信息
                formula.Name = dto.Name;
                formula.Effect = dto.Effect;
                formula.Usage = dto.Usage;
                formula.IsShared = dto.IsShared;
                formula.Instructions = dto.Instructions;
                formula.Indications = dto.Indications;
                formula.Contraindications = dto.Contraindications;
                formula.Preparation = dto.Preparation;
                formula.Remark = dto.Remark;
                formula.UpdateTime = DateTime.Now;
                formula.UpdatedBy = operatorName;

                // 删除现有的药材组成项
                _context.FormulaHerbItems.RemoveRange(formula.Herbs);

                // 添加新的药材组成项
                var newHerbItems = new List<FormulaHerbItem>();
                foreach (var herbDto in dto.Herbs)
                {
                    var herb = existingHerbs[herbDto.HerbId];
                    newHerbItems.Add(new FormulaHerbItem
                    {
                        Id = herbDto.Id ?? Guid.NewGuid(),
                        FormulaId = formula.Id,
                        HerbId = herbDto.HerbId,
                        Quantity = herbDto.Quantity,
                        Unit = herb.Unit,
                        Preparation = herbDto.Preparation,
                        Usage = herbDto.Usage,
                        Price = herb.Price,
                        SortOrder = herbDto.SortOrder
                    });
                }

                _context.FormulaHerbItems.AddRange(newHerbItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("更新验方成功 - 验方ID: {FormulaId}, 验方名称: {Name}, 操作员: {Operator}",
                    formula.Id, formula.Name, operatorName);

                return await GetByIdAsync(formula.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var formula = await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (formula == null)
                return false;

            // 权限检查：只有创建者或管理员可以删除
            if (formula.CreatedById != operatorId)
            {
                throw new UnauthorizedAccessException("只有验方创建者可以删除验方");
            }

            // 检查是否被处方引用
            var isUsedInPrescriptions = await _context.PrescriptionFormulas
                .AnyAsync(pf => pf.FormulaId == id);

            if (isUsedInPrescriptions)
            {
                throw new InvalidOperationException("该验方已被处方引用，无法删除");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 删除药材组成项
                _context.FormulaHerbItems.RemoveRange(formula.Herbs);

                // 删除验方
                _context.Formulas.Remove(formula);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("删除验方成功 - 验方ID: {FormulaId}, 验方名称: {Name}, 操作员: {Operator}",
                    formula.Id, formula.Name, operatorName);

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<FormulaDto>> GetByCreatorIdAsync(Guid creatorId)
        {
            var formulas = await _context.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.CreatedById == creatorId)
                .OrderByDescending(f => f.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 获取创建者名称
            var creator = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == creatorId);
            var creatorName = creator?.Name ?? "未知医生";

            foreach (var dto in dtos)
            {
                dto.CreatedByName = creatorName;
            }

            return dtos;
        }

        public async Task<List<FormulaDto>> GetSharedFormulasAsync()
        {
            var formulas = await _context.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.IsShared)
                .OrderByDescending(f => f.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 批量获取创建者信息
            var creatorIds = formulas.Where(f => f.CreatedById.HasValue).Select(f => f.CreatedById!.Value).Distinct().ToList();
            var creators = await _context.Doctors
                .Where(d => creatorIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            foreach (var dto in dtos)
            {
                if (dto.CreatedById.HasValue)
                {
                    dto.CreatedByName = creators.GetValueOrDefault(dto.CreatedById.Value, "未知医生");
                }
            }

            return dtos;
        }

        public async Task<List<FormulaDto>> GetPersonalFormulasAsync(Guid doctorId)
        {
            var formulas = await _context.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.CreatedById == doctorId && !f.IsShared)
                .OrderByDescending(f => f.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 获取医生名称
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId);
            var doctorName = doctor?.Name ?? "未知医生";

            foreach (var dto in dtos)
            {
                dto.CreatedByName = doctorName;
            }

            return dtos;
        }

        public async Task<List<FormulaDto>> SearchFormulasAsync(string keyword, int maxResults = 50)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<FormulaDto>();

            var searchTerm = keyword.Trim().ToLower();

            var formulas = await _context.Formulas
                .Include(f => f.Herbs)
                .Where(f => f.Name.ToLower().Contains(searchTerm) ||
                           f.Effect.ToLower().Contains(searchTerm) ||
                           f.Usage.ToLower().Contains(searchTerm) ||
                           (f.Instructions != null && f.Instructions.ToLower().Contains(searchTerm)) ||
                           (f.Indications != null && f.Indications.ToLower().Contains(searchTerm)))
                .OrderByDescending(f => f.CreateTime)
                .Take(maxResults)
                .ToListAsync();

            var dtos = _mapper.Map<List<FormulaDto>>(formulas);

            // 批量获取创建者信息
            var creatorIds = formulas.Where(f => f.CreatedById.HasValue).Select(f => f.CreatedById!.Value).Distinct().ToList();
            var creators = await _context.Doctors
                .Where(d => creatorIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name);

            foreach (var dto in dtos)
            {
                if (dto.CreatedById.HasValue)
                {
                    dto.CreatedByName = creators.GetValueOrDefault(dto.CreatedById.Value, "未知医生");
                }
            }

            return dtos;
        }

        public async Task<FormulaStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? doctorId = null)
        {
            var query = _context.Formulas.AsQueryable();

            if (doctorId.HasValue)
                query = query.Where(f => f.CreatedById == doctorId.Value);

            var formulas = await query
                .Where(f => f.CreateTime >= startDate && f.CreateTime <= endDate)
                .ToListAsync();

            var statistics = new FormulaStatisticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalCount = formulas.Count,
                SharedCount = formulas.Count(f => f.IsShared),
                PrivateCount = formulas.Count(f => !f.IsShared)
            };

            // 功效统计
            var effectStats = formulas
                .Where(f => !string.IsNullOrWhiteSpace(f.Effect))
                .GroupBy(f => f.Effect)
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.EffectStats = effectStats;

            // 创建者统计
            if (!doctorId.HasValue)
            {
                var creatorIds = formulas.Where(f => f.CreatedById.HasValue).Select(f => f.CreatedById!.Value).Distinct().ToList();
                var creators = await _context.Doctors
                    .Where(d => creatorIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id, d => d.Name);

                var creatorStats = formulas
                    .Where(f => f.CreatedById.HasValue)
                    .GroupBy(f => f.CreatedById!.Value)
                    .ToDictionary(g => creators.GetValueOrDefault(g.Key, "未知医生"), g => g.Count());

                statistics.CreatorStats = creatorStats;
            }

            return statistics;
        }

        // ==================== 验方高级功能 ====================

        public async Task<FormulaDetailDto?> CreateFromPrescriptionAsync(CreateFormulaFromPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            // 获取处方信息
            var prescription = await _context.Prescriptions
                .Include(p => p.Items)
                .ThenInclude(pi => pi.Herb)
                .FirstOrDefaultAsync(p => p.Id == dto.PrescriptionId);

            if (prescription == null)
                throw new ArgumentException("处方不存在");

            // 创建验方
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Effect = dto.Effect,
                Usage = dto.Usage,
                IsShared = dto.IsShared,
                CreatedById = operatorId,
                CreateTime = DateTime.Now,
                CreatedBy = operatorName,
                Remark = dto.Remark
            };

            // 创建药材组成项
            var herbItems = new List<FormulaHerbItem>();
            var sortOrder = 1;
            foreach (var item in prescription.Items)
            {
                herbItems.Add(new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    FormulaId = formula.Id,
                    HerbId = item.HerbId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Preparation = item.Preparation,
                    Usage = item.Usage,
                    Price = item.Price,
                    SortOrder = sortOrder++
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Formulas.Add(formula);
                await _context.SaveChangesAsync();

                _context.FormulaHerbItems.AddRange(herbItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("从处方创建验方成功 - 处方ID: {PrescriptionId}, 验方ID: {FormulaId}, 验方名称: {Name}, 操作员: {Operator}",
                    dto.PrescriptionId, formula.Id, formula.Name, operatorName);

                return await GetByIdAsync(formula.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FormulaDetailDto?> CopyFormulaAsync(Guid sourceFormulaId, string newName, Guid operatorId, string operatorName)
        {
            var sourceFormula = await _context.Formulas
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == sourceFormulaId);

            if (sourceFormula == null)
                return null;

            // 检查权限：只能复制共享验方或自己的验方
            if (!sourceFormula.IsShared && sourceFormula.CreatedById != operatorId)
            {
                throw new UnauthorizedAccessException("只能复制共享验方或自己创建的验方");
            }

            var newFormula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = newName,
                Effect = sourceFormula.Effect,
                Usage = sourceFormula.Usage,
                Instructions = sourceFormula.Instructions,
                Indications = sourceFormula.Indications,
                Contraindications = sourceFormula.Contraindications,
                Preparation = sourceFormula.Preparation,
                IsShared = false, // 复制的验方默认为私有
                CreatedById = operatorId,
                CreateTime = DateTime.Now,
                CreatedBy = operatorName,
                Remark = $"复制自验方: {sourceFormula.Name}"
            };

            // 复制药材组成项
            var newHerbItems = new List<FormulaHerbItem>();
            foreach (var herbItem in sourceFormula.Herbs)
            {
                newHerbItems.Add(new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    FormulaId = newFormula.Id,
                    HerbId = herbItem.HerbId,
                    Quantity = herbItem.Quantity,
                    Unit = herbItem.Unit,
                    Preparation = herbItem.Preparation,
                    Usage = herbItem.Usage,
                    Price = herbItem.Price,
                    SortOrder = herbItem.SortOrder
                });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Formulas.Add(newFormula);
                await _context.SaveChangesAsync();

                _context.FormulaHerbItems.AddRange(newHerbItems);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("复制验方成功 - 源验方ID: {SourceId}, 新验方ID: {NewId}, 新验方名称: {Name}, 操作员: {Operator}",
                    sourceFormulaId, newFormula.Id, newFormula.Name, operatorName);

                return await GetByIdAsync(newFormula.Id);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ShareFormulaAsync(Guid formulaId, Guid operatorId, string operatorName)
        {
            var formula = await _context.Formulas.FirstOrDefaultAsync(f => f.Id == formulaId);
            if (formula == null)
                return false;

            // 权限检查
            if (formula.CreatedById != operatorId)
            {
                throw new UnauthorizedAccessException("只有验方创建者可以分享验方");
            }

            formula.IsShared = true;
            formula.UpdateTime = DateTime.Now;
            formula.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("分享验方成功 - 验方ID: {FormulaId}, 操作员: {Operator}", formulaId, operatorName);

            return true;
        }

        public async Task<bool> UnshareFormulaAsync(Guid formulaId, Guid operatorId, string operatorName)
        {
            var formula = await _context.Formulas.FirstOrDefaultAsync(f => f.Id == formulaId);
            if (formula == null)
                return false;

            // 权限检查
            if (formula.CreatedById != operatorId)
            {
                throw new UnauthorizedAccessException("只有验方创建者可以取消分享验方");
            }

            formula.IsShared = false;
            formula.UpdateTime = DateTime.Now;
            formula.UpdatedBy = operatorName;

            await _context.SaveChangesAsync();

            _logger.LogInformation("取消分享验方成功 - 验方ID: {FormulaId}, 操作员: {Operator}", formulaId, operatorName);

            return true;
        }

        public async Task<List<FormulaRecommendationDto>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid? doctorId = null)
        {
            // 这里实现智能推荐逻辑
            // 可以基于症状、诊断、历史使用记录等进行推荐
            var recommendations = new List<FormulaRecommendationDto>();

            var query = _context.Formulas.AsQueryable();

            // 如果指定医生，优先推荐该医生的验方
            if (doctorId.HasValue)
            {
                query = query.Where(f => f.IsShared || f.CreatedById == doctorId.Value);
            }
            else
            {
                query = query.Where(f => f.IsShared);
            }

            var formulas = await query.ToListAsync();

            // 简单的关键词匹配推荐（实际项目中可以使用更复杂的算法）
            var keywords = new List<string>();
            if (!string.IsNullOrWhiteSpace(symptoms))
                keywords.AddRange(symptoms.Split(' ', '，', ',').Where(k => !string.IsNullOrWhiteSpace(k)));
            if (!string.IsNullOrWhiteSpace(diagnosis))
                keywords.AddRange(diagnosis.Split(' ', '，', ',').Where(k => !string.IsNullOrWhiteSpace(k)));

            foreach (var formula in formulas)
            {
                var matchScore = 0.0;
                var matchReasons = new List<string>();

                foreach (var keyword in keywords)
                {
                    if (formula.Effect.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 3.0;
                        matchReasons.Add($"功效匹配: {keyword}");
                    }
                    if (!string.IsNullOrEmpty(formula.Indications) && formula.Indications.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 2.0;
                        matchReasons.Add($"主治匹配: {keyword}");
                    }
                    if (formula.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 1.0;
                        matchReasons.Add($"名称匹配: {keyword}");
                    }
                }

                if (matchScore > 0)
                {
                    recommendations.Add(new FormulaRecommendationDto
                    {
                        FormulaId = formula.Id,
                        FormulaName = formula.Name,
                        Effect = formula.Effect,
                        MatchScore = matchScore,
                        UsageCount = 0, // 这里可以查询使用次数
                        MatchReason = string.Join(", ", matchReasons)
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.MatchScore).Take(10).ToList();
        }

        public async Task<List<FormulaDto>> GetFrequentlyUsedFormulasAsync(Guid? doctorId = null, int limit = 20)
        {
            // 基于处方使用频率推荐常用验方
            var query = from pf in _context.PrescriptionFormulas
                        join f in _context.Formulas on pf.FormulaId equals f.Id
                        group pf by f into g
                        orderby g.Count() descending
                        select g.Key;

            if (doctorId.HasValue)
            {
                query = from pf in _context.PrescriptionFormulas
                        join p in _context.Prescriptions on pf.PrescriptionId equals p.Id
                        join f in _context.Formulas on pf.FormulaId equals f.Id
                        where p.DoctorId == doctorId.Value
                        group pf by f into g
                        orderby g.Count() descending
                        select g.Key;
            }

            var formulas = await query.Take(limit).ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<FormulaValidationResult> ValidateFormulaAsync(Guid formulaId)
        {
            var result = new FormulaValidationResult { IsValid = true };

            var formula = await _context.Formulas
                .Include(f => f.Herbs)
                .ThenInclude(fh => fh.Herb)
                .FirstOrDefaultAsync(f => f.Id == formulaId);

            if (formula == null)
            {
                result.IsValid = false;
                result.Errors.Add("验方不存在");
                return result;
            }

            // 基本验证
            if (!formula.Herbs.Any())
            {
                result.IsValid = false;
                result.Errors.Add("验方必须包含至少一味药材");
            }

            // 剂量验证
            foreach (var herbItem in formula.Herbs)
            {
                if (herbItem.Quantity <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add($"药材 {herbItem.Herb?.Name ?? "未知"} 的剂量必须大于0");
                }

                if (herbItem.Quantity > 100) // 假设单味药材超过100g需要提醒
                {
                    result.Warnings.Add($"药材 {herbItem.Herb?.Name ?? "未知"} 剂量较大({herbItem.Quantity}{herbItem.Unit})，请确认用量");
                }
            }

            // 这里可以添加更多的验证逻辑
            // 比如：十八反十九畏的配伍禁忌检查
            // 孕妇禁用药检查等

            return result;
        }

        public async Task<List<FormulaUsageRecordDto>> GetUsageRecordsAsync(Guid formulaId)
        {
            var records = await (from pf in _context.PrescriptionFormulas
                                 join p in _context.Prescriptions on pf.PrescriptionId equals p.Id
                                 join mc in _context.MedicalCases on p.MedicalCaseId equals mc.Id
                                 join patient in _context.Patients on mc.PatientId equals patient.Id
                                 join doctor in _context.Doctors on p.DoctorId equals doctor.Id
                                 where pf.FormulaId == formulaId
                                 select new FormulaUsageRecordDto
                                 {
                                     Id = pf.Id,
                                     FormulaId = formulaId,
                                     PrescriptionId = p.Id,
                                     PatientId = patient.Id,
                                     PatientName = patient.Name,
                                     DoctorId = doctor.Id,
                                     DoctorName = doctor.Name,
                                     UsageDate = p.CreateTime,
                                     Modifications = pf.Modifications,
                                     Feedback = pf.Feedback
                                 }).OrderByDescending(r => r.UsageDate).ToListAsync();

            return records;
        }
    }
}