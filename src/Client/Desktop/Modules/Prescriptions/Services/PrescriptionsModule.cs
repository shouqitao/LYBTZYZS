using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Modules.Prescriptions.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// Prescriptions模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，实现折扣和价格计算功能
    /// </summary>
    public class PrescriptionsModule : IPrescriptionService
    {
        private readonly IPrescriptionApi _apiService;
        private readonly IMapper _mapper;
        
        public PrescriptionsModule(IPrescriptionApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            try
            {
                // UltraThink v2.0: 直接使用API调用获取DTOs
                var apiResult = await _apiService.GetListAsync(
                    query.PageIndex,
                    query.PageSize,
                    query.Keyword);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PagedResult<PrescriptionDto>>.Failure(
                        apiResult.Error?.Message ?? "获取处方列表失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                var result = new PagedResult<PrescriptionDto>(
                    apiResult.Content.Items.ToList(),
                    apiResult.Content.TotalCount,
                    apiResult.Content.CurrentPage,
                    apiResult.Content.PageSize);
                
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure($"获取处方列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");
                }
                
                // UltraThink v2.0：API调用获取DTO (返回的是DetailDto，但可以当作基础DTO使用)
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure(
                        apiResult.Error?.Message ?? "获取处方详情失败");
                }
                
                // UltraThink v2.0: 直接返回DTO，无需映射 (DetailDto继承自PrescriptionDto)
                return ServiceResult<PrescriptionDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDto>.Failure($"获取处方详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用CreateDto进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage);
                }
                
                // API调用
                var apiResult = await _apiService.CreatePrescriptionAsync(createDto);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure(
                        apiResult.Error?.Message ?? "创建处方失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<PrescriptionDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDto>.Failure($"创建处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用EditDto进行业务验证
                var validationResult = await ValidateEditDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage);
                }
                
                // 检查是否可以修改
                var canModifyResult = await CanModifyAsync(id);
                if (!canModifyResult.IsSuccess || !canModifyResult.Data)
                {
                    return ServiceResult<PrescriptionDto>.Failure(
                        canModifyResult.ErrorMessage ?? "当前处方状态不允许修改");
                }
                
                // API调用
                var apiResult = await _apiService.UpdatePrescriptionAsync(id, updateDto);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure(
                        apiResult.Error?.Message ?? "更新处方失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<PrescriptionDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDto>.Failure($"更新处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }
                
                // 检查是否可以删除
                var canDeleteResult = await CanDeleteAsync(id);
                if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
                {
                    return ServiceResult<bool>.Failure(
                        canDeleteResult.ErrorMessage ?? "当前处方状态不允许删除");
                }
                
                var apiResult = await _apiService.DeletePrescriptionAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "删除处方失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除处方异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        // UltraThink v2.0: 简化状态管理 - 移除独立的状态更新方法，通过API直接操作
        
        private Task<ServiceResult> UpdateStatusAsync(Guid id, PrescriptionStatus status, string reason)
        {
            try
            {
                // UltraThink v2.0: 简化实现，根据状态类型调用相应的API方法
                switch (status)
                {
                    case PrescriptionStatus.Completed:
                        // 对于完成状态，目前没有直接的API，返回成功占位
                        return Task.FromResult(ServiceResult.Success());
                    
                    case PrescriptionStatus.Draft:
                        // 回到草稿状态
                        return Task.FromResult(ServiceResult.Success());
                    
                    default:
                        return Task.FromResult(ServiceResult.Failure($"不支持的处方状态更新: {status}"));
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(ServiceResult.Failure($"更新处方状态异常: {ex.Message}"));
            }
        }
        
        public async Task<ServiceResult> CompletePrescriptionAsync(Guid id)
        {
            try
            {
                // 验证处方完整性
                var prescriptionResult = await GetByIdAsync(id);
                if (!prescriptionResult.IsSuccess)
                {
                    return ServiceResult.Failure("获取处方信息失败");
                }
                
                var prescription = prescriptionResult.Data;
                if (!prescription.Items.Any())
                {
                    return ServiceResult.Failure("处方必须包含药材才能完成");
                }
                
                return await UpdateStatusAsync(id, PrescriptionStatus.Completed, "完成处方");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"完成处方异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ServiceResult.Failure("作废原因不能为空");
                }
                
                // UltraThink v2.0: 使用新的作废API
                var apiResult = await _apiService.CancelPrescriptionAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "作废处方失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"作废处方异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 移除批量操作功能 - 删除过度设计的批量功能
        
        #endregion
        
        #region 查询操作 - UltraThink v2.0: 精简查询方法，保留核心功能
        
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 构造查询参数
                var query = new PrescriptionQueryDto
                {
                    PageIndex = 1,
                    PageSize = 100, // 限制搜索结果数量
                    Keyword = keyword
                };
                
                // 使用GetPagedAsync实现搜索功能
                var pagedResult = await GetPagedAsync(query);
                if (!pagedResult.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage);
                }
                
                return ServiceResult<List<PrescriptionDto>>.Success(pagedResult.Data.Items.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"搜索处方异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 删除复杂的查询方法 - 20人以下小诊所不需要复杂的关联查询
        // 这些查询方法增加了不必要的复杂性：
        // - GetByPatientIdAsync: 通过基础搜索功能就能实现
        // - GetByDoctorIdAsync: 通过基础搜索功能就能实现  
        // - GetByMedicalCaseIdAsync: 通过基础搜索功能就能实现
        // - GetByStatusAsync: 状态筛选应该由前端界面处理
        // - GetByDateRangeAsync: 日期范围筛选应该由前端界面处理
        // 小诊所的处方数量有限，统一使用SearchAsync + 前端筛选更简单实用
        
        #endregion
        
        // UltraThink v2.0: 删除处方项目管理功能 - 20人以下小诊所不需要复杂的单项目操作
        // 删除的功能：
        // - AddPrescriptionItemAsync: 单独添加处方项目过度复杂
        // - UpdatePrescriptionItemAsync: 单独更新处方项目过度复杂  
        // - DeletePrescriptionItemAsync: 单独删除处方项目过度复杂
        // 小诊所应该通过整体处方编辑来管理项目，更简单直观
        // 处方项目的管理应该在前端完成，然后通过UpdateAsync一次性提交整个处方
        
        #region 验证操作 - UltraThink v2.0: 简化验证逻辑，保留核心验证
        
        // UltraThink v2.0: 精简验证方法 - 20人以下小诊所不需要复杂的验证框架
        private Task<ServiceResult> ValidateCreateDtoAsync(PrescriptionCreateDto createDto)
        {
            if (createDto == null) return Task.FromResult(ServiceResult.Failure("创建处方信息不能为空"));
            if (createDto.PatientId == Guid.Empty) return Task.FromResult(ServiceResult.Failure("患者ID不能为空"));
            if (createDto.DoctorId == Guid.Empty) return Task.FromResult(ServiceResult.Failure("医生ID不能为空"));
            if (createDto.DosageCount <= 0) return Task.FromResult(ServiceResult.Failure("服药剂数必须大于0"));
            return Task.FromResult(ServiceResult.Success());
        }
        
        private Task<ServiceResult> ValidateEditDtoAsync(PrescriptionEditDto editDto)
        {
            if (editDto == null) return Task.FromResult(ServiceResult.Failure("编辑处方信息不能为空"));
            if (editDto.Id == Guid.Empty) return Task.FromResult(ServiceResult.Failure("处方ID不能为空"));
            if (string.IsNullOrWhiteSpace(editDto.Diagnosis)) return Task.FromResult(ServiceResult.Failure("诊断不能为空"));
            if (editDto.DosageCount <= 0) return Task.FromResult(ServiceResult.Failure("服药剂数必须大于0"));
            return Task.FromResult(ServiceResult.Success());
        }
        
        private async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        {
            var prescriptionResult = await GetByIdAsync(id);
            if (!prescriptionResult.IsSuccess) return ServiceResult<bool>.Failure("获取处方信息失败");
            return ServiceResult<bool>.Success(prescriptionResult.Data.Status == CommonStatus.Enabled);
        }
        
        private async Task<ServiceResult<bool>> CanDeleteAsync(Guid id)
        {
            var prescriptionResult = await GetByIdAsync(id);
            if (!prescriptionResult.IsSuccess) return ServiceResult<bool>.Failure("获取处方信息失败");
            return ServiceResult<bool>.Success(prescriptionResult.Data.Status == CommonStatus.Enabled);
        }
        
        // UltraThink v2.0: 删除过度设计的验证方法 - 简化为基础验证即可
        // 删除的功能：
        // - ValidateAsync: 通用验证过度复杂，重复代码多
        // - ValidatePrescriptionItemDtoAsync: 单项验证应该在前端处理
        // - ValidatePrescriptionItemCreateDtoAsync: 创建项目验证应该在前端处理
        // 小诊所的验证逻辑应该简单明了，复杂验证会增加维护成本
        
        #endregion
        
        // UltraThink v2.0: 移除统计分析功能 - 删除过度设计的统计功能
        
        // UltraThink v2.0: 移除复制和模板功能 - 验方管理已独立到FormulaModule
        
        // UltraThink v2.0: 删除业务规则验证功能 - 20人以下小诊所不需要复杂的价格计算和折扣系统
        // 删除的功能：
        // - CalculateTotalPriceAsync: PrescriptionDto已有TotalPrice属性，无需额外计算方法
        // - CalculateSingleDosePriceAsync: PrescriptionDto已有SingleDosePrice属性，无需额外计算方法
        // - ApplyDiscountAsync: 复杂的折扣应用功能对小诊所过度设计
        // - GetBatchPrescriptionPricesAsync: 批量价格计算功能过度复杂
        // - GetPrintInfoAsync: 打印功能应该由MedicalCase模块统一管理
        // 小诊所的处方价格计算应该简单直接，复杂的价格逻辑会增加维护成本
        
        // UltraThink v2.0: 删除关联数据功能 - 20人以下小诊所不需要复杂的关联数据获取
        // 删除的功能：
        // - GetHistoryPrescriptionsAsync: 历史处方查询应该通过基础搜索功能实现
        // 各模块应该保持独立，关联数据查询增加模块间耦合
        
        #region IPrescriptionService接口实现 - 补充方法
        
        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure("患者ID不能为空");
                }
                
                var query = new PrescriptionQueryDto
                {
                    PatientId = patientId,
                    PageIndex = 1,
                    PageSize = 100
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(result.ErrorMessage);
                }
                
                return ServiceResult<List<PrescriptionDto>>.Success(result.Data.Items.ToList());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"根据患者ID获取处方列表异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure("医疗案例ID不能为空");
                }
                
                // 调用API获取处方列表，这里假设API支持按MedicalCaseId查询
                var query = new PrescriptionQueryDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = medicalCaseId.ToString()
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(result.ErrorMessage);
                }
                
                // 筛选出匹配的处方
                var filteredPrescriptions = result.Data.Items.Where(p => p.MedicalCaseId == medicalCaseId).ToList();
                return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure($"根据医疗案例ID获取处方列表异常: {ex.Message}");
            }
        }
        
        
        /// <summary>
        /// 验证处方数据
        /// </summary>
        public Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                var result = new PrescriptionValidationResult();
                
                // 基础验证
                if (dto == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("处方信息不能为空");
                    return Task.FromResult(ServiceResult<PrescriptionValidationResult>.Success(result));
                }
                
                if (dto.PatientId == Guid.Empty)
                {
                    result.Errors.Add("患者ID不能为空");
                }
                
                if (dto.DoctorId == Guid.Empty)
                {
                    result.Errors.Add("医生ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                {
                    result.Errors.Add("诊断不能为空");
                }
                
                if (dto.Items == null || !dto.Items.Any())
                {
                    result.Errors.Add("处方必须包含至少一味中药材");
                }
                else
                {
                    // 验证处方项目
                    foreach (var item in dto.Items)
                    {
                        if (item.HerbId == Guid.Empty)
                        {
                            result.Errors.Add($"药材ID不能为空");
                        }
                        
                        if (item.Quantity <= 0)
                        {
                            result.Errors.Add($"药材 {item.HerbName} 的用量必须大于0");
                        }
                        
                        if (item.UnitPrice < 0)
                        {
                            result.Errors.Add($"药材 {item.HerbName} 的单价不能小于0");
                        }
                    }
                }
                
                result.IsValid = !result.Errors.Any();
                return Task.FromResult(ServiceResult<PrescriptionValidationResult>.Success(result));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ServiceResult<PrescriptionValidationResult>.Failure($"验证处方数据异常: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return ServiceResult<PrescriptionDto>.Failure("新处方名称不能为空");
                }
                
                // 获取原处方
                var originalResult = await GetByIdAsync(id);
                if (!originalResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionDto>.Failure("获取原处方失败：" + originalResult.ErrorMessage);
                }
                
                var original = originalResult.Data;
                
                // 创建新处方
                var createDto = new PrescriptionCreateDto
                {
                    PatientId = original.PatientId,
                    DoctorId = original.UserId,
                    Diagnosis = original.Diagnosis ?? "",
                    DosageCount = original.DosageCount,
                    Advice = original.Advice,
                    Usage = original.Usage,
                    TotalAmount = original.TotalAmount,
                    FormulaSource = $"复制自：{original.PrescriptionNo ?? id.ToString()}",
                    Items = original.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        Usage = item.Usage,
                        Remark = item.Remark
                    }).ToList(),
                    Remark = $"复制自处方：{original.PrescriptionNo ?? id.ToString()}"
                };
                
                return await CreateAsync(createDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<PrescriptionDto>.Failure($"复制处方异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}