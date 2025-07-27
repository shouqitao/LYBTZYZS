using AutoMapper;
using LYBT.Common.Enums.Logs;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Module.Records.Dtos;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Models;
using LYBT.Module.Records.Models.Dtos;
using System.Text.Json;

namespace LYBT.Module.Records.Services {

    /// <summary>
    /// 病历业务服务实现类，封装病历相关业务逻辑
    /// </summary>
    public class RecordService : IRecordService {
        private readonly IRecordRepository _recordRepository;
        private readonly IMapper _mapper;
        private readonly IUnifiedLogService _logService;

        /// <summary>
        /// 构造方法，注入仓储和映射服务
        /// </summary>
        public RecordService(IRecordRepository recordRepository, IMapper mapper, IUnifiedLogService logService) {
            _recordRepository = recordRepository;
            _mapper = mapper;
            _logService = logService;
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
        public async Task<bool> AddAsync(RecordCreateDto recordCreateDto, Guid operatorId, string operatorName) {
            var model = _mapper.Map<RecordModel>(recordCreateDto);
            model.Id = Guid.NewGuid();
            model.RecordTime = DateTime.Now;
            model.CreatedBy = operatorId.ToString();
            model.CreatedTime = DateTime.Now;
            var result = await _recordRepository.AddAsync(model);

            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Record,
                    ObjectId = model.Id,
                    ActionType = ActionType.Create,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                        Content = "新增病历",
                    NewValue = JsonSerializer.Serialize(model)
                });
            }

            return result;
        }

        /// <summary>
        /// 编辑病历记录
        /// </summary>
        public async Task<bool> UpdateAsync(RecordEditDto recordEditDto, Guid operatorId, string operatorName) {
            var model = await _recordRepository.GetByIdAsync(recordEditDto.Id);
            if (model == null)
                return false;

            var oldJson = JsonSerializer.Serialize(model);

            model.Diagnosis = recordEditDto.Diagnosis;
            model.ChiefComplaint = recordEditDto.ChiefComplaint ?? model.ChiefComplaint;
            model.PresentIllness = recordEditDto.PresentIllness ?? model.PresentIllness;
            model.TreatmentAdvice = recordEditDto.TreatmentAdvice ?? model.TreatmentAdvice;
            model.PrescriptionId = recordEditDto.PrescriptionId;
            model.DiagnosisResults = recordEditDto.DiagnosisResults;
            model.HerbalFormula = recordEditDto.HerbalFormula;
            model.TreatmentPlans = recordEditDto.TreatmentPlans;
            model.IsShared = recordEditDto.IsShared;
            model.SharedToDoctorIds = recordEditDto.SharedToDoctorIds;
            model.RecordTime = recordEditDto.RecordTime;

            var result = await _recordRepository.UpdateAsync(model);

            if (result) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Record,
                    ObjectId = model.Id,
                    ActionType = ActionType.Edit,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                        Content = "编辑病历",
                    OldValue = oldJson,
                    NewValue = JsonSerializer.Serialize(recordEditDto)
                });
            }

            return result;
        }

        /// <summary>
        /// 删除病历记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName) {
            var record = await _recordRepository.GetByIdAsync(id);
            var result = await _recordRepository.DeleteAsync(id);

            if (result && record != null) {
                await _logService.CreateLogAsync(new LogCreateDto {
                    LogType = LogType.Operation,
                    ObjectType = ObjectType.Record,
                    ObjectId = id,
                    ActionType = ActionType.Other,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                        Content = "删除病历",
                    OldValue = JsonSerializer.Serialize(record)
                });
            }

            return result;
        }

        /// <summary>
        /// 执行GetByPatientIdAsync操作。
        /// </summary>
        /// <param name="patientId">参数patientId</param>
        /// <returns>返回值</returns>
        public async Task<List<RecordDto>> GetByPatientIdAsync(Guid patientId) {
            var list = await _recordRepository.GetListByPatientIdAsync(patientId);
            return _mapper.Map<List<RecordDto>>(list);
        }

        /// <summary>
        /// 执行MarkAsSharedAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <param name="doctorIds">参数doctorIds</param>
        /// <returns>返回值</returns>
        public async Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds) {
            var model = await _recordRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.IsShared = true;
            model.SharedToDoctorIds = doctorIds;
            return await _recordRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 执行RevokeSharingAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<bool> RevokeSharingAsync(Guid id) {
            var model = await _recordRepository.GetByIdAsync(id);
            if (model == null)
                return false;
            model.IsShared = false;
            model.SharedToDoctorIds = new List<string>();
            return await _recordRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 执行GetSharedRecordsAsync操作。
        /// </summary>
        /// <param name="doctorId">参数doctorId</param>
        /// <returns>返回值</returns>
        public async Task<List<RecordDto>> GetSharedRecordsAsync(Guid doctorId) {
            var list = await _recordRepository.GetSharedRecordsAsync(doctorId);
            return _mapper.Map<List<RecordDto>>(list);
        }
    }
}