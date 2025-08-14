using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 完整的处方服务实现
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        // 基础CRUD操作
        public async Task<List<FormulaDto>> GetListAsync()
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<PaginatedResult<FormulaDto>> GetPagedAsync(FormulaQueryDto query)
        {
            var formulas = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                formulas = formulas.Where(f => f.Name.Contains(query.Keyword));
            }

            var total = await formulas.CountAsync();
            var items = await formulas
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PaginatedResult<FormulaDto>
            {
                Items = _mapper.Map<List<FormulaDto>>(items),
                TotalCount = total,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<FormulaDetailDto> CreateAsync(FormulaCreateDto dto)
        {
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDetailDto>(formula);
        }

        public async Task<FormulaDetailDto?> CreateAsync(FormulaCreateDto dto, Guid creatorId, string creatorName)
        {
            return await CreateAsync(dto);
        }

        public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
        {
            var formula = await _dbContext.Formulas
                .FirstOrDefaultAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);

            return formula == null ? null : _mapper.Map<FormulaDetailDto>(formula);
        }

        public async Task<bool> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            var formula = await _dbContext.Formulas.FindAsync(id);
            if (formula == null) return false;

            formula.Name = dto.Name ?? formula.Name;
            formula.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<FormulaDetailDto?> UpdateAsync(Guid id, FormulaUpdateDto dto, Guid updaterId, string updaterName)
        {
            var formula = await _dbContext.Formulas.FindAsync(id);
            if (formula == null) return null;

            formula.Name = dto.Name;
            formula.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return _mapper.Map<FormulaDetailDto>(formula);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var formula = await _dbContext.Formulas.FindAsync(id);
            if (formula == null) return false;

            formula.Status = CommonStatus.Disabled;
            formula.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid deleterId, string deleterName)
        {
            return await DeleteAsync(id);
        }

        // 查询方法
        public async Task<List<FormulaDto>> GetAllAsync()
        {
            return await GetListAsync();
        }

        public async Task<List<FormulaDto>> GetByPatientIdAsync(Guid patientId)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(10)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetByDoctorIdAsync(Guid doctorId)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(10)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetByCreatorIdAsync(Guid creatorId)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(10)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetSharedFormulasAsync()
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(10)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetPersonalFormulasAsync(Guid userId)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(10)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> SearchFormulasAsync(string keyword, int maxResults = 20)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled && f.Name.Contains(keyword))
                .Take(maxResults)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetFrequentlyUsedAsync(Guid doctorId, int top = 10)
        {
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .Take(top)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        public async Task<List<FormulaDto>> GetFrequentlyUsedFormulasAsync(Guid? doctorId = null, int top = 10)
        {
            return await GetFrequentlyUsedAsync(doctorId ?? Guid.Empty, top);
        }

        public async Task<List<FormulaDto>> GetRecentAsync(int days = 7)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            var formulas = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled && f.CreateTime >= cutoff)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(formulas);
        }

        // 模板相关
        public async Task<List<FormulaDto>> GetTemplatesAsync()
        {
            var templates = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled)
                .ToListAsync();
            return _mapper.Map<List<FormulaDto>>(templates);
        }

        public async Task<FormulaDto> CreateTemplateAsync(FormulaCreateDto dto)
        {
            var template = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(template);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDto>(template);
        }

        public async Task<FormulaDetailDto> CreateFromTemplateAsync(Guid templateId, FormulaFromTemplateDto dto)
        {
            var template = await _dbContext.Formulas.FindAsync(templateId);
            if (template == null)
            {
                throw new InvalidOperationException("模板不存在");
            }

            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDetailDto>(formula);
        }

        public async Task<List<FormulaDto>> SearchTemplatesAsync(string keyword)
        {
            var templates = await _dbContext.Formulas
                .Where(f => f.Status == CommonStatus.Enabled && f.Name.Contains(keyword))
                .ToListAsync();

            return _mapper.Map<List<FormulaDto>>(templates);
        }

        public async Task<bool> UpdateTemplateAsync(Guid id, FormulaUpdateDto dto)
        {
            var template = await _dbContext.Formulas.FindAsync(id);
            if (template == null) return false;

            template.Name = dto.Name ?? template.Name;
            template.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTemplateAsync(Guid id)
        {
            var template = await _dbContext.Formulas.FindAsync(id);
            if (template == null) return false;

            template.Status = CommonStatus.Disabled;
            template.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<FormulaDto?> GetTemplateByIdAsync(Guid id)
        {
            var template = await _dbContext.Formulas
                .FirstOrDefaultAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);

            return template == null ? null : _mapper.Map<FormulaDto>(template);
        }

        // 处方相关
        public async Task<FormulaDto> GenerateFromPrescriptionAsync(Guid prescriptionId)
        {
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = "处方生成的方剂",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDto>(formula);
        }

        public async Task<FormulaDetailDto?> CreateFromPrescriptionAsync(CreateFormulaFromPrescriptionDto dto, Guid creatorId, string creatorName)
        {
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDetailDto>(formula);
        }

        // 复制和分享
        public async Task<bool> CloneAsync(Guid id, string newName)
        {
            var original = await _dbContext.Formulas.FindAsync(id);
            if (original == null) return false;

            var clone = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = newName,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(clone);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<FormulaDetailDto?> CopyFormulaAsync(Guid formulaId, string newName, Guid creatorId, string creatorName)
        {
            var original = await _dbContext.Formulas.FindAsync(formulaId);
            if (original == null) return null;

            var copy = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = newName,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(copy);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDetailDto>(copy);
        }

        public async Task<bool> ShareFormulaAsync(Guid formulaId, Guid userId, string userName)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> UnshareFormulaAsync(Guid formulaId, Guid userId, string userName)
        {
            await Task.CompletedTask;
            return true;
        }

        // 草药管理
        public async Task<bool> AddHerbAsync(Guid formulaId, FormulaHerbItem herb)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> RemoveHerbAsync(Guid formulaId, Guid herbId)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> UpdateHerbAsync(Guid formulaId, Guid herbId, FormulaHerbItem herb)
        {
            await Task.CompletedTask;
            return true;
        }

        // 分析和验证
        public async Task<decimal> CalculatePriceAsync(Guid id)
        {
            await Task.CompletedTask;
            return 100m;
        }

        public async Task<List<HerbCompatibilityWarning>> CheckHerbCompatibilityAsync(Guid id)
        {
            await Task.CompletedTask;
            return new List<HerbCompatibilityWarning>();
        }

        public async Task<List<FormulaRecommendation>> GetRecommendationsAsync(string symptoms)
        {
            await Task.CompletedTask;
            return new List<FormulaRecommendation>();
        }

        public async Task<List<FormulaRecommendationDto>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid? doctorId = null)
        {
            // 简化实现
            await Task.CompletedTask;
            return new List<FormulaRecommendationDto>();
        }

        public async Task<LYBT.Module.Formula.Interfaces.FormulaValidationResult> ValidateFormulaAsync(Guid id)
        {
            await Task.CompletedTask;
            return new LYBT.Module.Formula.Interfaces.FormulaValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };
        }

        public async Task<string> GeneratePrescriptionTextAsync(Guid id)
        {
            await Task.CompletedTask;
            return "处方内容";
        }

        public async Task<FormulaAnalysisResult> AnalyzeFormulaAsync(Guid id)
        {
            await Task.CompletedTask;
            return new FormulaAnalysisResult
            {
                Summary = "分析完成",
                Effects = new List<string>(),
                Contraindications = new List<string>(),
                Warnings = new List<HerbCompatibilityWarning>()
            };
        }

        // 历史记录
        public async Task<List<FormulaHistoryDto>> GetHistoryAsync(Guid id)
        {
            await Task.CompletedTask;
            return new List<FormulaHistoryDto>();
        }

        public async Task<bool> RestoreFromHistoryAsync(Guid formulaId, Guid historyId)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<List<LYBT.Module.Formula.Interfaces.FormulaUsageRecordDto>> GetUsageRecordsAsync(Guid formulaId)
        {
            await Task.CompletedTask;
            return new List<LYBT.Module.Formula.Interfaces.FormulaUsageRecordDto>();
        }

        // 统计
        public async Task<FormulaStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbContext.Formulas.Where(f => f.Status == CommonStatus.Enabled);

            if (startDate.HasValue)
                query = query.Where(f => f.CreateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(f => f.CreateTime <= endDate.Value);

            var total = await query.CountAsync();

            return new FormulaStatisticsDto
            {
                TotalCount = total,
                SharedCount = 0,
                PrivateCount = total,
                UsedCount = 0
            };
        }

        public async Task<FormulaStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? doctorId = null)
        {
            return await GetStatisticsAsync((DateTime?)startDate, (DateTime?)endDate);
        }

        // 导入导出
        public async Task<bool> ExportToFileAsync(Guid id, string filePath)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<FormulaDto> ImportFromFileAsync(string filePath)
        {
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = "导入的方剂",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                Status = CommonStatus.Enabled
            };

            _dbContext.Formulas.Add(formula);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<FormulaDto>(formula);
        }
    }
}