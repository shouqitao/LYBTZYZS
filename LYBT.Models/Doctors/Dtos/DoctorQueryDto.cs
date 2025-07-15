using System.ComponentModel;
namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 医生分页与条件查询 DTO
    /// </summary>
    public class DoctorQueryDto {

        /// <summary>关键词（姓名/拼音码/手机号）</summary>
        [DisplayName("关键词（姓名/拼音码/手机号）")]
        public string? Keyword { get; set; }

        /// <summary>在职状态筛选</summary>
        [DisplayName("在职状态筛选")]
        public bool? IsActive { get; set; }

        /// <summary>页码，从1开始</summary>
        [DisplayName("页码，从1开始")]
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        [DisplayName("每页数量")]
        public int PageSize { get; set; } = 20;
    }
}