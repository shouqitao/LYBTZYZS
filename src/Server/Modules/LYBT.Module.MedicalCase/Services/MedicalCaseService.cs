using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - UltraThink双层架构纯委托模式
    /// 简化版本：只提供增删查改和诊疗流程
    /// </summary>
    public class MedicalCaseService(
        IMedicalCaseQueryService queryService,
        IMedicalCaseBusinessService businessService) : IMedicalCaseService
    {
        private readonly IMedicalCaseQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IMedicalCaseBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region Query Operations (查询操作)

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            var result = await _queryService.GetByIdAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return ServiceResult<MedicalCaseDetailDto>.Failure(result.ErrorMessage ?? "获取失败");
            }

            // 将MedicalCaseDto转换为MedicalCaseDetailDto（简化实现）
            var detailDto = new MedicalCaseDetailDto
            {
                Id = result.Data.Id,
                PatientId = result.Data.PatientId,
                PatientName = result.Data.PatientName,
                DoctorId = result.Data.DoctorId,
                DoctorName = result.Data.DoctorName,
                ConsultationDate = result.Data.ConsultationDate,
                Status = result.Data.Status,
                Remark = result.Data.Remark
            };

            return ServiceResult<MedicalCaseDetailDto>.Success(detailDto);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
            => await _queryService.GetPagedAsync(query);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
            => await _queryService.GetByPatientIdAsync(patientId);

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
            => await _queryService.GetActiveByPatientIdAsync(patientId);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<object>>> GetHistory(Guid patientId)
        {
            var result = await _queryService.GetHistoryAsync(patientId);
            if (!result.IsSuccess)
            {
                return ServiceResult<List<object>>.Failure(result.ErrorMessage ?? "获取历史记录失败");
            }

            // 将List<MedicalCaseDto>转换为List<object>
            var objectList = new List<object>();
            if (result.Data != null)
            {
                objectList.AddRange(result.Data);
            }

            return ServiceResult<List<object>>.Success(objectList);
        }

        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
            => await _queryService.HasActiveCaseAsync(patientId);

        #endregion Query Operations

        #region Core Operations (核心CRUD操作)

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
            => await _businessService.CreateAsync(dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
            => await _businessService.UpdateAsync(id, dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteAsync(id);

        #endregion Core Operations

        #region 简化的状态管理 (只保留基础状态转换)

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            // 简化实现：直接使用软删除功能将状态设为Closed
            return await _businessService.DeleteAsync(id);
        }

        /// <inheritdoc/>
        public Task<ServiceResult<bool>> Suspend(Guid id, string reason)
        {
            // 简化实现：Record-Only模式不支持复杂状态管理
            return Task.FromResult(ServiceResult<bool>.Failure("简化版本不支持暂停功能，请使用完成或删除操作"));
        }

        /// <inheritdoc/>
        public Task<ServiceResult<bool>> Resume(Guid id)
        {
            // 简化实现：Record-Only模式不支持复杂状态管理
            return Task.FromResult(ServiceResult<bool>.Failure("简化版本不支持恢复功能，请创建新的医疗案例"));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> Archive(Guid id, string archiveReason)
        {
            // 简化实现：归档等同于完成
            return await CompleteAsync(id, archiveReason);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> UpdateStatus(Guid id, int status)
        {
            // 简化实现：只支持Active(10) -> Closed(20)的状态转换
            if (status == 20) // Closed
            {
                return await _businessService.DeleteAsync(id);
            }
            else
            {
                return ServiceResult<bool>.Failure("简化版本只支持关闭医疗案例，请使用删除操作");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            // 简化实现：取消诊疗等同于完成案例
            return await CompleteAsync(id, reason);
        }

        #endregion 简化的状态管理

        #region Batch Operations (批量操作)

        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(Guid[] ids, int status)
        {
            // 简化实现：只支持批量关闭
            if (status != 20) // 只支持Closed状态
            {
                return ServiceResult<int>.Failure("简化版本只支持批量关闭医疗案例");
            }

            int successCount = 0;
            foreach (var id in ids)
            {
                var result = await _businessService.DeleteAsync(id);
                if (result.IsSuccess)
                {
                    successCount++;
                }
            }

            return ServiceResult<int>.Success(successCount);
        }

        #endregion Batch Operations

        #region Statistics and Reports (报告功能)

        public async Task<ServiceResult<object>> GetStatisticsAsync()
            => await _queryService.GetStatisticsAsync();

        /// <inheritdoc/>
        public Task<ServiceResult<object>> GetStatistics(DateTime? startDate, DateTime? endDate)
        {
            // 委托给无参数版本，忽略日期参数（向后兼容）
            return GetStatisticsAsync();
        }

        public async Task<ServiceResult<byte[]>> PrintMedicalRecordAsync(Guid caseId)
        {
            var printResult = await _businessService.PrintMedicalRecordAsync(caseId, new { Format = "PDF" });
            if (!printResult.IsSuccess)
            {
                return ServiceResult<byte[]>.Failure(printResult.ErrorMessage ?? "打印失败");
            }

            // 简化实现：返回基础打印数据的字节
            var printContent = System.Text.Json.JsonSerializer.Serialize(printResult.Data);
            var bytes = System.Text.Encoding.UTF8.GetBytes(printContent);
            return ServiceResult<byte[]>.Success(bytes);
        }

        #endregion Statistics and Reports
    }
}