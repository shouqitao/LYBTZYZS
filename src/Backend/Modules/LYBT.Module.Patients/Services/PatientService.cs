using System.Threading.Tasks;
using System.Linq;
using System;
﻿using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Infrastructure.Logging.Enums;
using LYBT.Models.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using System.Text.Json;

namespace LYBT.Module.Patients.Services {

    /// <summary>
    /// 病人服务实现（业务逻辑层）
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly IUnifiedLogService _logService;

        public PatientService(IPatientRepository patientRepository,
            IMapper mapper,
            IUnifiedLogService logService) {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
        }

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        public async Task<PatientDetailDto?> CreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name)) {
                throw new ArgumentException("患者姓名不能为空");
            }

            // 检查身份证号重复
            if (!string.IsNullOrEmpty(dto.IDNumber)) {
                if (await _patientRepository.IsIdNumberExistsAsync(dto.IDNumber)) {
                    throw new ArgumentException("身份证号已存在");
                }
            }

            // 检查手机号重复
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) {
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber)) {
                    throw new ArgumentException("手机号已存在");
                }
            }

            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
            model.CreateTime = DateTime.Now;
            model.UpdateTime = DateTime.Now;

            // 如果有身份证号，尝试解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IdNumber) && CommonHelper.CheckIdNumber(model.IdNumber)) {
                model.BirthDate = ExtractBirthDateFromIdNumber(model.IdNumber);
                if (model.BirthDate.HasValue) {
                    model.Age = CalculateAge(model.BirthDate.Value);
                }
            }

            var result = await _patientRepository.AddAsync(model);

            if (result) {
                await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Create,
                    $"新增患者档案：{model.Name}", JsonSerializer.Serialize(model));
                    
                // 返回创建的对象
                return _mapper.Map<PatientDetailDto>(model);
            }

            return null;
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<PatientDetailDto?> UpdateAsync(Guid id, PatientDetailDto dto, Guid operatorId, string operatorName) {
            var model = await _patientRepository.GetByIdAsync(id, true); // 管理员更新时包含禁用患者档案
            if (model == null)
                throw new ArgumentException("患者不存在");

            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name)) {
                throw new ArgumentException("患者姓名不能为空");
            }

            // 检查身份证号重复（排除当前患者）
            if (!string.IsNullOrEmpty(dto.IDNumber)) {
                if (await _patientRepository.IsIdNumberExistsAsync(dto.IDNumber, id)) {
                    throw new ArgumentException("身份证号已存在");
                }
            }

            // 检查手机号重复（排除当前患者）
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) {
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber, id)) {
                    throw new ArgumentException("手机号已存在");
                }
            }

            var oldJson = JsonSerializer.Serialize(model);
            _mapper.Map(dto, model);
            model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
            model.UpdateTime = DateTime.Now;

            // 如果身份证号变了，重新解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IdNumber) && CommonHelper.CheckIdNumber(model.IdNumber)) {
                model.BirthDate = ExtractBirthDateFromIdNumber(model.IdNumber);
                if (model.BirthDate.HasValue) {
                    model.Age = CalculateAge(model.BirthDate.Value);
                }
            }

            var result = await _patientRepository.UpdateAsync(model);

            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = model.Id,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"编辑患者档案：{model.Name}",
                    OldValue = oldJson,
                    NewValue = JsonSerializer.Serialize(dto)
                });
                
                return _mapper.Map<PatientDetailDto>(model);
            }

            return null;
        }

        /// <summary>
        /// 根据患者ID获取患者详情
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var model = await _patientRepository.GetByIdAsync(id, includeDisabled);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 获取所有患者列表
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<List<PatientDetailDto>> GetAllAsync(UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetListAsync(null, 1, 1000, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 分页查询患者
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetListAsync(query.Name, query.CurrentPage, query.PageSize, includeDisabled);
            var total = await _patientRepository.GetCountAsync(query.Name, includeDisabled);
            return new PaginatedResult<PatientDetailDto> {
                TotalCount = total,
                Items = list.Select(_mapper.Map<PatientDetailDto>).ToList(),
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.DisableAsync(id);
            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"删除患者：{id}"
                });
            }
            return result;
        }

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName) {
            bool result;
            string action;
            
            if (isActive) {
                result = await _patientRepository.EnableAsync(id);
                action = "启用";
            } else {
                result = await _patientRepository.DisableAsync(id);
                action = "禁用";
            }
            
            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = isActive ? ActionType.Enable : ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"{action}患者：{id}"
                });
            }
            return result;
        }

        /// <summary>
        /// 搜索患者（根据姓名、手机号、身份证号）
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.SearchAsync(keyword, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<List<PatientDetailDto>> GetActivePatientsAsync() {
            var patients = await _patientRepository.GetActivePatientsAsync();
            return patients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber) {
            var model = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber) {
            var model = await _patientRepository.GetByIdNumberAsync(idNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        #region 患者档案管理功能

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>
        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId) {
            var patient = await _patientRepository.GetByIdAsync(patientId, true);
            if (patient == null) {
                throw new ArgumentException("患者不存在");
            }

            // TODO: 从MedicalCase表中获取就诊记录
            // 这里简化实现，返回基础信息
            return new PatientVisitHistoryDto {
                PatientId = patient.Id,
                PatientName = patient.Name,
                TotalVisits = patient.VisitCount,
                LastVisitDate = patient.LastVisitTime,
                FirstVisitDate = patient.CreateTime,
                VisitRecords = new List<VisitRecordDto>(),
                AverageVisitInterval = 0
            };
        }

        /// <summary>
        /// 更新患者过敏史
        /// </summary>
        public async Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName) {
            var patient = await _patientRepository.GetByIdAsync(patientId, true);
            if (patient == null) {
                return false;
            }

            var oldValue = patient.AllergyHistory;
            patient.AllergyHistory = allergyHistory;
            patient.UpdateTime = DateTime.Now;

            var result = await _patientRepository.UpdateAsync(patient);
            if (result) {
                await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Update,
                    $"更新患者过敏史：{patient.Name}",
                    JsonSerializer.Serialize(new { OldValue = oldValue, NewValue = allergyHistory }));
            }

            return result;
        }

        /// <summary>
        /// 批量导入患者档案
        /// </summary>
        public async Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName) {
            var result = new PatientImportResultDto {
                TotalCount = patients.Count,
                ImportBatchId = Guid.NewGuid().ToString()
            };

            foreach (var dto in patients) {
                try {
                    // 检查重复
                    if (!string.IsNullOrEmpty(dto.IdNumber)) {
                        if (await _patientRepository.IsIdNumberExistsAsync(dto.IdNumber)) {
                            result.DuplicateCount++;
                            result.DuplicateRecords.Add($"{dto.Name} - 身份证号重复：{dto.IdNumber}");
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(dto.PhoneNumber)) {
                        if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber)) {
                            result.DuplicateCount++;
                            result.DuplicateRecords.Add($"{dto.Name} - 手机号重复：{dto.PhoneNumber}");
                            continue;
                        }
                    }

                    // 创建患者
                    var model = _mapper.Map<PatientModel>(dto);
                    model.Id = Guid.NewGuid();
                    model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
                    model.CreateTime = DateTime.Now;
                    model.UpdateTime = DateTime.Now;

                    if (await _patientRepository.AddAsync(model)) {
                        result.SuccessCount++;
                    } else {
                        result.FailedCount++;
                        result.FailedRecords.Add($"{dto.Name} - 保存失败");
                    }
                } catch (Exception ex) {
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
        public async Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query) {
            // TODO: 根据查询条件筛选患者
            var patients = await _patientRepository.GetListAsync(null, 1, 10000, query.IncludeInactive);
            
            return patients.Select(p => new PatientExportDto {
                Name = p.Name,
                Gender = p.Gender.ToString(),
                Age = p.Age,
                IdNumber = p.IdNumber,
                PhoneNumber = p.PhoneNumber,
                Address = p.Address,
                AllergyHistory = p.AllergyHistory,
                VisitCount = p.VisitCount,
                LastVisitTime = p.LastVisitTime,
                CreateTime = p.CreateTime
            }).ToList();
        }

        /// <summary>
        /// 合并重复患者档案
        /// </summary>
        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName) {
            var primary = await _patientRepository.GetByIdAsync(primaryId, true);
            var duplicate = await _patientRepository.GetByIdAsync(duplicateId, true);

            if (primary == null || duplicate == null) {
                return false;
            }

            // TODO: 合并就诊记录、处方等关联数据
            // 更新主患者的就诊次数
            primary.VisitCount += duplicate.VisitCount;
            if (duplicate.LastVisitTime > primary.LastVisitTime) {
                primary.LastVisitTime = duplicate.LastVisitTime;
            }

            // 禁用重复患者
            duplicate.Status = PatientStatus.Inactive;
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
        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId) {
            // TODO: 从标签表中获取
            await Task.CompletedTask;
            return new List<PatientTagDto>();
        }

        /// <summary>
        /// 设置患者标签（简化实现）
        /// </summary>
        public async Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName) {
            // TODO: 保存到标签表
            await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Update,
                $"设置患者标签",
                JsonSerializer.Serialize(new { PatientId = patientId, Tags = tags }));
            return true;
        }

        #endregion

        #region 患者查询和统计功能

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query, UserRole currentUserRole) {
            // TODO: 实现复杂查询逻辑
            // 这里简化为基本查询
            var basicQuery = new PatientPagedQueryDto {
                Name = query.Name,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
            return await GetPagedAsync(basicQuery, currentUserRole);
        }

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        public async Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null) {
            var allPatients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, true);
            var now = DateTime.Now;
            var today = DateTime.Today;

            return new PatientStatisticsDto {
                TotalPatients = allPatients.Count,
                ActivePatients = allPatients.Count(p => p.Status == PatientStatus.Active),
                InactivePatients = allPatients.Count(p => p.Status == PatientStatus.Inactive),
                MaleCount = allPatients.Count(p => p.Gender == Gender.Male),
                FemaleCount = allPatients.Count(p => p.Gender == Gender.Female),
                AverageAge = allPatients.Any() ? allPatients.Average(p => p.Age) : 0,
                TotalVisits = allPatients.Sum(p => p.VisitCount),
                AverageVisits = allPatients.Any() ? allPatients.Average(p => p.VisitCount) : 0,
                PatientsWithAllergy = allPatients.Count(p => !string.IsNullOrEmpty(p.AllergyHistory)),
                TodayNewPatients = allPatients.Count(p => p.CreateTime.Date == today),
                MonthNewPatients = allPatients.Count(p => p.CreateTime.Year == now.Year && p.CreateTime.Month == now.Month),
                LostPatients = allPatients.Count(p => p.LastVisitTime.HasValue && 
                    (now - p.LastVisitTime.Value).TotalDays > 180),
                NewPatients = allPatients.Count(p => 
                    (!startDate.HasValue || p.CreateTime >= startDate) &&
                    (!endDate.HasValue || p.CreateTime <= endDate))
            };
        }

        /// <summary>
        /// 获取患者年龄分布统计
        /// </summary>
        public async Task<List<AgeDistributionDto>> GetAgeDistributionAsync() {
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, false);
            var total = patients.Count;

            var ageRanges = new[] {
                new { Min = 0, Max = 18, Range = "0-18岁（儿童）" },
                new { Min = 19, Max = 35, Range = "19-35岁（青年）" },
                new { Min = 36, Max = 50, Range = "36-50岁（中年）" },
                new { Min = 51, Max = 65, Range = "51-65岁（中老年）" },
                new { Min = 66, Max = int.MaxValue, Range = "66岁以上（老年）" }
            };

            return ageRanges.Select(range => {
                var patientsInRange = patients.Where(p => p.Age >= range.Min && p.Age <= range.Max).ToList();
                return new AgeDistributionDto {
                    AgeRange = range.Range,
                    MinAge = range.Min,
                    MaxAge = range.Max == int.MaxValue ? 100 : range.Max,
                    Count = patientsInRange.Count,
                    Percentage = total > 0 ? (double)patientsInRange.Count / total * 100 : 0,
                    MaleCount = patientsInRange.Count(p => p.Gender == Gender.Male),
                    FemaleCount = patientsInRange.Count(p => p.Gender == Gender.Female)
                };
            }).ToList();
        }

        /// <summary>
        /// 获取患者性别分布统计
        /// </summary>
        public async Task<GenderDistributionDto> GetGenderDistributionAsync() {
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, false);
            var total = patients.Count;

            var maleCount = patients.Count(p => p.Gender == Gender.Male);
            var femaleCount = patients.Count(p => p.Gender == Gender.Female);
            var unknownCount = patients.Count(p => p.Gender == Gender.Unknown);

            return new GenderDistributionDto {
                MaleCount = maleCount,
                MalePercentage = total > 0 ? (double)maleCount / total * 100 : 0,
                FemaleCount = femaleCount,
                FemalePercentage = total > 0 ? (double)femaleCount / total * 100 : 0,
                UnknownCount = unknownCount,
                UnknownPercentage = total > 0 ? (double)unknownCount / total * 100 : 0,
                TotalCount = total
            };
        }

        /// <summary>
        /// 获取新增患者趋势
        /// </summary>
        public async Task<List<PatientTrendDto>> GetNewPatientTrendAsync(int months = 12) {
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, true);
            var startDate = DateTime.Now.AddMonths(-months);
            
            var monthlyData = patients
                .Where(p => p.CreateTime >= startDate)
                .GroupBy(p => new { p.CreateTime.Year, p.CreateTime.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => {
                    var monthPatients = g.ToList();
                    return new PatientTrendDto {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MonthName = $"{g.Key.Year}年{g.Key.Month}月",
                        NewPatients = monthPatients.Count,
                        VisitCount = monthPatients.Sum(p => p.VisitCount),
                        MaleCount = monthPatients.Count(p => p.Gender == Gender.Male),
                        FemaleCount = monthPatients.Count(p => p.Gender == Gender.Female),
                        GrowthRate = 0 // TODO: 计算环比增长率
                    };
                }).ToList();

            // 计算环比增长率
            for (int i = 1; i < monthlyData.Count; i++) {
                if (monthlyData[i - 1].NewPatients > 0) {
                    monthlyData[i].GrowthRate = 
                        (double)(monthlyData[i].NewPatients - monthlyData[i - 1].NewPatients) / 
                        monthlyData[i - 1].NewPatients * 100;
                }
            }

            return monthlyData;
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<List<PatientDetailDto>> GetRecentActivePatientsAsync(int days = 30) {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, false);
            var activePatients = patients
                .Where(p => p.LastVisitTime.HasValue && p.LastVisitTime.Value >= cutoffDate)
                .OrderByDescending(p => p.LastVisitTime)
                .ToList();

            return activePatients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 获取流失患者列表
        /// </summary>
        public async Task<List<PatientDetailDto>> GetInactivePatientsAsync(int days = 180) {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, false);
            var inactivePatients = patients
                .Where(p => !p.LastVisitTime.HasValue || p.LastVisitTime.Value < cutoffDate)
                .OrderBy(p => p.LastVisitTime ?? DateTime.MinValue)
                .ToList();

            return inactivePatients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 获取今日新增患者
        /// </summary>
        public async Task<List<PatientDetailDto>> GetTodayNewPatientsAsync() {
            var today = DateTime.Today;
            var patients = await _patientRepository.GetListAsync(null, 1, int.MaxValue, false);
            var todayPatients = patients
                .Where(p => p.CreateTime.Date == today)
                .OrderByDescending(p => p.CreateTime)
                .ToList();

            return todayPatients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 检查患者是否重复
        /// </summary>
        public async Task<List<PatientDetailDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber) {
            var duplicates = new List<PatientModel>();

            if (!string.IsNullOrEmpty(idNumber)) {
                var byIdNumber = await _patientRepository.GetByIdNumberAsync(idNumber);
                if (byIdNumber != null) {
                    duplicates.Add(byIdNumber);
                }
            }

            if (!string.IsNullOrEmpty(phoneNumber)) {
                var byPhone = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
                if (byPhone != null && !duplicates.Any(p => p.Id == byPhone.Id)) {
                    duplicates.Add(byPhone);
                }
            }

            return duplicates.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        #endregion


        /// <summary>
        /// 从身份证号码中提取出生日期
        /// </summary>
        private DateTime? ExtractBirthDateFromIdNumber(string idNumber) {
            if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 18) {
                return null;
            }

            try {
                var year = int.Parse(idNumber.Substring(6, 4));
                var month = int.Parse(idNumber.Substring(10, 2));
                var day = int.Parse(idNumber.Substring(12, 2));
                return new DateTime(year, month, day);
            } catch {
                return null;
            }
        }

        /// <summary>
        /// 计算年龄
        /// </summary>
        private int CalculateAge(DateTime birthDate) {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) {
                age--;
            }
            return age;
        }

        /// <summary>
        /// 统一的患者操作日志记录
        /// </summary>
        private async Task LogPatientOperationAsync(Guid operatorId, string operatorName,
            LogActionType actionType, string content, string? parameters = null) {
            await _logService.LogUserActionAsync(
                operatorId,
                operatorName,
                actionType,
                "Patients",
                "PatientManagement",
                content,
                parameters: parameters
            );
        }
    }
}