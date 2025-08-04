using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.EntityFrameworkCore;
using HerbStatus = LYBT.Shared.Models.Enums.HerbStatus;

namespace LYBT.Module.Herbs.Services {

    /// <summary>
    /// 药材业务服务实现类
    /// </summary>
    public class HerbService : IHerbService {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public HerbService(IHerbRepository repository, IMapper mapper, AppDbContext context) {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        /// <summary>
        /// 简单的拼音码生成方法
        /// </summary>
        private static string GetSimplePinyinCode(string name) {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            // 简化实现：只取第一个字符的大写
            return name.Substring(0, Math.Min(name.Length, 1)).ToUpperInvariant();
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
                return null;

            var dto = _mapper.Map<HerbDetailDto>(model);
            return dto;
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            var dtos = _mapper.Map<List<HerbDto>>(list);
            return dtos;
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query) {
            // 添加调试日志
            var totalInDb = await _context.Herbs.CountAsync();
            System.Diagnostics.Debug.WriteLine($"[HerbService] 数据库中总药材数: {totalInDb}");
            
            // 构建查询条件
            var dbQuery = _context.Herbs.AsQueryable();

            // 药材名称搜索
            if (!string.IsNullOrWhiteSpace(query.Name)) {
                var keyword = query.Name.Trim();
                dbQuery = dbQuery.Where(h => h.Name.Contains(keyword));
            }

            // 拼音码搜索
            if (!string.IsNullOrWhiteSpace(query.PinYinCode)) {
                var pinyin = query.PinYinCode.Trim().ToUpperInvariant();
                dbQuery = dbQuery.Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin));
            }

            // 五笔码搜索
            if (!string.IsNullOrWhiteSpace(query.WuBiCode)) {
                var wubi = query.WuBiCode.Trim().ToUpperInvariant();
                dbQuery = dbQuery.Where(h => h.WuBiCode != null && h.WuBiCode.Contains(wubi));
            }

            // 产地搜索
            if (!string.IsNullOrWhiteSpace(query.Origin)) {
                var origin = query.Origin.Trim();
                dbQuery = dbQuery.Where(h => h.Origin != null && h.Origin.Contains(origin));
            }

            // 批号搜索
            if (!string.IsNullOrWhiteSpace(query.BatchNo)) {
                var batchNo = query.BatchNo.Trim();
                dbQuery = dbQuery.Where(h => h.BatchNo != null && h.BatchNo.Contains(batchNo));
            }

            // 状态筛选
            if (query.Status.HasValue) {
                dbQuery = dbQuery.Where(h => h.Status == query.Status.Value);
            }

            // 是否包含停用药材
            if (!query.IncludeInactive) {
                dbQuery = dbQuery.Where(h => h.Status != HerbStatus.Inactive);
            }

            // 库存范围筛选
            if (query.MinStock.HasValue) {
                dbQuery = dbQuery.Where(h => h.Stock >= query.MinStock.Value);
            }
            if (query.MaxStock.HasValue) {
                dbQuery = dbQuery.Where(h => h.Stock <= query.MaxStock.Value);
            }

            // 价格范围筛选
            if (query.MinPrice.HasValue) {
                dbQuery = dbQuery.Where(h => h.Price >= query.MinPrice.Value);
            }
            if (query.MaxPrice.HasValue) {
                dbQuery = dbQuery.Where(h => h.Price <= query.MaxPrice.Value);
            }

            // 有效期范围筛选
            if (query.ExpireStartDate.HasValue) {
                dbQuery = dbQuery.Where(h => h.ExpireDate >= query.ExpireStartDate.Value);
            }
            if (query.ExpireEndDate.HasValue) {
                dbQuery = dbQuery.Where(h => h.ExpireDate <= query.ExpireEndDate.Value);
            }

            // 库存不足筛选
            if (query.OnlyLowStock) {
                dbQuery = dbQuery.Where(h => h.Stock <= query.LowStockThreshold);
            }

            // 即将过期筛选
            if (query.OnlyExpiring) {
                var expiringDate = DateTime.UtcNow.AddDays(query.ExpiringDaysThreshold);
                dbQuery = dbQuery.Where(h => h.ExpireDate.HasValue && h.ExpireDate <= expiringDate);
            }

            // 获取总数
            var total = await dbQuery.CountAsync();
            System.Diagnostics.Debug.WriteLine($"[HerbService] 应用过滤后药材数: {total}");

            // 分页查询
            var models = await dbQuery
                .OrderByDescending(h => h.CreateTime)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
                
            System.Diagnostics.Debug.WriteLine($"[HerbService] 分页查询返回: {models.Count} 条记录");

            var dtos = _mapper.Map<List<HerbDto>>(models);

            // 设置状态描述
            foreach (var dto in dtos) {
                var model = models.First(x => x.Id == dto.Id);
            }

            return new PaginatedResult<HerbDto> {
                TotalCount = total,
                Items = dtos,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<bool> AddAsync(HerbCreateDto dto) {
            var model = _mapper.Map<HerbModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                ? GetSimplePinyinCode(model.Name)
                : dto.PinYinCode;
            model.CreateTime = DateTime.UtcNow;
            return await _repository.AddAsync(model);
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        public async Task<bool> UpdateAsync(HerbUpdateDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;

            model.Name = dto.Name;
            model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                ? GetSimplePinyinCode(dto.Name)
                : dto.PinYinCode;
            model.Origin = dto.Origin;
            model.Unit = dto.Unit;
            model.Price = dto.Price;
            model.Stock = dto.Stock;
            model.BatchNo = dto.BatchNo;
            model.ExpireDate = dto.ExpireDate;
            model.Effect = dto.Effect;
            model.Remark = dto.Remark;
            model.Status = dto.Status;
            model.UpdateTime = DateTime.UtcNow;

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<int> ImportAsync(List<HerbImportDto> dtos) {
            var models = new List<HerbModel>();
            foreach (var dto in dtos) {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = GetSimplePinyinCode(model.Name);
                model.CreateTime = DateTime.UtcNow;
                models.Add(model);
            }

            var result = await _repository.AddRangeAsync(models);
            return result ? models.Count : 0;
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbDetailDto>> ExportAsync() {
            var list = await _repository.GetListAsync();
            var dtos = _mapper.Map<List<HerbDetailDto>>(list);

            return dtos;
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        public async Task<bool> UpdateStatusAsync(HerbStatusUpdateDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;

            model.Status = dto.Status;
            model.UpdateTime = DateTime.UtcNow;
            // 如果有原因，可以记录到备注中
            if (!string.IsNullOrWhiteSpace(dto.Reason)) {
                model.Remark = $"{model.Remark} [状态变更: {dto.Reason}]".Trim();
            }

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        public async Task<int> BatchUpdateStatusAsync(List<Guid> ids, string reason) {
            if (!ids.Any()) {
                return 0;
            }

            // 使用原生SQL避免EF Core的Contains转换问题
            var idStrings = string.Join("','", ids.Select(id => id.ToString()));
            var sql = $"SELECT * FROM Herbs WHERE Id IN ('{idStrings}')";

            var models = await _context.Herbs.FromSqlRaw(sql).ToListAsync();

            int count = 0;
            foreach (var model in models) {
                model.Status = HerbStatus.Inactive; // Default status for batch operations
                model.UpdateTime = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(reason)) {
                    model.Remark = $"{model.Remark} [批量状态变更: {reason}]".Trim();
                }
                count++;
            }

            await _context.SaveChangesAsync();
            return count;
        }

        /// <summary>
        /// 根据状态获取药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetByStatusAsync(HerbStatus status) {
            var models = await _context.Herbs
                .Where(h => h.Status == status)
                .OrderByDescending(h => h.CreateTime)
                .ToListAsync();

            var dtos = _mapper.Map<List<HerbDto>>(models);
            foreach (var dto in dtos) {
            }

            return dtos;
        }

        /// <summary>
        /// 获取可用药材列表（状态为Active）
        /// </summary>
        public async Task<List<HerbDto>> GetAvailableHerbsAsync() {
            return await GetByStatusAsync(HerbStatus.Active);
        }

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetOutOfStockHerbsAsync() {
            return await GetByStatusAsync(HerbStatus.OutOfStock);
        }

        /// <summary>
        /// 获取即将过期药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetExpiringHerbsAsync(int days = 30) {
            var expiringDate = DateTime.UtcNow.AddDays(days);
            var models = await _context.Herbs
                .Where(h => h.ExpireDate.HasValue && h.ExpireDate <= expiringDate && h.Status == HerbStatus.Active)
                .OrderBy(h => h.ExpireDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<HerbDto>>(models);
            foreach (var dto in dtos) {
                var model = models.First(x => x.Id == dto.Id);
            }

            return dtos;
        }

        /// <summary>
        /// 检查药材状态并自动更新过期药材
        /// </summary>
        public async Task<int> CheckAndUpdateExpiredHerbsAsync() {
            var expiredHerbs = await _context.Herbs
                .Where(h => h.ExpireDate.HasValue &&
                           h.ExpireDate <= DateTime.UtcNow &&
                           h.Status != HerbStatus.Expired)
                .ToListAsync();

            foreach (var herb in expiredHerbs) {
                herb.Status = HerbStatus.Expired;
                herb.UpdateTime = DateTime.UtcNow;
                herb.Remark = $"{herb.Remark} [系统自动标记为过期]".Trim();
            }

            await _context.SaveChangesAsync();
            return expiredHerbs.Count;
        }

        /// <summary>
        /// 获取药材状态统计信息
        /// </summary>
        public async Task<Dictionary<HerbStatus, int>> GetStatusStatisticsAsync() {
            var statistics = await _context.Herbs
                .GroupBy(h => h.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // 确保所有状态都有统计数据，即使数量为0
            foreach (HerbStatus status in Enum.GetValues<HerbStatus>()) {
                if (!statistics.ContainsKey(status)) {
                    statistics[status] = 0;
                }
            }

            return statistics;
        }

        /// <summary>
        /// 获取全部活动状态药材（用于处方检查）
        /// </summary>
        /// <returns>活动状态药材列表</returns>
        public async Task<List<HerbDto>> GetAllActiveHerbsAsync() {
            var models = await _context.Herbs
                .Where(h => h.Status == HerbStatus.Active)
                .OrderBy(h => h.Name)
                .ToListAsync();

            var dtos = _mapper.Map<List<HerbDto>>(models);
            foreach (var dto in dtos) {
                var model = models.First(x => x.Id == dto.Id);
            }

            return dtos;
        }
    }
}