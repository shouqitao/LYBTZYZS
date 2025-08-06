using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Doctors.Services {

    /// <summary>
    /// 医生业务服务实现类（简化版 - 仅基础功能）
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

            var dto = _mapper.Map<DoctorDetailDto>(model);
            
            // 获取统计信息（可选）
            // TODO: 实现统计逻辑
            dto.TodayPatientCount = 0;
            dto.TotalPatientCount = 0;
            
            return dto;
        }

        public async Task<List<DoctorDto>> GetAllAsync(UserRole currentUserRole) {
            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var models = await _doctorRepository.GetAllAsync(includeDisabled);
            return _mapper.Map<List<DoctorDto>>(models);
        }

        public async Task<PaginatedResult<DoctorDto>> GetPagedAsync(DoctorQueryDto query, UserRole currentUserRole) {
            query ??= new DoctorQueryDto();
            
            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var models = await _doctorRepository.GetAllAsync(includeDisabled);
            
            // 应用搜索过滤
            if (!string.IsNullOrWhiteSpace(query.SearchKeyword)) {
                var keyword = query.SearchKeyword.ToLower();
                models = models.Where(m =>
                    m.Name.ToLower().Contains(keyword) ||
                    m.Specialty.ToLower().Contains(keyword) ||
                    m.LicenseNumber.ToLower().Contains(keyword) ||
                    (m.PinYinCode != null && m.PinYinCode.ToLower().Contains(keyword))
                ).ToList();
            }

            // 按状态过滤
            if (query.Status.HasValue) {
                models = models.Where(m => m.Status == query.Status.Value).ToList();
            }

            // 排序
            models = query.OrderBy?.ToLower() switch {
                "name" => query.IsAscending ? models.OrderBy(m => m.Name).ToList() : models.OrderByDescending(m => m.Name).ToList(),
                "registrationfee" => query.IsAscending ? models.OrderBy(m => m.RegistrationFee).ToList() : models.OrderByDescending(m => m.RegistrationFee).ToList(),
                "createtime" => query.IsAscending ? models.OrderBy(m => m.CreateTime).ToList() : models.OrderByDescending(m => m.CreateTime).ToList(),
                _ => models.OrderByDescending(m => m.CreateTime).ToList()
            };

            // 分页
            var total = models.Count;
            var pagedModels = models
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtos = _mapper.Map<List<DoctorDto>>(pagedModels);
            return new PaginatedResult<DoctorDto>(dtos, total, query.CurrentPage, query.PageSize);
        }

        public async Task<DoctorDetailDto?> CreateAsync(DoctorCreateDto dto, Guid operatorId, string operatorName) {
            if (dto == null) {
                throw new ArgumentNullException(nameof(dto));
            }

            // 验证用户是否存在
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null) {
                throw new InvalidOperationException($"用户ID {dto.UserId} 不存在");
            }

            // 检查该用户是否已经是医生
            var existingDoctor = await _doctorRepository.GetByUserIdAsync(dto.UserId);
            if (existingDoctor != null) {
                throw new InvalidOperationException($"用户 {user.RealName} 已经是医生");
            }

            // 创建医生实体
            var model = new DoctorModel {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                User = user,
                Name = dto.Name,
                Specialty = dto.Specialty,
                RegistrationFee = dto.RegistrationFee,
                LicenseNumber = dto.LicenseNumber,
                ContactNumber = dto.ContactNumber,
                Introduction = dto.Introduction,
                PinYinCode = null, // TODO: 实现拼音码生成
                Status = DoctorStatus.Active,
                CreateTime = DateTime.Now
            };

            await _doctorRepository.AddAsync(model);
            return _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<DoctorDetailDto?> UpdateAsync(Guid id, DoctorUpdateDto dto, Guid operatorId, string operatorName) {
            if (dto == null) {
                throw new ArgumentNullException(nameof(dto));
            }

            var model = await _doctorRepository.GetByIdAsync(id, true);
            if (model == null) {
                return null;
            }

            // 更新字段
            model.Name = dto.Name;
            model.Specialty = dto.Specialty;
            model.RegistrationFee = dto.RegistrationFee;
            model.LicenseNumber = dto.LicenseNumber;
            model.ContactNumber = dto.ContactNumber;
            model.Introduction = dto.Introduction;
            model.Status = dto.Status;
            model.PinYinCode = null; // TODO: 实现拼音码生成
            model.UpdateTime = DateTime.Now;

            await _doctorRepository.UpdateAsync(model);
            return _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            var model = await _doctorRepository.GetByIdAsync(id, true);
            if (model == null) {
                return false;
            }

            // 软删除：将状态设置为已删除
            model.Status = DoctorStatus.Deleted;
            model.UpdateTime = DateTime.Now;
            await _doctorRepository.UpdateAsync(model);
            return true;
        }

        public async Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId, UserRole currentUserRole) {
            if (userId == Guid.Empty) {
                throw new ArgumentException("用户ID不能为空");
            }

            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var model = await _doctorRepository.GetByUserIdAsync(userId);
            
            if (model == null) {
                return null;
            }

            // 如果不能查看禁用医生，检查状态
            if (!includeDisabled && model.Status != DoctorStatus.Active) {
                return null;
            }

            return _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<bool> SetStatusAsync(Guid id, DoctorStatus status, Guid operatorId, string operatorName) {
            var model = await _doctorRepository.GetByIdAsync(id, true);
            if (model == null) {
                return false;
            }

            model.Status = status;
            model.UpdateTime = DateTime.Now;
            await _doctorRepository.UpdateAsync(model);
            return true;
        }

        public async Task<List<DoctorDto>> SearchAsync(string keyword, UserRole currentUserRole) {
            if (string.IsNullOrWhiteSpace(keyword)) {
                return new List<DoctorDto>();
            }

            var includeDisabled = CanViewDisabledDoctors(currentUserRole);
            var models = await _doctorRepository.GetAllAsync(includeDisabled);
            
            keyword = keyword.ToLower();
            var filtered = models.Where(m =>
                m.Name.ToLower().Contains(keyword) ||
                m.Specialty.ToLower().Contains(keyword) ||
                m.LicenseNumber.ToLower().Contains(keyword) ||
                (m.PinYinCode != null && m.PinYinCode.ToLower().Contains(keyword))
            ).Take(20).ToList();

            return _mapper.Map<List<DoctorDto>>(filtered);
        }

        public async Task<List<DoctorDto>> GetAvailableDoctorsAsync() {
            var models = await _doctorRepository.GetAllAsync(false);
            var availableDoctors = models.Where(m => m.Status == DoctorStatus.Active).ToList();
            return _mapper.Map<List<DoctorDto>>(availableDoctors);
        }

        #region 休息时间管理

        /// <summary>
        /// 设置医生休息状态（某天不出诊）
        /// </summary>
        public async Task<bool> SetDoctorRestAsync(Guid doctorId, DateTime date, bool isRest, Guid operatorId, string operatorName) {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId, true);
            if (doctor == null) {
                return false;
            }
            
            // TODO: 保存到休息记录表
            // 暂时使用内存存储或缓存
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// 获取医生休息记录
        /// </summary>
        public async Task<List<DoctorRestRecordDto>> GetDoctorRestRecordsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null) {
            // TODO: 从休息记录表获取数据
            var records = new List<DoctorRestRecordDto>();
            await Task.CompletedTask;
            return records;
        }

        /// <summary>
        /// 检查医生是否在某天休息
        /// </summary>
        public async Task<bool> IsDoctorRestingAsync(Guid doctorId, DateTime date) {
            // TODO: 从休息记录表查询
            await Task.CompletedTask;
            return false; // 默认不休息
        }

        /// <summary>
        /// 获取某天出诊的医生列表
        /// </summary>
        public async Task<List<DoctorDto>> GetAvailableDoctorsByDateAsync(DateTime date) {
            var allDoctors = await GetAvailableDoctorsAsync();
            var availableDoctors = new List<DoctorDto>();
            
            foreach (var doctor in allDoctors) {
                var isResting = await IsDoctorRestingAsync(doctor.Id, date);
                if (!isResting) {
                    availableDoctors.Add(doctor);
                }
            }
            
            return availableDoctors;
        }

        #endregion

        #region 简化的专长信息

        /// <summary>
        /// 更新医生基本信息（包含专长）
        /// </summary>
        public async Task<bool> UpdateDoctorInfoAsync(Guid doctorId, DoctorInfoUpdateDto info, Guid operatorId, string operatorName) {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId, true);
            if (doctor == null) {
                return false;
            }
            
            doctor.Specialty = info.Specialty;
            doctor.Title = info.Title;
            doctor.Introduction = info.Introduction;
            doctor.UpdateTime = DateTime.Now;
            doctor.LastOperatorId = operatorId;
            doctor.LastOperatorName = operatorName;
            
            await _doctorRepository.UpdateAsync(doctor);
            return true;
        }



        #endregion
    }
}