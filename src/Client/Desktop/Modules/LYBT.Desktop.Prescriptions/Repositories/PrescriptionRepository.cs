using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Repositories
{
    /// <summary>
    /// 处方数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class PrescriptionRepository : RepositoryBase<PrescriptionDto, PrescriptionCreateDto, PrescriptionUpdateDto, IPrescriptionApi>, IPrescriptionRepository
    {
        public PrescriptionRepository(
            IPrescriptionApi prescriptionApi,
            ILogger<PrescriptionRepository> logger)
            : base(prescriptionApi, logger)
        {
        }

        /// <summary>
        /// 更新处方信息（保持原有签名：接受id和dto参数）
        /// </summary>
        public async Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            if (id == Guid.Empty)
            {
                _logger.LogError("Cannot update prescription with invalid id");
                throw new ArgumentException("Prescription ID is required", nameof(id));
            }
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _api.UpdatePrescriptionAsync(id, dto);
                return response.Content ?? throw new InvalidOperationException($"更新处方失败，ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医案ID获取处方列表
        /// </summary>
        public async Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var response = await _api.GetPrescriptionsByMedicalCaseIdAsync(medicalCaseId);
                return response.Content ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案处方列表失败: {MedicalCaseId}", medicalCaseId);
                return new List<PrescriptionDto>();
            }
        }

        /// <summary>
        /// 导入验方到处方
        /// ENTRY-10: 集成导入命令到PrescriptionComposerViewModel
        /// </summary>
        public async Task<ServiceResult> ImportFormulaAsync(Guid prescriptionId, Guid formulaId)
        {
            try
            {
                var response = await _api.ImportFormulaAsync(prescriptionId, formulaId);
                return response.Content ?? ServiceResult.Failure("导入验方失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方失败: PrescriptionId={PrescriptionId}, FormulaId={FormulaId}", prescriptionId, formulaId);
                return ServiceResult.Failure($"导入验方失败：{ex.Message}");
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<Refit.ApiResponse<PrescriptionDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetPrescriptionByIdAsync(id);
        }

        protected override Task<Refit.ApiResponse<PagedResult<PrescriptionDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetPrescriptionsAsync(page, pageSize, keyword);
        }

        protected override Task<Refit.ApiResponse<PrescriptionDto>> CallApiCreateAsync(PrescriptionCreateDto dto)
        {
            return _api.CreatePrescriptionAsync(dto);
        }

        protected override Task<Refit.ApiResponse<PrescriptionDto>> CallApiUpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            return _api.UpdatePrescriptionAsync(id, dto);
        }

        protected override Task<Refit.ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeletePrescriptionAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(PrescriptionUpdateDto dto)
        {
            // PrescriptionUpdateDto没有Id属性，返回null表示需要外部提供ID
            return null;
        }

        #endregion
    }
}