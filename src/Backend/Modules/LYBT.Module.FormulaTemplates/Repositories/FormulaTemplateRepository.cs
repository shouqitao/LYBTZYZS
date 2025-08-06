using LYBT.Infrastructure.Data;
using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.FormulaTemplates.Repositories {

    /// <summary>
    /// 经验方模板仓储实现类，数据库操作
    /// </summary>
    public class FormulaTemplateRepository : IFormulaTemplateRepository {
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public FormulaTemplateRepository(AppDbContext context) {
            _context = context;
        }

        /// <summary>
        /// 获取模板详情
        /// </summary>
        public async Task<FormulaTemplateModel?> GetByIdAsync(Guid id) {
            return await _context.FormulaTemplates.FindAsync(id);
        }

        /// <summary>
        /// 获取全部模板列表
        /// </summary>
        public async Task<List<FormulaTemplateModel>> GetListAsync() {
            return await _context.FormulaTemplates
                .Where(f => f.IsActive)
                .OrderByDescending(f => f.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询验方模板列表
        /// </summary>
        public async Task<(List<FormulaTemplateModel> list, int total)> GetPagedAsync(PaginationRequest query, UserRole operatorRole) {
            var queryable = _context.FormulaTemplates.AsQueryable();

            // 根据操作者角色决定数据访问权限
            if (operatorRole != UserRole.Admin) {
                // 非管理员只能查看活跃状态的模板
                queryable = queryable.Where(f => f.IsActive);
            }

            // 总数统计
            var total = await queryable.CountAsync();

            // 分页和排序
            var list = await queryable
                .OrderByDescending(f => f.CreateTime)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (list, total);
        }

        /// <summary>
        /// 新增模板
        /// </summary>
        public async Task<bool> AddAsync(FormulaTemplateModel model) {
            _context.FormulaTemplates.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        public async Task<bool> UpdateAsync(FormulaTemplateModel model) {
            _context.FormulaTemplates.Update(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _context.FormulaTemplates.FindAsync(id);
            if (model == null)
                return false;
            _context.FormulaTemplates.Remove(model);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// 批量导入模板
        /// </summary>
        /// <param name="dtos">导入数据</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="operatorName">操作者姓名</param>
        /// <returns>导入数量</returns>
        public async Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos, Guid operatorId, string operatorName) {
            var models = new List<FormulaTemplateModel>();

            foreach (var dto in dtos) {
                var model = new FormulaTemplateModel {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Herbs = dto.Herbs.Select(h => new FormulaTemplateHerbItem {
                        HerbId = h.HerbId,
                        HerbName = h.Name,
                        Quantity = h.Quantity,
                        Unit = h.Unit ?? "g"
                    }).ToList(),
                    Remark = dto.Remark,
                    CreatedById = operatorId,
                    CreateTime = DateTime.Now,
                    IsActive = true
                };
                models.Add(model);
            }

            _context.FormulaTemplates.AddRange(models);
            var saved = await _context.SaveChangesAsync();
            return saved > 0 ? models.Count : 0;
        }

        /// <summary>
        /// 执行ExportAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<FormulaTemplateDetailDto>> ExportAsync() {
            var list = await _context.FormulaTemplates.ToListAsync();
            return list.Select(m => new FormulaTemplateDetailDto {
                Id = m.Id,
                Name = m.Name,
                Usage = m.Usage,
                Herbs = m.Herbs.Select(h => new FormulaTemplateHerbDto {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Unit = h.Unit,
                    Remark = h.Remark
                }).ToList(),
                Remark = m.Remark,
                CreateTime = m.CreateTime,
                UpdateTime = m.UpdateTime
            }).ToList();
        }

        /// <summary>
        /// 获取所有活动状态的验方模板
        /// </summary>
        public async Task<List<FormulaTemplateModel>> GetAllActiveAsync() {
            return await _context.FormulaTemplates
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定医生可见的验方模板（包括共享验方和自己创建的验方）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>可见的验方模板列表</returns>
        public async Task<List<FormulaTemplateModel>> GetVisibleForDoctorAsync(Guid doctorId) {
            return await _context.FormulaTemplates
                .Where(f => f.IsActive && (f.IsShared || f.CreatedById == doctorId))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 设置验方模板共享状态
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="isShared">是否共享</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SetSharingStatusAsync(Guid templateId, bool isShared, Guid operatorId) {
            var template = await _context.FormulaTemplates.FindAsync(templateId);
            if (template == null)
                return false;

            template.IsShared = isShared;
            template.UpdateTime = DateTime.Now;

            if (isShared) {
                template.SharedAt = DateTime.Now;
                template.SharedById = operatorId;
            } else {
                template.SharedAt = null;
                template.SharedById = null;
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}