using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Common.Helpers;
using LYBT.Common.Models;
using LYBT.Models.Doctors;
using LYBT.Module.Users.Models;
using LYBT.Common.Enums.Users;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using System.Collections.Generic;

namespace LYBT.Module.Doctors.Services {

    /// <summary>
    /// 医生业务服务实现类，实现医生业务逻辑
    /// </summary>
    public class DoctorService : IDoctorService {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入医生仓储与映射器
        /// </summary>
        public DoctorService(IDoctorRepository doctorRepository, IMapper mapper) {
            _doctorRepository = doctorRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取医生详情
        /// </summary>
        public async Task<DoctorDetailDto?> GetByIdAsync(Guid id) {
            var model = await _doctorRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId) {
            var model = await _doctorRepository.GetByUserIdAsync(userId);
            return model == null ? null : _mapper.Map<DoctorDetailDto>(model);
        }

        /// <summary>
        /// 关键词搜索
        /// </summary>
        public async Task<List<DoctorDto>> SearchAsync(string keyword) {
            var list = await _doctorRepository.SearchAsync(keyword);
            return _mapper.Map<List<DoctorDto>>(list);
        }

        public async Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query) {
            var (models, total) = await _doctorRepository.GetPagedAsync(query);
            return new PagedResultDto<DoctorDto> {
                TotalCount = total,
                Items = _mapper.Map<List<DoctorDto>>(models)
            };
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        public async Task<bool> AddAsync(DoctorCreateDto doctorCreateDto) {
            var model = _mapper.Map<DoctorModel>(doctorCreateDto);
            model.Id = Guid.NewGuid();
            // 状态直接使用请求中的值，默认激活
            model.Status = doctorCreateDto.Status;

            var user = new UserModel {
                Id = Guid.NewGuid(),
                UserName = doctorCreateDto.Phone,
                RealName = doctorCreateDto.Name,
                Roles = new List<UserRole> { UserRole.DiagnosingDoctor },
                IsActive = true,
                CreatedTime = DateTime.Now,
                PhoneNumber = doctorCreateDto.Phone,
                PasswordHash = PasswordHelper.Hash(doctorCreateDto.Password)
            };

            model.UserId = user.Id;
            model.User = user;

            return await _doctorRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑医生
        /// </summary>
        public async Task<bool> UpdateAsync(DoctorEditDto doctorEditDto) {
            var model = await _doctorRepository.GetByIdAsync(doctorEditDto.Id);
            if (model == null)
                return false;

            // 使用 AutoMapper 将 DTO 属性映射到实体
            _mapper.Map(doctorEditDto, model);

            // 更新关联的用户信息
            var user = model.User;
            user.RealName = doctorEditDto.Name;
            user.PhoneNumber = doctorEditDto.Phone;

            return await _doctorRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 禁用医生
        /// </summary>
        public async Task<bool> DisableAsync(Guid id) {
            return await _doctorRepository.DisableAsync(id);
        }

        public async Task<bool> EnableAsync(Guid id) {
            return await _doctorRepository.EnableAsync(id);
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            return await _doctorRepository.BatchDisableAsync(ids);
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            return await _doctorRepository.BatchEnableAsync(ids);
        }

        public async Task<bool> ResetPasswordAsync(Guid id, string newPassword) {
            var hash = PasswordHelper.Hash(newPassword);
            return await _doctorRepository.UpdatePasswordAsync(id, hash);
        }

        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var model = await _doctorRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            if (!PasswordHelper.Verify(model.User.PasswordHash, oldPassword))
                return false;
            return await _doctorRepository.UpdatePasswordAsync(id, PasswordHelper.Hash(newPassword));
        }
    }
}