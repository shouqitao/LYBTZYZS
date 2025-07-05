using AutoMapper;
using LYBT.Common.Helpers;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Common.Models; // 正确的using指令，PagedResultDto定义于此
using System.Collections.Generic;

namespace LYBT.Module.Doctors.Services {
    /// <summary>
    /// 医生业务服务实现类，实现医生业务逻辑
    /// </summary>
    public class DoctorService : IDoctorService {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IMapper _mapper;

        public DoctorService(IDoctorRepository doctorRepository, IMapper mapper) {
            _doctorRepository = doctorRepository;
            _mapper = mapper;
        }

        public async Task<DoctorDetailDto?> GetByIdAsync(Guid id) {
            var model = await _doctorRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<DoctorDetailDto>(model);
        }

        public async Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId) {
            var model = await _doctorRepository.GetByUserIdAsync(userId);
            return model == null ? null : _mapper.Map<DoctorDetailDto>(model);
        }

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

        public async Task<bool> AddAsync(DoctorCreateDto doctorCreateDto) {
            var model = _mapper.Map<DoctorModel>(doctorCreateDto);
            model.Id = Guid.NewGuid();
            return await _doctorRepository.AddAsync(model);
        }

        public async Task<bool> UpdateAsync(DoctorEditDto doctorEditDto) {
            var model = await _doctorRepository.GetByIdAsync(doctorEditDto.Id);
            if (model == null)
                throw new Exception("医生不存在。");
            // 只允许更新医生表自身字段，不允许更新User相关（账号、姓名）
            model.Gender = doctorEditDto.Gender;
            model.Birthday = doctorEditDto.Birthday ?? model.Birthday;
            model.Title = doctorEditDto.Title;
            model.LicenseNumber = doctorEditDto.LicenseNumber;
            model.Specialty = doctorEditDto.Specialty;
            model.Status = doctorEditDto.Status;
            model.WorkStatus = doctorEditDto.WorkStatus;
            model.PinyinCode = doctorEditDto.PinyinCode;
            model.Remark = doctorEditDto.Remark;
            model.ContactNumber = doctorEditDto.ContactNumber;
            // 不更新UserId、User
            return await _doctorRepository.UpdateAsync(model);
        }

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