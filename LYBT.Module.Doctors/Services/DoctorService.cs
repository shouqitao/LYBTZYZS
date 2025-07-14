using AutoMapper;
using LYBT.Common.Helpers;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Common.Models;
using LYBT.Module.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Services {
    /// <summary>
    /// 医生业务服务实现类，实现医生业务逻辑
    /// </summary>
    public class DoctorService : IDoctorService {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public DoctorService(IDoctorRepository doctorRepository,
            IUserRepository userRepository,
            IMapper mapper) {
            _doctorRepository = doctorRepository;
            _userRepository = userRepository;
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

        public async Task<bool> AddAsync(DoctorDetailDto dto) {
            if (dto.UserId == Guid.Empty)
                throw new Exception("UserId不能为空");

            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null)
                throw new Exception("关联的用户不存在，请先创建用户。");

            if (await _doctorRepository.GetByUserIdAsync(dto.UserId) != null)
                throw new Exception("该用户已经关联医生档案，请勿重复创建。");

            var model = _mapper.Map<DoctorModel>(dto);
            model.Id = Guid.NewGuid();
            try {
                return await _doctorRepository.AddAsync(model);
            } catch (DbUpdateException ex) {
                throw new Exception("保存医生信息时发生错误", ex);
            }
        }

        public async Task<bool> UpdateAsync(DoctorDetailDto dto) {
            var model = await _doctorRepository.GetByIdAsync(dto.Id);
            if (model == null)
                throw new Exception("医生不存在。");
            // 只允许更新医生表自身字段，不允许更新User相关（账号、姓名）
            model.Gender = dto.Gender;
            model.Birthday = dto.Birthday ?? model.Birthday;
            model.Title = dto.Title;
            model.LicenseNumber = dto.LicenseNumber;
            model.Specialty = dto.Specialty;
            model.Status = dto.Status;
            model.WorkStatus = dto.WorkStatus;
            model.PinyinCode = dto.PinyinCode;
            model.Remark = dto.Remark;
            model.ContactNumber = dto.ContactNumber;
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
    }
}