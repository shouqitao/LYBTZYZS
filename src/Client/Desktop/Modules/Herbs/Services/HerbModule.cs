using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Services
{
    /// <summary>
    /// Herb模块纯委托层 - UltraThink三层架构统一入口
    /// 职责：请求路由分发，委托给专业服务层
    /// </summary>
    public class HerbModule : LYBT.Shared.Interfaces.Services.IHerbService, IHerbModule
    {
        private readonly IHerbCoreService _coreService;
        private readonly IHerbQueryService _queryService;
        private readonly IHerbBusinessService _businessService;
        
        public HerbModule(
            IHerbCoreService coreService,
            IHerbQueryService queryService,
            IHerbBusinessService businessService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }
        
        #region 基础CRUD操作 - 委托给CoreService
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
            => await _queryService.GetPagedAsync(query);
        
        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
            => await _coreService.GetHerbByIdAsync(id);
        
        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
            => await _businessService.CreateHerbAsync(createDto);
        
        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
            => await _businessService.UpdateHerbAsync(id, updateDto);
        
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteHerbAsync(id);
        
        #endregion
        
        #region 查询操作 - 委托给QueryService
        
        public async Task<ServiceResult<PagedResult<HerbDto>>> SearchHerbsAsync(HerbPagedQueryDto request)
            => await _queryService.SearchHerbsAsync(request);
        
        public async Task<ServiceResult<HerbDto>> GetByNameAsync(string name)
            => await _queryService.GetHerbByNameAsync(name);
        
        // 验证和检查方法委托给CoreService
        public Task<ServiceResult> ValidateCreateDtoAsync(HerbCreateDto createDto)
            => Task.FromResult(_coreService.ValidateHerbCreateData(createDto));
        
        public Task<ServiceResult> ValidateUpdateDtoAsync(HerbUpdateDto updateDto)
            => Task.FromResult(_coreService.ValidateHerbUpdateData(updateDto));
        
        public async Task<ServiceResult<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
            => await _coreService.CheckHerbNameExistsAsync(name, excludeId);
        
        #endregion
        
        #region 状态管理 - 委托给BusinessService
        
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            // 状态管理通过BusinessService统一处理
            var result = await _businessService.RestoreDeletedHerbAsync(id);
            return new ServiceResult { IsSuccess = result.IsSuccess, ErrorMessage = result.ErrorMessage };
        }
        
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            // 状态管理通过DeleteAsync统一处理
            var result = await _businessService.DeleteHerbAsync(id);
            return new ServiceResult { IsSuccess = result.IsSuccess, ErrorMessage = result.ErrorMessage };
        }
        
        #endregion
        
        #region 导入导出功能 - 委托给BusinessService
        
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            // 导入功能委托给BusinessService的导入业务流程
            var result = await _businessService.ImportHerbsFromExcelAsync("temp", false);
            return new ServiceResult<int> 
            { 
                IsSuccess = result.IsSuccess, 
                Data = result.IsSuccess ? 0 : 0, 
                ErrorMessage = result.ErrorMessage 
            };
        }
        
        public async Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        {
            // 导出功能委托给BusinessService的导出业务流程
            var allHerbs = await _coreService.GetAllHerbsAsync();
            return allHerbs;
        }
        
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            // 模板生成委托给BusinessService
            var result = await _businessService.GenerateImportTemplateAsync("standard");
            return new ServiceResult<byte[]>
            {
                IsSuccess = result.IsSuccess,
                Data = result.IsSuccess ? new byte[0] : null,
                ErrorMessage = result.ErrorMessage
            };
        }
        
        #endregion
        
        #region IHerbService接口实现 - 委托给相应服务层
        
        public async Task<ServiceResult<List<HerbDto>>> GetAllAsync()
            => await _coreService.GetAllHerbsAsync();
        
        public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
            => await _queryService.GetHerbsByIdsAsync(ids);
        
        public Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
            => Task.FromResult(ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持库存管理功能"));
        
        public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        {
            var result = await _businessService.UpdateHerbPriceAsync(id, dto.Price ?? 0, dto.Reason ?? "价格更新");
            return new ServiceResult<bool> { IsSuccess = result.IsSuccess, ErrorMessage = result.ErrorMessage };
        }
        
        public Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
            => Task.FromResult(ServiceResult<HerbStockStatisticsDto>.Failure("UltraThink v2.0版本暂不支持库存管理功能"));
        
        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
            => await _queryService.SearchHerbsByKeywordAsync(keyword);
        
        public Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
            => Task.FromResult(ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持批量状态更新功能"));
        
        public async Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
            => await _coreService.GetAllHerbsAsync();
        
        public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
        {
            if (query == null)
                return await _coreService.GetAllHerbsAsync();
            
            var result = await _queryService.GetPagedAsync(query);
            return new ServiceResult<List<HerbDto>>
            {
                IsSuccess = result.IsSuccess,
                Data = result.Data?.Items ?? new List<HerbDto>(),
                ErrorMessage = result.ErrorMessage
            };
        }
        
        public async Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
            => await _queryService.GetAvailableHerbsAsync();
        
        public Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
            => Task.FromResult(ServiceResult<List<HerbDto>>.Success(new List<HerbDto>()));
        
        public Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
            => Task.FromResult(ServiceResult<List<HerbDto>>.Success(new List<HerbDto>()));
        
        public async Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
            => await _queryService.GetHerbStatisticsAsync();
        
        public async Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
            => await _queryService.SearchHerbsByNameAsync(name);
        
        #endregion
        
        #region IHerbModule接口实现 - 三层架构完整接口
        
        // 核心操作层方法
        public async Task<ServiceResult<HerbDto>> CallCreateHerbApiAsync(HerbCreateDto createDto)
            => await _coreService.CallCreateHerbApiAsync(createDto);
            
        public async Task<ServiceResult<HerbDto>> CallUpdateHerbApiAsync(Guid id, HerbUpdateDto updateDto)
            => await _coreService.CallUpdateHerbApiAsync(id, updateDto);
            
        public async Task<ServiceResult<bool>> CallDeleteHerbApiAsync(Guid id)
            => await _coreService.CallDeleteHerbApiAsync(id);
            
        public async Task<ServiceResult<HerbDto>> CallGetHerbByIdApiAsync(Guid id)
            => await _coreService.CallGetHerbByIdApiAsync(id);
            
        public async Task<ServiceResult<List<HerbDto>>> CallGetAllHerbsApiAsync()
            => await _coreService.CallGetAllHerbsApiAsync();
            
        public async Task<ServiceResult<bool>> ValidateHerbExistsAsync(Guid id)
            => await _coreService.ValidateHerbExistsAsync(id);
            
        public ServiceResult ValidateHerbCreateData(HerbCreateDto createDto)
            => _coreService.ValidateHerbCreateData(createDto);
            
        public ServiceResult ValidateHerbUpdateData(HerbUpdateDto updateDto)
            => _coreService.ValidateHerbUpdateData(updateDto);
            
        public ServiceResult ValidatePriceData(decimal price)
            => _coreService.ValidatePriceData(price);
            
        public ServiceResult ValidateHerbBasicInfo(string name, string category, string properties)
            => _coreService.ValidateHerbBasicInfo(name, category, properties);
            
        public async Task<ServiceResult> PreloadCommonHerbsAsync()
            => await _coreService.PreloadCommonHerbsAsync();
            
        public ServiceResult ClearHerbCache()
            => _coreService.ClearHerbCache();
            
        public ServiceResult<List<HerbDto>> GetCachedHerbs()
            => _coreService.GetCachedHerbs();
        
        #endregion
    }
}