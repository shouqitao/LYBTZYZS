using AutoMapper;
using LYBT.Common.Enums.Users;
using LYBT.Common.Helpers;
using LYBT.Common.Models;
using LYBT.Common.Responses;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Users.Interfaces;

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

        public async Task<ApiResponse<DoctorDetailDto>> GetByIdAsync(Guid id, UserRole currentUserRole) {
            try {
                if (id == Guid.Empty) {
                    return ApiResponse<DoctorDetailDto>.Fail("医生ID不能为空");
                }

                var includeDisabled = CanViewDisabledDoctors(currentUserRole);
                var model = await _doctorRepository.GetByIdAsync(id, includeDisabled);
                if (model == null) {
                    return ApiResponse<DoctorDetailDto>.Fail("医生不存在");
                }

                var dto = _mapper.Map<DoctorDetailDto>(model);
                return ApiResponse<DoctorDetailDto>.Success(dto);
            } catch (Exception ex) {
                return ApiResponse<DoctorDetailDto>.Fail($"获取医生详情失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<DoctorDetailDto>> GetByUserIdAsync(Guid userId, UserRole currentUserRole) {
            try {
                if (userId == Guid.Empty) {
                    return ApiResponse<DoctorDetailDto>.Fail("用户ID不能为空");
                }

                var includeDisabled = CanViewDisabledDoctors(currentUserRole);
                var model = await _doctorRepository.GetByUserIdAsync(userId, includeDisabled);
                if (model == null) {
                    return ApiResponse<DoctorDetailDto>.Fail("该用户未关联医生档案");
                }

                var dto = _mapper.Map<DoctorDetailDto>(model);
                return ApiResponse<DoctorDetailDto>.Success(dto);
            } catch (Exception ex) {
                return ApiResponse<DoctorDetailDto>.Fail($"获取医生详情失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoctorDto>>> SearchAsync(string keyword, UserRole currentUserRole) {
            try {
                var includeDisabled = CanViewDisabledDoctors(currentUserRole);
                var models = await _doctorRepository.SearchAsync(keyword ?? string.Empty, includeDisabled);
                var dtos = _mapper.Map<List<DoctorDto>>(models);
                return ApiResponse<List<DoctorDto>>.Success(dtos);
            } catch (Exception ex) {
                return ApiResponse<List<DoctorDto>>.Fail($"搜索医生失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<PagedResultDto<DoctorDto>>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole) {
            try {
                // 参数验证
                if (query.Page < 1)
                    query.Page = 1;
                if (query.PageSize < 1 || query.PageSize > 100)
                    query.PageSize = 20;

                var includeDisabled = CanViewDisabledDoctors(currentUserRole);
                var (models, total) = await _doctorRepository.GetPagedAsync(query, includeDisabled);
                var dtos = _mapper.Map<List<DoctorDto>>(models);

                var result = new PagedResultDto<DoctorDto> {
                    TotalCount = total,
                    Items = dtos
                };

                return ApiResponse<PagedResultDto<DoctorDto>>.Success(result);
            } catch (Exception ex) {
                return ApiResponse<PagedResultDto<DoctorDto>>.Fail($"获取医生列表失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> AddAsync(DoctorDetailDto dto, UserRole operatorRole) {
            try {
                // 权限验证：只有管理员可以创建医生档案
                if (operatorRole != UserRole.Admin) {
                    return ApiResponse<bool>.Fail("只有管理员可以创建医生档案");
                }

                // 参数验证
                if (dto.UserId == Guid.Empty) {
                    return ApiResponse<bool>.Fail("关联用户ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(dto.Specialty)) {
                    return ApiResponse<bool>.Fail("专科不能为空");
                }

                // 检查用户是否存在并且是医生角色
                var user = await _userRepository.GetByIdAsync(dto.UserId, true);
                if (user == null) {
                    return ApiResponse<bool>.Fail("关联的用户不存在，请先创建用户");
                }

                // 验证用户是否具有医生角色
                if (user.Role != UserRole.DiagnosingDoctor) {
                    return ApiResponse<bool>.Fail("只能为具有医生角色的用户创建医生档案");
                }

                // 检查用户是否已关联医生档案
                var existingDoctor = await _doctorRepository.GetByUserIdAsync(dto.UserId, true);
                if (existingDoctor != null) {
                    return ApiResponse<bool>.Fail("该用户已关联医生档案，请勿重复创建");
                }

                // 创建医生实体
                var model = _mapper.Map<DoctorModel>(dto);
                model.Id = Guid.NewGuid();
                model.User = user;
                model.CreatedTime = DateTime.Now;

                // 生成拼音码
                model.PinyinCode = CommonHelper.GetPinyinCode(user.RealName);

                var result = await _doctorRepository.AddAsync(model);
                var message = result ? "医生档案创建成功" : "医生档案创建失败";

                return result
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            } catch (Exception ex) {
                return ApiResponse<bool>.Fail($"创建医生档案失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateAsync(DoctorDetailDto dto, UserRole operatorRole, Guid operatorUserId) {
            try {
                // 参数验证
                if (dto.Id == Guid.Empty) {
                    return ApiResponse<bool>.Fail("医生ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(dto.Specialty)) {
                    return ApiResponse<bool>.Fail("专科不能为空");
                }

                // 获取现有医生信息
                var model = await _doctorRepository.GetByIdAsync(dto.Id, true);
                if (model == null) {
                    return ApiResponse<bool>.Fail("医生不存在");
                }

                // 权限验证：管理员可以修改任何医生档案，医生只能修改自己的档案
                if (operatorRole != UserRole.Admin && operatorRole != UserRole.DiagnosingDoctor) {
                    return ApiResponse<bool>.Fail("权限不足，无法修改医生档案");
                }

                if (operatorRole == UserRole.DiagnosingDoctor && model.UserId != operatorUserId) {
                    return ApiResponse<bool>.Fail("医生只能修改自己的档案");
                }

                // 更新医生字段（不更新UserId、User等关键字段）
                model.Gender = dto.Gender;
                model.Birthday = dto.Birthday;
                model.Title = dto.Title;
                model.LicenseNumber = dto.LicenseNumber;
                model.Specialty = dto.Specialty;
                model.Status = dto.Status;
                model.WorkStatus = dto.WorkStatus;
                model.Remark = dto.Remark;
                model.ContactNumber = dto.ContactNumber;

                // 更新拼音码
                model.PinyinCode = CommonHelper.GetPinyinCode(model.User.RealName);

                var result = await _doctorRepository.UpdateAsync(model);
                var message = result ? "医生信息更新成功" : "医生信息更新失败";

                return result
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            } catch (Exception ex) {
                return ApiResponse<bool>.Fail($"更新医生信息失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DisableAsync(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return ApiResponse<bool>.Fail("医生ID不能为空");
                }

                var result = await _doctorRepository.DisableAsync(id);
                var message = result ? "医生已禁用" : "禁用医生失败";

                return result
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            } catch (Exception ex) {
                return ApiResponse<bool>.Fail($"禁用医生失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> EnableAsync(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return ApiResponse<bool>.Fail("医生ID不能为空");
                }

                var result = await _doctorRepository.EnableAsync(id);
                var message = result ? "医生已启用" : "启用医生失败";

                return result
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            } catch (Exception ex) {
                return ApiResponse<bool>.Fail($"启用医生失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> BatchDisableAsync(List<Guid> ids) {
            try {
                if (ids == null || ids.Count == 0) {
                    return ApiResponse<int>.Fail("请选择要禁用的医生");
                }

                var count = await _doctorRepository.BatchDisableAsync(ids);
                return ApiResponse<int>.Success(count, $"成功禁用 {count} 名医生");
            } catch (Exception ex) {
                return ApiResponse<int>.Fail($"批量禁用医生失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> BatchEnableAsync(List<Guid> ids) {
            try {
                if (ids == null || ids.Count == 0) {
                    return ApiResponse<int>.Fail("请选择要启用的医生");
                }

                var count = await _doctorRepository.BatchEnableAsync(ids);
                return ApiResponse<int>.Success(count, $"成功启用 {count} 名医生");
            } catch (Exception ex) {
                return ApiResponse<int>.Fail($"批量启用医生失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<List<DoctorDto>>> GetActiveDoctorsAsync() {
            try {
                var models = await _doctorRepository.GetActiveDoctorsAsync();
                var dtos = _mapper.Map<List<DoctorDto>>(models);
                return ApiResponse<List<DoctorDto>>.Success(dtos);
            } catch (Exception ex) {
                return ApiResponse<List<DoctorDto>>.Fail($"获取在职医生列表失败：{ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> IsUserLinkedToDoctorAsync(Guid userId) {
            try {
                if (userId == Guid.Empty) {
                    return ApiResponse<bool>.Fail("用户ID不能为空");
                }

                var doctor = await _doctorRepository.GetByUserIdAsync(userId, true);
                return ApiResponse<bool>.Success(doctor != null);
            } catch (Exception ex) {
                return ApiResponse<bool>.Fail($"检查用户关联状态失败：{ex.Message}");
            }
        }
    }
}