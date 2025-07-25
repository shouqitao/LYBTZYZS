using AutoMapper;
using LYBT.Common.Enums.Logs;
using LYBT.Common.Enums.Users;
using LYBT.Common.Models;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Models.Dtos;
using System.Text.Json;
using CommonUtil = LYBT.CommonUtils.CommonUtils;

namespace LYBT.Module.Patients.Services {

    /// <summary>
    /// 病人服务实现（业务逻辑层）
    /// 实现软删除策略：患者只能禁用/启用，不能物理删除
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IRecordService _recordService;

        public PatientService(IPatientRepository patientRepository,
            IMapper mapper,
            ILogService logService,
            IRecordService recordService) {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
            _recordService = recordService;
        }

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        public async Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            // 数据验证
            var validation = await ValidatePatientAsync(dto, false);
            if (!validation.IsValid) {
                throw new ArgumentException(string.Join(", ", validation.Errors));
            }

            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinyinCode = CommonUtil.GetPinyinCode(model.Name);
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            // 如果有身份证号，尝试解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IDNumber) && CommonUtil.CheckIdNumber(model.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(model.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
                }
            }

            var result = await _patientRepository.AddAsync(model);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = model.Id,
                    ActionType = ActionType.Create,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"新增患者：{model.Name}",
                    NewValue = JsonSerializer.Serialize(model)
                });
            }

            return result;
        }

        public async Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            var model = await _patientRepository.GetByIdAsync(dto.Id, true); // 管理员更新时包含禁用患者
            if (model == null)
                throw new ArgumentException("病人不存在");

            // 数据验证
            var validation = await ValidatePatientAsync(dto, true);
            if (!validation.IsValid) {
                throw new ArgumentException(string.Join(", ", validation.Errors));
            }

            var oldJson = JsonSerializer.Serialize(model);
            _mapper.Map(dto, model);
            model.PinyinCode = CommonUtil.GetPinyinCode(model.Name);

            // 如果身份证号变了，重新解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IDNumber) && CommonUtil.CheckIdNumber(model.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(model.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
                }
            }

            var result = await _patientRepository.UpdateAsync(model);

            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = model.Id,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"编辑患者：{model.Name}",
                    OldValue = oldJson,
                    NewValue = JsonSerializer.Serialize(dto)
                });
            }

            return result;
        }

        /// <summary>
        /// 根据患者Id获取患者详情
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
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 按条件分页查询患者信息
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetListAsync(query.Keyword, query.Page, query.PageSize, includeDisabled);
            var total = await _patientRepository.GetCountAsync(query.Keyword, includeDisabled);
            return new PagedResultDto<PatientDetailDto> {
                TotalCount = total,
                Items = list.Select(_mapper.Map<PatientDetailDto>).ToList()
            };
        }

        public async Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.EnableAsync(id);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"启用患者：{id}"
                });
            }
            return result;
        }

        public async Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.DisableAsync(id);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"禁用患者：{id}"
                });
            }
            return result;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            var count = await _patientRepository.BatchDisableAsync(ids);
            if (count > 0) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"批量禁用患者：{count}人"
                });
            }
            return count;
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            var count = await _patientRepository.BatchEnableAsync(ids);
            if (count > 0) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"批量启用患者：{count}人"
                });
            }
            return count;
        }

        public async Task<List<PatientDetailDto>> SearchAsync(string keyword, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.SearchAsync(keyword, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 智能搜索患者（精确匹配优先，然后模糊搜索）
        /// 权限控制：禁用的患者仅管理员可查询
        /// </summary>
        public async Task<List<PatientDetailDto>> SmartSearchAsync(string keyword, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var results = new List<PatientModel>();

            // 先进行精确匹配
            var exactResults = await _patientRepository.ExactSearchAsync(keyword, includeDisabled);
            results.AddRange(exactResults);

            // 如果精确匹配没有结果，进行模糊搜索
            if (!results.Any()) {
                var fuzzyResults = await _patientRepository.SearchAsync(keyword, includeDisabled);
                results.AddRange(fuzzyResults);
            } else {
                // 如果有精确匹配，再补充一些模糊搜索结果
                var fuzzyResults = await _patientRepository.SearchAsync(keyword, includeDisabled);
                results.AddRange(fuzzyResults.Where(f => !results.Any(r => r.Id == f.Id)).Take(10));
            }

            return results.Take(20).Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        public async Task<List<PatientDetailDto>> GetForDoctorAsync(Guid doctorId, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetForDoctorAsync(doctorId, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        public async Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName) {
            var result = await _patientRepository.AssignDoctorAsync(patientId, doctorId);
            if (result) {
                await _logService.AddLogAsync(new LogDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = patientId,
                    ActionType = ActionType.Other,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    LogTime = DateTime.Now,
                    Content = $"授权患者{patientId}给医生{doctorId}"
                });
            }
            return result;
        }

        public async Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName) {
            int count = 0;
            foreach (var dto in dtos) {
                try {
                    if (await AddAsync(dto, operatorId, operatorName))
                        count++;
                } catch (Exception) {
                    // 导入失败的记录跳过，继续处理下一条
                    continue;
                }
            }
            return count;
        }

        public async Task<List<PatientDetailDto>> ExportAsync(UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        public async Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId) {
            var records = await _recordService.GetByPatientIdAsync(patientId);
            return records;
        }

        /// <summary>
        /// 快速创建患者（用于快速看诊场景）
        /// </summary>
        public async Task<PatientDetailDto> QuickCreateAsync(QuickPatientCreateDto dto, Guid operatorId, string operatorName) {
            var model = new PatientModel {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber ?? "",
                IDNumber = dto.IDNumber ?? "",
                Address = dto.Address ?? "",
                PinyinCode = CommonUtil.GetPinyinCode(dto.Name),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 如果提供了身份证，尝试解析年龄和出生日期
            if (!string.IsNullOrEmpty(dto.IDNumber) && CommonUtil.CheckIdNumber(dto.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(dto.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
                }
            } else if (dto.Age.HasValue) {
                model.Age = dto.Age.Value;
            }

            await _patientRepository.AddAsync(model);

            // 记录日志
            await _logService.AddLogAsync(new LogDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Patient,
                ObjectId = model.Id,
                ActionType = ActionType.Create,
                OperatorId = operatorId,
                OperatorName = operatorName,
                LogTime = DateTime.Now,
                Content = $"快速创建患者：{model.Name}",
                NewValue = JsonSerializer.Serialize(model)
            });

            return _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 验证患者数据
        /// </summary>
        public async Task<ValidationResult> ValidatePatientAsync(PatientDetailDto dto, bool isUpdate = false) {
            var result = new ValidationResult();

            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name)) {
                result.AddError("患者姓名不能为空");
            }

            // 身份证号码验证
            if (!string.IsNullOrEmpty(dto.IDNumber)) {
                if (!CommonUtil.CheckIdNumber(dto.IDNumber)) {
                    result.AddError("身份证号码格式不正确");
                } else {
                    // 检查重复性
                    var excludeId = isUpdate ? dto.Id : (Guid?)null;
                    if (await _patientRepository.IsIDNumberExistsAsync(dto.IDNumber, excludeId)) {
                        result.AddError("身份证号码已存在");
                    }
                }
            }

            // 手机号验证
            if (!string.IsNullOrEmpty(dto.PhoneNumber)) {
                var excludeId = isUpdate ? dto.Id : (Guid?)null;
                if (await _patientRepository.IsPhoneNumberExistsAsync(dto.PhoneNumber, excludeId)) {
                    result.AddError("手机号码已存在");
                }
            }

            return result;
        }

        public async Task<List<PatientDetailDto>> GetActivePatientsAsync() {
            var patients = await _patientRepository.GetActivePatientsAsync();
            return patients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 从身份证号码中提取出生日期
        /// </summary>
        private DateTime? ExtractBirthDateFromIDNumber(string idNumber) {
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
    }
}