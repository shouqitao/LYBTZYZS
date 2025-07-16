using LYBT.Module.Herbs.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IHerbApi {
        [Get("/api/herbs")]
        Task<List<HerbDto>> GetListAsync();

        [Get("/api/herbs/{id}")]
        Task<HerbDetailDto> GetByIdAsync(Guid id);

        [Post("/api/herbs")]
        Task<ApiSuccessResponse> AddAsync([Body] HerbCreateDto dto);

        [Put("/api/herbs")]
        Task<ApiSuccessResponse> UpdateAsync([Body] HerbEditDto dto);

        [Delete("/api/herbs/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);

        [Post("/api/herbs/import")]
        Task<ApiSuccessResponse> ImportAsync([Body] List<HerbImportDto> dtos);

        [Post("/api/herbs/export")]
        Task<List<HerbDetailDto>> ExportAsync();

        [Multipart]
        [Post("/api/herbs/importExcel")]
        Task<ImportCountResponse> ImportExcelAsync([AliasAs("file")] StreamPart file);

        [Get("/api/herbs/exportExcel")]
        Task<HttpContent> ExportExcelAsync();
    }
}
