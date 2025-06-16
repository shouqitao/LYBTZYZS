using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Models;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
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
        /// 获取医生列表
        /// </summary>
        public async Task<List<DoctorDto>> GetListAsync() {
            var list = await _doctorRepository.GetListAsync();
            return _mapper.Map<List<DoctorDto>>(list);
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        public async Task<bool> AddAsync(DoctorCreateDto doctorCreateDto) {
            var model = _mapper.Map<DoctorModel>(doctorCreateDto);
            model.Id = Guid.NewGuid();
            model.Status = DoctorStatus.Active;
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
            model.LicenseNumber = doctorEditDto.LicenseNumber;
            model.Title = doctorEditDto.Title;
            model.Status = doctorEditDto.Status;
            model.Remark = doctorEditDto.Remark;
            return await _doctorRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除医生
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _doctorRepository.DeleteAsync(id);
        }
    }
}
