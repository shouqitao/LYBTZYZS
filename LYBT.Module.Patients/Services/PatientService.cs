using AutoMapper;
using LYBT.Common.Models;
using LYBT.Models.Patient;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Models;

namespace LYBT.Module.Patients.Services {
    /// <summary>
    /// 病人服务实现（业务逻辑层）
    /// </summary>
    public class PatientService : IPatientService {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;

        public PatientService(IPatientRepository patientRepository, IMapper mapper) {
            _patientRepository = patientRepository;
            _mapper = mapper;
        }

        public async Task<bool> AddAsync(PatientCreateDto dto) {
            // 必填项校验等业务逻辑...
            var model = _mapper.Map<PatientModel>(dto);
            model.Id = Guid.NewGuid();
            // 省略拼音码生成等细节...
            return await _patientRepository.AddAsync(model);
        }

        public async Task<bool> UpdateAsync(PatientEditDto dto) {
            var model = await _patientRepository.GetByIdAsync(dto.Id);
            if (model == null)
                throw new ArgumentException("病人不存在");

            // 赋值更新，略...
            return await _patientRepository.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(string id) {
            return await _patientRepository.DeleteAsync(id);
        }

        public async Task<PatientDetailDto> GetByIdAsync(Guid id) {
            var model = await _patientRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        public async Task<List<PatientDto>> GetAllAsync() {
            var list = await _patientRepository.GetListAsync(null, 1, int.MaxValue);
            return list.Select(_mapper.Map<PatientDto>).ToList();
        }

        public async Task<PagedResultDto<PatientDto>> GetPagedAsync(PatientPagedQueryDto query) {
            // 若你有更高阶的分页接口，可自行扩展
            var list = await _patientRepository.GetListAsync(query.Keyword, query.Page, query.PageSize);
            var total = await _patientRepository.GetCountAsync(query.Keyword);
            return new PagedResultDto<PatientDto> {
                TotalCount = total,
                Items = list.Select(_mapper.Map<PatientDto>).ToList()
            };
        }

        public async Task<int> BatchDeleteAsync(List<string> ids) {
            int count = 0;
            foreach (var id in ids) {
                if (await _patientRepository.DeleteAsync(id))
                    count++;
            }
            return count;
        }
    }
}
