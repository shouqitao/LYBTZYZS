using AutoMapper;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.DiagnosisTreatment.Services {

    /// <summary>
    /// 诊疗业务服务实现类，实现诊疗模块的业务逻辑
    /// </summary>
    public class DiagnosisTreatmentService : IDiagnosisTreatmentService {
        private readonly IDiagnosisTreatmentRepository _diagnosisTreatmentRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入诊疗仓储与映射器
        /// </summary>
        public DiagnosisTreatmentService(IDiagnosisTreatmentRepository diagnosisTreatmentRepository, IMapper mapper) {
            _diagnosisTreatmentRepository = diagnosisTreatmentRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        public async Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id) {
            var model = await _diagnosisTreatmentRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<DiagnosisTreatmentDetailDto>(model);
        }

        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        public async Task<List<DiagnosisTreatmentDto>> GetListAsync() {
            var list = await _diagnosisTreatmentRepository.GetListAsync();
            return _mapper.Map<List<DiagnosisTreatmentDto>>(list);
        }

        /// <summary>
        /// 分页获取诊疗列表
        /// </summary>
        public async Task<PaginatedResult<DiagnosisTreatmentDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var allList = await _diagnosisTreatmentRepository.GetListAsync();
            var dtoList = _mapper.Map<List<DiagnosisTreatmentDto>>(allList);

            // 在内存中进行搜索和分页
            var filteredList = dtoList.AsQueryable();

            // 如果有搜索关键字，进行搜索过滤
            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    (x.PatientName != null && x.PatientName.Contains(query.SearchKeyword)) ||
                    (x.Diagnosis != null && x.Diagnosis.Contains(query.SearchKeyword))
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<DiagnosisTreatmentDto>(pagedList, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 新增诊疗
        /// </summary>
        public async Task<DiagnosisTreatmentDto?> AddAsync(DiagnosisTreatmentCreateDto dto) {
            var model = _mapper.Map<DiagnosisTreatmentModel>(dto);
            model.Id = Guid.NewGuid();
            model.CreateTime = DateTime.Now;
            var result = await _diagnosisTreatmentRepository.AddAsync(model);
            if (!result)
                return null;
            
            // 返回创建的对象
            return _mapper.Map<DiagnosisTreatmentDto>(model);
        }

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        public async Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto) {
            var model = await _diagnosisTreatmentRepository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.ChiefComplaint = dto.ChiefComplaint;
            model.PresentIllness = dto.PresentIllness;
            model.DiagnosisCatalogId = dto.DiagnosisCatalogId;
            model.Diagnosis = dto.Diagnosis;
            model.Treatments = _mapper.Map<List<TreatmentItemModel>>(dto.Treatments);
            model.Formula = _mapper.Map<FormulaModel>(dto.Formula);
            return await _diagnosisTreatmentRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除诊疗
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _diagnosisTreatmentRepository.DeleteAsync(id);
        }
    }
}