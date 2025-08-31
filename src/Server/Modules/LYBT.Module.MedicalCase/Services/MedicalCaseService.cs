using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - UltraThink三层架构纯委托模式
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly Core.MedicalCaseServiceCore _coreService;
        private readonly MedicalCaseQueryService _queryService;
        private readonly MedicalCaseBusinessService _businessService;

        public MedicalCaseService(
            Core.MedicalCaseServiceCore coreService,
            MedicalCaseQueryService queryService,
            MedicalCaseBusinessService businessService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Query Operations

        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
            => await _queryService.GetByPatientIdAsync(patientId);

        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
            => await _queryService.GetActiveByPatientIdAsync(patientId);

        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
            => await _queryService.GetHistoryAsync(id);

        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
            => await _queryService.HasActiveCaseAsync(patientId);

        #endregion

        #region Core Operations

        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
            => await _businessService.CreateAsync(dto);

        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
            => await _businessService.UpdateAsync(id, dto);

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteAsync(id);

        #endregion

        #region Status Management

        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
            => await _businessService.CompleteAsync(id, completionReason);

        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
            => await _businessService.SuspendAsync(id, reason);

        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
            => await _businessService.ResumeAsync(id);

        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
            => await _businessService.ArchiveAsync(id, archiveReason);

        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
            => await _businessService.UpdateStatusAsync(id, status);

        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
            => await UpdateStatusAsync(id, (int)Shared.Models.Enums.MedicalCaseStatus.Cancelled);

        #endregion

        #region Batch Operations

        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(Guid[] ids, int status)
        {
            if (!Enum.IsDefined(typeof(Shared.Models.Enums.MedicalCaseStatus), status))
                return ServiceResult<int>.Failure($"无效的状态值: {status}");

            var medicalCaseStatus = (Shared.Models.Enums.MedicalCaseStatus)status;
            return await _businessService.BatchUpdateStatusAsync(ids, medicalCaseStatus);
        }

        #endregion

        #region Legacy Support

        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            await Task.CompletedTask;
            var emptyStats = new { Message = "统计功能已废弃", TotalCount = 0 };
            return ServiceResult<object>.Success(emptyStats);
        }

        public async Task<ServiceResult<byte[]>> PrintMedicalRecordAsync(Guid caseId)
        {
            var caseResult = await GetByIdAsync(caseId);
            if (!caseResult.IsSuccess || caseResult.Data == null)
                return ServiceResult<byte[]>.Failure("医疗案例不存在");

            var tempBytes = System.Text.Encoding.UTF8.GetBytes($"医疗病历打印 - 案例ID: {caseId}, 生成时间: {DateTime.Now}");
            return ServiceResult<byte[]>.Success(tempBytes);
        }

        #endregion
    }
}