using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services.Archive
{
    /// <summary>
    /// 患者档案服务适配器
    /// UltraThink重构：适配现有PatientArchiveService到新的接口体系
    /// 保持向后兼容性，同时提供清晰的接口抽象
    /// </summary>
    public class PatientArchiveServiceAdapter : IPatientArchiveService
    {
        private readonly PatientArchiveService _archiveService;
        private readonly ILogger<PatientArchiveServiceAdapter> _logger;

        public PatientArchiveServiceAdapter(
            PatientArchiveService archiveService,
            ILogger<PatientArchiveServiceAdapter> logger)
        {
            _archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 更新患者过敏史
        /// </summary>
        public async Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName)
        {
            try
            {
                return await _archiveService.UpdateAllergyHistoryAsync(patientId, allergyHistory, operatorId, operatorName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "适配器调用更新过敏史失败: {PatientId}", patientId);                throw;
            }
        }

        /// <summary>
        /// 获取患者标签
        /// </summary>
        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId)
        {
            try
            {
                return await _archiveService.GetPatientTagsAsync(patientId);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用获取患者标签失败: {PatientId}", patientId);                throw;
            }
        }

        /// <summary>
        /// 设置患者标签
        /// </summary>
        public async Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName)
        {
            try
            {
                return await _archiveService.SetPatientTagsAsync(patientId, tags, operatorId, operatorName);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用设置患者标签失败: {PatientId}", patientId);                throw;
            }
        }

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>
        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId)
        {
            try
            {
                return await _archiveService.GetVisitHistoryAsync(patientId);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用获取就诊历史失败: {PatientId}", patientId);                throw;
            }
        }

        /// <summary>
        /// 合并重复患者
        /// </summary>
        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName)
        {
            try
            {
                return await _archiveService.MergeDuplicatePatientsAsync(primaryId, duplicateId, operatorId, operatorName);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用合并重复患者失败: Primary={PrimaryId}, Duplicate={DuplicateId}", primaryId, duplicateId);                throw;
            }
        }

        /// <summary>
        /// 批量导入患者
        /// </summary>
        public async Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName)
        {
            try
            {
                return await _archiveService.ImportPatientsAsync(patients, operatorId, operatorName);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用批量导入患者失败");                throw;
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query)
        {
            try
            {
                return await _archiveService.ExportPatientsAsync(query);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "适配器调用导出患者数据失败");
                throw;
            }
        }
    }
}
