using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Models;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            model.Status = DoctorStatus.Active;
            model.PasswordHash = HashPassword("123456");
            return await _doctorRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑医生
        /// </summary>
        public async Task<bool> UpdateAsync(DoctorEditDto doctorEditDto) {
            var model = await _doctorRepository.GetByIdAsync(doctorEditDto.Id);
            if (model == null)
                return false;
            model.Name = doctorEditDto.Name;
            model.Gender = doctorEditDto.Gender;
            model.Birthday = doctorEditDto.Birthday;
            model.Phone = doctorEditDto.Phone;
            model.PinyinCode = doctorEditDto.PinyinCode;
            model.LicenseNumber = doctorEditDto.LicenseNumber;
            model.Title = doctorEditDto.Title;
            model.Status = doctorEditDto.Status;
            model.Remark = doctorEditDto.Remark;
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
            var hash = HashPassword(newPassword);
            return await _doctorRepository.UpdatePasswordAsync(id, hash);
        }

        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var model = await _doctorRepository.GetByIdAsync(id);
            if (model == null) return false;
            if (model.PasswordHash != HashPassword(oldPassword)) return false;
            return await _doctorRepository.UpdatePasswordAsync(id, HashPassword(newPassword));
        }

        private static string HashPassword(string password) {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }
    }
}
