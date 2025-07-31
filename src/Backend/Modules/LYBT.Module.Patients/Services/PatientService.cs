using AutoMapper;
using LYBT.Common.Enums.Logs;
using LYBT.Shared.Models.Enums;
using LYBT.Common.Helpers;
using LYBT.Common.Models;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Models.Patients;
using LYBT.Models.Records;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Records.Interfaces;
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
        private readonly IRecordService _recordService;

        public PatientService(IPatientRepository patientRepository,
            IMapper mapper,
            IUnifiedLogService logService,
            IRecordService recordService) {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logService = logService;
            _recordService = recordService;
        }

        /// <summary>
        /// 新增患者档案档案，并记录操作日志
        /// </summary>
        public async Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            // 三要素匹配验证，防止重复患者档案
            var duplicateCheck = await CheckPatientDuplicateAsync(dto.Name, dto.PhoneNumber, dto.IDNumber);
            if (duplicateCheck.HasDuplicate) {
                throw new ArgumentException($"发现疑似重复患者档案：{duplicateCheck.Message}。如确需创建新患者档案，请联系管理员。");
            }

            // 数据验证
            var validation = await ValidatePatientAsync(dto, false);
            if (!validation.IsValid) {
                throw new ArgumentException(string.Join(", ", validation.Errors));
            }

            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinyinCode = CommonHelper.GetPinyinCode(model.Name);
            model.WuBiCode = CommonHelper.GetWuBiCode(model.Name);
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            // 如果有身份证号，尝试解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IDNumber) && CommonHelper.CheckIdNumber(model.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(model.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
                }
            }

            var result = await _patientRepository.AddAsync(model);

            if (result) {
                await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Create,
                    $"新增患者档案：{model.Name}", JsonSerializer.Serialize(model));
            }

            return result;
        }

        public async Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            var model = await _patientRepository.GetByIdAsync(dto.Id, true); // 管理员更新时包含禁用患者档案
            if (model == null)
                throw new ArgumentException("病人不存在");

            // 数据验证
            var validation = await ValidatePatientAsync(dto, true);
            if (!validation.IsValid) {
                throw new ArgumentException(string.Join(", ", validation.Errors));
            }

            var oldJson = JsonSerializer.Serialize(model);
            _mapper.Map(dto, model);
            model.PinyinCode = CommonHelper.GetPinyinCode(model.Name);

            // 如果身份证号变了，重新解析出生日期和年龄
            if (!string.IsNullOrEmpty(model.IDNumber) && CommonHelper.CheckIdNumber(model.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(model.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
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
            }

            return result;
        }

        /// <summary>
        /// 根据患者档案Id获取患者档案详情
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var model = await _patientRepository.GetByIdAsync(id, includeDisabled);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 获取所有患者档案列表
        /// 权限控制：禁用的患者档案仅管理员可查询
        /// </summary>
        public async Task<List<PatientDetailDto>> GetAllAsync(UserRole currentUserRole) {
            bool includeDisabled = currentUserRole == UserRole.Admin;
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue, includeDisabled);
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 按条件分页查询患者档案信息
        /// 权限控制：禁用的患者档案仅管理员可查询
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
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"启用患者档案：{id}"
                });
            }
            return result;
        }

        public async Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName) {
            var result = await _patientRepository.DisableAsync(id);
            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = id,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"禁用患者档案：{id}"
                });
            }
            return result;
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            var count = await _patientRepository.BatchDisableAsync(ids);
            if (count > 0) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Disable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"批量禁用患者档案：{count}人"
                });
            }
            return count;
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName) {
            var count = await _patientRepository.BatchEnableAsync(ids);
            if (count > 0) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Patient,
                    ObjectId = Guid.Empty,
                    ActionType = ActionType.Enable,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    Content = $"批量启用患者档案：{count}人"
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
        /// 智能搜索患者档案（精确匹配优先，然后模糊搜索）
        /// 权限控制：禁用的患者档案仅管理员可查询
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
        /// 查询或创建患者档案（用于挂号/看诊场景）
        /// 根据姓名和身份证号查询患者档案，如果不存在则创建新档案
        /// </summary>
        public async Task<PatientDetailDto> FindOrCreateAsync(PatientDetailDto dto, Guid operatorId, string operatorName) {
            // 先尝试查询现有患者档案
            PatientModel? existingPatient = null;
            
            // 如果有身份证号，优先按身份证号查询
            if (!string.IsNullOrEmpty(dto.IDNumber)) {
                existingPatient = await _patientRepository.GetByIdNumberAsync(dto.IDNumber);
            }
            
            // 如果没找到，再按姓名+电话查询
            if (existingPatient == null && !string.IsNullOrEmpty(dto.PhoneNumber)) {
                var patientsByName = await _patientRepository.GetByNameAsync(dto.Name);
                existingPatient = patientsByName.FirstOrDefault(p => p.PhoneNumber == dto.PhoneNumber);
            }
            
            if (existingPatient != null) {
                // 找到现有患者档案，返回现有档案
                return _mapper.Map<PatientDetailDto>(existingPatient);
            }
            
            // 没有找到现有档案，创建新档案
            var model = new PatientModel {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber ?? "",
                IDNumber = dto.IDNumber ?? "",
                Address = dto.Address ?? "",
                PinyinCode = CommonHelper.GetPinyinCode(dto.Name),
                WuBiCode = CommonHelper.GetWuBiCode(dto.Name),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 如果提供了身份证，尝试解析年龄和出生日期
            if (!string.IsNullOrEmpty(dto.IDNumber) && CommonHelper.CheckIdNumber(dto.IDNumber)) {
                model.DateOfBirth = ExtractBirthDateFromIDNumber(dto.IDNumber);
                if (model.DateOfBirth.HasValue) {
                    model.Age = CalculateAge(model.DateOfBirth.Value);
                }
            } else if (dto.Age > 0) {
                model.Age = dto.Age;
            }

            await _patientRepository.AddAsync(model);

            // 记录日志
            await LogPatientOperationAsync(operatorId, operatorName, LogActionType.Create,
                $"查询或创建患者档案：{model.Name}", JsonSerializer.Serialize(model));

            return _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 验证患者档案数据
        /// </summary>
        public async Task<ValidationResult> ValidatePatientAsync(PatientDetailDto dto, bool isUpdate = false) {
            var result = new ValidationResult();

            // 基础验证
            if (string.IsNullOrWhiteSpace(dto.Name)) {
                result.AddError("患者档案姓名不能为空");
            }

            // 身份证号码验证
            if (!string.IsNullOrEmpty(dto.IDNumber)) {
                if (!CommonHelper.CheckIdNumber(dto.IDNumber)) {
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

        /// <summary>
        /// 三要素匹配检查，防止重复患者档案
        /// </summary>
        private async Task<PatientDuplicateCheckResult> CheckPatientDuplicateAsync(string name, string phoneNumber, string idNumber) {
            var result = new PatientDuplicateCheckResult { HasDuplicate = false };
            var duplicateMessages = new List<string>();

            // 1. 身份证号完全匹配检查
            if (!string.IsNullOrEmpty(idNumber)) {
                var idMatches = await _patientRepository.GetPatientsByIdNumberAsync(idNumber);
                if (idMatches.Any()) {
                    duplicateMessages.Add($"身份证号 {idNumber} 已存在");
                    result.HasDuplicate = true;
                    result.MatchType |= PatientMatchType.IdNumber;
                    result.ExistingPatients.AddRange(idMatches);
                }
            }

            // 2. 姓名+手机号匹配检查
            if (!string.IsNullOrEmpty(phoneNumber)) {
                var namePhoneMatches = await _patientRepository.GetPatientsByNameAndPhoneAsync(name, phoneNumber);
                if (namePhoneMatches.Any()) {
                    duplicateMessages.Add($"姓名 {name} + 手机号 {phoneNumber} 组合已存在");
                    result.HasDuplicate = true;
                    result.MatchType |= PatientMatchType.NameAndPhone;
                    result.ExistingPatients.AddRange(namePhoneMatches.Where(p => !result.ExistingPatients.Any(ep => ep.Id == p.Id)));
                }
            }

            // 3. 高相似度姓名检查（考虑同音字、形近字等情况）
            var similarNameMatches = await _patientRepository.GetPatientsBySimilarNameAsync(name);
            if (similarNameMatches.Any()) {
                duplicateMessages.Add($"发现相似姓名患者档案：{string.Join(", ", similarNameMatches.Select(p => p.Name))}");
                result.HasSimilar = true;
                result.MatchType |= PatientMatchType.SimilarName;
                result.SimilarPatients.AddRange(similarNameMatches.Where(p => !result.ExistingPatients.Any(ep => ep.Id == p.Id)));
            }

            result.Message = string.Join("；", duplicateMessages);
            return result;
        }

        /// <summary>
        /// 统一的患者档案操作日志记录
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

    /// <summary>
    /// 患者档案重复检查结果
    /// </summary>
    public class PatientDuplicateCheckResult {

        /// <summary>
        /// 是否有重复
        /// </summary>
        public bool HasDuplicate { get; set; }

        /// <summary>
        /// 是否有相似
        /// </summary>
        public bool HasSimilar { get; set; }

        /// <summary>
        /// 匹配类型
        /// </summary>
        public PatientMatchType MatchType { get; set; }

        /// <summary>
        /// 提示消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 已存在的患者档案（强匹配）
        /// </summary>
        public List<PatientModel> ExistingPatients { get; set; } = new();

        /// <summary>
        /// 相似的患者档案（弱匹配）
        /// </summary>
        public List<PatientModel> SimilarPatients { get; set; } = new();
    }

    /// <summary>
    /// 患者档案匹配类型
    /// </summary>
    [Flags]
    public enum PatientMatchType {

        /// <summary>
        /// 无匹配
        /// </summary>
        None = 0,

        /// <summary>
        /// 身份证号匹配
        /// </summary>
        IdNumber = 1,

        /// <summary>
        /// 姓名+手机号匹配
        /// </summary>
        NameAndPhone = 2,

        /// <summary>
        /// 相似姓名匹配
        /// </summary>
        SimilarName = 4
    }
}