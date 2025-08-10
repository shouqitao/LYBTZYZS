using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者档案管理服务
    /// 负责患者档案的导入导出、合并、标签管理等高级功能
    /// </summary>
    public class PatientArchiveService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly IUnifiedLogService _logService;
        private readonly PatientValidationService _validationService;

        public PatientArchiveService(
            IPatientRepository patientRepository, 
            IMapper mapper, 
            IUnifiedLogService logService,
            PatientValidationService validationService)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
            _validationService = validationService;
        }

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>
        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, true);
            if (patient == null)
            {
                throw new ArgumentException("患者不存在");
            }

            // 这里简化实现，返回基础信息
            return new PatientVisitHistoryDto
            {
                PatientId = patient.Id,
                PatientName = patient.Name,
                TotalVisits = patient.VisitCount,
                LastVisitDate = patient.LastVisitTime,
                FirstVisitDate = patient.CreateTime,
                VisitRecords = new List<VisitRecordDto>()
                // AverageVisitInterval 是计算属性，会自动计算
            };
        }

        /// <summary>
        /// 更新患者过敏史
        /// </summary>
        public async Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, true);
            if (patient == null)
            {
                return false;
            }

            var oldValue = patient.AllergyHistory;
            patient.AllergyHistory = allergyHistory;
            patient.UpdateTime = DateTime.Now;

            var result = await _patientRepository.UpdateAsync(patient);
            if (result != null)
            {
                await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Update,
                    $"更新患者过敏史：{result.Name}",
                    JsonSerializer.Serialize(new { OldValue = oldValue, NewValue = allergyHistory }));
            }

            return result != null;
        }

        /// <summary>
        /// 批量导入患者档案
        /// </summary>
        public async Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName)
        {
            var result = new PatientImportResultDto
            {
                TotalCount = patients.Count,
                ImportBatchId = Guid.NewGuid().ToString()
            };

            foreach (var dto in patients)
            {
                try
                {
                    // 检查重复
                    var duplicateCheck = await CheckForDuplicatesAsync(dto);
                    if (duplicateCheck.HasDuplicates)
                    {
                        result.DuplicateCount++;
                        result.DuplicateRecords.AddRange(duplicateCheck.Messages);
                        continue;
                    }

                    // 创建患者
                    var model = CreatePatientModel(dto);
                    
                    var addResult = await _patientRepository.AddAsync(model);
                    if (addResult != null)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailedCount++;
                        result.FailedRecords.Add($"{dto.Name} - 保存失败");
                    }
                }
                catch (Exception ex)
                {
                    result.FailedCount++;
                    result.FailedRecords.Add($"{dto.Name} - {ex.Message}");
                }
            }

            await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Create,
                $"批量导入患者，成功：{result.SuccessCount}，失败：{result.FailedCount}，重复：{result.DuplicateCount}",
                JsonSerializer.Serialize(result));

            return result;
        }

        /// <summary>
        /// 导出患者档案
        /// </summary>
        public async Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query)
        {
            var patients = await _patientRepository.GetAllAsync();

            return patients.Select(p => new PatientExportDto
            {
                Name = p.Name,
                Gender = p.Gender.ToString(),
                Age = p.Age,
                IdCardNumber = p.IdNumber,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                AllergyHistory = p.AllergyHistory,
                VisitCount = p.VisitCount,
                LastVisitDate = p.LastVisitTime?.ToString("yyyy-MM-dd"),
                CreateTime = p.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();
        }

        /// <summary>
        /// 合并重复患者档案
        /// </summary>
        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName)
        {
            var primary = await _patientRepository.GetByIdAsync(primaryId, true);
            var duplicate = await _patientRepository.GetByIdAsync(duplicateId, true);

            if (primary == null || duplicate == null)
            {
                return false;
            }

            // 更新主患者的就诊次数
            primary.VisitCount += duplicate.VisitCount;
            if (duplicate.LastVisitTime > primary.LastVisitTime)
            {
                primary.LastVisitTime = duplicate.LastVisitTime;
            }

            // 禁用重复患者
            duplicate.Status = CommonStatus.Disabled;
            duplicate.DisableReason = $"与患者{primary.Name}(ID:{primaryId})合并";

            await _patientRepository.UpdateAsync(primary);
            await _patientRepository.UpdateAsync(duplicate);

            await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Update,
                $"合并患者档案：{duplicate.Name} -> {primary.Name}",
                JsonSerializer.Serialize(new { PrimaryId = primaryId, DuplicateId = duplicateId }));

            return true;
        }

        /// <summary>
        /// 获取患者标签（简化实现）
        /// </summary>
        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId)
        {
            await Task.CompletedTask;
            return new List<PatientTagDto>();
        }

        /// <summary>
        /// 设置患者标签（简化实现）
        /// </summary>
        public async Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName)
        {
            await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Update,
                $"设置患者标签",
                JsonSerializer.Serialize(new { PatientId = patientId, Tags = tags }));
            return true;
        }

        #region 私有方法

        /// <summary>
        /// 检查导入数据中的重复项
        /// </summary>
        private async Task<(bool HasDuplicates, List<string> Messages)> CheckForDuplicatesAsync(PatientImportDto dto)
        {
            var messages = new List<string>();
            
            if (!string.IsNullOrEmpty(dto.IdCardNumber))
            {
                if (await _patientRepository.IsIdNumberExistsAsync(dto.IdCardNumber))
                {
                    messages.Add($"{dto.Name} - 身份证号重复：{dto.IdCardNumber}");
                }
            }

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber))
                {
                    messages.Add($"{dto.Name} - 手机号重复：{dto.PhoneNumber}");
                }
            }

            return (messages.Any(), messages);
        }

        /// <summary>
        /// 创建患者模型
        /// </summary>
        private PatientModel CreatePatientModel(PatientImportDto dto)
        {
            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
            model.CreateTime = DateTime.Now;
            model.UpdateTime = DateTime.Now;

            // 处理身份证信息
            _validationService.ProcessIdNumberInfo(model);

            return model;
        }

        /// <summary>
        /// 统一的患者操作日志记录
        /// </summary>
        private async Task LogPatientOperationAsync(Guid operatorId, string operatorName,
            LogActionType actionType, string content, string? parameters = null)
        {
            await _logService.LogUserActionAsync(
                operatorId,
                operatorName,
                actionType,
                "Patients",
                "PatientArchiveManagement",
                content,
                parameters: parameters
            );
        }

        #endregion
    }
}