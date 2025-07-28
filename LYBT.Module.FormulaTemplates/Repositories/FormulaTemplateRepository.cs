using LYBT.Models.FormulaTemplates;
using LYBT.Models.Herbs;
using LYBT.Module.FormulaTemplates.Data;
using LYBT.Module.FormulaTemplates.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.FormulaTemplates.Repositories {

    /// <summary>
    /// 经验方模板仓储实现类，数据库操作
    /// </summary>
    public class FormulaTemplateRepository : IFormulaTemplateRepository {
        private readonly FormulaTemplateDbContext _context;

        /// <summary>
        /// 构造方法，注入数据库上下文
        /// </summary>
        public FormulaTemplateRepository(FormulaTemplateDbContext context) {
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
            return await Task.FromResult(_context.FormulaTemplates.ToList());
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
        /// 执行ImportAsync操作。
        /// </summary>
        /// <param name="dtos">参数dtos</param>
        /// <returns>返回值</returns>
        public async Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos) {
            int count = 0;
            foreach (var dto in dtos) {
                var model = new FormulaTemplateModel {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Herbs = dto.Herbs.Select(h => new FormulaTemplateHerbItem {
                        HerbId = h.Id,
                        HerbName = h.Name,
                        Quantity = h.Price, // Using Price as default quantity for now
                        Unit = "g" // Default unit
                    }).ToList(),
                    Remark = dto.Remark
                };
                _context.FormulaTemplates.Add(model);
                count += await _context.SaveChangesAsync() > 0 ? 1 : 0;
            }
            return count;
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
                Herbs = m.Herbs.Select(h => new HerbDto {
                    Id = h.HerbId,
                    Name = h.HerbName,
                    Price = 0 // FormulaTemplateHerbItem doesn't have UnitPrice, using 0 as default
                }).ToList(),
                Remark = m.Remark
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
            template.UpdatedAt = DateTime.Now;

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