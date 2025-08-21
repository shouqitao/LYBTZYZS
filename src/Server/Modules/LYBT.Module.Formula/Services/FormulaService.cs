using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Formula.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务主类 - 重构后的简化版本
    /// 负责核心CRUD操作，复杂逻辑委托给Helper类处理
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;
        
        // Helper类依赖注入
        private readonly FormulaValidationHelper _validationHelper;
        private readonly FormulaCalculationHelper _calculationHelper;
        private readonly FormulaQueryHelper _queryHelper;

        public FormulaService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<FormulaService> logger,
            FormulaValidationHelper validationHelper,
            FormulaCalculationHelper calculationHelper,
            FormulaQueryHelper queryHelper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _validationHelper = validationHelper;
            _calculationHelper = calculationHelper;
            _queryHelper = queryHelper;
        }

        #region 核心CRUD操作

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas
                    .FirstOrDefaultAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);

                if (formula == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("获取验方详情失败", ex);
            }
        }

        /// <summary>
        /// 创建验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                // 使用验证Helper进行数据验证
                var validationResult = await _validationHelper.ValidateCreateAsync(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage!);

                var formula = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Effect = dto.Effect,
                    Usage = dto.Usage,
                    // Property不在CreateDto中，使用默认值
                    IsShared = dto.IsShared,
                    Remark = dto.Remark,
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(formula);
                await _dbContext.SaveChangesAsync();

                var createdDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return ServiceResult<FormulaDto>.Failure("创建验方失败", ex);
            }
        }

        /// <summary>
        /// 更新验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                // 使用验证Helper进行数据验证
                var validationResult = await _validationHelper.ValidateUpdateAsync(id, dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage!);

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                // 更新字段
                formula.Name = dto.Name ?? formula.Name;
                formula.Effect = dto.Effect ?? formula.Effect;
                formula.Usage = dto.Usage ?? formula.Usage;
                // Property不在UpdateDto中，保持原值
                formula.Remark = dto.Remark ?? formula.Remark;
                formula.IsShared = dto.IsShared;

                await _dbContext.SaveChangesAsync();

                var updatedDto = _mapper.Map<FormulaDto>(formula);
                return ServiceResult<FormulaDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("更新验方失败", ex);
            }
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.Status = CommonStatus.Disabled;
                await _dbContext.SaveChangesAsync();
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除验方失败", ex);
            }
        }

        #endregion

        #region 委托给Helper类的复杂操作

        /// <summary>
        /// 分页查询验方 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        {
            return await _queryHelper.GetPagedAsync(query);
        }

        /// <summary>
        /// 搜索验方 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            return await _queryHelper.SearchFormulasAsync(query);
        }

        /// <summary>
        /// 获取验方列表 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
        {
            return await _queryHelper.GetFormulasAsync(keyword, category);
        }

        /// <summary>
        /// 获取所有验方 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            return await _queryHelper.GetAllFormulasAsync();
        }

        /// <summary>
        /// 获取验方模板 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        {
            return await _queryHelper.GetTemplatesAsync();
        }

        /// <summary>
        /// 根据类型获取验方 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        {
            return await _queryHelper.GetByTypeAsync(formulaType);
        }

        /// <summary>
        /// 获取分类列表 - 委托给QueryHelper
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            return await _queryHelper.GetCategoriesAsync();
        }

        /*
        /// <summary>
        /// 分析验方 (已废弃)
        /// </summary>
        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            // 验方分析功能已删除 - 小诊所医生凭经验判断
        }
        */

        #region 已废弃功能 - UltraThink精简
        /*
        /// <summary>
        /// 获取推荐验方 (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
        {
            // 智能推荐功能已删除 - 小诊所医生凭经验选方
        }

        /// <summary>
        /// 获取推荐验方（三参数重载） (已废弃)
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(
            string symptoms, string diagnosis, Guid doctorId)
        {
            // 智能推荐功能已删除 - 小诊所医生凭经验选方
        }
        */

        /// <summary>
        /// 从处方创建验方 - 委托给CalculationHelper
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
        {
            try
            {
                var result = await _calculationHelper.CreateFromPrescriptionAsync(prescriptionId, name);
                if (!result.IsSuccess)
                    return ServiceResult<FormulaDto>.Failure(result.ErrorMessage!);

                var dto = _mapper.Map<FormulaDto>(result.Data!);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方创建验方失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<FormulaDto>.Failure("从处方创建验方失败", ex);
            }
        }

        /*
        /// <summary>
        /// 导入验方 - 委托给CalculationHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(
            List<FormulaImportDto> formulas, 
            FormulaImportOptionsDto options)
        {
            return await _calculationHelper.ExecuteImportAsync(formulas, options);
        }

        /// <summary>
        /// 验证导入数据 - 委托给ValidationHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ValidateImportDataAsync(
            List<FormulaImportDto> formulas,
            FormulaImportOptionsDto options)
        {
            return await _validationHelper.ValidateImportDataAsync(formulas, options);
        }
        */

        /*
        /// <summary>
        /// 导出验方 - 委托给QueryHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportFormulasAsync(List<Guid> formulaIds)
        {
            return await _queryHelper.ExportFormulasAsync(formulaIds);
        }

        /// <summary>
        /// 导出所有验方 - 委托给QueryHelper (基础数据功能保留)
        /// </summary>
        public async Task<ServiceResult<List<FormulaExportDto>>> ExportAllFormulasAsync(
            bool includePrivate = false, 
            string? category = null)
        {
            return await _queryHelper.ExportAllFormulasAsync(includePrivate, category);
        }
        */

        /*
        /// <summary>
        /// 获取导入历史 (已废弃)
        /// </summary>
        public async Task<ServiceResult<PagedResult<FormulaImportResultDto>>> GetImportHistoryAsync(
            int pageIndex = 1,
            int pageSize = 20,
            string? importBatch = null)
        {
            // 导入历史功能已删除 - 小诊所不需要复杂导入追踪
        }
        */

        /// <summary>
        /// 复制验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                var validationResult = await _validationHelper.ValidateCopyAsync(id, newName);
                if (!validationResult.IsSuccess)
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage!);

                var original = await _dbContext.Formulas.FindAsync(id);
                if (original == null)
                    return ServiceResult<FormulaDto>.Failure("原验方不存在");

                var copy = new LYBT.Entities.Formula.Formula
                {
                    Id = Guid.NewGuid(),
                    Name = newName,
                    Effect = original.Effect,
                    Usage = original.Usage,
                    Property = original.Property,
                    IsShared = false,
                    Remark = $"复制自: {original.Name}",
                    Status = CommonStatus.Enabled
                };

                _dbContext.Formulas.Add(copy);
                await _dbContext.SaveChangesAsync();

                var dto = _mapper.Map<FormulaDto>(copy);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制验方失败: {Id}", id);
                return ServiceResult<FormulaDto>.Failure("复制验方失败", ex);
            }
        }

        /// <summary>
        /// 切换验方状态
        /// </summary>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.Status = formula.Status == CommonStatus.Enabled 
                    ? CommonStatus.Disabled 
                    : CommonStatus.Enabled;

                await _dbContext.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换验方状态失败: {Id}", id);
                return ServiceResult<bool>.Failure("切换验方状态失败", ex);
            }
        }

        /// <summary>
        /// 分享验方
        /// </summary>
        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var validationResult = await _validationHelper.ValidateSharePermissionAsync(id, operatorId);
                if (!validationResult.IsSuccess)
                    return ServiceResult<bool>.Failure(validationResult.ErrorMessage!);

                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.IsShared = true;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("验方分享成功: {FormulaId} by {OperatorName}({OperatorId})", 
                    id, operatorName, operatorId);
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分享验方失败: {Id}, {OperatorId}, {OperatorName}", id, operatorId, operatorName);
                return ServiceResult<bool>.Failure("分享验方失败", ex);
            }
        }

        /// <summary>
        /// 取消分享验方
        /// </summary>
        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                var formula = await _dbContext.Formulas.FindAsync(id);
                if (formula == null)
                    return ServiceResult<bool>.Failure("验方不存在");

                formula.IsShared = false;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("取消验方分享成功: {FormulaId} by {OperatorName}({OperatorId})", 
                    id, operatorName, operatorId);
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消分享验方失败: {Id}, {OperatorId}, {OperatorName}", id, operatorId, operatorName);
                return ServiceResult<bool>.Failure("取消分享验方失败", ex);
            }
        }

        /// <summary>
        /// 获取验方列表（兼容旧方法）
        /// </summary>
        public async Task<List<FormulaDto>> GetListAsync()
        {
            var result = await GetAllFormulasAsync();
            return result.IsSuccess ? result.Data! : new List<FormulaDto>();
        }

        /*
        /// <summary>
        /// 导出到Excel文件 (已废弃)
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToExcelAsync(List<Guid> formulaIds)
        {
            // Excel导出功能已删除 - 小诊所不需要复杂的Excel处理
        }
        */

        /*
        /// <summary>
        /// 从Excel导入验方 (已废弃)
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(
            byte[] fileData,
            string fileName,
            FormulaImportOptionsDto options)
        {
            // Excel导入功能已删除 - 小诊所手动录入验方
        }
        */

        /// <summary>
        /// 分析验方 (已废弃)
        /// UltraThink v2.0: 验方分析功能已删除 - 小诊所医生凭经验判断
        /// </summary>
        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            try
            {
                await Task.CompletedTask;
                var emptyResult = new FormulaAnalysisResult(); // 需要定义这个类或使用object
                return ServiceResult<FormulaAnalysisResult>.Success(emptyResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验方分析失败: {FormulaId}", formulaId);
                return ServiceResult<FormulaAnalysisResult>.Failure($"验方分析失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取推荐验方 (已废弃)
        /// UltraThink v2.0: 智能推荐功能已删除 - 小诊所医生凭经验选方
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方失败: {Syndrome}", syndrome);
                return ServiceResult<List<FormulaRecommendationDto>>.Failure($"获取推荐验方失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取推荐验方（三参数重载） (已废弃)
        /// UltraThink v2.0: 智能推荐功能已删除 - 小诊所医生凭经验选方
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方失败: {Symptoms}, {Diagnosis}, {DoctorId}", symptoms, diagnosis, doctorId);
                return ServiceResult<List<FormulaRecommendationDto>>.Failure($"获取推荐验方失败: {ex.Message}", ex);
            }
        }

        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("获取验方导入模板");

                var templateContent = @"验方导入模板 - UltraThink精简版
必填列：验方名称, 功效, 用法
可选列：性味归经, 是否共享(true/false), 备注

注意：
- 拼音码由系统自动生成，无需填写
  规则：每个字拼音首字母大写组合（如：六味地黄丸 → LWDHW）
- 验方名称不能重复
- 功效和用法为必填项
- 是否共享默认为false";

                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取导入模板异常");
                return ServiceResult<byte[]>.Failure($"获取导入模板异常: {ex.Message}", ex);
            }
        }

        #endregion
        
        #endregion
    }
}