using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.FormulaTemplates;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 验方模板服务实现
    /// </summary>
    public class FormulaTemplateService : IFormulaTemplateService
    {
        private readonly IFormulaTemplateApiService _apiService;

        public FormulaTemplateService(IFormulaTemplateApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ServiceResult<List<FormulaTemplateInfo>>> GetListAsync(string? keyword = null, string? category = null)
        {
            try
            {
                var response = await _apiService.GetFormulaTemplatesAsync(keyword, category);
                if (response.Success && response.Data != null)
                {
                    var templates = response.Data.Select(ConvertToFormulaTemplateInfo).ToList();
                    return ServiceResult<List<FormulaTemplateInfo>>.Success(templates);
                }
                return ServiceResult<List<FormulaTemplateInfo>>.Failure(response.Message ?? "获取验方模板列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaTemplateInfo>>.Failure($"获取验方模板列表时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _apiService.GetFormulaTemplateByIdAsync(id);
                if (response.Success && response.Data != null)
                {
                    var template = ConvertToFormulaTemplateInfo(response.Data);
                    return ServiceResult<FormulaTemplateInfo>.Success(template);
                }
                return ServiceResult<FormulaTemplateInfo>.Failure(response.Message ?? "获取验方模板详情失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaTemplateInfo>.Failure($"获取验方模板详情时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> CreateAsync(FormulaTemplateInfo template)
        {
            try
            {
                var createDto = new FormulaTemplateCreateDto
                {
                    Name = template.Name,
                    Category = template.Category,
                    Indications = template.Indications,
                    Usage = template.Usage,
                    Dosage = template.Dosage,
                    Contraindications = template.Contraindications,
                    Source = template.Source,
                    Remark = template.Remark,
                    Herbs = template.Herbs.Select(h => new FormulaHerbDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod,
                        SpecialInstructions = h.SpecialInstructions
                    }).ToList()
                };

                var response = await _apiService.CreateFormulaTemplateAsync(createDto);
                if (response.Success && response.Data != null)
                {
                    var createdTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return ServiceResult<FormulaTemplateInfo>.Success(createdTemplate);
                }
                return ServiceResult<FormulaTemplateInfo>.Failure(response.Message ?? "创建验方模板失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaTemplateInfo>.Failure($"创建验方模板时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> UpdateAsync(FormulaTemplateInfo template)
        {
            try
            {
                var updateDto = new FormulaTemplateUpdateDto
                {
                    Name = template.Name,
                    Category = template.Category,
                    Indications = template.Indications,
                    Usage = template.Usage,
                    Dosage = template.Dosage,
                    Contraindications = template.Contraindications,
                    Source = template.Source,
                    Remark = template.Remark,
                    Herbs = template.Herbs.Select(h => new FormulaHerbDto
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage,
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod,
                        SpecialInstructions = h.SpecialInstructions
                    }).ToList()
                };

                var response = await _apiService.UpdateFormulaTemplateAsync(template.Id, updateDto);
                if (response.Success && response.Data != null)
                {
                    var updatedTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return ServiceResult<FormulaTemplateInfo>.Success(updatedTemplate);
                }
                return ServiceResult<FormulaTemplateInfo>.Failure(response.Message ?? "更新验方模板失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaTemplateInfo>.Failure($"更新验方模板时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _apiService.DeleteFormulaTemplateAsync(id);
                return response.Success 
                    ? ServiceResult<bool>.Success(true) 
                    : ServiceResult<bool>.Failure(response.Message ?? "删除验方模板失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除验方模板时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                var response = await _apiService.BatchDeleteFormulaTemplatesAsync(ids);
                return response.Success && response.Data.HasValue
                    ? ServiceResult<int>.Success(response.Data.Value)
                    : ServiceResult<int>.Failure(response.Message ?? "批量删除验方模板失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量删除验方模板时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> CopyAsync(Guid id, string newName)
        {
            try
            {
                var response = await _apiService.CopyFormulaTemplateAsync(id, newName);
                if (response.Success && response.Data != null)
                {
                    var copiedTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return ServiceResult<FormulaTemplateInfo>.Success(copiedTemplate);
                }
                return ServiceResult<FormulaTemplateInfo>.Failure(response.Message ?? "复制验方模板失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaTemplateInfo>.Failure($"复制验方模板时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var response = await _apiService.ToggleFormulaTemplateStatusAsync(id);
                return response.Success
                    ? ServiceResult<bool>.Success(true)
                    : ServiceResult<bool>.Failure(response.Message ?? "切换验方模板状态失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"切换验方模板状态时发生错误：{ex.Message}");
            }
        }

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var response = await _apiService.GetCategoriesAsync();
                return response.Success && response.Data != null
                    ? ServiceResult<List<string>>.Success(response.Data)
                    : ServiceResult<List<string>>.Failure(response.Message ?? "获取分类列表失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<string>>.Failure($"获取分类列表时发生错误：{ex.Message}");
            }
        }

        #region Private Methods

        private FormulaTemplateInfo ConvertToFormulaTemplateInfo(FormulaTemplateDto dto)
        {
            return new FormulaTemplateInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = dto.Category ?? "其他",
                Indications = dto.Indications,
                Usage = dto.Usage,
                Dosage = dto.Dosage,
                Contraindications = dto.Contraindications,
                Source = dto.Source,
                Remark = dto.Remark,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedTime = dto.CreatedTime,
                UpdatedTime = dto.UpdatedTime,
                Herbs = dto.Herbs?.Select(h => new FormulaHerbItem
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    UnitPrice = h.UnitPrice,
                    ProcessingMethod = h.ProcessingMethod,
                    SpecialInstructions = h.SpecialInstructions
                }).ToList() ?? new List<FormulaHerbItem>()
            };
        }

        private FormulaTemplateInfo ConvertToFormulaTemplateInfo(FormulaTemplateDetailDto dto)
        {
            return new FormulaTemplateInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = dto.Category ?? "其他",
                Indications = dto.Indications,
                Usage = dto.Usage,
                Dosage = dto.Dosage,
                Contraindications = dto.Contraindications,
                Source = dto.Source,
                Remark = dto.Remark,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedTime = dto.CreatedTime,
                UpdatedTime = dto.UpdatedTime,
                Herbs = dto.Herbs?.Select(h => new FormulaHerbItem
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    UnitPrice = h.UnitPrice,
                    ProcessingMethod = h.ProcessingMethod,
                    SpecialInstructions = h.SpecialInstructions
                }).ToList() ?? new List<FormulaHerbItem>()
            };
        }

        #endregion
    }
}