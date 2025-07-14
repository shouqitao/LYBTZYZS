using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 HerbService 的说明
    /// </summary>
    public class HerbService : IHerbService {
        private readonly IHerbApi _api;

        public HerbService(IHerbApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<HerbDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(HerbDetailDto dto) {
            var create = new HerbCreateDto {
                Name = dto.Name,
                Pinyin = dto.Pinyin,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Stock = dto.Stock,
                BatchNo = dto.BatchNo,
                ExpireDate = dto.ExpireDate,
                Effect = dto.Effect,
                Remark = dto.Remark
            };
            var resp = await _api.AddAsync(create);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(HerbDetailDto dto) {
            var edit = new HerbEditDto {
                Id = dto.Id,
                Name = dto.Name,
                Pinyin = dto.Pinyin,
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Stock = dto.Stock,
                BatchNo = dto.BatchNo,
                ExpireDate = dto.ExpireDate,
                Effect = dto.Effect,
                Remark = dto.Remark
            };
            var resp = await _api.UpdateAsync(edit);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteAsync 的说明
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }

        public async Task<int> ImportAsync(IList<HerbDetailDto> dtos) {
            var list = new List<HerbImportDto>();
            foreach (var d in dtos) {
                list.Add(new HerbImportDto {
                    Name = d.Name,
                    Pinyin = d.Pinyin,
                    Origin = d.Origin,
                    Spec = d.Spec,
                    Unit = d.Unit,
                    Price = d.Price,
                    Stock = d.Stock,
                    BatchNo = d.BatchNo,
                    ExpireDate = d.ExpireDate,
                    Effect = d.Effect,
                    Remark = d.Remark
                });
            }
            var resp = await _api.ImportAsync(list);
            return resp.Success ? list.Count : 0;
        }

        public async Task<IList<HerbDetailDto>> ExportAsync() {
            return await _api.ExportAsync();
        }
    }
}
