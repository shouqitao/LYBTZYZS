using System;
using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Services.Status
{
    /// <summary>
    /// 鎮ｈ€呯姸鎬佺鐞嗘湇鍔″疄鐜?
    /// UltraThink閲嶆瀯锛氫笓娉ㄤ簬鎮ｈ€呯姸鎬佹帶鍒跺拰绠＄悊鍔熻兘
    /// 浠ｇ爜琛屾暟锛氱害90琛岋紝绗﹀悎500琛屼互涓嬫爣鍑?
    /// </summary>
    public class PatientStatusService : IPatientStatusService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly ILogger<PatientStatusService> _logger;

        public PatientStatusService(
            IPatientRepository patientRepository,
            ILogger<PatientStatusService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 璁剧疆鎮ｈ€呯姸鎬侊紙鍚敤/绂佺敤锛?
        /// </summary>
        public async Task<ServiceResult<bool>> SetPatientStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            try
            {
                bool result;
                string action;

                if (isActive)
                {
                    result = await _patientRepository.EnableAsync(id);
                    action = "鍚敤";                }
                else
                {
                    result = await _patientRepository.DisableAsync(id);                    action = "绂佺敤";                }

                if (result)
                {                    _logger.LogInformation("鎮ｈ€呯姸鎬佸彉鏇?- 鎿嶄綔鑰? {OperatorName} ({OperatorId}), 鎮ｈ€匢D: {PatientId}, 鎿嶄綔: {Action}",                        operatorName, operatorId, id, action);
                }
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "璁剧疆鎮ｈ€呯姸鎬佸け璐? {PatientId}", id);                return ServiceResult<bool>.Failure("设置患者状态失败");            }
        }

        /// <summary>
        /// 鍚敤鎮ｈ€?
        /// </summary>
        public async Task<ServiceResult<bool>> EnablePatientAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)                    return ServiceResult<bool>.Failure("鎮ｈ€呬笉瀛樺湪");                model.Status = CommonStatus.Enabled;

                var result = await _patientRepository.UpdateAsync(model);
                if (result != null)
                {                    _logger.LogInformation("鎮ｈ€呭惎鐢ㄦ垚鍔? {PatientId}", id);                    return ServiceResult<bool>.Success(true);
                }
                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "鍚敤鎮ｈ€呭け璐? {PatientId}", id);                return ServiceResult<bool>.Failure("鍚敤鎮ｈ€呭け璐?");            }
        }

        /// <summary>
        /// 绂佺敤鎮ｈ€?
        /// </summary>
        public async Task<ServiceResult<bool>> DisablePatientAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)                    return ServiceResult<bool>.Failure("鎮ｈ€呬笉瀛樺湪");                model.Status = CommonStatus.Disabled;

                var result = await _patientRepository.UpdateAsync(model);
                if (result != null)
                {                    _logger.LogInformation("鎮ｈ€呯鐢ㄦ垚鍔? {PatientId}", id);                    return ServiceResult<bool>.Success(true);
                }
                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "绂佺敤鎮ｈ€呭け璐? {PatientId}", id);                return ServiceResult<bool>.Failure("绂佺敤鎮ｈ€呭け璐?");
            }
        }
    }
}




