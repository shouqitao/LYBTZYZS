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
                return response.Data ?? throw new InvalidOperationException($"更新处方失败，ID: {id}");
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
                return response.Data ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案处方列表失败: {MedicalCaseId}", medicalCaseId);
                return new List<PrescriptionDto>();
            }
        }

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13, Issue #1374 ENTRY-16)
        /// </summary>
        public async Task<List<PrescriptionSearchResultDto>> GetPatientRecentPrescriptionsAsync(Guid patientId, int count = 5)
        {
            try
            {
                var response = await _api.GetPatientRecentPrescriptionsAsync(patientId, count);
                return response.Data ?? new List<PrescriptionSearchResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者最近处方失败: PatientId={PatientId}, Count={Count}", patientId, count);
                return new List<PrescriptionSearchResultDto>();
            }
        }

        /// <summary>
        /// 导入验方到处方 (Issue #1366 ENTRY-8, Issue #1367 ENTRY-9)
        /// 从已验证的验方批量导入药材，并记录引用的验方名称
        /// </summary>
        public async Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(Guid prescriptionId, Guid formulaId)
        {
            if (prescriptionId == Guid.Empty)
            {
                _logger.LogError("Cannot import formula with invalid prescription id");
                throw new ArgumentException("Prescription ID is required", nameof(prescriptionId));
            }
            if (formulaId == Guid.Empty)
            {
                _logger.LogError("Cannot import formula with invalid formula id");
                throw new ArgumentException("Formula ID is required", nameof(formulaId));
            }

            try
            {
                var response = await _api.ImportFormulaIntoPrescriptionAsync(prescriptionId, formulaId);
                return response.Data ?? throw new InvalidOperationException($"导入验方失败，处方ID: {prescriptionId}, 验方ID: {formulaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方失败，处方ID: {PrescriptionId}, 验方ID: {FormulaId}", prescriptionId, formulaId);
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<PrescriptionDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetPrescriptionByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<PrescriptionDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetPrescriptionsAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<PrescriptionDto>> CallApiCreateAsync(PrescriptionCreateDto dto)
        {
            return _api.CreatePrescriptionAsync(dto);
        }

        protected override Task<ApiResponse<PrescriptionDto>> CallApiUpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            return _api.UpdatePrescriptionAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
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