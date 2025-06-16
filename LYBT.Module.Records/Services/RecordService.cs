using AutoMapper;
using LYBT.Models;
using LYBT.Models.Records;
using LYBT.Module.Records.Dtos;
using LYBT.Module.Records.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Records.Services {
    /// <summary>
    /// 病历业务服务实现类，封装病历相关业务逻辑
    /// </summary>
    public class RecordService : IRecordService {
        private readonly IRecordRepository _recordRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储和映射服务
        /// </summary>
        public RecordService(IRecordRepository recordRepository, IMapper mapper) {
            _recordRepository = recordRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 根据ID获取病历详情
        /// </summary>
        public async Task<RecordDetailDto?> GetByIdAsync(Guid id) {
            var model = await _recordRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<RecordDetailDto>(model);
        }

        /// <summary>
        /// 获取病历列表
        /// </summary>
        public async Task<List<RecordDto>> GetListAsync() {
            var list = await _recordRepository.GetListAsync();
            return _mapper.Map<List<RecordDto>>(list);
        }

        /// <summary>
        /// 新增病历记录
        /// </summary>
        public async Task<bool> AddAsync(RecordCreateDto recordCreateDto) {
            var model = _mapper.Map<RecordModel>(recordCreateDto);
            model.Id = Guid.NewGuid();
            model.RecordTime = DateTime.Now;
            return await _recordRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑病历记录
        /// </summary>
        public async Task<bool> UpdateAsync(RecordEditDto recordEditDto) {
            var model = await _recordRepository.GetByIdAsync(recordEditDto.Id);
            if (model == null)
                return false;

            model.Diagnosis = recordEditDto.Diagnosis;
            model.ChiefComplaint = recordEditDto.ChiefComplaint ?? model.ChiefComplaint;
            model.PresentIllness = recordEditDto.PresentIllness ?? model.PresentIllness;
            model.TreatmentAdvice = recordEditDto.TreatmentAdvice ?? model.TreatmentAdvice;
            model.PrescriptionId = recordEditDto.PrescriptionId;
            model.RecordTime = recordEditDto.RecordTime;

            return await _recordRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除病历记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _recordRepository.DeleteAsync(id);
        }
    }
}
