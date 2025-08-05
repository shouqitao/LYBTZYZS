using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Infrastructure.Logging.Enums;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using System.Text.Json;

namespace LYBT.Module.Prescriptions.Services {

    /// <summary>
    /// 处方业务逻辑实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {
        private readonly IPrescriptionRepository _repository;
        private readonly IUnifiedLogService _logService;
        private readonly IMapper _mapper;

        public PrescriptionService(IPrescriptionRepository repository, IUnifiedLogService logService, IMapper mapper) {
            _repository = repository;
            _logService = logService;
            _mapper = mapper;
        }

        /// <summary>
        /// 执行GetAllAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<PrescriptionDto>> GetAllAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<PrescriptionDto>>(list);
        }

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        /// <param name="query">分页查询参数</param>
        /// <param name="operatorRole">操作者角色</param>
        /// <returns>分页结果</returns>
        public async Task<PaginatedResult<PrescriptionDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var allList = await _repository.GetListAsync();
            var dtoList = _mapper.Map<List<PrescriptionDto>>(allList);

            // 在内存中进行搜索和分页
            var filteredList = dtoList.AsQueryable();

            // 如果有搜索关键字，进行搜索过滤
            if (!string.IsNullOrEmpty(query.SearchKeyword)) {
                filteredList = filteredList.Where(x =>
                    x.Id.ToString().Contains(query.SearchKeyword) ||
                    x.PatientId.ToString().Contains(query.SearchKeyword) ||
                    x.DoctorId.ToString().Contains(query.SearchKeyword)
                );
            }

            var total = filteredList.Count();
            var pagedList = filteredList
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new PaginatedResult<PrescriptionDto>(pagedList, total, query.CurrentPage, query.PageSize);
        }

        /// <summary>
        /// 执行GetByIdAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <returns>返回值</returns>
        public async Task<PrescriptionDetailDto?> GetByIdAsync(string id) {
            if (!Guid.TryParse(id, out var gid))
                return null;
            var model = await _repository.GetByIdAsync(gid);
            return model == null ? null : _mapper.Map<PrescriptionDetailDto>(model);
        }

        /// <summary>
        /// 执行CreateAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<PrescriptionDto?> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName) {
            var model = _mapper.Map<LYBT.Models.Prescriptions.PrescriptionModel>(dto);
            model.Id = Guid.NewGuid();
            var success = await _repository.AddAsync(model);
            if (!success)
                return null;
                
            await _logService.CreateLogAsync(new LogCreateDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Create,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "新增处方",
                NewValue = JsonSerializer.Serialize(model)
            });
            
            // 返回创建的对象
            return _mapper.Map<PrescriptionDto>(model);
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName) {
            var old = await _repository.GetByIdAsync(dto.Id);
            if (old == null)
                return false;
            var model = _mapper.Map(dto, old);
            var success = await _repository.UpdateAsync(model);
            await _logService.CreateLogAsync(new LogCreateDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = model.Id,
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "编辑处方",
                OldValue = JsonSerializer.Serialize(old),
                NewValue = JsonSerializer.Serialize(model)
            });
            return success;
        }

        /// <summary>
        /// 执行DeleteAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName) {
            if (!Guid.TryParse(id, out var gid))
                return false;
            var item = await _repository.GetByIdAsync(gid);
            if (item == null)
                return false;
            var success = await _repository.DeleteAsync(gid);
            await _logService.CreateLogAsync(new LogCreateDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = gid,
                ActionType = ActionType.Other,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "删除处方",
                OldValue = JsonSerializer.Serialize(item)
            });
            return success;
        }

        /// <summary>
        /// 执行CancelAsync操作。
        /// </summary>
        /// <param name="id">参数id</param>
        /// <param name="operatorId">参数operatorId</param>
        /// <param name="operatorName">参数operatorName</param>
        /// <returns>返回值</returns>
        public async Task<bool> CancelAsync(string id, Guid operatorId, string operatorName) {
            if (!Guid.TryParse(id, out var gid))
                return false;
            var model = await _repository.GetByIdAsync(gid);
            if (model == null)
                return false;
            var success = await _repository.CancelAsync(gid);
            await _logService.CreateLogAsync(new LogCreateDto {
                LogType = LogType.Operation,
                ObjectType = ObjectType.Prescription,
                ObjectId = gid,
                ActionType = ActionType.Edit,
                OperatorId = operatorId,
                OperatorName = operatorName,
                Content = "作废处方",
                OldValue = JsonSerializer.Serialize(model)
            });
            return success;
        }
    }
}