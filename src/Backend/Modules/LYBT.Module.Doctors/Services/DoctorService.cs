using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using System.Text.RegularExpressions;

namespace LYBT.Module.Doctors.Services {

    /// <summary>
    /// 医生业务服务实现类
    /// </summary>
    public class DoctorService : IDoctorService {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public DoctorService(
            IDoctorRepository doctorRepository,
            IUserRepository userRepository,
            IMapper mapper) {
            _doctorRepository = doctorRepository;
            _userRepository = userRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 检查当前用户是否可以查看禁用医生
        /// </summary>
        private bool CanViewDisabledDoctors(UserRole userRole) {
            return userRole == UserRole.Admin;
        }

        public async Task<DoctorDetailDto?> GetByIdAsync(Guid id, UserRole currentUserRole) {
            if (id == Guid.Empty) {
                throw new ArgumentException("医生ID不能为空");
            }

            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var model = await _doctorRepository.GetByIdAsync(id, includeDisabled);
            if (model == null) {
                return null;
            }

            return _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId, UserRole currentUserRole) {
            if (userId == Guid.Empty) {
                throw new ArgumentException("用户ID不能为空");
            }

            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var model = await _doctorRepository.GetByUserIdAsync(userId, includeDisabled);
            if (model == null) {
                return null;
            }

            return _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<List<DoctorDto>> SearchAsync(string keyword, UserRole currentUserRole) {
            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var models = await _doctorRepository.SearchAsync(keyword ?? string.Empty, includeDisabled);
            return _mapper.Map<List<DoctorDto>>(models);
        }

        public async Task<PaginatedResult<DoctorDto>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole) {
            // 参数验证
            if (query.Page < 1)
                query.Page = 1;
            if (query.PageSize < 1 || query.PageSize > 100)
                query.PageSize = 20;

            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var (models, total) = await _doctorRepository.GetPagedAsync(query, includeDisabled);
            var dtos = _mapper.Map<List<DoctorDto>>(models);

            return new PaginatedResult<DoctorDto>(dtos, total, query.Page, query.PageSize);
        }

        public async Task<bool> AddAsync(DoctorDetailDto dto, UserRole operatorRole) {
            // 权限验证：只有管理员可以创建医生档案
            if (operatorRole != UserRole.Admin) {
                throw new UnauthorizedAccessException("只有管理员可以创建医生档案");
            }

            // 参数验证
            if (dto.UserId == Guid.Empty) {
                throw new ArgumentException("关联用户ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Specialty)) {
                throw new ArgumentException("专科不能为空");
            }

            // 1. 检查用户是否存在
            var user = await _userRepository.GetByIdAsync(dto.UserId, true);
            if (user == null) {
                throw new InvalidOperationException("选择的用户不存在，请重新选择");
            }

            // 2. 判断用户角色是否是医生
            if (user.Role != UserRole.DiagnosingDoctor) {
                throw new InvalidOperationException($"该用户不是医生账户，当前角色：{user.Role}，无法创建医生档案");
            }

            // 3. 检查该医生用户是否已创建过医生档案
            var existingDoctor = await _doctorRepository.GetByUserIdAsync(dto.UserId, true);
            if (existingDoctor != null) {
                throw new InvalidOperationException($"该医生用户（{user.RealName}）已存在医生档案，请勿重复创建");
            }

            // 4. 检查身份证号码是否已存在
            if (!string.IsNullOrWhiteSpace(dto.IdNumber)) {
                // 验证身份证格式
                if (!IsValidIdNumber(dto.IdNumber)) {
                    throw new ArgumentException("身份证号码格式不正确");
                }

                // 检查身份证号码重复
                if (await _doctorRepository.IsIdNumberExistsAsync(dto.IdNumber)) {
                    throw new InvalidOperationException($"身份证号码 {dto.IdNumber} 已存在，无法创建重复的医生档案");
                }
            }

            // 创建医生实体
            var model = _mapper.Map<DoctorModel>(dto);
            model.Id = Guid.NewGuid();
            model.User = user;
            model.CreateTime = DateTime.Now;

            // 生成拼音码
            model.PinYinCode = CommonHelper.GetPinyinCode(user.RealName);

            return await _doctorRepository.AddAsync(model);
        }

        public async Task<bool> UpdateAsync(DoctorDetailDto dto, UserRole operatorRole, Guid operatorUserId) {
            // 参数验证
            if (dto.Id == Guid.Empty) {
                throw new ArgumentException("医生ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.Specialty)) {
                throw new ArgumentException("专科不能为空");
            }

            // 获取现有医生信息
            var model = await _doctorRepository.GetByIdAsync(dto.Id, true);
            if (model == null) {
                throw new InvalidOperationException("医生不存在");
            }

            // 权限验证：管理员可以修改任何医生档案，医生只能修改自己的档案
            if (operatorRole != UserRole.Admin && operatorRole != UserRole.DiagnosingDoctor) {
                throw new UnauthorizedAccessException("权限不足，无法修改医生档案");
            }

            if (operatorRole == UserRole.DiagnosingDoctor && model.UserId != operatorUserId) {
                throw new UnauthorizedAccessException("医生只能修改自己的档案");
            }

            // 检查身份证号码（如果要更新）
            if (!string.IsNullOrWhiteSpace(dto.IdNumber) && dto.IdNumber != model.IdNumber) {
                // 验证身份证格式
                if (!IsValidIdNumber(dto.IdNumber)) {
                    throw new ArgumentException("身份证号码格式不正确");
                }

                // 检查身份证号码重复（排除当前医生）
                if (await _doctorRepository.IsIdNumberExistsAsync(dto.IdNumber, dto.Id)) {
                    throw new InvalidOperationException($"身份证号码 {dto.IdNumber} 已存在，无法更新");
                }
            }

            // 更新医生字段（不更新UserId、User等关键字段）
            model.Gender = dto.Gender;
            model.Birthday = dto.Birthday;
            model.Title = dto.Title;
            model.LicenseNumber = dto.LicenseNumber;
            model.IdNumber = dto.IdNumber;
            model.Specialty = dto.Specialty;
            model.Status = dto.Status;
            model.WorkStatus = dto.WorkStatus;
            model.Remark = dto.Remark;
            model.ContactNumber = dto.ContactNumber;

            // 更新拼音码
            model.PinYinCode = CommonHelper.GetPinyinCode(model.User.RealName);

            return await _doctorRepository.UpdateAsync(model);
        }

        public async Task<bool> DisableAsync(Guid id) {
            if (id == Guid.Empty) {
                throw new ArgumentException("医生ID不能为空");
            }

            return await _doctorRepository.DisableAsync(id);
        }

        public async Task<bool> EnableAsync(Guid id) {
            if (id == Guid.Empty) {
                throw new ArgumentException("医生ID不能为空");
            }

            return await _doctorRepository.EnableAsync(id);
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            if (ids == null || ids.Count == 0) {
                throw new ArgumentException("请选择要禁用的医生");
            }

            return await _doctorRepository.BatchDisableAsync(ids);
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            if (ids == null || ids.Count == 0) {
                throw new ArgumentException("请选择要启用的医生");
            }

            return await _doctorRepository.BatchEnableAsync(ids);
        }

        public async Task<List<DoctorDto>> GetActiveDoctorsAsync() {
            var models = await _doctorRepository.GetActiveDoctorsAsync();
            return _mapper.Map<List<DoctorDto>>(models);
        }

        public async Task<bool> IsUserLinkedToDoctorAsync(Guid userId) {
            if (userId == Guid.Empty) {
                throw new ArgumentException("用户ID不能为空");
            }

            var doctor = await _doctorRepository.GetByUserIdAsync(userId, true);
            return doctor != null;
        }

        /// <summary>
        /// 验证身份证号码格式
        /// </summary>
        /// <param name="idNumber">身份证号码</param>
        /// <returns>是否有效</returns>
        private static bool IsValidIdNumber(string idNumber) {
            if (string.IsNullOrWhiteSpace(idNumber)) {
                return false;
            }

            // 18位身份证号码正则表达式
            var regex = new Regex(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$");
            if (!regex.IsMatch(idNumber)) {
                return false;
            }

            // 验证校验位
            var weightFactors = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            var checkCodes = new char[] { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

            var sum = 0;
            for (int i = 0; i < 17; i++) {
                sum += int.Parse(idNumber[i].ToString()) * weightFactors[i];
            }

            var checkCode = checkCodes[sum % 11];
            return char.ToUpper(idNumber[17]) == checkCode;
        }
    }
}