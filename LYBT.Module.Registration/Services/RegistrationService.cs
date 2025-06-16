using AutoMapper;
using LYBT.Common.Enums;
using LYBT.Models;
using LYBT.Models.Registration;
using LYBT.Module.Registration.Dtos;
using LYBT.Module.Registration.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Registration.Services {
    /// <summary>
    /// 挂号业务服务实现类
    /// </summary>
    public class RegistrationService : IRegistrationService {
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public RegistrationService(IRegistrationRepository registrationRepository, IMapper mapper) {
            _registrationRepository = registrationRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id) {
            var model = await _registrationRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<RegistrationDetailDto>(model);
        }

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        public async Task<List<RegistrationDto>> GetListAsync() {
            var list = await _registrationRepository.GetListAsync();
            return _mapper.Map<List<RegistrationDto>>(list);
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        public async Task<bool> AddAsync(RegistrationCreateDto dto) {
            var model = _mapper.Map<RegistrationModel>(dto);
            model.Id = Guid.NewGuid();
            model.RegistrationTime = DateTime.Now;
            model.Status = RegistrationStatus.Pending;
            return await _registrationRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationEditDto dto) {
            var model = await _registrationRepository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.RegistrationType = dto.RegistrationType;
            model.Remark = dto.Remark;
            return await _registrationRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _registrationRepository.DeleteAsync(id);
        }
    }
}
