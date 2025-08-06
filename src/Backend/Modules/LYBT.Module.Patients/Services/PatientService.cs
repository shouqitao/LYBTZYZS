using AutoMapper;
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